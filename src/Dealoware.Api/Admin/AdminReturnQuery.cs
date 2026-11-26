using System.Globalization;
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
/// <para>
/// These are exactly the keys each list page reads (<see cref="AdminListPages"/>
/// passes its own address through <see cref="Filter"/> first, so a page and
/// its way back after sign-in keep the same values). A status is one or more
/// of the list's own names, comma-separated. A date filter is either an ET
/// calendar date (yyyy-MM-dd, what the pages write) or an instant.
/// </para>
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
        ("includeDeleted", OneOf("true", "false"))
    ];

    private static readonly (string Key, Check Check)[] Dates =
    [
        ("createdFrom", DateOrInstant),
        ("createdTo", DateOrInstant),
        ("updatedFrom", DateOrInstant),
        ("updatedTo", DateOrInstant)
    ];

    private static readonly Dictionary<string, (string Key, Check Check)[]> Lists = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/admin/participants"] =
        [
            ("sort", SortKey(AdminListQuery.ParticipantSortKeys)),
            .. Common,
            ("status", Statuses(AdminReadEndpoints.ParticipantStatuses)),
            .. Dates
        ],
        ["/admin/artifacts"] =
        [
            ("sort", SortKey(AdminListQuery.ArtifactSortKeys)),
            .. Common,
            ("participant", Text(AdminListQuery.MaxFilterLength)),
            .. Dates
        ],
        ["/admin/negotiations"] =
        [
            ("sort", SortKey(AdminListQuery.NegotiationSortKeys)),
            .. Common,
            ("participant", Text(AdminListQuery.MaxFilterLength)),
            .. Dates,
            ("status", Statuses(AdminReadEndpoints.NegotiationStatuses)),
            ("artifact", Id),
            ("negotiationId", Id)
        ],
        ["/admin/offers"] =
        [
            ("sort", SortKey(AdminListQuery.OfferSortKeys)),
            .. Common,
            ("participant", Text(AdminListQuery.MaxFilterLength)),
            .. Dates,
            ("status", Statuses(AdminReadEndpoints.OfferStatuses)),
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

    /// <summary>One or more of the list's statuses, comma-separated: the known ones kept, in the list's order, once each.</summary>
    private static Check Statuses(IReadOnlyList<string> allowed) => (string raw, out string kept) =>
    {
        var named = raw.Split(',')
            .Select(part => allowed.FirstOrDefault(a => string.Equals(a, part.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Where(a => a is not null)
            .ToHashSet(StringComparer.Ordinal);
        kept = string.Join(',', allowed.Where(named.Contains));
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

    /// <summary>A calendar date as the pages write it (yyyy-MM-dd, kept as it is), or an instant, kept in UTC.</summary>
    private static bool DateOrInstant(string raw, out string kept)
    {
        if (DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            kept = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return true;
        }

        kept = string.Empty;
        if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at))
            return false;
        kept = at.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        return true;
    }
}
