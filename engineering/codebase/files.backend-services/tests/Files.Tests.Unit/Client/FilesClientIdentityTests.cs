using System.Buffers.Text;
using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Client;
using WoW.Two.Sdk.Beta.Be.Files.Client.Transport;
using WoW.Two.Sdk.Beta.Be.Http.Auth.OAuth2;
using WoW.Two.Sdk.Beta.Be.Identity.Testing;

namespace Files.Tests.Unit.Client;

public sealed class FilesClientIdentityTests : IDisposable
{
    private const string ClientId = "orders";
    private const string FilesResource = "https://files.test/api/";
    private static readonly Uri FilesRoot = new("https://files.test/api");

    private readonly FakeTokenIssuer issuer = new();
    private readonly FilesServiceHandler service = new();
    private readonly FakePerson person = new() { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Permissions = ["files:read"] };

    [Fact]
    public async Task SendAsync_ShouldSendAClientCredentialsToken_WhenNoPersonIsOnTheCall()
    {
        await using var provider = Provider(options => options.Identity = Identity());

        await Send(provider);

        var claims = Claims(service.Calls.Single().Bearer);
        claims.GetProperty("sub").GetString().Should().Be(ClientId);
        claims.GetProperty("client_id").GetString().Should().Be(ClientId);
        claims.GetProperty("aud").GetString().Should().Be(FilesResource, "the resource defaults to the service root with its slash");
        claims.TryGetProperty("act", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ShouldSendAnExchangedToken_WhenAPersonIsOnTheCall()
    {
        await using var provider = Provider(options => options.Identity = Identity());
        PutPersonOnTheCall(provider);

        await Send(provider);

        var claims = Claims(service.Calls.Single().Bearer);
        claims.GetProperty("sub").GetString().Should().Be(person.Id.ToString());
        claims.GetProperty("act").GetProperty("sub").GetString().Should().Be(ClientId);
        claims.GetProperty("aud").GetString().Should().Be(FilesResource);
        service.Calls.Single().ApiKey.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_ShouldSendAClientCredentialsToken_WhenTheCallRunsAsTheServiceForAPresentPerson()
    {
        await using var provider = Provider(options => options.Identity = Identity());
        PutPersonOnTheCall(provider);

        using (FilesClientCalls.AsService())
            await Send(provider);
        await Send(provider);

        var asService = Claims(service.Calls[0].Bearer);
        asService.GetProperty("sub").GetString().Should().Be(ClientId);
        asService.TryGetProperty("act", out _).Should().BeFalse();
        Claims(service.Calls[1].Bearer).GetProperty("sub").GetString().Should().Be(person.Id.ToString(), "the scope ended");
    }

    [Fact]
    public async Task SendAsync_ShouldSendTheApiKey_WhenTheClientHasOne()
    {
        await using var provider = Provider(options => options.ApiKey = "tenant-key");
        PutPersonOnTheCall(provider);

        await Send(provider);

        service.Calls.Single().Should().Be(new FilesCall(null, "tenant-key"));
    }

    [Fact]
    public void Options_ShouldFailValidation_WhenBothApiKeyAndIdentityAreSet()
    {
        using var provider = Provider(options =>
        {
            options.ApiKey = "tenant-key";
            options.Identity = Identity();
        });

        FluentActions.Invoking(() => provider.GetRequiredService<IOptions<FilesClientOptions>>().Value)
            .Should().Throw<OptionsValidationException>()
            .WithMessage("*set ApiKey or Identity, not both*");
    }

    public void Dispose() => issuer.Dispose();

    private ServiceProvider Provider(Action<FilesClientOptions> configure)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddFilesClient(options =>
            {
                options.BaseAddress = FilesRoot;
                configure(options);
            })
            .ConfigurePrimaryHttpMessageHandler(() => service);
        services.AddHttpClient(OAuth2HttpClientNames.TokenEndpoint).ConfigurePrimaryHttpMessageHandler(issuer.CreateTokenEndpointHandler);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static FilesClientIdentityOptions Identity() => new()
    {
        TokenEndpoint = new Uri("https://identity.test/connect/token"),
        ClientId = ClientId,
        ClientSecret = "test-secret",
    };

    /// <summary>Makes the current request carry the person's own token, as the calling service received it.</summary>
    private void PutPersonOnTheCall(IServiceProvider provider)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer " + issuer.IssuePersonToken(person, "https://orders.test/", ClientId);
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
    }

    private static async Task Send(IServiceProvider provider)
    {
        var sent = await provider.GetRequiredService<FilesApiTransport>().SendAsync(new HttpRequestMessage(HttpMethod.Get, "files"), CancellationToken.None);
        sent.IsSuccess.Should().BeTrue();
    }

    private static JsonElement Claims(string? bearer)
    {
        bearer.Should().NotBeNullOrEmpty();
        return JsonDocument.Parse(Base64Url.DecodeFromChars(bearer!.Split('.')[1])).RootElement;
    }

    /// <summary>Represents what one call brought to the Files service.</summary>
    private sealed record FilesCall(string? Bearer, string? ApiKey);

    /// <summary>Stands in for the Files service: records each call's credentials and answers <c>204</c>.</summary>
    private sealed class FilesServiceHandler : HttpMessageHandler
    {
        public List<FilesCall> Calls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var bearer = request.Headers.Authorization is { Scheme: "Bearer" } header ? header.Parameter : null;
            var apiKey = request.Headers.TryGetValues(FilesClientConstants.ApiKeyHeader, out var keys) ? keys.Single() : null;
            Calls.Add(new FilesCall(bearer, apiKey));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
    }
}
