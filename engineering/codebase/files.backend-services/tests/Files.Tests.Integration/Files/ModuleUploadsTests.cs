using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using static Files.Tests.Integration.Files.ModuleHarness;

namespace Files.Tests.Integration.Files;

/// <summary>Uploads through the module: bytes checked, stored once per tenant, retried safely, under the app policy.</summary>
public sealed class ModuleUploadsTests : IAsyncLifetime
{
    private ModuleHarness harness = null!;

    public async Task InitializeAsync() => harness = await CreateAsync(options =>
    {
        options.Policies["tiny"] = new() { MaxBytes = 10 };
        options.Policies["gallery"] = new();
        options.Policies["gallery"].AllowedKinds.Add(FileKind.Image);
        options.Policies["tools"] = new() { AllowExecutables = true };
    });

    public async Task DisposeAsync() => await harness.DisposeAsync();

    [Fact]
    public async Task UploadAsync_ShouldStoreTheBytesOnce_WhenTwoFilesShareThem()
    {
        var first = Value(await harness.UploadAsync(Png, "a.png"));
        var second = Value(await harness.UploadAsync(Png, "b.png"));

        second.Id.Should().NotBe(first.Id);
        second.Sha256.Should().Be(first.Sha256).And.Be(Sha256(Png));
        harness.BlobFiles().Should().ContainSingle();
        (await CountAsync<FileBlobEntity>()).Should().Be(1);
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnTheFirstFile_WhenRetriedWithTheSameKey()
    {
        var first = Value(await harness.UploadAsync(Png, "a.png", idempotencyKey: "upload-1"));
        var retry = Value(await harness.UploadAsync(Png, "renamed.png", idempotencyKey: "upload-1"));

        retry.Should().Be(first);
        (await CountAsync<FileEntity>()).Should().Be(1);
    }

    [Fact]
    public async Task UploadAsync_ShouldRecordTheTypeAndTheCaller_WhenTheFileIsAccepted()
    {
        await using var scope = harness.As("t1", appId: "gallery", subjectId: "ann", organizationId: "acme");
        using var content = new MemoryStream(Jpeg);
        var file = Value(await scope.ServiceProvider.GetRequiredService<WoW.Two.Sdk.Beta.Be.Files.Uploads.IFileUploadsBroker>()
            .UploadAsync(content, new() { Name = "photo", Visibility = FileVisibility.Public }));

        file.Should().Match<FileModel>(model =>
            model.Name == "photo.jpg" && model.ContentType == "image/jpeg" && model.Kind == FileKind.Image
            && model.Size == Jpeg.Length && model.State == FileState.Ready && model.Visibility == FileVisibility.Public
            && model.TenantId == "t1" && model.AppId == "gallery" && model.OwnerId == "ann" && model.OrganizationId == "acme"
            && model.CreatedAtUtc == harness.Clock.GetUtcNow());
    }

    [Fact]
    public async Task UploadAsync_ShouldFailWithContentMismatch_WhenTheBytesContradictTheName()
    {
        Error(await harness.UploadAsync(Jpeg, "photo.png")).Message.Should().Contain("does not match");
        harness.BlobFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task UploadAsync_ShouldFailWithFileTooLarge_WhenThePolicyLimitIsPassed()
    {
        Error(await harness.UploadAsync(Png, "a.png", appId: "tiny")).Message.Should().Contain("at most 10 bytes");
    }

    [Fact]
    public async Task UploadAsync_ShouldFailWithTypeNotAllowed_WhenThePolicyRefusesTheKind()
    {
        Error(await harness.UploadAsync(Pdf, "report.pdf", appId: "gallery")).Message.Should().Contain("application/pdf");
        Value(await harness.UploadAsync(Png, "a.png", appId: "gallery")).Kind.Should().Be(FileKind.Image);
    }

    [Fact]
    public async Task UploadAsync_ShouldRefuseExecutables_UnlessThePolicyAllowsThem()
    {
        Error(await harness.UploadAsync(Exe, "setup.exe")).Type.Should().Be(AppErrorType.Validation);
        Value(await harness.UploadAsync(Exe, "setup.exe", appId: "tools")).Kind.Should().Be(FileKind.Executable);
    }

    [Fact]
    public async Task UploadAsync_ShouldKeepTenantsApart_WhenBothUploadTheSameBytes()
    {
        var mine = Value(await harness.UploadAsync(Png, "a.png", tenantId: "t1"));
        Value(await harness.UploadAsync(Png, "a.png", tenantId: "t2"));

        harness.BlobFiles().Select(path => Path.GetRelativePath(harness.BlobRoot, path).Split(Path.DirectorySeparatorChar)[0])
            .Should().BeEquivalentTo("t1", "t2");
        Error(await harness.StoreAsync(store => store.GetAsync(mine.Id), tenantId: "t2")).Type.Should().Be(AppErrorType.FileNotFound);
    }

    [Fact]
    public async Task UploadAsync_ShouldFailWithTenantInvalid_WhenTheTenantIsNotAKeySegment()
    {
        Error(await harness.UploadAsync(Png, "a.png", tenantId: "../escape")).Type.Should().Be(AppErrorType.Forbidden);
    }

    [Fact]
    public async Task UploadAsync_ShouldLeaveNoSpooledFile_WhenItFinishes()
    {
        Value(await harness.UploadAsync(Png, "a.png"));
        Error(await harness.UploadAsync(Jpeg, "b.png"));

        Directory.GetFiles(harness.TempRoot).Should().BeEmpty();
    }

    [Fact]
    public async Task UploadAsync_ShouldRecordTheRefusal_WhenACheckRefusesTheFile()
    {
        Error(await harness.UploadAsync(Jpeg, "photo.png", appId: "gallery"));

        var trail = Value(await harness.WithAsync<WoW.Two.Sdk.Beta.Be.Files.Audit.IFileAuditBroker, WoW.Two.Sdk.Beta.Be.Foundation.Results.Result<WoW.Two.Sdk.Beta.Be.Files.Audit.FileAuditPage>>(
            audit => audit.ListAsync(new WoW.Two.Sdk.Beta.Be.Files.Audit.FileAuditQuery())));

        trail.Items.Should().ContainSingle().Which.Should().Match<WoW.Two.Sdk.Beta.Be.Files.Audit.FileAuditEntryModel>(entry =>
            entry.Action == WoW.Two.Sdk.Beta.Be.Files.Audit.FileAuditAction.Upload
            && entry.Outcome == WoW.Two.Sdk.Beta.Be.Files.Audit.FileAuditOutcome.Refused
            && entry.ErrorCode == "Validation" && entry.FileId == null && entry.AppId == "gallery" && entry.SubjectId == "user-1");
    }

    [Fact]
    public async Task UploadAsync_ShouldFailWithAppNotAllowed_WhenTheCallerNamesAnotherApp()
    {
        await using var scope = harness.As("t1", appId: "gallery");
        using var content = new MemoryStream(Png);
        var result = await scope.ServiceProvider.GetRequiredService<WoW.Two.Sdk.Beta.Be.Files.Uploads.IFileUploadsBroker>()
            .UploadAsync(content, new() { Name = "a.png", AppId = "tools" });

        Error(result).Type.Should().Be(AppErrorType.Forbidden);
    }

    private Task<int> CountAsync<TEntity>() where TEntity : class =>
        harness.WithAsync<FilesDbContext, int>(db => db.Set<TEntity>().CountAsync());
}
