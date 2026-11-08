using Files.Api.Migrate.Input;

namespace Files.Api.Migrate.Sources;

/// <summary>Picks the reader of a request's source kind.</summary>
public static class SourceReaderFactory
{
    /// <summary>Creates the reader of the request's source.</summary>
    /// <param name="request">The parsed request.</param>
    /// <exception cref="MigrateInputException">The kind is unknown, or its folder, file or connection is unusable.</exception>
    public static ISourceReader Create(MigrateRequest request) => request.Source.Kind switch
    {
        MigrateSourceKindConstants.VolumeFolder => VolumeFolderSourceReader.For(request),
        MigrateSourceKindConstants.SqliteColumn => SqliteColumnSourceReader.For(request),
        MigrateSourceKindConstants.PostgresColumn => PostgresColumnSourceReader.For(request),
        _ => throw new MigrateInputException($"The source kind '{request.Source.Kind}' is unknown."),
    };
}
