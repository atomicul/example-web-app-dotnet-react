using Microsoft.AspNetCore.Http.HttpResults;

namespace TodoApi;

/// <summary>
/// Serves the challenge files an ACME client such as certbot writes under <c>AcmeChallenge:WebRoot</c>, so a
/// certificate authority can verify over HTTP that the app answers for its domain. Without a web root, serves nothing.
/// </summary>
public static class AcmeChallenge
{
    private const string RequestPath = "/.well-known/acme-challenge";

    public static WebApplication MapAcmeChallenge(this WebApplication app)
    {
        var webRoot = app.Configuration["AcmeChallenge:WebRoot"];
        if (string.IsNullOrEmpty(webRoot))
        {
            return app;
        }

        var directory = Path.Join(webRoot, RequestPath);
        app.MapGet($"{RequestPath}/{{token}}", Results<PhysicalFileHttpResult, NotFound> (string token) =>
        {
            var path = Path.Join(directory, token);
            return IsToken(token) && File.Exists(path)
                ? TypedResults.PhysicalFile(path, "text/plain")
                : TypedResults.NotFound();
        }).ExcludeFromDescription();
        return app;
    }

    private static bool IsToken(string token) =>
        token.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}
