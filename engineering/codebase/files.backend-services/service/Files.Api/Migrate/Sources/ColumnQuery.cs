using System.Data.Common;
using System.Globalization;
using Files.Api.Migrate.Input;

namespace Files.Api.Migrate.Sources;

/// <summary>Builds a column source's SQL and maps its rows to items. The select lists the key columns, then the optional
/// <c>name</c> and <c>type</c>, and the content last, so a sequential reader streams the bytes after every other value.</summary>
/// <remarks>Every identifier is quoted (<c>"</c> doubled); a <c>schema.table</c> name is quoted part by part. Rows whose
/// content is <c>NULL</c> hold no file and are left out.</remarks>
public sealed class ColumnQuery
{
    private ColumnQuery(IReadOnlyList<string> key, string selectSql, string countSql, int nameOrdinal, int typeOrdinal, int contentOrdinal)
    {
        Key = key;
        SelectSql = selectSql;
        CountSql = countSql;
        NameOrdinal = nameOrdinal;
        TypeOrdinal = typeOrdinal;
        ContentOrdinal = contentOrdinal;
    }

    /// <summary>Gets the key columns, in order.</summary>
    public IReadOnlyList<string> Key { get; }

    /// <summary>Gets the statement that reads every row in key order.</summary>
    public string SelectSql { get; }

    /// <summary>Gets the statement that counts the rows and sums their content's bytes.</summary>
    public string CountSql { get; }

    /// <summary>Gets the ordinal of the <c>name</c> column, or <c>-1</c>.</summary>
    public int NameOrdinal { get; }

    /// <summary>Gets the ordinal of the <c>type</c> column, or <c>-1</c>.</summary>
    public int TypeOrdinal { get; }

    /// <summary>Gets the ordinal of the content column, always the last.</summary>
    public int ContentOrdinal { get; }

    /// <summary>Builds the query of a <c>sqlite-column</c> source.</summary>
    /// <param name="source">The parsed source.</param>
    public static ColumnQuery ForSqlite(MigrateSource source) => For(source, "length");

    /// <summary>Builds the query of a <c>postgres-column</c> source.</summary>
    /// <param name="source">The parsed source.</param>
    public static ColumnQuery ForPostgres(MigrateSource source) => For(source, "octet_length");

    /// <summary>Quotes one identifier.</summary>
    /// <param name="identifier">The column or table part.</param>
    public static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    /// <summary>Quotes a table name, part by part across its <c>.</c>.</summary>
    /// <param name="table">The table, optionally schema-qualified.</param>
    public static string QuoteTable(string table) => string.Join('.', table.Split('.').Select(Quote));

    /// <summary>Maps the reader's current row to an item, reading its values in ordinal order.</summary>
    /// <param name="reader">The reader on a row of <see cref="SelectSql"/>.</param>
    public SourceItem Map(DbDataReader reader)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var parts = new string[Key.Count];
        for (var ordinal = 0; ordinal < Key.Count; ordinal++)
        {
            parts[ordinal] = Format(reader.GetValue(ordinal)) ?? string.Empty;
            values[Key[ordinal]] = parts[ordinal];
        }

        var name = NameOrdinal < 0 ? null : Format(reader.GetValue(NameOrdinal));
        var type = TypeOrdinal < 0 ? null : Format(reader.GetValue(TypeOrdinal));
        var content = ContentOrdinal;
        return new SourceItem
        {
            Key = string.Join('/', parts),
            Values = values,
            Name = string.IsNullOrWhiteSpace(name) ? null : name,
            ContentType = string.IsNullOrWhiteSpace(type) ? null : type,
            OpenContent = () => reader.GetStream(content),
        };
    }

    /// <summary>Formats a column value as placeholder text: a <see cref="Guid"/> in its <c>D</c> form, bytes as lowercase
    /// hex, everything else in the invariant culture; <c>NULL</c> as <see langword="null"/>.</summary>
    /// <param name="value">The value the reader returned.</param>
    public static string? Format(object? value) => value switch
    {
        null or DBNull => null,
        string text => text,
        Guid guid => guid.ToString("D"),
        byte[] bytes => Convert.ToHexStringLower(bytes),
        bool flag => flag ? "true" : "false",
        DateTime time => time.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset time => time.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    private static ColumnQuery For(MigrateSource source, string lengthFunction)
    {
        var columns = source.Key.Select(Quote).ToList();
        var keyList = string.Join(", ", columns);
        var nameOrdinal = -1;
        var typeOrdinal = -1;
        if (source.Name is { } name)
        {
            nameOrdinal = columns.Count;
            columns.Add(Quote(name));
        }
        if (source.Type is { } type)
        {
            typeOrdinal = columns.Count;
            columns.Add(Quote(type));
        }

        var content = Quote(source.Content!);
        var contentOrdinal = columns.Count;
        columns.Add(content);
        var table = QuoteTable(source.Table!);

        return new ColumnQuery(
            source.Key,
            $"SELECT {string.Join(", ", columns)} FROM {table} WHERE {content} IS NOT NULL ORDER BY {keyList}",
            $"SELECT count(*), coalesce(sum({lengthFunction}({content})), 0) FROM {table} WHERE {content} IS NOT NULL",
            nameOrdinal,
            typeOrdinal,
            contentOrdinal);
    }
}
