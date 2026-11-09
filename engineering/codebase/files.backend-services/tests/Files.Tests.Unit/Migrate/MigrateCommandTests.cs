using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Files.Api.Migrate.Input;
using Files.Api.Migrate.Verbs;
using Microsoft.Extensions.Time.Testing;

namespace Files.Tests.Unit.Migrate;

public sealed class MigrateInputTests
{
    // ocharo-platform engineering/deployment/files-migration.yml, each source as the job writes it into stdin.
    private const string Manifest = """
        [
          { "id": "brand-media", "wave": 1, "kind": "volume-folder", "volume": "brand-data", "path": "blobs/media", "reference": "brand.media:{sha256}" },
          { "id": "catalogue-media", "wave": 1, "kind": "volume-folder", "volume": "catalogue-data", "path": "blobs/media", "reference": "catalogue.media:{sha256}" },
          { "id": "catalogue-release-assets", "wave": 1, "kind": "sqlite-column", "volume": "catalogue-data", "database": "ocharo-catalogue.db", "table": "catalogue_release_assets", "key": "Sha256", "content": "Bytes", "type": "ContentType", "reference": "catalogue.release:{Sha256}" },
          { "id": "desk-preparation-media", "wave": 3, "kind": "postgres-column", "database": "desk", "table": "preparation.media", "key": ["organization_id", "id"], "content": "bytes", "name": "file_name", "type": "content_type", "reference": "desk.preparation-media:{organization_id}/{id}" },
          { "id": "desk-preparation-exports", "wave": 3, "kind": "postgres-column", "database": "desk", "table": "preparation.exports", "key": ["organization_id", "id"], "content": "bytes", "reference": "desk.preparation-export:{organization_id}/{id}" },
          { "id": "desk-commerce-media", "wave": 3, "kind": "postgres-column", "database": "desk", "table": "commerce.media", "key": ["brand_id", "sha256"], "content": "bytes", "type": "content_type", "reference": "desk.commerce-media:{brand_id}/{sha256}" },
          { "id": "desk-sourcing-media", "wave": 3, "kind": "postgres-column", "database": "desk", "table": "sourcing.media_contents", "key": ["organization_id", "sha256"], "content": "bytes", "reference": "desk.sourcing-media:{organization_id}/{sha256}" }
        ]
        """;

    public static TheoryData<int> ManifestSources => new() { 0, 1, 2, 3, 4, 5, 6 };

    [Theory]
    [MemberData(nameof(ManifestSources))]
    public void Parse_ShouldReadEveryOcharoManifestSource(int index)
    {
        var source = JsonNode.Parse(Manifest)!.AsArray()[index]!.AsObject();

        var request = MigrateRequestParser.Parse(Mover.Input((JsonObject)source.DeepClone(), root: "/source", connection: "Host=db;Database=desk"), MigrateVerbConstants.Push);

        request.Source.Id.Should().Be(source["id"]!.GetValue<string>());
        request.Source.Kind.Should().Be(source["kind"]!.GetValue<string>());
        request.Source.Reference!.ToString().Should().Be(source["reference"]!.GetValue<string>());
        request.Files.Should().Be(new Uri("https://files.example.test/"));
        request.Token.Should().Be(Mover.Token);
    }

    [Fact]
    public void Parse_ShouldReadACompositeKeyInOrder()
    {
        var source = JsonNode.Parse(Manifest)!.AsArray()[3]!.AsObject();

        var request = MigrateRequestParser.Parse(Mover.Input((JsonObject)source.DeepClone(), connection: "Host=db"), MigrateVerbConstants.Plan);

        request.Source.Key.Should().Equal("organization_id", "id");
        request.Source.Name.Should().Be("file_name");
        request.Source.Type.Should().Be("content_type");
    }

    [Fact]
    public void Parse_ShouldAddTheTrailingSlash_WhenTheFilesAddressHasNone()
    {
        var input = JsonNode.Parse(Mover.Input(Mover.VolumeSource(), root: "/source"))!.AsObject();
        input["files"] = "https://files.example.test/base";

        MigrateRequestParser.Parse(input.ToJsonString(), MigrateVerbConstants.Verify).Files.Should().Be(new Uri("https://files.example.test/base/"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1, 2]")]
    [InlineData("""{"files":"https://f/","import":"i","token":"t","root":"/r"}""")]
    [InlineData("""{"files":"https://f/","import":"i","token":"t","root":"/r","source":{"id":"s","kind":"s3-bucket"}}""")]
    [InlineData("""{"files":"https://f/","import":"i","token":"t","root":"/r","source":{"id":"s","kind":"sqlite-column","database":"d.db","key":"k","content":"c"}}""")]
    [InlineData("""{"files":"https://f/","import":"i","token":"t","root":"/r","source":{"id":"s","kind":"sqlite-column","database":"d.db","table":"t","key":[],"content":"c"}}""")]
    [InlineData("""{"files":"https://f/","import":"i","token":"t","root":"/r","source":{"id":"s","kind":"volume-folder","path":"p","reference":"x:{name}"}}""")]
    [InlineData("""{"files":"https://f/","import":"i","token":"t","source":{"id":"s","kind":"volume-folder","path":"p"}}""")]
    [InlineData("""{"files":"https://f/","import":"i","root":"/r","source":{"id":"s","kind":"volume-folder","path":"p"}}""")]
    [InlineData("""{"files":"ftp://f/","import":"i","token":"t","root":"/r","source":{"id":"s","kind":"volume-folder","path":"p"}}""")]
    [InlineData("""{"files":"https://f/","import":"i","token":"t","source":{"id":"s","kind":"postgres-column","table":"t","key":"k","content":"c"}}""")]
    [InlineData("""{"files":"https://f/","import":"i","token":"t","root":"/r","source":{"id":7,"kind":"volume-folder","path":"p"}}""")]
    public async Task RunAsync_ShouldExitOne_WhenTheInputIsRefused(string stdin)
    {
        var handler = new FakeFilesHandler(_ => throw new InvalidOperationException("no call expected"));

        var run = await Mover.RunAsync(MigrateVerbConstants.Push, stdin, Mover.Options(handler));

        run.Code.Should().Be(MigrateExitCodeConstants.Refused);
        run.Stdout.Should().BeEmpty();
        run.Stderr.Should().NotBeEmpty();
        handler.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData]
    [InlineData("deploy")]
    [InlineData("push", "extra")]
    public async Task RunAsync_ShouldExitOne_WhenTheVerbIsMissingOrUnknown(params string[] args)
    {
        var stderr = new StringWriter();

        var code = await Files.Api.Migrate.MigrateCommand.RunAsync(args, new StringReader("{}"), new StringWriter(), stderr);

        code.Should().Be(MigrateExitCodeConstants.Refused);
        stderr.ToString().Should().Contain("migrate plan|push|verify");
    }

    [Fact]
    public async Task RunAsync_ShouldNeverEchoTheTokenOrTheConnection_WhenItRefusesTheInput()
    {
        const string stdin = """{"files":"not an address","import":"i","token":"fimp_secret.mac","connection":"Password=hunter2","source":{"id":"s","kind":"postgres-column","table":"t","key":"k","content":"c"}}""";

        var run = await Mover.RunAsync(MigrateVerbConstants.Push, stdin, Mover.Options());

        run.Code.Should().Be(MigrateExitCodeConstants.Refused);
        run.Stderr.Should().NotContain("fimp_secret").And.NotContain("hunter2");
    }

    [Fact]
    public async Task RunAsync_ShouldReadAMissingSourceFolderAsEmpty_AndExitZero()
    {
        using var root = new TempFolder();

        var run = await Mover.RunAsync(MigrateVerbConstants.Plan, Mover.Input(Mover.VolumeSource(path: "blobs/none"), root: root.Path), Mover.Options());

        run.Code.Should().Be(MigrateExitCodeConstants.Done);
        run.Lines.Should().Equal("""{"result":{"files":0,"bytes":0,"skipped":0,"failed":0}}""");
        run.Stderr.Should().Contain("reads as an empty source");
    }

    [Fact]
    public async Task RunAsync_ShouldExitOne_WhenTheRootIsMissing()
    {
        using var root = new TempFolder();

        var run = await Mover.RunAsync(MigrateVerbConstants.Plan, Mover.Input(Mover.VolumeSource(), root: Path.Combine(root.Path, "not-mounted")), Mover.Options());

        run.Code.Should().Be(MigrateExitCodeConstants.Refused);
    }

    [Fact]
    public async Task RunAsync_ShouldNameTheSqliteError_AndExitTwo_WhenTheDatabaseIsUnreadable()
    {
        using var root = new TempFolder();
        root.Write("c.db", Encoding.UTF8.GetBytes(new string('x', 4096)));
        var source = new JsonObject { ["id"] = "c", ["kind"] = "sqlite-column", ["database"] = "c.db", ["table"] = "a", ["key"] = "k", ["content"] = "b" };

        var run = await Mover.RunAsync(MigrateVerbConstants.Plan, Mover.Input(source, root: root.Path), Mover.Options());

        run.Code.Should().Be(MigrateExitCodeConstants.Failed);
        run.Stderr.Should().Contain("The source could not be read: SQLite Error 26");
    }
}

public sealed class PlanVerbTests
{
    [Fact]
    public async Task RunAsync_ShouldCountFilesAndBytes_WithoutTouchingTheNetwork()
    {
        using var root = new TempFolder();
        root.Write("blobs/media/ab/a", new byte[3]);
        root.Write("blobs/media/ab/b", new byte[4]);
        root.Write("blobs/media/cd/c", new byte[5]);
        var input = JsonNode.Parse(Mover.Input(Mover.VolumeSource(), root: root.Path))!.AsObject();
        input.Remove("token");
        input.Remove("files");
        var handler = new FakeFilesHandler(_ => throw new InvalidOperationException("plan touches nothing remote"));

        var run = await Mover.RunAsync(MigrateVerbConstants.Plan, input.ToJsonString(), Mover.Options(handler));

        run.Code.Should().Be(MigrateExitCodeConstants.Done);
        run.Lines.Should().Equal("""{"result":{"files":3,"bytes":12,"skipped":0,"failed":0}}""");
        run.Stderr.Should().Contain("plan: files=3 bytes=12 skipped=0 failed=0");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_ShouldCountASqliteColumn()
    {
        using var root = new TempFolder();
        var path = Path.Combine(root.Path, "c.db");
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE a (k TEXT, b BLOB); INSERT INTO a VALUES ('x', x'0102'), ('y', x'030405'), ('z', NULL);";
            command.ExecuteNonQuery();
        }

        var source = new JsonObject { ["id"] = "c", ["kind"] = "sqlite-column", ["database"] = "c.db", ["table"] = "a", ["key"] = "k", ["content"] = "b" };
        var run = await Mover.RunAsync(MigrateVerbConstants.Plan, Mover.Input(source, root: root.Path), Mover.Options());

        run.Code.Should().Be(MigrateExitCodeConstants.Done);
        run.Lines.Should().Equal("""{"result":{"files":2,"bytes":5,"skipped":0,"failed":0}}""");
    }
}

public sealed class PushVerbTests : IDisposable
{
    private readonly TempFolder root = new();
    private readonly Dictionary<string, byte[]> files = new()
    {
        ["a.bin"] = Encoding.UTF8.GetBytes("alpha"),
        ["b.bin"] = Encoding.UTF8.GetBytes("bravo!"),
        ["c.bin"] = Encoding.UTF8.GetBytes("charlie"),
        ["d.bin"] = Encoding.UTF8.GetBytes("delta"),
        ["e.bin"] = Encoding.UTF8.GetBytes("echo"),
    };

    public PushVerbTests()
    {
        foreach (var (name, bytes) in files)
            root.Write($"blobs/media/{name}", bytes);
    }

    [Fact]
    public async Task RunAsync_ShouldSkipMapKeys_AndCountEachAnswer()
    {
        var handler = new FakeFilesHandler(request => request.Method == HttpMethod.Get
            ? FakeFilesHandler.Map(("brand-media", "a.bin", Mover.Sha256(files["a.bin"]), 5), ("other-source", "b.bin", "00", 6))
            : request.Field("key") switch
            {
                "b.bin" => FakeFilesHandler.Stored(request),
                "c.bin" => FakeFilesHandler.Json(HttpStatusCode.OK, "{}"),
                "d.bin" => FakeFilesHandler.Json(HttpStatusCode.Conflict, "{}"),
                _ => FakeFilesHandler.Json(HttpStatusCode.UnprocessableEntity, "{}"),
            });

        var run = await Mover.RunAsync(MigrateVerbConstants.Push, Input(), Mover.Options(handler));

        run.Code.Should().Be(MigrateExitCodeConstants.Failed);
        run.Lines.Should().Equal("""{"result":{"files":5,"bytes":27,"skipped":2,"failed":2}}""");
        run.Stderr.Should().Contain("failed d.bin: 409").And.Contain("failed e.bin: 422").And.NotContain("alpha");
        handler.Requests.Should().HaveCount(5);
        handler.Requests[0].PathAndQuery.Should().Be("/api/files/v1/imports/imp_1/map?limit=500");
        handler.Requests.Should().OnlyContain(request => request.Token == Mover.Token);
        handler.Uploads.Select(upload => upload.Field("key")).Should().Equal("b.bin", "c.bin", "d.bin", "e.bin");
    }

    [Fact]
    public async Task RunAsync_ShouldSendTheFieldsBeforeTheFile_WithTheHashOfTheBytes()
    {
        var handler = new FakeFilesHandler(request => request.Method == HttpMethod.Get ? FakeFilesHandler.Map() : FakeFilesHandler.Stored(request));

        var run = await Mover.RunAsync(MigrateVerbConstants.Push, Input(), Mover.Options(handler));

        run.Code.Should().Be(MigrateExitCodeConstants.Done);
        run.Count("skipped").Should().Be(0);
        var upload = handler.Uploads.First();
        upload.PathAndQuery.Should().Be("/api/files/v1/imports/imp_1/files");
        var sha = Mover.Sha256(files["a.bin"]);
        upload.Fields.Should().Equal(
            new KeyValuePair<string, string>("source", "brand-media"),
            new KeyValuePair<string, string>("key", "a.bin"),
            new KeyValuePair<string, string>("sha256", sha),
            new KeyValuePair<string, string>("reference", $"brand.media:{sha}"),
            new KeyValuePair<string, string>("file", "<file>"));
        upload.File.Should().Equal(files["a.bin"]);
        upload.FileName.Should().Be("a.bin");
        upload.FileContentType.Should().BeNull("Files detects the type when the source names none");
    }

    [Fact]
    public async Task RunAsync_ShouldReadEveryMapPage_AndSkipKeysFromEach()
    {
        var handler = new FakeFilesHandler(request => request.PathAndQuery switch
        {
            "/api/files/v1/imports/imp_1/map?limit=500" => FakeFilesHandler.MapPage("page 2", ("brand-media", "a.bin", "aa", 5), ("brand-media", "b.bin", "bb", 6)),
            "/api/files/v1/imports/imp_1/map?limit=500&cursor=page%202" => FakeFilesHandler.MapPage(null, ("brand-media", "c.bin", "cc", 7), ("brand-media", "d.bin", "dd", 5)),
            _ => FakeFilesHandler.Stored(request),
        });

        var run = await Mover.RunAsync(MigrateVerbConstants.Push, Input(), Mover.Options(handler));

        run.Code.Should().Be(MigrateExitCodeConstants.Done);
        run.Count("skipped").Should().Be(4);
        handler.Uploads.Select(upload => upload.Field("key")).Should().Equal("e.bin");
    }

    [Fact]
    public async Task RunAsync_ShouldStopAndExitOne_WhenAnUploadIsUnauthorized()
    {
        var handler = new FakeFilesHandler(request => request.Method == HttpMethod.Get ? FakeFilesHandler.Map() : FakeFilesHandler.Json(HttpStatusCode.Unauthorized, "{}"));

        var run = await Mover.RunAsync(MigrateVerbConstants.Push, Input(), Mover.Options(handler));

        run.Code.Should().Be(MigrateExitCodeConstants.Refused);
        handler.Uploads.Should().ContainSingle("a refused token is never retried, nor the next file sent");
        run.Stderr.Should().Contain("401").And.NotContain(Mover.Token);
    }

    [Fact]
    public async Task RunAsync_ShouldExitOne_WhenTheMapIsUnauthorized()
    {
        var handler = new FakeFilesHandler(_ => FakeFilesHandler.Json(HttpStatusCode.Unauthorized, "{}"));

        var run = await Mover.RunAsync(MigrateVerbConstants.Push, Input(), Mover.Options(handler));

        run.Code.Should().Be(MigrateExitCodeConstants.Refused);
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task RunAsync_ShouldRetryA503ThreeTimes_ThenCountItFailed_AndExitTwo()
    {
        var handler = new FakeFilesHandler(request => request.Method == HttpMethod.Get ? FakeFilesHandler.Map() : FakeFilesHandler.Json(HttpStatusCode.ServiceUnavailable, "{}"));

        var run = await Mover.RunAsync(MigrateVerbConstants.Push, Input(), Mover.Options(handler));

        run.Code.Should().Be(MigrateExitCodeConstants.Failed);
        run.Count("failed").Should().Be(5);
        handler.Uploads.Should().HaveCount(5 * 4);
        handler.Uploads.Take(4).Should().OnlyContain(upload => upload.Field("key") == "a.bin" && upload.File!.SequenceEqual(files["a.bin"]));
    }

    [Fact]
    public async Task RunAsync_ShouldStoreTheFile_WhenARetryGetsThrough()
    {
        var failures = 0;
        var handler = new FakeFilesHandler(request => request.Method == HttpMethod.Get
            ? FakeFilesHandler.Map()
            : request.Field("key") == "a.bin" && failures++ < 2 ? FakeFilesHandler.Json(HttpStatusCode.BadGateway, "{}") : FakeFilesHandler.Stored(request));

        var run = await Mover.RunAsync(MigrateVerbConstants.Push, Input(), Mover.Options(handler));

        run.Code.Should().Be(MigrateExitCodeConstants.Done);
        run.Lines[^1].Should().Be("""{"result":{"files":5,"bytes":27,"skipped":0,"failed":0}}""");
        handler.Uploads.Should().HaveCount(7);
    }

    [Fact]
    public async Task RunAsync_ShouldExitTwo_WhenTheMapStaysUnavailable()
    {
        var handler = new FakeFilesHandler(_ => throw new HttpRequestException("connection refused"));

        var run = await Mover.RunAsync(MigrateVerbConstants.Push, Input(), Mover.Options(handler));

        run.Code.Should().Be(MigrateExitCodeConstants.Failed);
        handler.Requests.Should().HaveCount(4);
    }

    [Fact]
    public async Task RunAsync_ShouldSpillALargeRowToATempFile_AndUploadItWhole()
    {
        var database = Path.Combine(root.Path, "c.db");
        var big = Enumerable.Range(0, 300_000).Select(i => (byte)(i % 251)).ToArray();
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={database};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE assets (Sha256 TEXT, Bytes BLOB, ContentType TEXT); INSERT INTO assets VALUES ('k1', $big, 'application/pdf'), ('k2', x'0102', NULL);";
            command.Parameters.AddWithValue("$big", big);
            command.ExecuteNonQuery();
        }

        using var spill = new TempFolder();
        var handler = new FakeFilesHandler(request => request.Method == HttpMethod.Get ? FakeFilesHandler.Map() : FakeFilesHandler.Stored(request));
        var source = new JsonObject
        {
            ["id"] = "catalogue-release-assets", ["kind"] = "sqlite-column", ["database"] = "c.db", ["table"] = "assets",
            ["key"] = "Sha256", ["content"] = "Bytes", ["type"] = "ContentType", ["reference"] = "catalogue.release:{Sha256}",
        };

        var run = await Mover.RunAsync(MigrateVerbConstants.Push, Mover.Input(source, root: root.Path), Mover.Options(handler, memoryLimit: 1024, tempDirectory: spill.Path));

        run.Code.Should().Be(MigrateExitCodeConstants.Done);
        var uploads = handler.Uploads.ToList();
        uploads[0].File.Should().Equal(big);
        uploads[0].Field("sha256").Should().Be(Mover.Sha256(big));
        uploads[0].Field("contentType").Should().Be("application/pdf");
        uploads[0].FileContentType.Should().Be("application/pdf");
        uploads[0].Field("reference").Should().Be("catalogue.release:k1");
        uploads[1].Field("contentType").Should().BeNull();
        Directory.EnumerateFiles(spill.Path).Should().BeEmpty("a spilled row's file is deleted once uploaded");
    }

    [Fact]
    public async Task RunAsync_ShouldWriteOnlyProgressAndResultJsonLines()
    {
        var handler = new FakeFilesHandler(request => request.Method == HttpMethod.Get ? FakeFilesHandler.Map() : FakeFilesHandler.Stored(request));

        var run = await Mover.RunAsync(MigrateVerbConstants.Push, Input(), Mover.Options(handler, progress: TimeSpan.Zero));

        run.Lines.Should().HaveCount(6);
        run.Lines.SkipLast(1).Should().OnlyContain(line => Regex.IsMatch(line, """^\{"progress":\{"files":\d+,"bytes":\d+\}\}$"""));
        run.Lines[^1].Should().MatchRegex("""^\{"result":\{"files":\d+,"bytes":\d+,"skipped":\d+,"failed":\d+\}\}$""");
        run.Lines[0].Should().Be("""{"progress":{"files":1,"bytes":5}}""");
        foreach (var line in run.Lines)
            JsonDocument.Parse(line).RootElement.EnumerateObject().Should().ContainSingle();
    }

    public void Dispose() => root.Dispose();

    private string Input() => Mover.Input(Mover.VolumeSource(), root: root.Path);
}

public sealed class VerifyVerbTests : IDisposable
{
    private readonly TempFolder root = new();

    public VerifyVerbTests()
    {
        root.Write("blobs/media/a", [1]);
        root.Write("blobs/media/b", [2]);
        root.Write("blobs/media/c", [3]);
    }

    [Fact]
    public async Task RunAsync_ShouldFailMissingAndDifferentKeys_AndExitTwo()
    {
        var handler = new FakeFilesHandler(_ => FakeFilesHandler.Map(
            ("brand-media", "a", Mover.Sha256([1]).ToUpperInvariant(), 1),
            ("brand-media", "b", Mover.Sha256([9]), 1),
            ("brand-media", "z", Mover.Sha256([7]), 1)));

        var run = await Mover.RunAsync(MigrateVerbConstants.Verify, Mover.Input(Mover.VolumeSource(), root: root.Path), Mover.Options(handler));

        run.Code.Should().Be(MigrateExitCodeConstants.Failed);
        run.Lines.Should().Equal("""{"result":{"files":3,"bytes":3,"skipped":0,"failed":2}}""");
        run.Stderr.Should().Contain("failed b: different").And.Contain("failed c: missing").And.NotContain("failed a");
        handler.Uploads.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_ShouldExitZero_WhenEveryKeyMatches()
    {
        var handler = new FakeFilesHandler(_ => FakeFilesHandler.Map(
            ("brand-media", "a", Mover.Sha256([1]), 1),
            ("brand-media", "b", Mover.Sha256([2]), 1),
            ("brand-media", "c", Mover.Sha256([3]), 1)));

        var run = await Mover.RunAsync(MigrateVerbConstants.Verify, Mover.Input(Mover.VolumeSource(), root: root.Path), Mover.Options(handler));

        run.Code.Should().Be(MigrateExitCodeConstants.Done);
        run.Count("failed").Should().Be(0);
    }

    public void Dispose() => root.Dispose();
}

public sealed class MoverOutputTests
{
    [Fact]
    public void Progress_ShouldWriteAtMostOneLinePerInterval()
    {
        var time = new FakeTimeProvider();
        var stdout = new StringWriter();
        var output = new MoverOutput(stdout, new StringWriter(), time, TimeSpan.FromSeconds(2));
        var counters = new MoverCounters { Files = 1, Bytes = 10 };

        output.Progress(counters);
        time.Advance(TimeSpan.FromSeconds(2));
        output.Progress(counters);
        counters.Files = 2;
        output.Progress(counters);
        time.Advance(TimeSpan.FromSeconds(3));
        output.Progress(counters);

        stdout.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Should().Equal(
            """{"progress":{"files":1,"bytes":10}}""",
            """{"progress":{"files":2,"bytes":10}}""");
    }
}
