namespace WoW.Two.Sdk.Beta.Be.Files.Formats.Types;

/// <summary>Represents a file the type checks accepted: the name to store it under and the type it was recognized as.</summary>
/// <param name="Name">The safe name, with the type's extension added when the given name had none.</param>
/// <param name="Type">The recognized type; its content type is the one to store and serve.</param>
public sealed record RecognizedFileModel(string Name, FileTypeModel Type);
