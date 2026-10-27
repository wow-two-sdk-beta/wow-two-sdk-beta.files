using System.Text.Json;
using System.Text.Json.Serialization;

namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Serializes a <see cref="FileId"/> as its canonical string, the form every wire and URL carries.</summary>
public sealed class FileIdJsonConverter : JsonConverter<FileId>
{
    /// <inheritdoc />
    public override FileId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        FileId.TryParse(reader.GetString(), out var id) ? id : throw new JsonException("A file id must be a UUID string.");

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, FileId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
