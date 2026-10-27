using AwesomeAssertions;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using static Files.Tests.Integration.Files.ModuleHarness;

namespace Files.Tests.Integration.Files;

/// <summary>The module's store: reads whole and ranged, newest-first pages, renames within the type, delete, restore
/// and expiry on a fake clock.</summary>
public sealed class ModuleStoreTests : IAsyncLifetime
{
    private ModuleHarness harness = null!;

    public async Task InitializeAsync() => harness = await CreateAsync(options =>
        options.Policies["brief"] = new() { DeletedRetention = TimeSpan.FromMinutes(5) });

    public async Task DisposeAsync() => await harness.DisposeAsync();

    [Fact]
    public async Task OpenReadAsync_ShouldReturnTheBytes_WhenNoRangeIsGiven()
    {
        var file = Value(await harness.UploadAsync(Pdf, "doc.pdf"));

        var bytes = await harness.StoreAsync(async store => await ReadAsync(Value(await store.OpenReadAsync(file.Id))));

        bytes.Should().Equal(Pdf);
    }

    [Fact]
    public async Task OpenReadAsync_ShouldReturnTheRange_WhenARangeIsGiven()
    {
        var file = Value(await harness.UploadAsync(Pdf, "doc.pdf"));

        var (content, bytes) = await harness.StoreAsync(async store =>
        {
            var opened = Value(await store.OpenReadAsync(file.Id, new ByteRange(2, 5)));
            return (opened, await ReadAsync(opened));
        });

        bytes.Should().Equal(Pdf[2..6]);
        content.Should().Match<FileContent>(read => read.Length == 4 && read.TotalLength == Pdf.Length && read.Range == new ByteRange(2, 5));
    }

    [Fact]
    public async Task OpenReadAsync_ShouldClampTheEnd_WhenTheRangeRunsPastIt()
    {
        var file = Value(await harness.UploadAsync(Pdf, "doc.pdf"));

        var bytes = await harness.StoreAsync(async store => await ReadAsync(Value(await store.OpenReadAsync(file.Id, new ByteRange(Pdf.Length - 3, null)))));

        bytes.Should().Equal(Pdf[^3..]);
    }

    [Fact]
    public async Task OpenReadAsync_ShouldFailWithRangeInvalid_WhenTheRangeStartsPastTheEnd()
    {
        var file = Value(await harness.UploadAsync(Pdf, "doc.pdf"));

        Error(await harness.StoreAsync(store => store.OpenReadAsync(file.Id, new ByteRange(Pdf.Length, null))))
            .Message.Should().Contain("byte range");
    }

    [Fact]
    public async Task ListAsync_ShouldPageNewestFirst_WhenFilesExceedTheLimit()
    {
        var ids = new List<FileId>();
        for (var index = 0; index < 5; index++)
            ids.Add(Value(await harness.UploadAsync(Png, $"p{index}.png")).Id);

        var pages = new List<FilePage>();
        string? cursor = null;
        do
        {
            var page = Value(await harness.StoreAsync(store => store.ListAsync(new FileQuery { Limit = 2, Cursor = cursor })));
            pages.Add(page);
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        pages.Select(page => page.Items.Count).Should().Equal(2, 2, 1);
        pages.SelectMany(page => page.Items).Select(file => file.Id).Should().Equal(ids.AsEnumerable().Reverse());
    }

    [Fact]
    public async Task ListAsync_ShouldApplyTheFilters_WhenTheyAreGiven()
    {
        Value(await harness.UploadAsync(Png, "a.png", appId: "brief"));
        Value(await harness.UploadAsync(Pdf, "b.pdf"));

        var images = Value(await harness.StoreAsync(store => store.ListAsync(new FileQuery { Kind = FileKind.Image })));
        var brief = Value(await harness.StoreAsync(store => store.ListAsync(new FileQuery { AppId = "brief" })));
        var later = Value(await harness.StoreAsync(store => store.ListAsync(new FileQuery { CreatedAfterUtc = harness.Clock.GetUtcNow() })));

        images.Items.Should().ContainSingle().Which.Name.Should().Be("a.png");
        brief.Items.Should().ContainSingle().Which.Name.Should().Be("a.png");
        later.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsync_ShouldFailWithCursorInvalid_WhenTheCursorWasNotIssued()
    {
        Error(await harness.StoreAsync(store => store.ListAsync(new FileQuery { Cursor = "abc" }))).Type.Should().Be(AppErrorType.Validation);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRenameWithinTheType_AndRefuseAnotherType()
    {
        var file = Value(await harness.UploadAsync(Jpeg, "a.jpg"));

        Value(await harness.StoreAsync(store => store.UpdateAsync(file.Id, new FilePatch { Name = "../b.jpeg" }))).Name.Should().Be("b.jpeg");
        Value(await harness.StoreAsync(store => store.UpdateAsync(file.Id, new FilePatch { Name = "notes.xyz" }))).Name.Should().Be("notes.xyz");
        Error(await harness.StoreAsync(store => store.UpdateAsync(file.Id, new FilePatch { Name = "b.exe" }))).Message.Should().Contain("does not match");
        Error(await harness.StoreAsync(store => store.UpdateAsync(file.Id, new FilePatch { Name = "  " }))).Type.Should().Be(AppErrorType.Validation);
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeVisibilityAndExpiry_WhenTheyAreGiven()
    {
        var file = Value(await harness.UploadAsync(Png, "a.png"));
        var expiry = harness.Clock.GetUtcNow().AddDays(1);

        var changed = Value(await harness.StoreAsync(store => store.UpdateAsync(file.Id, new FilePatch { Visibility = FileVisibility.Public, ExpiresAtUtc = expiry })));

        changed.Visibility.Should().Be(FileVisibility.Public);
        changed.ExpiresAtUtc.Should().Be(expiry);
    }

    [Fact]
    public async Task DeleteAsync_ShouldHideTheFileUntilItIsRestored()
    {
        var file = Value(await harness.UploadAsync(Png, "a.png"));

        var deleted = Value(await harness.StoreAsync(store => store.DeleteAsync(file.Id)));

        deleted.DeletedAtUtc.Should().Be(harness.Clock.GetUtcNow());
        Error(await harness.StoreAsync(store => store.OpenReadAsync(file.Id))).Type.Should().Be(AppErrorType.FileNotFound);
        Value(await harness.StoreAsync(store => store.ListAsync(new FileQuery()))).Items.Should().BeEmpty();
        Value(await harness.StoreAsync(store => store.ListAsync(new FileQuery { IncludeDeleted = true }))).Items.Should().ContainSingle();

        Value(await harness.StoreAsync(store => store.RestoreAsync(file.Id))).DeletedAtUtc.Should().BeNull();
        Value(await harness.StoreAsync(store => store.OpenReadAsync(file.Id))).Content.Dispose();
    }

    [Fact]
    public async Task RestoreAsync_ShouldFailWithFileNotFound_WhenTheRetentionHasPassed()
    {
        var file = Value(await harness.UploadAsync(Png, "a.png", appId: "brief"));
        Value(await harness.StoreAsync(store => store.DeleteAsync(file.Id)));

        harness.Clock.Advance(TimeSpan.FromMinutes(6));

        Error(await harness.StoreAsync(store => store.RestoreAsync(file.Id))).Type.Should().Be(AppErrorType.FileNotFound);
    }

    [Fact]
    public async Task GetAsync_ShouldFailWithFileNotFound_WhenTheFileHasExpired()
    {
        var file = Value(await harness.UploadAsync(Png, "a.png", expiresAtUtc: harness.Clock.GetUtcNow().AddHours(1)));
        Value(await harness.StoreAsync(store => store.GetAsync(file.Id)));

        harness.Clock.Advance(TimeSpan.FromHours(2));

        Error(await harness.StoreAsync(store => store.GetAsync(file.Id))).Type.Should().Be(AppErrorType.FileNotFound);
    }

    private static async Task<byte[]> ReadAsync(FileContent content)
    {
        await using var stream = content.Content;
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        return copy.ToArray();
    }
}
