using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WoW.Two.Sdk.Beta.Be.Files.Testing.Files;

namespace Files.Tests.E2E;

/// <summary>The Files API on the wire: who may call it (API keys, trusted issuers' tokens, scopes), the streamed
/// multipart upload, ranged content and the audit trail.</summary>
[Collection(FilesApiCollection.Name)]
public sealed class FilesApiEndpointTests(FilesApiFixture fixture)
{
    private const string Files = "/api/files/v1/files";

    [Fact]
    public async Task List_ShouldAnswer401_WithoutCredentials()
    {
        using var client = fixture.Host.CreateClient();

        using var response = await client.GetAsync(Files);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_ShouldAnswer401_WhenTheKeyIsUnknown()
    {
        using var client = Client("not-a-known-key-0123456789");

        using var response = await client.GetAsync(Files);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Upload_ShouldAnswer403_WhenTheKeyCannotWrite()
    {
        using var client = Client(FilesApiFixture.ReadKey);

        using var response = await client.PostAsync(Files, Multipart(FilesSamples.Png, "a.png"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Upload_ShouldStoreTheFileAndServeItsBytes_WithAKey()
    {
        using var client = Client(FilesApiFixture.AdminKey);

        using var created = await client.PostAsync(Files, Multipart(FilesSamples.Pdf, "report.pdf"));
        var file = await DataAsync(created);
        var id = file.GetProperty("id").GetString();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{Files}/{id}/content");
        request.Headers.Range = new RangeHeaderValue(0, 3);
        using var range = await client.SendAsync(request);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        file.GetProperty("tenantId").GetString().Should().Be("e2e");
        file.GetProperty("kind").GetString().Should().Be("pdf");
        file.GetProperty("visibility").GetString().Should().Be("private");
        range.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        (await range.Content.ReadAsByteArrayAsync()).Should().Equal(FilesSamples.Pdf[..4]);
    }

    [Fact]
    public async Task Upload_ShouldAnswer400_WhenTheBodyHasNoFilePart()
    {
        using var client = Client(FilesApiFixture.AdminKey);
        using var form = new MultipartFormDataContent { { new StringContent("a.png"), "name" } };

        using var response = await client.PostAsync(Files, form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_ShouldAnswer400_WhenTheKeysAppPolicyIsPassed()
    {
        using var client = Client(FilesApiFixture.TinyKey);

        using var response = await client.PostAsync(Files, Multipart(FilesSamples.Png, "a.png"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("at most 8 bytes");
    }

    [Fact]
    public async Task Token_ShouldActInTheIssuersTenant_WhenTheIssuerIsTrusted()
    {
        using var client = fixture.Host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(FilesApiFixture.Issuer, FilesApiFixture.IssuerKey));

        using var created = await client.PostAsync(Files, Multipart(FilesSamples.Png, "a.png"));
        var file = await DataAsync(created);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        file.GetProperty("tenantId").GetString().Should().Be("tokens");
        file.GetProperty("appId").GetString().Should().Be("orders");
        file.GetProperty("ownerId").GetString().Should().Be("orders-service");
    }

    [Theory]
    [InlineData("https://elsewhere.test", FilesApiFixture.IssuerKey)]
    [InlineData(FilesApiFixture.Issuer, "a-different-signing-key-0123456789abcdef")]
    public async Task Token_ShouldAnswer401_WhenTheIssuerOrSignatureIsNotTrusted(string issuer, string key)
    {
        using var client = fixture.Host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(issuer, key));

        using var response = await client.GetAsync(Files);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Audit_ShouldListTheCalls_ForAnAdminOnly()
    {
        using var admin = Client(FilesApiFixture.AdminKey);
        using var reader = Client(FilesApiFixture.ReadKey);
        using var created = await admin.PostAsync(Files, Multipart(FilesSamples.Png, "a.png"));
        var id = (await DataAsync(created)).GetProperty("id").GetString();
        using (var content = await admin.GetAsync($"{Files}/{id}/content"))
            content.StatusCode.Should().Be(HttpStatusCode.OK);

        using var trail = await admin.GetAsync($"/api/files/v1/audit?fileId={id}");
        using var refused = await reader.GetAsync("/api/files/v1/audit");

        var actions = (await DataAsync(trail)).GetProperty("items").EnumerateArray().Select(entry => entry.GetProperty("action").GetString());
        actions.Should().Equal("download", "upload");
        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private HttpClient Client(string apiKey)
    {
        var client = fixture.Host.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return client;
    }

    private static MultipartFormDataContent Multipart(byte[] bytes, string name)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent { { new StringContent(name), "name" }, { file, "file", name } };
    }

    /// <summary>Mints a client-credentials token; the bearer handler checks lifetimes on the real clock.</summary>
    private static string Token(string issuer, string key)
    {
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = FilesApiFixture.Audience,
            Subject = new ClaimsIdentity([new Claim("sub", "orders-service"), new Claim("azp", "orders"), new Claim("scope", "files.read files.write")]),
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
        });
    }

    private static async Task<JsonElement> DataAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("data").Clone();
    }
}
