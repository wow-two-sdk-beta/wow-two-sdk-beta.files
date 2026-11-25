using System.Reflection;
using Files.Application.Status.Models;
using Files.Application.Status.Queries;
using Files.Infrastructure.Status.Services;
using Microsoft.Extensions.Hosting;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Infrastructure.Status.QueryHandlers;

/// <inheritdoc />
public sealed class GetFilesSystemStatusQueryHandler(IHostEnvironment host) : IQueryHandler<GetFilesSystemStatusQuery, AppResult<FilesSystemStatus>>
{
    /// <inheritdoc />
    public ValueTask<AppResult<FilesSystemStatus>> HandleAsync(GetFilesSystemStatusQuery query, CancellationToken cancellationToken)
    {
        var version = Assembly.Load(host.ApplicationName)
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

        return ValueTask.FromResult(AppResult<FilesSystemStatus>.Ok(new FilesSystemStatus(version, ImagingProbe.State)));
    }
}
