using System.Web;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Files.Module.Urls;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;

namespace Files.Tests.Unit.Module;

public sealed class SignedFileUrlIssuerTests
{
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 10, 27, 9, 0, 0, TimeSpan.Zero));
    private readonly SignedFileUrlIssuer issuer;

    public SignedFileUrlIssuerTests()
    {
        var options = new FilesModuleOptions();
        options.Delivery.BaseUrl = new Uri("https://files.example.test/");
        options.Delivery.SigningKey = "unit-signing-key-0123456789abcdef0123";
        options.Delivery.MaxLifetime = TimeSpan.FromHours(1);
        issuer = new SignedFileUrlIssuer(Options.Create(options), clock);
    }

    [Fact]
    public void Issue_ShouldSignAUrlThatVerifies_UntilItExpires()
    {
        var id = FileId.New();

        var url = issuer.Issue("default", id, TimeSpan.FromMinutes(5), FileDisposition.Attachment);
        var query = HttpUtility.ParseQueryString(url.Url.Query);

        url.Url.AbsolutePath.Should().Be($"/f/{id}");
        url.ExpiresAtUtc.Should().Be(clock.GetUtcNow().AddMinutes(5));
        issuer.Verify("default", id, long.Parse(query["exp"]!), query["d"]!, query["sig"]).Should().BeTrue();

        clock.Advance(TimeSpan.FromMinutes(6));
        issuer.Verify("default", id, long.Parse(query["exp"]!), query["d"]!, query["sig"]).Should().BeFalse();
    }

    [Fact]
    public void Verify_ShouldFail_WhenAnySignedPartChanges()
    {
        var id = FileId.New();
        var query = HttpUtility.ParseQueryString(issuer.Issue("acme", id, TimeSpan.FromMinutes(5), FileDisposition.Inline).Url.Query);
        var expires = long.Parse(query["exp"]!);

        issuer.Verify("acme", id, expires, "i", query["sig"]).Should().BeTrue();
        issuer.Verify("other", id, expires, "i", query["sig"]).Should().BeFalse();
        issuer.Verify("acme", FileId.New(), expires, "i", query["sig"]).Should().BeFalse();
        issuer.Verify("acme", id, expires + 60, "i", query["sig"]).Should().BeFalse();
        issuer.Verify("acme", id, expires, "a", query["sig"]).Should().BeFalse();
        issuer.Verify("acme", id, expires, "i", null).Should().BeFalse();
    }

    [Fact]
    public void Issue_ShouldPrefixTheTenantAndCapTheLifetime_WhenTheTenantIsNotTheDefault()
    {
        var url = issuer.Issue("acme", FileId.New(), TimeSpan.FromDays(30), FileDisposition.Inline);

        url.Url.AbsolutePath.Should().StartWith("/t/acme/f/");
        url.ExpiresAtUtc.Should().Be(clock.GetUtcNow().AddHours(1));
    }

    [Fact]
    public void PublicUrl_ShouldAddressTheBytes_UnderTheTenant()
    {
        issuer.PublicUrl("default", "ab12", "png").AbsoluteUri.Should().Be("https://files.example.test/m/ab12.png");
        issuer.PublicUrl("acme", "ab12", "png").AbsoluteUri.Should().Be("https://files.example.test/t/acme/m/ab12.png");
    }
}
