using WoW.Two.Sdk.Beta.Be.Foundation.Errors;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Holds the errors of imports.</summary>
public static class FileImportErrors
{
    /// <summary>Creates the error for an import id no import has.</summary>
    public static AppError ImportNotFound() => AppErrorFactory.NotFound("The import was not found.");

    /// <summary>Creates the error for a tenant that is not a safe storage key segment.</summary>
    public static AppError TenantInvalid() =>
        AppErrorFactory.Validation($"The tenant must match {FilesConstants.TenantIdPattern}.");

    /// <summary>Creates the error for a source list that is empty, too long, repeats an id or holds a malformed one.</summary>
    public static AppError SourcesInvalid() =>
        AppErrorFactory.Validation($"The sources must list 1 to {FileImportConstants.MaxSources} distinct ids matching {FileImportConstants.SourceIdPattern}.");

    /// <summary>Creates the error for a push naming a source the import does not list.</summary>
    public static AppError SourceUnknown() => AppErrorFactory.Validation("The source is not one of the import's sources.");

    /// <summary>Creates the error for a key that is blank or longer than <see cref="FileImportConstants.MaxKeyLength"/>.</summary>
    public static AppError KeyInvalid() =>
        AppErrorFactory.Validation($"The key must be 1 to {FileImportConstants.MaxKeyLength} characters.");

    /// <summary>Creates the error for a <c>sha256</c> field that is not 64 lowercase hex characters.</summary>
    public static AppError Sha256Invalid() => AppErrorFactory.Validation("The sha256 must be 64 lowercase hex characters.");

    /// <summary>Creates the error for a reference that is not <c>{recordType}:{recordId}</c>.</summary>
    public static AppError ReferenceInvalid() =>
        AppErrorFactory.Validation($"The reference must be {{recordType}}:{{recordId}}, each 1 to {FilesConstants.MaxReferenceLength} characters.");

    /// <summary>Creates the error for bytes whose SHA-256 differs from the <c>sha256</c> field.</summary>
    public static AppError Sha256Mismatch() => AppErrorFactory.BusinessRule("The bytes' SHA-256 does not match the sha256 field.");

    /// <summary>Creates the error for a key already moved with other bytes.</summary>
    public static AppError KeyConflict() => AppErrorFactory.Conflict("The key was already moved with another SHA-256.");

    /// <summary>Creates the error for a map cursor no previous page returned.</summary>
    public static AppError CursorInvalid() => AppErrorFactory.Validation("The map cursor is not valid.");
}
