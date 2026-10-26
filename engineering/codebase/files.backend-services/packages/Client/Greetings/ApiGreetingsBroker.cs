using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Files.Greetings;

namespace WoW.Two.Sdk.Beta.Be.Files.Client.Greetings;

/// <summary>Integrates the running Files service's greetings API.</summary>
/// <remarks>An unreachable or failing service returns an <see cref="AppErrorType.ExternalUnavailable"/> failure.</remarks>
internal sealed class ApiGreetingsBroker(HttpClient http) : IGreetingsBroker
{
    /// <inheritdoc />
    public async Task<Result<GreetingModel>> GreetAsync(string? name, CancellationToken cancellationToken = default)
    {
        var path = name is null
            ? GreetingRouteConstants.Greetings
            : $"{GreetingRouteConstants.Greetings}?{GreetingRouteConstants.NameParameter}={Uri.EscapeDataString(name)}";

        try
        {
            using var response = await http.GetAsync(path, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var envelope = await response.Content.ReadFromJsonAsync<GreetingEnvelope>(JsonSerializerOptions.Web, cancellationToken);
                return envelope?.Data is { } greeting
                    ? Result<GreetingModel>.Ok(greeting)
                    : Result<GreetingModel>.Fail(AppErrorFactory.SerializationFailed("The Files service answered without a greeting."));
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var problem = await ReadProblemAsync(response, cancellationToken);
                return Result<GreetingModel>.Fail(AppErrorFactory.Validation(problem?.Detail ?? problem?.Title ?? "The Files service rejected the name."));
            }

            return Result<GreetingModel>.Fail(AppErrorFactory.ExternalUnavailable($"The Files service answered {(int)response.StatusCode}."));
        }
        catch (HttpRequestException)
        {
            return Result<GreetingModel>.Fail(AppErrorFactory.ExternalUnavailable("The Files service is unreachable."));
        }
        catch (JsonException exception)
        {
            return Result<GreetingModel>.Fail(AppErrorFactory.SerializationFailed("The Files service answered malformed JSON.", exception));
        }
    }

    /// <summary>Reads the problem body, or nothing when the response carries none.</summary>
    private static async Task<ProblemBody?> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemBody>(JsonSerializerOptions.Web, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
