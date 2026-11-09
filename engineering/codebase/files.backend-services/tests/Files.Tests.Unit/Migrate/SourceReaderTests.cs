using System.Data;
using System.Text;
using AwesomeAssertions;
using Files.Api.Migrate.Input;
using Files.Api.Migrate.Sources;
using Microsoft.Data.Sqlite;

namespace Files.Tests.Unit.Migrate;

public sealed class VolumeFolderSourceReaderTests : IDisposable
{
    private readonly TempFolder root = new();

    [Fact]
    public async Task ReadAsync_ShouldKeyEachFileByItsSlashPathBelowTheFolder_InOrdinalOrder()
    {
        var second = Encoding.UTF8.GetBytes("second");
        var first = Encoding.UTF8.GetBytes("first file");
        root.Write("blobs/media/cd/cdef", second);
        root.Write("blobs/media/ab/abcd", first);
        root.Write("blobs/media/ab/.abcd.tmp", [1, 2, 3]);
        root.Write("blobs/previews/ab/abcd", [9]);

        var reader = VolumeFolderSourceReader.For(Request("blobs/media"));
        var items = await ToListAsync(reader.ReadAsync(CancellationToken.None));

        items.Select(item => item.Key).Should().Equal("ab/abcd", "cd/cdef");
        items[0].Values.Should().BeEquivalentTo(new Dictionary<string, string> { ["path"] = "ab/abcd" });
        items[0].Size.Should().Be(first.Length);
        items[0].FilePath.Should().EndWith("ab/abcd");
        items[0].Name.Should().BeNull();
        items[0].ContentType.Should().BeNull();
        await using var content = items[0].OpenContent();
        new StreamReader(content).ReadToEnd().Should().Be("first file");
    }

    [Fact]
    public async Task CountAsync_ShouldCountTheFilesAndSumTheirBytes()
    {
        root.Write("blobs/media/ab/a", new byte[10]);
        root.Write("blobs/media/cd/c", new byte[32]);

        var totals = await VolumeFolderSourceReader.For(Request("blobs/media")).CountAsync(CancellationToken.None);

        totals.Should().Be(new SourceTotals(2, 42));
    }

    [Fact]
    public async Task For_ShouldReadAMissingFolderAsAnEmptySource()
    {
        var reader = VolumeFolderSourceReader.For(Request("blobs/missing"));

        reader.Exists.Should().BeFalse();
        (await reader.CountAsync(CancellationToken.None)).Should().Be(new SourceTotals(0, 0));
        (await ToListAsync(reader.ReadAsync(CancellationToken.None))).Should().BeEmpty();
    }

    [Fact]
    public void For_ShouldRefuseTheSource_WhenThePathLeavesTheRoot()
    {
        var act = () => VolumeFolderSourceReader.For(Request("../outside"));

        act.Should().Throw<MigrateInputException>();
    }

    [Fact]
    public void For_ShouldRefuseTheSource_WhenThePathNamesAFile()
    {
        root.Write("blobs/media", [1]);

        var act = () => VolumeFolderSourceReader.For(Request("blobs/media"));

        act.Should().Throw<MigrateInputException>().WithMessage("*is a file*");
    }

    [Fact]
    public void For_ShouldRefuseTheSource_WhenTheRootIsMissing()
    {
        var request = Request("blobs/media") with { Root = Path.Combine(root.Path, "not-mounted") };

        var act = () => VolumeFolderSourceReader.For(request);

        act.Should().Throw<MigrateInputException>().WithMessage("The root does not exist.");
    }

    [Fact]
    public void Render_ShouldFillPathAndSha256()
    {
        var template = ReferenceTemplate.Parse("brand.media:{path}@{sha256}", ["path"]);

        template.Render(new Dictionary<string, string> { ["path"] = "ab/abcd" }, "ff00").Should().Be("brand.media:ab/abcd@ff00");
    }

    public void Dispose() => root.Dispose();

    private MigrateRequest Request(string path) => new()
    {
        Root = root.Path,
        Source = new MigrateSource { Id = "brand-media", Kind = MigrateSourceKindConstants.VolumeFolder, Path = path },
    };

    internal static async Task<List<SourceItem>> ToListAsync(IAsyncEnumerable<SourceItem> items)
    {
        var list = new List<SourceItem>();
        await foreach (var item in items)
            list.Add(item);
        return list;
    }
}

public sealed class SqliteColumnSourceReaderTests : IDisposable
{
    private readonly TempFolder root = new();

    [Fact]
    public async Task ReadAsync_ShouldKeyRowsByTheKeyColumn_AndMapTheirType_LeavingNullContentOut()
    {
        Database("ocharo-catalogue.db",
            "CREATE TABLE catalogue_release_assets (Sha256 TEXT, Bytes BLOB, ContentType TEXT)",
            ("INSERT INTO catalogue_release_assets VALUES ('bb', $bytes, 'image/png')", [0x89, 0x50]),
            ("INSERT INTO catalogue_release_assets VALUES ('aa', $bytes, 'application/pdf')", [0x25, 0x50, 0x44]),
            ("INSERT INTO catalogue_release_assets VALUES ('cc', NULL, 'image/png')", null));
        var source = Source(key: ["Sha256"], content: "Bytes", type: "ContentType", reference: "catalogue.release:{Sha256}");

        var reader = SqliteColumnSourceReader.For(Request(source));
        var keys = new List<string>();
        var types = new List<string?>();
        var bytes = new List<byte[]>();
        await foreach (var item in reader.ReadAsync(CancellationToken.None))
        {
            keys.Add(item.Key);
            types.Add(item.ContentType);
            await using var content = item.OpenContent();
            using var copy = new MemoryStream();
            await content.CopyToAsync(copy);
            bytes.Add(copy.ToArray());
            source.Reference!.Render(item.Values, "hash").Should().Be($"catalogue.release:{item.Key}");
        }

        keys.Should().Equal("aa", "bb");
        types.Should().Equal("application/pdf", "image/png");
        bytes[0].Should().Equal(0x25, 0x50, 0x44);
        (await reader.CountAsync(CancellationToken.None)).Should().Be(new SourceTotals(2, 5));
    }

    [Fact]
    public async Task ReadAsync_ShouldJoinACompositeKeyWithSlash_AndReadTheName()
    {
        Database("desk.db",
            "CREATE TABLE media (organization_id TEXT, id INTEGER, file_name TEXT, bytes BLOB)",
            ("INSERT INTO media VALUES ('org-1', 2, 'b.jpg', $bytes)", [2]),
            ("INSERT INTO media VALUES ('org-1', 1, NULL, $bytes)", [1]));
        var source = Source(key: ["organization_id", "id"], content: "bytes", name: "file_name", reference: "desk.media:{organization_id}/{id}", database: "desk.db", table: "media");

        var items = await VolumeFolderSourceReaderTests.ToListAsync(SqliteColumnSourceReader.For(Request(source)).ReadAsync(CancellationToken.None));

        items.Select(item => item.Key).Should().Equal("org-1/1", "org-1/2");
        items.Select(item => item.Name).Should().Equal(null, "b.jpg");
        items[1].Values.Should().BeEquivalentTo(new Dictionary<string, string> { ["organization_id"] = "org-1", ["id"] = "2" });
        source.Reference!.Render(items[1].Values, "hash").Should().Be("desk.media:org-1/2");
    }

    [Fact]
    public async Task ReadAsync_ShouldReadAClosedWalDatabase_InAReadOnlyFolder_WithoutCreatingItsSideFiles()
    {
        Database("data/ocharo-catalogue.db",
            "PRAGMA journal_mode=WAL; CREATE TABLE catalogue_release_assets (Sha256 TEXT NOT NULL PRIMARY KEY, ContentType TEXT NOT NULL, Bytes BLOB NOT NULL)",
            ("INSERT INTO catalogue_release_assets VALUES ('aa', 'application/pdf', $bytes)", [0x25, 0x50]),
            ("INSERT INTO catalogue_release_assets VALUES ('bb', 'image/png', $bytes)", [0x89]));
        var folder = Path.Combine(root.Path, "data");
        var database = Path.Combine(folder, "ocharo-catalogue.db");
        File.Exists(database + "-wal").Should().BeFalse("the last connection's close checkpoints and removes the WAL");
        var source = Source(key: ["Sha256"], content: "Bytes", type: "ContentType", database: "data/ocharo-catalogue.db");
        ReadOnly(folder, true);
        try
        {
            var reader = SqliteColumnSourceReader.For(Request(source));
            var items = await VolumeFolderSourceReaderTests.ToListAsync(reader.ReadAsync(CancellationToken.None));

            SqliteColumnSourceReader.IsClosedWal(database).Should().BeTrue();
            items.Select(item => item.Key).Should().Equal("aa", "bb");
            (await reader.CountAsync(CancellationToken.None)).Should().Be(new SourceTotals(2, 3));
            Directory.GetFiles(folder).Select(Path.GetFileName).Should().Equal("ocharo-catalogue.db");
        }
        finally
        {
            ReadOnly(folder, false);
        }
    }

    [Fact]
    public void IsClosedWal_ShouldBeFalse_ForARollbackJournalDatabase_OrALiveWal()
    {
        Database("plain.db", "CREATE TABLE a (k TEXT)");
        Database("live.db", "PRAGMA journal_mode=WAL; CREATE TABLE a (k TEXT)");
        File.WriteAllBytes(Path.Combine(root.Path, "live.db-wal"), []);

        SqliteColumnSourceReader.IsClosedWal(Path.Combine(root.Path, "plain.db")).Should().BeFalse();
        SqliteColumnSourceReader.IsClosedWal(Path.Combine(root.Path, "live.db")).Should().BeFalse();
    }

    [Fact]
    public void For_ShouldRefuseTheSource_WhenTheDatabaseIsMissing()
    {
        var act = () => SqliteColumnSourceReader.For(Request(Source(key: ["Sha256"], content: "Bytes")));

        act.Should().Throw<MigrateInputException>();
        File.Exists(Path.Combine(root.Path, "ocharo-catalogue.db")).Should().BeFalse("a read-only open never creates the file");
    }

    public void Dispose() => root.Dispose();

    private static MigrateSource Source(string[] key, string content, string? type = null, string? name = null, string? reference = null, string database = "ocharo-catalogue.db", string table = "catalogue_release_assets") => new()
    {
        Id = "catalogue-release-assets",
        Kind = MigrateSourceKindConstants.SqliteColumn,
        Database = database,
        Table = table,
        Key = key,
        Content = content,
        Type = type,
        Name = name,
        Reference = reference is null ? null : ReferenceTemplate.Parse(reference, key),
    };

    private MigrateRequest Request(MigrateSource source) => new() { Root = root.Path, Source = source };

    private static void ReadOnly(string folder, bool readOnly)
    {
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(folder, readOnly ? UnixFileMode.UserRead | UnixFileMode.UserExecute : UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    private void Database(string file, string create, params (string Sql, byte[]? Bytes)[] rows)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root.Path, file))!);
        using var connection = new SqliteConnection($"Data Source={Path.Combine(root.Path, file)};Pooling=False");
        connection.Open();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = create;
            command.ExecuteNonQuery();
        }

        foreach (var (sql, bytes) in rows)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            if (bytes is not null)
                command.Parameters.AddWithValue("$bytes", bytes);
            command.ExecuteNonQuery();
        }
    }
}

public sealed class PostgresColumnQueryTests
{
    [Fact]
    public void ForPostgres_ShouldQuoteEveryIdentifier_AndReadTheContentLast()
    {
        var query = ColumnQuery.ForPostgres(DeskSource(["organization_id", "id"], name: "file_name", type: "content_type", table: "preparation.media"));

        query.SelectSql.Should().Be("""SELECT "organization_id", "id", "file_name", "content_type", "bytes" FROM "preparation"."media" WHERE "bytes" IS NOT NULL ORDER BY "organization_id", "id" """.TrimEnd());
        query.CountSql.Should().Be("""SELECT count(*), coalesce(sum(octet_length("bytes")), 0) FROM "preparation"."media" WHERE "bytes" IS NOT NULL""");
        query.ContentOrdinal.Should().Be(4);
    }

    [Fact]
    public void ForPostgres_ShouldSkipTheOptionalColumns_WhenTheSourceNamesNone()
    {
        var query = ColumnQuery.ForPostgres(DeskSource(["organization_id", "sha256"], table: "sourcing.media_contents"));

        query.SelectSql.Should().Be("""SELECT "organization_id", "sha256", "bytes" FROM "sourcing"."media_contents" WHERE "bytes" IS NOT NULL ORDER BY "organization_id", "sha256" """.TrimEnd());
        query.NameOrdinal.Should().Be(-1);
        query.TypeOrdinal.Should().Be(-1);
    }

    [Fact]
    public void Quote_ShouldDoubleAnEmbeddedQuote()
    {
        ColumnQuery.Quote("we\"ird").Should().Be("\"we\"\"ird\"");
        ColumnQuery.QuoteTable("a\".b").Should().Be("\"a\"\"\".\"b\"");
    }

    [Fact]
    public void Map_ShouldJoinGuidKeysInTheirDForm_AndRenderTheReference()
    {
        var organization = Guid.Parse("6F9619FF-8B86-D011-B42D-00C04FC964FF");
        var id = Guid.Parse("0E984725-C51C-4BF4-9960-E1C80E27ABA0");
        var source = DeskSource(["organization_id", "id"], name: "file_name", type: "content_type", reference: "desk.preparation-media:{organization_id}/{id}");
        var query = ColumnQuery.ForPostgres(source);
        using var reader = Rows(
            [("organization_id", typeof(Guid)), ("id", typeof(Guid)), ("file_name", typeof(string)), ("content_type", typeof(string)), ("bytes", typeof(byte[]))],
            [organization, id, "look.jpg", "image/jpeg", new byte[] { 7, 8 }]);

        reader.Read().Should().BeTrue();
        var item = query.Map(reader);

        item.Key.Should().Be("6f9619ff-8b86-d011-b42d-00c04fc964ff/0e984725-c51c-4bf4-9960-e1c80e27aba0");
        item.Name.Should().Be("look.jpg");
        item.ContentType.Should().Be("image/jpeg");
        source.Reference!.Render(item.Values, "hash").Should().Be($"desk.preparation-media:{organization:D}/{id:D}");
        using var content = item.OpenContent();
        new BinaryReader(content).ReadBytes(10).Should().Equal(7, 8);
    }

    [Fact]
    public void Map_ShouldPreferTheSha256KeyColumnOverTheHash_AndLeaveAnEmptyTypeToDetection()
    {
        var brand = Guid.NewGuid();
        var source = DeskSource(["brand_id", "sha256"], type: "content_type", reference: "desk.commerce-media:{brand_id}/{sha256}", table: "commerce.media");
        using var reader = Rows(
            [("brand_id", typeof(Guid)), ("sha256", typeof(string)), ("content_type", typeof(string)), ("bytes", typeof(byte[]))],
            [brand, "abc123", "", new byte[] { 1 }]);

        reader.Read().Should().BeTrue();
        var item = ColumnQuery.ForPostgres(source).Map(reader);

        item.ContentType.Should().BeNull();
        source.Reference!.Render(item.Values, "computed").Should().Be($"desk.commerce-media:{brand:D}/abc123");
    }

    [Fact]
    public void For_ShouldRefuseTheSource_WhenTheConnectionStringDoesNotParse()
    {
        var request = new MigrateRequest { Connection = "Host=db;Not A Keyword=1", Source = DeskSource(["id"]) };

        var act = () => PostgresColumnSourceReader.For(request);

        act.Should().Throw<MigrateInputException>().Which.Message.Should().NotContain("Host=db");
    }

    private static MigrateSource DeskSource(string[] key, string? name = null, string? type = null, string? reference = null, string table = "preparation.media") => new()
    {
        Id = "desk-preparation-media",
        Kind = MigrateSourceKindConstants.PostgresColumn,
        Table = table,
        Key = key,
        Content = "bytes",
        Name = name,
        Type = type,
        Reference = reference is null ? null : ReferenceTemplate.Parse(reference, key),
    };

    private static DataTableReader Rows((string Name, Type Type)[] columns, object[] row)
    {
        var table = new DataTable();
        foreach (var (name, type) in columns)
            table.Columns.Add(name, type);
        table.Rows.Add(row);
        return table.CreateDataReader();
    }
}
