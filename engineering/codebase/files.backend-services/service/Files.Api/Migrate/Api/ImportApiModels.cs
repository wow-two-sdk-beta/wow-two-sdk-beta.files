namespace Files.Api.Migrate.Api;

/// <summary>Represents one row of the import's id map, as far as the mover reads it.</summary>
/// <param name="Sha256">The lowercase hex SHA-256 of the stored bytes.</param>
/// <param name="Size">The stored size in bytes.</param>
public sealed record ImportMapEntry(string Sha256, long Size);

/// <summary>Represents the fields of one <c>POST /imports/{id}/files</c>, sent before the file part.</summary>
/// <param name="Source">The source id.</param>
/// <param name="Key">The source key.</param>
/// <param name="Sha256">The lowercase hex SHA-256 of the bytes.</param>
/// <param name="Reference">The rendered reference, or <see langword="null"/>.</param>
/// <param name="Name">The file's name, or <see langword="null"/> for the key's last segment.</param>
/// <param name="ContentType">The content type, or <see langword="null"/> for Files to detect.</param>
public sealed record ImportUpload(string Source, string Key, string Sha256, string? Reference, string? Name, string? ContentType);

/// <summary>Names how Files answered one upload.</summary>
public enum ImportUploadOutcome
{
    /// <summary><c>201</c>: the file was stored.</summary>
    Stored,

    /// <summary><c>200</c>: the key was already there with the same SHA-256.</summary>
    Skipped,

    /// <summary><c>409</c>, <c>422</c>, another client error, or a 5xx or network error after every retry.</summary>
    Failed,

    /// <summary><c>401</c>, <c>403</c> or <c>404</c>: the token or the import is refused; the run stops.</summary>
    Refused,
}

/// <summary>Represents Files' answer to one upload.</summary>
/// <param name="Outcome">How the upload ended.</param>
/// <param name="Reason">The status code, or <c>unreachable</c>; written to standard error on failure.</param>
public sealed record ImportUploadResult(ImportUploadOutcome Outcome, string Reason);

/// <summary>Signals that Files refused the token or does not know the import, so the run exits
/// <c>1</c> and is not retried.</summary>
/// <param name="message">The reason, naming a status code only.</param>
public sealed class MigrateRefusedException(string message) : Exception(message);

/// <summary>Signals that Files stayed unreachable or answered unreadably after every retry, so the run exits <c>2</c> and
/// a rerun resumes.</summary>
/// <param name="message">The reason, naming a status code only.</param>
public sealed class MigrateUnavailableException(string message) : Exception(message);
