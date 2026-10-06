using System.Text.Json;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// Exact signed-out exemptions (route note r2 §2.2). Path equality is
/// ordinal case-insensitive. No prefix or query matching.
/// </summary>
public static class AdminSessionExemptions
{
    public const string PendingCookieName = "dw_admin_pending";

    public static bool IsAnonymousAuthAsset(PathString path, string method) =>
        HttpMethods.IsGet(method)
        && path.StartsWithSegments("/admin/auth", StringComparison.OrdinalIgnoreCase, out var rest)
        && rest.HasValue
        && rest.Value.Length > 1
        && rest.Value.Contains('.', StringComparison.Ordinal);

    public static bool TryMatchUnconditional(string path, string method)
    {
        return Exact(path, "/admin/sign-in") && IsGetOrPost(method)
               || Exact(path, "/admin/reset") && IsGetOrPost(method)
               || Exact(path, "/admin/reset/sent") && HttpMethods.IsGet(method)
               || Exact(path, "/admin/link-expired") && HttpMethods.IsGet(method)
               || Exact(path, "/admin/api/auth/sign-in") && HttpMethods.IsPost(method)
               || Exact(path, "/admin/api/auth/reset") && HttpMethods.IsPost(method)
               || Exact(path, "/admin/sign-out") && HttpMethods.IsPost(method)
               || Exact(path, "/admin/api/auth/sign-out") && HttpMethods.IsPost(method);
    }

    public static bool TryMatchTokenRow(string path, string method, out string requiredKind)
    {
        if (Exact(path, "/admin/sign-in/code") && IsGetOrPost(method)
            || Exact(path, "/admin/api/auth/sign-in/code") && HttpMethods.IsPost(method)
            || Exact(path, "/admin/sign-in/recovery") && IsGetOrPost(method)
            || Exact(path, "/admin/api/auth/sign-in/recovery") && HttpMethods.IsPost(method))
        {
            requiredKind = AdminAuthToken.KindPending;
            return true;
        }

        if (Exact(path, "/admin/bootstrap") && IsGetOrPost(method)
            || Exact(path, "/admin/api/auth/bootstrap") && HttpMethods.IsPost(method))
        {
            requiredKind = AdminAuthToken.KindBootstrap;
            return true;
        }

        if (Exact(path, "/admin/reset/confirm") && IsGetOrPost(method)
            || Exact(path, "/admin/api/auth/reset/confirm") && HttpMethods.IsPost(method))
        {
            requiredKind = AdminAuthToken.KindReset;
            return true;
        }

        requiredKind = string.Empty;
        return false;
    }

    public static bool TryCanonicalPath(HttpContext context, out string canonical)
    {
        var raw = context.Request.Path.Value;
        var encoded = context.Request.Path.ToUriComponent();
        if (!AdminPathCanonicalizer.TryCanonicalize(raw, out canonical)
            || !AdminPathCanonicalizer.TryCanonicalize(encoded, out _))
        {
            canonical = string.Empty;
            return false;
        }

        return true;
    }

    public static async Task<bool> HasValidTokenAsync(
        HttpContext context,
        string kind,
        IAdminAuthTokenStore tokens,
        IAdminClock clock,
        CancellationToken ct)
    {
        var raw = await ReadTokenAsync(context, kind, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var row = await tokens.GetByHashAsync(AdminAuthService.HashToken(raw), ct).ConfigureAwait(false);
        return row is not null
               && string.Equals(row.Kind, kind, StringComparison.Ordinal)
               && row.IsUsable(clock.UtcNow);
    }

    public static bool Exact(string path, string expected) =>
        string.Equals(path, expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsGetOrPost(string method) =>
        HttpMethods.IsGet(method) || HttpMethods.IsPost(method);

    private static async Task<string?> ReadTokenAsync(HttpContext context, string kind, CancellationToken ct)
    {
        if (kind == AdminAuthToken.KindPending)
        {
            var cookie = context.Request.Cookies[PendingCookieName];
            return string.IsNullOrWhiteSpace(cookie) ? null : cookie;
        }

        if (context.Request.Query.TryGetValue("token", out var query) && !string.IsNullOrWhiteSpace(query))
        {
            return query.ToString();
        }

        if (!HttpMethods.IsPost(context.Request.Method)
            || context.Request.ContentType is null
            || !context.Request.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;
            using var doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: ct)
                .ConfigureAwait(false);
            context.Request.Body.Position = 0;

            var root = doc.RootElement;
            if (kind == AdminAuthToken.KindPending
                && root.TryGetProperty("pendingToken", out var pending)
                && pending.ValueKind == JsonValueKind.String)
            {
                return pending.GetString();
            }

            if (root.TryGetProperty("linkToken", out var link) && link.ValueKind == JsonValueKind.String)
            {
                return link.GetString();
            }

            if (root.TryGetProperty("token", out var token) && token.ValueKind == JsonValueKind.String)
            {
                return token.GetString();
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
