namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Represents an import token.</summary>
/// <param name="Token">The token, <c>fimp_{payload}.{mac}</c>.</param>
/// <param name="ExpiresAt">When it stops working.</param>
public sealed record FileImportTokenModel(string Token, DateTimeOffset ExpiresAt);
