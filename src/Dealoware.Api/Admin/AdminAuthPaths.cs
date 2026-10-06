using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Features;

namespace Dealoware.Api.Admin;

/// <summary>
/// Locked r5 inventory (sha c5e9d232; r3 and r4 VOID). Exact path+method
/// session exemptions. §10 C1–C8 win over §§2–5.
/// </summary>
public static class AdminAuthPaths
{
    public const string SignIn = "/admin/sign-in";
    public const string SignInCode = "/admin/sign-in/code";
    public const string SignInRecovery = "/admin/sign-in/recovery";
    public const string Bootstrap = "/admin/bootstrap";
    public const string LinkExpired = "/admin/link-expired";
    public const string Authenticator = "/admin/setup/authenticator";
    public const string RecoveryCodes = "/admin/setup/recovery-codes";
    public const string Reset = "/admin/reset";
    public const string ResetSent = "/admin/reset/sent";
    public const string ResetConfirm = "/admin/reset/confirm";
    public const string SignOut = "/admin/sign-out";
    public const string SettingsSecurity = "/admin/settings/security";
    public const string Stats = "/admin/";
    public const string StatsBare = "/admin";

    public const string ApiSignIn = "/admin/api/auth/sign-in";
    public const string ApiSignInCode = "/admin/api/auth/sign-in/code";
    public const string ApiSignInRecovery = "/admin/api/auth/sign-in/recovery";
    public const string ApiReset = "/admin/api/auth/reset";
    public const string ApiBootstrap = "/admin/api/auth/bootstrap";
    public const string ApiResetConfirm = "/admin/api/auth/reset/confirm";
    public const string ApiSignOut = "/admin/api/auth/sign-out";

    public enum TokenCondition
    {
        None,
        PendingSignIn,
        Bootstrap,
        Reset,
        Enrol
    }

    public readonly record struct Exemption(string Path, string[] Methods, TokenCondition Condition);

    public static readonly Exemption[] PageExemptions =
    [
        new(SignIn, ["GET", "POST"], TokenCondition.None),
        new(SignInCode, ["GET", "POST"], TokenCondition.PendingSignIn),
        new(SignInRecovery, ["GET", "POST"], TokenCondition.PendingSignIn),
        new(Reset, ["GET", "POST"], TokenCondition.None),
        new(ResetSent, ["GET"], TokenCondition.None),
        // r5 §10 C4.3 / COMMON 1a9aabc5: GET takes no token and has no side
        // effects. The emailed link token is #token= only, then POST body only.
        new(Bootstrap, ["GET", "POST"], TokenCondition.None),
        new(ResetConfirm, ["GET", "POST"], TokenCondition.None),
        new(LinkExpired, ["GET"], TokenCondition.None),
        new(Authenticator, ["GET", "POST"], TokenCondition.Enrol),
        new(RecoveryCodes, ["GET"], TokenCondition.Enrol),
        new(SignOut, ["POST"], TokenCondition.None)
    ];

    public static readonly Exemption[] AuthApiExemptions =
    [
        new(ApiSignIn, ["POST"], TokenCondition.None),
        new(ApiSignInCode, ["POST"], TokenCondition.PendingSignIn),
        new(ApiSignInRecovery, ["POST"], TokenCondition.PendingSignIn),
        new(ApiReset, ["POST"], TokenCondition.None),
        new(ApiBootstrap, ["POST"], TokenCondition.None),
        new(ApiResetConfirm, ["POST"], TokenCondition.None),
        new(ApiSignOut, ["POST"], TokenCondition.None)
    ];

    public static string RawPath(HttpContext context)
    {
        var raw = context.Features.Get<IHttpRequestFeature>()?.RawTarget;
        if (string.IsNullOrEmpty(raw))
            raw = context.Request.Path.Value ?? string.Empty;
        var q = raw.IndexOf('?', StringComparison.Ordinal);
        return q >= 0 ? raw[..q] : raw;
    }

    /// <summary>C1: canonicalise before exemption compare. False → not exempt.</summary>
    public static bool TryCanonicalize(string rawPath, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrEmpty(rawPath))
            return false;

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(rawPath);
        }
        catch (UriFormatException)
        {
            return false;
        }

        if (ContainsEncodedSeparator(decoded) || decoded.Contains('\\'))
            return false;
        foreach (var ch in decoded)
        {
            var cat = char.GetUnicodeCategory(ch);
            if (cat is UnicodeCategory.Control or UnicodeCategory.Format)
                return false;
        }

        if (decoded.Contains("//", StringComparison.Ordinal)
            || decoded.Contains(';')
            || decoded.Split('/').Any(segment => segment == ".."))
        {
            return false;
        }

        if (decoded.Length > 1
            && decoded.EndsWith('/')
            && !decoded.Equals(Stats, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        canonical = decoded;
        return true;
    }

    public static bool IsAuthAssetPath(string canonical) =>
        canonical.StartsWith("/admin/auth/", StringComparison.OrdinalIgnoreCase)
        && !canonical.Equals("/admin/auth/", StringComparison.OrdinalIgnoreCase);

    public static bool TryMatchExemption(string canonical, string method, out Exemption exemption)
    {
        foreach (var row in PageExemptions.Concat(AuthApiExemptions))
        {
            if (!string.Equals(canonical, row.Path, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!row.Methods.Any(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase)))
                continue;
            exemption = row;
            return true;
        }

        exemption = default;
        return false;
    }

    public static async Task<bool> IsExemptAsync(
        HttpContext context,
        IAdminAuthTokenGate gate,
        CancellationToken cancellationToken)
    {
        if (!TryCanonicalize(RawPath(context), out var canonical))
            return false;

        if (IsAuthAssetPath(canonical) && HttpMethods.IsGet(context.Request.Method))
            return true;

        if (!TryMatchExemption(canonical, context.Request.Method, out var row))
            return false;

        return row.Condition switch
        {
            TokenCondition.None => true,
            TokenCondition.PendingSignIn => await gate.HasValidPendingSignInAsync(context, cancellationToken),
            TokenCondition.Bootstrap => await gate.HasValidBootstrapTokenAsync(context, cancellationToken),
            TokenCondition.Reset => await gate.HasValidResetTokenAsync(context, cancellationToken),
            TokenCondition.Enrol => await gate.HasEnrolPendingAsync(context, cancellationToken),
            _ => false
        };
    }

    /// <summary>C6 / §5 return-path checks. Failures → /admin/. Never echo the raw value.</summary>
    public static string SafeReturnPath(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return Stats;
        if (candidate.Length > 2048)
            return Stats;

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(candidate.Trim());
        }
        catch (UriFormatException)
        {
            return Stats;
        }

        if (ContainsReturnEncodedResidue(decoded))
            return Stats;
        if (decoded.Length == 0 || decoded[0] != '/')
            return Stats;
        if (decoded.Length > 1 && (decoded[1] is '/' or '\\'))
            return Stats;
        if (decoded.Contains('\\') || decoded.Contains("..", StringComparison.Ordinal))
            return Stats;
        var firstSlash = decoded.IndexOf('/');
        if (decoded.Contains(':') && decoded.IndexOf(':') < firstSlash)
            return Stats;
        foreach (var ch in decoded)
        {
            var cat = char.GetUnicodeCategory(ch);
            if (cat is UnicodeCategory.Control or UnicodeCategory.Format)
                return Stats;
        }

        var pathOnly = decoded;
        var query = "";
        var q = decoded.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
        {
            pathOnly = decoded[..q];
            query = decoded[q..];
        }

        if (pathOnly.Equals(StatsBare, StringComparison.OrdinalIgnoreCase))
            pathOnly = Stats;
        if (!IsAllowedReturnPath(pathOnly))
            return Stats;
        if (query.Length > 1)
            return Stats;
        return pathOnly;
    }

    private static bool ContainsEncodedSeparator(string value) =>
        value.Contains("%2f", StringComparison.OrdinalIgnoreCase)
        || value.Contains("%5c", StringComparison.OrdinalIgnoreCase)
        || value.Contains("%2e", StringComparison.OrdinalIgnoreCase)
        || value.Contains("%25", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsReturnEncodedResidue(string value) =>
        value.Contains("%2f", StringComparison.OrdinalIgnoreCase)
        || value.Contains("%5c", StringComparison.OrdinalIgnoreCase)
        || value.Contains("%2e%2e", StringComparison.OrdinalIgnoreCase)
        || value.Contains("%25", StringComparison.OrdinalIgnoreCase);

    private static bool IsAllowedReturnPath(string path)
    {
        if (string.Equals(path, Stats, StringComparison.OrdinalIgnoreCase))
            return true;

        string[] lists = ["/admin/participants", "/admin/artifacts", "/admin/negotiations", "/admin/offers"];
        if (lists.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)))
            return true;
        if (string.Equals(path, "/admin/audit", StringComparison.OrdinalIgnoreCase))
            return true;

        var detail = Regex.Match(
            path,
            "^/admin/(participants|artifacts|negotiations|offers)/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$",
            RegexOptions.CultureInvariant);
        if (detail.Success)
            return true;

        var audit = Regex.Match(
            path,
            "^/admin/audit/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$",
            RegexOptions.CultureInvariant);
        return audit.Success;
    }
}
