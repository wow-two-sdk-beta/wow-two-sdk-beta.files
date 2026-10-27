using AwesomeAssertions;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using Xunit;

namespace WoW.Two.Sdk.Beta.Be.Files.Testing.Files;

/// <summary>The conformance suite of the Files brokers: one set of rules every implementation passes.</summary>
/// <remarks>Derive a test class per implementation — the module, the client against a host — and supply two callers in
/// different tenants. Each test instance should get tenants of its own, so tests never see each other's files.</remarks>
public abstract class FilesBrokerConformance
{
    /// <summary>Gets the brokers as the tenant under test reaches them.</summary>
    protected abstract FilesBrokerSet Brokers { get; }

    /// <summary>Gets the brokers as another tenant reaches them.</summary>
    protected abstract FilesBrokerSet OtherTenant { get; }

    /// <summary>An upload is stored as the type its bytes prove.</summary>
    [Fact]
    public async Task UploadAsync_ShouldStoreTheRecognizedType_WhenTheBytesMatchTheName()
    {
        var file = Value(await UploadAsync(FilesSamples.Png, "photo.png"));

        file.Should().Match<FileModel>(model =>
            model.Name == "photo.png" && model.ContentType == "image/png" && model.Kind == FileKind.Image
            && model.Size == FilesSamples.Png.Length && model.Sha256 == FilesSamples.Sha256(FilesSamples.Png)
            && model.State == FileState.Ready && model.Visibility == FileVisibility.Private && model.DeletedAtUtc == null);
    }

    /// <summary>Bytes that contradict the name are refused.</summary>
    [Fact]
    public async Task UploadAsync_ShouldFailValidation_WhenAJpegIsNamedPng()
    {
        Error(await UploadAsync(FilesSamples.Jpeg, "photo.png")).Type.Should().Be(AppErrorType.Validation);
    }

    /// <summary>A retry with the same idempotency key returns the first upload's file.</summary>
    [Fact]
    public async Task UploadAsync_ShouldReturnTheFirstFile_WhenRetriedWithTheSameKey()
    {
        var key = Guid.NewGuid().ToString("N");

        var first = Value(await UploadAsync(FilesSamples.Pdf, "a.pdf", key));
        var retry = Value(await UploadAsync(FilesSamples.Pdf, "a.pdf", key));

        retry.Id.Should().Be(first.Id);
    }

    /// <summary>A file's bytes read back whole, or as a range.</summary>
    [Fact]
    public async Task OpenReadAsync_ShouldReturnTheBytes_WholeOrRanged()
    {
        var file = Value(await UploadAsync(FilesSamples.Pdf, "doc.pdf"));

        var whole = Value(await Brokers.Store.OpenReadAsync(file.Id));
        var range = Value(await Brokers.Store.OpenReadAsync(file.Id, new ByteRange(1, 3)));

        (await ReadAsync(whole)).Should().Equal(FilesSamples.Pdf);
        whole.ContentType.Should().Be("application/pdf");
        (await ReadAsync(range)).Should().Equal(FilesSamples.Pdf[1..4]);
        range.TotalLength.Should().Be(FilesSamples.Pdf.Length);
    }

    /// <summary>An unknown id is not found.</summary>
    [Fact]
    public async Task GetAsync_ShouldFailNotFound_WhenTheIdIsUnknown()
    {
        Error(await Brokers.Store.GetAsync(FileId.New())).Type.Should().Be(AppErrorType.FileNotFound);
    }

    /// <summary>Lists come newest first and page through a cursor.</summary>
    [Fact]
    public async Task ListAsync_ShouldPageNewestFirst()
    {
        var older = Value(await UploadAsync(FilesSamples.Png, "older.png"));
        var newer = Value(await UploadAsync(FilesSamples.Png, "newer.png"));

        var first = Value(await Brokers.Store.ListAsync(new FileQuery { Limit = 1 }));
        var second = Value(await Brokers.Store.ListAsync(new FileQuery { Limit = 1, Cursor = first.NextCursor }));

        first.Items.Select(file => file.Id).Should().Equal(newer.Id);
        second.Items.Select(file => file.Id).Should().Equal(older.Id);
        second.NextCursor.Should().BeNull();
    }

    /// <summary>A rename and a visibility change show on the next read.</summary>
    [Fact]
    public async Task UpdateAsync_ShouldChangeTheNameAndVisibility()
    {
        var file = Value(await UploadAsync(FilesSamples.Png, "a.png"));

        Value(await Brokers.Store.UpdateAsync(file.Id, new FilePatch { Name = "b.png", Visibility = FileVisibility.Public }));

        Value(await Brokers.Store.GetAsync(file.Id)).Should().Match<FileModel>(model =>
            model.Name == "b.png" && model.Visibility == FileVisibility.Public);
    }

    /// <summary>A deleted file is hidden until it is restored.</summary>
    [Fact]
    public async Task DeleteAsync_ShouldHideTheFile_UntilRestored()
    {
        var file = Value(await UploadAsync(FilesSamples.Png, "a.png"));

        Value(await Brokers.Store.DeleteAsync(file.Id)).DeletedAtUtc.Should().NotBeNull();
        Error(await Brokers.Store.OpenReadAsync(file.Id)).Type.Should().Be(AppErrorType.FileNotFound);
        Value(await Brokers.Store.ListAsync(new FileQuery())).Items.Should().NotContain(listed => listed.Id == file.Id);

        Value(await Brokers.Store.RestoreAsync(file.Id)).DeletedAtUtc.Should().BeNull();
        Value(await Brokers.Store.ListAsync(new FileQuery())).Items.Should().Contain(listed => listed.Id == file.Id);
    }

    /// <summary>Another tenant neither reads nor lists the file.</summary>
    [Fact]
    public async Task OtherTenant_ShouldSeeNothing_OfTheTenantsFiles()
    {
        var file = Value(await UploadAsync(FilesSamples.Png, "a.png"));

        Error(await OtherTenant.Store.GetAsync(file.Id)).Type.Should().Be(AppErrorType.FileNotFound);
        Error(await OtherTenant.Store.DeleteAsync(file.Id)).Type.Should().Be(AppErrorType.FileNotFound);
        Value(await OtherTenant.Store.ListAsync(new FileQuery())).Items.Should().NotContain(listed => listed.Id == file.Id);
    }

    /// <summary>A record attaches once however often it asks, and detaches once.</summary>
    [Fact]
    public async Task References_ShouldAttachListAndDetach_OncePerRecord()
    {
        var file = Value(await UploadAsync(FilesSamples.Png, "a.png"));
        var product = new FileReference("catalogue.product", "p-1");
        var post = new FileReference("blog.post", "42");

        Done(await Brokers.References.AttachAsync(file.Id, product));
        Done(await Brokers.References.AttachAsync(file.Id, product));
        Done(await Brokers.References.AttachAsync(file.Id, post));

        Value(await Brokers.References.ListAsync(file.Id)).Should().Equal(product, post);
        Done(await Brokers.References.DetachAsync(file.Id, product));
        Error(await Brokers.References.DetachAsync(file.Id, product)).Type.Should().Be(AppErrorType.NotFound);
        Value(await Brokers.References.ListAsync(file.Id)).Should().Equal(post);
    }

    /// <summary>A blank record type is refused.</summary>
    [Fact]
    public async Task References_ShouldFailValidation_WhenTheRecordTypeIsBlank()
    {
        var file = Value(await UploadAsync(FilesSamples.Png, "a.png"));

        Error(await Brokers.References.AttachAsync(file.Id, new FileReference(" ", "1"))).Type.Should().Be(AppErrorType.Validation);
    }

    /// <summary>Another tenant cannot attach to the tenant's file.</summary>
    [Fact]
    public async Task OtherTenant_ShouldNotAttach_ToTheTenantsFile()
    {
        var file = Value(await UploadAsync(FilesSamples.Png, "a.png"));

        Error(await OtherTenant.References.AttachAsync(file.Id, new FileReference("blog.post", "1"))).Type.Should().Be(AppErrorType.FileNotFound);
    }

    private async Task<Result<FileModel>> UploadAsync(byte[] bytes, string name, string? idempotencyKey = null)
    {
        using var content = new MemoryStream(bytes);
        return await Brokers.Uploads.UploadAsync(content, new UploadSpec { Name = name, IdempotencyKey = idempotencyKey });
    }

    private static async Task<byte[]> ReadAsync(FileContent content)
    {
        await using var stream = content.Content;
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        return copy.ToArray();
    }

    /// <summary>Returns the success value, failing the test with the broker's own error otherwise.</summary>
    private static T Value<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Success success
            ? success.Value
            : throw new Xunit.Sdk.XunitException(((Result<T>.Failure)result).Error.Message);

    /// <summary>Returns the failure's error, failing the test when the call succeeded.</summary>
    private static AppError Error<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Failure failure ? failure.Error : throw new Xunit.Sdk.XunitException("The call succeeded.");

    /// <summary>Returns the failure's error, failing the test when the call succeeded.</summary>
    private static AppError Error(Result result) =>
        result is Result.Failure failure ? failure.Error : throw new Xunit.Sdk.XunitException("The call succeeded.");

    /// <summary>Fails the test with the broker's own error unless the call succeeded.</summary>
    private static void Done(Result result)
    {
        if (result is Result.Failure failure)
            throw new Xunit.Sdk.XunitException(failure.Error.Message);
    }
}
