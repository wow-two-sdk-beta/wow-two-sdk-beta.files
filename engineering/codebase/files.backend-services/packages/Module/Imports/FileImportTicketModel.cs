namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Represents an opened import and its first token.</summary>
/// <param name="Id">The import's id.</param>
/// <param name="Token">The import token the mover sends as <see cref="FileImportConstants.TokenHeader"/>.</param>
/// <param name="ExpiresAt">When the token stops working.</param>
public sealed record FileImportTicketModel(string Id, string Token, DateTimeOffset ExpiresAt);
