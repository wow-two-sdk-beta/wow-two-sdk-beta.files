using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Foundation.Serialization;

namespace WoW.Two.Sdk.Beta.Be.Files.Client.Transport;

/// <summary>Sends calls to the Files service and reads its answers: the <c>{ data }</c> envelope on success, the problem
/// body's <c>code</c> back into an <see cref="AppError"/> on failure.</summary>
/// <remarks>An unreachable or failing service returns an <see cref="AppErrorType.ExternalUnavailable"/> failure.</remarks>
internal sealed class FilesApiTransport(IHttpClientFactory clients)
{
    /// <summary>Gets the wire's JSON options, the be sdk preset the service writes with.</summary>
    public static JsonSerializerOptions Json => JsonOptionsConstants.Default;

    /// <summary>Sends a call whose answer carries a value.</summary>
    public async Task<Result<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken) where T : notnull
    {
        var sent = await SendRawAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (sent is Result<HttpResponseMessage>.Failure failure)
            return Result<T>.Fail(failure.Error);

        using var response = ((Result<HttpResponseMessage>.Success)sent).Value;
        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<Envelope<T>>(Json, cancellationToken);
            return envelope is { Data: { } data }
                ? Result<T>.Ok(data)
                : Result<T>.Fail(AppErrorFactory.SerializationFailed("The Files service answered without data."));
        }
        catch (JsonException exception)
        {
            return Result<T>.Fail(AppErrorFactory.SerializationFailed("The Files service answered malformed JSON.", exception));
        }
    }

    /// <summary>Sends a call whose answer carries no value.</summary>
    public async Task<Result> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sent = await SendRawAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (sent is Result<HttpResponseMessage>.Failure failure)
            return Result.Fail(failure.Error);

        ((Result<HttpResponseMessage>.Success)sent).Value.Dispose();
        return Result.Ok();
    }

    /// <summary>Sends a call and hands back a successful response, which the caller disposes.</summary>
    public async Task<Result<HttpResponseMessage>> SendRawAsync(HttpRequestMessage request, HttpCompletionOption completion, CancellationToken cancellationToken)
    {
        try
        {
            var response = await clients.CreateClient(FilesClientConstants.HttpClientName).SendAsync(request, completion, cancellationToken);
            if (response.IsSuccessStatusCode)
                return Result<HttpResponseMessage>.Ok(response);

            using (response)
                return Result<HttpResponseMessage>.Fail(await ReadErrorAsync(response, cancellationToken));
        }
        catch (HttpRequestException)
        {
            return Result<HttpResponseMessage>.Fail(AppErrorFactory.ExternalUnavailable("The Files service is unreachable."));
        }
    }

    /// <summary>Maps a failed answer to the error the service reported, by the problem's <c>code</c>, else by status.</summary>
    internal static async Task<AppError> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        Problem? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<Problem>(Json, cancellationToken);
        }
        catch (JsonException)
        {
            // A body that is no problem falls back to the status.
        }

        var message = problem?.Detail ?? problem?.Title ?? $"The Files service answered {(int)response.StatusCode}.";
        if (Enum.TryParse<AppErrorType>(problem?.Code, ignoreCase: false, out var type))
            return AppError.Of(type, message);

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.RequestedRangeNotSatisfiable => AppErrorFactory.Validation(message),
            HttpStatusCode.Unauthorized => AppErrorFactory.Unauthorized(message),
            HttpStatusCode.Forbidden => AppErrorFactory.Forbidden(message),
            HttpStatusCode.NotFound => AppErrorFactory.NotFound(message),
            HttpStatusCode.Conflict => AppErrorFactory.Conflict(message),
            _ => AppErrorFactory.ExternalUnavailable(message),
        };
    }

    /// <summary>Represents the service's success envelope.</summary>
    private sealed record Envelope<T>(T? Data);

    /// <summary>Represents the parts of a problem body the client reads.</summary>
    private sealed record Problem(string? Code, string? Detail, string? Title);
}
