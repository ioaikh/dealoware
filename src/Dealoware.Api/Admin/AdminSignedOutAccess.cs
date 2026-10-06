namespace Dealoware.Api.Admin;

/// <summary>
/// Route-prefix note r5 sha c5e9d232 (r3 and r4 VOID). Exact path + method
/// after C1. F2: GET /admin/bootstrap takes no token and has no side effects;
/// the token is checked on POST only. POST rows are exempt so link failures
/// go to /admin/link-expired.
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

    public static bool IsBootstrapGet(string canonical, string method)
        => PathsEqual(canonical, BootstrapPage)
           && string.Equals(method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase);

    public static bool IsOwnedSignedOutPair(string canonical, string method)
        => IsAuthStatic(canonical) && string.Equals(method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase)
           || IsLinkExpiredGet(canonical, method)
           || IsBootstrapPage(canonical, method)
           || IsBootstrapApiPost(canonical, method);
}
