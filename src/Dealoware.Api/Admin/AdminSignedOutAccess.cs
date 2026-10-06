using Dealoware.Domain.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// r2 §2 signed-out session exemptions. Exact path + method, ordinal
/// case-insensitive, no prefix or query matching. Step 2 owns the
/// bootstrap and link-expired rows plus /admin/auth/ static files.
/// </summary>
public static class AdminSignedOutAccess
{
    public const string BootstrapPage = "/admin/bootstrap";
    public const string BootstrapApi = "/admin/api/auth/bootstrap";
    public const string LinkExpired = "/admin/link-expired";
    public const string AuthStaticPrefix = "/admin/auth";

    public static bool PathsEqual(string canonical, string exact)
        => string.Equals(canonical, exact, StringComparison.OrdinalIgnoreCase);

    public static bool IsAuthStatic(string canonical)
        => canonical.StartsWith(AuthStaticPrefix + "/", StringComparison.OrdinalIgnoreCase);

    public static bool IsLinkExpiredGet(string canonical, string method)
        => PathsEqual(canonical, LinkExpired)
           && string.Equals(method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase);

    public static bool IsBootstrapPage(string canonical, string method)
        => PathsEqual(canonical, BootstrapPage)
           && (string.Equals(method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase)
               || string.Equals(method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase));

    public static bool IsBootstrapApiPost(string canonical, string method)
        => PathsEqual(canonical, BootstrapApi)
           && string.Equals(method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase);

    public static async Task<string?> ReadBootstrapTokenAsync(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("token", out var queryToken)
            && !string.IsNullOrWhiteSpace(queryToken))
        {
            return queryToken.ToString();
        }

        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return null;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(context.RequestAborted);
        context.Request.Body.Position = 0;
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("token", out var tokenProp)
                && tokenProp.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return tokenProp.GetString();
            }
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }

        return null;
    }

    public static async Task<bool> HasUsableBootstrapTokenAsync(
        HttpContext context,
        IAdminBootstrapService bootstrap)
    {
        var raw = await ReadBootstrapTokenAsync(context);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var inspect = await bootstrap.InspectAsync(raw, context.RequestAborted);
        return inspect.Valid;
    }
}
