using System.Globalization;
using Dealoware.Domain.Negotiations;
using Microsoft.AspNetCore.WebUtilities;

namespace Dealoware.Api.Admin;

/// <summary>
/// TD-ADM-177: the part of a list page's query that may come back after
/// sign-in. Only the list query keys the read API knows
/// (<see cref="AdminListQuery"/>) are kept, each one checked again here and
/// written out anew in a fixed order. An unknown key, a key given twice and a
/// value that does not pass are dropped; the path itself stays. Nothing of
/// the raw text is echoed: every kept value is re-encoded.
/// Only the four entity lists take a query. The audit list takes none until
/// its viewer (T07) names its own keys.
/// </summary>
public static class AdminReturnQuery
{
    private delegate bool Check(string raw, out string kept);

    private static readonly (string Key, Check Check)[] Common =
    [
        ("q", Text(AdminListQuery.MaxQueryLength)),
        ("dir", OneOf("asc", "desc")),
        ("offset", Whole(0, int.MaxValue)),
        ("limit", Whole(1, AdminListQuery.MaxLimit)),
        ("includeDeleted", OneOf("true", "false")),
        ("participant", Text(AdminListQuery.MaxFilterLength)),
        ("createdFrom", Instant),
        ("createdTo", Instant),
        ("updatedFrom", Instant),
        ("updatedTo", Instant)
    ];

    private static readonly Dictionary<string, (string Key, Check Check)[]> Lists = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/admin/participants"] =
        [
            ("sort", SortKey(AdminListQuery.ParticipantSortKeys)),
            .. Common
        ],
        ["/admin/artifacts"] =
        [
            ("sort", SortKey(AdminListQuery.ArtifactSortKeys)),
            .. Common,
            ("artifact", Id)
        ],
        ["/admin/negotiations"] =
        [
            ("sort", SortKey(AdminListQuery.NegotiationSortKeys)),
            .. Common,
            ("status", OneOf(Enum.GetNames<NegotiationStatus>())),
            ("artifact", Id),
            ("negotiationId", Id)
        ],
        ["/admin/offers"] =
        [
            ("sort", SortKey(AdminListQuery.OfferSortKeys)),
            .. Common,
            ("status", OneOf(Enum.GetNames<OfferStatus>())),
            ("artifact", Id),
            ("negotiationId", Id),
            ("amountMin", Amount),
            ("amountMax", Amount)
        ]
    };

    /// <summary>The keys a list path keeps, in the order they are written out. Empty for any other path.</summary>
    public static IReadOnlyList<string> KeysFor(string listPath)
        => Lists.TryGetValue(listPath, out var keys) ? keys.Select(k => k.Key).ToArray() : [];

    /// <summary>
    /// The checked query for a list path, as "?key=value&amp;…", or "" when
    /// nothing is kept (or the path takes no query). <paramref name="rawQuery"/>
    /// is the text after "?", still percent-encoded; each value is decoded once.
    /// </summary>
    public static string Filter(string listPath, string rawQuery)
    {
        if (string.IsNullOrEmpty(rawQuery) || !Lists.TryGetValue(listPath, out var keys))
            return string.Empty;

        Dictionary<string, Microsoft.Extensions.Primitives.StringValues> parsed;
        try
        {
            parsed = QueryHelpers.ParseQuery(rawQuery);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or UriFormatException)
        {
            return string.Empty;
        }

        var kept = new List<string>(keys.Length);
        foreach (var (key, check) in keys)
        {
            // Exact spelling only, and once only: anything else is not a known key.
            var match = parsed.Keys.Where(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (match.Length != 1 || !string.Equals(match[0], key, StringComparison.Ordinal))
                continue;
            var values = parsed[key];
            if (values.Count != 1 || values[0] is not { } raw)
                continue;
            if (check(raw, out var value))
                kept.Add(key + "=" + Uri.EscapeDataString(value));
        }

        return kept.Count == 0 ? string.Empty : "?" + string.Join('&', kept);
    }

    private static Check Text(int maxLength) => (string raw, out string kept) =>
    {
        kept = raw.Trim();
        if (kept.Length == 0 || kept.Length > maxLength)
            return false;
        foreach (var ch in kept)
        {
            var category = char.GetUnicodeCategory(ch);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate
                or UnicodeCategory.OtherNotAssigned or UnicodeCategory.PrivateUse)
            {
                return false;
            }
        }

        return true;
    };

    private static Check OneOf(params string[] allowed) => (string raw, out string kept) =>
    {
        kept = allowed.FirstOrDefault(a => string.Equals(a, raw, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        return kept.Length > 0;
    };

    private static Check SortKey(HashSet<string> allowed) => (string raw, out string kept) =>
    {
        kept = allowed.FirstOrDefault(a => string.Equals(a, raw, StringComparison.Ordinal)) ?? string.Empty;
        return kept.Length > 0;
    };

    private static Check Whole(int min, int max) => (string raw, out string kept) =>
    {
        kept = string.Empty;
        if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < min || n > max)
            return false;
        kept = n.ToString(CultureInfo.InvariantCulture);
        return true;
    };

    private static bool Id(string raw, out string kept)
    {
        kept = string.Empty;
        if (!Guid.TryParseExact(raw, "D", out var id))
            return false;
        kept = id.ToString("D");
        return true;
    }

    private static bool Amount(string raw, out string kept)
    {
        kept = string.Empty;
        if (!decimal.TryParse(raw, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount))
            return false;
        kept = amount.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    private static bool Instant(string raw, out string kept)
    {
        kept = string.Empty;
        if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at))
            return false;
        kept = at.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        return true;
    }
}
