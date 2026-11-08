namespace Files.Api.Auth;

/// <summary>Holds the operator's credential: the one key that runs tenants on this host, such as Wheelhouse opening an
/// import. It reaches the operator routes only, never a file route.</summary>
/// <remarks>Bound from <c>Files:Operator</c> and read on use.</remarks>
public sealed class FilesOperatorSettings
{
    /// <summary>Holds the section the settings bind from.</summary>
    public const string SectionName = "Files:Operator";

    /// <summary>Gets or sets the operator key's random part, 32 hex characters (<c>generate: { random: 16, encoding: hex
    /// }</c>); presented as <c>files_{value}</c>, the marker prepended when the value lacks it. Empty turns the operator
    /// routes off.</summary>
    public string? ApiKey { get; set; }
}
