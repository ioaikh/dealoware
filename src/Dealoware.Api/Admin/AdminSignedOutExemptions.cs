using System.Text.Json;
using Dealoware.Domain.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// r2 §2 signed-out session exemptions owned by Step 4, plus the
/// /admin/auth/ static-asset folder. Path match is ordinal
/// case-insensitive equality. Query strings never create an exemption.
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
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        if (HttpMethods.IsGet(method)
            && path.StartsWith(AuthStaticPrefix + "/", StringComparison.OrdinalIgnoreCase))
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
