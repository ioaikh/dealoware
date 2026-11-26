using Dealoware.Api.Admin;

namespace Dealoware.Api.Tests;

/// <summary>r3 §5 / §10 C6 remapped return-path allowlist.</summary>
public class AdminReturnPathTests
{
    [Theory]
    [InlineData("/admin/")]
    [InlineData("/admin")]
    [InlineData("/admin/participants")]
    [InlineData("/admin/artifacts")]
    [InlineData("/admin/negotiations")]
    [InlineData("/admin/offers")]
    [InlineData("/admin/audit")]
    [InlineData("/admin/participants/11111111-1111-1111-1111-111111111111")]
    [InlineData("/admin/audit/22222222-2222-2222-2222-222222222222")]
    public void TdAdm023_C6_AllowsRemappedInventory(string input)
    {
        Assert.True(AdminReturnPath.TryValidate(input, out var path));
        Assert.StartsWith("/admin/", path, StringComparison.Ordinal);
        Assert.Equal(
            "https://admin.core.dealoware.com" + path,
            AdminReturnPath.ToFixedOriginLocation(path));
    }

    [Theory]
    [InlineData("/admin/sign-in")]
    [InlineData("/admin/sign-in/code")]
    [InlineData("/admin/setup/authenticator")]
    [InlineData("/admin/setup/recovery-codes")]
    [InlineData("/admin/settings/security")]
    [InlineData("/admin/sign-out")]
    [InlineData("/admin/auth/app.js")]
    [InlineData("/artifacts")]
    [InlineData("//admin/")]
    [InlineData("/admin/../participants")]
    [InlineData("/admin/%2e%2e")]
    [InlineData("https://evil.example/admin/")]
    public void TdAdm023_C6_RejectsAuthAndOffAllowlist(string input)
    {
        Assert.False(AdminReturnPath.TryValidate(input, out _));
        Assert.Equal("/admin/", AdminReturnPath.Resolve(input));
    }

    [Fact]
    public void TdAdm023_C6_NormalizesBareAdminToStats()
    {
        Assert.True(AdminReturnPath.TryValidate("/admin", out var path));
        Assert.Equal("/admin/", path);
    }

    private const string Id = "0b6c1f0e-3a54-4c58-9f0e-6f1d2c3b4a59";

    /// <summary>TD-ADM-177: a list's known query keys come back, checked again and re-encoded; nothing else does.</summary>
    [Theory]
    // Known keys kept, in a fixed order, values in their canonical spelling; unknown keys dropped.
    [InlineData(
        "/admin/offers?status=open&sort=amount&dir=ASC&limit=50&offset=100&amountMin=1.5&negotiationId=" + Id + "&bogus=1",
        "/admin/offers?sort=amount&dir=asc&offset=100&limit=50&status=Open&negotiationId=" + Id + "&amountMin=1.5")]
    [InlineData("/admin/participants?next=/admin/sign-in", "/admin/participants")]
    [InlineData("/admin/participants?returnPath=https%3A%2F%2Fevil.example%2F", "/admin/participants")]
    // A known key with a bad value is dropped; the rest stays.
    [InlineData("/admin/offers?limit=999&status=Open", "/admin/offers?status=Open")]
    [InlineData("/admin/offers?limit=0&offset=-1&offset2=3", "/admin/offers")]
    // A sort the API takes but no heading of the page offers is dropped: the way back keeps what the page reads.
    [InlineData("/admin/negotiations?status=Withdrawn&sort=participant", "/admin/negotiations")]
    [InlineData("/admin/negotiations?sort=status&dir=asc", "/admin/negotiations?sort=status&dir=asc")]
    [InlineData("/admin/offers?sort=createdAt", "/admin/offers")]
    [InlineData("/admin/participants?sort=displayName", "/admin/participants")]
    [InlineData("/admin/participants?sort=amount", "/admin/participants")]
    [InlineData("/admin/participants?includeDeleted=yes", "/admin/participants")]
    [InlineData("/admin/artifacts?artifact=not-a-guid&includeDeleted=TRUE", "/admin/artifacts?includeDeleted=true")]
    [InlineData("/admin/offers?amountMin=1e5&amountMax=12.50", "/admin/offers?amountMax=12.50")]
    [InlineData("/admin/participants?createdFrom=2026-10-05T19:55:00-04:00", "/admin/participants?createdFrom=2026-10-05T23%3A55%3A00Z")]
    // A status is one or more of the list's own names, comma-separated: the known ones kept, in the list's order.
    [InlineData("/admin/offers?status=withdrawn,OPEN,Deleted,open", "/admin/offers?status=Open%2CWithdrawn")]
    [InlineData("/admin/negotiations?status=Withdrawn,Accepted", "/admin/negotiations")]
    [InlineData("/admin/participants?status=suspended", "/admin/participants?status=Suspended")]
    [InlineData("/admin/artifacts?status=Open&participant=sub-1", "/admin/artifacts?participant=sub-1")]
    // A date filter as the pages write it (an ET calendar date) stays a date.
    [InlineData("/admin/negotiations?createdFrom=2026-10-05&updatedTo=2026-13-40", "/admin/negotiations?createdFrom=2026-10-05")]
    // A date outside the years 1900 to 2999 is dropped (an end of day past the last date cannot be worked out).
    [InlineData("/admin/offers?createdTo=9999-12-31&createdFrom=0001-01-01&updatedFrom=1899-12-31&updatedTo=2999-12-31", "/admin/offers?updatedTo=2999-12-31")]
    [InlineData("/admin/participants?createdFrom=1900-01-01&createdTo=9999-12-31T23:59:59Z", "/admin/participants?createdFrom=1900-01-01")]
    // A key given twice, or spelled another way, is not a known key.
    [InlineData("/admin/participants?q=a&q=b", "/admin/participants")]
    [InlineData("/admin/participants?Q=a", "/admin/participants")]
    // Text is decoded once and written out encoded: no separator, quote or control character survives raw.
    [InlineData("/admin/participants?q=a%20b%26c%3Dd", "/admin/participants?q=a%20b%26c%3Dd")]
    [InlineData("/admin/participants?q=%2F%2Fevil.example", "/admin/participants?q=%2F%2Fevil.example")]
    [InlineData("/admin/participants?q=%3Cscript%3E", "/admin/participants?q=%3Cscript%3E")]
    [InlineData("/admin/participants?q=a%0Ab", "/admin/participants")]
    [InlineData("/admin/participants?q=a%E2%80%8Bb", "/admin/participants")]
    // Only the four entity lists take a query.
    [InlineData("/admin/?status=Open", "/admin/")]
    [InlineData("/admin/audit?action=sign_out", "/admin/audit")]
    [InlineData("/admin/offers/" + Id + "?status=Open", "/admin/offers/" + Id)]
    public void TdAdm177_KnownListQuery_IsKeptCheckedAndReEncoded_UnknownIsDropped(string input, string expected)
    {
        Assert.True(AdminReturnPath.TryValidate(input, out var path));
        Assert.Equal(expected, path);
        // What is kept is accepted again as it stands: the cookie is read back through the same rule.
        Assert.True(AdminReturnPath.TryValidate(path, out var again));
        Assert.Equal(path, again);
    }

    [Theory]
    [InlineData("/admin/participants%3Fq=1")]
    [InlineData("/admin/participants?q=1#x")]
    [InlineData("/admin/sign-in?q=1")]
    [InlineData("//evil.example/admin/participants?q=1")]
    [InlineData("/admin/settings/security?tab=1")]
    public void TdAdm177_AQueryNeverRescuesAPathOffTheList(string input)
        => Assert.Equal("/admin/", AdminReturnPath.Resolve(input));

    [Fact]
    public void TdAdm177_TooLongAfterReEncoding_FallsBackToStats()
    {
        // 100 characters is the longest search text; each one grows when encoded, and still fits.
        var longest = new string('é', AdminListQuery.MaxQueryLength);
        Assert.True(AdminReturnPath.TryValidate("/admin/participants?q=" + Uri.EscapeDataString(longest), out var path));
        Assert.True(path.Length <= AdminReturnPath.MaxLength);

        Assert.Equal("/admin/", AdminReturnPath.Resolve("/admin/participants?q=" + new string('a', AdminReturnPath.MaxLength)));
    }

    [Fact]
    public void TdAdm177_EveryListNamesItsKeys_TheAuditListNone()
    {
        Assert.Equal("sort", AdminReturnQuery.KeysFor("/admin/participants")[0]);
        Assert.Contains("amountMin", AdminReturnQuery.KeysFor("/admin/offers"));
        Assert.DoesNotContain("amountMin", AdminReturnQuery.KeysFor("/admin/negotiations"));
        // Participants filter by Active / Suspended; they have no participant filter, and Artifacts no artifact filter.
        Assert.Contains("status", AdminReturnQuery.KeysFor("/admin/participants"));
        Assert.DoesNotContain("participant", AdminReturnQuery.KeysFor("/admin/participants"));
        Assert.DoesNotContain("artifact", AdminReturnQuery.KeysFor("/admin/artifacts"));
        Assert.DoesNotContain("status", AdminReturnQuery.KeysFor("/admin/artifacts"));
    }

    [Fact]
    public void EachListKeepsOnlyTheSortsItsHeadingsOffer()
    {
        foreach (var list in AdminListPages.All)
        {
            foreach (var sort in AdminListQuery.ParticipantSortKeys
                         .Concat(AdminListQuery.ArtifactSortKeys)
                         .Concat(AdminListQuery.NegotiationSortKeys)
                         .Concat(AdminListQuery.OfferSortKeys)
                         .Distinct(StringComparer.Ordinal))
            {
                var kept = AdminReturnQuery.Filter(list.Path, "sort=" + sort);
                Assert.Equal(list.SortKeys.Contains(sort, StringComparer.Ordinal) ? "?sort=" + sort : string.Empty, kept);
            }
        }
        Assert.Empty(AdminReturnQuery.KeysFor("/admin/audit"));
        Assert.Empty(AdminReturnQuery.KeysFor("/admin/"));
    }
}
