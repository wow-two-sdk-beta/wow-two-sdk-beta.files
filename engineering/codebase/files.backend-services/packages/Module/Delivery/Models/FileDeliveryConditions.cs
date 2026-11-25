namespace WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Models;

internal sealed record FileDeliveryConditions(IReadOnlyList<string?> IfNoneMatch, string? Range);
