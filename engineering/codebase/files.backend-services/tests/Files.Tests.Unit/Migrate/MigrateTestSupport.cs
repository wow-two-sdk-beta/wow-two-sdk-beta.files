using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Files.Api.Migrate;

namespace Files.Tests.Unit.Migrate;

/// <summary>A throwaway folder under the system temp, removed on dispose.</summary>
public sealed class TempFolder : IDisposable
{
    public TempFolder() => Path = Directory.CreateTempSubdirectory("files-migrate-tests-").FullName;

    public string Path { get; }

    public string Write(string relative, byte[] bytes)
    {
        var full = System.IO.Path.Combine(Path, relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, bytes);
        return full;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}

/// <summary>One request the fake Files service saw: its route, token and multipart fields in order.</summary>
public sealed record SeenRequest(HttpMethod Method, string PathAndQuery, string? Token, IReadOnlyList<KeyValuePair<string, string>> Fields, byte[]? File, string? FileContentType, string? FileName)
{
    public string? Field(string name) => Fields.FirstOrDefault(field => field.Key == name).Value;
}

/// <summary>A fake Files service: records every request and answers through a function.</summary>
public sealed class FakeFilesHandler(Func<SeenRequest, HttpResponseMessage> answer) : HttpMessageHandler
{
    public List<SeenRequest> Requests { get; } = [];

    public IEnumerable<SeenRequest> Uploads => Requests.Where(request => request.Method == HttpMethod.Post);

    public static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Map(params (string Source, string Key, string Sha256, long Size)[] items) => MapPage(null, items);

    public static HttpResponseMessage MapPage(string? next, params (string Source, string Key, string Sha256, long Size)[] items)
    {
        var data = new JsonObject
        {
            ["items"] = new JsonArray(items.Select(item => (JsonNode)new JsonObject
            {
                ["source"] = item.Source,
                ["key"] = item.Key,
                ["reference"] = null,
                ["fileId"] = Guid.NewGuid().ToString(),
                ["sha256"] = item.Sha256,
                ["size"] = item.Size,
            }).ToArray()),
            ["next"] = next,
        };
        return Json(HttpStatusCode.OK, new JsonObject { ["data"] = data }.ToJsonString());
    }

    public static HttpResponseMessage Stored(SeenRequest request) => Json(HttpStatusCode.Created,
        $$$"""{"data":{"fileId":"{{{Guid.NewGuid()}}}","sha256":"{{{request.Field("sha256")}}}","size":{{{request.File!.Length}}},"created":true}}""");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var fields = new List<KeyValuePair<string, string>>();
        byte[]? file = null;
        string? fileType = null, fileName = null;
        if (request.Content is MultipartFormDataContent form)
        {
            foreach (var part in form)
            {
                var disposition = part.Headers.ContentDisposition!;
                var name = disposition.Name!.Trim('"');
                if (disposition.FileName is null)
                {
                    fields.Add(new(name, await part.ReadAsStringAsync(cancellationToken)));
                    continue;
                }

                fields.Add(new(name, "<file>"));
                file = await part.ReadAsByteArrayAsync(cancellationToken);
                fileType = part.Headers.ContentType?.ToString();
                fileName = disposition.FileName.Trim('"');
            }
        }

        var token = request.Headers.TryGetValues("X-Import-Token", out var values) ? values.Single() : null;
        var seen = new SeenRequest(request.Method, request.RequestUri!.PathAndQuery, token, fields, file, fileType, fileName);
        Requests.Add(seen);
        return answer(seen);
    }
}

/// <summary>Runs the mover in-process and builds its standard input.</summary>
public static class Mover
{
    public const string Token = "fimp_test-token.mac";

    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    public static MigrateOptions Options(HttpMessageHandler? handler = null, long memoryLimit = 64L * 1024 * 1024, string? tempDirectory = null, TimeSpan? progress = null) => new()
    {
        Handler = handler,
        RetryDelays = [TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero],
        MemoryLimit = memoryLimit,
        TempDirectory = tempDirectory ?? Path.GetTempPath(),
        ProgressInterval = progress ?? TimeSpan.FromHours(1),
    };

    public static string Input(JsonObject source, string? root = null, string? connection = null) => new JsonObject
    {
        ["files"] = "https://files.example.test/",
        ["import"] = "imp_1",
        ["token"] = Token,
        ["source"] = source,
        ["connection"] = connection,
        ["root"] = root,
    }.ToJsonString();

    public static JsonObject VolumeSource(string id = "brand-media", string path = "blobs/media", string? reference = "brand.media:{sha256}") => new()
    {
        ["id"] = id,
        ["wave"] = 1,
        ["kind"] = "volume-folder",
        ["volume"] = "brand-data",
        ["path"] = path,
        ["reference"] = reference,
    };

    public static async Task<MoverRun> RunAsync(string verb, string stdin, MigrateOptions options)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var code = await MigrateCommand.RunAsync([verb], new StringReader(stdin), stdout, stderr, options);
        return new MoverRun(code, stdout.ToString(), stderr.ToString());
    }
}

/// <summary>One mover run's exit code and output.</summary>
public sealed record MoverRun(int Code, string Stdout, string Stderr)
{
    public IReadOnlyList<string> Lines => Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public JsonElement Result => JsonDocument.Parse(Lines[^1]).RootElement.GetProperty("result");

    public long Count(string name) => Result.GetProperty(name).GetInt64();
}
