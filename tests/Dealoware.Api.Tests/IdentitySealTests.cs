using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Negotiations.Dtos;

namespace Dealoware.Api.Tests;

/// <summary>
/// Identity-seal leak-proof tests (Issue #7).
/// 
/// Verifies that Negotiation/Offer/public Participant responses expose opaque IDs only
/// and contain no contact PII (email, phone, address, etc.).
/// 
/// Accept/Decline/Counter/Close responses return state only — no contact release.
/// This is a PoC stub; contact exchange on accept is MVP (P7/A9).
/// </summary>
[Collection("WebAppTests")]
public class IdentitySealTests
{
    private readonly IsolatedWebApplicationFactory _factory;

    private static readonly string[] ForbiddenFieldNames = new[]
    {
        "email", "phone", "telephone", "mobile", "address", "street",
        "city", "zip", "zipcode", "postalcode", "postal_code",
        "contact", "contactinfo", "contact_info", "contactdetails",
        "firstname", "first_name", "lastname", "last_name", "fullname", "full_name",
        "ssn", "socialsecurity", "social_security", "taxid", "tax_id"
    };

    private static readonly Regex EmailPattern = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex PhonePattern = new(
        @"(?<![a-fA-F0-9-])(\+?1?[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}(?![a-fA-F0-9-])",
        RegexOptions.Compiled);
    
    private static readonly Regex GuidPattern = new(
        @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
        RegexOptions.Compiled);

    public IdentitySealTests(IsolatedWebApplicationFactory factory)
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

    private static void AssertNoContactPiiInJson(string json, string context)
    {
        var lowerJson = json.ToLowerInvariant();

        foreach (var forbidden in ForbiddenFieldNames)
        {
            Assert.DoesNotContain($"\"{forbidden}\"", lowerJson, StringComparison.OrdinalIgnoreCase);
        }

        Assert.False(EmailPattern.IsMatch(json),
            $"Email pattern detected in {context}: {json}");

        var jsonWithoutGuids = GuidPattern.Replace(json, "GUID_PLACEHOLDER");
        Assert.False(PhonePattern.IsMatch(jsonWithoutGuids),
            $"Phone pattern detected in {context}: {json}");
    }

    private static void AssertIdentitySealedFlag(string json, string context)
    {
        Assert.Contains("\"identitySealed\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"identitySealed\":true", json.Replace(" ", ""), StringComparison.OrdinalIgnoreCase);
    }

    #region Create Negotiation - Leak-Proof Tests

    [Fact]
    public async Task CreateNegotiation_Response_NoContactPii_IdentitySealed()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"seal-create-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"seal-create-b-{Guid.NewGuid()}");

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

        var json = await response.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json, "CreateNegotiation response");
        AssertIdentitySealedFlag(json, "CreateNegotiation response");

        var negotiation = JsonSerializer.Deserialize<NegotiationResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.True(negotiation!.IdentitySealed);
        Assert.StartsWith("participant:", negotiation.PartyAParticipantId);
        Assert.StartsWith("participant:", negotiation.PartyBParticipantId);
    }

    #endregion

    #region Get Negotiation - Leak-Proof Tests (Both Parties)

    [Fact]
    public async Task GetNegotiation_BothParties_NoContactPii_IdentitySealed()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"seal-get-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"seal-get-b-{Guid.NewGuid()}");

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
        var jsonA = await responseA.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(jsonA, "GetNegotiation as PartyA");
        AssertIdentitySealedFlag(jsonA, "GetNegotiation as PartyA");

        var responseB = await clientB.GetAsync($"/negotiations/{created.Id}");
        var jsonB = await responseB.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(jsonB, "GetNegotiation as PartyB");
        AssertIdentitySealedFlag(jsonB, "GetNegotiation as PartyB");
    }

    #endregion

    #region Place Offer - Leak-Proof Tests

    [Fact]
    public async Task PlaceOffer_Response_NoContactPii_IdentitySealed()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"seal-offer-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"seal-offer-b-{Guid.NewGuid()}");

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
            new CreateOfferRequest { Amount = 1000m, Currency = "USD", Terms = "Cash payment" });

        Assert.Equal(HttpStatusCode.Created, offerResponse.StatusCode);
        var json = await offerResponse.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json, "PlaceOffer response");
        AssertIdentitySealedFlag(json, "PlaceOffer response");

        var offer = JsonSerializer.Deserialize<OfferResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.True(offer!.IdentitySealed);
    }

    [Fact]
    public async Task GetNegotiationWithOffers_NoContactPii_AllIdentitySealed()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"seal-offers-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"seal-offers-b-{Guid.NewGuid()}");

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

        var getResponse = await clientA.GetAsync($"/negotiations/{negotiation.Id}");
        var json = await getResponse.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json, "GetNegotiation with offers");

        var neg = JsonSerializer.Deserialize<NegotiationResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.True(neg!.IdentitySealed);
        Assert.NotNull(neg.Offers);
        Assert.All(neg.Offers, o => Assert.True(o.IdentitySealed));
    }

    #endregion

    #region Accept Offer - Leak-Proof Tests (Critical: No Contact Release)

    [Fact]
    public async Task AcceptOffer_Response_NoContactPii_StateOnly_IdentitySealed()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"seal-accept-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"seal-accept-b-{Guid.NewGuid()}");

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
            new CreateOfferRequest { Amount = 1000m, Currency = "USD", Terms = "Standard terms" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var acceptResponse = await clientB.PostAsync($"/offers/{offer!.Id}/accept", null);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        var json = await acceptResponse.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json, "AcceptOffer response");
        AssertIdentitySealedFlag(json, "AcceptOffer response");

        var accepted = JsonSerializer.Deserialize<OfferResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("Accepted", accepted!.Status);
        Assert.True(accepted.IdentitySealed);
    }

    [Fact]
    public async Task AcceptOffer_SubsequentGet_NoContactPii_IdentitySealed()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"seal-accept-get-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"seal-accept-get-b-{Guid.NewGuid()}");

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

        var getResponseA = await clientA.GetAsync($"/negotiations/{negotiation.Id}");
        var jsonA = await getResponseA.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(jsonA, "GET after accept as PartyA");
        AssertIdentitySealedFlag(jsonA, "GET after accept as PartyA");

        var getResponseB = await clientB.GetAsync($"/negotiations/{negotiation.Id}");
        var jsonB = await getResponseB.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(jsonB, "GET after accept as PartyB");
        AssertIdentitySealedFlag(jsonB, "GET after accept as PartyB");
    }

    #endregion

    #region Decline Offer - Leak-Proof Tests

    [Fact]
    public async Task DeclineOffer_Response_NoContactPii_StateOnly_IdentitySealed()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"seal-decline-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"seal-decline-b-{Guid.NewGuid()}");

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

        var json = await declineResponse.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json, "DeclineOffer response");
        AssertIdentitySealedFlag(json, "DeclineOffer response");

        var declined = JsonSerializer.Deserialize<OfferResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("Declined", declined!.Status);
        Assert.True(declined.IdentitySealed);
    }

    #endregion

    #region Counter Offer - Leak-Proof Tests

    [Fact]
    public async Task CounterOffer_Response_NoContactPii_StateOnly_IdentitySealed()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"seal-counter-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"seal-counter-b-{Guid.NewGuid()}");

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

        var counterResponse = await clientB.PostAsJsonAsync($"/offers/{offer!.Id}/counter",
            new CounterOfferRequest { Amount = 800m, Currency = "USD", Terms = "Counter terms" });

        Assert.Equal(HttpStatusCode.Created, counterResponse.StatusCode);
        var json = await counterResponse.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json, "CounterOffer response");
        AssertIdentitySealedFlag(json, "CounterOffer response");

        var counter = JsonSerializer.Deserialize<OfferResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("Open", counter!.Status);
        Assert.True(counter.IdentitySealed);
    }

    #endregion

    #region Close Negotiation - Leak-Proof Tests

    [Fact]
    public async Task CloseNegotiation_Response_NoContactPii_StateOnly_IdentitySealed()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"seal-close-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"seal-close-b-{Guid.NewGuid()}");

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

        var closeResponse = await clientA.PostAsync($"/negotiations/{negotiation.Id}/close", null);
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);

        var json = await closeResponse.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json, "CloseNegotiation response");
        AssertIdentitySealedFlag(json, "CloseNegotiation response");

        var closed = JsonSerializer.Deserialize<NegotiationResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("Closed", closed!.Status);
        Assert.True(closed.IdentitySealed);
        if (closed.Offers != null)
        {
            Assert.All(closed.Offers, o => Assert.True(o.IdentitySealed));
        }
    }

    #endregion

    #region Full Negotiation Flow - End-to-End Leak-Proof Test

    [Fact]
    public async Task FullNegotiationFlow_NoContactPiiAtAnyStep()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"seal-e2e-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"seal-e2e-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var json1 = await negResponse.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json1, "Step 1: Create negotiation");
        AssertIdentitySealedFlag(json1, "Step 1: Create negotiation");
        var negotiation = JsonSerializer.Deserialize<NegotiationResponse>(json1,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var offer1Response = await clientA.PostAsJsonAsync($"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD", Terms = "Initial offer" });
        var json2 = await offer1Response.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json2, "Step 2: Place first offer");
        AssertIdentitySealedFlag(json2, "Step 2: Place first offer");
        var offer1 = JsonSerializer.Deserialize<OfferResponse>(json2,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var counterResponse = await clientB.PostAsJsonAsync($"/offers/{offer1!.Id}/counter",
            new CounterOfferRequest { Amount = 800m, Currency = "USD", Terms = "Counter proposal" });
        var json3 = await counterResponse.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json3, "Step 3: Counter offer");
        AssertIdentitySealedFlag(json3, "Step 3: Counter offer");
        var counter = JsonSerializer.Deserialize<OfferResponse>(json3,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var acceptResponse = await clientA.PostAsync($"/offers/{counter!.Id}/accept", null);
        var json4 = await acceptResponse.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json4, "Step 4: Accept counter offer");
        AssertIdentitySealedFlag(json4, "Step 4: Accept counter offer");

        var finalGetA = await clientA.GetAsync($"/negotiations/{negotiation.Id}");
        var json5a = await finalGetA.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json5a, "Step 5a: Final GET as PartyA");
        AssertIdentitySealedFlag(json5a, "Step 5a: Final GET as PartyA");

        var finalGetB = await clientB.GetAsync($"/negotiations/{negotiation.Id}");
        var json5b = await finalGetB.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json5b, "Step 5b: Final GET as PartyB");
        AssertIdentitySealedFlag(json5b, "Step 5b: Final GET as PartyB");

        var finalNeg = JsonSerializer.Deserialize<NegotiationResponse>(json5a,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.True(finalNeg!.IdentitySealed);
        Assert.NotNull(finalNeg.Offers);
        Assert.Contains(finalNeg.Offers, o => o.Status == "Accepted");
        Assert.All(finalNeg.Offers, o => Assert.True(o.IdentitySealed));
    }

    #endregion

    #region Terms Field Validation - No PII Smuggling

    [Fact]
    public async Task OfferTerms_NoContactInfoSmuggled()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"seal-terms-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"seal-terms-b-{Guid.NewGuid()}");

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
            new CreateOfferRequest
            {
                Amount = 1000m,
                Currency = "USD",
                Terms = "Delivery within 7 days. Payment on receipt."
            });

        var json = await offerResponse.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json, "Offer with valid terms");

        var getResponse = await clientA.GetAsync($"/negotiations/{negotiation.Id}");
        var getJson = await getResponse.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(getJson, "GET negotiation with valid terms");
    }

    #endregion

    #region Participant ID Format Verification

    [Fact]
    public async Task ParticipantIds_AreOpaqueFormat()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"seal-opaque-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"seal-opaque-b-{Guid.NewGuid()}");

        Assert.StartsWith("participant:", subA);
        Assert.StartsWith("participant:", subB);
        Assert.True(Guid.TryParse(subA.Replace("participant:", ""), out _));
        Assert.True(Guid.TryParse(subB.Replace("participant:", ""), out _));

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

        Assert.Equal(subA, negotiation!.PartyAParticipantId);
        Assert.Equal(subB, negotiation.PartyBParticipantId);

        var offerResponse = await clientA.PostAsJsonAsync($"/negotiations/{negotiation.Id}/offers",
            new CreateOfferRequest { Amount = 1000m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        Assert.Equal(subA, offer!.FromParticipantId);
        Assert.Equal(subB, offer.ToParticipantId);
    }

    #endregion

    #region Health Endpoint - No Auth Required

    [Fact]
    public async Task Health_NoAuthRequired_NoContactPii()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        AssertNoContactPiiInJson(json, "Health endpoint");
    }

    #endregion
}
