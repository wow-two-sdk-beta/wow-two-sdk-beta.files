using AwesomeAssertions;
using WoW.Two.Sdk.Beta.Be.Files.Module.Sweeps;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using static Files.Tests.Integration.Files.ModuleHarness;

namespace Files.Tests.Integration.Files;

/// <summary>References keep files alive: orphans purge after their app's retention, expiry waits for the last
/// reference, and an app's default lifetime stamps uploads.</summary>
public sealed class ModuleReferencesTests : IAsyncLifetime
{
    private static readonly FileReference Product = new("catalogue.product", "p-1");

    private ModuleHarness harness = null!;

    public async Task InitializeAsync() => harness = await CreateAsync(options =>
    {
        options.Policies["drafts"] = new() { OrphanRetention = TimeSpan.FromHours(1) };
        options.Policies["exports"] = new() { DefaultLifetime = TimeSpan.FromDays(7) };
    });

    public async Task DisposeAsync() => await harness.DisposeAsync();

    [Fact]
    public async Task DetachAsync_ShouldStartTheOrphanRetention_WhenTheLastReferenceGoes()
    {
        var file = Value(await harness.UploadAsync(Png, "a.png", appId: "drafts"));
        Done(await ReferencesAsync(references => references.AttachAsync(file.Id, Product)));

        harness.Clock.Advance(TimeSpan.FromHours(2));
        (await SweepAsync()).Files.Should().Be(0);

        Done(await ReferencesAsync(references => references.DetachAsync(file.Id, Product)));
        harness.Clock.Advance(TimeSpan.FromMinutes(59));
        Value(await harness.StoreAsync(store => store.GetAsync(file.Id)));

        harness.Clock.Advance(TimeSpan.FromMinutes(2));
        Error(await harness.StoreAsync(store => store.GetAsync(file.Id))).Type.Should().Be(AppErrorType.FileNotFound);
        (await SweepAsync()).Files.Should().Be(1);
    }

    [Fact]
    public async Task UploadAsync_ShouldStartTheOrphanRetention_WhenNothingAttaches()
    {
        var file = Value(await harness.UploadAsync(Png, "a.png", appId: "drafts"));

        harness.Clock.Advance(TimeSpan.FromHours(2));

        Error(await harness.StoreAsync(store => store.GetAsync(file.Id))).Type.Should().Be(AppErrorType.FileNotFound);
    }

    [Fact]
    public async Task UploadAsync_ShouldKeepUnreferencedFiles_WhenTheAppHasNoOrphanRetention()
    {
        var file = Value(await harness.UploadAsync(Png, "a.png"));

        harness.Clock.Advance(TimeSpan.FromDays(400));

        Value(await harness.StoreAsync(store => store.GetAsync(file.Id))).Id.Should().Be(file.Id);
        (await SweepAsync()).Files.Should().Be(0);
    }

    [Fact]
    public async Task Expiry_ShouldWaitForTheLastReference_WhenARecordStillUsesTheFile()
    {
        var file = Value(await harness.UploadAsync(Png, "a.png", expiresAtUtc: harness.Clock.GetUtcNow().AddHours(1)));
        Done(await ReferencesAsync(references => references.AttachAsync(file.Id, Product)));

        harness.Clock.Advance(TimeSpan.FromHours(2));
        Value(await harness.StoreAsync(store => store.GetAsync(file.Id)));
        (await SweepAsync()).Files.Should().Be(0);

        Done(await ReferencesAsync(references => references.DetachAsync(file.Id, Product)));
        (await SweepAsync()).Files.Should().Be(1);
    }

    [Fact]
    public async Task UploadAsync_ShouldExpireAfterTheAppsLifetime_WhenTheUploadSetsNoExpiry()
    {
        var file = Value(await harness.UploadAsync(Pdf, "export.pdf", appId: "exports"));
        var own = Value(await harness.UploadAsync(Pdf, "kept.pdf", appId: "exports", expiresAtUtc: harness.Clock.GetUtcNow().AddDays(30)));

        file.ExpiresAtUtc.Should().Be(harness.Clock.GetUtcNow().AddDays(7));
        own.ExpiresAtUtc.Should().Be(harness.Clock.GetUtcNow().AddDays(30));
    }

    [Fact]
    public async Task AttachAsync_ShouldFailWithFileNotFound_WhenTheFileIsDeleted()
    {
        var file = Value(await harness.UploadAsync(Png, "a.png"));
        Value(await harness.StoreAsync(store => store.DeleteAsync(file.Id)));

        Failure(await ReferencesAsync(references => references.AttachAsync(file.Id, Product))).Type.Should().Be(AppErrorType.FileNotFound);
    }

    [Fact]
    public async Task SweepAsync_ShouldDropTheReferences_WhenADeletedFileIsPurged()
    {
        var file = Value(await harness.UploadAsync(Png, "a.png"));
        Done(await ReferencesAsync(references => references.AttachAsync(file.Id, Product)));
        Value(await harness.StoreAsync(store => store.DeleteAsync(file.Id)));

        harness.Clock.Advance(TimeSpan.FromDays(31));

        (await SweepAsync()).Should().Be(new FileSweepResultModel(1, 1));
        (await harness.WithAsync<WoW.Two.Sdk.Beta.Be.Files.Module.Persistence.FilesDbContext, int>(db =>
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.References))).Should().Be(0);
    }

    private Task<T> ReferencesAsync<T>(Func<IFileReferencesBroker, Task<T>> call) => harness.WithAsync(call);

    private Task<FileSweepResultModel> SweepAsync() =>
        harness.WithAsync<IFilesSweepService, FileSweepResultModel>(sweep => sweep.SweepAsync());

    private static void Done(Result result)
    {
        if (result is Result.Failure failure)
            throw new Xunit.Sdk.XunitException(failure.Error.Message);
    }

    private static AppError Failure(Result result) =>
        result is Result.Failure failure ? failure.Error : throw new Xunit.Sdk.XunitException("The call succeeded.");
}
