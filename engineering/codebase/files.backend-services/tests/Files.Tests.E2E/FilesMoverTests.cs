using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Files.Api.Migrate;

namespace Files.Tests.E2E;

/// <summary>The mover against the real import routes: Wheelhouse's operator key opens an import, the mover pushes a
/// folder with the import token, a rerun resumes, and verify compares every key (product/features/imports.md).</summary>
[Collection(FilesApiCollection.Name)]
public sealed class FilesMoverTests(FilesApiFixture fixture) : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("files-mover-e2e-").FullName;

    [Fact]
    public async Task Mover_ShouldPushResumeAndVerifyAFolder_ThroughTheImportRoutes()
    {
        var folder = Path.Combine(root, "blobs", "media");
        Directory.CreateDirectory(Path.Combine(folder, "ab"));
        await File.WriteAllBytesAsync(Path.Combine(folder, "ab", "first.bin"), [1, 2, 3, 4]);
        await File.WriteAllBytesAsync(Path.Combine(folder, "second.bin"), [5, 6, 7]);
        var (id, token) = await OpenAsync("brand-media");

        var (pushed, pushOutput) = await RunAsync("push", id, token);
        var (resumed, resumeOutput) = await RunAsync("push", id, token);
        var (verified, _) = await RunAsync("verify", id, token);
        await File.WriteAllBytesAsync(Path.Combine(folder, "second.bin"), [9, 9, 9]);
        var (changed, _) = await RunAsync("verify", id, token);

        pushed.Should().Be(0);
        Result(pushOutput).GetProperty("files").GetInt32().Should().Be(2);
        resumed.Should().Be(0);
        Result(resumeOutput).GetProperty("skipped").GetInt32().Should().Be(2);
        verified.Should().Be(0);
        changed.Should().Be(2);
        var map = await MapAsync(id);
        map.Select(item => item.GetProperty("key").GetString()).Should().BeEquivalentTo("ab/first.bin", "second.bin");
        map.Select(item => item.GetProperty("reference").GetString()).Should().AllSatisfy(reference => reference.Should().StartWith("brand.media:"));
    }

    public void Dispose() => Directory.Delete(root, recursive: true);

    private async Task<(string Id, string Token)> OpenAsync(string source)
    {
        using var client = OperatorClient();
        using var response = await client.PostAsJsonAsync("/api/files/v1/imports", new { tenant = "mover", sources = new[] { source } });
        response.EnsureSuccessStatusCode();
        var data = await FilesApiRequests.DataAsync(response);
        return (data.GetProperty("id").GetString()!, data.GetProperty("token").GetString()!);
    }

    private async Task<List<JsonElement>> MapAsync(string id)
    {
        using var client = OperatorClient();
        using var response = await client.GetAsync($"/api/files/v1/imports/{id}/map");
        return [.. (await FilesApiRequests.DataAsync(response)).GetProperty("items").EnumerateArray()];
    }

    private async Task<(int Exit, string Stdout)> RunAsync(string verb, string id, string token)
    {
        var input = JsonSerializer.Serialize(new
        {
            files = "http://localhost/",
            import = id,
            token,
            source = new { id = "brand-media", kind = "volume-folder", path = "blobs/media", reference = "brand.media:{sha256}" },
            root,
        });
        using var stdout = new StringWriter();
        var exit = await MigrateCommand.RunAsync(
            [verb],
            new StringReader(input),
            stdout,
            TextWriter.Null,
            new MigrateOptions { Handler = fixture.Host.Server.CreateHandler(), RetryDelays = [] });
        return (exit, stdout.ToString());
    }

    private static JsonElement Result(string stdout) =>
        JsonDocument.Parse(stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1]).RootElement.GetProperty("result");

    private HttpClient OperatorClient()
    {
        var client = fixture.Host.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", FilesApiFixture.OperatorKey);
        return client;
    }
}
