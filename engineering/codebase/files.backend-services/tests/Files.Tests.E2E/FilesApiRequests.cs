using System.Net.Http.Headers;
using System.Text.Json;

namespace Files.Tests.E2E;

/// <summary>Builds the API's requests and reads its envelope, for every E2E class.</summary>
internal static class FilesApiRequests
{
    /// <summary>Builds a multipart upload of one file part and its name.</summary>
    public static MultipartFormDataContent Multipart(byte[] bytes, string name)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent { { new StringContent(name), "name" }, { file, "file", name } };
    }

    /// <summary>Reads the <c>data</c> of the API's envelope.</summary>
    public static async Task<JsonElement> DataAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("data").Clone();
    }
}
