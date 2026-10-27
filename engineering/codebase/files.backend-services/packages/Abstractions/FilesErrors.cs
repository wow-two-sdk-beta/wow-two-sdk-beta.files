using WoW.Two.Sdk.Beta.Be.Foundation.Errors;

namespace WoW.Two.Sdk.Beta.Be.Files;

/// <summary>Holds the error catalogue of the Files contract.</summary>
public static class FilesErrors
{
    /// <summary>Creates the error for a file id the caller's tenant does not hold.</summary>
    public static AppError FileNotFound() => AppErrorFactory.FileNotFound("The file was not found.");

    /// <summary>Creates the error for a file whose checks or presets have not finished.</summary>
    public static AppError FileNotReady() => AppErrorFactory.Conflict("The file is not ready yet.");

    /// <summary>Creates the error for a file over the app policy's size limit.</summary>
    /// <param name="maxBytes">The policy's limit in bytes.</param>
    public static AppError FileTooLarge(long maxBytes) =>
        AppErrorFactory.Validation($"The file must be at most {maxBytes} bytes.");

    /// <summary>Creates the error for a type the app policy does not allow.</summary>
    /// <param name="contentType">The rejected content type.</param>
    public static AppError TypeNotAllowed(string contentType) =>
        AppErrorFactory.Validation($"Files of type {contentType} are not allowed.");

    /// <summary>Creates the error for bytes that contradict the claimed name or content type.</summary>
    public static AppError ContentMismatch() =>
        AppErrorFactory.Validation("The file's content does not match its name or type.");

    /// <summary>Creates the error for a name that is empty or longer than <see cref="FilesConstants.MaxNameLength"/>.</summary>
    public static AppError NameInvalid() =>
        AppErrorFactory.Validation($"The file name must be 1 to {FilesConstants.MaxNameLength} characters.");

    /// <summary>Creates the error for a reference already removed or never attached.</summary>
    public static AppError ReferenceNotFound() => AppErrorFactory.NotFound("The file reference was not found.");

    /// <summary>Creates the error for a signed URL past its expiry or with a bad signature.</summary>
    public static AppError UrlInvalid() => AppErrorFactory.Forbidden("The file link is invalid or expired.");

    /// <summary>Creates the error for a tenant the instance has suspended.</summary>
    public static AppError TenantSuspended() => AppErrorFactory.Forbidden("The files tenant is suspended.");

    /// <summary>Creates the error for a tenant id that is not a safe storage key segment.</summary>
    public static AppError TenantInvalid() => AppErrorFactory.Forbidden("The files tenant is not valid.");

    /// <summary>Creates the error for a list cursor no previous page returned.</summary>
    public static AppError CursorInvalid() => AppErrorFactory.Validation("The list cursor is not valid.");

    /// <summary>Creates the error for a byte range outside the file.</summary>
    /// <param name="size">The file's size in bytes.</param>
    public static AppError RangeInvalid(long size) =>
        AppErrorFactory.Validation($"The byte range must lie within the file's {size} bytes.");
}
