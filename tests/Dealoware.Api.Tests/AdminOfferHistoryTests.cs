using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>A CoreOwner set through test config, on its own PostgreSQL database when DB_HOST is set.</summary>
public sealed class OfferHistoryWebApplicationFactory : IsolatedWebApplicationFactory
{
    public const string OwnerEmail = "owner@example.test";

    protected override string? PostgresDatabaseName => "dealoware_ci_offer_history";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("CoreOwner:Email", OwnerEmail);
        base.ConfigureWebHost(builder);
    }
}

/// <summary>
/// T08a, the offer-history test (T08 spec): no admin action changes what a
/// party sent in an offer, or removes an offer row. Every admin action, and
/// the refused version of each, runs against offers in every status, through
/// the real routes and the real session gate. After each one every offer row
/// is still in the table, and its negotiation, sender, receiver, amount,
/// currency, terms and sent time are byte-identical. Only the status (Open
/// to Cancelled, nothing else), the time of last change, the delete mark and
/// the version may differ. A refused action changes nothing at all.
/// <para>
/// Runs on SQLite, and in CI's PostgreSQL job too (.github/workflows/ci.yml).
/// The database rule that makes the history unalterable for everyone is not
/// this test; it is a later task.
/// </para>
/// </summary>
[Collection("WebAppTests")]
public partial class AdminOfferHistoryTests : IClassFixture<OfferHistoryWebApplicationFactory>
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string OwnerEmail = OfferHistoryWebApplicationFactory.OwnerEmail;

    /// <summary>Not ASCII, so "unchanged" is shown on the bytes, not on a lucky encoding.</summary>
    private const string Terms = "Net 30 — «as sent» ✓ 12½ % · naïve";

    private readonly OfferHistoryWebApplicationFactory _factory;

    public AdminOfferHistoryTests(OfferHistoryWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- the actions the owner may take ----

    public static TheoryData<string> Actions => new()
    {
        "edit-participant", "edit-artifact", "edit-negotiation-end-time",
        "close-negotiation", "expire-negotiation", "cancel-offer",
        "delete-offer", "delete-open-negotiation", "delete-closed-negotiation",
        "delete-participant", "delete-artifact"
    };

    [Theory]
    [MemberData(nameof(Actions))]
    public async Task TdAdm178_AnAdminAction_LeavesEveryOfferAsSent_AndRemovesNoRow(string action)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var before = await OffersAsync();

        var reply = await ActAsync(action, seed, session);

        Assert.True(reply.Status == 200, $"{action}: {reply.Status} {reply.Body}");
        var after = await OffersAsync();
        AssertHistoryKept(before, after, action);
        AssertOnlyItsOffersChanged(action, seed, before, after);
    }

    // ---- the refused versions ----

    public static TheoryData<string> Refusals => new()
    {
        "stale-version", "write-amount", "write-currency", "write-terms", "write-sent-time",
        "write-sender", "write-receiver", "write-negotiation", "set-accepted", "set-withdrawn",
        "set-open-again", "delete-without-confirm-token", "blocked-artifact-delete"
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public async Task TdAdm005_TdAdm178_ARefusedAction_ChangesNothingAtAll(string refusal)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var openOffer = seed.Open1.Id;
        var etag = await ETagAsync("offers", openOffer, session);
        var expected = HttpStatusCode.BadRequest;
        Func<Task<Reply>> attempt;

        switch (refusal)
        {
            case "stale-version":
            {
                // Read, let the record move on, then write with what was read.
                var stale = await ETagAsync("negotiations", seed.Open.Id, session);
                var moved = await PatchAsync("negotiations", seed.Open.Id, new { endsAt = "2031-01-01T00:00:00+00:00" }, session, stale);
                Assert.Equal(200, moved.Status);
                attempt = () => PatchAsync("negotiations", seed.Open.Id, new { status = "Closed" }, session, stale);
                expected = HttpStatusCode.Conflict;
                break;
            }
            case "write-amount":
                attempt = () => PatchAsync("offers", openOffer, new { amount = 1.00m }, session, etag);
                break;
            case "write-currency":
                attempt = () => PatchAsync("offers", openOffer, new { currency = "EUR" }, session, etag);
                break;
            case "write-terms":
                attempt = () => PatchAsync("offers", openOffer, new { terms = "Changed after sending" }, session, etag);
                break;
            case "write-sent-time":
                attempt = () => PatchAsync("offers", openOffer, new { createdAt = "2020-01-01T00:00:00+00:00" }, session, etag);
                break;
            case "write-sender":
                attempt = () => PatchAsync("offers", openOffer, new { fromParticipantId = seed.PartyB.Sub }, session, etag);
                break;
            case "write-receiver":
                attempt = () => PatchAsync("offers", openOffer, new { toParticipantId = seed.PartyA.Sub }, session, etag);
                break;
            case "write-negotiation":
                attempt = () => PatchAsync("offers", openOffer, new { negotiationId = seed.Closed.Id }, session, etag);
                break;
            case "set-accepted":
                attempt = () => PatchAsync("offers", openOffer, new { status = "Accepted" }, session, etag);
                break;
            case "set-withdrawn":
                attempt = () => PatchAsync("offers", openOffer, new { status = "Withdrawn" }, session, etag);
                break;
            case "set-open-again":
            {
                var cancelled = await ETagAsync("offers", seed.Cancelled.Id, session);
                attempt = () => PatchAsync("offers", seed.Cancelled.Id, new { status = "Open" }, session, cancelled);
                break;
            }
            case "delete-without-confirm-token":
                attempt = () => SendAsync(HttpMethod.Delete, $"/admin/api/offers/{openOffer}", session);
                expected = (HttpStatusCode)428;
                break;
            case "blocked-artifact-delete":
                attempt = () => SendAsync(HttpMethod.Post, $"/admin/api/artifacts/{seed.Artifact.Id}/delete-intent", session);
                expected = HttpStatusCode.Conflict;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(refusal));
        }

        var tables = await EntityTablesAsync();
        var before = await OffersAsync();

        var reply = await attempt();

        Assert.True(reply.Status == (int)expected, $"{refusal}: {reply.Status} {reply.Body}");
        Assert.Equal(tables, await EntityTablesAsync());
        AssertHistoryKept(before, await OffersAsync(), refusal);
    }

    // ---- every action in a row, on one set of records ----

    [Fact]
    public async Task TdAdm178_EveryActionInTurn_OnTheSameRecords_KeepsTheHistory()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var first = await OffersAsync();

        foreach (var action in Actions.Select(a => (string)a[0]))
        {
            var before = await OffersAsync();
            var reply = await ActAsync(action, seed, session);
            Assert.True(reply.Status == 200, $"{action}: {reply.Status} {reply.Body}");
            var after = await OffersAsync();
            AssertHistoryKept(before, after, action);
            AssertOnlyItsOffersChanged(action, seed, before, after);
        }

        // And from the very start: every row of the seed is there, as sent, deleted or not.
        var last = await OffersAsync();
        AssertHistoryKept(first, last, "all actions");
        Assert.All(seed.AllOffers, o => Assert.True(last.ContainsKey(o.Id)));
    }

    // ---- the source: no admin code removes offer rows ----

    private static readonly Regex[] RemovesOffers =
    [
        // Offers ... .Remove( / .RemoveRange( / .ExecuteDelete( in one statement.
        new(@"\bOffers\b[^;]*?\.(Remove|RemoveRange|ExecuteDelete|ExecuteDeleteAsync)\s*\(", RegexOptions.Singleline),
        new(@"\bSet<Offer>\(\)[^;]*?\.(Remove|RemoveRange|ExecuteDelete|ExecuteDeleteAsync)\s*\(", RegexOptions.Singleline),
        // A tracked offer handed to Remove: db.Remove(offer), context.RemoveRange(offers).
        new(@"\.Remove(Range)?\(\s*(offer|offers)\b", RegexOptions.IgnoreCase),
        // Anything marked deleted for the change tracker.
        new(@"EntityState\.Deleted"),
        // SQL, the table name bare or quoted (also as an escaped quote inside a C# string).
        new(@"DELETE\s+FROM\s+[\\""`]*offers\b", RegexOptions.IgnoreCase),
        new(@"TRUNCATE\s+(TABLE\s+)?[\\""`]*offers\b", RegexOptions.IgnoreCase)
    ];

    [Fact]
    public void TdAdm178_NoCodeUnderAdmin_RemovesRowsFromTheOffersTable()
    {
        // The check finds what it looks for.
        foreach (var sample in new[]
                 {
                     "await db.Offers.Where(o => o.Id == id).ExecuteDeleteAsync(ct);",
                     "db.Offers.Remove(offer);",
                     "db.Offers\n    .RemoveRange(list);",
                     "db.Set<Offer>().Where(o => true).ExecuteDelete();",
                     "db.Remove(offer);",
                     "db.Entry(row).State = EntityState.Deleted;",
                     "await db.Database.ExecuteSqlRawAsync(\"DELETE FROM \\\"Offers\\\" WHERE 1=1\");",
                     "TRUNCATE TABLE offers"
                 })
        {
            Assert.True(RemovesOffers.Any(r => r.IsMatch(sample)), $"The check misses: {sample}");
        }

        // What admin code does delete is not an offer: the confirm tokens, the recovery notice, sessions.
        Assert.DoesNotContain(RemovesOffers, r => r.IsMatch(
            "await db.AdminDeleteConfirmTokens.Where(t => t.EntityType == type).ExecuteDeleteAsync(cancellationToken);"));

        var src = Path.Combine(RepoRoot(), "src");
        var files = Directory.EnumerateDirectories(src, "Admin", SearchOption.AllDirectories)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.cs", SearchOption.AllDirectories))
            .ToList();
        Assert.Contains(files, f => f.EndsWith("AdminDeleteEndpoints.cs", StringComparison.Ordinal));
        Assert.Contains(files, f => f.EndsWith("AdminEditEndpoints.cs", StringComparison.Ordinal));

        var found = new List<string>();
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var regex in RemovesOffers)
            {
                foreach (Match match in regex.Matches(text))
                    found.Add($"{Path.GetRelativePath(src, file)}: {match.Value}");
            }
        }

        Assert.Empty(found);
    }

    // ---- the actions ----

    private async Task<Reply> ActAsync(string action, Seed seed, Guid session)
    {
        switch (action)
        {
            case "edit-participant":
                return await PatchAsync("participants", seed.PartyA.Id, new { displayName = "Renamed party", isActive = false },
                    session, await ETagAsync("participants", seed.PartyA.Id, session));
            case "edit-artifact":
                return await PatchAsync("artifacts", seed.Artifact.Id, new { name = "Renamed widget", description = "New description" },
                    session, await ETagAsync("artifacts", seed.Artifact.Id, session));
            case "edit-negotiation-end-time":
                return await PatchAsync("negotiations", seed.Open.Id, new { endsAt = "2032-03-04T05:06:07+00:00" },
                    session, await ETagAsync("negotiations", seed.Open.Id, session));
            case "close-negotiation":
                return await PatchAsync("negotiations", seed.ToClose.Id, new { status = "Closed" },
                    session, await ETagAsync("negotiations", seed.ToClose.Id, session));
            case "expire-negotiation":
                return await PatchAsync("negotiations", seed.ToExpire.Id, new { status = "Expired" },
                    session, await ETagAsync("negotiations", seed.ToExpire.Id, session));
            case "cancel-offer":
                return await PatchAsync("offers", seed.Open1.Id, new { status = "Cancelled" },
                    session, await ETagAsync("offers", seed.Open1.Id, session));
            case "delete-offer":
                return await DeleteAsync("offers", seed.Open2.Id, session);
            case "delete-open-negotiation":
                return await DeleteAsync("negotiations", seed.Open.Id, session);
            case "delete-closed-negotiation":
                return await DeleteAsync("negotiations", seed.Closed.Id, session);
            case "delete-participant":
                return await DeleteAsync("participants", seed.PartyC.Id, session);
            case "delete-artifact":
            {
                // An artifact can go only once no live negotiation refers to it: delete its last one first.
                var first = await DeleteAsync("negotiations", seed.OtherArtifactNegotiation.Id, session);
                Assert.Equal(200, first.Status);
                return await DeleteAsync("artifacts", seed.OtherArtifact.Id, session);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    /// <summary>
    /// That the action changed exactly the offers it names, and no other: the
    /// history check alone would also pass for an action that did nothing, or
    /// one that also cancelled or deleted offers it had no business with. The
    /// seed holds other open offers in the same negotiation and in others, so
    /// a change too wide shows here.
    /// </summary>
    private static void AssertOnlyItsOffersChanged(string action, Seed seed, IReadOnlyDictionary<Guid, OfferRow> before, IReadOnlyDictionary<Guid, OfferRow> after)
    {
        HashSet<Guid> Where(Func<OfferRow, bool> match) => before.Where(kv => match(kv.Value)).Select(kv => kv.Key).ToHashSet();
        HashSet<Guid> OpenIn(Guid negotiation) => Where(o => o.NegotiationId == negotiation && o.DeletedAt is null && o.Status == nameof(OfferStatus.Open));
        HashSet<Guid> LiveIn(Guid negotiation) => Where(o => o.NegotiationId == negotiation && o.DeletedAt is null);
        HashSet<Guid> Just(Guid id) => Where(o => false).Append(id).ToHashSet();

        var (expected, deletes) = action switch
        {
            "close-negotiation" => (OpenIn(seed.ToClose.Id), false),
            "expire-negotiation" => (OpenIn(seed.ToExpire.Id), false),
            "cancel-offer" => (Just(seed.Open1.Id), false),
            "delete-offer" => (Just(seed.Open2.Id), true),
            "delete-open-negotiation" => (LiveIn(seed.Open.Id), true),
            "delete-closed-negotiation" => (LiveIn(seed.Closed.Id), true),
            "delete-participant" => (LiveIn(seed.WithPartyC.Id), true),
            "delete-artifact" => (LiveIn(seed.OtherArtifactNegotiation.Id), true),
            // The edits of a participant, an artifact and an end time change no offer at all.
            _ => (new HashSet<Guid>(), false)
        };

        var changed = before.Keys
            .Where(id => before[id].Status != after[id].Status || before[id].DeletedAt != after[id].DeletedAt)
            .ToHashSet();
        Assert.True(
            expected.SetEquals(changed),
            $"{action}: expected these offers to change: [{string.Join(", ", expected.Order())}]; changed: [{string.Join(", ", changed.Order())}]");

        if (action.StartsWith("delete-", StringComparison.Ordinal) || action is "close-negotiation" or "expire-negotiation" or "cancel-offer")
            Assert.NotEmpty(expected);
        foreach (var id in expected)
        {
            if (deletes)
            {
                Assert.NotNull(after[id].DeletedAt);
            }
            else
            {
                Assert.Equal(nameof(OfferStatus.Cancelled), after[id].Status);
                Assert.Equal(before[id].DeletedAt, after[id].DeletedAt);
            }
        }
    }

    /// <summary>
    /// The heart of the test. Every row that was there is there; what the
    /// party sent is byte-identical; the status moved Open to Cancelled or not
    /// at all; a deleted row stays deleted; no row was added.
    /// </summary>
    private static void AssertHistoryKept(IReadOnlyDictionary<Guid, OfferRow> before, IReadOnlyDictionary<Guid, OfferRow> after, string what)
    {
        Assert.True(before.Count == after.Count, $"{what}: {before.Count} offer rows before, {after.Count} after.");
        foreach (var (id, was) in before)
        {
            Assert.True(after.TryGetValue(id, out var now), $"{what}: offer {id} is gone from the table.");
            Assert.True(was.AsSent == now!.AsSent, $"{what}: offer {id} changed what was sent.\n  before {was.AsSent}\n  after  {now.AsSent}");
            Assert.True(
                was.Status == now.Status || (was.Status == nameof(OfferStatus.Open) && now.Status == nameof(OfferStatus.Cancelled)),
                $"{what}: offer {id} went from {was.Status} to {now.Status}.");
            if (was.DeletedAt is not null)
                Assert.Equal(was.DeletedAt, now.DeletedAt);
        }
    }

    // ---- the seed: offers in every status, one already deleted ----

    private sealed record Seed(
        Participant PartyA,
        Participant PartyB,
        Participant PartyC,
        Artifact Artifact,
        Artifact OtherArtifact,
        Negotiation Open,
        Negotiation ToClose,
        Negotiation ToExpire,
        Negotiation Closed,
        Negotiation WithPartyC,
        Negotiation OtherArtifactNegotiation,
        Offer Open1,
        Offer Open2,
        Offer Cancelled,
        IReadOnlyList<Offer> AllOffers);

    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var tag = Guid.NewGuid().ToString("N")[..8];

        var a = Participant.Create($"{tag}-Alpha");
        var b = Participant.Create($"{tag}-Beta");
        var c = Participant.Create($"{tag}-Gamma");
        db.Participants.AddRange(a, b, c);

        var artifact = Artifact.Create(a.Sub, [SubjectEntity.Create($"{tag}-Widget", "widget")], "sell");
        var other = Artifact.Create(b.Sub, [SubjectEntity.Create($"{tag}-Gadget", "gadget")], "sell");
        var ofC = Artifact.Create(c.Sub, [SubjectEntity.Create($"{tag}-Thing", "thing")], "sell");
        db.Artifacts.AddRange(artifact, other, ofC);

        var open = NewNegotiation(artifact, a, b);
        var toClose = NewNegotiation(artifact, a, b);
        var toExpire = NewNegotiation(artifact, b, a);
        var closed = NewNegotiation(artifact, a, b);
        var withC = NewNegotiation(ofC, c, a);
        var otherNegotiation = NewNegotiation(other, b, a);
        db.Negotiations.AddRange(open, toClose, toExpire, closed, withC, otherNegotiation);

        // Every status on one open negotiation, and one offer already deleted.
        var open1 = NewOffer(open, a, b, 100.10m);
        var open2 = NewOffer(open, b, a, 200.20m);
        // Open offers the actions on open1 and open2 must leave alone: one beside them, more in other negotiations.
        var open3 = NewOffer(open, a, b, 250.25m);
        var accepted = NewOffer(open, b, a, 300.30m);
        accepted.Accept(a.Sub);
        var declined = NewOffer(open, a, b, 40m);
        declined.Decline(b.Sub);
        var withdrawn = NewOffer(open, b, a, 50m);
        withdrawn.Withdraw(b.Sub);
        var superseded = NewOffer(open, a, b, 60m);
        superseded.Supersede();
        var cancelled = NewOffer(open, b, a, 70m);
        cancelled.Cancel();
        var deleted = NewOffer(open, a, b, 80.08m);

        var closing = new[] { NewOffer(toClose, a, b, 1.01m), NewOffer(toClose, b, a, 2.02m) };
        var expiring = new[] { NewOffer(toExpire, b, a, 3.03m) };
        var closedOffers = new[] { NewOffer(closed, a, b, 4.04m), NewOffer(closed, b, a, 5.05m) };
        closedOffers[0].Decline(b.Sub);
        closedOffers[1].Cancel();
        var ofCOffers = new[] { NewOffer(withC, c, a, 6.06m) };
        var otherOffers = new[] { NewOffer(otherNegotiation, b, a, 7.07m) };
        closed.Close();

        var all = new List<Offer> { open1, open2, open3, accepted, declined, withdrawn, superseded, cancelled, deleted };
        all.AddRange(closing);
        all.AddRange(expiring);
        all.AddRange(closedOffers);
        all.AddRange(ofCOffers);
        all.AddRange(otherOffers);
        db.Offers.AddRange(all);
        await db.SaveChangesAsync();

        var at = DateTimeOffset.UtcNow;
        await db.Offers.Where(o => o.Id == deleted.Id).ExecuteUpdateAsync(s => s.SetProperty(o => o.DeletedAt, at));

        return new Seed(a, b, c, artifact, other, open, toClose, toExpire, closed, withC, otherNegotiation,
            open1, open2, cancelled, all);
    }

    private static Negotiation NewNegotiation(Artifact artifact, Participant a, Participant b)
    {
        var (negotiation, errors) = Negotiation.Create(
            artifact.Id, a.Sub, b.Sub, "sell", "buy",
            new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2030, 6, 1, 9, 0, 0, TimeSpan.Zero));
        Assert.Empty(errors);
        return negotiation!;
    }

    private static Offer NewOffer(Negotiation negotiation, Participant from, Participant to, decimal amount)
    {
        var (offer, errors) = Offer.Create(negotiation.Id, from.Sub, to.Sub, amount, "usd", Terms);
        Assert.Empty(errors);
        return offer!;
    }

    // ---- readings ----

    /// <summary>One offer row: what was sent, as one string of exact values, and what may change.</summary>
    private sealed record OfferRow(Guid NegotiationId, string AsSent, string Status, DateTimeOffset? DeletedAt)
    {
        public override string ToString() => $"{AsSent}|{Status}|{DeletedAt?.UtcTicks}";
    }

    /// <summary>Every row of the offers table, deleted or not.</summary>
    private async Task<IReadOnlyDictionary<Guid, OfferRow>> OffersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var rows = await db.Offers.AsNoTracking().ToListAsync();
        return rows
            .OrderBy(o => o.Id)
            .ToDictionary(
                o => o.Id,
                o => new OfferRow(
                    o.NegotiationId,
                    string.Join(
                        '|',
                        o.NegotiationId.ToString("N"),
                        o.FromParticipantId,
                        o.ToParticipantId,
                        o.Amount?.ToString(CultureInfo.InvariantCulture) ?? "(null)",
                        o.Currency ?? "(null)",
                        o.Terms is null ? "(null)" : Convert.ToHexString(Encoding.UTF8.GetBytes(o.Terms)),
                        o.CreatedAt.UtcTicks.ToString(CultureInfo.InvariantCulture),
                        o.CreatedAt.Offset.Ticks.ToString(CultureInfo.InvariantCulture)),
                    o.Status.ToString(),
                    o.DeletedAt));
    }

    /// <summary>Every row of the four entity tables, every column, as one string.</summary>
    private async Task<string> EntityTablesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var participants = (await db.Participants.AsNoTracking().ToListAsync())
            .Select(p => new { p.Id, p.Sub, p.DisplayName, p.LoginEmail, p.ContactEmail, p.IsActive, p.CreatedAt, p.UpdatedAt, p.DeletedAt, p.Version })
            .OrderBy(p => p.Id);
        var artifacts = (await db.Artifacts.AsNoTracking().Include(a => a.Entities).ToListAsync())
            .Select(a => new
            {
                a.Id, a.OwnerParticipantId, a.Intent, a.CreatedAt, a.UpdatedAt, a.DeletedAt, a.Version,
                Entities = a.Entities.OrderBy(e => e.Id).Select(e => new { e.Id, e.Name, e.Description })
            })
            .OrderBy(a => a.Id);
        var negotiations = (await db.Negotiations.AsNoTracking().ToListAsync())
            .Select(n => new { n.Id, n.ArtifactId, n.PartyAParticipantId, n.PartyBParticipantId, n.Status, n.StartsAt, n.EndsAt, n.CreatedAt, n.UpdatedAt, n.DeletedAt, n.Version })
            .OrderBy(n => n.Id);
        var offers = (await db.Offers.AsNoTracking().ToListAsync())
            .Select(o => new { o.Id, o.NegotiationId, o.FromParticipantId, o.ToParticipantId, o.Status, o.Amount, o.Currency, o.Terms, o.CreatedAt, o.UpdatedAt, o.DeletedAt, o.Version })
            .OrderBy(o => o.Id);
        return JsonSerializer.Serialize(new { participants, artifacts, negotiations, offers });
    }

    // ---- requests ----

    private sealed record Reply(int Status, string Body, string? ETag);

    private async Task<Guid> NewSessionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(OwnerEmail, "testhmac-not-an-ip", scope.ServiceProvider.GetRequiredService<IAdminClock>().UtcNow);
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private async Task<string> ETagAsync(string type, Guid id, Guid session)
    {
        var reply = await SendAsync(HttpMethod.Get, $"/admin/api/{type}/{id}", session);
        Assert.True(reply.Status == 200 && reply.ETag is not null, $"GET {type}/{id}: {reply.Status} {reply.Body}");
        return reply.ETag!;
    }

    private Task<Reply> PatchAsync(string type, Guid id, object body, Guid session, string ifMatch)
        => SendAsync(HttpMethod.Patch, $"/admin/api/{type}/{id}", session, JsonSerializer.Serialize(body), ifMatch);

    /// <summary>The whole confirm path: the intent, then the delete with its token.</summary>
    private async Task<Reply> DeleteAsync(string type, Guid id, Guid session)
    {
        var intent = await SendAsync(HttpMethod.Post, $"/admin/api/{type}/{id}/delete-intent", session);
        if (intent.Status != 200)
            return intent;
        var token = JsonDocument.Parse(intent.Body).RootElement.GetProperty("confirmToken").GetString();
        return await SendAsync(HttpMethod.Delete, $"/admin/api/{type}/{id}", session, confirmToken: token);
    }

    private async Task<Reply> SendAsync(HttpMethod method, string path, Guid session, string? body = null, string? ifMatch = null, string? confirmToken = null)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Remove("Origin");

        using var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        var cookies = $"{AdminSessionCookie.Name}={session:D}";
        if (method != HttpMethod.Get)
        {
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            cookies += $"; {AdminAntiForgeryCookie.Name}={nonce}";
            request.Headers.TryAddWithoutValidation(
                AdminAntiForgeryCookie.HeaderName,
                _factory.Services.GetRequiredService<AdminAntiForgeryService>().ComputeRequestToken(nonce));
            request.Headers.TryAddWithoutValidation("Origin", AdminHostOptions.ProductionAdminOrigin);
        }

        request.Headers.TryAddWithoutValidation("Cookie", cookies);
        if (ifMatch is not null)
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        if (confirmToken is not null)
            request.Headers.TryAddWithoutValidation(AdminDeleteEndpoints.ConfirmTokenHeader, confirmToken);
        if (body is not null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request);
        var etag = response.Headers.TryGetValues("ETag", out var values) ? values.Single() : null;
        return new Reply((int)response.StatusCode, await response.Content.ReadAsStringAsync(), etag);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
