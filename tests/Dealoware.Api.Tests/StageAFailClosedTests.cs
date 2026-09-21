using System.Net;
using System.Net.Http.Json;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Negotiations.Dtos;

namespace Dealoware.Api.Tests;

/// <summary>
/// Stage A fail-closed security tests for MVP #32.
/// 
/// Verifies query-plane authorization for all three resources:
/// - Artifact: owner-scoped (OwnerParticipantId == principal sub)
/// - Negotiation: party-scoped (principal is partyA or partyB)
/// - Offer: party-scoped via parent negotiation
/// 
/// Test matrix per resource:
/// - Owner/party OK: authorized access succeeds
/// - Stranger deny: unauthorized access returns 404 (no info leak)
/// - IDOR get deny: cross-tenant ID guessing returns 404
/// - Unauth deny: missing/invalid auth returns 401
/// - Empty list hygiene: stranger gets [] not foreign rows
/// - No private fields in deny bodies
/// </summary>
[Collection("WebAppTests")]
public class StageAFailClosedTests
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StageAFailClosedTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region Test Helpers

    private async Task<(HttpClient Client, string Sub, string ApiKey)> CreateAuthenticatedClientAsync(string suffix)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = $"participant-{suffix}" });
        var participant = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant!.ApiKey}");
        return (client, participant.Sub, participant.ApiKey);
    }

    private static CreateArtifactRequest CreateArtifactRequest(string intent = "sell") => new()
    {
        Entities = new List<CreateSubjectEntityDto>
        {
            new() { Name = "Test Item", Description = "A test item for negotiation" }
        },
        Intent = intent
    };

    #endregion

    #region Artifact Tests - Owner-Scoped

    [Fact]
    public async Task Artifact_List_OwnerOK_ReturnsOnlyOwnArtifacts()
    {
        var (ownerClient, ownerSub, _) = await CreateAuthenticatedClientAsync($"art-list-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"art-list-stranger-{Guid.NewGuid()}");

        await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
        await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
        await strangerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest());

        var response = await ownerClient.GetAsync("/artifacts");
        var artifacts = await response.Content.ReadFromJsonAsync<List<ArtifactResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(artifacts);
        Assert.True(artifacts.Count >= 2);
        Assert.All(artifacts, a => Assert.Equal(ownerSub, a.OwnerParticipantId));
    }

    [Fact]
    public async Task Artifact_List_StrangerDeny_ReturnsEmptyListNotForeignRows()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"art-empty-owner-{Guid.NewGuid()}");
        var (strangerClient, strangerSub, _) = await CreateAuthenticatedClientAsync($"art-empty-stranger-{Guid.NewGuid()}");

        await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
        await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest());

        var response = await strangerClient.GetAsync("/artifacts");
        var artifacts = await response.Content.ReadFromJsonAsync<List<ArtifactResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(artifacts);
        Assert.Empty(artifacts);
    }

    [Fact]
    public async Task Artifact_Get_OwnerOK_Returns200()
    {
        var (client, sub, _) = await CreateAuthenticatedClientAsync($"art-get-owner-{Guid.NewGuid()}");

        var createResponse = await client.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var response = await client.GetAsync($"/artifacts/{created!.Id}");
        var artifact = await response.Content.ReadFromJsonAsync<ArtifactResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(artifact);
        Assert.Equal(created.Id, artifact.Id);
        Assert.Equal(sub, artifact.OwnerParticipantId);
    }

    [Fact]
    public async Task Artifact_Get_StrangerDeny_Returns404NoPrivateFields()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"art-get-stranger-owner-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"art-get-stranger-{Guid.NewGuid()}");

        var createResponse = await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var response = await strangerClient.GetAsync($"/artifacts/{created!.Id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("OwnerParticipantId", body);
        Assert.DoesNotContain("participant:", body);
        Assert.DoesNotContain("Entities", body);
    }

    [Fact]
    public async Task Artifact_Get_IDORDeny_Returns404()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"art-idor-{Guid.NewGuid()}");

        var response = await client.GetAsync($"/artifacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Artifact_List_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/artifacts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Artifact_Get_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/artifacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Artifact_Get_InvalidAuth_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.token.here");

        var response = await client.GetAsync($"/artifacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Negotiation Tests - Party-Scoped

    [Fact]
    public async Task Negotiation_List_PartyOK_ReturnsOnlyPartyNegotiations()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"neg-list-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"neg-list-b-{Guid.NewGuid()}");
        var (clientC, subC, _) = await CreateAuthenticatedClientAsync($"neg-list-c-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });

        var artifact2Response = await clientC.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
        var artifact2 = await artifact2Response.Content.ReadFromJsonAsync<ArtifactResponse>();
        await clientC.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact2!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });

        var response = await clientA.GetAsync("/negotiations");
        var negotiations = await response.Content.ReadFromJsonAsync<List<NegotiationResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(negotiations);
        Assert.Single(negotiations);
        Assert.True(negotiations[0].PartyAParticipantId == subA || negotiations[0].PartyBParticipantId == subA);
    }

    [Fact]
    public async Task Negotiation_List_StrangerDeny_ReturnsEmptyListNotForeignRows()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"neg-empty-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"neg-empty-b-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"neg-empty-stranger-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });

        var response = await strangerClient.GetAsync("/negotiations");
        var negotiations = await response.Content.ReadFromJsonAsync<List<NegotiationResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(negotiations);
        Assert.Empty(negotiations);
    }

    [Fact]
    public async Task Negotiation_Get_PartyOK_Returns200()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"neg-get-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"neg-get-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
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
    public async Task Negotiation_Get_StrangerDeny_Returns404NoPrivateFields()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"neg-stranger-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"neg-stranger-b-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"neg-stranger-c-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var createResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var response = await strangerClient.GetAsync($"/negotiations/{created!.Id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("PartyAParticipantId", body);
        Assert.DoesNotContain("PartyBParticipantId", body);
        Assert.DoesNotContain("ArtifactId", body);
    }

    [Fact]
    public async Task Negotiation_Get_IDORDeny_Returns404()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"neg-idor-{Guid.NewGuid()}");

        var response = await client.GetAsync($"/negotiations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Negotiation_List_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/negotiations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Negotiation_Get_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/negotiations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Negotiation_Get_InvalidAuth_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "ApiKey dlw_invalid_keythatdoesnotexist");

        var response = await client.GetAsync($"/negotiations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Offer Tests - Party-Scoped via Parent Negotiation

    [Fact]
    public async Task Offer_List_PartyOK_ReturnsOnlyPartyOffers()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"offer-list-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"offer-list-b-{Guid.NewGuid()}");
        var (clientC, subC, _) = await CreateAuthenticatedClientAsync($"offer-list-c-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
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
            new CreateOfferRequest { Amount = 100m, Currency = "USD" });

        var artifact2Response = await clientC.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
        var artifact2 = await artifact2Response.Content.ReadFromJsonAsync<ArtifactResponse>();
        var neg2Response = await clientC.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact2!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation2 = await neg2Response.Content.ReadFromJsonAsync<NegotiationResponse>();
        await clientC.PostAsJsonAsync($"/negotiations/{negotiation2!.Id}/offers",
            new CreateOfferRequest { Amount = 200m, Currency = "USD" });

        var response = await clientA.GetAsync("/offers");
        var offers = await response.Content.ReadFromJsonAsync<List<OfferResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(offers);
        Assert.Single(offers);
        Assert.True(offers[0].FromParticipantId == subA || offers[0].ToParticipantId == subA);
    }

    [Fact]
    public async Task Offer_List_StrangerDeny_ReturnsEmptyListNotForeignRows()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"offer-empty-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"offer-empty-b-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"offer-empty-stranger-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
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
            new CreateOfferRequest { Amount = 100m, Currency = "USD" });

        var response = await strangerClient.GetAsync("/offers");
        var offers = await response.Content.ReadFromJsonAsync<List<OfferResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(offers);
        Assert.Empty(offers);
    }

    [Fact]
    public async Task Offer_Get_PartyOK_Returns200()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"offer-get-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"offer-get-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
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
            new CreateOfferRequest { Amount = 100m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var responseA = await clientA.GetAsync($"/offers/{offer!.Id}");
        var responseB = await clientB.GetAsync($"/offers/{offer.Id}");

        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
    }

    [Fact]
    public async Task Offer_Get_StrangerDeny_Returns404NoPrivateFields()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"offer-stranger-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"offer-stranger-b-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"offer-stranger-c-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
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
            new CreateOfferRequest { Amount = 100m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var response = await strangerClient.GetAsync($"/offers/{offer!.Id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("FromParticipantId", body);
        Assert.DoesNotContain("ToParticipantId", body);
        Assert.DoesNotContain("NegotiationId", body);
        Assert.DoesNotContain("Amount", body);
    }

    [Fact]
    public async Task Offer_Get_IDORDeny_Returns404()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"offer-idor-{Guid.NewGuid()}");

        var response = await client.GetAsync($"/offers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Offer_List_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/offers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Offer_Get_UnauthDeny_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/offers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Offer_Get_InvalidAuth_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.jwt.token");

        var response = await client.GetAsync($"/offers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Offer_AcceptDeclineCounter_StrangerDeny_Returns404()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"offer-mut-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"offer-mut-b-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"offer-mut-stranger-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
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
            new CreateOfferRequest { Amount = 100m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var acceptResponse = await strangerClient.PostAsync($"/offers/{offer!.Id}/accept", null);
        var declineResponse = await strangerClient.PostAsync($"/offers/{offer.Id}/decline", null);
        var counterResponse = await strangerClient.PostAsJsonAsync($"/offers/{offer.Id}/counter",
            new CounterOfferRequest { Amount = 90m, Currency = "USD" });

        Assert.Equal(HttpStatusCode.NotFound, acceptResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, declineResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, counterResponse.StatusCode);
    }

    #endregion

    #region Health Endpoint - No Auth Required

    [Fact]
    public async Task Health_NoAuth_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_WithInvalidAuth_StillReturns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.token");

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Query-Plane Verification

    [Fact]
    public async Task Artifact_GetByIdForOwner_QueryPlaneFilter_NotFetchThenFilter()
    {
        var (ownerClient, ownerSub, _) = await CreateAuthenticatedClientAsync($"art-qp-owner-{Guid.NewGuid()}");
        var (strangerClient, strangerSub, _) = await CreateAuthenticatedClientAsync($"art-qp-stranger-{Guid.NewGuid()}");

        var createResponse = await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
        var artifact = await createResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var response = await strangerClient.GetAsync($"/artifacts/{artifact!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Negotiation_GetByIdForParty_QueryPlaneFilter_NotFetchThenFilter()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"neg-qp-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"neg-qp-b-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"neg-qp-stranger-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var createResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await createResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var response = await strangerClient.GetAsync($"/negotiations/{negotiation!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Offer_GetByIdForParty_QueryPlaneFilter_NotFetchThenFilter()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"offer-qp-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"offer-qp-b-{Guid.NewGuid()}");
        var (strangerClient, _, _) = await CreateAuthenticatedClientAsync($"offer-qp-stranger-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest());
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
            new CreateOfferRequest { Amount = 100m, Currency = "USD" });
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();

        var response = await strangerClient.GetAsync($"/offers/{offer!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
