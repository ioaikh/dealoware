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
using Dealoware.Domain.Negotiations;
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
/// A delete never marks a row a party changed after the intent, a confirm
/// token deletes once, and a delete and its audit rows land together or not
/// at all.
/// <para>
/// The party acts through the engine's own routes (place, counter, accept,
/// profile), which are as they were. The tests put the party's action at
/// each place it can fall: between the intent and the DELETE; inside the
/// DELETE request, between the server's read of the rows and its
/// transaction; and at the same time, with both transactions live (one holds
/// the row, the other is seen to wait on it, then the first commits). The
/// last need row locks, so they run on PostgreSQL only
/// (<see cref="PostgresOnlyFactAttribute"/>).
/// </para>
/// </summary>
public partial class AdminDeleteApiTests
{
    // ---- A party action or an admin write between the intent and the DELETE ------------

    // TD-ADM-092 negative: the party accepts the offer after the owner opened the delete.
    [Fact]
    public async Task TdAdm092_PartyAcceptsAfterTheIntent_Delete409_AcceptStands_TokenStillUnused()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("offers", deal.OfferId, session);

        using (var accept = await deal.Recipient.PostAsync($"/offers/{deal.OfferId}/accept", null))
            Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        using var delete = await DeleteAsync("offers", deal.OfferId, session, intent.Token);

        await AssertChangedAsync(delete);
        await AssertAcceptStandsAsync(deal);
        Assert.Null(await TokenUsedAtAsync(deal.OfferId));

        // Opened again, the owner sees the accepted offer and can delete it.
        var again = await IntentAsync("offers", deal.OfferId, session);
        Assert.Equal("Accepted", again.Body.GetProperty("identity").GetProperty("status").GetString());
        using var retry = await DeleteAsync("offers", deal.OfferId, session, again.Token);
        await OkAsync(retry);
        Assert.Equal(OfferStatus.Accepted, (await OfferAsync(deal.OfferId)).Status);
    }

    // TD-ADM-093 negative: a party places or counters an offer after the owner opened the
    // negotiation's delete. The offer the owner never saw is not deleted, and nor is the rest.
    [Theory]
    [InlineData("place")]
    [InlineData("counter")]
    public async Task TdAdm093_PartyAddsAnOfferAfterTheIntent_Delete409_NothingDeleted(string how)
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("negotiations", deal.NegotiationId, session);
        AssertCascade(intent, participants: 0, artifacts: 0, negotiations: 1, offers: 1, openNegotiations: 1, openOffers: 1);

        var route = how == "place" ? $"/negotiations/{deal.NegotiationId}/offers" : $"/offers/{deal.OfferId}/counter";
        using var added = await deal.Recipient.PostAsJsonAsync(
            route, new CreateOfferRequest { Amount = 900m, Currency = "USD", Terms = "the offer the owner never saw" });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var newOffer = (await added.Content.ReadFromJsonAsync<OfferResponse>())!.Id;

        using var delete = await DeleteAsync("negotiations", deal.NegotiationId, session, intent.Token);

        await AssertChangedAsync(delete);
        await AssertLiveAsync("negotiations", deal.NegotiationId);
        await AssertLiveAsync("offers", newOffer);
        await AssertLiveAsync("offers", deal.OfferId);
    }

    // TD-ADM-095 negative: the participant renames themselves after the intent.
    [Fact]
    public async Task TdAdm095_PartyRenamesThemselvesAfterTheIntent_Delete409_TheirNameStands()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var participantId = ParticipantIdOf(deal.MakerSub);
        var intent = await IntentAsync("participants", participantId, session);

        using (var rename = await deal.Maker.PatchAsJsonAsync("/profile", new UpdateProfileRequest { DisplayName = "My own new name" }))
            Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        using var delete = await DeleteAsync("participants", participantId, session, intent.Token);

        await AssertChangedAsync(delete);
        var participant = await ParticipantAsync(participantId);
        Assert.Null(participant.DeletedAt);
        Assert.Equal("My own new name", participant.DisplayName);
        await AssertLiveAsync("negotiations", deal.NegotiationId);
    }

    // An admin edit after the intent moves the Version: the delete prepared before it is refused.
    [Fact]
    public async Task AdminEditAfterTheIntent_Delete409_TheEditStands()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("negotiations", deal.NegotiationId, session);
        using (var read = await SendAsync(HttpMethod.Get, $"/admin/api/negotiations/{deal.NegotiationId}", session))
        {
            var etag = read.Headers.ETag!.ToString();
            using var edit = await SendAsync(
                HttpMethod.Patch, $"/admin/api/negotiations/{deal.NegotiationId}", session,
                body: """{"endsAt":"2031-01-01T00:00:00Z"}""", ifMatch: etag);
            await OkAsync(edit);
        }

        using var delete = await DeleteAsync("negotiations", deal.NegotiationId, session, intent.Token);

        await AssertChangedAsync(delete);
        var negotiation = await NegotiationAsync(deal.NegotiationId);
        Assert.Null(negotiation.DeletedAt);
        Assert.Equal(1u, negotiation.Version);
    }

    // ---- A party action inside the DELETE request, before its transaction ---------------

    // The server has read the offer (still Open) and matched the token; the party's accept
    // lands before the transaction. The claim finds the row changed: 409, the accept stands.
    [Fact]
    public async Task TdAdm092_PartyAcceptsBetweenTheServersReadAndItsWrite_Delete409_AcceptStands()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("offers", deal.OfferId, session);
        var interleaved = 0;
        await using var host = WithUnitOfWork(beforeTransaction: async () =>
        {
            interleaved++;
            using var accept = await deal.Recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);
            Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        });

        using var delete = await DeleteAsync("offers", deal.OfferId, session, intent.Token, host);

        Assert.Equal(1, interleaved);
        await AssertChangedAsync(delete);
        await AssertAcceptStandsAsync(deal);
        Assert.Null(await TokenUsedAtAsync(deal.OfferId));
    }

    // A party places an offer inside the DELETE of its negotiation: the new offer would be left
    // live under a deleted negotiation, so the delete stops.
    [Fact]
    public async Task TdAdm093_PartyPlacesAnOfferBetweenTheServersReadAndItsWrite_Delete409_NothingLeftBehind()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("negotiations", deal.NegotiationId, session);
        Guid? newOffer = null;
        await using var host = WithUnitOfWork(beforeTransaction: async () =>
        {
            using var placed = await deal.Recipient.PostAsJsonAsync(
                $"/negotiations/{deal.NegotiationId}/offers", new CreateOfferRequest { Amount = 900m, Currency = "USD" });
            Assert.Equal(HttpStatusCode.Created, placed.StatusCode);
            newOffer = (await placed.Content.ReadFromJsonAsync<OfferResponse>())!.Id;
        });

        using var delete = await DeleteAsync("negotiations", deal.NegotiationId, session, intent.Token, host);

        await AssertChangedAsync(delete);
        await AssertLiveAsync("negotiations", deal.NegotiationId);
        await AssertLiveAsync("offers", deal.OfferId);
        await AssertLiveAsync("offers", newOffer!.Value);
    }

    // A party starts a new negotiation inside the DELETE of one of its participants.
    [Fact]
    public async Task TdAdm095_NewNegotiationBetweenTheServersReadAndItsWrite_Delete409_NothingLeftBehind()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var recipientId = ParticipantIdOf(deal.RecipientSub);
        var intent = await IntentAsync("participants", recipientId, session);
        Guid? started = null;
        await using var host = WithUnitOfWork(beforeTransaction: async () =>
            started = await StartNegotiationAsync(deal.Maker, deal.RecipientSub));

        using var delete = await DeleteAsync("participants", recipientId, session, intent.Token, host);

        await AssertChangedAsync(delete);
        Assert.Null((await ParticipantAsync(recipientId)).DeletedAt);
        await AssertLiveAsync("negotiations", deal.NegotiationId);
        await AssertLiveAsync("negotiations", started!.Value);
    }

    // ---- The other way round: the engine after a delete ---------------------------------

    // An engine request that had read its row before a delete marked it: 409 in the engine's
    // own error shape, not 500, and nothing of the request is saved.
    [Fact]
    public async Task EngineAccept_WhoseOfferADeleteJustMarked_409NotA500_NothingOfItSaved()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("offers", deal.OfferId, session);
        var landed = 0;
        await using var host = WithDbInterceptor(new BeforeSave<Offer>(async () =>
        {
            landed++;
            // The accept has read the offer as Open, changed it in memory, and is about to save.
            using var delete = await DeleteAsync("offers", deal.OfferId, session, intent.Token);
            await OkAsync(delete);
        }));
        using var recipient = PartyClient(host, deal.RecipientApiKey);

        using var accept = await recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);

        Assert.Equal(1, landed);
        await AssertEngineWriteConflictAsync(accept);
        await AssertDeleteStandsAsync(deal);
    }

    // T05 review carry-over: an engine save that PostgreSQL stopped as a deadlock or a
    // serialization failure is the engine's 409 too, and nothing of it is saved. Any other
    // database fault stays a 500.
    [Theory]
    [InlineData(PostgresErrorCodes.DeadlockDetected, 409)]
    [InlineData(PostgresErrorCodes.SerializationFailure, 409)]
    [InlineData(PostgresErrorCodes.UniqueViolation, 500)]
    public async Task EngineSave_StoppedAsATransactionCollision_409_OtherFaults500(string sqlState, int expected)
    {
        var deal = await PartyDealAsync();
        await using var host = WithDbInterceptor(new BeforeSave<Offer>(() => throw new DbUpdateException(
            "save failed", new PostgresException("the transaction was stopped", "ERROR", "ERROR", sqlState))));
        using var recipient = PartyClient(host, deal.RecipientApiKey);

        using var accept = await recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);

        Assert.Equal(expected, (int)accept.StatusCode);
        if (expected == 409)
            await AssertEngineWriteConflictAsync(accept);
        var offer = await OfferAsync(deal.OfferId);
        Assert.Equal(OfferStatus.Open, offer.Status);
        Assert.Equal(0, await ReadAsync(db => db.AcceptGrants.CountAsync(g => g.OfferId == deal.OfferId)));

        // The same request, made again, goes through.
        using var again = await deal.Recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    // ---- The delete and its audit rows: together or not at all ----------------------------

    // Spec §10 fail closed: a fault inside the transaction, after the rows are claimed, leaves
    // every row, the audit log and the token as they were; the same token then works.
    [Fact]
    public async Task FaultInsideTheTransaction_NothingDeleted_NoAudit_TokenUnused()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("negotiations", seed.Open.Id, session);
        var auditBefore = await AuditCountAsync();
        await using var host = WithUnitOfWork(afterRowsClaimed: () => throw new InvalidOperationException("fault after the claims"));

        using var response = await DeleteAsync("negotiations", seed.Open.Id, session, intent.Token, host);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await AssertNothingOfTheNegotiationDeleteAsync(seed, auditBefore);
        Assert.Null(await TokenUsedAtAsync(seed.Open.Id));

        using var retry = await DeleteAsync("negotiations", seed.Open.Id, session, intent.Token);
        await OkAsync(retry);
    }

    // A PostgreSQL deadlock or serialization failure in the delete's transaction: 409, nothing
    // written, and the owner can confirm again.
    [Theory]
    [InlineData(PostgresErrorCodes.DeadlockDetected)]
    [InlineData(PostgresErrorCodes.SerializationFailure)]
    public async Task DeadlockOrSerializationFailureInTheTransaction_409_NothingWritten(string sqlState)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("negotiations", seed.Open.Id, session);
        var auditBefore = await AuditCountAsync();
        await using var host = WithUnitOfWork(afterRowsClaimed: () => throw new DbUpdateException(
            "save failed", new PostgresException("the transaction was stopped", "ERROR", "ERROR", sqlState)));

        using var response = await DeleteAsync("negotiations", seed.Open.Id, session, intent.Token, host);

        await AssertChangedAsync(response);
        await AssertNothingOfTheNegotiationDeleteAsync(seed, auditBefore);
        using var retry = await DeleteAsync("negotiations", seed.Open.Id, session, intent.Token);
        await OkAsync(retry);
    }

    // TD-ADM-080 / TD-ADM-102: when an audit row cannot be written, nothing is deleted.
    [Theory]
    [MemberData(nameof(EveryType))]
    public async Task AuditInsertFails_NothingDeleted_OnEachRoute(string type)
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var id = seed.DeletableId(type);
        var intent = await IntentAsync(type, id, session);
        var auditBefore = await AuditCountAsync();
        await using var host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAdminAuditRepository>();
            services.AddScoped<IAdminAuditRepository, FailingAuditRepository>();
        }));
        host.Services.GetRequiredService<FakeAdminClock>().UtcNow = Clock().UtcNow;

        using var response = await DeleteAsync(type, id, session, intent.Token, host);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await AssertLiveAsync(type, id);
        Assert.Equal(auditBefore, await AuditCountAsync());
        Assert.Null(await TokenUsedAtAsync(id));
    }

    // Every refusal leaves no audit row behind.
    [Fact]
    public async Task RefusedDeletes_WriteNoAuditRow()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var auditBefore = await AuditCountAsync();

        using var blind = await DeleteAsync("offers", seed.OpenOffers[0].Id, session, token: null);
        using var unknown = await DeleteAsync("offers", seed.OpenOffers[0].Id, session, new string('f', 64));
        using var blocked = await SendAsync(HttpMethod.Post, $"/admin/api/artifacts/{seed.Artifact.Id}/delete-intent", session);
        var intent = await IntentAsync("offers", seed.OpenOffers[1].Id, session);
        using var otherRecord = await DeleteAsync("offers", seed.OpenOffers[0].Id, session, intent.Token);

        Assert.Equal((HttpStatusCode)428, blind.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, otherRecord.StatusCode);
        Assert.Equal(auditBefore, await AuditCountAsync());
    }

    // ---- Both at once, two live transactions (PostgreSQL row locks) -----------------------

    // TD-ADM-090 (double submit): two confirms of one token, both inside their transactions at
    // once. The first holds the token's row; the second waits on it, then finds it used. One
    // delete, one set of audit rows.
    [PostgresOnlyFact]
    public async Task TdAdm090_PostgresLive_TwoConfirmsOfOneTokenAtOnce_DeleteOnce()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("negotiations", seed.Open.Id, session);
        Task<HttpResponseMessage>? second = null;
        await using var host = WithUnitOfWork(afterRowsClaimed: async () =>
        {
            second = DeleteAsync("negotiations", seed.Open.Id, session, intent.Token);
            await WaitUntilARequestWaitsOnARowLockAsync();
        });

        using var first = await DeleteAsync("negotiations", seed.Open.Id, session, intent.Token, host);
        Assert.NotNull(second);
        using var secondResponse = await second;

        var body = await OkAsync(first);
        await AssertTokenInvalidAsync(secondResponse);
        var correlationId = body.GetProperty("correlationId").GetGuid();
        var deleteAudits = await ReadAsync(db => db.AdminAuditLog
            .CountAsync(a => a.Action == AdminAuditActions.EntityDelete && a.EntityId == seed.Open.Id));
        Assert.Equal(1, deleteAudits);
        Assert.Equal(
            1 + seed.OpenOffers.Count + 1,
            await ReadAsync(db => db.AdminAuditLog.CountAsync(a => a.CorrelationId == correlationId)));
        Assert.Equal(1u, (await NegotiationAsync(seed.Open.Id)).Version);
    }

    // TD-ADM-092: the party's accept has written the offer and not yet committed. The delete
    // waits on that row, and when the accept commits, finds it changed: 409, the accept stands.
    [PostgresOnlyFact]
    public async Task TdAdm092_PostgresLive_PartyAcceptHoldsTheRow_DeleteWaitsThen409_AcceptStands()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("offers", deal.OfferId, session);

        Task<HttpResponseMessage>? deleting = null;
        await using (var host = WithDbInterceptor(new BeforeCommit<Offer>(async () =>
                     {
                         deleting = DeleteAsync("offers", deal.OfferId, session, intent.Token);
                         await WaitUntilARequestWaitsOnARowLockAsync();
                     })))
        {
            using var recipient = PartyClient(host, deal.RecipientApiKey);
            using var accept = await recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);
            Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        }

        Assert.NotNull(deleting);
        using var delete = await deleting;
        await AssertChangedAsync(delete);
        await AssertAcceptStandsAsync(deal);
        Assert.Null(await TokenUsedAtAsync(deal.OfferId));
    }

    // TD-ADM-092: the delete holds the offer and has not yet committed. The party's accept waits
    // on that row, and when the delete commits, finds it changed: the engine's 409.
    [PostgresOnlyFact]
    public async Task TdAdm092_PostgresLive_DeleteHoldsTheRow_PartyAcceptWaitsThen409_DeleteStands()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("offers", deal.OfferId, session);

        Task<HttpResponseMessage>? accepting = null;
        await using var host = WithUnitOfWork(afterRowsClaimed: async () =>
        {
            accepting = deal.Recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);
            await WaitUntilARequestWaitsOnARowLockAsync();
        });

        using var delete = await DeleteAsync("offers", deal.OfferId, session, intent.Token, host);
        Assert.NotNull(accepting);
        using var accept = await accepting;

        await OkAsync(delete);
        await AssertEngineWriteConflictAsync(accept);
        await AssertDeleteStandsAsync(deal);
    }

    // TD-ADM-093: the party's accept holds an offer of the negotiation. The delete has claimed
    // the negotiation and waits on the offer; when the accept commits, the delete finds it
    // changed: 409, and the negotiation is not deleted either.
    [PostgresOnlyFact]
    public async Task TdAdm093_PostgresLive_PartyAcceptHoldsAnOffer_NegotiationDeleteWaitsThen409_NothingDeleted()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var intent = await IntentAsync("negotiations", deal.NegotiationId, session);

        Task<HttpResponseMessage>? deleting = null;
        await using (var host = WithDbInterceptor(new BeforeCommit<Offer>(async () =>
                     {
                         deleting = DeleteAsync("negotiations", deal.NegotiationId, session, intent.Token);
                         await WaitUntilARequestWaitsOnARowLockAsync();
                     })))
        {
            using var recipient = PartyClient(host, deal.RecipientApiKey);
            using var accept = await recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);
            Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        }

        Assert.NotNull(deleting);
        using var delete = await deleting;
        await AssertChangedAsync(delete);
        await AssertAcceptStandsAsync(deal);
        var negotiation = await NegotiationAsync(deal.NegotiationId);
        Assert.Null(negotiation.DeletedAt);
        Assert.Equal(0u, negotiation.Version);
        Assert.Empty(await AuditAsync(deal.NegotiationId));
    }

    // ---- Harness: the parties, through the engine's own routes -----------------------------

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
        var negotiationId = await StartNegotiationAsync(maker, recipientSub);

        using var offerResponse = await maker.PostAsJsonAsync(
            $"/negotiations/{negotiationId}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD", Terms = TermsAsSent });
        Assert.Equal(HttpStatusCode.Created, offerResponse.StatusCode);
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        return new PartyDeal(maker, makerSub, makerKey, recipient, recipientSub, recipientKey, negotiationId, offer!.Id);
    }

    /// <summary>A new artifact of the maker's and a negotiation about it with the counterparty.</summary>
    private static async Task<Guid> StartNegotiationAsync(HttpClient maker, string counterpartySub)
    {
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
            CounterpartyParticipantId = counterpartySub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        Assert.Equal(HttpStatusCode.Created, negotiationResponse.StatusCode);
        return (await negotiationResponse.Content.ReadFromJsonAsync<NegotiationResponse>())!.Id;
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

    /// <summary>When the token made for this record was used; null while unused.</summary>
    private Task<DateTimeOffset?> TokenUsedAtAsync(Guid entityId)
        => ReadAsync(db => db.AdminDeleteConfirmTokens.AsNoTracking()
            .Where(t => t.EntityId == entityId)
            .Select(t => t.ConsumedAt)
            .SingleAsync());

    /// <summary>The party's accept is whole: the offer Accepted with its grants, not deleted, no admin mark, no audit row.</summary>
    private async Task AssertAcceptStandsAsync(PartyDeal deal)
    {
        var offer = await OfferAsync(deal.OfferId);
        Assert.Equal(OfferStatus.Accepted, offer.Status);
        Assert.Null(offer.DeletedAt);
        Assert.Equal(0u, offer.Version);
        Assert.Null(offer.UpdatedAt);
        Assert.Equal(1000m, offer.Amount);
        Assert.Equal("USD", offer.Currency);
        Assert.Equal(TermsAsSent, offer.Terms);
        Assert.Equal(2, await ReadAsync(db => db.AcceptGrants.CountAsync(g => g.OfferId == deal.OfferId)));
        Assert.Empty(await AuditAsync(deal.OfferId));
    }

    /// <summary>The delete is whole: the offer marked once, still Open and as sent, with its audit row; nothing of an accept.</summary>
    private async Task AssertDeleteStandsAsync(PartyDeal deal)
    {
        var offer = await OfferAsync(deal.OfferId);
        Assert.NotNull(offer.DeletedAt);
        Assert.Equal(OfferStatus.Open, offer.Status);
        Assert.Equal(1u, offer.Version);
        Assert.Equal(1000m, offer.Amount);
        Assert.Equal("USD", offer.Currency);
        Assert.Equal(TermsAsSent, offer.Terms);
        Assert.Equal(0, await ReadAsync(db => db.AcceptGrants.CountAsync(g => g.OfferId == deal.OfferId)));
        var audit = Assert.Single(await AuditAsync(deal.OfferId));
        Assert.Equal(AdminAuditActions.EntityDelete, audit.Action);
    }

    /// <summary>The engine's answer to a save that lost a race: 409, its own error shape, one fixed sentence.</summary>
    private static async Task AssertEngineWriteConflictAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Conflict, $"{(int)response.StatusCode}: {text}");
        var body = JsonDocument.Parse(text).RootElement;
        Assert.Equal(EngineWriteConflictMiddleware.Message, body.GetProperty("error").GetString());
        Assert.Single(body.EnumerateObject());
    }

    private async Task AssertNothingOfTheNegotiationDeleteAsync(Seed seed, int auditBefore)
    {
        await AssertLiveAsync("negotiations", seed.Open.Id);
        foreach (var offer in seed.OpenOffers.Append(seed.Accepted))
        {
            var stored = await OfferAsync(offer.Id);
            Assert.Null(stored.DeletedAt);
            Assert.Equal(0u, stored.Version);
            Assert.Null(stored.UpdatedAt);
        }

        Assert.Equal(auditBefore, await AuditCountAsync());
    }

    // ---- Harness: two live transactions ------------------------------------------------------

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

    // ---- Harness: hooks -------------------------------------------------------------------------

    /// <summary>
    /// The same host, with the delete's unit of work wrapped.
    /// <paramref name="beforeTransaction"/> runs once the request has read its
    /// rows and matched the token, just before its transaction starts.
    /// <paramref name="afterRowsClaimed"/> runs inside the transaction, after
    /// the token is used and the rows are claimed and marked, before anything
    /// is saved: a fault there must leave nothing behind, and a request sent
    /// there must wait.
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

    /// <summary>Runs once, just before the first save that changes a row of <typeparamref name="T"/>.</summary>
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
