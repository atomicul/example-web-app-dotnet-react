using Microsoft.AspNetCore.StaticAssets;
using Microsoft.AspNetCore.StaticFiles;

namespace TodoApi;

/// <summary>
/// Serves the SPA that <c>make build</c> puts in <c>wwwroot</c>: its files as the SDK listed and compressed them when
/// publishing the API, and its <c>index.html</c> for every other path that isn't a file, for the SPA's router.
/// Vite names every file it builds under <c>assets/</c> after its content, so browsers keep those for a year without
/// asking again, while they ask whether <c>index.html</c>, which names them, changed on every load.
/// </summary>
public static class Spa
{
    private const string HashedAssetsPath = "/assets";

    public static WebApplication MapSpa(this WebApplication app)
    {
        app.Use(CacheHashedAssetsForever);
        app.MapStaticAssets();
        app.MapFallbackToFile("index.html", new StaticFileOptions { OnPrepareResponse = RevalidateOnEveryLoad });
        return app;
    }

    /// <summary>
    /// The SDK only caches the copies it names after their content itself, which Vite's files never ask for, so its
    /// headers are replaced as the response starts: they can't be changed on its endpoints.
    /// </summary>
    private static Task CacheHashedAssetsForever(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments(HashedAssetsPath)
            && context.GetEndpoint()?.Metadata.GetMetadata<StaticAssetDescriptor>() is not null)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.CacheControl = "max-age=31536000, immutable";
                return Task.CompletedTask;
            });
        }

        return next(context);
    }

    /// <summary>
    /// Without a lifetime, browsers would keep the page for a while of their own choosing, and could load a release
    /// whose files are gone after a deploy.
    /// </summary>
    private static void RevalidateOnEveryLoad(StaticFileResponseContext context) =>
        context.Context.Response.Headers.CacheControl = "no-cache";
}
