using System.Net;
using System.Net.Http.Headers;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Identity.Core.Signals;
using WoW.Two.Sdk.Beta.Be.Identity.Testing;
using WoW.Two.Sdk.Beta.Be.Messaging.Buses;

namespace Files.Tests.E2E;

/// <summary>A person's call through Identity: the app's scope and the person's <c>files:</c> permission together, and a
/// revoked session's token refused before it expires (t8 C6, C12, § Policy shape).</summary>
[Collection(FilesApiCollection.Name)]
public sealed class FilesApiIdentityTests(FilesApiFixture fixture)
{
    private const string Files = "/api/files/v1/files";

    [Fact]
    public async Task Person_ShouldAnswer403_WhenTheAppHasTheScopeButThePersonLacksThePermission()
    {
        var person = Person();
        using var client = Bearer(fixture.Issuer.IssueExchangedToken(person, "orders", FilesApiFixture.Audience, FilesScopeConstants.Read));

        using var response = await client.GetAsync(Files);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Person_ShouldAnswer403_WhenThePersonHasThePermissionButTheAppLacksTheScope()
    {
        var person = Person(FilesPermissionConstants.Read);
        using var client = Bearer(fixture.Issuer.IssueExchangedToken(person, "orders", FilesApiFixture.Audience, FilesScopeConstants.Write));

        using var response = await client.GetAsync(Files);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Person_ShouldUploadAsThemselves_WhenTheAppHasTheScopeAndThePersonThePermission()
    {
        var person = Person(FilesPermissionConstants.Write);
        using var client = Bearer(fixture.Issuer.IssueExchangedToken(person, "orders", FilesApiFixture.Audience, FilesScopeConstants.Write));

        using var created = await client.PostAsync(Files, FilesApiRequests.Multipart([1, 2, 3, 4], "a.bin"));
        var file = await FilesApiRequests.DataAsync(created);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        file.GetProperty("tenantId").GetString().Should().Be("tokens");
        file.GetProperty("ownerId").GetString().Should().Be(person.Id.ToString("D"));
        file.GetProperty("appId").GetString().Should().Be("orders");
    }

    [Fact]
    public async Task Person_ShouldActInTheirIssuersTenant_WhenTwoIssuersAreTrusted()
    {
        var person = Person(FilesPermissionConstants.Write);
        using var client = Bearer(fixture.OtherIssuer.IssueExchangedToken(person, "orders", FilesApiFixture.OtherAudience, FilesScopeConstants.Write));

        using var created = await client.PostAsync(Files, FilesApiRequests.Multipart([5, 6, 7], "b.bin"));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        (await FilesApiRequests.DataAsync(created)).GetProperty("tenantId").GetString().Should().Be("other");
    }

    [Fact]
    public async Task Person_ShouldAnswer401_OnceIdentitySignalsTheirSessionEnded()
    {
        var person = Person(FilesPermissionConstants.Read);
        using var client = Bearer(fixture.Issuer.IssueExchangedToken(person, "orders", FilesApiFixture.Audience, FilesScopeConstants.Read));
        using (var before = await client.GetAsync(Files))
            before.StatusCode.Should().Be(HttpStatusCode.OK);

        // A verified signal lands on the local bus; publishing there runs the same deny-list handler.
        var bus = fixture.Host.Services.GetRequiredService<IEventBus>();
        await bus.PublishAsync(new SessionEndedEvent
        {
            Issuer = fixture.Issuer.Issuer,
            SessionId = person.SessionId!.Value.ToString("D"),
            Subject = person.Id.ToString("D"),
            At = DateTimeOffset.UtcNow,
            TokensExpireBy = DateTimeOffset.UtcNow.AddMinutes(10),
        });

        var status = HttpStatusCode.OK;
        for (var attempt = 0; attempt < 20 && status != HttpStatusCode.Unauthorized; attempt++)
        {
            using var after = await client.GetAsync(Files);
            status = after.StatusCode;
            if (status != HttpStatusCode.Unauthorized)
                await Task.Delay(100);
        }

        status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Signal_ShouldAnswer401_WhenItCarriesNoSignature()
    {
        using var client = fixture.Host.CreateClient();

        using var response = await client.PostAsync("/signals/identity", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static FakePerson Person(params string[] permissions) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        SessionId = Guid.NewGuid(),
        Role = "member",
        Permissions = permissions,
    };

    private HttpClient Bearer(string token)
    {
        var client = fixture.Host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
