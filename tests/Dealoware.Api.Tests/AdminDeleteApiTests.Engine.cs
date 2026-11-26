using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Negotiations.Dtos;
using Dealoware.Application.Profile.Dtos;
using Dealoware.Domain.Negotiations;

namespace Dealoware.Api.Tests;

/// <summary>
/// The engine respects an admin delete (owner's decision, 26 Nov 2026): a
/// soft-deleted participant cannot sign in or act, and a party cannot read or
/// act on a soft-deleted negotiation, offer or artifact. Each refusal is the
/// engine's own answer for an id that does not exist (or, for sign-in, for an
/// unknown or inactive credential), so it says nothing more. The live rows
/// next to the deleted ones keep working as before.
/// </summary>
public partial class AdminDeleteApiTests
{
    // ---- A deleted participant -------------------------------------------------------

    // Neither its API key nor a JWT issued before the delete signs in; nor can it get a new
    // JWT. Each answer is the one an unknown credential gets.
    [Fact]
    public async Task Engine_DeletedParticipant_CannotSignInEitherWay_SameAnswerAsUnknown()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var jwt = await JwtForAsync(deal.MakerApiKey);
        using (var before = await SendAsPartyAsync(HttpMethod.Get, "/profile", "Bearer", jwt))
            Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        await DeleteThroughIntentAsync("participants", ParticipantIdOf(deal.MakerSub), session);

        var unknownKey = $"dlw_{Guid.NewGuid():N}_{Guid.NewGuid():N}";
        foreach (var (scheme, credential, unknown) in new[]
                 {
                     ("ApiKey", deal.MakerApiKey, unknownKey),
                     ("Bearer", deal.MakerApiKey, unknownKey),
                     ("Bearer", jwt, "not.a.jwt")
                 })
        {
            foreach (var (method, path) in new[] { (HttpMethod.Get, "/profile"), (HttpMethod.Get, "/negotiations"), (HttpMethod.Get, "/offers") })
            {
                using var deleted = await SendAsPartyAsync(method, path, scheme, credential);
                using var never = await SendAsPartyAsync(method, path, scheme, unknown);
                Assert.Equal(HttpStatusCode.Unauthorized, deleted.StatusCode);
                await AssertSameAnswerAsync(never, deleted);
            }
        }

        var client = _factory.CreateClient();
        using var token = await client.PostAsJsonAsync("/auth/token", new TokenRequest { ApiKey = deal.MakerApiKey });
        using var unknownToken = await client.PostAsJsonAsync("/auth/token", new TokenRequest { ApiKey = unknownKey });
        Assert.Equal(HttpStatusCode.Unauthorized, token.StatusCode);
        await AssertSameAnswerAsync(unknownToken, token);

        // The other party still signs in and reads its own profile.
        using var other = await deal.Recipient.GetAsync("/profile");
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
    }

    // A deleted participant cannot be named as a counterparty: the answer an unknown one gets.
    [Fact]
    public async Task Engine_DeletedParticipant_CannotBeACounterparty_SameAnswerAsUnknown()
    {
        var deal = await PartyDealAsync();
        var (third, thirdSub, _) = await PartyAsync("third");
        var session = await NewSessionAsync();
        await DeleteThroughIntentAsync("participants", ParticipantIdOf(thirdSub), session);
        var artifactId = await NewArtifactAsync(deal.Maker);

        using var withDeleted = await deal.Maker.PostAsJsonAsync("/negotiations", NegotiationWith(artifactId, thirdSub));
        using var withUnknown = await deal.Maker.PostAsJsonAsync("/negotiations", NegotiationWith(artifactId, $"participant:{Guid.NewGuid()}"));

        await AssertSameAnswerAsync(withUnknown, withDeleted);
        Assert.NotEqual(HttpStatusCode.Created, withDeleted.StatusCode);
        using var live = await deal.Maker.PostAsJsonAsync("/negotiations", NegotiationWith(artifactId, deal.RecipientSub));
        Assert.Equal(HttpStatusCode.Created, live.StatusCode);
        third.Dispose();
    }

    // ---- A deleted negotiation ---------------------------------------------------------

    // Read, place an offer, close; and its offers: read, accept, decline, counter. Every answer
    // is the one an unknown id gets, and nothing is written. Lists leave them out.
    [Fact]
    public async Task Engine_DeletedNegotiation_NoReadNoAction_SameAnswerAsUnknown_ListsLeaveItOut()
    {
        var deal = await PartyDealAsync();
        var live = await PartyDealAsync();
        var session = await NewSessionAsync();
        await DeleteThroughIntentAsync("negotiations", deal.NegotiationId, session);
        var unknown = Guid.NewGuid();

        foreach (var party in new[] { deal.Maker, deal.Recipient })
        {
            await AssertLikeUnknownAsync(party, HttpMethod.Get, $"/negotiations/{deal.NegotiationId}", $"/negotiations/{unknown}");
            await AssertLikeUnknownAsync(party, HttpMethod.Post, $"/negotiations/{deal.NegotiationId}/offers", $"/negotiations/{unknown}/offers",
                new CreateOfferRequest { Amount = 5m, Currency = "USD" });
            await AssertLikeUnknownAsync(party, HttpMethod.Post, $"/negotiations/{deal.NegotiationId}/close", $"/negotiations/{unknown}/close");
            await AssertLikeUnknownAsync(party, HttpMethod.Get, $"/offers/{deal.OfferId}", $"/offers/{unknown}");
        }

        await AssertLikeUnknownAsync(deal.Recipient, HttpMethod.Post, $"/offers/{deal.OfferId}/accept", $"/offers/{unknown}/accept");
        await AssertLikeUnknownAsync(deal.Recipient, HttpMethod.Post, $"/offers/{deal.OfferId}/decline", $"/offers/{unknown}/decline");
        await AssertLikeUnknownAsync(deal.Recipient, HttpMethod.Post, $"/offers/{deal.OfferId}/counter", $"/offers/{unknown}/counter",
            new CreateOfferRequest { Amount = 7m, Currency = "USD" });

        var negotiation = await NegotiationAsync(deal.NegotiationId);
        Assert.Equal(NegotiationStatus.Open, negotiation.Status);
        Assert.Equal(1u, negotiation.Version);
        var offer = await OfferAsync(deal.OfferId);
        Assert.Equal(OfferStatus.Open, offer.Status);
        Assert.Equal(1u, offer.Version);
        Assert.Single(await ReadAsync(db => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(db.Offers.Where(o => o.NegotiationId == deal.NegotiationId))));
        Assert.Equal(0, await ReadAsync(db => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(db.AcceptGrants, g => g.OfferId == deal.OfferId)));

        using var negotiations = await deal.Maker.GetAsync("/negotiations");
        var negotiationsText = await negotiations.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, negotiations.StatusCode);
        Assert.DoesNotContain(deal.NegotiationId.ToString(), negotiationsText, StringComparison.OrdinalIgnoreCase);
        using var offers = await deal.Recipient.GetAsync("/offers");
        Assert.DoesNotContain(deal.OfferId.ToString(), await offers.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        // A live negotiation of the same kind is untouched by all this.
        using var liveRead = await live.Maker.GetAsync($"/negotiations/{live.NegotiationId}");
        Assert.Equal(HttpStatusCode.OK, liveRead.StatusCode);
        using var liveAccept = await live.Recipient.PostAsync($"/offers/{live.OfferId}/accept", null);
        Assert.Equal(HttpStatusCode.OK, liveAccept.StatusCode);
    }

    // ---- A deleted offer in a live negotiation -----------------------------------------

    // Only the offer is deleted: read, accept, decline, counter all get the unknown-id answer;
    // the negotiation reads without it, and its sender may place a new one.
    [Fact]
    public async Task Engine_DeletedOffer_NoReadNoAction_SameAnswerAsUnknown_TheNegotiationGoesOn()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        await DeleteThroughIntentAsync("offers", deal.OfferId, session);
        var unknown = Guid.NewGuid();

        await AssertLikeUnknownAsync(deal.Maker, HttpMethod.Get, $"/offers/{deal.OfferId}", $"/offers/{unknown}");
        await AssertLikeUnknownAsync(deal.Recipient, HttpMethod.Get, $"/offers/{deal.OfferId}", $"/offers/{unknown}");
        await AssertLikeUnknownAsync(deal.Recipient, HttpMethod.Post, $"/offers/{deal.OfferId}/accept", $"/offers/{unknown}/accept");
        await AssertLikeUnknownAsync(deal.Recipient, HttpMethod.Post, $"/offers/{deal.OfferId}/decline", $"/offers/{unknown}/decline");
        await AssertLikeUnknownAsync(deal.Recipient, HttpMethod.Post, $"/offers/{deal.OfferId}/counter", $"/offers/{unknown}/counter",
            new CreateOfferRequest { Amount = 7m, Currency = "USD" });
        var offer = await OfferAsync(deal.OfferId);
        Assert.Equal(OfferStatus.Open, offer.Status);
        Assert.Equal(1u, offer.Version);

        using var read = await deal.Maker.GetAsync($"/negotiations/{deal.NegotiationId}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.DoesNotContain(deal.OfferId.ToString(), await read.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        using var offers = await deal.Maker.GetAsync("/offers");
        Assert.DoesNotContain(deal.OfferId.ToString(), await offers.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        // The deleted open offer no longer counts as the maker's open offer.
        using var again = await deal.Maker.PostAsJsonAsync(
            $"/negotiations/{deal.NegotiationId}/offers", new CreateOfferRequest { Amount = 950m, Currency = "USD" });
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        var newOffer = (await again.Content.ReadFromJsonAsync<OfferResponse>())!.Id;
        using var accept = await deal.Recipient.PostAsync($"/offers/{newOffer}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);

        // Accepting the new offer did not touch the deleted one.
        var deleted = await OfferAsync(deal.OfferId);
        Assert.Equal(OfferStatus.Open, deleted.Status);
        Assert.Equal(1u, deleted.Version);
    }

    // ---- A deleted artifact ----------------------------------------------------------

    // No new negotiation on it (the unknown-artifact answer); its owner cannot read it; it is
    // gone from the owner's list and from search.
    [Fact]
    public async Task Engine_DeletedArtifact_NoNewNegotiation_NoRead_NotListedOrFound_SameAnswerAsUnknown()
    {
        var deal = await PartyDealAsync();
        var session = await NewSessionAsync();
        var word = $"zq{Guid.NewGuid():N}"[..14];
        var artifactId = await NewArtifactAsync(deal.Maker, word);
        var liveArtifactId = await NewArtifactAsync(deal.Maker, word);
        await DeleteThroughIntentAsync("artifacts", artifactId, session);

        using var onDeleted = await deal.Maker.PostAsJsonAsync("/negotiations", NegotiationWith(artifactId, deal.RecipientSub));
        using var onUnknown = await deal.Maker.PostAsJsonAsync("/negotiations", NegotiationWith(Guid.NewGuid(), deal.RecipientSub));
        Assert.Equal(HttpStatusCode.NotFound, onDeleted.StatusCode);
        await AssertSameAnswerAsync(onUnknown, onDeleted);
        await AssertLikeUnknownAsync(deal.Maker, HttpMethod.Get, $"/artifacts/{artifactId}", $"/artifacts/{Guid.NewGuid()}");

        using var list = await deal.Maker.GetAsync("/artifacts");
        var listText = await list.Content.ReadAsStringAsync();
        Assert.DoesNotContain(artifactId.ToString(), listText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(liveArtifactId.ToString(), listText, StringComparison.OrdinalIgnoreCase);

        using var search = await deal.Recipient.GetAsync($"/search/artifacts?q={word}");
        var searchText = await search.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        Assert.DoesNotContain(artifactId.ToString(), searchText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(liveArtifactId.ToString(), searchText, StringComparison.OrdinalIgnoreCase);

        using var onLive = await deal.Maker.PostAsJsonAsync("/negotiations", NegotiationWith(liveArtifactId, deal.RecipientSub));
        Assert.Equal(HttpStatusCode.Created, onLive.StatusCode);
    }

    // ---- Harness -------------------------------------------------------------------------

    private async Task<string> JwtForAsync(string apiKey)
    {
        var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/auth/token", new TokenRequest { ApiKey = apiKey });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;
    }

    private async Task<HttpResponseMessage> SendAsPartyAsync(HttpMethod method, string path, string scheme, string credential)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue(scheme, credential);
        return await client.SendAsync(request);
    }

    private static async Task<Guid> NewArtifactAsync(HttpClient owner, string name = "Party item")
    {
        using var response = await owner.PostAsJsonAsync("/artifacts", new CreateArtifactRequest
        {
            Entities = [new CreateSubjectEntityDto { Name = name, Description = "A negotiable item" }],
            Intent = "sell"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ArtifactResponse>())!.Id;
    }

    private static CreateNegotiationRequest NegotiationWith(Guid artifactId, string counterpartySub) => new()
    {
        ArtifactId = artifactId,
        CounterpartyParticipantId = counterpartySub,
        CallerIntent = "sell",
        CounterpartyIntent = "buy"
    };

    /// <summary>The request on the deleted row gets exactly what the same request on an unknown id gets.</summary>
    private static async Task AssertLikeUnknownAsync(
        HttpClient party,
        HttpMethod method,
        string deletedPath,
        string unknownPath,
        object? body = null)
    {
        async Task<HttpResponseMessage> Send(string path)
        {
            var request = new HttpRequestMessage(method, path);
            if (body is not null)
                request.Content = JsonContent.Create(body, body.GetType());
            return await party.SendAsync(request);
        }

        using var deleted = await Send(deletedPath);
        using var unknown = await Send(unknownPath);
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
        await AssertSameAnswerAsync(unknown, deleted);
    }

    private static async Task AssertSameAnswerAsync(HttpResponseMessage expected, HttpResponseMessage actual)
    {
        Assert.Equal(expected.StatusCode, actual.StatusCode);
        Assert.Equal(
            Normalize(await expected.Content.ReadAsStringAsync()),
            Normalize(await actual.Content.ReadAsStringAsync()));
    }

    /// <summary>A problem body carries the request's trace id; nothing else may differ.</summary>
    private static string Normalize(string body)
        => System.Text.RegularExpressions.Regex.Replace(body, "\"traceId\":\"[^\"]*\"", "\"traceId\":\"\"");
}
