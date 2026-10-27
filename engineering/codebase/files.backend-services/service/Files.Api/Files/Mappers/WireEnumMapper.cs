using System.Text.Json;
using WoW.Two.Sdk.Beta.Be.Foundation.Serialization;

namespace Files.Api.Files.Mappers;

/// <summary>Maps an enum's wire name in a query (<c>raw-photo</c>, <c>ready</c>) to its member, as the body JSON reads it.</summary>
internal static class WireEnumMapper
{
    /// <summary>Maps the name, or reports it unknown.</summary>
    public static bool TryParse<TEnum>(string? value, out TEnum? parsed) where TEnum : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        try
        {
            parsed = JsonSerializer.Deserialize<TEnum>(JsonSerializer.Serialize(value), JsonOptionsConstants.Default);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
