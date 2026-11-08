using System.Text.Json;

namespace Files.Api.Migrate.Input;

/// <summary>Reads the mover's one JSON document and checks every field its verb and source kind need.</summary>
/// <remarks>Messages name fields only, never their values: the document carries the import token and a connection
/// string.</remarks>
public static class MigrateRequestParser
{
    /// <summary>Parses standard input for a verb.</summary>
    /// <param name="json">The whole of standard input.</param>
    /// <param name="verb">The verb the run takes, one of <see cref="MigrateVerbConstants"/>.</param>
    /// <exception cref="MigrateInputException">The input is no JSON object, a field is missing or of the wrong type, or the
    /// source kind is unknown.</exception>
    public static MigrateRequest Parse(string json, string verb)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new MigrateInputException($"Standard input is not one JSON document (line {(exception.LineNumber ?? 0) + 1}).");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new MigrateInputException("Standard input must be one JSON object.");
            if (!root.TryGetProperty("source", out var sourceElement) || sourceElement.ValueKind != JsonValueKind.Object)
                throw new MigrateInputException("The input has no 'source' object.");

            var source = ParseSource(sourceElement);
            var remote = verb is MigrateVerbConstants.Push or MigrateVerbConstants.Verify;
            var request = new MigrateRequest
            {
                Files = remote ? ParseFiles(RequiredString(root, "files")) : null,
                Import = remote ? RequiredString(root, "import") : OptionalString(root, "import"),
                Token = remote ? RequiredString(root, "token") : null,
                Source = source,
                Connection = source.Kind == MigrateSourceKindConstants.PostgresColumn ? RequiredString(root, "connection") : null,
                Root = source.Kind == MigrateSourceKindConstants.PostgresColumn ? OptionalString(root, "root") : RequiredString(root, "root"),
            };
            return request;
        }
    }

    private static MigrateSource ParseSource(JsonElement source)
    {
        var id = RequiredString(source, "id", "source.");
        var kind = RequiredString(source, "kind", "source.");
        var reference = OptionalString(source, "reference", "source.");
        switch (kind)
        {
            case MigrateSourceKindConstants.VolumeFolder:
                return new MigrateSource
                {
                    Id = id,
                    Kind = kind,
                    Path = OptionalString(source, "path", "source.") ?? throw Missing("source.path"),
                    Reference = reference is null ? null : ReferenceTemplate.Parse(reference, ["path"]),
                };
            case MigrateSourceKindConstants.SqliteColumn:
            case MigrateSourceKindConstants.PostgresColumn:
                var key = ParseKey(source);
                return new MigrateSource
                {
                    Id = id,
                    Kind = kind,
                    Database = kind == MigrateSourceKindConstants.SqliteColumn ? RequiredString(source, "database", "source.") : OptionalString(source, "database", "source."),
                    Table = RequiredString(source, "table", "source."),
                    Key = key,
                    Content = RequiredString(source, "content", "source."),
                    Name = OptionalString(source, "name", "source."),
                    Type = OptionalString(source, "type", "source."),
                    Reference = reference is null ? null : ReferenceTemplate.Parse(reference, key.ToArray()),
                };
            default:
                throw new MigrateInputException($"The source kind '{kind}' is unknown.");
        }
    }

    private static string[] ParseKey(JsonElement source)
    {
        if (!source.TryGetProperty("key", out var key) || key.ValueKind == JsonValueKind.Null)
            throw Missing("source.key");

        string[] columns = key.ValueKind switch
        {
            JsonValueKind.String => [key.GetString()!],
            JsonValueKind.Array => key.EnumerateArray()
                .Select(column => column.ValueKind == JsonValueKind.String ? column.GetString()! : throw WrongType("source.key"))
                .ToArray(),
            _ => throw WrongType("source.key"),
        };
        if (columns.Length == 0 || columns.Any(string.IsNullOrWhiteSpace))
            throw new MigrateInputException("The field 'source.key' must name at least one column, none empty.");
        if (columns.Distinct(StringComparer.Ordinal).Count() != columns.Length)
            throw new MigrateInputException("The field 'source.key' names a column twice.");

        return columns;
    }

    private static Uri ParseFiles(string files)
    {
        if (!Uri.TryCreate(files.EndsWith('/') ? files : files + "/", UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new MigrateInputException("The field 'files' must be an absolute http or https address.");

        return uri;
    }

    private static string RequiredString(JsonElement element, string name, string prefix = "") =>
        OptionalString(element, name, prefix) is { } value && !string.IsNullOrWhiteSpace(value) ? value : throw Missing(prefix + name);

    private static string? OptionalString(JsonElement element, string name, string prefix = "")
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;

        return value.ValueKind == JsonValueKind.String ? value.GetString() : throw WrongType(prefix + name);
    }

    private static MigrateInputException Missing(string field) => new($"The input has no '{field}'.");

    private static MigrateInputException WrongType(string field) => new($"The field '{field}' has the wrong type.");
}
