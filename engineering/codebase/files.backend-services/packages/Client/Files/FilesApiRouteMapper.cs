using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using WoW.Two.Sdk.Beta.Be.Files.Client.Transport;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Client.Files;

/// <summary>Maps broker calls to the service's relative routes under <see cref="FilesRouteConstants.Api"/>.</summary>
internal static class FilesApiRouteMapper
{
    /// <summary>Maps to the files resource, with a query.</summary>
    public static string Files(IEnumerable<KeyValuePair<string, string?>>? query = null) =>
        query is null ? FilesRouteConstants.Files : QueryHelpers.AddQueryString(FilesRouteConstants.Files, query);

    /// <summary>Maps to one file, or a part of it.</summary>
    public static string File(FileId id, string? part = null) =>
        part is null ? $"{FilesRouteConstants.Files}/{id}" : $"{FilesRouteConstants.Files}/{id}/{part}";

    /// <summary>Maps to one reference of a file.</summary>
    public static string Reference(FileId id, FileReference reference) =>
        $"{File(id, "references")}/{Uri.EscapeDataString(reference.RecordType)}/{Uri.EscapeDataString(reference.RecordId)}";

    /// <summary>Maps to the audit trail, with a query.</summary>
    public static string Audit(IEnumerable<KeyValuePair<string, string?>> query) =>
        QueryHelpers.AddQueryString($"{FilesRouteConstants.Api}/audit", query);

    /// <summary>Maps an enum to its wire name, as the service reads it from a query.</summary>
    public static string WireName<TEnum>(TEnum value) where TEnum : struct, Enum =>
        JsonSerializer.Serialize(value, FilesApiTransport.Json).Trim('"');
}
