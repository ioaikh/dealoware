using System.Text.Json;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// Exact signed-out exemptions for AdminSessionMiddleware (route-prefix note r3 §2 + §10 C1).
/// Path is canonicalised first, then compared with ordinal case-insensitive equality.
/// GET /admin/reset/confirm takes no token (b6194918 item 6). The reset token is
/// checked on POST only, in the body.
/// </summary>
public static class AdminSignedOutExemptions
{
    public const string AnonymousAuthAssetPrefix = "/admin/auth/";

    public const string SignIn = "/admin/sign-in";
    public const string SignInCode = "/admin/sign-in/code";
    public const string SignInRecovery = "/admin/sign-in/recovery";
    public const string SetupAuthenticator = "/admin/setup/authenticator";
    public const string SetupRecoveryCodes = "/admin/setup/recovery-codes";
    public const string Reset = "/admin/reset";
    public const string ResetSent = "/admin/reset/sent";
    public const string ResetConfirm = "/admin/reset/confirm";
    public const string Bootstrap = "/admin/bootstrap";
    public const string LinkExpired = "/admin/link-expired";
    public const string SignOut = "/admin/sign-out";

    public const string ApiSignIn = "/admin/api/auth/sign-in";
    public const string ApiSignInCode = "/admin/api/auth/sign-in/code";
    public const string ApiSignInRecovery = "/admin/api/auth/sign-in/recovery";
    public const string ApiReset = "/admin/api/auth/reset";
    public const string ApiResetConfirm = "/admin/api/auth/reset/confirm";
    public const string ApiResetConfirmCheck = "/admin/api/auth/reset/confirm/check";
    public const string ApiBootstrap = "/admin/api/auth/bootstrap";
    public const string ApiSignOut = "/admin/api/auth/sign-out";

    public const string ResetPage = Reset;
    public const string ResetSentPage = ResetSent;
    public const string ResetConfirmPage = ResetConfirm;
    public const string LinkExpiredPage = LinkExpired;
    public const string ResetApi = ApiReset;
    public const string ResetConfirmApi = ApiResetConfirm;
    public const string ResetConfirmCheckApi = ApiResetConfirmCheck;
    public const string AuthStaticPrefix = "/admin/auth";

    public enum TokenKind
    {
        None,
        PendingSignIn,
        Bootstrap
    }

    public readonly record struct ExemptionRule(
        string Path,
        HttpMethod Method,
        TokenKind Token);

    public static readonly IReadOnlyList<ExemptionRule> Rules =
    [
        new(SignIn, HttpMethod.Get, TokenKind.None),
        new(SignIn, HttpMethod.Post, TokenKind.None),
        new(SignInCode, HttpMethod.Get, TokenKind.PendingSignIn),
        new(SignInCode, HttpMethod.Post, TokenKind.PendingSignIn),
        new(SignInRecovery, HttpMethod.Get, TokenKind.PendingSignIn),
        new(SignInRecovery, HttpMethod.Post, TokenKind.PendingSignIn),
        new(Reset, HttpMethod.Get, TokenKind.None),
        new(Reset, HttpMethod.Post, TokenKind.None),
        new(ResetSent, HttpMethod.Get, TokenKind.None),
        new(Bootstrap, HttpMethod.Get, TokenKind.Bootstrap),
        new(Bootstrap, HttpMethod.Post, TokenKind.Bootstrap),
        new(ResetConfirm, HttpMethod.Get, TokenKind.None),
        new(ResetConfirm, HttpMethod.Post, TokenKind.None),
        new(LinkExpired, HttpMethod.Get, TokenKind.None),
        new(SetupAuthenticator, HttpMethod.Get, TokenKind.PendingSignIn),
        new(SetupAuthenticator, HttpMethod.Post, TokenKind.PendingSignIn),
        new(SetupRecoveryCodes, HttpMethod.Get, TokenKind.PendingSignIn),
        new(SignOut, HttpMethod.Post, TokenKind.None),
        new(ApiSignIn, HttpMethod.Post, TokenKind.None),
        new(ApiSignInCode, HttpMethod.Post, TokenKind.PendingSignIn),
        new(ApiSignInRecovery, HttpMethod.Post, TokenKind.PendingSignIn),
        new(ApiReset, HttpMethod.Post, TokenKind.None),
        new(ApiBootstrap, HttpMethod.Post, TokenKind.Bootstrap),
        new(ApiResetConfirm, HttpMethod.Post, TokenKind.None),
        new(ApiResetConfirmCheck, HttpMethod.Post, TokenKind.None),
        new(ApiSignOut, HttpMethod.Post, TokenKind.None)
    ];

    /// <summary>
    /// r5 C8: GET under /admin/auth/ is exempt only for static files (name.ext).
    /// Extension-less handler-like paths are not anonymous and stay behind the session gate.
    /// </summary>
    public static bool IsAnonymousAuthAsset(string canonicalPath)
        => IsAuthStaticFile(canonicalPath);

    public static bool IsSignOut(string canonicalPath)
        => string.Equals(canonicalPath, SignOut, StringComparison.OrdinalIgnoreCase)
           || string.Equals(canonicalPath, ApiSignOut, StringComparison.OrdinalIgnoreCase);

    public static bool IsAuthPost(string canonicalPath)
        => string.Equals(canonicalPath, SignIn, StringComparison.OrdinalIgnoreCase)
           || string.Equals(canonicalPath, SignInCode, StringComparison.OrdinalIgnoreCase)
           || string.Equals(canonicalPath, SignInRecovery, StringComparison.OrdinalIgnoreCase)
           || string.Equals(canonicalPath, SetupAuthenticator, StringComparison.OrdinalIgnoreCase)
           || string.Equals(canonicalPath, ApiSignIn, StringComparison.OrdinalIgnoreCase)
           || string.Equals(canonicalPath, ApiSignInCode, StringComparison.OrdinalIgnoreCase)
           || string.Equals(canonicalPath, ApiSignInRecovery, StringComparison.OrdinalIgnoreCase)
           || string.Equals(canonicalPath, ApiReset, StringComparison.OrdinalIgnoreCase)
           || string.Equals(canonicalPath, ApiBootstrap, StringComparison.OrdinalIgnoreCase)
           || string.Equals(canonicalPath, ApiResetConfirm, StringComparison.OrdinalIgnoreCase)
           || string.Equals(canonicalPath, ApiResetConfirmCheck, StringComparison.OrdinalIgnoreCase);

    public static ExemptionRule? FindRule(string canonicalPath, string method)
    {
        foreach (var rule in Rules)
        {
            if (!string.Equals(rule.Path, canonicalPath, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.Equals(rule.Method.Method, method, StringComparison.OrdinalIgnoreCase))
                continue;
            return rule;
        }

        return null;
    }

    public static async Task<bool> HasValidPendingTokenAsync(
        HttpContext context,
        IAdminCoreOwnerAccountRepository accounts,
        CancellationToken cancellationToken)
    {
        if (!context.Request.Cookies.TryGetValue(AdminPendingAuthCookie.Name, out var raw)
            || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var pending = await accounts.GetPendingByTokenHashAsync(
            AdminPendingToken.Hash(raw),
            cancellationToken);
        return pending is not null && pending.IsUsable();
    }

    public static bool TryGetCanonicalPath(HttpContext context, out string path)
        => AdminPathCanonicalizer.TryGetComparablePath(context, out path);

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

    public static async Task<string?> ReadResetTokenAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return null;
        }

        if (context.Request.ContentLength is > 0)
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
