using System.Net;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;

namespace Dealoware.Api.Tests;

/// <summary>
/// Reads the two set-up pages the way their owner does: the authenticator
/// key off S-A6 (GET /admin/setup/authenticator) and the recovery codes off
/// S-A7 (GET /admin/setup/recovery-codes). Both used to be JSON; the server
/// now writes them into the page, and tests that need them get them here.
/// </summary>
internal static partial class AdminEnrollmentPageReader
{
    /// <summary>The key shown on S-A6, without the gaps between its groups: the base32 text an app takes.</summary>
    public static async Task<string> ReadKeyAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        return ReadKey(await response.Content.ReadAsStringAsync());
    }

    public static string ReadKey(string html)
    {
        var shown = Assert.Single(KeyElement().Matches(html));
        var groups = KeyGroup().Matches(shown.Groups["groups"].Value).Select(g => g.Groups[1].Value).ToList();
        Assert.NotEmpty(groups);
        // Groups of four, the last one possibly shorter.
        Assert.All(groups.SkipLast(1), g => Assert.Equal(AdminEnrollmentPages.KeyGroupLength, g.Length));
        Assert.InRange(groups[^1].Length, 1, AdminEnrollmentPages.KeyGroupLength);
        return string.Concat(groups);
    }

    /// <summary>The recovery codes shown on S-A7, in order.</summary>
    public static async Task<IReadOnlyList<string>> ReadRecoveryCodesAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        return ReadRecoveryCodes(await response.Content.ReadAsStringAsync());
    }

    /// <summary>Empty when the page shows no list of codes.</summary>
    public static IReadOnlyList<string> ReadRecoveryCodes(string html)
    {
        var lists = CodeList().Matches(html);
        if (lists.Count == 0)
        {
            return [];
        }

        var items = Assert.Single(lists).Groups["items"].Value;
        var codes = CodeItem().Matches(items).Select(i => i.Groups[1].Value).ToList();
        // Nothing in the list but its codes.
        Assert.Equal(items, string.Concat(codes.Select(c => $"<li>{c}</li>")));
        return codes;
    }

    /// <summary>The QR code drawn on S-A6, as its modules without the quiet zone.</summary>
    public static bool[,] ReadQr(string html)
    {
        var svg = Assert.Single(QrSvg().Matches(html));
        var side = int.Parse(svg.Groups["side"].Value);
        Assert.Equal(svg.Groups["side"].Value, svg.Groups["width"].Value);
        Assert.Equal(svg.Groups["side"].Value, svg.Groups["height"].Value);
        return QrTestReader.FromSvgPath(svg.Groups["path"].Value, side, AdminQrCode.QuietZone);
    }

    [GeneratedRegex("<span id=\"manual-key\" class=\"key\" translate=\"no\">(?<groups>(?:<span>[A-Z2-7]+</span>(?:<wbr>)?)+)</span>")]
    private static partial Regex KeyElement();

    [GeneratedRegex("<span>([A-Z2-7]+)</span>")]
    private static partial Regex KeyGroup();

    [GeneratedRegex("<ul id=\"recovery-codes-list\"[^>]*>(?<items>.*?)</ul>", RegexOptions.Singleline)]
    private static partial Regex CodeList();

    [GeneratedRegex("<li>([^<]+)</li>")]
    private static partial Regex CodeItem();

    [GeneratedRegex(
        "<svg id=\"authenticator-qr\" class=\"qr\" role=\"img\" aria-label=\"[^\"]+\" aria-describedby=\"manual-key-help\""
        + " viewBox=\"0 0 (?<side>\\d+) \\k<side>\" shape-rendering=\"crispEdges\">"
        + "<rect width=\"(?<width>\\d+)\" height=\"(?<height>\\d+)\" fill=\"#fff\"/>"
        + "<path fill=\"#000\" d=\"(?<path>[^\"]+)\"/></svg>")]
    private static partial Regex QrSvg();
}
