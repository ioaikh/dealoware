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
public sealed class EditWebApplicationFactory : IsolatedWebApplicationFactory
{
    public const string OwnerEmail = "owner@example.test";

    protected override string? PostgresDatabaseName => "dealoware_ci_edit";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("CoreOwner:Email", OwnerEmail);
        base.ConfigureWebHost(builder);
    }
}

/// <summary>
/// T05a, A7 Step 9: the admin edit API, through the real routes and the real
/// session gate. Each test names the edit path (E1 to E8, E12) and the test
/// design case (TD-ADM-*) it covers. E9, E10 and E11 (an offer's amount,
/// currency and terms) are not built, by the owner's decision "Cancel only";
/// the tests here show those three are refused and never change.
/// <para>
/// The party-action and transaction tests are in AdminEditApiTests.Races.cs.
/// </para>
/// </summary>
[Collection("WebAppTests")]
public partial class AdminEditApiTests : IClassFixture<EditWebApplicationFactory>
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string OwnerEmail = EditWebApplicationFactory.OwnerEmail;
    private const string GenericDeny = """{"error":"BadRequest"}""";

    /// <summary>Not ASCII, so "unchanged" is shown on the bytes, not on a lucky encoding.</summary>
    private const string TermsAsSent = "Net 30 — «as sent» ✓ 12½ % · naïve";

    private static readonly string[] Types = ["participants", "artifacts", "negotiations", "offers"];

    private readonly EditWebApplicationFactory _factory;

    public AdminEditApiTests(EditWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- E1: participant Suspended to Active (and back) ----------------------

    // E1, TD-ADM-178 step 6 (IsActive both ways), TD-ADM-080 (audit before/after)
    [Fact]
    public async Task E1_TdAdm178_Activate_SuspendedToActive_AuditsBeforeAndAfter()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("participants", seed.Suspended.Id, session);

        using var response = await PatchAsync("participants", seed.Suspended.Id, new { isActive = true }, session, read.ETag);

        var body = await OkAsync(response);
        Assert.True(body.GetProperty("isActive").GetBoolean());
        Assert.Equal(1u, body.GetProperty("version").GetUInt32());
        Assert.True((await ParticipantAsync(seed.Suspended.Id)).IsActive);

        var audit = Assert.Single(await AuditAsync(seed.Suspended.Id));
        AssertEditAudit(audit, "Participant", seed.Suspended.Id);
        AssertSnapshot(audit.BeforeSnapshot, ("ParticipantActive", "false"), ("EntityVersion", "0"));
        AssertSnapshot(audit.AfterSnapshot, ("ParticipantActive", "true"), ("EntityVersion", "1"));

        // And back: Active to Suspended, with the token the write returned.
        using var back = await PatchAsync("participants", seed.Suspended.Id, new { isActive = false }, session, ETagOf(response));
        Assert.False((await OkAsync(back)).GetProperty("isActive").GetBoolean());
        Assert.Equal(2u, (await ParticipantAsync(seed.Suspended.Id)).Version);
        Assert.Equal(2, (await AuditAsync(seed.Suspended.Id)).Count);
    }

    // ---- E2: participant display name ----------------------------------------

    // E2, TD-ADM-060 (participant edit), TD-ADM-080, TD-ADM-178 step 6 (256 / 257)
    [Fact]
    public async Task E2_TdAdm060_DisplayName_Saved_OptionalAndAtMost256()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("participants", seed.PartyA.Id, session);
        var longest = new string('n', 256);

        using var response = await PatchAsync("participants", seed.PartyA.Id, new { displayName = longest }, session, read.ETag);

        var body = await OkAsync(response);
        Assert.Equal(longest, body.GetProperty("displayName").GetString());
        var stored = await ParticipantAsync(seed.PartyA.Id);
        Assert.Equal(longest, stored.DisplayName);
        Assert.Equal(1u, stored.Version);
        Assert.NotNull(stored.UpdatedAt);

        var audit = Assert.Single(await AuditAsync(seed.PartyA.Id));
        AssertEditAudit(audit, "Participant", seed.PartyA.Id);
        AssertSnapshot(audit.BeforeSnapshot, ("DisplayName", seed.PartyA.DisplayName), ("EntityVersion", "0"));
        AssertSnapshot(audit.AfterSnapshot, ("DisplayName", longest), ("EntityVersion", "1"));

        // 257: refused with a message for the field, nothing written.
        using var tooLong = await PatchAsync(
            "participants", seed.PartyA.Id, new { displayName = new string('n', 257) }, session, ETagOf(response));
        await AssertFieldErrorAsync(tooLong, "displayName", AdminEditCopy.DisplayNameTooLong);
        Assert.Equal(1u, (await ParticipantAsync(seed.PartyA.Id)).Version);

        // Optional: null clears it.
        using var cleared = await PatchAsync(
            "participants", seed.PartyA.Id, new { displayName = (string?)null }, session, ETagOf(response));
        Assert.Equal(JsonValueKind.Null, (await OkAsync(cleared)).GetProperty("displayName").ValueKind);
        Assert.Null((await ParticipantAsync(seed.PartyA.Id)).DisplayName);
    }

    // ---- E3, E4: artifact subject name and description -----------------------

    // E3, TD-ADM-061 (artifact edit), TD-ADM-178 step 7 (Name 4096 / 4097 / empty)
    [Fact]
    public async Task E3_TdAdm061_Name_Saved_RequiredAndAtMost4096()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("artifacts", seed.Artifact.Id, session);

        using var response = await PatchAsync("artifacts", seed.Artifact.Id, new { name = "  Blue widget  " }, session, read.ETag);

        var body = await OkAsync(response);
        Assert.Equal("Blue widget", body.GetProperty("name").GetString());
        var stored = await ArtifactAsync(seed.Artifact.Id);
        Assert.Equal("Blue widget", stored.Entities.Single().Name);
        Assert.Equal("widget-desc", stored.Entities.Single().Description);
        Assert.Equal(1u, stored.Version);

        var audit = Assert.Single(await AuditAsync(seed.Artifact.Id));
        AssertEditAudit(audit, "Artifact", seed.Artifact.Id);
        AssertSnapshot(audit.BeforeSnapshot, ("ArtifactName", seed.Artifact.Entities[0].Name), ("EntityVersion", "0"));
        AssertSnapshot(audit.AfterSnapshot, ("ArtifactName", "Blue widget"), ("EntityVersion", "1"));

        var etag = ETagOf(response);
        using var empty = await PatchAsync("artifacts", seed.Artifact.Id, new { name = "   " }, session, etag);
        await AssertFieldErrorAsync(empty, "name", AdminEditCopy.NameRequired);
        using var notText = await PatchAsync("artifacts", seed.Artifact.Id, new { name = 7 }, session, etag);
        await AssertFieldErrorAsync(notText, "name", AdminEditCopy.NameRequired);
        using var tooLong = await PatchAsync("artifacts", seed.Artifact.Id, new { name = new string('n', 4097) }, session, etag);
        await AssertFieldErrorAsync(tooLong, "name", AdminEditCopy.NameTooLong);
        Assert.Equal(1u, (await ArtifactAsync(seed.Artifact.Id)).Version);

        using var longest = await PatchAsync("artifacts", seed.Artifact.Id, new { name = new string('n', 4096) }, session, etag);
        await OkAsync(longest);
        Assert.Equal(4096, (await ArtifactAsync(seed.Artifact.Id)).Entities.Single().Name.Length);
    }

    // E4, TD-ADM-061, TD-ADM-178 step 7 (Description empty)
    [Fact]
    public async Task E4_TdAdm061_Description_Saved_MayBeEmpty_AtMost4096()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("artifacts", seed.Artifact.Id, session);

        using var response = await PatchAsync("artifacts", seed.Artifact.Id, new { description = "" }, session, read.ETag);

        await OkAsync(response);
        var stored = await ArtifactAsync(seed.Artifact.Id);
        Assert.Equal("", stored.Entities.Single().Description);
        Assert.Equal(seed.Artifact.Entities[0].Name, stored.Entities.Single().Name);

        var audit = Assert.Single(await AuditAsync(seed.Artifact.Id));
        AssertSnapshot(audit.BeforeSnapshot, ("ArtifactDescription", "widget-desc"), ("EntityVersion", "0"));
        AssertSnapshot(audit.AfterSnapshot, ("ArtifactDescription", ""), ("EntityVersion", "1"));

        using var tooLong = await PatchAsync(
            "artifacts", seed.Artifact.Id, new { description = new string('d', 4097) }, session, ETagOf(response));
        await AssertFieldErrorAsync(tooLong, "description", AdminEditCopy.DescriptionTooLong);
        Assert.Equal(1u, (await ArtifactAsync(seed.Artifact.Id)).Version);
    }

    // E3/E4: an artifact with several subject entities is never edited by guess.
    [Fact]
    public async Task E3_E4_SeveralSubjectEntities_TheWriteNamesWhichOne()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("artifacts", seed.TwoEntityArtifact.Id, session);
        var second = seed.TwoEntityArtifact.Entities[1];
        Assert.Contains(
            read.Body.GetProperty("entities").EnumerateArray(),
            e => e.GetProperty("id").GetGuid() == second.Id);

        using var unnamed = await PatchAsync("artifacts", seed.TwoEntityArtifact.Id, new { name = "Which one" }, session, read.ETag);
        await AssertGenericDenyAsync(unnamed);
        using var notThere = await PatchAsync(
            "artifacts", seed.TwoEntityArtifact.Id, new { name = "Which one", entityId = Guid.NewGuid() }, session, read.ETag);
        await AssertGenericDenyAsync(notThere);

        using var named = await PatchAsync(
            "artifacts", seed.TwoEntityArtifact.Id, new { name = "Second, renamed", entityId = second.Id }, session, read.ETag);

        await OkAsync(named);
        var stored = await ArtifactAsync(seed.TwoEntityArtifact.Id);
        Assert.Equal("Second, renamed", stored.Entities.Single(e => e.Id == second.Id).Name);
        Assert.Equal(
            seed.TwoEntityArtifact.Entities[0].Name,
            stored.Entities.Single(e => e.Id == seed.TwoEntityArtifact.Entities[0].Id).Name);
    }

    // ---- E5: artifact owner ---------------------------------------------------

    // E5, TD-ADM-178 step 7 (Owner set to a soft-deleted or unknown participant)
    [Fact]
    public async Task E5_TdAdm178_Owner_Reassigned_OnlyToAnExistingParticipantThatIsNotDeleted()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("artifacts", seed.Artifact.Id, session);

        // By Sub, as the read model gives it.
        using var response = await PatchAsync(
            "artifacts", seed.Artifact.Id, new { ownerParticipantId = seed.PartyB.Sub }, session, read.ETag);

        var body = await OkAsync(response);
        Assert.Equal(seed.PartyB.Sub, body.GetProperty("ownerParticipantId").GetString());
        Assert.Equal(seed.PartyB.DisplayName, body.GetProperty("ownerName").GetString());
        Assert.Equal(seed.PartyB.Sub, (await ArtifactAsync(seed.Artifact.Id)).OwnerParticipantId);

        var audit = Assert.Single(await AuditAsync(seed.Artifact.Id));
        AssertEditAudit(audit, "Artifact", seed.Artifact.Id);
        AssertSnapshot(audit.BeforeSnapshot, ("ArtifactOwnerParticipantId", seed.PartyA.Sub), ("EntityVersion", "0"));
        AssertSnapshot(audit.AfterSnapshot, ("ArtifactOwnerParticipantId", seed.PartyB.Sub), ("EntityVersion", "1"));

        // By id, as a picker gives it.
        using var byId = await PatchAsync(
            "artifacts", seed.Artifact.Id, new { ownerParticipantId = seed.PartyA.Id }, session, ETagOf(response));
        await OkAsync(byId);
        Assert.Equal(seed.PartyA.Sub, (await ArtifactAsync(seed.Artifact.Id)).OwnerParticipantId);

        // Unknown and soft-deleted: the same reply, and nothing written.
        var etag = ETagOf(byId);
        using var unknown = await PatchAsync(
            "artifacts", seed.Artifact.Id, new { ownerParticipantId = $"participant:{Guid.NewGuid()}" }, session, etag);
        using var deleted = await PatchAsync(
            "artifacts", seed.Artifact.Id, new { ownerParticipantId = seed.DeletedParticipant.Sub }, session, etag);
        await AssertGenericDenyAsync(unknown);
        await AssertGenericDenyAsync(deleted);
        using var blank = await PatchAsync("artifacts", seed.Artifact.Id, new { ownerParticipantId = " " }, session, etag);
        await AssertFieldErrorAsync(blank, "ownerParticipantId", AdminEditCopy.OwnerNotChosen);

        var stored = await ArtifactAsync(seed.Artifact.Id);
        Assert.Equal(seed.PartyA.Sub, stored.OwnerParticipantId);
        Assert.Equal(2u, stored.Version);
    }

    // ---- E6: negotiation end time ---------------------------------------------

    // E6, TD-ADM-178 step 3 (EndsAt: valid while Open; before StartsAt; on a Closed negotiation)
    [Fact]
    public async Task E6_TdAdm178_EndsAt_WhileOpen_AfterTheStart()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("negotiations", seed.Open.Id, session);
        Assert.Equal(seed.Open.StartsAt, read.Body.GetProperty("startsAt").GetDateTimeOffset());
        var newEnd = seed.Open.StartsAt!.Value.AddDays(30);

        using var response = await PatchAsync(
            "negotiations", seed.Open.Id, new { endsAt = newEnd.ToString("O", CultureInfo.InvariantCulture) }, session, read.ETag);

        var body = await OkAsync(response);
        Assert.Equal(newEnd, body.GetProperty("endsAt").GetDateTimeOffset());
        var stored = await NegotiationAsync(seed.Open.Id);
        Assert.Equal(newEnd, stored.EndsAt);
        Assert.Equal(NegotiationStatus.Open, stored.Status);
        Assert.Equal(1u, stored.Version);

        var audit = Assert.Single(await AuditAsync(seed.Open.Id));
        AssertEditAudit(audit, "Negotiation", seed.Open.Id);
        Assert.Null(audit.CorrelationId);
        var after = JsonDocument.Parse(audit.AfterSnapshot!).RootElement;
        Assert.Equal(newEnd, after.GetProperty("NegotiationEndsAt").GetDateTimeOffset());
        Assert.Equal(1u, after.GetProperty("EntityVersion").GetUInt32());
        Assert.Equal(seed.Open.EndsAt, JsonDocument.Parse(audit.BeforeSnapshot!).RootElement.GetProperty("NegotiationEndsAt").GetDateTimeOffset());

        var etag = ETagOf(response);
        using var beforeStart = await PatchAsync(
            "negotiations", seed.Open.Id, new { endsAt = seed.Open.StartsAt.Value.AddMinutes(-1).ToString("O") }, session, etag);
        await AssertFieldErrorAsync(beforeStart, "endsAt", AdminEditCopy.EndsAtNotAfterStart);
        foreach (var notATime in new object[] { "next week", "2026-12-01T10:00:00", "9999-12-31T00:00:00Z", 5 })
        {
            using var invalid = await PatchAsync("negotiations", seed.Open.Id, new { endsAt = notATime }, session, etag);
            await AssertFieldErrorAsync(invalid, "endsAt", AdminEditCopy.EndsAtInvalid);
        }

        Assert.Equal(1u, (await NegotiationAsync(seed.Open.Id)).Version);

        // Closed or expired: the generic refusal, with no word about the status.
        foreach (var final in new[] { seed.Closed, seed.Expired })
        {
            var finalRead = await GetAsync("negotiations", final.Id, session);
            using var refused = await PatchAsync(
                "negotiations", final.Id, new { endsAt = DateTimeOffset.UtcNow.AddDays(3).ToString("O") }, session, finalRead.ETag);
            await AssertGenericDenyAsync(refused);
            Assert.Equal(0u, (await NegotiationAsync(final.Id)).Version);
        }
    }

    // ---- E7, E8: negotiation Open to Closed, Open to Expired ----------------

    // E7 and E8, TD-ADM-178 step 2 (close and expire a negotiation holding 3 open offers + 1 accepted)
    [Theory]
    [InlineData("Closed")]
    [InlineData("Expired")]
    public async Task E7_E8_TdAdm178_CloseOrExpire_CancelsEveryOpenOffer_InOneTransaction(string status)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("negotiations", seed.Open.Id, session);
        Assert.Equal(3, read.Body.GetProperty("openOfferCount").GetInt32());
        var sent = await OfferTermsAsync(seed.OpenOffers.Select(o => o.Id).Append(seed.Accepted.Id));

        using var response = await PatchAsync("negotiations", seed.Open.Id, new { status }, session, read.ETag);

        var body = await OkAsync(response);
        Assert.Equal(status, body.GetProperty("status").GetString());
        Assert.Equal(0, body.GetProperty("openOfferCount").GetInt32());
        var stored = await NegotiationAsync(seed.Open.Id);
        Assert.Equal(Enum.Parse<NegotiationStatus>(status), stored.Status);
        Assert.Equal(1u, stored.Version);

        foreach (var open in seed.OpenOffers)
        {
            var offer = await OfferAsync(open.Id);
            Assert.Equal(OfferStatus.Cancelled, offer.Status);
            Assert.Equal(1u, offer.Version);
            Assert.Equal(stored.UpdatedAt, offer.UpdatedAt);
        }

        // The accepted offer is the parties' outcome: not touched.
        var accepted = await OfferAsync(seed.Accepted.Id);
        Assert.Equal(OfferStatus.Accepted, accepted.Status);
        Assert.Equal(0u, accepted.Version);
        Assert.Empty(await AuditAsync(seed.Accepted.Id));

        // What the parties sent is as they sent it.
        Assert.Equal(sent, await OfferTermsAsync(seed.OpenOffers.Select(o => o.Id).Append(seed.Accepted.Id)));

        // One audit row for the negotiation, one for each cancelled offer, tied together.
        var audit = Assert.Single(await AuditAsync(seed.Open.Id));
        AssertEditAudit(audit, "Negotiation", seed.Open.Id);
        AssertSnapshot(audit.BeforeSnapshot, ("NegotiationStatus", "Open"), ("EntityVersion", "0"));
        AssertSnapshot(audit.AfterSnapshot, ("NegotiationStatus", status), ("EntityVersion", "1"));
        Assert.NotNull(audit.CorrelationId);
        foreach (var open in seed.OpenOffers)
        {
            var offerAudit = Assert.Single(await AuditAsync(open.Id));
            AssertEditAudit(offerAudit, "Offer", open.Id);
            Assert.Equal(audit.CorrelationId, offerAudit.CorrelationId);
            AssertSnapshot(offerAudit.BeforeSnapshot, ("OfferStatus", "Open"), ("EntityVersion", "0"));
            AssertSnapshot(offerAudit.AfterSnapshot, ("OfferStatus", "Cancelled"), ("EntityVersion", "1"));
        }

        // Final: no way back, and no way across.
        var etag = ETagOf(response);
        foreach (var next in new[] { "Open", "Closed", "Expired" })
        {
            using var again = await PatchAsync("negotiations", seed.Open.Id, new { status = next }, session, etag);
            await AssertGenericDenyAsync(again);
        }

        Assert.Equal(1u, (await NegotiationAsync(seed.Open.Id)).Version);
    }

    // TD-ADM-178 step 1: the only statuses the owner sets on a negotiation are Closed and Expired.
    [Theory]
    [InlineData("\"Open\"")]
    [InlineData("\"Withdrawn\"")]
    [InlineData("\"Accepted\"")]
    [InlineData("\"Cancelled\"")]
    [InlineData("\"closed\"")]
    [InlineData("1")]
    [InlineData("null")]
    public async Task E7_E8_TdAdm178_AnyOtherNegotiationStatus_Refused(string statusJson)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("negotiations", seed.Open.Id, session);

        using var response = await PatchRawAsync("negotiations", seed.Open.Id, $$"""{"status":{{statusJson}}}""", session, read.ETag);

        await AssertGenericDenyAsync(response);
        var stored = await NegotiationAsync(seed.Open.Id);
        Assert.Equal(NegotiationStatus.Open, stored.Status);
        Assert.Equal(0u, stored.Version);
        foreach (var open in seed.OpenOffers)
            Assert.Equal(OfferStatus.Open, (await OfferAsync(open.Id)).Status);
        Assert.Empty(await AuditAsync(seed.Open.Id));
    }

    // ---- E12: offer Open to Cancelled -----------------------------------------

    // E12, TD-ADM-178 step 4 (Open to Cancelled), TD-ADM-080
    [Fact]
    public async Task E12_TdAdm178_Cancel_OpenOffer_LeavesWhatWasSent()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var target = seed.OpenOffers[0];
        var read = await GetAsync("offers", target.Id, session);
        var sent = await OfferTermsAsync([target.Id]);

        using var response = await PatchAsync("offers", target.Id, new { status = "Cancelled" }, session, read.ETag);

        var body = await OkAsync(response);
        Assert.Equal("Cancelled", body.GetProperty("status").GetString());
        Assert.Equal(TermsAsSent, body.GetProperty("terms").GetString());
        var stored = await OfferAsync(target.Id);
        Assert.Equal(OfferStatus.Cancelled, stored.Status);
        Assert.Equal(1u, stored.Version);
        Assert.NotNull(stored.UpdatedAt);
        Assert.Equal(sent, await OfferTermsAsync([target.Id]));

        var audit = Assert.Single(await AuditAsync(target.Id));
        AssertEditAudit(audit, "Offer", target.Id);
        Assert.Null(audit.CorrelationId);
        AssertSnapshot(audit.BeforeSnapshot, ("OfferStatus", "Open"), ("EntityVersion", "0"));
        AssertSnapshot(audit.AfterSnapshot, ("OfferStatus", "Cancelled"), ("EntityVersion", "1"));

        // The negotiation and its other offers are not part of this write.
        Assert.Equal(0u, (await NegotiationAsync(seed.Open.Id)).Version);
        Assert.Equal(OfferStatus.Open, (await OfferAsync(seed.OpenOffers[1].Id)).Status);

        // Cancelled is final.
        using var again = await PatchAsync("offers", target.Id, new { status = "Cancelled" }, session, ETagOf(response));
        await AssertGenericDenyAsync(again);
        Assert.Equal(1u, (await OfferAsync(target.Id)).Version);
    }

    // TD-ADM-178 step 4: the owner never sets Accepted, Declined, Superseded or Withdrawn (PROD-W).
    [Theory]
    [InlineData("\"Accepted\"")]
    [InlineData("\"Declined\"")]
    [InlineData("\"Withdrawn\"")]
    [InlineData("\"Superseded\"")]
    [InlineData("\"Open\"")]
    [InlineData("\"cancelled\"")]
    [InlineData("4")]
    [InlineData("null")]
    public async Task E12_TdAdm178_AnOutcomeTheOwnerMayNotSet_Refused(string statusJson)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var target = seed.OpenOffers[0];
        var read = await GetAsync("offers", target.Id, session);

        using var response = await PatchRawAsync("offers", target.Id, $$"""{"status":{{statusJson}}}""", session, read.ETag);

        await AssertGenericDenyAsync(response);
        var stored = await OfferAsync(target.Id);
        Assert.Equal(OfferStatus.Open, stored.Status);
        Assert.Equal(0u, stored.Version);
        Assert.Empty(await AuditAsync(target.Id));
    }

    // TD-ADM-178 step 4: any change from each final status. The reply is the same for all of them.
    [Fact]
    public async Task E12_TdAdm178_EveryOtherOfferStatusIsFinal_SameRefusal()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var bodies = new HashSet<string>(StringComparer.Ordinal);

        foreach (var final in new[] { seed.Accepted, seed.Declined, seed.Withdrawn, seed.Superseded, seed.Cancelled })
        {
            var read = await GetAsync("offers", final.Id, session);
            using var response = await PatchAsync("offers", final.Id, new { status = "Cancelled" }, session, read.ETag);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            bodies.Add(await response.Content.ReadAsStringAsync());
            var stored = await OfferAsync(final.Id);
            Assert.Equal(final.Status, stored.Status);
            Assert.Equal(0u, stored.Version);
            Assert.Empty(await AuditAsync(final.Id));
        }

        // One body for all five: it does not say which status the offer has.
        Assert.Equal(GenericDeny, Assert.Single(bodies));
    }

    // ---- E9, E10, E11 are not built: what a party sent stays as sent ----------

    // TD-ADM-005 (denied field: no write, no dump), TD-ADM-178 step 5; the owner's decision "Cancel only".
    [Theory]
    [InlineData("""{"amount":1}""")]
    [InlineData("""{"currency":"EUR"}""")]
    [InlineData("""{"terms":"rewritten by the console"}""")]
    [InlineData("""{"amount":1,"currency":"EUR","terms":"rewritten by the console"}""")]
    [InlineData("""{"status":"Cancelled","amount":1}""")]
    [InlineData("""{"status":"Cancelled","terms":null}""")]
    [InlineData("""{"amount":null}""")]
    public async Task E9_E10_E11_TdAdm005_AmountCurrencyTerms_NotEditable_NothingWritten(string body)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var target = seed.OpenOffers[0];
        var read = await GetAsync("offers", target.Id, session);
        var sent = await OfferTermsAsync([target.Id]);

        using var response = await PatchRawAsync("offers", target.Id, body, session, read.ETag);

        // The generic refusal: it repeats neither the value sent nor the value stored.
        await AssertGenericDenyAsync(response);
        Assert.Equal(sent, await OfferTermsAsync([target.Id]));
        var stored = await OfferAsync(target.Id);
        Assert.Equal(OfferStatus.Open, stored.Status);
        Assert.Equal(0u, stored.Version);
        Assert.Null(stored.UpdatedAt);
        Assert.Empty(await AuditAsync(target.Id));
    }

    // After every admin write that touches an offer, its amount, currency and terms are the bytes the party sent.
    [Fact]
    public async Task E9_E10_E11_AfterEveryAdminWritePath_AmountCurrencyTermsAreByteIdentical()
    {
        var session = await NewSessionAsync();

        // E12: cancel one offer.
        var first = await SeedAsync();
        var firstSent = await OfferTermsAsync(first.AllOffers.Select(o => o.Id));
        var read = await GetAsync("offers", first.OpenOffers[0].Id, session);
        using (var cancel = await PatchAsync("offers", first.OpenOffers[0].Id, new { status = "Cancelled" }, session, read.ETag))
            await OkAsync(cancel);
        // E6, then E7: move the end time, then close, which cancels the rest.
        var negotiation = await GetAsync("negotiations", first.Open.Id, session);
        using var moved = await PatchAsync(
            "negotiations", first.Open.Id, new { endsAt = DateTimeOffset.UtcNow.AddDays(9).ToString("O") }, session, negotiation.ETag);
        await OkAsync(moved);
        using (var close = await PatchAsync("negotiations", first.Open.Id, new { status = "Closed" }, session, ETagOf(moved)))
            await OkAsync(close);
        Assert.Equal(firstSent, await OfferTermsAsync(first.AllOffers.Select(o => o.Id)));

        // E8: expire.
        var second = await SeedAsync();
        var secondSent = await OfferTermsAsync(second.AllOffers.Select(o => o.Id));
        negotiation = await GetAsync("negotiations", second.Open.Id, session);
        using (var expire = await PatchAsync("negotiations", second.Open.Id, new { status = "Expired" }, session, negotiation.ETag))
            await OkAsync(expire);
        Assert.Equal(secondSent, await OfferTermsAsync(second.AllOffers.Select(o => o.Id)));

        // The offers were written (cancelled, next version), so the comparison above is of written rows.
        foreach (var open in first.OpenOffers.Concat(second.OpenOffers))
        {
            var offer = await OfferAsync(open.Id);
            Assert.Equal(OfferStatus.Cancelled, offer.Status);
            Assert.Equal(1u, offer.Version);
        }
    }

    // ---- Fields that are read-only by design ----------------------------------

    // TD-ADM-005, TD-ADM-141, TD-ADM-178 steps 6 and 7 (writes to Id, Sub, LoginEmail, ContactEmail, CreatedAt, Intent, values)
    [Theory]
    [InlineData("participants", """{"id":"7b1b6a0e-3f3b-4d0c-9c64-0f2f6f4f4c11"}""")]
    [InlineData("participants", """{"sub":"participant:other"}""")]
    [InlineData("participants", """{"loginEmail":"probe-login@example.test"}""")]
    [InlineData("participants", """{"contactEmail":"probe-contact@example.test"}""")]
    [InlineData("participants", """{"createdAt":"2020-01-01T00:00:00Z"}""")]
    [InlineData("participants", """{"version":99}""")]
    [InlineData("participants", """{"deletedAt":null}""")]
    [InlineData("participants", """{"displayName":"ok","passwordHash":"probe-value"}""")]
    [InlineData("participants", """{"displayName":"one","displayName":"two"}""")]
    [InlineData("participants", """{"DisplayName":"wrong case"}""")]
    [InlineData("artifacts", """{"intent":"buy"}""")]
    [InlineData("artifacts", """{"values":[{"amount":1,"currency":"USD"}]}""")]
    [InlineData("artifacts", """{"entities":[{"name":"x"}]}""")]
    [InlineData("artifacts", """{"createdAt":"2020-01-01T00:00:00Z"}""")]
    [InlineData("artifacts", """{"entityId":"7b1b6a0e-3f3b-4d0c-9c64-0f2f6f4f4c11"}""")]
    [InlineData("artifacts", """{"name":"x","entityId":"not-an-id"}""")]
    [InlineData("negotiations", """{"partyAParticipantId":"participant:other"}""")]
    [InlineData("negotiations", """{"partyBParticipantId":"participant:other"}""")]
    [InlineData("negotiations", """{"artifactId":"7b1b6a0e-3f3b-4d0c-9c64-0f2f6f4f4c11"}""")]
    [InlineData("negotiations", """{"startsAt":"2020-01-01T00:00:00Z"}""")]
    [InlineData("negotiations", """{"partyAIntent":"buy"}""")]
    [InlineData("offers", """{"negotiationId":"7b1b6a0e-3f3b-4d0c-9c64-0f2f6f4f4c11"}""")]
    [InlineData("offers", """{"fromParticipantId":"participant:other"}""")]
    [InlineData("offers", """{"toParticipantId":"participant:other"}""")]
    [InlineData("offers", """{"createdAt":"2020-01-01T00:00:00Z"}""")]
    public async Task TdAdm005_ReadOnlyOrUnknownField_SameRefusal_NothingWritten(string type, string body)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var id = seed.EditableId(type);
        var read = await GetAsync(type, id, session);

        using var response = await PatchRawAsync(type, id, body, session, read.ETag);

        await AssertGenericDenyAsync(response);
        var after = await GetAsync(type, id, session);
        Assert.Equal(read.ETag, after.ETag);
        Assert.Equal(read.Body.GetRawText(), after.Body.GetRawText());
        Assert.Empty(await AuditAsync(id));
    }

    // A body that cannot be read as one JSON object of fields gets the same refusal.
    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("\"displayName\"")]
    [InlineData("""{"displayName":{"a":{"b":{"c":{"d":1}}}}}""")]
    public async Task TdAdm141_UnreadableBody_GenericRefusal(string body)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("participants", seed.PartyA.Id, session);

        using var response = await PatchRawAsync("participants", seed.PartyA.Id, body, session, read.ETag);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0u, (await ParticipantAsync(seed.PartyA.Id)).Version);
        Assert.Empty(await AuditAsync(seed.PartyA.Id));
    }

    [Fact]
    public async Task TdAdm141_OversizedBody_GenericRefusal()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("participants", seed.PartyA.Id, session);
        var body = JsonSerializer.Serialize(new { displayName = new string('x', AdminEditEndpoints.MaxBodyBytes) });

        using var response = await PatchRawAsync("participants", seed.PartyA.Id, body, session, read.ETag);

        await AssertGenericDenyAsync(response);
        Assert.Equal(0u, (await ParticipantAsync(seed.PartyA.Id)).Version);
    }

    // TD-ADM-141: a refusal repeats neither what was sent nor what is stored.
    [Fact]
    public async Task TdAdm141_ValidationReply_RepeatsNoValue()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("participants", seed.PartyA.Id, session);
        var probe = "PROBE-" + new string('z', 300);

        using var response = await PatchRawAsync(
            "participants", seed.PartyA.Id, JsonSerializer.Serialize(new { displayName = probe, isActive = "yes" }), session, read.ETag);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("PROBE-", text);
        Assert.DoesNotContain(seed.PartyA.DisplayName!, text);
        Assert.DoesNotContain(seed.PartyA.LoginEmail!, text);
        var fields = JsonDocument.Parse(text).RootElement.GetProperty("fields");
        Assert.Equal(AdminEditCopy.DisplayNameTooLong, fields.GetProperty("displayName").GetString());
        Assert.Equal(AdminEditCopy.ActiveNotChosen, fields.GetProperty("isActive").GetString());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"isActive":true}""")]
    public async Task NothingToChange_SaidSo_NothingWritten(string body)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("participants", seed.PartyA.Id, session);

        using var response = await PatchRawAsync("participants", seed.PartyA.Id, body, session, read.ETag);

        await AssertFieldErrorAsync(response, AdminEditCopy.FormKey, AdminEditCopy.NoChanges);
        Assert.Equal(0u, (await ParticipantAsync(seed.PartyA.Id)).Version);
        Assert.Empty(await AuditAsync(seed.PartyA.Id));
    }

    // ---- The gates every write passes ------------------------------------------

    // TD-ADM-003 / TD-ADM-005 fail closed: no session, no write.
    [Theory]
    [MemberData(nameof(EveryType))]
    public async Task TdAdm003_NoSession_401_OnEachRoute(string type)
    {
        var seed = await SeedAsync();
        var id = seed.EditableId(type);
        var read = await GetAsync(type, id, await NewSessionAsync());

        using var none = await PatchRawAsync(type, id, seed.ValidBody(type), session: null, read.ETag);
        using var forged = await PatchRawAsync(type, id, seed.ValidBody(type), Guid.NewGuid(), read.ETag);

        foreach (var response in new[] { none, forged })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("""{"error":"Unauthorized"}""", await response.Content.ReadAsStringAsync());
        }

        await AssertUntouchedAsync(type, id);
    }

    // TD-ADM-130 / Spec §8: every PATCH is behind the anti-forgery token and the Origin gate. No exemption.
    [Theory]
    [MemberData(nameof(EveryType))]
    public async Task NoAntiForgeryToken_OrWrongOrigin_403_OnEachRoute(string type)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var id = seed.EditableId(type);
        var read = await GetAsync(type, id, session);

        using var noToken = await PatchRawAsync(type, id, seed.ValidBody(type), session, read.ETag, antiForgery: false);
        using var noOrigin = await PatchRawAsync(type, id, seed.ValidBody(type), session, read.ETag, origin: null);
        using var otherSite = await PatchRawAsync(type, id, seed.ValidBody(type), session, read.ETag, origin: "https://evil.example");

        foreach (var response in new[] { noToken, noOrigin, otherSite })
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("""{"error":"Forbidden"}""", await response.Content.ReadAsStringAsync());
        }

        await AssertUntouchedAsync(type, id);
    }

    // Spec §5: a write names the state it was prepared against. Without it, 428; unreadable, 400.
    [Theory]
    [MemberData(nameof(EveryType))]
    public async Task IfMatch_Missing428_NotOneOfOurs400_OnEachRoute(string type)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var id = seed.EditableId(type);
        var read = await GetAsync(type, id, session);

        using var missing = await PatchRawAsync(type, id, seed.ValidBody(type), session, ifMatch: null);
        Assert.Equal((HttpStatusCode)428, missing.StatusCode);
        Assert.Equal("""{"error":"PreconditionRequired"}""", await missing.Content.ReadAsStringAsync());

        foreach (var notOurs in new[] { "*", "\"0\"", "0", "\"\"", $"{read.ETag}, {read.ETag}", "\"0-xyz\"" })
        {
            using var response = await PatchRawAsync(type, id, seed.ValidBody(type), session, notOurs);
            await AssertGenericDenyAsync(response);
        }

        await AssertUntouchedAsync(type, id);
    }

    // ---- 409: no silent overwrite ------------------------------------------------

    // TD-ADM-081: read V, the row moves to V+1 elsewhere, the write with V is refused.
    [Theory]
    [MemberData(nameof(EveryType))]
    public async Task TdAdm081_StaleIfMatch_409_OnEachRoute(string type)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var id = seed.EditableId(type);
        var stale = await GetAsync(type, id, session);

        // Another editor's write lands first.
        using (var first = await PatchRawAsync(type, id, seed.ValidBody(type), session, stale.ETag))
            await OkAsync(first);
        var afterFirst = await GetAsync(type, id, session);
        Assert.NotEqual(stale.ETag, afterFirst.ETag);

        using var second = await PatchRawAsync(type, id, seed.OtherValidBody(type), session, stale.ETag);

        await AssertConflictAsync(second);
        var afterSecond = await GetAsync(type, id, session);
        Assert.Equal(afterFirst.ETag, afterSecond.ETag);
        Assert.Equal(afterFirst.Body.GetRawText(), afterSecond.Body.GetRawText());
        Assert.Single(await AuditAsync(id));
    }

    // A token is for one row: another row's token, even with the same version, does not open this one.
    [Fact]
    public async Task TdAdm081_AnotherRowsToken_409()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var other = await GetAsync("participants", seed.Suspended.Id, session);

        using var response = await PatchAsync("participants", seed.PartyA.Id, new { displayName = "x" }, session, other.ETag);

        await AssertConflictAsync(response);
        Assert.Equal(0u, (await ParticipantAsync(seed.PartyA.Id)).Version);
    }

    // The detail read and the write give the same token for the same state; a proxy's W/ is tolerated.
    [Fact]
    public async Task ETag_OnDetailReads_IsWhatTheWriteReturns_AndWhatTheNextWriteTakes()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        foreach (var type in Types)
        {
            var read = await GetAsync(type, seed.EditableId(type), session);
            Assert.Matches("^\"[0-9]+-[0-9a-f]{16}\"$", read.ETag);
            Assert.StartsWith($"\"{read.Body.GetProperty("version").GetUInt32()}-", read.ETag);
        }

        var first = await GetAsync("participants", seed.PartyA.Id, session);
        using var write = await PatchAsync("participants", seed.PartyA.Id, new { displayName = "Renamed" }, session, "W/" + first.ETag);
        await OkAsync(write);
        var reread = await GetAsync("participants", seed.PartyA.Id, session);
        Assert.Equal(reread.ETag, ETagOf(write));
        Assert.StartsWith("\"1-", reread.ETag);
    }

    // ---- Soft-deleted and missing rows ---------------------------------------------

    // TD-ADM-178 / TD-ADM-UI-na-B09: a direct write to a soft-deleted record is refused, as for no record.
    [Theory]
    [MemberData(nameof(EveryType))]
    public async Task SoftDeletedRow_Refused_AsIfNotThere_OnEachRoute(string type)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var id = seed.DeletedId(type);
        var read = await GetAsync(type, id, session, includeDeleted: true);

        using var deleted = await PatchRawAsync(type, id, seed.ValidBody(type), session, read.ETag);
        using var missing = await PatchRawAsync(type, Guid.NewGuid(), seed.ValidBody(type), session, read.ETag);

        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(await missing.Content.ReadAsStringAsync(), await deleted.Content.ReadAsStringAsync());

        var after = await GetAsync(type, id, session, includeDeleted: true);
        Assert.Equal(read.ETag, after.ETag);
        Assert.Empty(await AuditAsync(id));
    }

    // The reply to a write is the detail read: a soft-deleted owner's name stays hidden, as on the lists.
    [Fact]
    public async Task WriteReply_KeepsTheNameOfASoftDeletedReferenceHidden()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("artifacts", seed.OrphanArtifact.Id, session);

        using var response = await PatchAsync("artifacts", seed.OrphanArtifact.Id, new { description = "still here" }, session, read.ETag);

        var body = await OkAsync(response);
        Assert.True(body.GetProperty("ownerDeleted").GetBoolean());
        Assert.False(body.TryGetProperty("ownerName", out _));
        Assert.DoesNotContain(seed.DeletedParticipant.DisplayName!, body.GetRawText());
    }

    // ---- The audit row ---------------------------------------------------------------

    // TD-ADM-080 step 3, Spec §10: allowed fields only, no credential, no raw address.
    [Fact]
    public async Task TdAdm080_AuditRow_HoldsOnlyTheNamedFields_NoCredentialNoAddress()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("participants", seed.PartyA.Id, session);

        using var response = await PatchAsync(
            "participants", seed.PartyA.Id, new { displayName = "Alpha, renamed" }, session, read.ETag);
        await OkAsync(response);

        var audit = Assert.Single(await AuditAsync(seed.PartyA.Id));
        Assert.Equal(OwnerEmail, audit.ActorEmail);
        Assert.Equal(_factory.Services.GetRequiredService<IIpHasher>().Hash("unknown"), audit.IpHmac);
        foreach (var snapshot in new[] { audit.BeforeSnapshot!, audit.AfterSnapshot! })
        {
            var names = JsonDocument.Parse(snapshot).RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray();
            Assert.Equal(["DisplayName", "EntityVersion"], names);
            Assert.DoesNotContain(seed.PartyA.LoginEmail!, snapshot);
            Assert.DoesNotContain(seed.PartyA.ContactEmail!, snapshot);
            Assert.DoesNotContain(seed.PartyA.Sub, snapshot);
            Assert.DoesNotContain(session.ToString("D"), snapshot);
        }

        // And it reads back through the audit API as written.
        using var list = await SendAsync(
            HttpMethod.Get, $"/admin/api/audit?action=entity_edit&entityId={seed.PartyA.Id}", null, session);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listed = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, listed.GetProperty("total").GetInt32());
        var row = listed.GetProperty("items")[0];
        Assert.Equal("Participant", row.GetProperty("entityType").GetString());
        Assert.Equal(audit.AfterSnapshot, row.GetProperty("afterSnapshot").GetString());
    }

    // Spec §10 large snapshots: a long text is stored as its beginning, its length and its hash.
    [Fact]
    public async Task TdAdm080_LongText_AuditKeepsBeginningLengthAndHash()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("artifacts", seed.Artifact.Id, session);
        var name = new string('n', 4096);
        var description = string.Concat(Enumerable.Repeat("é—✓ ", 1024));

        using var response = await PatchAsync("artifacts", seed.Artifact.Id, new { name, description }, session, read.ETag);
        await OkAsync(response);

        var audit = Assert.Single(await AuditAsync(seed.Artifact.Id));
        Assert.True(audit.AfterSnapshot!.Length <= AdminAuditSnapshots.MaxSnapshotChars);
        var after = JsonDocument.Parse(audit.AfterSnapshot).RootElement;
        Assert.False(after.TryGetProperty("_invalid", out _));
        foreach (var (key, full) in new[] { ("ArtifactName", name), ("ArtifactDescription", description) })
        {
            var stored = after.GetProperty(key);
            Assert.StartsWith(stored.GetProperty("truncated").GetString()!, full);
            Assert.Equal(full.Length, stored.GetProperty("length").GetInt32());
            Assert.Equal(
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(full))),
                stored.GetProperty("sha256").GetString());
        }

        Assert.Equal("widget-desc", JsonDocument.Parse(audit.BeforeSnapshot!).RootElement.GetProperty("ArtifactDescription").GetString());
    }

    // ---- Harness ------------------------------------------------------------------------

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
        Negotiation DeletedNegotiation,
        IReadOnlyList<Offer> OpenOffers,
        Offer Accepted,
        Offer Declined,
        Offer Withdrawn,
        Offer Superseded,
        Offer Cancelled,
        Offer DeletedOffer)
    {
        public IEnumerable<Offer> AllOffers
            => OpenOffers.Concat([Accepted, Declined, Withdrawn, Superseded, Cancelled, DeletedOffer]);

        /// <summary>A live row of the type that a valid edit can change.</summary>
        public Guid EditableId(string type) => type switch
        {
            "participants" => PartyA.Id,
            "artifacts" => Artifact.Id,
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

        public string ValidBody(string type) => type switch
        {
            "participants" => """{"displayName":"First writer"}""",
            "artifacts" => """{"name":"First writer"}""",
            "negotiations" => """{"endsAt":"2031-01-01T00:00:00Z"}""",
            _ => """{"status":"Cancelled"}"""
        };

        public string OtherValidBody(string type) => type switch
        {
            "participants" => """{"displayName":"Second writer"}""",
            "artifacts" => """{"name":"Second writer"}""",
            "negotiations" => """{"status":"Closed"}""",
            _ => """{"status":"Cancelled"}"""
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
            partyA.Sub,
            [SubjectEntity.Create($"{tag}-First", "first"), SubjectEntity.Create($"{tag}-Second", "second")],
            "sell");
        var orphan = Artifact.Create(deletedParty.Sub, [SubjectEntity.Create($"{tag}-Orphan", "owner is deleted")], "sell");
        var deletedArtifact = Artifact.Create(partyA.Sub, [SubjectEntity.Create($"{tag}-GoneWidget", "gone")], "sell");
        db.Artifacts.AddRange(artifact, twoEntities, orphan, deletedArtifact);

        var start = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
        // Whole seconds, so the value reads back the same from every store.
        var open = NewNegotiation(artifact, partyA, partyB, start, new DateTimeOffset(2030, 6, 1, 9, 0, 0, TimeSpan.Zero));
        var closed = NewNegotiation(artifact, partyA, partyB);
        closed.Close();
        var expired = NewNegotiation(artifact, partyA, partyB);
        expired.Expire();
        var deletedNegotiation = NewNegotiation(artifact, partyA, partyB);
        db.Negotiations.AddRange(open, closed, expired, deletedNegotiation);

        // Three open offers and one accepted on the open negotiation (TD-ADM-178 step 2).
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
        var deletedOffer = NewOffer(deletedNegotiation, partyA, partyB, 9m);
        db.Offers.AddRange(openOffers);
        db.Offers.AddRange(accepted, declined, withdrawn, superseded, cancelled, deletedOffer);

        await db.SaveChangesAsync();

        var at = DateTimeOffset.UtcNow;
        await db.Participants.Where(p => p.Id == deletedParty.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, at));
        await db.Artifacts.Where(a => a.Id == deletedArtifact.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.DeletedAt, at));
        await db.Negotiations.Where(n => n.Id == deletedNegotiation.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, at));
        await db.Offers.Where(o => o.Id == deletedOffer.Id).ExecuteUpdateAsync(s => s.SetProperty(o => o.DeletedAt, at));

        return new Seed(
            partyA, partyB, suspended, deletedParty,
            artifact, twoEntities, orphan, deletedArtifact,
            open, closed, expired, deletedNegotiation,
            openOffers, accepted, declined, withdrawn, superseded, cancelled, deletedOffer);
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

    private async Task<Guid> NewSessionAsync(WebApplicationFactory<Program>? factory = null)
    {
        using var scope = (factory ?? _factory).Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(OwnerEmail, "testhmac-not-an-ip", Clock().UtcNow);
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

    private async Task AssertUntouchedAsync(string type, Guid id)
    {
        var version = type switch
        {
            "participants" => (await ParticipantAsync(id)).Version,
            "artifacts" => (await ArtifactAsync(id)).Version,
            "negotiations" => (await NegotiationAsync(id)).Version,
            _ => (await OfferAsync(id)).Version
        };
        Assert.Equal(0u, version);
        Assert.Empty(await AuditAsync(id));
    }

    private sealed record DetailRead(JsonElement Body, string ETag);

    private async Task<DetailRead> GetAsync(
        string type,
        Guid id,
        Guid session,
        bool includeDeleted = false,
        WebApplicationFactory<Program>? factory = null)
    {
        var path = $"/admin/api/{type}/{id}" + (includeDeleted ? "?includeDeleted=true" : string.Empty);
        using var response = await SendAsync(HttpMethod.Get, path, null, session, factory: factory);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return new DetailRead(await response.Content.ReadFromJsonAsync<JsonElement>(), ETagOf(response));
    }

    private static string ETagOf(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("ETag", out var values), "The reply has no ETag.");
        return Assert.Single(values!);
    }

    private Task<HttpResponseMessage> PatchAsync(
        string type,
        Guid id,
        object body,
        Guid? session,
        string? ifMatch,
        WebApplicationFactory<Program>? factory = null)
        => SendAsync(HttpMethod.Patch, $"/admin/api/{type}/{id}", JsonSerializer.Serialize(body), session, ifMatch, factory: factory);

    private Task<HttpResponseMessage> PatchRawAsync(
        string type,
        Guid id,
        string body,
        Guid? session,
        string? ifMatch,
        bool antiForgery = true,
        string? origin = AdminHostOptions.ProductionAdminOrigin)
        => SendAsync(HttpMethod.Patch, $"/admin/api/{type}/{id}", body, session, ifMatch, antiForgery, origin);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        string? body,
        Guid? session,
        string? ifMatch = null,
        bool antiForgery = true,
        string? origin = AdminHostOptions.ProductionAdminOrigin,
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

    /// <summary>The one body of every refusal that is not about a field's value.</summary>
    private static async Task AssertGenericDenyAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{(int)response.StatusCode}: {text}");
        Assert.Equal(GenericDeny, text);
    }

    private static async Task AssertConflictAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Conflict, $"{(int)response.StatusCode}: {text}");
        var body = JsonDocument.Parse(text).RootElement;
        Assert.Equal("Conflict", body.GetProperty("error").GetString());
        Assert.Equal(AdminEditCopy.Conflict, body.GetProperty("message").GetString());
        Assert.Equal(2, body.EnumerateObject().Count());
    }

    private static async Task AssertFieldErrorAsync(HttpResponseMessage response, string field, string message)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{(int)response.StatusCode}: {text}");
        var body = JsonDocument.Parse(text).RootElement;
        Assert.Equal("BadRequest", body.GetProperty("error").GetString());
        var only = Assert.Single(body.GetProperty("fields").EnumerateObject());
        Assert.Equal(field, only.Name);
        Assert.Equal(message, only.Value.GetString());
    }

    private static void AssertEditAudit(AdminAuditEntry audit, string entityType, Guid entityId)
    {
        Assert.Equal(AdminAuditActions.EntityEdit, audit.Action);
        Assert.Equal("entity_edit", audit.Action);
        Assert.Equal(entityType, audit.EntityType);
        Assert.Equal(entityId, audit.EntityId);
        Assert.Equal(OwnerEmail, audit.ActorEmail);
        Assert.Null(audit.ReasonClass);
        Assert.NotNull(audit.BeforeSnapshot);
        Assert.NotNull(audit.AfterSnapshot);
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
}
