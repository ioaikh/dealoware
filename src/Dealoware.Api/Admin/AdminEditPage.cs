namespace Dealoware.Api.Admin;

/// <summary>
/// Serves the edit/Expire HTML shell under /admin/{type}/{id}/edit.
/// The shell has no entity data; the page loads values via the admin API.
/// </summary>
internal static class AdminEditPage
{
    private static readonly Dictionary<string, string> Screens = new(StringComparer.OrdinalIgnoreCase)
    {
        ["participants"] = "Participant",
        ["artifacts"] = "Artifact",
        ["negotiations"] = "Negotiation",
        ["offers"] = "Offer"
    };

    public static IResult Page(string type, Guid id)
    {
        if (!Screens.TryGetValue(type, out var screen))
            return AdminDeny.NotFoundResult();

        var html = Read("edit.html")
            .Replace("{{SCREEN}}", screen, StringComparison.Ordinal)
            .Replace("{{TYPE}}", type, StringComparison.Ordinal)
            .Replace("{{ID}}", id.ToString("D"), StringComparison.Ordinal);
        return new NoStoreContent(html, "text/html; charset=utf-8");
    }

    public static IResult Css() => new NoStoreContent(Read("edit.css"), "text/css; charset=utf-8");

    public static IResult Js() => new NoStoreContent(Read("edit.js"), "application/javascript; charset=utf-8");

    /// <summary>
    /// Signed-in shell only. These files are mapped after AdminSessionMiddleware
    /// and must never be copied to wwwroot (anonymous static files).
    /// Only /admin/auth/ static files may load signed out (route note r3 b079a814).
    /// </summary>
    private sealed class NoStoreContent : IResult
    {
        private readonly string _body;
        private readonly string _contentType;

        public NoStoreContent(string body, string contentType)
        {
            _body = body;
            _contentType = contentType;
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers.CacheControl = "no-store";
            httpContext.Response.ContentType = _contentType;
            await httpContext.Response.WriteAsync(_body);
        }
    }

    private static string Read(string fileName)
    {
        foreach (var root in new[]
                 {
                     Path.Combine(AppContext.BaseDirectory, "Admin", "Ui"),
                     Path.Combine(Directory.GetCurrentDirectory(), "Admin", "Ui"),
                     Path.Combine(AppContext.BaseDirectory, "src", "Dealoware.Api", "Admin", "Ui")
                 })
        {
            var path = Path.Combine(root, fileName);
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        throw new FileNotFoundException($"Admin edit UI file '{fileName}' was not found.");
    }
}
