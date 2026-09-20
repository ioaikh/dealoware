using System.Net;
using System.Net.Http.Json;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Negotiations.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dealoware.Api.Tests;

/// <summary>
/// PoC Negotiation Scenario Tests.
/// 
/// Maps directly to scenario IDs from:
/// docs/product/2026-09-20__product__guide__poc-negotiation-scenarios.md
/// 
/// See also: tests/ScenarioMap.md for S* → test method mapping.
/// 
/// Exercises the full negotiation flow including:
/// - S1-S2: Bootstrap (register participants, create artifact)
/// - S3-S6: Happy path deal flow (create negotiation → offer → counter → accept)
/// - S7: Alternate — decline offer
/// - S8: Alternate — close negotiation
/// - S9: Negative — non-complementary intents
/// - S10: Negative — non-party access (404, no info leak)
/// - S11: Negative — duplicate open offer same side
/// - S12: Negative — mutations after close
/// - S13: Optional — expiration via endsAt (P1)
/// - S14: Optional — provide↔consume (P1)
/// 
/// Intent pairs in this suite: buy↔sell and provide↔consume only.
/// </summary>
[Collection("WebAppTests")]
public class PocNegotiationScenarioTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public PocNegotiationScenarioTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    #region Test Helpers

    private async Task<(HttpClient Client, string Sub, string ApiKey)> RegisterParticipantAsync(string displayName)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = displayName });
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var participant = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(participant);
        Assert.NotNull(participant.ApiKey);
        Assert.StartsWith("participant:", participant.Sub);
        
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant.ApiKey}");
        return (client, participant.Sub, participant.ApiKey);
    }

    private static CreateArtifactRequest CreateSellArtifactRequest() => new()
    {
        Entities = new List<CreateSubjectEntityDto>
        {
            new()
            {
                Name = "2020 Toyota Camry",
                Description = "Well-maintained sedan, single owner",
                Properties = new List<CreatePropertyDto>
                {
                    new() { Name = "mileage", Type = "number", Value = "45000" },
                    new() { Name = "color", Type = "string", Value = "silver" }
                },
                Facts = new List<string> { "Clean title", "Regular maintenance" }
            }
        },
        Intent = "sell",
        Values = new List<CreateValueDto> { new() { Amount = 22000m, Currency = "USD" } },
        Locations = new List<string> { "Los Angeles, CA" }
    };

    #endregion

    #region S1-S2: Bootstrap — Register Participants and Create Artifact

    /// <summary>
    /// S1.1: Register Seller participant.
    /// Expected: 201, returns sub and apiKey.
    /// </summary>
    [Fact]
    public async Task S1_1_RegisterSeller_Returns201WithCredentials()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = "Seller" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var participant = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        
        Assert.NotNull(participant);
        Assert.NotNull(participant.Sub);
        Assert.StartsWith("participant:", participant.Sub);
        Assert.NotNull(participant.ApiKey);
        Assert.StartsWith("dlw_", participant.ApiKey);
        Assert.NotNull(participant.ApiKeyPrefix);
        Assert.Equal("Seller", participant.DisplayName);
        Assert.True(participant.CreatedAt > DateTimeOffset.MinValue);
    }

    /// <summary>
    /// S1.2: Register Buyer participant.
    /// Expected: 201, returns sub and apiKey.
    /// </summary>
    [Fact]
    public async Task S1_2_RegisterBuyer_Returns201WithCredentials()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = "Buyer" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var participant = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        
        Assert.NotNull(participant);
        Assert.StartsWith("participant:", participant.Sub);
        Assert.StartsWith("dlw_", participant.ApiKey);
        Assert.Equal("Buyer", participant.DisplayName);
    }

    /// <summary>
    /// S2.1: Seller creates sell artifact.
    /// Expected: 201, returns artifact with id and intent=sell.
    /// </summary>
    [Fact]
    public async Task S2_1_SellerCreatesArtifact_Returns201WithId()
    {
        var (sellerClient, _, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");

        var response = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var artifact = await response.Content.ReadFromJsonAsync<ArtifactResponse>();
        
        Assert.NotNull(artifact);
        Assert.NotEqual(Guid.Empty, artifact.Id);
        Assert.Equal("sell", artifact.Intent);
        Assert.NotNull(artifact.Entities);
        Assert.Single(artifact.Entities);
        Assert.Equal("2020 Toyota Camry", artifact.Entities[0].Name);
    }

    #endregion

    #region S3-S6: Happy Path — Full Deal Flow

    /// <summary>
    /// S3.1: Seller creates negotiation with Buyer (sell ↔ buy).
    /// Expected: 201, returns negotiation with status=Open.
    /// </summary>
    [Fact]
    public async Task S3_1_SellerCreatesNegotiation_Returns201Open()
    {
        var (sellerClient, sellerSub, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (_, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negotiationRequest = new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        };

        var response = await sellerClient.PostAsJsonAsync("/negotiations", negotiationRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var negotiation = await response.Content.ReadFromJsonAsync<NegotiationResponse>();
        
        Assert.NotNull(negotiation);
        Assert.NotEqual(Guid.Empty, negotiation.Id);
        Assert.Equal(artifact.Id, negotiation.ArtifactId);
        Assert.Equal(sellerSub, negotiation.PartyAParticipantId);
        Assert.Equal(buyerSub, negotiation.PartyBParticipantId);
        Assert.Equal("sell", negotiation.PartyAIntent);
        Assert.Equal("buy", negotiation.PartyBIntent);
        Assert.Equal("Open", negotiation.Status);
        Assert.True(negotiation.IdentitySealed);
    }

    /// <summary>
    /// S4.1: Seller places first offer ($22,000).
    /// Expected: 201, returns offer with status=Open.
    /// </summary>
    [Fact]
    public async Task S4_1_SellerPlacesOffer_Returns201Open()
    {
        var (sellerClient, sellerSub, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (_, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await sellerClient.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerRequest = new CreateOfferRequest
        {
            Amount = 22000m,
            Currency = "USD",
            Terms = "Cash only, as-is condition"
        };

        var response = await sellerClient.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers", offerRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var offer = await response.Content.ReadFromJsonAsync<OfferResponse>();
        
        Assert.NotNull(offer);
        Assert.NotEqual(Guid.Empty, offer.Id);
        Assert.Equal(negotiation.Id, offer.NegotiationId);
        Assert.Equal(sellerSub, offer.FromParticipantId);
        Assert.Equal(buyerSub, offer.ToParticipantId);
        Assert.Equal("Open", offer.Status);
        Assert.Equal(22000m, offer.Amount);
        Assert.Equal("USD", offer.Currency);
        Assert.Equal("Cash only, as-is condition", offer.Terms);
        Assert.True(offer.IdentitySealed);
    }

    /// <summary>
    /// S5.1: Buyer counters with $18,000.
    /// Expected: 201, original offer → Superseded, new counter-offer with status=Open.
    /// </summary>
    [Fact]
    public async Task S5_1_BuyerCounters_Returns201SupersedesOriginal()
    {
        var (sellerClient, sellerSub, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (buyerClient, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await sellerClient.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await sellerClient.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 22000m, Currency = "USD", Terms = "Cash only" });
        var originalOffer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var counterRequest = new CounterOfferRequest
        {
            Amount = 18000m,
            Currency = "USD",
            Terms = "Financing available, inspection required"
        };

        var response = await buyerClient.PostAsJsonAsync($"/offers/{originalOffer!.Id}/counter", counterRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var counterOffer = await response.Content.ReadFromJsonAsync<OfferResponse>();
        
        Assert.NotNull(counterOffer);
        Assert.Equal("Open", counterOffer.Status);
        Assert.Equal(18000m, counterOffer.Amount);
        Assert.Equal(buyerSub, counterOffer.FromParticipantId);
        Assert.Equal(sellerSub, counterOffer.ToParticipantId);

        var negResult = await sellerClient.GetAsync($"/negotiations/{negotiation.Id}");
        var finalNeg = await negResult.Content.ReadFromJsonAsync<NegotiationResponse>();
        var supersededOffer = finalNeg!.Offers!.First(o => o.Id == originalOffer.Id);
        Assert.Equal("Superseded", supersededOffer.Status);
    }

    /// <summary>
    /// S6.1: Seller accepts Buyer's counter-offer.
    /// Expected: 200, offer status=Accepted, identitySealed=true (no PII release in PoC).
    /// </summary>
    [Fact]
    public async Task S6_1_SellerAcceptsCounter_Returns200Accepted()
    {
        var (sellerClient, _, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (buyerClient, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await sellerClient.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await sellerClient.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 22000m, Currency = "USD" });
        var originalOffer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var counterResponse = await buyerClient.PostAsJsonAsync($"/offers/{originalOffer!.Id}/counter",
            new CounterOfferRequest { Amount = 18000m, Currency = "USD" });
        var counterOffer = await counterResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var response = await sellerClient.PostAsync($"/offers/{counterOffer!.Id}/accept", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var accepted = await response.Content.ReadFromJsonAsync<OfferResponse>();
        
        Assert.NotNull(accepted);
        Assert.Equal("Accepted", accepted.Status);
        Assert.True(accepted.IdentitySealed);
    }

    /// <summary>
    /// S6.2: View final negotiation state after deal.
    /// Expected: 200, negotiation contains accepted offer.
    /// </summary>
    [Fact]
    public async Task S6_2_ViewFinalNegotiationState_ContainsAcceptedOffer()
    {
        var (sellerClient, _, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (buyerClient, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await sellerClient.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await sellerClient.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 22000m, Currency = "USD" });
        var originalOffer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var counterResponse = await buyerClient.PostAsJsonAsync($"/offers/{originalOffer!.Id}/counter",
            new CounterOfferRequest { Amount = 18000m, Currency = "USD" });
        var counterOffer = await counterResponse.Content.ReadFromJsonAsync<OfferResponse>();

        await sellerClient.PostAsync($"/offers/{counterOffer!.Id}/accept", null);

        var response = await sellerClient.GetAsync($"/negotiations/{negotiation.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var finalNeg = await response.Content.ReadFromJsonAsync<NegotiationResponse>();
        
        Assert.NotNull(finalNeg);
        Assert.NotNull(finalNeg.Offers);
        Assert.Contains(finalNeg.Offers, o => o.Status == "Accepted");
    }

    /// <summary>
    /// Scenario_S1_to_S6_HappyPathDeal: Complete happy path flow as single end-to-end test.
    /// S1: Register Seller + Buyer → S2: Create sell artifact → S3: Create negotiation (sell↔buy)
    /// → S4: Seller places offer → S5: Buyer counters → S6: Seller accepts counter.
    /// Mirrors Postman collection 02-Happy-Path-Deal folder.
    /// </summary>
    [Fact]
    public async Task Scenario_S1_to_S6_HappyPathDeal()
    {
        var (sellerClient, sellerSub, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (buyerClient, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        Assert.Equal(HttpStatusCode.Created, artifactResponse.StatusCode);
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.Equal("sell", artifact!.Intent);

        var negResponse = await sellerClient.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        Assert.Equal(HttpStatusCode.Created, negResponse.StatusCode);
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.Equal("Open", negotiation!.Status);
        Assert.Equal("sell", negotiation.PartyAIntent);
        Assert.Equal("buy", negotiation.PartyBIntent);

        var offer1Response = await sellerClient.PostAsJsonAsync($"/negotiations/{negotiation.Id}/offers",
            new CreateOfferRequest { Amount = 22000m, Currency = "USD", Terms = "Cash only, as-is" });
        Assert.Equal(HttpStatusCode.Created, offer1Response.StatusCode);
        var offer1 = await offer1Response.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.Equal("Open", offer1!.Status);
        Assert.Equal(22000m, offer1.Amount);

        var counterResponse = await buyerClient.PostAsJsonAsync($"/offers/{offer1.Id}/counter",
            new CounterOfferRequest { Amount = 18000m, Currency = "USD", Terms = "Financing available" });
        Assert.Equal(HttpStatusCode.Created, counterResponse.StatusCode);
        var counter = await counterResponse.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.Equal("Open", counter!.Status);
        Assert.Equal(18000m, counter.Amount);

        var acceptResponse = await sellerClient.PostAsync($"/offers/{counter.Id}/accept", null);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var accepted = await acceptResponse.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.Equal("Accepted", accepted!.Status);
        Assert.True(accepted.IdentitySealed);

        var finalGetResponse = await sellerClient.GetAsync($"/negotiations/{negotiation.Id}");
        Assert.Equal(HttpStatusCode.OK, finalGetResponse.StatusCode);
        var finalNeg = await finalGetResponse.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.Contains(finalNeg!.Offers!, o => o.Status == "Accepted" && o.Amount == 18000m);
        Assert.Contains(finalNeg.Offers!, o => o.Status == "Superseded" && o.Amount == 22000m);
    }

    #endregion

    #region S7: Alternate — Buyer Declines Offer

    /// <summary>
    /// S7.1: Buyer declines Seller's offer.
    /// Expected: 200, offer status=Declined, negotiation remains Open.
    /// </summary>
    [Fact]
    public async Task S7_1_BuyerDeclinesOffer_Returns200Declined()
    {
        var (sellerClient, _, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (buyerClient, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await sellerClient.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await sellerClient.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 25000m, Currency = "USD", Terms = "Firm price" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var response = await buyerClient.PostAsync($"/offers/{offer!.Id}/decline", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var declined = await response.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.Equal("Declined", declined!.Status);

        var negResult = await sellerClient.GetAsync($"/negotiations/{negotiation.Id}");
        var finalNeg = await negResult.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.Equal("Open", finalNeg!.Status);
    }

    #endregion

    #region S8: Alternate — Close Negotiation

    /// <summary>
    /// S8.1: Close negotiation, all open offers cancelled.
    /// Expected: 200, negotiation status=Closed, open offers → Cancelled.
    /// </summary>
    [Fact]
    public async Task S8_1_CloseNegotiation_Returns200Closed()
    {
        var (sellerClient, _, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (_, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await sellerClient.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        await sellerClient.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 20000m, Currency = "USD", Terms = "Open offer" });

        var response = await sellerClient.PostAsync($"/negotiations/{negotiation.Id}/close", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var closed = await response.Content.ReadFromJsonAsync<NegotiationResponse>();
        
        Assert.Equal("Closed", closed!.Status);
        Assert.NotNull(closed.Offers);
        Assert.All(closed.Offers, o => Assert.Equal("Cancelled", o.Status));
    }

    #endregion

    #region S9: Negative — Non-Complementary Intents

    /// <summary>
    /// S9.1: Non-complementary intents (both sell) rejected.
    /// Expected: 400, error mentions "complementary".
    /// </summary>
    [Fact]
    public async Task S9_1_NonComplementaryIntents_Returns400()
    {
        var (sellerClient, _, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (_, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var request = new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "sell"
        };

        var response = await sellerClient.PostAsJsonAsync("/negotiations", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("complementary", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// S9: Additional non-complementary pairs (buy/buy, provide/provide, cross-category).
    /// Intent pairs in this suite: buy↔sell and provide↔consume only.
    /// </summary>
    [Theory]
    [InlineData("buy", "buy")]
    [InlineData("sell", "sell")]
    [InlineData("provide", "provide")]
    [InlineData("consume", "consume")]
    [InlineData("buy", "provide")]
    [InlineData("sell", "consume")]
    public async Task S9_NonComplementaryPairs_AllReturn400(string callerIntent, string counterpartyIntent)
    {
        var (clientA, _, _) = await RegisterParticipantAsync($"A-{Guid.NewGuid():N}");
        var (_, subB, _) = await RegisterParticipantAsync($"B-{Guid.NewGuid():N}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto> { new() { Name = "Test Item" } },
            Intent = callerIntent
        });
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

    #endregion

    #region S10: Negative — Non-Party GET (404, No Info Leak)

    /// <summary>
    /// S10.1-S10.3: Register Observer and attempt to view negotiation.
    /// Expected: 404 (not 403) to prevent information leakage.
    /// </summary>
    [Fact]
    public async Task S10_NonPartyGet_Returns404NotFound()
    {
        var (sellerClient, _, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (_, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");
        var (observerClient, _, _) = await RegisterParticipantAsync($"Observer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await sellerClient.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var response = await observerClient.GetAsync($"/negotiations/{negotiation!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region S11: Negative — Second Open Offer Same Side

    /// <summary>
    /// S11.1-S11.3: Party cannot have two open offers simultaneously.
    /// Expected: 400, error mentions "already have an open offer".
    /// </summary>
    [Fact]
    public async Task S11_SecondOpenOfferSameSide_Returns400()
    {
        var (sellerClient, _, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (_, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await sellerClient.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var firstOfferResponse = await sellerClient.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 21000m, Currency = "USD", Terms = "First offer" });
        Assert.Equal(HttpStatusCode.Created, firstOfferResponse.StatusCode);

        var secondOfferResponse = await sellerClient.PostAsJsonAsync($"/negotiations/{negotiation.Id}/offers",
            new CreateOfferRequest { Amount = 20000m, Currency = "USD", Terms = "Second attempt" });

        Assert.Equal(HttpStatusCode.BadRequest, secondOfferResponse.StatusCode);
        var content = await secondOfferResponse.Content.ReadAsStringAsync();
        Assert.Contains("open offer", content, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region S12: Negative — Mutations After Close

    /// <summary>
    /// S12.1-S12.4: Closed negotiations reject all mutations.
    /// Expected: 409 Conflict.
    /// </summary>
    [Fact]
    public async Task S12_MutationsAfterClose_Return409()
    {
        var (sellerClient, _, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (buyerClient, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await sellerClient.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var closeResponse = await sellerClient.PostAsync($"/negotiations/{negotiation!.Id}/close", null);
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);
        var closed = await closeResponse.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.Equal("Closed", closed!.Status);

        var placeOfferResponse = await buyerClient.PostAsJsonAsync($"/negotiations/{negotiation.Id}/offers",
            new CreateOfferRequest { Amount = 19000m, Currency = "USD" });
        Assert.Equal(HttpStatusCode.Conflict, placeOfferResponse.StatusCode);
        var placeContent = await placeOfferResponse.Content.ReadAsStringAsync();
        Assert.Contains("Closed", placeContent);

        var doubleCloseResponse = await sellerClient.PostAsync($"/negotiations/{negotiation.Id}/close", null);
        Assert.Equal(HttpStatusCode.Conflict, doubleCloseResponse.StatusCode);
    }

    #endregion

    #region S13: Optional — Expiration via EndsAt

    /// <summary>
    /// S13.1-S13.3: Expired negotiations reject mutations, GET still works.
    /// Expected: 409 for mutations, 200 for GET with status=Expired.
    /// </summary>
    [Fact]
    public async Task S13_ExpiredNegotiation_MutationsReturn409_GETReturnsOK()
    {
        var (sellerClient, _, _) = await RegisterParticipantAsync($"Seller-{Guid.NewGuid():N}");
        var (_, buyerSub, _) = await RegisterParticipantAsync($"Buyer-{Guid.NewGuid():N}");

        var artifactResponse = await sellerClient.PostAsJsonAsync("/artifacts", CreateSellArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await sellerClient.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = buyerSub,
            CallerIntent = "sell",
            CounterpartyIntent = "buy",
            EndsAt = DateTimeOffset.Parse("2020-01-01T00:00:00Z")
        });
        Assert.Equal(HttpStatusCode.Created, negResponse.StatusCode);
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var placeOfferResponse = await sellerClient.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 15000m, Currency = "USD" });
        Assert.Equal(HttpStatusCode.Conflict, placeOfferResponse.StatusCode);
        var placeContent = await placeOfferResponse.Content.ReadAsStringAsync();
        Assert.Contains("expired", placeContent, StringComparison.OrdinalIgnoreCase);

        var getResponse = await sellerClient.GetAsync($"/negotiations/{negotiation.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var expired = await getResponse.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.Equal("Expired", expired!.Status);
    }

    #endregion

    #region S14: Optional — Alternative Intent Pairs

    /// <summary>
    /// S14.1: Provide ↔ Consume (service offering).
    /// Expected: 201, intents are provide/consume.
    /// </summary>
    [Fact]
    public async Task S14_1_ProvideConsume_Returns201()
    {
        var (providerClient, _, _) = await RegisterParticipantAsync($"Provider-{Guid.NewGuid():N}");
        var (_, consumerSub, _) = await RegisterParticipantAsync($"Consumer-{Guid.NewGuid():N}");

        var artifactResponse = await providerClient.PostAsJsonAsync("/artifacts", new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>
            {
                new() { Name = "Consulting Service", Description = "Expert consulting" }
            },
            Intent = "provide"
        });
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var response = await providerClient.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = consumerSub,
            CallerIntent = "provide",
            CounterpartyIntent = "consume"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var negotiation = await response.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.Equal("provide", negotiation!.PartyAIntent);
        Assert.Equal("consume", negotiation.PartyBIntent);
    }

    /// <summary>
    /// S14: Verify buy↔sell and provide↔consume complementary pairs succeed.
    /// Intent pairs in this suite: buy↔sell and provide↔consume only.
    /// </summary>
    [Theory]
    [InlineData("buy", "sell")]
    [InlineData("sell", "buy")]
    [InlineData("provide", "consume")]
    [InlineData("consume", "provide")]
    public async Task S14_ComplementaryPairs_BuySellProvideConsume_Succeed(string callerIntent, string counterpartyIntent)
    {
        var (clientA, _, _) = await RegisterParticipantAsync($"A-{Guid.NewGuid():N}");
        var (_, subB, _) = await RegisterParticipantAsync($"B-{Guid.NewGuid():N}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto> { new() { Name = "Test Item" } },
            Intent = callerIntent
        });
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var response = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = callerIntent,
            CounterpartyIntent = counterpartyIntent
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var negotiation = await response.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.Equal(callerIntent, negotiation!.PartyAIntent);
        Assert.Equal(counterpartyIntent, negotiation.PartyBIntent);
    }

    #endregion
}
