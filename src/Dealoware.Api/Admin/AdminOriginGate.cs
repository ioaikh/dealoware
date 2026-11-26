using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// R13: exact Origin (or Referer-origin fallback) against the configured admin
/// origin. Never uses the Host header. Reason class only; no header values.
/// </summary>
public static class AdminOriginGate
{
    public const string OriginMissing = "origin_missing";
    public const string OriginNull = "origin_null";
    public const string OriginMismatch = "origin_mismatch";

    public static string ConfiguredOrigin => AdminHostOptions.ProductionAdminOrigin;

    public static bool IsStateChanging(HttpRequest request)
        => HttpMethods.IsPost(request.Method)
           || HttpMethods.IsPut(request.Method)
           || HttpMethods.IsPatch(request.Method)
           || HttpMethods.IsDelete(request.Method);

    public static bool TryValidate(HttpContext context, out string reasonClass)
    {
        if (context.Request.Headers.TryGetValue("Origin", out var originValues)
            && !string.IsNullOrWhiteSpace(originValues.ToString()))
        {
            if (IsSameOriginSignOutFormNavigation(context))
            {
                reasonClass = string.Empty;
                return true;
            }

            return MatchPresented(originValues.ToString(), isOriginHeader: true, out reasonClass);
        }

        if (context.Request.Headers.TryGetValue("Referer", out var refererValues)
            && !string.IsNullOrWhiteSpace(refererValues.ToString()))
        {
            var referer = refererValues.ToString();
            if (string.Equals(referer, "null", StringComparison.OrdinalIgnoreCase)
                || !TryGetOrigin(referer, out var refererOrigin))
            {
                reasonClass = OriginNull;
                return false;
            }

            return MatchPresented(refererOrigin, isOriginHeader: false, out reasonClass);
        }

        reasonClass = OriginMissing;
        return false;
    }

    /// <summary>
    /// Set on the request when <see cref="IsSameOriginSignOutFormNavigation"/>
    /// let it through: the anti-forgery check then takes the sign-out form's
    /// hidden field only, never a header token.
    /// </summary>
    public const string NullOriginSignOutKey = "AdminNullOriginSignOut";

    /// <summary>
    /// The one request this check takes without a real origin: the shell's
    /// sign-out form, sent by the browser itself with JavaScript off.
    /// <para>
    /// Every admin page is served under Referrer-Policy: no-referrer, which the
    /// project keeps so that no page ever says where it came from. Under that
    /// policy the Fetch standard has the browser send "Origin: null" (and no
    /// Referer) with any form POST, even to the page's own origin, and every
    /// engine does. So the plain check can never pass a form sent without
    /// JavaScript.
    /// </para>
    /// <para>
    /// For POST /admin/sign-out alone (the exact canonical path the form-token
    /// check uses, urlencoded), an Origin of exactly "null" is taken as this
    /// site when the browser also says so in Sec-Fetch-Site: same-origin and
    /// Sec-Fetch-Mode: navigate. Those are forbidden header names: a page cannot
    /// set them, and the browser derives Sec-Fetch-Site from the real initiator,
    /// so a form or a sandboxed frame on another site (which also sends
    /// "Origin: null") is "cross-site" or "same-site". Without those headers (an
    /// old browser, a non-browser client) the request is refused as before. Any
    /// Origin other than exactly "null" is judged by the ordinary exact match
    /// alone. On this branch the anti-forgery token must be the form's hidden
    /// field (<see cref="NullOriginSignOutKey"/>), checked exactly as the header
    /// is; a header token does not do. At worst a request that got this far
    /// could end the owner's session, and it cannot without that field.
    /// </para>
    /// </summary>
    private static bool IsSameOriginSignOutFormNavigation(HttpContext context)
    {
        var request = context.Request;
        if (!HttpMethods.IsPost(request.Method)
            || !IsSingle(request.Headers.Origin, "null")
            || !IsSingle(request.Headers["Sec-Fetch-Site"], "same-origin")
            || !IsSingle(request.Headers["Sec-Fetch-Mode"], "navigate")
            || !request.HasFormContentType
            || !AdminPathCanonicalizer.TryGetComparablePath(context, out var canonical)
            || !string.Equals(canonical, AdminSignedOutExemptions.SignOut, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        context.Items[NullOriginSignOutKey] = true;
        return true;
    }

    private static bool IsSingle(Microsoft.Extensions.Primitives.StringValues values, string expected)
        => values.Count == 1 && string.Equals(values[0], expected, StringComparison.Ordinal);

    private static bool MatchPresented(string presented, bool isOriginHeader, out string reasonClass)
    {
        if (string.Equals(presented, "null", StringComparison.OrdinalIgnoreCase))
        {
            reasonClass = OriginNull;
            return false;
        }

        var origin = isOriginHeader ? presented.Trim() : presented;
        if (string.Equals(origin, ConfiguredOrigin, StringComparison.Ordinal))
        {
            reasonClass = string.Empty;
            return true;
        }

        reasonClass = OriginMismatch;
        return false;
    }

    public static bool TryGetOrigin(string url, out string origin)
    {
        origin = string.Empty;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        origin = uri.IsDefaultPort
            ? $"{uri.Scheme}://{uri.Host}"
            : $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        return true;
    }
}
