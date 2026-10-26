namespace WoW.Two.Sdk.Beta.Be.Files.Urls;

/// <summary>Decides whether a browser shows a served file or saves it.</summary>
public enum FileDisposition
{
    /// <summary>Show it in the browser; active types are still forced to download.</summary>
    Inline = 0,

    /// <summary>Save it under its name.</summary>
    Attachment,
}
