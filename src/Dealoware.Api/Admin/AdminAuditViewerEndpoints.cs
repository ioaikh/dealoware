namespace Dealoware.Api.Admin;

/// <summary>
/// Read-only CoreOwner audit viewer (A7 Step 15 / S-D1, S-D2).
/// Pages and signed-in shell assets are mapped after AdminSessionMiddleware.
/// They are never published through UseStaticFiles / wwwroot.
/// Route note r3 (b079a814): pages live under /admin/... ; r2 is void.
/// Spec §5 allowed return paths: /admin/audit and /admin/audit/{id} (GUID).
/// </summary>
public static class AdminAuditViewerEndpoints
{
    public const string ListPath = "/admin/audit";
    public const string ShellCssPath = "/admin/audit/shell/audit.css";
    public const string ShellJsPath = "/admin/audit/shell/audit.js";

    private const string ResourcePrefix = "Dealoware.Api.Admin.Ui.";

    public static void MapAdminAuditViewer(this WebApplication app)
    {
        var audit = app.MapGroup("/admin/audit")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName);

        audit.MapGet("", ServeList).WithName("AdminAuditList");
        audit.MapGet("/", ServeList).WithName("AdminAuditListSlash");
        audit.MapGet("/shell/audit.css", ServeCss).WithName("AdminAuditShellCss");
        audit.MapGet("/shell/audit.js", ServeJs).WithName("AdminAuditShellJs");
        audit.MapGet("/{id:guid}", ServeDetail).WithName("AdminAuditDetail");
    }

    /// <summary>
    /// Spec §5 / route note r3: post-auth return paths for this viewer.
    /// Shell assets are not return paths.
    /// </summary>
    public static bool IsAllowedReturnPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var value = path.Trim();
        var query = value.IndexOf('?', StringComparison.Ordinal);
        if (query >= 0)
            value = value[..query];
        if (value.Length > 1)
            value = value.TrimEnd('/');

        if (value.Equals(ListPath, StringComparison.OrdinalIgnoreCase))
            return true;

        const string prefix = ListPath + "/";
        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var rest = value[prefix.Length..];
        return Guid.TryParse(rest, out _);
    }

    private static IResult ServeList() =>
        NoStoreContent(LoadUiResource("audit-list.html"), "text/html; charset=utf-8");

    private static IResult ServeDetail(Guid id)
    {
        _ = id;
        return NoStoreContent(LoadUiResource("audit-detail.html"), "text/html; charset=utf-8");
    }

    private static IResult ServeCss() =>
        NoStoreContent(LoadUiResource("audit.css"), "text/css; charset=utf-8");

    private static IResult ServeJs() =>
        NoStoreContent(LoadUiResource("audit.js"), "text/javascript; charset=utf-8");

    private static IResult NoStoreContent(string body, string contentType) =>
        new NoStoreContentResult(body, contentType);

    private sealed class NoStoreContentResult(string body, string contentType) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.OnStarting(static state =>
            {
                var response = (HttpResponse)state!;
                response.Headers.CacheControl = "no-store";
                response.Headers.Pragma = "no-cache";
                return Task.CompletedTask;
            }, httpContext.Response);

            await TypedResults.Content(body, contentType).ExecuteAsync(httpContext);
        }
    }

    internal static string LoadUiResource(string fileName)
    {
        var assembly = typeof(AdminAuditViewerEndpoints).Assembly;
        var resourceName = ResourcePrefix + fileName;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing admin UI resource {resourceName}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    internal static IReadOnlyList<string> EmbeddedUiResourceNames()
    {
        return typeof(AdminAuditViewerEndpoints).Assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .ToArray();
    }
}
