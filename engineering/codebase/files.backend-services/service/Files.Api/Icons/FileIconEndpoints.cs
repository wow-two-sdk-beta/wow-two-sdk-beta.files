using System.Text.RegularExpressions;

namespace Files.Api.Icons;

/// <summary>
/// Maps the misses of the icon files the web bundle ships under <c>wwwroot/icons</c>: static files answer every registry
/// extension (<c>/icons/pdf.svg</c>) and kind (<c>/icons/kinds/video.svg</c>) before routing, and any other well-formed
/// icon name gets the generic kind's icon instead of the app shell.
/// </summary>
public static partial class FileIconEndpoints
{
    /// <summary>Holds the path the icon files sit under, in the web root and on the wire; other origins embed them.</summary>
    public const string Path = "/icons";

    /// <summary>Holds the web-root file every unknown icon name answers with.</summary>
    private const string GenericIcon = "icons/kinds/generic.svg";

    /// <summary>Maps <c>/icons/{name}.svg</c> and <c>/icons/kinds/{name}.svg</c> for names the bundle has no file for.</summary>
    public static IEndpointRouteBuilder MapFileIcons(this IEndpointRouteBuilder app)
    {
        var icons = app.MapGroup(Path).AllowAnonymous();
        icons.MapGet("/{file}", ServeGeneric);
        icons.MapGet("/kinds/{file}", ServeGeneric);
        return app;
    }

    /// <summary>Answers a lowercase <c>{name}.svg</c> with the generic icon, cached for an hour; anything else is not found.</summary>
    private static IResult ServeGeneric(string file, IWebHostEnvironment environment, HttpResponse response)
    {
        var icon = environment.WebRootFileProvider.GetFileInfo(GenericIcon);
        if (!IconName().IsMatch(file) || !icon.Exists)
            return TypedResults.NotFound();

        response.Headers.CacheControl = "public, max-age=3600";
        return TypedResults.Stream(icon.CreateReadStream(), "image/svg+xml");
    }

    [GeneratedRegex("^[a-z0-9]{1,16}\\.svg$")]
    private static partial Regex IconName();
}
