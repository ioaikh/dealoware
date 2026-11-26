using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Endpoints;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Negotiations.Dtos;
using Dealoware.Application.Profile.Dtos;
using Dealoware.Domain.Admin;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Dealoware.Api.Tests;

/// <summary>
/// An admin write never saves over a party's action, and a write and its
/// audit rows land together or not at all.
/// <para>
/// The party acts through the engine's own routes (place, counter, accept,
/// close, profile), which are as they were: none of them moves
/// <c>Version</c>. The tests put the party's action at each place it can
/// fall:
/// </para>
/// <list type="bullet">
/// <item>between the admin's read of the record and the write request;</item>
/// <item>inside the write request, between the server's read of the row and
/// its transaction (the write's unit of work is wrapped, so the party's
/// request runs at exactly that point);</item>
/// <item>at the same time, with both transactions live: one holds the row,
/// the other is seen to wait on it, then the first commits. These need row
/// locks, so they run on PostgreSQL only (<see cref="PostgresOnlyFactAttribute"/>).</item>
/// </list>
/// </summary>
public partial class AdminEditApiTests
{
    // ---- A party action between the admin's read and the admin's write -------

    // E12, TD-ADM-081 (no silent overwrite): the party accepts after the admin opened the offer.
    [Fact]
    public async Task E12_PartyAcceptsAfterTheAdminReadTheOffer_AdminCancel409_AcceptStands()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("offers", deal.OfferId, session);
        Assert.Equal("Open", read.Body.GetProperty("status").GetString());

        // The party's accept does not move Version: a token of Version alone would still match.
        using (var accept = await deal.Recipient.PostAsync($"/offers/{deal.OfferId}/accept", null))
            Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        Assert.Equal(0u, (await OfferAsync(deal.OfferId)).Version);

        using var cancel = await PatchAsync("offers", deal.OfferId, new { status = "Cancelled" }, session, read.ETag);

        await AssertConflictAsync(cancel);
        await AssertAcceptStandsAsync(deal);
    }

    // E12: the same, with the party's accept landing inside the admin's write request,
    // after the server read the row (still Open) and before its transaction.
    [Fact]
    public async Task E12_PartyAcceptsBetweenTheServersReadAndItsWrite_AdminCancel409_AcceptStands()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("offers", deal.OfferId, session);
        var interleaved = 0;
        await using var host = WithUnitOfWork(beforeTransaction: async () =>
        {
            interleaved++;
            // The admin's request has read the offer as Open and matched the token.
            using var accept = await deal.Recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);
            Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        });

        using var cancel = await PatchAsync("offers", deal.OfferId, new { status = "Cancelled" }, session, read.ETag, host);

        Assert.Equal(1, interleaved);
        await AssertConflictAsync(cancel);
        await AssertAcceptStandsAsync(deal);
    }

    // E7 and E8: a close prepared before a party placed or countered an offer must not cancel
    // the offer the admin never saw. The negotiation's token covers its open offers.
    [Theory]
    [InlineData("Closed", "place")]
    [InlineData("Closed", "counter")]
    [InlineData("Expired", "place")]
    [InlineData("Expired", "counter")]
    public async Task E7_E8_PartyAddsAnOfferAfterTheAdminReadTheNegotiation_Close409_TheNewOfferIsNotCancelled(
        string status,
        string how)
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("negotiations", deal.NegotiationId, session);
        Assert.Equal(1, read.Body.GetProperty("openOfferCount").GetInt32());

        // Neither route writes the negotiation's own row.
        var route = how == "place" ? $"/negotiations/{deal.NegotiationId}/offers" : $"/offers/{deal.OfferId}/counter";
        using var added = await deal.Recipient.PostAsJsonAsync(
            route, new CreateOfferRequest { Amount = 900m, Currency = "USD", Terms = "the offer the admin never saw" });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var newOffer = await added.Content.ReadFromJsonAsync<OfferResponse>();
        var untouched = await NegotiationAsync(deal.NegotiationId);
        Assert.Equal(0u, untouched.Version);
        Assert.Null(untouched.UpdatedAt);

        using var close = await PatchAsync("negotiations", deal.NegotiationId, new { status }, session, read.ETag);

        await AssertConflictAsync(close);
        Assert.Equal(NegotiationStatus.Open, (await NegotiationAsync(deal.NegotiationId)).Status);
        var stored = await OfferAsync(newOffer!.Id);
        Assert.Equal(OfferStatus.Open, stored.Status);
        Assert.Equal(0u, stored.Version);
        Assert.Equal(
            how == "place" ? OfferStatus.Open : OfferStatus.Superseded,
            (await OfferAsync(deal.OfferId)).Status);
        Assert.Empty(await AuditAsync(deal.NegotiationId));
        Assert.Empty(await AuditAsync(newOffer.Id));

        // Read again, the admin sees the new offer, and the close covers it.
        var again = await GetAsync("negotiations", deal.NegotiationId, session);
        Assert.Equal(how == "place" ? 2 : 1, again.Body.GetProperty("openOfferCount").GetInt32());
        using var retry = await PatchAsync("negotiations", deal.NegotiationId, new { status }, session, again.ETag);
        await OkAsync(retry);
        Assert.Equal(OfferStatus.Cancelled, (await OfferAsync(newOffer.Id)).Status);
        Assert.Single(await AuditAsync(newOffer.Id));
    }

    // E7 and E8: an accept after the admin's read is seen the same way, by the token.
    [Fact]
    public async Task E7_PartyAcceptsAfterTheAdminReadTheNegotiation_Close409_AcceptStands()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("negotiations", deal.NegotiationId, session);
        using (var accept = await deal.Recipient.PostAsync($"/offers/{deal.OfferId}/accept", null))
            Assert.Equal(HttpStatusCode.OK, accept.StatusCode);

        using var close = await PatchAsync("negotiations", deal.NegotiationId, new { status = "Closed" }, session, read.ETag);

        await AssertConflictAsync(close);
        await AssertAcceptStandsAsync(deal);
        Assert.Equal(NegotiationStatus.Open, (await NegotiationAsync(deal.NegotiationId)).Status);
    }

    // E7 and E8: closing or expiring must not cancel an offer a party accepted meanwhile.
    [Theory]
    [InlineData("Closed")]
    [InlineData("Expired")]
    public async Task E7_E8_PartyAcceptsBetweenTheServersReadAndItsWrite_Close409_NothingCancelled(string status)
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("negotiations", deal.NegotiationId, session);
        Assert.Equal(1, read.Body.GetProperty("openOfferCount").GetInt32());
        await using var host = WithUnitOfWork(beforeTransaction: async () =>
        {
            using var accept = await deal.Recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);
            Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        });

        using var close = await PatchAsync("negotiations", deal.NegotiationId, new { status }, session, read.ETag, host);

        await AssertConflictAsync(close);
        await AssertAcceptStandsAsync(deal);
        var negotiation = await NegotiationAsync(deal.NegotiationId);
        Assert.Equal(NegotiationStatus.Open, negotiation.Status);
        Assert.Equal(0u, negotiation.Version);
        Assert.Empty(await AuditAsync(deal.NegotiationId));

        // Read again, the admin can still close it: the accepted offer stays accepted.
        var again = await GetAsync("negotiations", deal.NegotiationId, session);
        using var retry = await PatchAsync("negotiations", deal.NegotiationId, new { status }, session, again.ETag);
        await OkAsync(retry);
        Assert.Equal(OfferStatus.Accepted, (await OfferAsync(deal.OfferId)).Status);
    }

    // E7: a party places an offer inside the admin's close, after the server read the open offers.
    [Fact]
    public async Task E7_PartyPlacesAnOfferBetweenTheServersReadAndItsWrite_Close409_TheNewOfferIsNotLeftBehind()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("negotiations", deal.NegotiationId, session);
        Guid? newOfferId = null;
        await using var host = WithUnitOfWork(beforeTransaction: async () =>
        {
            using var placed = await deal.Recipient.PostAsJsonAsync(
                $"/negotiations/{deal.NegotiationId}/offers", new CreateOfferRequest { Amount = 900m, Currency = "USD" });
            Assert.Equal(HttpStatusCode.Created, placed.StatusCode);
            newOfferId = (await placed.Content.ReadFromJsonAsync<OfferResponse>())!.Id;
        });

        using var close = await PatchAsync("negotiations", deal.NegotiationId, new { status = "Closed" }, session, read.ETag, host);

        // Closed with an open offer in it would be the fault; the write stops instead.
        await AssertConflictAsync(close);
        Assert.Equal(NegotiationStatus.Open, (await NegotiationAsync(deal.NegotiationId)).Status);
        Assert.Equal(OfferStatus.Open, (await OfferAsync(newOfferId!.Value)).Status);
        Assert.Equal(OfferStatus.Open, (await OfferAsync(deal.OfferId)).Status);
        Assert.Empty(await AuditAsync(deal.NegotiationId));
    }

    // E7: a party closes the negotiation itself while the admin's close is under way.
    [Fact]
    public async Task E7_PartyClosesBetweenTheServersReadAndItsWrite_AdminClose409_OneClose()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("negotiations", deal.NegotiationId, session);
        await using var host = WithUnitOfWork(beforeTransaction: async () =>
        {
            using var close = await deal.Maker.PostAsync($"/negotiations/{deal.NegotiationId}/close", null);
            Assert.Equal(HttpStatusCode.OK, close.StatusCode);
        });

        using var adminClose = await PatchAsync("negotiations", deal.NegotiationId, new { status = "Expired" }, session, read.ETag, host);

        await AssertConflictAsync(adminClose);
        var negotiation = await NegotiationAsync(deal.NegotiationId);
        Assert.Equal(NegotiationStatus.Closed, negotiation.Status);
        Assert.Equal(0u, negotiation.Version);
        var offer = await OfferAsync(deal.OfferId);
        Assert.Equal(OfferStatus.Cancelled, offer.Status);
        Assert.Equal(0u, offer.Version);
        Assert.Empty(await AuditAsync(deal.NegotiationId));
        Assert.Empty(await AuditAsync(deal.OfferId));
    }

    // E6: the end time is not moved on a negotiation a party closed meanwhile.
    [Fact]
    public async Task E6_PartyClosesAfterTheAdminReadTheNegotiation_EndsAt409()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("negotiations", deal.NegotiationId, session);

        using (var close = await deal.Maker.PostAsync($"/negotiations/{deal.NegotiationId}/close", null))
            Assert.Equal(HttpStatusCode.OK, close.StatusCode);
        using var move = await PatchAsync(
            "negotiations", deal.NegotiationId, new { endsAt = "2031-01-01T00:00:00Z" }, session, read.ETag);

        await AssertConflictAsync(move);
        Assert.Null((await NegotiationAsync(deal.NegotiationId)).EndsAt);
        Assert.Empty(await AuditAsync(deal.NegotiationId));
    }

    // E2: a participant renames themselves; the admin's rename, prepared before, does not replace it.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task E2_PartyRenamesThemselves_AdminRename409_TheirNameStands(bool insideTheWriteRequest)
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var participantId = ParticipantIdOf(deal.MakerSub);
        var read = await GetAsync("participants", participantId, session);

        async Task PartyRenamesAsync()
        {
            using var rename = await deal.Maker.PatchAsJsonAsync("/profile", new UpdateProfileRequest { DisplayName = "My own new name" });
            Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        }

        HttpResponseMessage adminRename;
        if (insideTheWriteRequest)
        {
            await using var host = WithUnitOfWork(beforeTransaction: PartyRenamesAsync);
            adminRename = await PatchAsync("participants", participantId, new { displayName = "Admin's name" }, session, read.ETag, host);
        }
        else
        {
            await PartyRenamesAsync();
            adminRename = await PatchAsync("participants", participantId, new { displayName = "Admin's name" }, session, read.ETag);
        }

        using (adminRename)
            await AssertConflictAsync(adminRename);
        var participant = await ParticipantAsync(participantId);
        Assert.Equal("My own new name", participant.DisplayName);
        Assert.Equal(0u, participant.Version);
        Assert.Empty(await AuditAsync(participantId));
    }

    // ---- The other way round: the engine after an admin write --------------------

    // After an admin write, the engine's party routes answer as they always did.
    [Fact]
    public async Task E12_AfterAnAdminCancel_ThePartysAcceptGetsTheEnginesOwn409()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("offers", deal.OfferId, session);
        using (var cancel = await PatchAsync("offers", deal.OfferId, new { status = "Cancelled" }, session, read.ETag))
            await OkAsync(cancel);

        using var accept = await deal.Recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);

        Assert.Equal(HttpStatusCode.Conflict, accept.StatusCode);
        Assert.Contains("Offer is Cancelled, cannot accept", await accept.Content.ReadAsStringAsync());
        Assert.Equal(OfferStatus.Cancelled, (await OfferAsync(deal.OfferId)).Status);
        Assert.Equal(0, await ReadAsync(db => db.AcceptGrants.CountAsync(g => g.OfferId == deal.OfferId)));

        // And a negotiation the admin did not touch still takes a party's offer and accept.
        var next = await PartyDealAsync();
        using var accepted = await next.Recipient.PostAsync($"/offers/{next.OfferId}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    // ---- The write and its audit rows: together or not at all -------------------

    // TD-ADM-178 step 2 (the fault hook inside the close/expire transaction), Spec §10 fail closed.
    [Theory]
    [InlineData("Closed")]
    [InlineData("Expired")]
    public async Task E7_E8_TdAdm178_FaultInsideTheTransaction_LeavesNegotiationOffersAndAuditUnchanged(string status)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("negotiations", seed.Open.Id, session);
        var auditBefore = await AuditCountAsync();
        await using var host = WithUnitOfWork(afterRowsClaimed: () => throw new InvalidOperationException("fault after the offer update"));

        using var response = await PatchAsync("negotiations", seed.Open.Id, new { status }, session, read.ETag, host);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await AssertNothingOfTheCloseAsync(seed, auditBefore);

        // The same token still opens the row: nothing moved.
        using var retry = await PatchAsync("negotiations", seed.Open.Id, new { status }, session, read.ETag);
        await OkAsync(retry);
    }

    // A PostgreSQL deadlock or serialization failure in the write's transaction is a collision
    // like any other: 409, nothing written, and the admin can send the same request again.
    [Theory]
    [InlineData(PostgresErrorCodes.DeadlockDetected)]
    [InlineData(PostgresErrorCodes.SerializationFailure)]
    public async Task E7_DeadlockOrSerializationFailureInTheTransaction_409_NothingWritten(string sqlState)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("negotiations", seed.Open.Id, session);
        var auditBefore = await AuditCountAsync();
        await using var host = WithUnitOfWork(afterRowsClaimed: () => throw new DbUpdateException(
            "save failed", new PostgresException("the transaction was stopped", "ERROR", "ERROR", sqlState)));

        using var response = await PatchAsync("negotiations", seed.Open.Id, new { status = "Closed" }, session, read.ETag, host);

        await AssertConflictAsync(response);
        await AssertNothingOfTheCloseAsync(seed, auditBefore);
        using var retry = await PatchAsync("negotiations", seed.Open.Id, new { status = "Closed" }, session, read.ETag);
        await OkAsync(retry);
    }

    // Any other database fault is not a collision: it stays a 500.
    [Fact]
    public void TransactionCollision_IsOnlyDeadlockOrSerializationFailure()
    {
        static PostgresException Postgres(string sqlState) => new("fault", "ERROR", "ERROR", sqlState);

        Assert.True(DatabaseErrors.IsTransactionCollision(Postgres(PostgresErrorCodes.DeadlockDetected)));
        Assert.True(DatabaseErrors.IsTransactionCollision(Postgres(PostgresErrorCodes.SerializationFailure)));
        Assert.True(DatabaseErrors.IsTransactionCollision(
            new InvalidOperationException("outer", new DbUpdateException("save failed", Postgres(PostgresErrorCodes.DeadlockDetected)))));
        Assert.False(DatabaseErrors.IsTransactionCollision(Postgres(PostgresErrorCodes.UniqueViolation)));
        Assert.False(DatabaseErrors.IsTransactionCollision(Postgres(PostgresErrorCodes.LockNotAvailable)));
        Assert.False(DatabaseErrors.IsTransactionCollision(new DbUpdateException("deadlock detected")));
        Assert.False(DatabaseErrors.IsTransactionCollision(new TimeoutException()));
    }

    // TD-ADM-080 / TD-ADM-102: when the audit row cannot be written, the edit is not saved.
    [Theory]
    [MemberData(nameof(EveryType))]
    public async Task TdAdm080_AuditInsertFails_TheEditIsNotSaved_OnEachRoute(string type)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var id = seed.EditableId(type);
        var read = await GetAsync(type, id, session);
        var auditBefore = await AuditCountAsync();
        await using var host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAdminAuditRepository>();
            services.AddScoped<IAdminAuditRepository, FailingAuditRepository>();
        }));
        host.Services.GetRequiredService<FakeAdminClock>().UtcNow = Clock().UtcNow;

        using var response = await SendAsync(
            HttpMethod.Patch, $"/admin/api/{type}/{id}", seed.ValidBody(type), session, read.ETag, factory: host);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var after = await GetAsync(type, id, session);
        Assert.Equal(read.ETag, after.ETag);
        Assert.Equal(read.Body.GetRawText(), after.Body.GetRawText());
        Assert.Equal(auditBefore, await AuditCountAsync());
    }

    // A refused write of any kind leaves no audit row behind.
    [Fact]
    public async Task RefusedWrites_WriteNoAuditRow()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("offers", seed.Accepted.Id, session);
        var auditBefore = await AuditCountAsync();

        using var notAllowed = await PatchAsync("offers", seed.Accepted.Id, new { status = "Cancelled" }, session, read.ETag);
        using var notEditable = await PatchAsync("offers", seed.Accepted.Id, new { amount = 1 }, session, read.ETag);
        using var stale = await PatchAsync("offers", seed.OpenOffers[0].Id, new { status = "Cancelled" }, session, read.ETag);
        using var noToken = await PatchAsync("offers", seed.OpenOffers[0].Id, new { status = "Cancelled" }, session, ifMatch: null);

        Assert.Equal(HttpStatusCode.BadRequest, notAllowed.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, notEditable.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal((HttpStatusCode)428, noToken.StatusCode);
        Assert.Equal(auditBefore, await AuditCountAsync());
    }

    // ---- The cascade asks the field policy ------------------------------------------

    // TD-ADM-005: closing or expiring writes each open offer's status, so the policy is asked
    // about that write as well as the negotiation's. Refused there, nothing is written.
    [Theory]
    [InlineData("Closed")]
    [InlineData("Expired")]
    public async Task E7_E8_TdAdm005_PolicyRefusesTheOfferStatusWrite_CloseRefused_NothingWritten(string status)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var read = await GetAsync("negotiations", seed.Open.Id, session);
        var auditBefore = await AuditCountAsync();
        await using var host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFieldPolicy>();
            services.AddSingleton<IFieldPolicy>(new NoOfferStatusWritePolicy());
        }));
        host.Services.GetRequiredService<FakeAdminClock>().UtcNow = Clock().UtcNow;

        using var response = await PatchAsync("negotiations", seed.Open.Id, new { status }, session, read.ETag, host);

        await AssertGenericDenyAsync(response);
        await AssertNothingOfTheCloseAsync(seed, auditBefore);

        // The end time has no cascade, so that policy still lets it through.
        using var endsAt = await PatchAsync(
            "negotiations", seed.Open.Id, new { endsAt = "2031-01-01T00:00:00Z" }, session, read.ETag, host);
        await OkAsync(endsAt);
    }

    // ---- Harness: the parties, through the engine's own routes ------------------

    /// <summary>Two registered participants, one open negotiation between them, one open offer from maker to recipient.</summary>
    private sealed record PartyDeal(
        HttpClient Maker,
        string MakerSub,
        string MakerApiKey,
        HttpClient Recipient,
        string RecipientSub,
        string RecipientApiKey,
        Guid NegotiationId,
        Guid OfferId);

    private async Task<PartyDeal> PartyDealAsync()
    {
        var (maker, makerSub, makerKey) = await PartyAsync("maker");
        var (recipient, recipientSub, recipientKey) = await PartyAsync("recipient");

        using var artifactResponse = await maker.PostAsJsonAsync("/artifacts", new CreateArtifactRequest
        {
            Entities = [new CreateSubjectEntityDto { Name = "Party item", Description = "A negotiable item" }],
            Intent = "sell"
        });
        Assert.Equal(HttpStatusCode.Created, artifactResponse.StatusCode);
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        using var negotiationResponse = await maker.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = recipientSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        Assert.Equal(HttpStatusCode.Created, negotiationResponse.StatusCode);
        var negotiation = await negotiationResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        using var offerResponse = await maker.PostAsJsonAsync(
            $"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD", Terms = TermsAsSent });
        Assert.Equal(HttpStatusCode.Created, offerResponse.StatusCode);
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        return new PartyDeal(maker, makerSub, makerKey, recipient, recipientSub, recipientKey, negotiation.Id, offer!.Id);
    }

    private async Task<(HttpClient Client, string Sub, string ApiKey)> PartyAsync(string role)
    {
        var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest { DisplayName = $"{role}-{Guid.NewGuid():N}" });
        Assert.True(response.IsSuccessStatusCode, $"register: {(int)response.StatusCode}");
        var registered = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {registered!.ApiKey}");
        return (client, registered.Sub, registered.ApiKey);
    }

    /// <summary>The same participant, through another host of the same store.</summary>
    private static HttpClient PartyClient(WebApplicationFactory<Program> host, string apiKey)
    {
        var client = host.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {apiKey}");
        return client;
    }

    private static Guid ParticipantIdOf(string sub) => Guid.Parse(sub["participant:".Length..]);

    /// <summary>The party's accept is whole: the offer Accepted with its grants, no admin mark on it, no audit row.</summary>
    private async Task AssertAcceptStandsAsync(PartyDeal deal)
    {
        var offer = await OfferAsync(deal.OfferId);
        Assert.Equal(OfferStatus.Accepted, offer.Status);
        Assert.Equal(0u, offer.Version);
        Assert.Null(offer.UpdatedAt);
        Assert.Equal(1000m, offer.Amount);
        Assert.Equal("USD", offer.Currency);
        Assert.Equal(TermsAsSent, offer.Terms);
        Assert.Equal(2, await ReadAsync(db => db.AcceptGrants.CountAsync(g => g.OfferId == deal.OfferId)));
        Assert.Empty(await AuditAsync(deal.OfferId));
    }

    /// <summary>The admin's write is whole: the offer Cancelled once, as sent, with its audit row; nothing of an accept.</summary>
    private async Task AssertCancelStandsAsync(PartyDeal deal)
    {
        var offer = await OfferAsync(deal.OfferId);
        Assert.Equal(OfferStatus.Cancelled, offer.Status);
        Assert.Equal(1u, offer.Version);
        Assert.Equal(1000m, offer.Amount);
        Assert.Equal("USD", offer.Currency);
        Assert.Equal(TermsAsSent, offer.Terms);
        Assert.Equal(0, await ReadAsync(db => db.AcceptGrants.CountAsync(g => g.OfferId == deal.OfferId)));
        Assert.Single(await AuditAsync(deal.OfferId));
    }

    /// <summary>The engine's answer to a save that found its row changed: 409, its own error shape, one fixed sentence.</summary>
    private static async Task AssertEngineWriteConflictAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Conflict, $"{(int)response.StatusCode}: {text}");
        var body = JsonDocument.Parse(text).RootElement;
        Assert.Equal(EngineWriteConflictMiddleware.Message, body.GetProperty("error").GetString());
        Assert.Single(body.EnumerateObject());
    }

    private async Task AssertNothingOfTheCloseAsync(Seed seed, int auditBefore)
    {
        var negotiation = await NegotiationAsync(seed.Open.Id);
        Assert.Equal(NegotiationStatus.Open, negotiation.Status);
        Assert.Equal(0u, negotiation.Version);
        foreach (var open in seed.OpenOffers)
        {
            var offer = await OfferAsync(open.Id);
            Assert.Equal(OfferStatus.Open, offer.Status);
            Assert.Equal(0u, offer.Version);
            Assert.Null(offer.UpdatedAt);
        }

        Assert.Equal(auditBefore, await AuditCountAsync());
    }

    // ---- Harness: two live transactions ------------------------------------------

    /// <summary>
    /// The party's accept goes first and is held just before its commit: the
    /// offer is written, its row is locked, the transaction is live. The
    /// admin's write is then sent and seen to wait on that row. Only then
    /// does the accept commit.
    /// </summary>
    private async Task<(HttpResponseMessage Accept, HttpResponseMessage AdminWrite)> PartyAcceptFirstAsync(
        PartyDeal deal,
        Func<Task<HttpResponseMessage>> adminWrite)
    {
        Task<HttpResponseMessage>? writing = null;
        await using var host = WithDbInterceptor(new BeforeCommit<Offer>(async () =>
        {
            writing = adminWrite();
            await WaitUntilARequestWaitsOnARowLockAsync();
        }));
        using var recipient = PartyClient(host, deal.RecipientApiKey);

        var accept = await recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);

        Assert.NotNull(writing);
        return (accept, await writing);
    }

    /// <summary>
    /// The admin's write goes first and is held inside its transaction, after
    /// its rows are claimed and before anything is committed. The party's
    /// accept is then sent and seen to wait on the offer's row. Only then
    /// does the admin's write commit.
    /// </summary>
    private async Task<(HttpResponseMessage AdminWrite, HttpResponseMessage Accept)> AdminWriteFirstAsync(
        PartyDeal deal,
        Func<WebApplicationFactory<Program>, Task<HttpResponseMessage>> adminWrite)
    {
        Task<HttpResponseMessage>? accepting = null;
        await using var host = WithUnitOfWork(afterRowsClaimed: async () =>
        {
            accepting = deal.Recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);
            await WaitUntilARequestWaitsOnARowLockAsync();
        });

        var write = await adminWrite(host);

        Assert.NotNull(accepting);
        return (write, await accepting);
    }

    /// <summary>
    /// Returns once PostgreSQL shows a connection of this database waiting on
    /// a lock: the second request has reached the row the first one holds.
    /// </summary>
    private async Task WaitUntilARequestWaitsOnARowLockAsync()
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var waiting = await ReadAsync(db => db.Database
                .SqlQueryRaw<int>(
                    "SELECT count(*)::int AS \"Value\" FROM pg_stat_activity "
                    + "WHERE datname = current_database() AND wait_event_type = 'Lock'")
                .SingleAsync());
            if (waiting > 0)
                return;
            await Task.Delay(50);
        }

        Assert.Fail("No request came to wait on the row the other transaction holds.");
    }

    // ---- Harness: hooks ---------------------------------------------------------------

    /// <summary>
    /// The same host, with the write's unit of work wrapped.
    /// <paramref name="beforeTransaction"/> runs once the write request has
    /// read its rows and matched the token, just before its transaction
    /// starts: where a party's action can still slip in.
    /// <paramref name="afterRowsClaimed"/> runs inside the transaction, after
    /// the rows are claimed and before anything is saved: a fault there must
    /// leave nothing behind, and a party's request sent there must wait.
    /// </summary>
    private WebApplicationFactory<Program> WithUnitOfWork(
        Func<Task>? beforeTransaction = null,
        Func<Task>? afterRowsClaimed = null)
    {
        var host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAdminAuditUnitOfWork>();
            services.AddScoped<IAdminAuditUnitOfWork>(sp => new HookedUnitOfWork(
                new AdminAuditUnitOfWork(
                    sp.GetRequiredService<DealowareDbContext>(),
                    sp.GetRequiredService<IAdminAuditRepository>()),
                beforeTransaction,
                afterRowsClaimed));
        }));
        host.Services.GetRequiredService<FakeAdminClock>().UtcNow = Clock().UtcNow;
        return host;
    }

    /// <summary>The same host and store, with one more interceptor on its database context.</summary>
    private WebApplicationFactory<Program> WithDbInterceptor(IInterceptor interceptor)
    {
        var host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.ConfigureDbContext<DealowareDbContext>(options => options.AddInterceptors(interceptor))));
        host.Services.GetRequiredService<FakeAdminClock>().UtcNow = Clock().UtcNow;
        return host;
    }

    /// <summary>
    /// Runs once, just before the first save that changes a row of
    /// <typeparamref name="T"/>: the request has read its rows and changed
    /// them in memory, and has written nothing yet.
    /// </summary>
    private sealed class BeforeSave<T>(Func<Task> act) : SaveChangesInterceptor
        where T : class
    {
        private Func<Task>? _act = act;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (_act is { } run
                && eventData.Context is { } db
                && db.ChangeTracker.Entries<T>().Any(e => e.State == EntityState.Modified))
            {
                _act = null;
                await run();
            }

            return result;
        }
    }

    /// <summary>
    /// Runs once, just before the commit of the first transaction that
    /// changed a row of <typeparamref name="T"/>: the rows are written and
    /// held, and the transaction is still live.
    /// </summary>
    private sealed class BeforeCommit<T>(Func<Task> act) : DbTransactionInterceptor
        where T : class
    {
        private Func<Task>? _act = act;

        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction,
            TransactionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            if (_act is { } run
                && eventData.Context is { } db
                && db.ChangeTracker.Entries<T>().Any(e => e.State == EntityState.Modified))
            {
                _act = null;
                await run();
            }

            return result;
        }
    }

    /// <summary>The real policy, except that nobody may write an offer's status.</summary>
    private sealed class NoOfferStatusWritePolicy : IFieldPolicy
    {
        private readonly FieldPolicy _inner = new();

        public bool IsRegistered(FieldClass fieldClass) => _inner.IsRegistered(fieldClass);

        public bool Evaluate(
            FieldPrincipal principal,
            FieldClass fieldClass,
            FieldAction action,
            FieldResourceContext resourceContext)
            => !(fieldClass == FieldClass.OfferStatus && action == FieldAction.Write)
               && _inner.Evaluate(principal, fieldClass, action, resourceContext);
    }

    /// <summary>An audit log that cannot be written to.</summary>
    private sealed class FailingAuditRepository : IAdminAuditRepository
    {
        public Task AddAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("audit insert failed");

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<(IReadOnlyList<AdminAuditEntry> Items, int Total)> ListPageAsync(
            AdminAuditListCriteria criteria,
            CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<AdminAuditEntry>, int)>(([], 0));
    }

    private sealed class HookedUnitOfWork(
        IAdminAuditUnitOfWork inner,
        Func<Task>? beforeTransaction,
        Func<Task>? afterRowsClaimed) : IAdminAuditUnitOfWork
    {
        public Task ExecutePairedAsync(
            Func<CancellationToken, Task> mutateAsync,
            AdminAuditEntry auditEntry,
            CancellationToken cancellationToken = default)
            => ExecutePairedAsync(mutateAsync, [auditEntry], cancellationToken);

        public async Task ExecutePairedAsync(
            Func<CancellationToken, Task> mutateAsync,
            IReadOnlyList<AdminAuditEntry> auditEntries,
            CancellationToken cancellationToken = default)
        {
            if (beforeTransaction is not null)
                await beforeTransaction();

            await inner.ExecutePairedAsync(
                async ct =>
                {
                    await mutateAsync(ct);
                    if (afterRowsClaimed is not null)
                        await afterRowsClaimed();
                },
                auditEntries,
                cancellationToken);
        }
    }
}
