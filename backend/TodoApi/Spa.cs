namespace TodoApi;

/// <summary>
/// Serves the SPA that <c>make build</c> puts in <c>wwwroot</c>: its files as the SDK listed and compressed them when
/// publishing the API, and its <c>index.html</c> for every other path that isn't a file, for the SPA's router.
/// </summary>
public static class Spa
{
    public static WebApplication MapSpa(this WebApplication app)
    {
        app.MapStaticAssets();
        app.MapFallbackToFile("index.html");
        return app;
    }
}
