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
