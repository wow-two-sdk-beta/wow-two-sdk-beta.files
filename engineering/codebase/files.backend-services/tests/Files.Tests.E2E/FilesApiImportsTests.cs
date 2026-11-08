using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using WoW.Two.Sdk.Beta.Be.Files.Testing.Files;
using static Files.Tests.E2E.FilesApiRequests;

namespace Files.Tests.E2E;

/// <summary>Imports on the wire: the operator key opens one and mints its tokens, the import token pushes files into its
/// own import only, and the id map lists what moved (<c>product/features/imports.md</c>).</summary>
[Collection(FilesApiCollection.Name)]
public sealed class FilesApiImportsTests(FilesApiFixture fixture)
{
    private const string Imports = "/api/files/v1/imports";
    private const string Tenant = "imports";
    private const string Source = "brand-media";

    [Fact]
    public async Task Open_ShouldAnswer201WithAToken_WithTheOperatorKey()
    {
        using var client = KeyClient(FilesApiFixture.OperatorKey);

        using var response = await client.PostAsJsonAsync(Imports, new { tenant = Tenant, sources = new[] { Source, "catalogue-media" } });
        var ticket = await DataAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ticket.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        ticket.GetProperty("token").GetString().Should().StartWith("fimp_").And.Contain(".");
        ticket.GetProperty("expiresAt").GetDateTimeOffset().Should().BeCloseTo(fixture.Host.Clock.GetUtcNow().AddHours(6), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Open_ShouldAnswer401_WithoutAKey()
    {
        using var client = fixture.Host.CreateClient();

        using var response = await client.PostAsJsonAsync(Imports, new { tenant = Tenant, sources = new[] { Source } });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(FilesApiFixture.AdminKey)]
    [InlineData(FilesApiFixture.TenantsScopedKey)]
    public async Task Open_ShouldAnswer403_WithATenantKey(string key)
    {
        using var client = KeyClient(key);

        using var response = await client.PostAsJsonAsync(Imports, new { tenant = Tenant, sources = new[] { Source } });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Open_ShouldAnswer403_WithAServiceTokenHoldingTheTenantsScope()
    {
        using var client = fixture.Host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", fixture.Issuer.IssueServiceToken("wheelhouse", FilesApiFixture.Audience, "files.tenants"));

        using var response = await client.PostAsJsonAsync(Imports, new { tenant = Tenant, sources = new[] { Source } });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Open_ShouldAnswer400_WhenTheSourcesRepeat()
    {
        using var client = KeyClient(FilesApiFixture.OperatorKey);

        using var response = await client.PostAsJsonAsync(Imports, new { tenant = Tenant, sources = new[] { Source, Source } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OperatorKey_ShouldAnswer403_OnAFileRoute()
    {
        using var client = KeyClient(FilesApiFixture.OperatorKey);

        using var response = await client.GetAsync("/api/files/v1/files");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Push_ShouldStoreThenRepeatThenConflict_PerSourceKey()
    {
        var (id, token) = await OpenAsync();
        using var client = TokenClient(token);

        using var created = await client.PostAsync($"{Imports}/{id}/files", Push("logo", FilesSamples.Png));
        var file = await DataAsync(created);
        using var again = await client.PostAsync($"{Imports}/{id}/files", Push("logo", FilesSamples.Png));
        var repeat = await DataAsync(again);
        using var conflict = await client.PostAsync($"{Imports}/{id}/files", Push("logo", FilesSamples.Pdf));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        file.GetProperty("created").GetBoolean().Should().BeTrue();
        file.GetProperty("sha256").GetString().Should().Be(FilesSamples.Sha256(FilesSamples.Png));
        file.GetProperty("size").GetInt64().Should().Be(FilesSamples.Png.Length);
        again.StatusCode.Should().Be(HttpStatusCode.OK);
        repeat.GetProperty("created").GetBoolean().Should().BeFalse();
        repeat.GetProperty("fileId").GetString().Should().Be(file.GetProperty("fileId").GetString());
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Push_ShouldAnswer422AndCountItAsFailed_WhenTheBytesContradictTheSha256()
    {
        var (id, token) = await OpenAsync();
        using var client = TokenClient(token);

        using var response = await client.PostAsync($"{Imports}/{id}/files", Push("broken", FilesSamples.Png, sha256: FilesSamples.Sha256(FilesSamples.Pdf)));
        using var read = await client.GetAsync($"{Imports}/{id}");
        var import = await DataAsync(read);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        import.GetProperty("tenant").GetString().Should().Be(Tenant);
        import.GetProperty("state").GetString().Should().Be("open");
        var source = import.GetProperty("sources").EnumerateArray().Single(row => row.GetProperty("id").GetString() == Source);
        source.GetProperty("failed").GetInt64().Should().Be(1);
        source.GetProperty("files").GetInt64().Should().Be(0);
    }

    [Fact]
    public async Task Map_ShouldListTheMovedKeyWithItsFile_AndTheReferenceShouldBeAttached()
    {
        var (id, token) = await OpenAsync();
        using var client = TokenClient(token);
        var sha = FilesSamples.Sha256(FilesSamples.Jpeg);

        using var pushed = await client.PostAsync($"{Imports}/{id}/files", Push(sha, FilesSamples.Jpeg, reference: $"brand.media:{sha}"));
        var fileId = (await DataAsync(pushed)).GetProperty("fileId").GetString();
        using var mapped = await client.GetAsync($"{Imports}/{id}/map");
        var map = await DataAsync(mapped);
        using var reader = KeyClient(FilesApiFixture.ImportsReadKey);
        using var stored = await reader.GetAsync($"/api/files/v1/files/{fileId}");
        var file = await DataAsync(stored);
        using var attached = await reader.GetAsync($"/api/files/v1/files/{fileId}/references");
        var references = await DataAsync(attached);
        using var operatorClient = KeyClient(FilesApiFixture.OperatorKey);
        using var operatorRead = await operatorClient.GetAsync($"{Imports}/{id}");
        var import = await DataAsync(operatorRead);

        pushed.StatusCode.Should().Be(HttpStatusCode.Created);
        mapped.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = map.GetProperty("items").EnumerateArray().Should().ContainSingle().Subject;
        item.GetProperty("source").GetString().Should().Be(Source);
        item.GetProperty("key").GetString().Should().Be(sha);
        item.GetProperty("reference").GetString().Should().Be($"brand.media:{sha}");
        item.GetProperty("fileId").GetString().Should().Be(fileId);
        item.GetProperty("sha256").GetString().Should().Be(sha);
        item.GetProperty("size").GetInt64().Should().Be(FilesSamples.Jpeg.Length);
        (!map.TryGetProperty("next", out var next) || next.ValueKind == JsonValueKind.Null).Should().BeTrue("one item fits one page");
        file.GetProperty("tenantId").GetString().Should().Be(Tenant);
        file.GetProperty("visibility").GetString().Should().Be("private");
        file.GetProperty("ownerId").GetString().Should().Be($"import:{id}");
        file.GetProperty("appId").GetString().Should().Be("brand");
        file.GetProperty("name").GetString().Should().Be($"{sha}.jpg");
        references.EnumerateArray().Should().ContainSingle(reference =>
            reference.GetProperty("recordType").GetString() == "brand.media" && reference.GetProperty("recordId").GetString() == sha);
        operatorRead.StatusCode.Should().Be(HttpStatusCode.OK);
        import.GetProperty("sources").EnumerateArray().Single(row => row.GetProperty("id").GetString() == Source)
            .GetProperty("bytes").GetInt64().Should().Be(FilesSamples.Jpeg.Length);
    }

    [Fact]
    public async Task Token_ShouldAnswer401_OnAnotherImport()
    {
        var (id, _) = await OpenAsync();
        var (_, otherToken) = await OpenAsync();
        using var client = TokenClient(otherToken);

        using var push = await client.PostAsync($"{Imports}/{id}/files", Push("logo", FilesSamples.Png));
        using var read = await client.GetAsync($"{Imports}/{id}");
        using var map = await client.GetAsync($"{Imports}/{id}/map");

        push.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        read.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        map.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_ShouldAnswer401_WhenTamperedOrSentAsABearerToken()
    {
        var (id, token) = await OpenAsync();
        using var tampered = TokenClient(token[..^2] + (token[^2..] == "AA" ? "BB" : "AA"));
        using var bearer = fixture.Host.CreateClient();
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var forged = await tampered.GetAsync($"{Imports}/{id}");
        using var asBearer = await bearer.GetAsync($"{Imports}/{id}");

        forged.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        asBearer.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_ShouldReachNoFileRouteAndNoOperatorRoute()
    {
        var (id, token) = await OpenAsync();
        using var client = TokenClient(token);

        using var files = await client.GetAsync("/api/files/v1/files");
        using var mint = await client.PostAsync($"{Imports}/{id}/tokens", content: null);

        files.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        mint.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Push_ShouldAnswer403WithTheOperatorKey_And401WithNothing()
    {
        var (id, _) = await OpenAsync();
        using var operatorClient = KeyClient(FilesApiFixture.OperatorKey);
        using var anonymous = fixture.Host.CreateClient();

        using var asOperator = await operatorClient.PostAsync($"{Imports}/{id}/files", Push("logo", FilesSamples.Png));
        using var asNobody = await anonymous.PostAsync($"{Imports}/{id}/files", Push("logo", FilesSamples.Png));

        asOperator.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        asNobody.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tokens_ShouldMintAWorkingToken_And404ForAnUnknownImport()
    {
        var (id, _) = await OpenAsync();
        using var client = KeyClient(FilesApiFixture.OperatorKey);

        using var minted = await client.PostAsync($"{Imports}/{id}/tokens", content: null);
        var token = (await DataAsync(minted)).GetProperty("token").GetString()!;
        using var unknown = await client.PostAsync($"{Imports}/{Guid.CreateVersion7()}/tokens", content: null);
        using var tokenClient = TokenClient(token);
        using var read = await tokenClient.GetAsync($"{Imports}/{id}");

        minted.StatusCode.Should().Be(HttpStatusCode.OK);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
        read.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Opens an import of the <c>imports</c> tenant with the operator key.</summary>
    private async Task<(string Id, string Token)> OpenAsync()
    {
        using var client = KeyClient(FilesApiFixture.OperatorKey);
        using var response = await client.PostAsJsonAsync(Imports, new { tenant = Tenant, sources = new[] { Source } });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ticket = await DataAsync(response);
        return (ticket.GetProperty("id").GetString()!, ticket.GetProperty("token").GetString()!);
    }

    /// <summary>Builds a push: the fields, then the file part.</summary>
    private static MultipartFormDataContent Push(string key, byte[] bytes, string? sha256 = null, string? reference = null)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(Source), "source" },
            { new StringContent(key), "key" },
            { new StringContent(sha256 ?? FilesSamples.Sha256(bytes)), "sha256" },
        };
        if (reference is not null)
            form.Add(new StringContent(reference), "reference");

        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", "file");
        return form;
    }

    private HttpClient KeyClient(string key)
    {
        var client = fixture.Host.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", key);
        return client;
    }

    private HttpClient TokenClient(string token)
    {
        var client = fixture.Host.CreateClient();
        client.DefaultRequestHeaders.Add("X-Import-Token", token);
        return client;
    }
}
