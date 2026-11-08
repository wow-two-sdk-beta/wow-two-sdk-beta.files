using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Imports;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using static Files.Tests.Integration.Imports.ImportsHarness;

namespace Files.Tests.Integration.Imports;

/// <summary>Imports through the module: a push uploads as the import, in its tenant, once per (import, source, key);
/// the bytes are checked against the mover's SHA-256; the id map pages in push order.</summary>
public sealed class FilesImportsTests : IAsyncLifetime
{
    private ImportsHarness harness = null!;

    public async Task InitializeAsync() => harness = await CreateAsync();

    public async Task DisposeAsync() => await harness.DisposeAsync();

    [Fact]
    public async Task PushAsync_ShouldUploadInTheImportsTenantAsTheImport_WithTheImportsIdempotencyKey()
    {
        var import = await harness.OpenAsync();
        var sha = Sha256(Png);

        var pushed = Value(await harness.PushAsync(import.Id, "logos/logo", Png, reference: $"brand.media:{sha}"));

        pushed.Created.Should().BeTrue();
        var file = (await harness.RowsAsync($"SELECT tenant_id, owner_id, app_id, name, visibility, idempotency_key, orphan_purge_at_utc FROM files_files WHERE id = '{pushed.FileId}'")).Single();
        file["tenant_id"].Should().Be("ocharo");
        file["owner_id"].Should().Be($"import:{import.Id}");
        file["app_id"].Should().Be("brand");
        file["name"].Should().Be("logo.png");
        file["visibility"].Should().Be("Private");
        file["idempotency_key"].Should().Be($"import:{import.Id}:brand-media:logos/logo");
        var audit = (await harness.RowsAsync($"SELECT tenant_id, subject_id, action FROM files_audit WHERE file_id = '{pushed.FileId}'")).Single();
        audit["tenant_id"].Should().Be("ocharo");
        audit["subject_id"].Should().Be($"import:{import.Id}");
        var reference = (await harness.RowsAsync($"SELECT tenant_id, record_type, record_id FROM files_references WHERE file_id = '{pushed.FileId}'")).Single();
        reference["record_type"].Should().Be("brand.media");
        reference["record_id"].Should().Be(sha);
    }

    [Fact]
    public async Task PushAsync_ShouldLeaveTheScopesCallerAsItWas_AfterThePush()
    {
        var import = await harness.OpenAsync();
        await using var scope = harness.Scope();
        var imports = scope.ServiceProvider.GetRequiredService<IFilesImportsService>();

        Value(await imports.PushAsync(import.Id, new MemoryStream(Png), new FileImportPushSpec { Source = "brand-media", Key = "a", Sha256 = Sha256(Png) }));

        scope.ServiceProvider.GetRequiredService<IFilesCallerService>().Current.TenantId.Should().Be("default");
        scope.ServiceProvider.GetRequiredService<FilesCallerOverride>().Current.Should().BeNull();
    }

    [Fact]
    public async Task PushAsync_ShouldAnswerTheStoredFile_WhenTheKeyIsPushedAgainWithTheSameSha256()
    {
        var import = await harness.OpenAsync();
        var first = Value(await harness.PushAsync(import.Id, "logo", Png));

        var again = Value(await harness.PushAsync(import.Id, "logo", Png));

        again.Should().Be(first with { Created = false });
        (await harness.RowsAsync("SELECT id FROM files_files")).Should().ContainSingle();
    }

    [Fact]
    public async Task PushAsync_ShouldConflict_WhenTheKeyIsPushedAgainWithAnotherSha256()
    {
        var import = await harness.OpenAsync();
        Value(await harness.PushAsync(import.Id, "logo", Png));

        var conflict = await harness.PushAsync(import.Id, "logo", Pdf);

        ErrorOf(conflict).Type.Should().Be(AppErrorType.Conflict);
    }

    [Fact]
    public async Task PushAsync_ShouldRefuseAndCountAFailure_WhenTheBytesContradictTheSha256()
    {
        var import = await harness.OpenAsync("brand-media", "catalogue-media");

        var refused = await harness.PushAsync(import.Id, "logo", Png, sha256: Sha256(Pdf));
        var state = Value(await harness.ImportsAsync(imports => imports.GetAsync(import.Id)));

        ErrorOf(refused).Type.Should().Be(AppErrorType.BusinessRule);
        state.Sources.Should().Equal(new FileImportSourceModel("brand-media", 0, 0, 1), new FileImportSourceModel("catalogue-media", 0, 0, 0));
        (await harness.RowsAsync("SELECT id FROM files_files")).Should().BeEmpty();
    }

    [Theory]
    [InlineData("unknown-source", "logo", "SHA")]
    [InlineData("brand-media", "", "SHA")]
    [InlineData("brand-media", "logo", "not-a-sha")]
    public async Task PushAsync_ShouldRefuseAValidationError_WhenAFieldIsWrong(string source, string key, string sha)
    {
        var import = await harness.OpenAsync();

        var refused = await harness.PushAsync(import.Id, key, Png, sha256: sha == "SHA" ? Sha256(Png) : sha, source: source);

        ErrorOf(refused).Type.Should().Be(AppErrorType.Validation);
    }

    [Fact]
    public async Task GetAsync_ShouldCountEachSourcesFilesAndBytes()
    {
        var import = await harness.OpenAsync("brand-media", "catalogue-media");
        Value(await harness.PushAsync(import.Id, "a", Png));
        Value(await harness.PushAsync(import.Id, "b", Pdf));
        Value(await harness.PushAsync(import.Id, "c", Png, source: "catalogue-media"));

        var state = Value(await harness.ImportsAsync(imports => imports.GetAsync(import.Id)));

        state.Should().Match<FileImportModel>(model => model.Id == import.Id && model.Tenant == "ocharo" && model.State == "open");
        state.Sources.Should().Equal(
            new FileImportSourceModel("brand-media", 2, Png.Length + Pdf.Length, 0),
            new FileImportSourceModel("catalogue-media", 1, Png.Length, 0));
    }

    [Fact]
    public async Task MapAsync_ShouldPageTheMovedKeysInPushOrder()
    {
        var import = await harness.OpenAsync();
        var other = await harness.OpenAsync();
        Value(await harness.PushAsync(import.Id, "a", Png));
        Value(await harness.PushAsync(other.Id, "elsewhere", Png));
        Value(await harness.PushAsync(import.Id, "b", Pdf));
        Value(await harness.PushAsync(import.Id, "c", Png));

        var first = Value(await harness.ImportsAsync(imports => imports.MapAsync(import.Id, cursor: null, limit: 2)));
        var second = Value(await harness.ImportsAsync(imports => imports.MapAsync(import.Id, first.Next, limit: 2)));

        first.Items.Select(item => item.Key).Should().Equal("a", "b");
        first.Next.Should().NotBeNull();
        second.Items.Select(item => item.Key).Should().Equal("c");
        second.Next.Should().BeNull();
        second.Items[0].Should().Match<FileImportItemModel>(item => item.Source == "brand-media" && item.Sha256 == Sha256(Png) && item.Size == Png.Length);
    }

    [Theory]
    [InlineData("Not A Tenant", "brand-media")]
    [InlineData("ocharo", "")]
    [InlineData("ocharo", "Brand Media")]
    public async Task OpenAsync_ShouldRefuse_WhenTheTenantOrASourceIsMalformed(string tenant, string source)
    {
        var opened = await harness.ImportsAsync(imports => imports.OpenAsync(new FileImportSpec { Tenant = tenant, Sources = source.Length > 0 ? [source] : [] }));

        ErrorOf(opened).Type.Should().Be(AppErrorType.Validation);
    }

    [Fact]
    public async Task OpenAsync_ShouldRefuse_WhenTheSourcesRepeatOrPassFifty()
    {
        var repeated = await harness.ImportsAsync(imports => imports.OpenAsync(new FileImportSpec { Tenant = "ocharo", Sources = ["a", "a"] }));
        var tooMany = await harness.ImportsAsync(imports => imports.OpenAsync(new FileImportSpec { Tenant = "ocharo", Sources = [.. Enumerable.Range(0, 51).Select(i => $"s{i}")] }));

        ErrorOf(repeated).Type.Should().Be(AppErrorType.Validation);
        ErrorOf(tooMany).Type.Should().Be(AppErrorType.Validation);
    }

    [Fact]
    public async Task Tokens_ShouldReadBackTheImportAndTenant_UntilTheyExpire()
    {
        var import = await harness.OpenAsync();

        var claims = harness.Tokens.Read(import.Token);
        harness.Clock.Advance(TimeSpan.FromHours(6));
        var expired = harness.Tokens.Read(import.Token);

        claims.Should().Be(new FileImportTokenClaims(import.Id, "ocharo", harness.Clock.GetUtcNow()));
        import.ExpiresAt.Should().Be(harness.Clock.GetUtcNow());
        expired.Should().BeNull();
    }

    [Fact]
    public async Task Tokens_ShouldReadNothing_WhenTamperedMalformedOrSignedUnderAnotherKey()
    {
        var import = await harness.OpenAsync();
        await using var elsewhere = await CreateAsync("another-signing-key-0123456789abcdef0123");
        var foreign = elsewhere.Tokens.Issue(import.Id, "ocharo").Token;
        var payload = import.Token["fimp_".Length..import.Token.IndexOf('.')];
        var forged = "fimp_" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($$"""{"imp":"{{import.Id}}","ten":"other","exp":9999999999}"""))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_') + import.Token[import.Token.IndexOf('.')..];

        harness.Tokens.Read(foreign).Should().BeNull();
        harness.Tokens.Read(forged).Should().BeNull();
        harness.Tokens.Read(import.Token[..^1]).Should().BeNull();
        harness.Tokens.Read(payload).Should().BeNull();
        harness.Tokens.Read("Bearer " + import.Token).Should().BeNull();
        harness.Tokens.Read(null).Should().BeNull();
    }

    private static AppError ErrorOf<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Failure failure ? failure.Error : throw new Xunit.Sdk.XunitException("The call succeeded.");
}
