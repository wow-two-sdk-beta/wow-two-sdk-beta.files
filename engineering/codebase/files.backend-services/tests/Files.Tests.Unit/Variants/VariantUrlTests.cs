using System.Net;
using System.Text;
using System.Web;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using WoW.Two.Sdk.Beta.Be.Files.Client;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Files.Module.Urls;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace Files.Tests.Unit.Variants;

/// <summary>A variant's signed URL signs its width; the Client maps the variant fields both ways.</summary>
public sealed class VariantUrlTests
{
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 8, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Issue_ShouldSignTheWidth_SoAChangedWidthFailsToVerify()
    {
        var issuer = Issuer();
        var id = FileId.New();

        var query = HttpUtility.ParseQueryString(issuer.Issue("default", id, TimeSpan.FromMinutes(5), FileDisposition.Inline, 480).Url.Query);
        var expires = long.Parse(query["exp"]!);

        query["w"].Should().Be("480");
        issuer.Verify("default", id, expires, "i", query["sig"], 480).Should().BeTrue();
        issuer.Verify("default", id, expires, "i", query["sig"], 900).Should().BeFalse();
        issuer.Verify("default", id, expires, "i", query["sig"]).Should().BeFalse();
    }

    [Fact]
    public void Issue_ShouldLeaveTheOriginalsUrlWithoutAWidth()
    {
        var issuer = Issuer();
        var id = FileId.New();

        var query = HttpUtility.ParseQueryString(issuer.Issue("default", id, TimeSpan.FromMinutes(5), FileDisposition.Inline).Url.Query);

        query["w"].Should().BeNull();
        issuer.Verify("default", id, long.Parse(query["exp"]!), "i", query["sig"]).Should().BeTrue();
        issuer.Verify("default", id, long.Parse(query["exp"]!), "i", query["sig"], 480).Should().BeFalse();
    }

    [Fact]
    public async Task Client_ShouldReadTheVariants_OfAFile()
    {
        var handler = new RecordingHandler("""
            {"data":{"id":"0193a1b2-0000-7000-8000-000000000001","tenantId":"t1","name":"a.png","contentType":"image/png",
            "kind":"image","size":10,"sha256":"ab","visibility":"public","state":"ready","createdAtUtc":"2026-11-08T09:00:00+00:00",
            "variants":[{"width":480,"height":320,"contentType":"image/webp","size":1234},
                        {"width":900,"height":600,"contentType":"image/webp","size":4321}]}}
            """);
        await using var provider = Client(handler);

        var file = Value(await provider.GetRequiredService<IFileStoreBroker>().GetAsync(FileId.New()));

        file.Variants.Should().Equal(
            new FileVariantModel { Width = 480, Height = 320, ContentType = "image/webp", Size = 1234 },
            new FileVariantModel { Width = 900, Height = 600, ContentType = "image/webp", Size = 4321 });
    }

    [Fact]
    public async Task Client_ShouldSendTheWidth_ForSignedAndPublicUrls()
    {
        var handler = new RecordingHandler("""{"data":{"url":"https://files.example.test/f/x?w=480","expiresAtUtc":"2026-11-08T09:05:00+00:00"}}""");
        await using var provider = Client(handler);
        var urls = provider.GetRequiredService<IFileUrlsBroker>();

        Value(await urls.IssueAsync(FileId.New(), new UrlSpec { Width = 480 }));
        handler.Body.Should().Contain("\"width\":480");

        handler.Answer = """{"data":"https://files.example.test/m/abc.webp"}""";
        Value(await urls.GetPublicUrlAsync(FileId.New(), 900)).AbsolutePath.Should().Be("/m/abc.webp");
        handler.Uri!.Query.Should().Be("?width=900");
    }

    private SignedFileUrlIssuer Issuer()
    {
        var options = new FilesModuleOptions();
        options.Delivery.BaseUrl = new Uri("https://files.example.test/");
        options.Delivery.SigningKey = "unit-signing-key-0123456789abcdef0123";
        return new SignedFileUrlIssuer(Options.Create(options), clock);
    }

    private static ServiceProvider Client(RecordingHandler handler)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddFilesClient(options =>
        {
            options.BaseAddress = new Uri("https://files.example.test/");
            options.ApiKey = "files_UnitTestKey0123456789abcdefgh";
        }).ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }

    private static T Value<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Success success ? success.Value : throw new Xunit.Sdk.XunitException(((Result<T>.Failure)result).Error.Message);

    /// <summary>Answers every call with one JSON body and records the last request.</summary>
    private sealed class RecordingHandler(string answer) : HttpMessageHandler
    {
        public string Answer { get; set; } = answer;

        public Uri? Uri { get; private set; }

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Answer, Encoding.UTF8, "application/json") };
        }
    }
}
