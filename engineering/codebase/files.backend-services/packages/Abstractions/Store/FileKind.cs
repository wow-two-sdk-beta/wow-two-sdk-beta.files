using System.Text.Json.Serialization;

namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Classifies a file for its icon, its preview and its policy group; the file-type registry assigns it.</summary>
/// <remarks>The wire and the registry name a kind in kebab case: <c>raw-photo</c>, <c>disk-image</c>, <c>model-3d</c>.</remarks>
public enum FileKind
{
    /// <summary>A type the registry does not know.</summary>
    Generic = 0,

    /// <summary>A raster image: JPEG, PNG, GIF, WebP, AVIF, HEIC.</summary>
    Image,

    /// <summary>A camera RAW photo.</summary>
    [JsonStringEnumMemberName("raw-photo")]
    RawPhoto,

    /// <summary>A vector image: SVG, EPS.</summary>
    Vector,

    /// <summary>A video.</summary>
    Video,

    /// <summary>An audio file.</summary>
    Audio,

    /// <summary>A PDF document.</summary>
    Pdf,

    /// <summary>A word-processing document.</summary>
    Document,

    /// <summary>A spreadsheet.</summary>
    Spreadsheet,

    /// <summary>A slide deck.</summary>
    Presentation,

    /// <summary>Plain text or a log.</summary>
    Text,

    /// <summary>A Markdown document.</summary>
    Markdown,

    /// <summary>An e-book.</summary>
    Ebook,

    /// <summary>Source code or a project file.</summary>
    Code,

    /// <summary>Structured data: JSON, XML, YAML, CSV.</summary>
    Data,

    /// <summary>A database file.</summary>
    Database,

    /// <summary>A configuration file.</summary>
    Config,

    /// <summary>An archive: ZIP, TAR, 7z, RAR.</summary>
    Archive,

    /// <summary>A disk image or virtual disk.</summary>
    [JsonStringEnumMemberName("disk-image")]
    DiskImage,

    /// <summary>An executable or installer.</summary>
    Executable,

    /// <summary>A font.</summary>
    Font,

    /// <summary>A 3D model: GLB, glTF, OBJ, STL.</summary>
    [JsonStringEnumMemberName("model-3d")]
    Model3D,

    /// <summary>A CAD or GIS drawing.</summary>
    Cad,

    /// <summary>A design-tool file.</summary>
    Design,

    /// <summary>A calendar or contact card.</summary>
    Calendar,

    /// <summary>A certificate or key.</summary>
    Certificate,

    /// <summary>An email message.</summary>
    Email,
}
