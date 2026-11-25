using Files.Application.Files.Models;
using Files.Application.Files.Queries;
using Files.Infrastructure.Access.Extensions;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Infrastructure.Files.QueryHandlers;

/// <inheritdoc />
public sealed class OpenFileContentQueryHandler(IFileStoreBroker store, FilesCallerOverride callers)
    : IQueryHandler<OpenFileContentQuery, AppResult<FileDownload>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<FileDownload>> HandleAsync(OpenFileContentQuery request, CancellationToken ct)
    {
        using var caller = callers.Use(request.Actor);
        if (await store.GetAsync(request.Id, ct) is not Result<FileModel>.Success { Value: var file })
            return AppResult<FileDownload>.Fail(FilesErrors.FileNotFound());

        if (request.Preset is not null && request.HasRange)
            return AppResult<FileDownload>.Fail(AppErrorFactory.Validation("A preset image request reads the whole image."));

        if (!ByteRangeHeaderParser.TryParse(request.Range, file.Size, out var range))
        {
            var error = FilesErrors.RangeInvalid(file.Size);
            return AppResult<FileDownload>.Fail(AppError.Of(error.Type, error.Message,
                new Dictionary<string, object?> { [FileDownload.UnsatisfiedRangeLength] = file.Size }));
        }

        var opened = request.Preset is null
            ? await store.OpenReadAsync(request.Id, range, ct)
            : await store.OpenImageAsync(request.Id, request.Preset, ct);

        return opened.Match(content => AppResult<FileDownload>.Ok(new FileDownload(file, content)), error => AppResult<FileDownload>.Fail(error));
    }
}
