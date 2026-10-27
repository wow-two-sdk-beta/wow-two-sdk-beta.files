using System.Text.Json.Serialization;

namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Represents a file's identity: a UUIDv7 the instance assigns, which survives every tenant move.</summary>
/// <param name="Value">The underlying UUID.</param>
[JsonConverter(typeof(FileIdJsonConverter))]
public readonly record struct FileId(Guid Value)
{
    /// <summary>Creates a new, time-ordered file id.</summary>
    public static FileId New() => new(Guid.CreateVersion7());

    /// <summary>Parses the canonical text form, as <see cref="ToString"/> writes it.</summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="id">The parsed id, when the text is one.</param>
    /// <returns>Whether the text held an id.</returns>
    public static bool TryParse(string? text, out FileId id)
    {
        var parsed = Guid.TryParse(text, out var value);
        id = new FileId(value);
        return parsed;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}
