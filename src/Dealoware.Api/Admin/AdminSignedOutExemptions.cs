using System.Text.Json;
using Dealoware.Domain.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// r3 §2 signed-out session exemptions owned by Step 4, plus the
/// /admin/auth/ static-file folder (C8). C1: canonicalise the raw path
/// before any compare. Query strings never create an exemption.
/// Token rows require a server-validated unused reset token.
/// </summary>
public static class AdminSignedOutExemptions
{
    public const string ResetPage = "/admin/reset";
    public const string ResetSentPage = "/admin/reset/sent";
    public const string ResetConfirmPage = "/admin/reset/confirm";
    public const string LinkExpiredPage = "/admin/link-expired";
    public const string ResetApi = "/admin/api/auth/reset";
    public const string ResetConfirmApi = "/admin/api/auth/reset/confirm";
    public const string AuthStaticPrefix = "/admin/auth";

    public static async Task<bool> IsExemptAsync(
        HttpContext context,
        IAdminPasswordResetTokenRepository tokens,
        IAdminClock clock,
        CancellationToken cancellationToken)
    {
        if (!TryGetCanonicalPath(context, out var path))
        {
            return false;
        }

        var method = context.Request.Method;

        if (HttpMethods.IsGet(method) && IsAuthStaticFile(path))
        {
            return true;
        }

        if (PathEquals(path, ResetPage) && (HttpMethods.IsGet(method) || HttpMethods.IsPost(method)))
        {
            return true;
        }

        if (PathEquals(path, ResetSentPage) && HttpMethods.IsGet(method))
        {
            return true;
        }

        if (PathEquals(path, LinkExpiredPage) && HttpMethods.IsGet(method))
        {
            return true;
        }

        if (PathEquals(path, ResetApi) && HttpMethods.IsPost(method))
        {
            return true;
        }

        if ((PathEquals(path, ResetConfirmPage) && (HttpMethods.IsGet(method) || HttpMethods.IsPost(method)))
            || (PathEquals(path, ResetConfirmApi) && HttpMethods.IsPost(method)))
        {
            var raw = await ReadResetTokenAsync(context, cancellationToken);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var hash = AdminPasswordResetToken.HashRaw(raw.Trim());
            var row = await tokens.FindUsableByHashAsync(hash, clock.UtcNow, cancellationToken);
            return row is not null;
        }

        return false;
    }

    public static bool TryGetCanonicalPath(HttpContext context, out string path)
        => AdminPathCanonicalizer.TryCanonicalize(AdminPathCanonicalizer.RawPath(context), out path);

    public static bool IsFormGet(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method)
            || !TryGetCanonicalPath(context, out var path)
            || IsAuthStaticFile(path))
        {
            return false;
        }

        return PathEquals(path, ResetPage)
               || PathEquals(path, ResetSentPage)
               || PathEquals(path, ResetConfirmPage)
               || PathEquals(path, LinkExpiredPage);
    }

    public static bool IsAuthStaticFile(string canonicalPath)
    {
        if (!canonicalPath.StartsWith(AuthStaticPrefix + "/", StringComparison.OrdinalIgnoreCase)
            || canonicalPath.EndsWith('/'))
        {
            return false;
        }

        var relative = canonicalPath[(AuthStaticPrefix.Length + 1)..];
        if (string.IsNullOrEmpty(relative))
        {
            return false;
        }

        var lastSlash = relative.LastIndexOf('/');
        var fileName = lastSlash >= 0 ? relative[(lastSlash + 1)..] : relative;
        var dot = fileName.LastIndexOf('.');
        return dot > 0 && dot < fileName.Length - 1;
    }

    public static bool PathEquals(string path, string expected)
        => string.Equals(path, expected, StringComparison.OrdinalIgnoreCase);

    public static async Task<string?> ReadResetTokenAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (context.Request.Query.TryGetValue("token", out var queryToken)
            && !string.IsNullOrWhiteSpace(queryToken))
        {
            return queryToken.ToString();
        }

        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.ContentLength is > 0)
        {
            context.Request.EnableBuffering();
        }

        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);
            if (form.TryGetValue("token", out var formToken) && !string.IsNullOrWhiteSpace(formToken))
            {
                return formToken.ToString();
            }
        }

        var contentType = context.Request.ContentType;
        if (!string.IsNullOrEmpty(contentType)
            && contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
            && context.Request.ContentLength is > 0 and < 8192)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var json = await reader.ReadToEndAsync(cancellationToken);
            context.Request.Body.Position = 0;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("token", out var tokenProp)
                    && tokenProp.ValueKind == JsonValueKind.String)
                {
                    return tokenProp.GetString();
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        return null;
    }
}
