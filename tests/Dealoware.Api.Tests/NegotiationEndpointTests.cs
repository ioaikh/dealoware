using System.Net;
using System.Net.Http.Json;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Negotiations.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dealoware.Api.Tests;

/// <summary>
/// Tests for Negotiation endpoints (D7-D10).
/// Covers create, get, place offers, accept/decline/counter, close, and expiration.
/// </summary>
[Collection("WebAppTests")]
public class NegotiationEndpointTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public NegotiationEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, string Sub, string ApiKey)> CreateAuthenticatedClientAsync(string suffix)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = $"participant-{suffix}" });
        var participant = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant!.ApiKey}");
        return (client, participant.Sub, participant.ApiKey);
    }

    private static CreateArtifactRequest CreateArtifactRequest(string intent) => new()
    {
        Entities = new List<CreateSubjectEntityDto>
        {
            new() { Name = "Test Item", Description = "A negotiable item" }
        },
        Intent = intent
    };

    #region Create Negotiation Tests

    [Fact]
    public async Task CreateNegotiation_ValidComplementaryIntents_Returns201()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"neg-create-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"neg-create-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negotiationRequest = new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        };

        var response = await clientA.PostAsJsonAsync("/negotiations", negotiationRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var negotiation = await response.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.NotNull(negotiation);
        Assert.NotEqual(Guid.Empty, negotiation.Id);
        Assert.Equal(artifact.Id, negotiation.ArtifactId);
        Assert.Equal(subA, negotiation.PartyAParticipantId);
        Assert.Equal(subB, negotiation.PartyBParticipantId);
        Assert.Equal("Open", negotiation.Status);
    }

    [Theory]
    [InlineData("buy", "sell")]
    [InlineData("sell", "buy")]
    [InlineData("provide", "consume")]
    [InlineData("consume", "provide")]
    [InlineData("rent", "rent")]
    public async Task CreateNegotiation_AllComplementaryPairs_Succeed(string callerIntent, string counterpartyIntent)
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"neg-pair-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"neg-pair-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest(callerIntent));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var request = new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = callerIntent,
            CounterpartyIntent = counterpartyIntent
        };

        var response = await clientA.PostAsJsonAsync("/negotiations", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("buy", "buy")]
    [InlineData("sell", "sell")]
    [InlineData("buy", "provide")]
    [InlineData("sell", "consume")]
    [InlineData("provide", "provide")]
    public async Task CreateNegotiation_NonComplementaryIntents_Returns400(string callerIntent, string counterpartyIntent)
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"neg-noncomp-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"neg-noncomp-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest(callerIntent));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var request = new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = callerIntent,
            CounterpartyIntent = counterpartyIntent
        };

        var response = await clientA.PostAsJsonAsync("/negotiations", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("complementary", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateNegotiation_MissingArtifact_Returns404()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"neg-noart-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"neg-noart-b-{Guid.NewGuid()}");

        var request = new CreateNegotiationRequest
        {
            ArtifactId = Guid.NewGuid(),
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        };

        var response = await clientA.PostAsJsonAsync("/negotiations", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateNegotiation_SamePartyAAndB_Returns400()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"neg-same-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var request = new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subA,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        };

        var response = await clientA.PostAsJsonAsync("/negotiations", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateNegotiation_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new CreateNegotiationRequest
        {
            ArtifactId = Guid.NewGuid(),
            CounterpartyParticipantId = "participant:test",
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        };

        var response = await client.PostAsJsonAsync("/negotiations", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Get Negotiation Tests

    [Fact]
    public async Task GetNegotiation_AsParty_Returns200WithOffers()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"neg-get-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"neg-get-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var createResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var responseA = await clientA.GetAsync($"/negotiations/{created!.Id}");
        var responseB = await clientB.GetAsync($"/negotiations/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
    }

    [Fact]
    public async Task GetNegotiation_NonParty_Returns404()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"neg-nonparty-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"neg-nonparty-b-{Guid.NewGuid()}");
        var (clientC, _, _) = await CreateAuthenticatedClientAsync($"neg-nonparty-c-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var createResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var response = await clientC.GetAsync($"/negotiations/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetNegotiation_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/negotiations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Place Offer Tests

    [Fact]
    public async Task PlaceOffer_ValidRequest_Returns201()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"offer-place-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"offer-place-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerRequest = new CreateOfferRequest
        {
            Amount = 1000m,
            Currency = "USD",
            Terms = "Cash payment preferred"
        };

        var response = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers", offerRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var offer = await response.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.NotNull(offer);
        Assert.Equal("Open", offer.Status);
        Assert.Equal(1000m, offer.Amount);
        Assert.Equal("USD", offer.Currency);
        Assert.Equal(subA, offer.FromParticipantId);
        Assert.Equal(subB, offer.ToParticipantId);
    }

    [Fact]
    public async Task PlaceOffer_OneOpenPerSide_SecondAttemptReturns400()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"offer-one-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"offer-one-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerRequest = new CreateOfferRequest { Amount = 1000m, Currency = "USD" };
        await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers", offerRequest);

        var secondResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation.Id}/offers", offerRequest);

        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
        var content = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("already have an open offer", content);
    }

    [Fact]
    public async Task PlaceOffer_ClosedNegotiation_Returns409()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"offer-closed-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"offer-closed-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        await clientA.PostAsync($"/negotiations/{negotiation!.Id}/close", null);

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });

        Assert.Equal(HttpStatusCode.Conflict, offerResponse.StatusCode);
    }

    [Fact]
    public async Task PlaceOffer_NonParty_Returns404()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"offer-nonparty-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"offer-nonparty-b-{Guid.NewGuid()}");
        var (clientC, _, _) = await CreateAuthenticatedClientAsync($"offer-nonparty-c-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var response = await clientC.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOffer_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/negotiations/{Guid.NewGuid()}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Accept Offer Tests

    [Fact]
    public async Task AcceptOffer_ValidRequest_AcceptsAndCancelsOtherOpens()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"accept-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"accept-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerAResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offerA = await offerAResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var offerBResponse = await clientB.PostAsJsonAsync($"/negotiations/{negotiation.Id}/offers",
            new CreateOfferRequest { Amount = 900m, Currency = "USD" });
        var offerB = await offerBResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var acceptResponse = await clientB.PostAsync($"/offers/{offerA!.Id}/accept", null);

        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var accepted = await acceptResponse.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.Equal("Accepted", accepted!.Status);
        Assert.DoesNotContain("contact", accepted.Terms ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", accepted.Terms ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phone", accepted.Terms ?? "", StringComparison.OrdinalIgnoreCase);

        var negResult = await clientA.GetAsync($"/negotiations/{negotiation.Id}");
        var negState = await negResult.Content.ReadFromJsonAsync<NegotiationResponse>();
        var otherOffer = negState!.Offers!.First(o => o.Id == offerB!.Id);
        Assert.Equal("Cancelled", otherOffer.Status);
    }

    [Fact]
    public async Task AcceptOffer_NotRecipient_Returns404()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"accept-notrecip-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"accept-notrecip-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var acceptResponse = await clientA.PostAsync($"/offers/{offer!.Id}/accept", null);

        Assert.Equal(HttpStatusCode.NotFound, acceptResponse.StatusCode);
    }

    [Fact]
    public async Task AcceptOffer_AlreadyAccepted_Returns409()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"accept-twice-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"accept-twice-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        await clientB.PostAsync($"/offers/{offer!.Id}/accept", null);
        var secondAccept = await clientB.PostAsync($"/offers/{offer.Id}/accept", null);

        Assert.Equal(HttpStatusCode.Conflict, secondAccept.StatusCode);
    }

    [Fact]
    public async Task AcceptOffer_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/offers/{Guid.NewGuid()}/accept", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Decline Offer Tests

    [Fact]
    public async Task DeclineOffer_ValidRequest_Returns200()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"decline-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"decline-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var declineResponse = await clientB.PostAsync($"/offers/{offer!.Id}/decline", null);

        Assert.Equal(HttpStatusCode.OK, declineResponse.StatusCode);
        var declined = await declineResponse.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.Equal("Declined", declined!.Status);
    }

    [Fact]
    public async Task DeclineOffer_NotRecipient_Returns404()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"decline-notrecip-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"decline-notrecip-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var declineResponse = await clientA.PostAsync($"/offers/{offer!.Id}/decline", null);

        Assert.Equal(HttpStatusCode.NotFound, declineResponse.StatusCode);
    }

    [Fact]
    public async Task DeclineOffer_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/offers/{Guid.NewGuid()}/decline", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Counter Offer Tests

    [Fact]
    public async Task CounterOffer_ValidRequest_SupersedesPriorAndCreatesNew()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"counter-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"counter-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var originalOfferResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var originalOffer = await originalOfferResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var counterResponse = await clientB.PostAsJsonAsync($"/offers/{originalOffer!.Id}/counter",
            new CounterOfferRequest { Amount = 800m, Currency = "USD", Terms = "Counter terms" });

        Assert.Equal(HttpStatusCode.Created, counterResponse.StatusCode);
        var counterOffer = await counterResponse.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.NotNull(counterOffer);
        Assert.Equal("Open", counterOffer.Status);
        Assert.Equal(800m, counterOffer.Amount);
        Assert.Equal(subB, counterOffer.FromParticipantId);
        Assert.Equal(subA, counterOffer.ToParticipantId);

        var negResult = await clientA.GetAsync($"/negotiations/{negotiation.Id}");
        var negState = await negResult.Content.ReadFromJsonAsync<NegotiationResponse>();
        var supersededOffer = negState!.Offers!.First(o => o.Id == originalOffer.Id);
        Assert.Equal("Superseded", supersededOffer.Status);
    }

    [Fact]
    public async Task CounterOffer_AlreadyHasOpen_Returns400()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"counter-hasopen-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"counter-hasopen-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerA = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offerAData = await offerA.Content.ReadFromJsonAsync<OfferResponse>();

        await clientB.PostAsJsonAsync($"/negotiations/{negotiation.Id}/offers",
            new CreateOfferRequest { Amount = 800m, Currency = "USD" });

        var counterResponse = await clientB.PostAsJsonAsync($"/offers/{offerAData!.Id}/counter",
            new CounterOfferRequest { Amount = 750m, Currency = "USD" });

        Assert.Equal(HttpStatusCode.BadRequest, counterResponse.StatusCode);
    }

    [Fact]
    public async Task CounterOffer_NotRecipient_Returns404()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"counter-notrecip-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"counter-notrecip-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var counterResponse = await clientA.PostAsJsonAsync($"/offers/{offer!.Id}/counter",
            new CounterOfferRequest { Amount = 800m, Currency = "USD" });

        Assert.Equal(HttpStatusCode.NotFound, counterResponse.StatusCode);
    }

    [Fact]
    public async Task CounterOffer_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/offers/{Guid.NewGuid()}/counter",
            new CounterOfferRequest { Amount = 800m, Currency = "USD" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Close Negotiation Tests

    [Fact]
    public async Task CloseNegotiation_CancelsAllOpenOffers()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"close-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"close-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        await clientB.PostAsJsonAsync($"/negotiations/{negotiation.Id}/offers",
            new CreateOfferRequest { Amount = 900m, Currency = "USD" });

        var closeResponse = await clientA.PostAsync($"/negotiations/{negotiation.Id}/close", null);

        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);
        var closed = await closeResponse.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.Equal("Closed", closed!.Status);
        Assert.All(closed.Offers!, o => Assert.Equal("Cancelled", o.Status));
    }

    [Fact]
    public async Task CloseNegotiation_PostClose_MutationsReturn409()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"close-postmut-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"close-postmut-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        await clientA.PostAsync($"/negotiations/{negotiation!.Id}/close", null);

        var placeResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });

        Assert.Equal(HttpStatusCode.Conflict, placeResponse.StatusCode);
    }

    [Fact]
    public async Task CloseNegotiation_AlreadyClosed_Returns409()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"close-twice-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"close-twice-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        await clientA.PostAsync($"/negotiations/{negotiation!.Id}/close", null);
        var secondClose = await clientA.PostAsync($"/negotiations/{negotiation.Id}/close", null);

        Assert.Equal(HttpStatusCode.Conflict, secondClose.StatusCode);
    }

    [Fact]
    public async Task CloseNegotiation_NonParty_Returns404()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"close-nonparty-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"close-nonparty-b-{Guid.NewGuid()}");
        var (clientC, _, _) = await CreateAuthenticatedClientAsync($"close-nonparty-c-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var closeResponse = await clientC.PostAsync($"/negotiations/{negotiation!.Id}/close", null);

        Assert.Equal(HttpStatusCode.NotFound, closeResponse.StatusCode);
    }

    [Fact]
    public async Task CloseNegotiation_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/negotiations/{Guid.NewGuid()}/close", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Expiration (D10) Tests

    [Fact]
    public async Task Expiration_ExpiredNegotiation_MutationsReturn409_GETReturnsOK()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"expire-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"expire-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy",
            EndsAt = DateTimeOffset.UtcNow.AddMilliseconds(-100)
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var getResponse = await clientA.GetAsync($"/negotiations/{negotiation!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.Equal("Expired", fetched!.Status);

        var placeResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        Assert.Equal(HttpStatusCode.Conflict, placeResponse.StatusCode);

        var closeResponse = await clientA.PostAsync($"/negotiations/{negotiation.Id}/close", null);
        Assert.Equal(HttpStatusCode.Conflict, closeResponse.StatusCode);
    }

    [Fact]
    public async Task Expiration_OfferMutationsOnExpiredNegotiation_Return409()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"expire-offer-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"expire-offer-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy",
            EndsAt = DateTimeOffset.UtcNow.AddSeconds(2)
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        await Task.Delay(2500);

        var acceptResponse = await clientB.PostAsync($"/offers/{offer!.Id}/accept", null);
        Assert.Equal(HttpStatusCode.Conflict, acceptResponse.StatusCode);

        var declineResponse = await clientB.PostAsync($"/offers/{offer.Id}/decline", null);
        Assert.Equal(HttpStatusCode.Conflict, declineResponse.StatusCode);

        var counterResponse = await clientB.PostAsJsonAsync($"/offers/{offer.Id}/counter",
            new CounterOfferRequest { Amount = 800m, Currency = "USD" });
        Assert.Equal(HttpStatusCode.Conflict, counterResponse.StatusCode);
    }

    #endregion

    #region Health Endpoint Tests

    [Fact]
    public async Task Health_WithoutAuth_Returns200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}
