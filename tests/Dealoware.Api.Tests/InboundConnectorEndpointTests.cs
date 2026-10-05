using System.Net;
using System.Net.Http.Json;
using Dealoware.Api.Endpoints;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Negotiations.Dtos;

namespace Dealoware.Api.Tests;

/// <summary>
/// Tests for Inbound Connector endpoints.
/// 
/// Contract (locked): Inbound is a connector they call.
/// - Grok attaches a connector or a public MCP.
/// - Muse calls an API or MCP we expose.
/// - A dot uses account plugins.
/// These three stay apart — three separate local HTTP requests.
/// 
/// We do not run a client inside their bot and we do not poll them.
/// Create still returns an id. The other Participant can GET it.
/// The core does not call a model.
/// </summary>
[Collection("WebAppTests")]
public class InboundConnectorEndpointTests
{
    private readonly IsolatedWebApplicationFactory _factory;

    public InboundConnectorEndpointTests(IsolatedWebApplicationFactory factory)
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

    private async Task<(Guid NegotiationId, string PartyASub, string PartyBSub, HttpClient ClientA, HttpClient ClientB)> CreateNegotiationAsync(string suffix)
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"connector-a-{suffix}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"connector-b-{suffix}");

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
        var negotiation = await response.Content.ReadFromJsonAsync<NegotiationResponse>();

        return (negotiation!.Id, subA, subB, clientA, clientB);
    }

    #region Grok Connector Tests

    [Fact]
    public async Task GrokConnector_ValidRequest_Returns200()
    {
        var (negotiationId, partyASub, _, clientA, _) = await CreateNegotiationAsync($"grok-valid-{Guid.NewGuid()}");

        var request = new GrokConnectorRequest { NegotiationId = negotiationId };
        var response = await clientA.PostAsJsonAsync("/connectors/grok/initiate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GrokConnectorResponse>();
        Assert.NotNull(result);
        Assert.Equal(negotiationId, result.NegotiationId);
        Assert.Equal("grok", result.ConnectorType);
        Assert.Equal(partyASub, result.ParticipantId);
        Assert.Equal("Open", result.Status);
    }

    [Fact]
    public async Task GrokConnector_AsCounterparty_Returns200()
    {
        var (negotiationId, _, partyBSub, _, clientB) = await CreateNegotiationAsync($"grok-counterparty-{Guid.NewGuid()}");

        var request = new GrokConnectorRequest { NegotiationId = negotiationId };
        var response = await clientB.PostAsJsonAsync("/connectors/grok/initiate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GrokConnectorResponse>();
        Assert.NotNull(result);
        Assert.Equal(negotiationId, result.NegotiationId);
        Assert.Equal(partyBSub, result.ParticipantId);
    }

    [Fact]
    public async Task GrokConnector_NonParty_Returns404()
    {
        var (negotiationId, _, _, _, _) = await CreateNegotiationAsync($"grok-nonparty-{Guid.NewGuid()}");
        var (clientC, _, _) = await CreateAuthenticatedClientAsync($"grok-nonparty-c-{Guid.NewGuid()}");

        var request = new GrokConnectorRequest { NegotiationId = negotiationId };
        var response = await clientC.PostAsJsonAsync("/connectors/grok/initiate", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GrokConnector_InvalidNegotiationId_Returns404()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"grok-invalid-{Guid.NewGuid()}");

        var request = new GrokConnectorRequest { NegotiationId = Guid.NewGuid() };
        var response = await clientA.PostAsJsonAsync("/connectors/grok/initiate", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GrokConnector_EmptyNegotiationId_Returns400()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"grok-empty-{Guid.NewGuid()}");

        var request = new GrokConnectorRequest { NegotiationId = Guid.Empty };
        var response = await clientA.PostAsJsonAsync("/connectors/grok/initiate", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GrokConnector_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new GrokConnectorRequest { NegotiationId = Guid.NewGuid() };
        var response = await client.PostAsJsonAsync("/connectors/grok/initiate", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Muse Connector Tests

    [Fact]
    public async Task MuseConnector_GetStatus_Returns200()
    {
        var (negotiationId, _, _, clientA, _) = await CreateNegotiationAsync($"muse-status-{Guid.NewGuid()}");

        var request = new MuseConnectorRequest { NegotiationId = negotiationId, Operation = "get_status" };
        var response = await clientA.PostAsJsonAsync("/connectors/muse/call", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MuseConnectorResponse>();
        Assert.NotNull(result);
        Assert.Equal(negotiationId, result.NegotiationId);
        Assert.Equal("muse", result.ConnectorType);
        Assert.Equal("get_status", result.Operation);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task MuseConnector_ListOffers_Returns200()
    {
        var (negotiationId, _, _, clientA, _) = await CreateNegotiationAsync($"muse-offers-{Guid.NewGuid()}");

        var request = new MuseConnectorRequest { NegotiationId = negotiationId, Operation = "list_offers" };
        var response = await clientA.PostAsJsonAsync("/connectors/muse/call", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MuseConnectorResponse>();
        Assert.NotNull(result);
        Assert.Equal("list_offers", result.Operation);
    }

    [Fact]
    public async Task MuseConnector_CustomOperation_Returns200()
    {
        var (negotiationId, _, _, clientA, _) = await CreateNegotiationAsync($"muse-custom-{Guid.NewGuid()}");

        var request = new MuseConnectorRequest { NegotiationId = negotiationId, Operation = "custom_op" };
        var response = await clientA.PostAsJsonAsync("/connectors/muse/call", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MuseConnector_AsCounterparty_Returns200()
    {
        var (negotiationId, _, partyBSub, _, clientB) = await CreateNegotiationAsync($"muse-counterparty-{Guid.NewGuid()}");

        var request = new MuseConnectorRequest { NegotiationId = negotiationId, Operation = "get_status" };
        var response = await clientB.PostAsJsonAsync("/connectors/muse/call", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MuseConnector_NonParty_Returns404()
    {
        var (negotiationId, _, _, _, _) = await CreateNegotiationAsync($"muse-nonparty-{Guid.NewGuid()}");
        var (clientC, _, _) = await CreateAuthenticatedClientAsync($"muse-nonparty-c-{Guid.NewGuid()}");

        var request = new MuseConnectorRequest { NegotiationId = negotiationId, Operation = "get_status" };
        var response = await clientC.PostAsJsonAsync("/connectors/muse/call", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MuseConnector_MissingOperation_Returns400()
    {
        var (negotiationId, _, _, clientA, _) = await CreateNegotiationAsync($"muse-noop-{Guid.NewGuid()}");

        var request = new MuseConnectorRequest { NegotiationId = negotiationId, Operation = "" };
        var response = await clientA.PostAsJsonAsync("/connectors/muse/call", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MuseConnector_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new MuseConnectorRequest { NegotiationId = Guid.NewGuid(), Operation = "get_status" };
        var response = await client.PostAsJsonAsync("/connectors/muse/call", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Dot Connector Tests

    [Fact]
    public async Task DotConnector_ViewAction_Returns200()
    {
        var (negotiationId, partyASub, _, clientA, _) = await CreateNegotiationAsync($"dot-view-{Guid.NewGuid()}");

        var request = new DotConnectorRequest { NegotiationId = negotiationId, PluginAction = "view" };
        var response = await clientA.PostAsJsonAsync("/connectors/dot/plugin", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DotConnectorResponse>();
        Assert.NotNull(result);
        Assert.Equal(negotiationId, result.NegotiationId);
        Assert.Equal("dot", result.ConnectorType);
        Assert.Equal("view", result.PluginAction);
        Assert.Equal(partyASub, result.AccountId);
        Assert.NotNull(result.ActionResult);
    }

    [Fact]
    public async Task DotConnector_InteractAction_Returns200()
    {
        var (negotiationId, _, _, clientA, _) = await CreateNegotiationAsync($"dot-interact-{Guid.NewGuid()}");

        var request = new DotConnectorRequest { NegotiationId = negotiationId, PluginAction = "interact" };
        var response = await clientA.PostAsJsonAsync("/connectors/dot/plugin", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DotConnectorResponse>();
        Assert.NotNull(result);
        Assert.Equal("interact", result.PluginAction);
    }

    [Fact]
    public async Task DotConnector_CustomAction_Returns200()
    {
        var (negotiationId, _, _, clientA, _) = await CreateNegotiationAsync($"dot-custom-{Guid.NewGuid()}");

        var request = new DotConnectorRequest { NegotiationId = negotiationId, PluginAction = "custom_action" };
        var response = await clientA.PostAsJsonAsync("/connectors/dot/plugin", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DotConnector_AsCounterparty_Returns200()
    {
        var (negotiationId, _, partyBSub, _, clientB) = await CreateNegotiationAsync($"dot-counterparty-{Guid.NewGuid()}");

        var request = new DotConnectorRequest { NegotiationId = negotiationId, PluginAction = "view" };
        var response = await clientB.PostAsJsonAsync("/connectors/dot/plugin", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DotConnectorResponse>();
        Assert.NotNull(result);
        Assert.Equal(partyBSub, result.AccountId);
    }

    [Fact]
    public async Task DotConnector_NonParty_Returns404()
    {
        var (negotiationId, _, _, _, _) = await CreateNegotiationAsync($"dot-nonparty-{Guid.NewGuid()}");
        var (clientC, _, _) = await CreateAuthenticatedClientAsync($"dot-nonparty-c-{Guid.NewGuid()}");

        var request = new DotConnectorRequest { NegotiationId = negotiationId, PluginAction = "view" };
        var response = await clientC.PostAsJsonAsync("/connectors/dot/plugin", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DotConnector_MissingPluginAction_Returns400()
    {
        var (negotiationId, _, _, clientA, _) = await CreateNegotiationAsync($"dot-noaction-{Guid.NewGuid()}");

        var request = new DotConnectorRequest { NegotiationId = negotiationId, PluginAction = "" };
        var response = await clientA.PostAsJsonAsync("/connectors/dot/plugin", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DotConnector_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new DotConnectorRequest { NegotiationId = Guid.NewGuid(), PluginAction = "view" };
        var response = await client.PostAsJsonAsync("/connectors/dot/plugin", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Create Negotiation → ID Return → Counterparty GET Tests

    [Fact]
    public async Task CreateNegotiation_ReturnsId_CounterpartyCanGet()
    {
        var (clientA, subA, _) = await CreateAuthenticatedClientAsync($"create-get-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"create-get-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negotiationRequest = new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        };

        var createResponse = await clientA.PostAsJsonAsync("/negotiations", negotiationRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var negotiation = await createResponse.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.NotNull(negotiation);
        Assert.NotEqual(Guid.Empty, negotiation.Id);

        var getResponse = await clientB.GetAsync($"/negotiations/{negotiation.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<NegotiationResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(negotiation.Id, fetched.Id);
        Assert.Equal(subA, fetched.PartyAParticipantId);
        Assert.Equal(subB, fetched.PartyBParticipantId);
    }

    [Fact]
    public async Task CreateNegotiation_CounterpartyGetsCorrectIntents()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"intent-a-{Guid.NewGuid()}");
        var (clientB, subB, _) = await CreateAuthenticatedClientAsync($"intent-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("provide"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negotiationRequest = new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "provide",
            CounterpartyIntent = "consume"
        };

        var createResponse = await clientA.PostAsJsonAsync("/negotiations", negotiationRequest);
        var negotiation = await createResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var getResponse = await clientB.GetAsync($"/negotiations/{negotiation!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        Assert.Equal("provide", fetched!.PartyAIntent);
        Assert.Equal("consume", fetched.PartyBIntent);
    }

    [Fact]
    public async Task CreateNegotiation_NonParty_CannotGet()
    {
        var (clientA, _, _) = await CreateAuthenticatedClientAsync($"noget-a-{Guid.NewGuid()}");
        var (_, subB, _) = await CreateAuthenticatedClientAsync($"noget-b-{Guid.NewGuid()}");
        var (clientC, _, _) = await CreateAuthenticatedClientAsync($"noget-c-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", CreateArtifactRequest("sell"));
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negotiationRequest = new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        };

        var createResponse = await clientA.PostAsJsonAsync("/negotiations", negotiationRequest);
        var negotiation = await createResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var getResponse = await clientC.GetAsync($"/negotiations/{negotiation!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    #endregion

    #region Three Distinct Routes Verification

    [Fact]
    public async Task ThreeConnectorRoutes_AreDistinct_AndAllWork()
    {
        var (negotiationId, _, _, clientA, _) = await CreateNegotiationAsync($"distinct-routes-{Guid.NewGuid()}");

        var grokResponse = await clientA.PostAsJsonAsync("/connectors/grok/initiate",
            new GrokConnectorRequest { NegotiationId = negotiationId });
        var museResponse = await clientA.PostAsJsonAsync("/connectors/muse/call",
            new MuseConnectorRequest { NegotiationId = negotiationId, Operation = "get_status" });
        var dotResponse = await clientA.PostAsJsonAsync("/connectors/dot/plugin",
            new DotConnectorRequest { NegotiationId = negotiationId, PluginAction = "view" });

        Assert.Equal(HttpStatusCode.OK, grokResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, museResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, dotResponse.StatusCode);

        var grokResult = await grokResponse.Content.ReadFromJsonAsync<GrokConnectorResponse>();
        var museResult = await museResponse.Content.ReadFromJsonAsync<MuseConnectorResponse>();
        var dotResult = await dotResponse.Content.ReadFromJsonAsync<DotConnectorResponse>();

        Assert.Equal("grok", grokResult!.ConnectorType);
        Assert.Equal("muse", museResult!.ConnectorType);
        Assert.Equal("dot", dotResult!.ConnectorType);
    }

    #endregion
}
