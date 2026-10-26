namespace WoW.Two.Sdk.Beta.Be.Files.References;

/// <summary>Names a record that uses a file; a file lives while any reference does.</summary>
/// <param name="RecordType">The record's type, product-qualified: <c>catalogue.product</c>.</param>
/// <param name="RecordId">The record's id.</param>
public readonly record struct FileReference(string RecordType, string RecordId);
