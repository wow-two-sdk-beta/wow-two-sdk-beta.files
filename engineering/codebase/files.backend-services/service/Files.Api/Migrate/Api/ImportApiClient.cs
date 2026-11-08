using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Files.Api.Migrate.Api;

/// <summary>Calls the import routes of <c>product/features/imports.md</c> § API with the import token: pages the id map
/// and uploads one file at a time.</summary>
/// <remarks>A 5xx, <c>408</c>, <c>429</c> or network error is retried after each of <paramref name="retryDelays"/>;
/// <c>401</c>, <c>403</c> and <c>404</c> are never retried.</remarks>
/// <param name="http">The client, its base address the Files service's.</param>
/// <param name="import">The import's id.</param>
/// <param name="token">The import token.</param>
/// <param name="retryDelays">The waits before each retry.</param>
/// <param name="time">The clock the waits run on.</param>
/// <param name="mapPageSize">The <c>limit</c> each map page asks for.</param>
public sealed class ImportApiClient(HttpClient http, string import, string token, IReadOnlyList<TimeSpan> retryDelays, TimeProvider time, int mapPageSize)
{
    /// <summary>Holds the header the import token travels in.</summary>
    public const string TokenHeader = "X-Import-Token";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private string ImportPath => $"api/files/v1/imports/{Uri.EscapeDataString(import)}";

    /// <summary>Reads every page of the import's map and keeps one source's rows, by key.</summary>
    /// <param name="source">The source id whose rows are kept.</param>
    /// <param name="cancellationToken">The token that cancels the read.</param>
    /// <exception cref="MigrateRefusedException">Files refused the token or does not know the import.</exception>
    /// <exception cref="MigrateUnavailableException">Files stayed unreachable or answered unreadably.</exception>
    public async Task<IReadOnlyDictionary<string, ImportMapEntry>> ReadMapAsync(string source, CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, ImportMapEntry>(StringComparer.Ordinal);
        string? cursor = null;
        do
        {
            var path = string.Create(CultureInfo.InvariantCulture, $"{ImportPath}/map?limit={mapPageSize}")
                + (cursor is null ? string.Empty : "&cursor=" + Uri.EscapeDataString(cursor));
            using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, path), cancellationToken)
                ?? throw new MigrateUnavailableException("The import's map is unreachable.");
            var status = (int)response.StatusCode;
            if (IsRefusal(response.StatusCode))
                throw new MigrateRefusedException($"The import's map answered {status}.");
            if (!response.IsSuccessStatusCode)
                throw new MigrateUnavailableException($"The import's map answered {status}.");

            var next = await ReadPageAsync(response, source, map, cancellationToken);
            if (next is not null && next == cursor)
                throw new MigrateUnavailableException("The import's map repeated its cursor.");
            cursor = next;
        }
        while (cursor is not null);

        return map;
    }

    /// <summary>Uploads one file: its fields, then the bytes.</summary>
    /// <param name="upload">The upload's fields.</param>
    /// <param name="content">The bytes, replayed for every attempt.</param>
    /// <param name="cancellationToken">The token that cancels the upload.</param>
    public async Task<ImportUploadResult> UploadAsync(ImportUpload upload, ContentBuffer content, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"{ImportPath}/files") { Content = Form(upload, content) }, cancellationToken);
        if (response is null)
            return new ImportUploadResult(ImportUploadOutcome.Failed, "unreachable");

        var reason = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);
        var outcome = response.StatusCode switch
        {
            HttpStatusCode.Created => ImportUploadOutcome.Stored,
            HttpStatusCode.OK => ImportUploadOutcome.Skipped,
            _ when IsRefusal(response.StatusCode) => ImportUploadOutcome.Refused,
            _ => ImportUploadOutcome.Failed,
        };
        return new ImportUploadResult(outcome, reason);
    }

    /// <summary>Builds the multipart body: <c>source</c>, <c>key</c>, <c>sha256</c> and the optional fields before the
    /// file part, which carries a content type only when the source names one.</summary>
    /// <param name="upload">The upload's fields.</param>
    /// <param name="content">The bytes.</param>
    public static MultipartFormDataContent Form(ImportUpload upload, ContentBuffer content)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(upload.Source), "source" },
            { new StringContent(upload.Key), "key" },
            { new StringContent(upload.Sha256), "sha256" },
        };
        if (upload.Reference is not null)
            form.Add(new StringContent(upload.Reference), "reference");
        if (upload.Name is not null)
            form.Add(new StringContent(upload.Name), "name");
        if (upload.ContentType is not null)
            form.Add(new StringContent(upload.ContentType), "contentType");

        var file = content.CreateContent();
        if (upload.ContentType is not null && MediaTypeHeaderValue.TryParse(upload.ContentType, out var type))
            file.Headers.ContentType = type;
        form.Add(file, "file", upload.Name ?? upload.Key[(upload.Key.LastIndexOf('/') + 1)..]);
        return form;
    }

    private static bool IsRefusal(HttpStatusCode status) =>
        status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound;

    private static bool IsTransient(HttpStatusCode status) =>
        (int)status >= 500 || status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;

    private static async Task<string?> ReadPageAsync(HttpResponseMessage response, string source, Dictionary<string, ImportMapEntry> map, CancellationToken cancellationToken)
    {
        try
        {
            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
            var page = document.RootElement.TryGetProperty("data", out var data) ? data : document.RootElement;
            foreach (var item in page.GetProperty("items").EnumerateArray())
            {
                var row = item.Deserialize<MapItem>(Json);
                if (row is { Source: { } rowSource, Key: { } key, Sha256: { } sha256 } && rowSource == source)
                    map[key] = new ImportMapEntry(sha256.ToLowerInvariant(), row.Size);
            }

            return page.TryGetProperty("next", out var next) ? next.ValueKind switch
            {
                JsonValueKind.String when next.GetString() is { Length: > 0 } text => text,
                JsonValueKind.Number => next.GetRawText(),
                _ => null,
            } : null;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new MigrateUnavailableException("The import's map answered malformed JSON.");
        }
    }

    private async Task<HttpResponseMessage?> SendAsync(Func<HttpRequestMessage> create, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = create();
                request.Headers.TryAddWithoutValidation(TokenHeader, token);
                response = await http.SendAsync(request, cancellationToken);
                if (!IsTransient(response.StatusCode))
                    return response;
            }
            catch (HttpRequestException)
            {
                // Unreachable: retried below.
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The request timed out: retried below.
            }

            if (attempt >= retryDelays.Count)
                return response;

            response?.Dispose();
            await Task.Delay(retryDelays[attempt], time, cancellationToken);
        }
    }

    /// <summary>Represents one map row on the wire.</summary>
    private sealed record MapItem(string? Source, string? Key, string? Reference, string? FileId, string? Sha256, long Size);
}
