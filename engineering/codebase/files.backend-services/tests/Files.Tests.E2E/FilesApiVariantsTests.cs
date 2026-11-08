using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using WoW.Two.Sdk.Beta.Be.Files.Testing.Files;

namespace Files.Tests.E2E;

/// <summary>WebP variants on the wire: an image uploaded under an app with widths lists its variants, and each serves as
/// <c>image/webp</c> at its public immutable URL and through a signed URL that names its width.</summary>
[Collection(FilesApiCollection.Name)]
public sealed class FilesApiVariantsTests(FilesApiFixture fixture)
{
    private const string Files = "/api/files/v1/files";

    [Fact]
    public async Task Variants_ShouldServeAsWebp_AtThePublicAndTheSignedUrl()
    {
        using var client = fixture.Host.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", FilesApiFixture.AdminKey);
        var image = new ByteArrayContent(FilesSamples.PngImage(1200, 800));
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var body = new MultipartFormDataContent
        {
            { new StringContent("wide.png"), "name" },
            { new StringContent("wide"), "appId" },
            { new StringContent("public"), "visibility" },
            { image, "file", "wide.png" },
        };

        using var created = await client.PostAsync(Files, body);
        var file = await FilesApiRequests.DataAsync(created);
        var id = file.GetProperty("id").GetString();
        using var publicUrl = await client.GetAsync($"{Files}/{id}/public-url?width=900");
        var publicPath = new Uri((await FilesApiRequests.DataAsync(publicUrl)).GetString()!).PathAndQuery;
        using var signed = await client.PostAsJsonAsync($"{Files}/{id}/urls", new { width = 480 });
        var signedPath = new Uri((await FilesApiRequests.DataAsync(signed)).GetProperty("url").GetString()!).PathAndQuery;
        using var publicVariant = await client.GetAsync(publicPath);
        using var signedVariant = await client.GetAsync(signedPath);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        file.GetProperty("variants").EnumerateArray().Select(variant => variant.GetProperty("width").GetInt32()).Should().Equal(480, 900);
        publicVariant.StatusCode.Should().Be(HttpStatusCode.OK);
        publicVariant.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
        FilesSamples.IsWebp(await publicVariant.Content.ReadAsByteArrayAsync()).Should().BeTrue();
        signedVariant.StatusCode.Should().Be(HttpStatusCode.OK);
        signedVariant.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
    }
}
