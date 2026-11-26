using System.Net;
using System.Net.Http.Json;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dealoware.Api.Tests;

/// <summary>
/// An admin write never saves over a party's action, and a write and its
/// audit rows land together or not at all.
/// <para>
/// The party acts through the engine's own routes (accept, close, profile),
/// which are as they were: none of them moves <c>Version</c>. The tests put
/// the party's action at the two places it can fall: between the admin's read
/// of the record and the write request, and, inside the write request,
/// between the server's read of the row and its transaction. The second
/// place is reached by wrapping the unit of work the write uses, so the
/// party's request runs at exactly that point.
/// </para>
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
        var participantId = Guid.Parse(deal.MakerSub["participant:".Length..]);
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

    // The other way round: after an admin write, the engine's party routes answer as they always did.
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

    // ---- The two at the very same moment (PostgreSQL row locks) ------------------

    // E12: the admin's cancel and the party's accept overlap. One of them is the outcome, whole.
    [PostgresOnlyFact]
    public async Task E12_PostgresParallel_AdminCancelAndPartyAcceptAtOnce_OneOutcome_NeverAMix()
    {
        var session = await NewSessionAsync();
        for (var round = 0; round < 8; round++)
        {
            var deal = await PartyDealAsync();
            var read = await GetAsync("offers", deal.OfferId, session);

            var cancelling = PatchAsync("offers", deal.OfferId, new { status = "Cancelled" }, session, read.ETag);
            var accepting = deal.Recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);
            using var cancel = await cancelling;
            using var accept = await accepting;

            var offer = await OfferAsync(deal.OfferId);
            if (accept.StatusCode == HttpStatusCode.OK)
            {
                // The party's accept is the outcome; the admin's cancel did not save over it.
                await AssertConflictAsync(cancel);
                await AssertAcceptStandsAsync(deal);
            }
            else
            {
                // The admin's cancel is the outcome; the accept left nothing behind.
                AssertEnginesOwnRefusal(accept);
                await OkAsync(cancel);
                Assert.Equal(OfferStatus.Cancelled, offer.Status);
                Assert.Equal(1u, offer.Version);
                Assert.Equal(TermsAsSent, offer.Terms);
                Assert.Equal(0, await ReadAsync(db => db.AcceptGrants.CountAsync(g => g.OfferId == deal.OfferId)));
                Assert.Single(await AuditAsync(deal.OfferId));
            }
        }
    }

    // E7: the admin's close and the party's accept of an offer in it overlap.
    [PostgresOnlyFact]
    public async Task E7_PostgresParallel_AdminCloseAndPartyAcceptAtOnce_AnAcceptedOfferIsNeverCancelled()
    {
        var session = await NewSessionAsync();
        for (var round = 0; round < 8; round++)
        {
            var deal = await PartyDealAsync();
            var read = await GetAsync("negotiations", deal.NegotiationId, session);

            var closing = PatchAsync("negotiations", deal.NegotiationId, new { status = "Closed" }, session, read.ETag);
            var accepting = deal.Recipient.PostAsync($"/offers/{deal.OfferId}/accept", null);
            using var close = await closing;
            using var accept = await accepting;

            var offer = await OfferAsync(deal.OfferId);
            var negotiation = await NegotiationAsync(deal.NegotiationId);
            if (accept.StatusCode == HttpStatusCode.OK)
            {
                // Accepted first. The close either saw it accepted and closed
                // around it, or was prepared before it and is refused.
                await AssertAcceptStandsAsync(deal);
                if (close.StatusCode == HttpStatusCode.OK)
                {
                    Assert.Equal(NegotiationStatus.Closed, negotiation.Status);
                    Assert.Single(await AuditAsync(deal.NegotiationId));
                }
                else
                {
                    await AssertConflictAsync(close);
                    Assert.Equal(NegotiationStatus.Open, negotiation.Status);
                    Assert.Equal(0u, negotiation.Version);
                    Assert.Empty(await AuditAsync(deal.NegotiationId));
                }
            }
            else
            {
                AssertEnginesOwnRefusal(accept);
                await OkAsync(close);
                Assert.Equal(NegotiationStatus.Closed, negotiation.Status);
                Assert.Equal(OfferStatus.Cancelled, offer.Status);
                Assert.Equal(1u, offer.Version);
                Assert.Equal(0, await ReadAsync(db => db.AcceptGrants.CountAsync(g => g.OfferId == deal.OfferId)));
                Assert.Single(await AuditAsync(deal.OfferId));
            }
        }
    }

    /// <summary>
    /// How the engine's accept answers when the admin's write landed first:
    /// its own 409 when it read the offer after that write, and a 500 when it
    /// had read the offer before and found the row moved as it saved (the
    /// engine has no handler for that; it is as it was, and nothing is saved).
    /// </summary>
    private static void AssertEnginesOwnRefusal(HttpResponseMessage accept)
        => Assert.Contains(accept.StatusCode, new[] { HttpStatusCode.Conflict, HttpStatusCode.InternalServerError });

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

        // The same token still opens the row: nothing moved.
        using var retry = await PatchAsync("negotiations", seed.Open.Id, new { status }, session, read.ETag);
        await OkAsync(retry);
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

    // ---- Harness: the parties, through the engine's own routes ------------------

    /// <summary>Two registered participants, one open negotiation between them, one open offer from maker to recipient.</summary>
    private sealed record PartyDeal(
        HttpClient Maker,
        string MakerSub,
        HttpClient Recipient,
        string RecipientSub,
        Guid NegotiationId,
        Guid OfferId);

    private async Task<PartyDeal> PartyDealAsync()
    {
        var (maker, makerSub) = await PartyAsync("maker");
        var (recipient, recipientSub) = await PartyAsync("recipient");

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

        return new PartyDeal(maker, makerSub, recipient, recipientSub, negotiation.Id, offer!.Id);
    }

    private async Task<(HttpClient Client, string Sub)> PartyAsync(string role)
    {
        var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest { DisplayName = $"{role}-{Guid.NewGuid():N}" });
        Assert.True(response.IsSuccessStatusCode, $"register: {(int)response.StatusCode}");
        var registered = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {registered!.ApiKey}");
        return (client, registered.Sub);
    }

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

    // ---- Harness: a hook at the edge of the write's transaction -----------------

    /// <summary>
    /// The same host, with the write's unit of work wrapped.
    /// <paramref name="beforeTransaction"/> runs once the write request has
    /// read its rows and matched the token, just before its transaction
    /// starts: where a party's action can still slip in.
    /// <paramref name="afterRowsClaimed"/> runs inside the transaction, after
    /// the rows are claimed and before anything is saved: a fault there must
    /// leave nothing behind.
    /// </summary>
    private WebApplicationFactory<Program> WithUnitOfWork(
        Func<Task>? beforeTransaction = null,
        Action? afterRowsClaimed = null)
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
        Action? afterRowsClaimed) : IAdminAuditUnitOfWork
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
                    afterRowsClaimed?.Invoke();
                },
                auditEntries,
                cancellationToken);
        }
    }
}
