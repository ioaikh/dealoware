using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// Exact signed-out exemptions for AdminSessionMiddleware (route-prefix note r3 §2 + §10 C1).
/// Path is canonicalised first, then compared with ordinal case-insensitive equality.
/// No prefix or query matching except GET static files under /admin/auth/.
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
    public const string ApiBootstrap = "/admin/api/auth/bootstrap";
    public const string ApiSignOut = "/admin/api/auth/sign-out";

    public enum TokenKind
    {
        None,
        PendingSignIn,
        Bootstrap,
        PasswordReset
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
        new(ResetConfirm, HttpMethod.Get, TokenKind.PasswordReset),
        new(ResetConfirm, HttpMethod.Post, TokenKind.PasswordReset),
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
        new(ApiResetConfirm, HttpMethod.Post, TokenKind.PasswordReset),
        new(ApiSignOut, HttpMethod.Post, TokenKind.None)
    ];

    public static bool IsAnonymousAuthAsset(string canonicalPath)
        => canonicalPath.StartsWith(AnonymousAuthAssetPrefix, StringComparison.OrdinalIgnoreCase);

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
           || string.Equals(canonicalPath, ApiResetConfirm, StringComparison.OrdinalIgnoreCase);

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
}
