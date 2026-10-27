using System.Net;
using System.Net.Http.Headers;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Testing.Files;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace Files.Tests.E2E;

/// <summary>The content host on the wire: signed and public URLs, ranges, entity tags and the user-content headers.</summary>
[Collection(FilesApiCollection.Name)]
public sealed class DeliveryEndpointTests(FilesApiFixture fixture)
{
    [Fact]
    public async Task SignedUrl_ShouldServeTheFile_WithTheUserContentHeaders()
    {
        var (file, url) = await UploadAndSignAsync(FilesSamples.Pdf, "report.pdf");
        using var client = fixture.Host.CreateClient();

        using var response = await client.GetAsync(url.PathAndQuery);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(FilesSamples.Pdf);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("inline");
        response.Headers.ETag!.Tag.Should().Be($"\"{file.Sha256}\"");
        response.Headers.CacheControl!.Private.Should().BeTrue();
        Header(response, "X-Content-Type-Options").Should().Be("nosniff");
        Header(response, "Content-Security-Policy").Should().Contain("sandbox").And.Contain("default-src 'none'");
        Header(response, "Cross-Origin-Resource-Policy").Should().Be("cross-origin");
    }

    [Fact]
    public async Task SignedUrl_ShouldBeRefused_WhenItHasExpired()
    {
        var (_, url) = await UploadAndSignAsync(FilesSamples.Png, "a.png", TimeSpan.FromMinutes(1));
        using var client = fixture.Host.CreateClient();

        fixture.Host.Clock.Advance(TimeSpan.FromMinutes(2));
        using var response = await client.GetAsync(url.PathAndQuery);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SignedUrl_ShouldBeRefused_WhenAPartIsChanged()
    {
        var (_, url) = await UploadAndSignAsync(FilesSamples.Png, "a.png");
        using var client = fixture.Host.CreateClient();

        using var attachment = await client.GetAsync(url.PathAndQuery.Replace("d=i", "d=a"));
        using var otherFile = await client.GetAsync(url.PathAndQuery.Replace(url.Segments[^1], FileId.New().ToString()));

        attachment.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        otherFile.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SignedUrl_ShouldServeOneRange_WhenARangeIsAsked()
    {
        var (_, url) = await UploadAndSignAsync(FilesSamples.Pdf, "report.pdf");
        using var client = fixture.Host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url.PathAndQuery);
        request.Headers.Range = new RangeHeaderValue(2, 5);

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(FilesSamples.Pdf[2..6]);
        response.Content.Headers.ContentRange!.ToString().Should().Be($"bytes 2-5/{FilesSamples.Pdf.Length}");
    }

    [Fact]
    public async Task SignedUrl_ShouldServeTheSuffix_WhenASuffixRangeIsAsked()
    {
        var (_, url) = await UploadAndSignAsync(FilesSamples.Pdf, "report.pdf");
        using var client = fixture.Host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url.PathAndQuery);
        request.Headers.Range = new RangeHeaderValue(null, 4);

        using var response = await client.SendAsync(request);

        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(FilesSamples.Pdf[^4..]);
    }

    [Fact]
    public async Task SignedUrl_ShouldAnswer416_WhenTheRangeStartsPastTheEnd()
    {
        var (_, url) = await UploadAndSignAsync(FilesSamples.Pdf, "report.pdf");
        using var client = fixture.Host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url.PathAndQuery);
        request.Headers.Range = new RangeHeaderValue(FilesSamples.Pdf.Length, null);

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.RequestedRangeNotSatisfiable);
        response.Content.Headers.ContentRange!.ToString().Should().Be($"bytes */{FilesSamples.Pdf.Length}");
    }

    [Fact]
    public async Task SignedUrl_ShouldAnswer304_WhenTheEntityTagMatches()
    {
        var (file, url) = await UploadAndSignAsync(FilesSamples.Png, "a.png");
        using var client = fixture.Host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url.PathAndQuery);
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{file.Sha256}\""));

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task SignedUrl_ShouldAnswerHeadWithoutABody()
    {
        var (_, url) = await UploadAndSignAsync(FilesSamples.Png, "a.png");
        using var client = fixture.Host.CreateClient();

        using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url.PathAndQuery));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentLength.Should().Be(FilesSamples.Png.Length);
        (await response.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task SignedUrl_ShouldForceADownload_WhenTheTypeIsActive()
    {
        var (_, url) = await UploadAndSignAsync("<script>alert(1)</script>"u8.ToArray(), "page.html");
        using var client = fixture.Host.CreateClient();

        using var response = await client.GetAsync(url.PathAndQuery);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        Header(response, "Content-Security-Policy").Should().Contain("sandbox");
    }

    [Fact]
    public async Task PublicUrl_ShouldServeTheFileImmutably_WhenTheFileIsPublic()
    {
        var file = await UploadAsync(FilesSamples.Jpeg, "photo.jpg", FileVisibility.Public);
        var url = Value(await WithAsync<IFileUrlsBroker, Uri>(urls => urls.GetPublicUrlAsync(file.Id)));
        using var client = fixture.Host.CreateClient();

        using var response = await client.GetAsync(url.PathAndQuery);

        url.AbsolutePath.Should().Be($"/m/{file.Sha256}.jpg");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(FilesSamples.Jpeg);
        response.Headers.CacheControl!.ToString().Should().Contain("immutable").And.Contain("public");
    }

    [Fact]
    public async Task PublicUrl_ShouldNotServe_WhenNoPublicFileHasTheBytes()
    {
        var file = await UploadAsync(FilesSamples.Png, "private.png", FileVisibility.Private);
        using var client = fixture.Host.CreateClient();

        using var response = await client.GetAsync($"/m/{file.Sha256}.png");
        using var malformed = await client.GetAsync("/m/not-a-digest.png");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        malformed.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<(FileModel File, Uri Url)> UploadAndSignAsync(byte[] bytes, string name, TimeSpan? lifetime = null)
    {
        var file = await UploadAsync(bytes, name, FileVisibility.Private);
        var url = Value(await WithAsync<IFileUrlsBroker, SignedFileUrl>(urls =>
            urls.IssueAsync(file.Id, new UrlSpec { Lifetime = lifetime ?? TimeSpan.FromMinutes(5) })));
        return (file, url.Url);
    }

    private async Task<FileModel> UploadAsync(byte[] bytes, string name, FileVisibility visibility)
    {
        using var content = new MemoryStream(bytes);
        return Value(await WithAsync<IFileUploadsBroker, FileModel>(uploads =>
            uploads.UploadAsync(content, new UploadSpec { Name = name, Visibility = visibility })));
    }

    private async Task<Result<T>> WithAsync<TService, T>(Func<TService, Task<Result<T>>> call) where TService : notnull where T : notnull
    {
        await using var scope = fixture.Host.Services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<TService>());
    }

    private static string Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) || response.Content.Headers.TryGetValues(name, out values)
            ? string.Join(", ", values)
            : string.Empty;

    private static T Value<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Success success ? success.Value : throw new Xunit.Sdk.XunitException(((Result<T>.Failure)result).Error.Message);
}
