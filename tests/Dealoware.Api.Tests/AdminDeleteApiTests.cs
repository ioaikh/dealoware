using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
public sealed class DeleteWebApplicationFactory : IsolatedWebApplicationFactory
{
    public const string OwnerEmail = "owner@example.test";

    protected override string? PostgresDatabaseName => "dealoware_ci_delete";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("CoreOwner:Email", OwnerEmail);
        base.ConfigureWebHost(builder);
    }
}

/// <summary>
/// T06a, A7 Step 10: confirm-before-delete and soft-delete cascades, through
/// the real routes and the real session gate. Each test names the test
/// design case (TD-ADM-*) it covers.
/// <para>
/// The party-action, token-race and transaction tests are in
/// AdminDeleteApiTests.Races.cs.
/// </para>
/// </summary>
[Collection("WebAppTests")]
public partial class AdminDeleteApiTests : IClassFixture<DeleteWebApplicationFactory>
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string OwnerEmail = DeleteWebApplicationFactory.OwnerEmail;

    /// <summary>Not ASCII, so "unchanged" is shown on the bytes, not on a lucky encoding.</summary>
    private const string TermsAsSent = "Net 30 — «as sent» ✓ 12½ % · naïve";

    private static readonly string[] Types =
        [AdminDeleteEndpoints.Participants, AdminDeleteEndpoints.Artifacts, AdminDeleteEndpoints.Negotiations, AdminDeleteEndpoints.Offers];

    private readonly DeleteWebApplicationFactory _factory;

    public AdminDeleteApiTests(DeleteWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- TD-ADM-090: the intent, the token, the blind DELETE, cancel ------------------

    // TD-ADM-090 step 1: a DELETE without a token is refused, and nothing is deleted.
    [Theory]
    [MemberData(nameof(EveryType))]
    public async Task TdAdm090_BlindDelete_NoToken_428_NothingDeleted(string type)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var id = seed.DeletableId(type);
        var auditBefore = await AuditCountAsync();

        using var blind = await DeleteAsync(type, id, session, token: null);
        using var blank = await DeleteAsync(type, id, session, token: "   ");

        foreach (var response in new[] { blind, blank })
        {
            Assert.Equal((HttpStatusCode)428, response.StatusCode);
            Assert.Equal("""{"error":"PreconditionRequired"}""", await response.Content.ReadAsStringAsync());
        }

        await AssertLiveAsync(type, id);
        Assert.Equal(auditBefore, await AuditCountAsync());
    }

    // TD-ADM-090 steps 2 and 4: the intent names the record and what goes with it, hands
    // out a token, and changes nothing; not confirming it (cancel) writes no audit row.
    [Theory]
    [MemberData(nameof(EveryType))]
    public async Task TdAdm090_Intent_NamesTheRecordAndCascade_ChangesNothing_NoAudit(string type)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var id = seed.DeletableId(type);
        var auditBefore = await AuditCountAsync();

        var intent = await IntentAsync(type, id, session);

        Assert.Equal(type, intent.Body.GetProperty("entityType").GetString());
        Assert.Equal(id, intent.Body.GetProperty("id").GetGuid());
        Assert.Matches("^[0-9a-f]{64}$", intent.Token);
        Assert.Equal(Clock().UtcNow.AddMinutes(5), intent.Body.GetProperty("expiresAt").GetDateTimeOffset());
        var identity = intent.Body.GetProperty("identity");
        switch (type)
        {
            case "participants":
                Assert.Equal(seed.PartyA.DisplayName, identity.GetProperty("displayName").GetString());
                Assert.True(identity.GetProperty("isActive").GetBoolean());
                break;
            case "artifacts":
                Assert.Equal(seed.OrphanArtifact.Entities[0].Name, identity.GetProperty("name").GetString());
                break;
            case "negotiations":
                Assert.Equal("Open", identity.GetProperty("status").GetString());
                Assert.True(identity.GetProperty("isOpen").GetBoolean());
                break;
            default:
                Assert.Equal("Open", identity.GetProperty("status").GetString());
                Assert.Equal(100.10m, identity.GetProperty("amount").GetDecimal());
                Assert.Equal("USD", identity.GetProperty("currency").GetString());
                Assert.Equal(seed.Open.Id, identity.GetProperty("negotiationId").GetGuid());
                break;
        }

        // The token is stored as its hash only, bound to this session and record.
        var stored = await ReadAsync(db => db.AdminDeleteConfirmTokens.AsNoTracking().SingleAsync(t => t.EntityId == id));
        Assert.Equal(Sha256Hex(intent.Token), stored.TokenHash);
        Assert.NotEqual(intent.Token, stored.TokenHash);
        Assert.Equal(session, stored.SessionId);
        Assert.Equal(type, stored.EntityType);
        Assert.Null(stored.ConsumedAt);

        // Cancel: the owner never confirms. Nothing changed, nothing audited.
        await AssertLiveAsync(type, id);
        Assert.Equal(auditBefore, await AuditCountAsync());
        Assert.Equal(0, await ReadAsync(db => db.AdminAuditLog.CountAsync(a => a.Action == AdminAuditActions.EntityDelete && a.EntityId == id)));
        Assert.Equal("no-store", intent.CacheControl);
    }

    // TD-ADM-090 step 5: a token is used once. The replay is refused and deletes nothing more.
    [Fact]
    public async Task TdAdm090_TokenReplay_SecondUse409_NothingMore()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("offers", seed.OpenOffers[0].Id, session);

        using var first = await DeleteAsync("offers", seed.OpenOffers[0].Id, session, intent.Token);
        await OkAsync(first);
        var audits = await AuditCountAsync();

        using var replay = await DeleteAsync("offers", seed.OpenOffers[0].Id, session, intent.Token);

        await AssertTokenInvalidAsync(replay);
        Assert.Equal(audits, await AuditCountAsync());
        Assert.NotNull(await ReadAsync(db => db.AdminDeleteConfirmTokens.AsNoTracking()
            .Where(t => t.EntityId == seed.OpenOffers[0].Id).Select(t => t.ConsumedAt).SingleAsync()));
    }

    // TD-ADM-090 negative: an expired token. Good up to five minutes, not at five.
    [Fact]
    public async Task TdAdm090_ExpiredToken_409_NothingDeleted_GoodJustBefore()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var start = Clock().UtcNow;
        try
        {
            var late = await IntentAsync("offers", seed.OpenOffers[0].Id, session);
            var inTime = await IntentAsync("offers", seed.OpenOffers[1].Id, session);

            Clock().UtcNow = start.AddMinutes(5);
            using var expired = await DeleteAsync("offers", seed.OpenOffers[0].Id, session, late.Token);
            await AssertTokenInvalidAsync(expired);
            await AssertLiveAsync("offers", seed.OpenOffers[0].Id);

            Clock().UtcNow = start.AddMinutes(5).AddMilliseconds(-1);
            using var justInTime = await DeleteAsync("offers", seed.OpenOffers[1].Id, session, inTime.Token);
            await OkAsync(justInTime);
        }
        finally
        {
            Clock().UtcNow = start;
        }
    }

    // TD-ADM-090 negative: a token made in another session (stolen token, wrong actor).
    [Fact]
    public async Task TdAdm090_TokenOfAnotherSession_409_NothingDeleted()
    {
        var seed = await SeedAsync();
        var owner = await NewSessionAsync();
        var other = await NewSessionAsync();
        var intent = await IntentAsync("negotiations", seed.Open.Id, owner);

        using var stolen = await DeleteAsync("negotiations", seed.Open.Id, other, intent.Token);

        await AssertTokenInvalidAsync(stolen);
        await AssertLiveAsync("negotiations", seed.Open.Id);

        // Still good in its own session.
        using var own = await DeleteAsync("negotiations", seed.Open.Id, owner, intent.Token);
        await OkAsync(own);
    }

    // The token names one record: not another of the same type, not the same id under another type.
    [Fact]
    public async Task TdAdm090_TokenForAnotherRecord_409_NothingDeleted()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("offers", seed.OpenOffers[0].Id, session);

        using var otherOffer = await DeleteAsync("offers", seed.OpenOffers[1].Id, session, intent.Token);
        using var otherType = await DeleteAsync("negotiations", seed.OpenOffers[0].Id, session, intent.Token);
        using var unknown = await DeleteAsync("offers", seed.OpenOffers[0].Id, session, new string('0', 64));

        await AssertTokenInvalidAsync(otherOffer);
        await AssertTokenInvalidAsync(otherType);
        await AssertTokenInvalidAsync(unknown);
        await AssertLiveAsync("offers", seed.OpenOffers[0].Id);
        await AssertLiveAsync("offers", seed.OpenOffers[1].Id);
    }

    // A new intent for the same record in the same session replaces the earlier token.
    [Fact]
    public async Task TdAdm090_NewIntentForTheSameRecord_ReplacesTheEarlierToken()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var first = await IntentAsync("offers", seed.OpenOffers[0].Id, session);
        var second = await IntentAsync("offers", seed.OpenOffers[0].Id, session);
        Assert.NotEqual(first.Token, second.Token);

        using var old = await DeleteAsync("offers", seed.OpenOffers[0].Id, session, first.Token);
        await AssertTokenInvalidAsync(old);

        using var current = await DeleteAsync("offers", seed.OpenOffers[0].Id, session, second.Token);
        await OkAsync(current);
    }

    // The token travels in its header only: never a URL, never two, never another shape.
    [Fact]
    public async Task TdAdm090_TokenInTheUrl_OrMalformed_Refused_NothingDeleted()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var id = seed.OpenOffers[0].Id;
        var intent = await IntentAsync("offers", id, session);

        using var inUrl = await SendAsync(HttpMethod.Delete, $"/admin/api/offers/{id}?confirmToken={intent.Token}", session);
        using var bothPlaces = await SendAsync(
            HttpMethod.Delete, $"/admin/api/offers/{id}?confirmToken={intent.Token}", session, token: intent.Token);
        using var upper = await DeleteAsync("offers", id, session, intent.Token.ToUpperInvariant());
        using var shortened = await DeleteAsync("offers", id, session, intent.Token[..63]);
        using var two = await SendAsync(HttpMethod.Delete, $"/admin/api/offers/{id}", session, token: intent.Token, secondToken: intent.Token);

        foreach (var response in new[] { inUrl, bothPlaces, upper, shortened, two })
            await AssertGenericDenyAsync(response);
        await AssertLiveAsync("offers", id);

        // Untouched by all that, the token still works from its header.
        using var ok = await DeleteAsync("offers", id, session, intent.Token);
        await OkAsync(ok);
    }

    // ---- The gates: session, Origin, anti-forgery -------------------------------------

    // Core-owner session, Origin and anti-forgery on both routes; no exemption.
    [Theory]
    [MemberData(nameof(EveryType))]
    public async Task Gates_BothRoutes_NeedSessionOriginAndAntiForgery(string type)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var id = seed.DeletableId(type);
        var intent = await IntentAsync(type, id, session);
        var intentPath = $"/admin/api/{type}/{id}/delete-intent";
        var deletePath = $"/admin/api/{type}/{id}";

        foreach (var (method, path) in new[] { (HttpMethod.Post, intentPath), (HttpMethod.Delete, deletePath) })
        {
            using var noSession = await SendAsync(method, path, session: null, token: intent.Token);
            using var unknownSession = await SendAsync(method, path, session: Guid.NewGuid(), token: intent.Token);
            using var noOrigin = await SendAsync(method, path, session, token: intent.Token, origin: null);
            using var foreignOrigin = await SendAsync(method, path, session, token: intent.Token, origin: "https://evil.example");
            using var noAntiForgery = await SendAsync(method, path, session, token: intent.Token, antiForgery: false);

            Assert.Equal(HttpStatusCode.Unauthorized, noSession.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, unknownSession.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, noOrigin.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, foreignOrigin.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, noAntiForgery.StatusCode);
        }

        await AssertLiveAsync(type, id);
        Assert.Single(await ReadAsync(db => db.AdminDeleteConfirmTokens.Where(t => t.EntityId == id).ToListAsync()));
    }

    // A session that has not passed its second step, or is not the configured owner's: 401.
    [Fact]
    public async Task Gates_SessionWithoutTotpOrNotTheOwner_401()
    {
        var seed = await SeedAsync();
        var noTotp = await NewSessionAsync(totpVerified: false);
        var stranger = await NewSessionAsync(email: "someone-else@example.test");

        foreach (var session in new[] { noTotp, stranger })
        {
            using var intent = await SendAsync(HttpMethod.Post, $"/admin/api/offers/{seed.OpenOffers[0].Id}/delete-intent", session);
            Assert.Equal(HttpStatusCode.Unauthorized, intent.StatusCode);
        }

        Assert.Equal(0, await ReadAsync(db => db.AdminDeleteConfirmTokens.CountAsync(t => t.EntityId == seed.OpenOffers[0].Id)));
    }

    // ---- TD-ADM-091: soft delete only, never twice -----------------------------------

    // TD-ADM-091: DeletedAt set, Version moved, nothing erased; an offer's amount, currency
    // and terms byte-identical; there is no hard delete.
    [Fact]
    public async Task TdAdm091_SoftDeleteOnly_NothingErased_NoHardDelete()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var offerIds = seed.AllOffers.Where(o => o.NegotiationId == seed.Open.Id).Select(o => o.Id).ToList();
        var asSent = await OfferTermsAsync(offerIds);
        var counts = await RowCountsAsync();

        using (var hard = await SendAsync(HttpMethod.Delete, $"/admin/api/negotiations/{seed.Open.Id}?hard=true", session, token: new string('a', 64)))
            await AssertGenericDenyAsync(hard);
        using (var purge = await SendAsync(HttpMethod.Post, $"/admin/api/negotiations/{seed.Open.Id}/purge", session))
            Assert.Equal(HttpStatusCode.NotFound, purge.StatusCode);

        await DeleteThroughIntentAsync("negotiations", seed.Open.Id, session);

        // Every row is still there; the marked ones carry DeletedAt and their next Version.
        Assert.Equal(counts, await RowCountsAsync());
        var negotiation = await NegotiationAsync(seed.Open.Id);
        var now = Clock().UtcNow.ToUniversalTime();
        Assert.Equal(now.AddTicks(-(now.UtcTicks % TimeSpan.TicksPerMillisecond)), negotiation.DeletedAt);
        Assert.Equal(1u, negotiation.Version);
        Assert.Equal(NegotiationStatus.Open, negotiation.Status);
        Assert.Equal(asSent, await OfferTermsAsync(offerIds));
        foreach (var offer in seed.OpenOffers)
        {
            var stored = await OfferAsync(offer.Id);
            Assert.NotNull(stored.DeletedAt);
            Assert.Equal(OfferStatus.Open, stored.Status);
            Assert.Equal(1u, stored.Version);
        }
    }

    // TD-ADM-091: a soft-deleted record cannot be deleted again, or asked about: 404 on both routes.
    [Theory]
    [MemberData(nameof(EveryType))]
    public async Task TdAdm091_SoftDeletedOrUnknown_404_OnBothRoutes(string type)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var deleted = seed.DeletedId(type);

        foreach (var id in new[] { deleted, Guid.NewGuid() })
        {
            using var intent = await SendAsync(HttpMethod.Post, $"/admin/api/{type}/{id}/delete-intent", session);
            using var delete = await DeleteAsync(type, id, session, new string('a', 64));
            Assert.Equal(HttpStatusCode.NotFound, intent.StatusCode);
            Assert.Equal("""{"error":"NotFound"}""", await intent.Content.ReadAsStringAsync());
            // No token was ever made for it, so the DELETE is refused on the token first.
            await AssertTokenInvalidAsync(delete);
        }

        Assert.Empty(await AuditAsync(deleted));
    }

    // TD-ADM-091: a record deleted after the intent (by its cascade): 404, not a second mark.
    [Fact]
    public async Task TdAdm091_RecordDeletedByAnotherDeleteAfterTheIntent_404_NotMarkedTwice()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var offerIntent = await IntentAsync("offers", seed.OpenOffers[0].Id, session);
        await DeleteThroughIntentAsync("negotiations", seed.Open.Id, session);
        var deletedAt = (await OfferAsync(seed.OpenOffers[0].Id)).DeletedAt;

        using var again = await DeleteAsync("offers", seed.OpenOffers[0].Id, session, offerIntent.Token);

        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        var offer = await OfferAsync(seed.OpenOffers[0].Id);
        Assert.Equal(deletedAt, offer.DeletedAt);
        Assert.Equal(1u, offer.Version);
        Assert.Single(await AuditAsync(seed.OpenOffers[0].Id));
    }

    // ---- TD-ADM-092: an offer --------------------------------------------------------------

    // TD-ADM-092: only that offer; the parent negotiation and its other offers stay; one audit row.
    [Fact]
    public async Task TdAdm092_DeleteOffer_OnlyThatOffer_ParentStays_OneAuditRow()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var target = seed.Accepted;
        var asSent = await OfferTermsAsync([target.Id]);

        var intent = await IntentAsync("offers", target.Id, session);
        AssertCascade(intent, participants: 0, artifacts: 0, negotiations: 0, offers: 1, openNegotiations: 0, openOffers: 0);
        using var response = await DeleteAsync("offers", target.Id, session, intent.Token);

        var body = await OkAsync(response);
        Assert.Equal(1, body.GetProperty("deleted").GetProperty("offers").GetInt32());
        var offer = await OfferAsync(target.Id);
        Assert.NotNull(offer.DeletedAt);
        Assert.Equal(OfferStatus.Accepted, offer.Status);
        Assert.Equal(asSent, await OfferTermsAsync([target.Id]));

        var parent = await NegotiationAsync(seed.Open.Id);
        Assert.Null(parent.DeletedAt);
        Assert.Equal(0u, parent.Version);
        foreach (var sibling in seed.OpenOffers)
            await AssertLiveAsync("offers", sibling.Id);

        var audit = Assert.Single(await AuditAsync(target.Id));
        AssertDeleteAudit(audit, "Offer", target.Id, body.GetProperty("correlationId").GetGuid());
        var before = JsonDocument.Parse(audit.BeforeSnapshot!).RootElement;
        Assert.Equal(
            new[] { "EntityVersion", "OfferAmount", "OfferCurrency", "OfferNegotiationId", "OfferStatus", "SoftDeletedAt" },
            before.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal("Accepted", before.GetProperty("OfferStatus").GetString());
        Assert.Equal(400.40m, before.GetProperty("OfferAmount").GetDecimal());
        Assert.Equal("USD", before.GetProperty("OfferCurrency").GetString());
        Assert.Equal(seed.Open.Id, before.GetProperty("OfferNegotiationId").GetGuid());
        Assert.Equal(0u, before.GetProperty("EntityVersion").GetUInt32());
        Assert.Equal(JsonValueKind.Null, before.GetProperty("SoftDeletedAt").ValueKind);
        var after = JsonDocument.Parse(audit.AfterSnapshot!).RootElement;
        Assert.Equal("1", after.GetProperty("EntityVersion").GetRawText());
        Assert.Equal(offer.DeletedAt, after.GetProperty("SoftDeletedAt").GetDateTimeOffset());
        Assert.Empty(await AuditAsync(seed.Open.Id));
    }

    // ---- TD-ADM-093: a negotiation ---------------------------------------------------------

    // TD-ADM-093: the intent warns it is open and counts its offers; the delete takes it and
    // its live offers, not its artifact or parties; stats drop; the audit rows share one id.
    [Fact]
    public async Task TdAdm093_DeleteOpenNegotiation_WarnsAndCounts_TakesItsOffers_StatsDrop_OneCorrelationId()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var statsBefore = await StatsAsync(session);
        var live = seed.OpenOffers.Append(seed.Accepted).ToList();

        var intent = await IntentAsync("negotiations", seed.Open.Id, session);
        Assert.True(intent.Body.GetProperty("identity").GetProperty("isOpen").GetBoolean());
        AssertCascade(intent, participants: 0, artifacts: 0, negotiations: 1, offers: live.Count, openNegotiations: 1, openOffers: 3);
        using var response = await DeleteAsync("negotiations", seed.Open.Id, session, intent.Token);

        var body = await OkAsync(response);
        var correlationId = body.GetProperty("correlationId").GetGuid();
        Assert.Equal(1, body.GetProperty("deleted").GetProperty("negotiations").GetInt32());
        Assert.Equal(live.Count, body.GetProperty("deleted").GetProperty("offers").GetInt32());

        Assert.NotNull((await NegotiationAsync(seed.Open.Id)).DeletedAt);
        foreach (var offer in live)
        {
            Assert.NotNull((await OfferAsync(offer.Id)).DeletedAt);
            AssertDeleteAudit(Assert.Single(await AuditAsync(offer.Id)), "Offer", offer.Id, correlationId);
        }

        var negotiationAudit = Assert.Single(await AuditAsync(seed.Open.Id));
        AssertDeleteAudit(negotiationAudit, "Negotiation", seed.Open.Id, correlationId);
        var before = JsonDocument.Parse(negotiationAudit.BeforeSnapshot!).RootElement;
        Assert.Equal("Open", before.GetProperty("NegotiationStatus").GetString());
        Assert.Equal(seed.Artifact.Id, before.GetProperty("NegotiationArtifactId").GetGuid());
        Assert.Equal(1 + live.Count, await ReadAsync(db => db.AdminAuditLog.CountAsync(a => a.CorrelationId == correlationId)));

        // The already soft-deleted offer of that negotiation is left exactly as it was.
        var earlier = await OfferAsync(seed.DeletedOpenOffer.Id);
        Assert.Equal(0u, earlier.Version);
        Assert.Empty(await AuditAsync(seed.DeletedOpenOffer.Id));

        // No artifact, no participant, no other negotiation.
        await AssertLiveAsync("artifacts", seed.Artifact.Id);
        await AssertLiveAsync("participants", seed.PartyA.Id);
        await AssertLiveAsync("participants", seed.PartyB.Id);
        await AssertLiveAsync("negotiations", seed.Closed.Id);

        // Stats after commit: one open negotiation and four offers fewer, one accept fewer.
        var statsAfter = await StatsAsync(session);
        Assert.Equal(statsBefore.GetProperty("openNegotiations").GetInt32() - 1, statsAfter.GetProperty("openNegotiations").GetInt32());
        Assert.Equal(statsBefore.GetProperty("offers").GetInt32() - live.Count, statsAfter.GetProperty("offers").GetInt32());
        Assert.Equal(statsBefore.GetProperty("accepts").GetInt32() - 1, statsAfter.GetProperty("accepts").GetInt32());
        Assert.Equal(statsBefore.GetProperty("participants").GetInt32(), statsAfter.GetProperty("participants").GetInt32());

        // Gone from the default read; there with the soft-deleted toggle.
        using var read = await SendAsync(HttpMethod.Get, $"/admin/api/negotiations/{seed.Open.Id}", session);
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        using var withDeleted = await SendAsync(HttpMethod.Get, $"/admin/api/negotiations/{seed.Open.Id}?includeDeleted=true", session);
        Assert.Equal(HttpStatusCode.OK, withDeleted.StatusCode);
    }

    // TD-ADM-093: a closed negotiation is deleted the same way, with no open warning.
    [Fact]
    public async Task TdAdm093_DeleteClosedNegotiation_NoOpenWarning_TakesItsOffers()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();

        var intent = await IntentAsync("negotiations", seed.Closed.Id, session);
        Assert.False(intent.Body.GetProperty("identity").GetProperty("isOpen").GetBoolean());
        AssertCascade(intent, participants: 0, artifacts: 0, negotiations: 1, offers: 2, openNegotiations: 0, openOffers: 0);
        using var response = await DeleteAsync("negotiations", seed.Closed.Id, session, intent.Token);

        await OkAsync(response);
        Assert.NotNull((await OfferAsync(seed.Declined.Id)).DeletedAt);
        Assert.NotNull((await OfferAsync(seed.Withdrawn.Id)).DeletedAt);
        Assert.Equal(NegotiationStatus.Closed, (await NegotiationAsync(seed.Closed.Id)).Status);
    }

    // ---- TD-ADM-094: an artifact -----------------------------------------------------------

    // TD-ADM-094: blocked while a negotiation that is not deleted refers to it, with those ids;
    // allowed once none does; the delete takes only the artifact.
    [Fact]
    public async Task TdAdm094_ArtifactBlockedWhileReferenced_ListsTheIds_ThenAllowed()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var referring = new[] { seed.Open.Id, seed.Closed.Id, seed.Expired.Id, seed.Lapsed.Id }.Order().ToList();

        using (var blocked = await SendAsync(HttpMethod.Post, $"/admin/api/artifacts/{seed.Artifact.Id}/delete-intent", session))
        {
            var body = await AssertBlockedAsync(blocked);
            Assert.Equal(referring.Count, body.GetProperty("negotiationCount").GetInt32());
            Assert.Equal(referring, body.GetProperty("negotiationIds").EnumerateArray().Select(e => e.GetGuid()).Order().ToList());
        }

        // The soft-deleted negotiation that refers to it is not listed; no token was made.
        Assert.Equal(0, await ReadAsync(db => db.AdminDeleteConfirmTokens.CountAsync(t => t.EntityId == seed.Artifact.Id)));

        foreach (var id in referring)
            await DeleteThroughIntentAsync("negotiations", id, session);

        var intent = await IntentAsync("artifacts", seed.Artifact.Id, session);
        AssertCascade(intent, participants: 0, artifacts: 1, negotiations: 0, offers: 0, openNegotiations: 0, openOffers: 0);
        using var response = await DeleteAsync("artifacts", seed.Artifact.Id, session, intent.Token);

        await OkAsync(response);
        var artifact = await ArtifactAsync(seed.Artifact.Id);
        Assert.NotNull(artifact.DeletedAt);
        Assert.Equal(seed.Artifact.Entities[0].Name, artifact.Entities.Single().Name);
        AssertDeleteAudit(Assert.Single(await AuditAsync(seed.Artifact.Id)), "Artifact", seed.Artifact.Id, correlationId: null);
        await AssertLiveAsync("participants", seed.PartyA.Id);
    }

    // An artifact with several subject entities is named by the one with the lowest id, in the
    // intent and in its audit row, whatever order the rows come back in.
    [Fact]
    public async Task TdAdm094_ArtifactWithSeveralEntities_NamedByTheLowestId()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var expected = seed.TwoEntityArtifact.Entities.OrderBy(e => e.Id).First().Name;

        var intent = await IntentAsync("artifacts", seed.TwoEntityArtifact.Id, session);
        Assert.Equal(expected, intent.Body.GetProperty("identity").GetProperty("name").GetString());
        using var response = await DeleteAsync("artifacts", seed.TwoEntityArtifact.Id, session, intent.Token);

        await OkAsync(response);
        var audit = Assert.Single(await AuditAsync(seed.TwoEntityArtifact.Id));
        Assert.Equal(expected, JsonDocument.Parse(audit.BeforeSnapshot!).RootElement.GetProperty("ArtifactName").GetString());
        Assert.Equal(2, (await ArtifactAsync(seed.TwoEntityArtifact.Id)).Entities.Count);
    }

    // TD-ADM-094 negative: a negotiation that refers to the artifact appears after the intent:
    // the delete is refused as blocked, and the artifact stays.
    [Fact]
    public async Task TdAdm094_NegotiationAppearsAfterTheIntent_DeleteBlocked_ArtifactStays()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("artifacts", seed.OrphanArtifact.Id, session);

        var newcomer = NewNegotiation(seed.OrphanArtifact, seed.PartyA, seed.PartyB);
        await ReadAsync(async db =>
        {
            db.Negotiations.Add(newcomer);
            return await db.SaveChangesAsync();
        });

        using var response = await DeleteAsync("artifacts", seed.OrphanArtifact.Id, session, intent.Token);

        var body = await AssertBlockedAsync(response);
        Assert.Equal(newcomer.Id, Assert.Single(body.GetProperty("negotiationIds").EnumerateArray()).GetGuid());
        await AssertLiveAsync("artifacts", seed.OrphanArtifact.Id);
    }

    // ---- TD-ADM-095: a participant ---------------------------------------------------------

    // TD-ADM-095: the participant, every negotiation it is a party to and their offers; the
    // intent counts them; artifacts are never deleted; other participants stay.
    [Fact]
    public async Task TdAdm095_DeleteParticipant_TakesItsNegotiationsAndTheirOffers_NeverArtifacts()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var negotiations = new[] { seed.Open, seed.Closed, seed.Expired, seed.Lapsed };
        var offers = seed.AllOffers.Where(o => o.Id != seed.DeletedOffer.Id && o.Id != seed.DeletedOpenOffer.Id).ToList();
        var artifacts = new[] { seed.Artifact, seed.TwoEntityArtifact, seed.OrphanArtifact };
        var asSent = await OfferTermsAsync(offers.Select(o => o.Id));

        var intent = await IntentAsync("participants", seed.PartyB.Id, session);
        AssertCascade(intent, participants: 1, artifacts: 0, negotiations: negotiations.Length, offers: offers.Count,
            openNegotiations: 2, openOffers: 4);
        using var response = await DeleteAsync("participants", seed.PartyB.Id, session, intent.Token);

        var body = await OkAsync(response);
        var correlationId = body.GetProperty("correlationId").GetGuid();
        Assert.NotNull((await ParticipantAsync(seed.PartyB.Id)).DeletedAt);
        foreach (var negotiation in negotiations)
            Assert.NotNull((await NegotiationAsync(negotiation.Id)).DeletedAt);
        foreach (var offer in offers)
            Assert.NotNull((await OfferAsync(offer.Id)).DeletedAt);
        Assert.Equal(asSent, await OfferTermsAsync(offers.Select(o => o.Id)));
        Assert.Equal(
            1 + negotiations.Length + offers.Count,
            await ReadAsync(db => db.AdminAuditLog.CountAsync(a => a.CorrelationId == correlationId && a.Action == "entity_delete")));
        var participantAudit = Assert.Single(await AuditAsync(seed.PartyB.Id));
        AssertDeleteAudit(participantAudit, "Participant", seed.PartyB.Id, correlationId);
        AssertSnapshot(participantAudit.BeforeSnapshot,
            ("DisplayName", seed.PartyB.DisplayName), ("ParticipantActive", "true"), ("EntityVersion", "0"), ("SoftDeletedAt", null));

        // Never an artifact (even the one it might be thought to own), never the other party.
        foreach (var artifact in artifacts)
            await AssertLiveAsync("artifacts", artifact.Id);
        await AssertLiveAsync("participants", seed.PartyA.Id);
        await AssertLiveAsync("participants", seed.Suspended.Id);
    }

    // TD-ADM-095: a participant that owns an artifact and is in no negotiation: only itself.
    [Fact]
    public async Task TdAdm095_ParticipantOwningAnArtifact_TheArtifactStays()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var owner = Participant.Create("Owner of a thing");
        var owned = Artifact.Create(owner.Sub, [SubjectEntity.Create("Owned thing", "kept")], "sell");
        await ReadAsync(async db =>
        {
            db.Participants.Add(owner);
            db.Artifacts.Add(owned);
            return await db.SaveChangesAsync();
        });

        var intent = await IntentAsync("participants", owner.Id, session);
        AssertCascade(intent, participants: 1, artifacts: 0, negotiations: 0, offers: 0, openNegotiations: 0, openOffers: 0);
        using var response = await DeleteAsync("participants", owner.Id, session, intent.Token);

        await OkAsync(response);
        await AssertLiveAsync("artifacts", owned.Id);
        Assert.Equal(owner.Sub, (await ArtifactAsync(owned.Id)).OwnerParticipantId);
        Assert.Single(await AuditAsync(owner.Id));
        _ = seed;
    }

    // ---- TD-ADM-060 / TD-ADM-061: the delete step of each entity's CRUD -------------------

    // TD-ADM-060 step 5 and TD-ADM-061 step 5: delete through the confirm path, then the record
    // is gone from the default read and there with the soft-deleted toggle.
    [Theory]
    [InlineData("participants")]
    [InlineData("artifacts")]
    public async Task TdAdm060_TdAdm061_DeleteViaConfirm_GoneFromReads_ThereWithTheToggle(string type)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var id = seed.DeletableId(type);

        await DeleteThroughIntentAsync(type, id, session);

        using var read = await SendAsync(HttpMethod.Get, $"/admin/api/{type}/{id}", session);
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        using var withToggle = await SendAsync(HttpMethod.Get, $"/admin/api/{type}/{id}?includeDeleted=true", session);
        Assert.Equal(HttpStatusCode.OK, withToggle.StatusCode);
        Assert.NotEqual(JsonValueKind.Null, (await withToggle.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("deletedAt").ValueKind);

        // An edit of the deleted record is refused as if it were not there.
        using var edit = await SendAsync(
            HttpMethod.Patch,
            $"/admin/api/{type}/{id}",
            session,
            body: type == "participants" ? """{"displayName":"x"}""" : """{"name":"x"}""",
            ifMatch: "\"0-0000000000000000\"");
        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);
    }

    // ---- The audit row ------------------------------------------------------------------

    // The audit rows hold no token, no credential, no email and no raw address.
    [Fact]
    public async Task Audit_HoldsNoTokenNoCredentialNoEmailNoAddress()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("participants", seed.PartyA.Id, session);
        using var response = await DeleteAsync("participants", seed.PartyA.Id, session, intent.Token);
        var correlationId = (await OkAsync(response)).GetProperty("correlationId").GetGuid();

        var audits = await ReadAsync(db => db.AdminAuditLog.AsNoTracking().Where(a => a.CorrelationId == correlationId).ToListAsync());
        Assert.NotEmpty(audits);
        var hash = Sha256Hex(intent.Token);
        foreach (var audit in audits)
        {
            Assert.Equal(OwnerEmail, audit.ActorEmail);
            Assert.Null(audit.ReasonClass);
            Assert.Equal(44, audit.IpHmac.Length);
            var text = audit.BeforeSnapshot + audit.AfterSnapshot;
            Assert.DoesNotContain(intent.Token, text);
            Assert.DoesNotContain(hash, text);
            Assert.DoesNotContain(session.ToString(), text);
            Assert.DoesNotContain("alpha-login@example.test", text);
            Assert.DoesNotContain("alpha-contact@example.test", text);
            Assert.DoesNotContain("127.0.0.1", text);
        }

        Assert.DoesNotContain(_factory.LogCollector.Lines, line => line.Contains(intent.Token, StringComparison.Ordinal));
    }

    // ---- Domain --------------------------------------------------------------------------

    // A soft-deleted row is never deleted again; SoftDelete touches DeletedAt, Version and UpdatedAt only.
    [Fact]
    public void Domain_SoftDelete_OnceOnly_AndNothingElse()
    {
        var at = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.FromHours(-5));
        var participant = Participant.Create("P");
        var artifact = Artifact.Create(participant.Sub, [SubjectEntity.Create("A", "d")], "sell");
        var (negotiation, _) = Negotiation.Create(artifact.Id, participant.Sub, "participant:other", "sell", "buy");
        var (offer, _) = Offer.Create(negotiation!.Id, participant.Sub, "participant:other", 12.5m, "usd", TermsAsSent);

        Assert.True(participant.SoftDelete(at));
        Assert.True(artifact.SoftDelete(at));
        Assert.True(negotiation.SoftDelete(at));
        Assert.True(offer!.SoftDelete(at));

        Assert.False(participant.SoftDelete(at.AddDays(1)));
        Assert.False(artifact.SoftDelete(at.AddDays(1)));
        Assert.False(negotiation.SoftDelete(at.AddDays(1)));
        Assert.False(offer.SoftDelete(at.AddDays(1)));

        Assert.Equal(at.ToUniversalTime(), participant.DeletedAt);
        Assert.Equal(TimeSpan.Zero, participant.DeletedAt!.Value.Offset);
        Assert.Equal(1u, participant.Version);
        Assert.Equal(1u, artifact.Version);
        Assert.Equal(1u, negotiation.Version);
        Assert.Equal(1u, offer.Version);
        Assert.Equal(at.ToUniversalTime(), offer.UpdatedAt);
        Assert.Equal(OfferStatus.Open, offer.Status);
        Assert.Equal(12.5m, offer.Amount);
        Assert.Equal("USD", offer.Currency);
        Assert.Equal(TermsAsSent, offer.Terms);
        Assert.Equal(NegotiationStatus.Open, negotiation.Status);
        Assert.True(participant.IsActive);
    }

    // ---- Harness -------------------------------------------------------------------------

    public static TheoryData<string> EveryType()
    {
        var data = new TheoryData<string>();
        foreach (var type in Types)
            data.Add(type);
        return data;
    }

    private FakeAdminClock Clock() => _factory.Services.GetRequiredService<FakeAdminClock>();

    private sealed record Seed(
        Participant PartyA,
        Participant PartyB,
        Participant Suspended,
        Participant DeletedParticipant,
        Artifact Artifact,
        Artifact TwoEntityArtifact,
        Artifact OrphanArtifact,
        Artifact DeletedArtifact,
        Negotiation Open,
        Negotiation Closed,
        Negotiation Expired,
        Negotiation Lapsed,
        Negotiation DeletedNegotiation,
        IReadOnlyList<Offer> OpenOffers,
        Offer Accepted,
        Offer Declined,
        Offer Withdrawn,
        Offer Superseded,
        Offer Cancelled,
        Offer LapsedOffer,
        Offer DeletedOpenOffer,
        Offer DeletedOffer)
    {
        public IEnumerable<Offer> AllOffers
            => OpenOffers.Concat(
                [Accepted, Declined, Withdrawn, Superseded, Cancelled, LapsedOffer, DeletedOpenOffer, DeletedOffer]);

        /// <summary>A live record of the type that a delete can take (an artifact no negotiation refers to).</summary>
        public Guid DeletableId(string type) => type switch
        {
            "participants" => PartyA.Id,
            "artifacts" => OrphanArtifact.Id,
            "negotiations" => Open.Id,
            _ => OpenOffers[0].Id
        };

        public Guid DeletedId(string type) => type switch
        {
            "participants" => DeletedParticipant.Id,
            "artifacts" => DeletedArtifact.Id,
            "negotiations" => DeletedNegotiation.Id,
            _ => DeletedOffer.Id
        };
    }

    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var tag = Guid.NewGuid().ToString("N")[..8];

        var partyA = Participant.Create($"{tag}-Alpha");
        partyA.UpdateLoginEmail($"{tag}-alpha-login@example.test");
        partyA.UpdateContactEmail($"{tag}-alpha-contact@example.test");
        var partyB = Participant.Create($"{tag}-Beta");
        var suspended = Participant.Create($"{tag}-Suspended");
        suspended.Deactivate();
        var deletedParty = Participant.Create($"{tag}-DeletedPerson");
        db.Participants.AddRange(partyA, partyB, suspended, deletedParty);

        var artifact = Artifact.Create(partyA.Sub, [SubjectEntity.Create($"{tag}-RedWidget", "widget-desc")], "sell");
        var twoEntities = Artifact.Create(
            partyB.Sub,
            [SubjectEntity.Create($"{tag}-First", "first"), SubjectEntity.Create($"{tag}-Second", "second")],
            "sell");
        var orphan = Artifact.Create(deletedParty.Sub, [SubjectEntity.Create($"{tag}-Orphan", "no negotiation")], "sell");
        var deletedArtifact = Artifact.Create(partyA.Sub, [SubjectEntity.Create($"{tag}-GoneWidget", "gone")], "sell");
        db.Artifacts.AddRange(artifact, twoEntities, orphan, deletedArtifact);

        var start = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
        var open = NewNegotiation(artifact, partyA, partyB, start, new DateTimeOffset(2030, 6, 1, 9, 0, 0, TimeSpan.Zero));
        var closed = NewNegotiation(artifact, partyA, partyB);
        closed.Close();
        var expired = NewNegotiation(artifact, partyA, partyB);
        expired.Expire();
        var lapsed = NewNegotiation(artifact, partyA, partyB, endsAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var deletedNegotiation = NewNegotiation(artifact, partyA, partyB);
        db.Negotiations.AddRange(open, closed, expired, lapsed, deletedNegotiation);

        var openOffers = new[]
        {
            NewOffer(open, partyA, partyB, 100.10m),
            NewOffer(open, partyB, partyA, 200.20m),
            NewOffer(open, partyA, partyB, 300.30m)
        };
        var accepted = NewOffer(open, partyB, partyA, 400.40m);
        accepted.Accept(partyA.Sub);
        var declined = NewOffer(closed, partyA, partyB, 50m);
        declined.Decline(partyB.Sub);
        var withdrawn = NewOffer(closed, partyB, partyA, 75m);
        withdrawn.Withdraw(partyB.Sub);
        var superseded = NewOffer(expired, partyA, partyB, 30m);
        superseded.Supersede();
        var cancelled = NewOffer(expired, partyB, partyA, 40m);
        cancelled.Cancel();
        var lapsedOffer = NewOffer(lapsed, partyA, partyB, 60m);
        var deletedOpenOffer = NewOffer(open, partyB, partyA, 500.50m);
        var deletedOffer = NewOffer(deletedNegotiation, partyA, partyB, 9m);
        db.Offers.AddRange(openOffers);
        db.Offers.AddRange(accepted, declined, withdrawn, superseded, cancelled, lapsedOffer, deletedOpenOffer, deletedOffer);

        await db.SaveChangesAsync();

        var at = DateTimeOffset.UtcNow;
        await db.Participants.Where(p => p.Id == deletedParty.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, at));
        await db.Artifacts.Where(a => a.Id == deletedArtifact.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.DeletedAt, at));
        await db.Negotiations.Where(n => n.Id == deletedNegotiation.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, at));
        await db.Offers.Where(o => o.Id == deletedOffer.Id || o.Id == deletedOpenOffer.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.DeletedAt, at));

        return new Seed(
            partyA, partyB, suspended, deletedParty,
            artifact, twoEntities, orphan, deletedArtifact,
            open, closed, expired, lapsed, deletedNegotiation,
            openOffers, accepted, declined, withdrawn, superseded, cancelled, lapsedOffer, deletedOpenOffer, deletedOffer);
    }

    private static Negotiation NewNegotiation(
        Artifact artifact,
        Participant a,
        Participant b,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? endsAt = null)
    {
        var (negotiation, errors) = Negotiation.Create(artifact.Id, a.Sub, b.Sub, "sell", "buy", startsAt, endsAt);
        Assert.Empty(errors);
        return negotiation!;
    }

    private static Offer NewOffer(Negotiation negotiation, Participant from, Participant to, decimal amount)
    {
        var (offer, errors) = Offer.Create(negotiation.Id, from.Sub, to.Sub, amount, "usd", TermsAsSent);
        Assert.Empty(errors);
        return offer!;
    }

    private async Task<Guid> NewSessionAsync(
        WebApplicationFactory<Program>? factory = null,
        bool totpVerified = true,
        string email = OwnerEmail)
    {
        using var scope = (factory ?? _factory).Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(email, "testhmac-not-an-ip", Clock().UtcNow);
        if (totpVerified)
            session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private async Task<T> ReadAsync<T>(Func<DealowareDbContext, Task<T>> read)
    {
        using var scope = _factory.Services.CreateScope();
        return await read(scope.ServiceProvider.GetRequiredService<DealowareDbContext>());
    }

    private Task<Participant> ParticipantAsync(Guid id)
        => ReadAsync(db => db.Participants.AsNoTracking().SingleAsync(p => p.Id == id));

    private Task<Artifact> ArtifactAsync(Guid id)
        => ReadAsync(db => db.Artifacts.AsNoTracking().Include(a => a.Entities).SingleAsync(a => a.Id == id));

    private Task<Negotiation> NegotiationAsync(Guid id)
        => ReadAsync(db => db.Negotiations.AsNoTracking().SingleAsync(n => n.Id == id));

    private Task<Offer> OfferAsync(Guid id)
        => ReadAsync(db => db.Offers.AsNoTracking().SingleAsync(o => o.Id == id));

    private Task<List<AdminAuditEntry>> AuditAsync(Guid entityId)
        => ReadAsync(db => db.AdminAuditLog.AsNoTracking().Where(a => a.EntityId == entityId).ToListAsync());

    private Task<int> AuditCountAsync()
        => ReadAsync(db => db.AdminAuditLog.CountAsync());

    /// <summary>Every row of the four tables, deleted or not: a soft delete never removes one.</summary>
    private Task<(int, int, int, int)> RowCountsAsync()
        => ReadAsync(async db => (
            await db.Participants.CountAsync(),
            await db.Artifacts.CountAsync(),
            await db.Negotiations.CountAsync(),
            await db.Offers.CountAsync()));

    /// <summary>
    /// What the party sent, offer by offer: the amount as stored, the
    /// currency, and the terms as UTF-8 bytes in hex. Equal lists mean equal bytes.
    /// </summary>
    private async Task<List<string>> OfferTermsAsync(IEnumerable<Guid> ids)
    {
        var wanted = ids.ToList();
        var offers = await ReadAsync(db => db.Offers.AsNoTracking().Where(o => wanted.Contains(o.Id)).ToListAsync());
        Assert.Equal(wanted.Count, offers.Count);
        return offers
            .OrderBy(o => o.Id)
            .Select(o => string.Join(
                '|',
                o.Id.ToString("N"),
                o.Amount?.ToString(CultureInfo.InvariantCulture) ?? "(null)",
                o.Currency ?? "(null)",
                o.Terms is null ? "(null)" : Convert.ToHexString(Encoding.UTF8.GetBytes(o.Terms)),
                o.FromParticipantId,
                o.ToParticipantId,
                o.CreatedAt.UtcTicks.ToString(CultureInfo.InvariantCulture)))
            .ToList();
    }

    /// <summary>Not soft-deleted, its Version where it was, and no delete audit row for it.</summary>
    private async Task AssertLiveAsync(string type, Guid id)
    {
        var (deletedAt, version) = type switch
        {
            "participants" => await ReadAsync(db => db.Participants.Where(p => p.Id == id).Select(p => new { p.DeletedAt, p.Version }).SingleAsync()) is var p ? (p.DeletedAt, p.Version) : default,
            "artifacts" => await ReadAsync(db => db.Artifacts.Where(a => a.Id == id).Select(a => new { a.DeletedAt, a.Version }).SingleAsync()) is var a ? (a.DeletedAt, a.Version) : default,
            "negotiations" => await ReadAsync(db => db.Negotiations.Where(n => n.Id == id).Select(n => new { n.DeletedAt, n.Version }).SingleAsync()) is var n ? (n.DeletedAt, n.Version) : default,
            _ => await ReadAsync(db => db.Offers.Where(o => o.Id == id).Select(o => new { o.DeletedAt, o.Version }).SingleAsync()) is var o ? (o.DeletedAt, o.Version) : default
        };
        Assert.Null(deletedAt);
        Assert.Equal(0u, version);
        Assert.DoesNotContain(await AuditAsync(id), a => a.Action == AdminAuditActions.EntityDelete);
    }

    private sealed record Intent(JsonElement Body, string Token, string? CacheControl);

    private async Task<Intent> IntentAsync(string type, Guid id, Guid session, WebApplicationFactory<Program>? factory = null)
    {
        using var response = await SendAsync(HttpMethod.Post, $"/admin/api/{type}/{id}/delete-intent", session, factory: factory);
        var body = await OkAsync(response);
        return new Intent(body, body.GetProperty("confirmToken").GetString()!, response.Headers.CacheControl?.ToString());
    }

    private Task<HttpResponseMessage> DeleteAsync(
        string type,
        Guid id,
        Guid? session,
        string? token,
        WebApplicationFactory<Program>? factory = null)
        => SendAsync(HttpMethod.Delete, $"/admin/api/{type}/{id}", session, token: token, factory: factory);

    /// <summary>The whole confirm path: intent, then the delete with its token. Returns the reply body.</summary>
    private async Task<JsonElement> DeleteThroughIntentAsync(string type, Guid id, Guid session)
    {
        var intent = await IntentAsync(type, id, session);
        using var response = await DeleteAsync(type, id, session, intent.Token);
        return await OkAsync(response);
    }

    private async Task<JsonElement> StatsAsync(Guid session)
    {
        using var response = await SendAsync(HttpMethod.Get, "/admin/api/stats", session);
        return await OkAsync(response);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        Guid? session,
        string? token = null,
        string? secondToken = null,
        bool antiForgery = true,
        string? origin = AdminHostOptions.ProductionAdminOrigin,
        string? body = null,
        string? ifMatch = null,
        WebApplicationFactory<Program>? factory = null)
    {
        factory ??= _factory;
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Remove("Origin");

        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        var cookies = new List<string>();
        if (session is { } id)
            cookies.Add($"{AdminSessionCookie.Name}={id:D}");
        if (antiForgery && method != HttpMethod.Get)
        {
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            cookies.Add($"{AdminAntiForgeryCookie.Name}={nonce}");
            request.Headers.TryAddWithoutValidation(
                AdminAntiForgeryCookie.HeaderName,
                factory.Services.GetRequiredService<AdminAntiForgeryService>().ComputeRequestToken(nonce));
        }

        if (cookies.Count > 0)
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        if (origin is not null)
            request.Headers.TryAddWithoutValidation("Origin", origin);
        if (token is not null)
            request.Headers.TryAddWithoutValidation(AdminDeleteEndpoints.ConfirmTokenHeader, token);
        if (secondToken is not null)
            request.Headers.TryAddWithoutValidation(AdminDeleteEndpoints.ConfirmTokenHeader, secondToken);
        if (ifMatch is not null)
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        if (body is not null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return await client.SendAsync(request);
    }

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement;
    }

    private static async Task AssertGenericDenyAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{(int)response.StatusCode}: {text}");
        Assert.Equal("""{"error":"BadRequest"}""", text);
    }

    private static async Task AssertTokenInvalidAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Conflict, $"{(int)response.StatusCode}: {text}");
        var body = JsonDocument.Parse(text).RootElement;
        Assert.Equal(AdminDeleteCopy.TokenInvalidError, body.GetProperty("error").GetString());
        Assert.Equal(AdminDeleteCopy.TokenInvalid, body.GetProperty("message").GetString());
        Assert.Equal(2, body.EnumerateObject().Count());
    }

    private static async Task AssertChangedAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Conflict, $"{(int)response.StatusCode}: {text}");
        var body = JsonDocument.Parse(text).RootElement;
        Assert.Equal("Conflict", body.GetProperty("error").GetString());
        Assert.Equal(AdminDeleteCopy.Changed, body.GetProperty("message").GetString());
        Assert.Equal(2, body.EnumerateObject().Count());
    }

    private static async Task<JsonElement> AssertBlockedAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Conflict, $"{(int)response.StatusCode}: {text}");
        var body = JsonDocument.Parse(text).RootElement;
        Assert.Equal(AdminDeleteCopy.BlockedError, body.GetProperty("error").GetString());
        Assert.Equal(AdminDeleteCopy.ArtifactBlocked, body.GetProperty("message").GetString());
        return body;
    }

    private static void AssertCascade(
        Intent intent,
        int participants,
        int artifacts,
        int negotiations,
        int offers,
        int openNegotiations,
        int openOffers)
    {
        var cascade = intent.Body.GetProperty("cascade");
        Assert.Equal(participants, cascade.GetProperty("participants").GetInt32());
        Assert.Equal(artifacts, cascade.GetProperty("artifacts").GetInt32());
        Assert.Equal(negotiations, cascade.GetProperty("negotiations").GetInt32());
        Assert.Equal(offers, cascade.GetProperty("offers").GetInt32());
        Assert.Equal(openNegotiations, cascade.GetProperty("openNegotiations").GetInt32());
        Assert.Equal(openOffers, cascade.GetProperty("openOffers").GetInt32());
    }

    private static void AssertDeleteAudit(AdminAuditEntry audit, string entityType, Guid entityId, Guid? correlationId)
    {
        Assert.Equal(AdminAuditActions.EntityDelete, audit.Action);
        Assert.Equal("entity_delete", audit.Action);
        Assert.Equal(entityType, audit.EntityType);
        Assert.Equal(entityId, audit.EntityId);
        Assert.Equal(OwnerEmail, audit.ActorEmail);
        Assert.NotNull(audit.CorrelationId);
        if (correlationId is { } expected)
            Assert.Equal(expected, audit.CorrelationId);
        Assert.NotNull(audit.BeforeSnapshot);
        Assert.NotNull(audit.AfterSnapshot);
        var after = JsonDocument.Parse(audit.AfterSnapshot!).RootElement;
        Assert.Equal(JsonValueKind.String, after.GetProperty("SoftDeletedAt").ValueKind);
    }

    /// <summary>The snapshot holds exactly these fields; each value is compared as its JSON text or its string.</summary>
    private static void AssertSnapshot(string? snapshot, params (string Field, string? Value)[] expected)
    {
        Assert.NotNull(snapshot);
        var root = JsonDocument.Parse(snapshot).RootElement;
        Assert.Equal(expected.Select(e => e.Field).Order(), root.EnumerateObject().Select(p => p.Name).Order());
        foreach (var (field, value) in expected)
        {
            var actual = root.GetProperty(field);
            var text = actual.ValueKind switch
            {
                JsonValueKind.String => actual.GetString(),
                JsonValueKind.Null => null,
                _ => actual.GetRawText()
            };
            Assert.Equal(value, text);
        }
    }

    private static string Sha256Hex(string raw)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(raw)));
}
