namespace Dealoware.Application.Admin;

/// <summary>
/// Builds reset and bootstrap token links. Callers pass the raw token only.
/// Paths match route note r5 c5e9d232: /admin/reset/confirm and /admin/bootstrap.
/// Token sits in the URL fragment (Chief Security answers tip b6194918, item 6).
/// Origin is this fixed constant; never the request Host.
/// </summary>
public static class AdminMailPagePaths
{
    public const string Origin = "https://admin.core.dealoware.com";
    public const string Host = "admin.core.dealoware.com";
    public const string ResetConfirmPath = "/admin/reset/confirm";
    public const string BootstrapPath = "/admin/bootstrap";
    public const int MaxTokenLength = 512;

    public static string Build(MailLinkKind kind, string token)
    {
        if (token is null || token.Length == 0 || token.Length > MaxTokenLength)
            throw new ArgumentException("Token is required and must be 512 characters or fewer.", nameof(token));

        var path = PathFor(kind);
        var url = AttachToken(Origin + path, token);
        EnsureBuiltLink(url, path);
        return url;
    }

    public static string PathFor(MailLinkKind kind) => kind switch
    {
        MailLinkKind.Reset => ResetConfirmPath,
        MailLinkKind.Bootstrap => BootstrapPath,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    /// <summary>
    /// Token in the fragment (b6194918 item 6). Query strings are banned (r5 / C4.3).
    /// </summary>
    private static string AttachToken(string pageUrl, string token)
        => pageUrl + "#token=" + Uri.EscapeDataString(token);

    private static void EnsureBuiltLink(string url, string expectedPath)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("Mail link could not be built.");

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
            throw new InvalidOperationException("Mail links must use https.");
        if (!string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Mail links must use host " + Host + ".");
        if (!uri.IsDefaultPort)
            throw new InvalidOperationException("Mail links must not include a port.");
        if (!string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidOperationException("Mail links must not include userinfo.");
        if (url.Contains('?', StringComparison.Ordinal) || !string.IsNullOrEmpty(uri.Query))
            throw new InvalidOperationException("Mail links must not include a query string.");
        if (CountTokenFragment(url) != 1 || !uri.Fragment.StartsWith("#token=", StringComparison.Ordinal))
            throw new InvalidOperationException("Mail links must include exactly one #token= fragment.");
        if (!string.Equals(uri.AbsolutePath, expectedPath, StringComparison.Ordinal))
            throw new InvalidOperationException("Mail link path must be " + expectedPath + ".");
        if (!string.Equals(uri.GetLeftPart(UriPartial.Authority), Origin, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Mail links must use origin " + Origin + ".");
    }

    private static int CountTokenFragment(string url)
    {
        const string marker = "#token=";
        var count = 0;
        for (var i = 0; (i = url.IndexOf(marker, i, StringComparison.Ordinal)) >= 0; i += marker.Length)
            count++;
        return count;
    }
}
