using System.Text.RegularExpressions;

namespace Dealoware.Api.Tests;

/// <summary>
/// Reads the anti-forgery request token the way a browser page does: from the
/// <c>dw-csrf</c> meta of a signed-out admin page. The matching cookie comes
/// with the same response. Tests that post to the admin sign-in routes get
/// their token here.
/// </summary>
internal static partial class AdminPageAntiForgery
{
    public static async Task<string> ReadTokenAsync(HttpResponseMessage response)
        => ReadToken(await response.Content.ReadAsStringAsync());

    public static string ReadToken(string html)
    {
        var meta = Assert.Single(CsrfMeta().Matches(html));
        return meta.Groups[1].Value;
    }

    [GeneratedRegex("<meta name=\"dw-csrf\" content=\"([0-9A-F]{64})\">")]
    private static partial Regex CsrfMeta();
}
