using System.Net;
using AwesomeAssertions;

namespace Files.Tests.E2E;

/// <summary>The icon files on the wire: served from the web root for other origins to embed, with the generic icon for
/// any name the bundle has no file for and a 404, never the app shell, for anything else under <c>/icons</c>.</summary>
[Collection(FilesApiCollection.Name)]
public sealed class IconFileTests(FilesApiFixture fixture)
{
    [Fact]
    public async Task Get_ShouldServeTheExtensionsIcon_ForOtherOriginsToEmbed()
    {
        using var client = fixture.Host.CreateClient();

        using var response = await client.GetAsync("/icons/pdf.svg");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/svg+xml");
        (await response.Content.ReadAsStringAsync()).Should().StartWith("<svg").And.Contain(">PDF</text>");
        Header(response, "Cross-Origin-Resource-Policy").Should().Be("cross-origin");
    }

    [Fact]
    public async Task Get_ShouldServeTheKindsIcon_WithoutALabel()
    {
        using var client = fixture.Host.CreateClient();

        using var response = await client.GetAsync("/icons/kinds/video.svg");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().StartWith("<svg").And.NotContain("<text");
    }

    [Theory]
    [InlineData("/icons/xyz123.svg")]
    [InlineData("/icons/kinds/hologram.svg")]
    public async Task Get_ShouldServeTheGenericIcon_WhenTheBundleHasNoFileForTheName(string path)
    {
        using var client = fixture.Host.CreateClient();
        var generic = await client.GetStringAsync("/icons/kinds/generic.svg");

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/svg+xml");
        (await response.Content.ReadAsStringAsync()).Should().Be(generic);
        Header(response, "Cross-Origin-Resource-Policy").Should().Be("cross-origin");
    }

    [Theory]
    [InlineData("/icons/readme.txt")]
    [InlineData("/icons/a.b.svg")]
    public async Task Get_ShouldReturn404_WhenTheNameIsNotAnIconName(string path)
    {
        using var client = fixture.Host.CreateClient();

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().NotBe("text/html");
    }

    [Fact]
    public async Task Get_ShouldKeepTheSameOriginResourcePolicy_OutsideTheIconFolder()
    {
        using var client = fixture.Host.CreateClient();

        using var response = await client.GetAsync("/");

        Header(response, "Cross-Origin-Resource-Policy").Should().Be("same-origin");
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? values.Single() : null;
}
