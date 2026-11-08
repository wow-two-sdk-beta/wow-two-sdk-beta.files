using Files.Api.Migrate.Input;

namespace Files.Api.Migrate.Sources;

/// <summary>Resolves a manifest path below the run's root, refusing one that escapes it.</summary>
public static class SourcePaths
{
    /// <summary>Resolves <paramref name="relative"/> below <paramref name="root"/>.</summary>
    /// <param name="root">The run's <c>root</c>.</param>
    /// <param name="relative">The manifest's <c>path</c> or <c>database</c>.</param>
    /// <param name="field">The field the path came from, named in the refusal.</param>
    /// <exception cref="MigrateInputException">The path leaves the root.</exception>
    public static string Resolve(string root, string relative, string field)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(fullRoot, relative)));
        var inside = full == fullRoot || full.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        return inside ? full : throw new MigrateInputException($"The field '{field}' leaves the root.");
    }
}
