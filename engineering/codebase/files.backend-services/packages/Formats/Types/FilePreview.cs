namespace WoW.Two.Sdk.Beta.Be.Files.Formats.Types;

/// <summary>Refers to the previewer that shows a file of a type.</summary>
public enum FilePreview
{
    /// <summary>No previewer; the icon stands in.</summary>
    None = 0,

    /// <summary>A browser image.</summary>
    Image,

    /// <summary>A browser video player.</summary>
    Video,

    /// <summary>A browser audio player.</summary>
    Audio,

    /// <summary>A PDF renderer.</summary>
    Pdf,

    /// <summary>A text or code view.</summary>
    Text,
}
