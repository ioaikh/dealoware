using System.Net;
using System.Net.Http.Json;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dealoware.Api.Tests;

/// <summary>
/// Tests for Artifact endpoints with authentication (fail-closed).
/// Verifies authn ≠ authz: valid token doesn't mean access to all artifacts.
/// </summary>
public class ArtifactAuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ArtifactAuthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static CreateArtifactRequest CreateValidRequest() => new()
    {
        Entities = new List<CreateSubjectEntityDto>
        {
            new()
            {
                Name = "Test Entity",
                Description = "A test entity description"
            }
        },
        Intent = "sell"
    };

    private async Task<(HttpClient Client, RegisterResponse Participant)> RegisterParticipantAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register", new { DisplayName = "Test" });
        var participant = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        return (client, participant!);
    }

    [Fact]
    public async Task CreateArtifact_WithoutAuthorization_Returns401()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/artifacts", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateArtifact_WithApiKeyAuth_CreatesWithSubAsOwner()
    {
        var (client, participant) = await RegisterParticipantAsync();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant.ApiKey}");
        
        var request = CreateValidRequest();
        var response = await client.PostAsJsonAsync("/artifacts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var artifact = await response.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.NotNull(artifact);
        Assert.Equal(participant.Sub, artifact.OwnerParticipantId);
    }

    [Fact]
    public async Task CreateArtifact_WithBearerApiKey_CreatesWithSubAsOwner()
    {
        var (client, participant) = await RegisterParticipantAsync();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {participant.ApiKey}");
        
        var request = CreateValidRequest();
        var response = await client.PostAsJsonAsync("/artifacts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var artifact = await response.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.NotNull(artifact);
        Assert.Equal(participant.Sub, artifact.OwnerParticipantId);
    }

    [Fact]
    public async Task CreateArtifact_WithJwtAuth_CreatesWithSubAsOwner()
    {
        var (client, participant) = await RegisterParticipantAsync();
        
        var tokenResponse = await client.PostAsJsonAsync("/auth/token", 
            new TokenRequest { ApiKey = participant.ApiKey });
        var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token!.AccessToken}");
        
        var request = CreateValidRequest();
        var response = await client.PostAsJsonAsync("/artifacts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var artifact = await response.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.NotNull(artifact);
        Assert.Equal(participant.Sub, artifact.OwnerParticipantId);
    }

    [Fact]
    public async Task GetArtifact_WithoutAuthorization_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/artifacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetArtifact_OwnArtifact_WithAuth_Returns200()
    {
        var (client, participant) = await RegisterParticipantAsync();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant.ApiKey}");
        
        var createResponse = await client.PostAsJsonAsync("/artifacts", CreateValidRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var getResponse = await client.GetAsync($"/artifacts/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var artifact = await getResponse.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.Equal(created.Id, artifact!.Id);
        Assert.Equal(participant.Sub, artifact.OwnerParticipantId);
    }

    [Fact]
    public async Task GetArtifact_OtherOwnerArtifact_ValidAuth_Returns404_AuthnNotAuthz()
    {
        var (client1, participant1) = await RegisterParticipantAsync();
        var (client2, participant2) = await RegisterParticipantAsync();
        
        client1.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant1.ApiKey}");
        var createResponse = await client1.PostAsJsonAsync("/artifacts", CreateValidRequest());
        var artifact = await createResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        client2.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant2.ApiKey}");
        var getResponse = await client2.GetAsync($"/artifacts/{artifact!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task ListOwnArtifacts_WithoutAuthorization_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/artifacts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListOwnArtifacts_WithAuth_ReturnsOnlyOwnArtifacts()
    {
        var (client1, participant1) = await RegisterParticipantAsync();
        var (client2, participant2) = await RegisterParticipantAsync();
        
        client1.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant1.ApiKey}");
        await client1.PostAsJsonAsync("/artifacts", CreateValidRequest());
        await client1.PostAsJsonAsync("/artifacts", CreateValidRequest());
        
        client2.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant2.ApiKey}");
        await client2.PostAsJsonAsync("/artifacts", CreateValidRequest());

        var listResponse = await client1.GetAsync("/artifacts");
        var artifacts = await listResponse.Content.ReadFromJsonAsync<List<ArtifactResponse>>();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.True(artifacts!.Count >= 2);
        Assert.All(artifacts, a => Assert.Equal(participant1.Sub, a.OwnerParticipantId));
    }

    [Fact]
    public async Task ArtifactEndpoints_WithInvalidApiKey_Return401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "ApiKey dlw_invalid_keythatdoesnotexist");

        var createResponse = await client.PostAsJsonAsync("/artifacts", CreateValidRequest());
        var getResponse = await client.GetAsync($"/artifacts/{Guid.NewGuid()}");
        var listResponse = await client.GetAsync("/artifacts");

        Assert.Equal(HttpStatusCode.Unauthorized, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);
    }

    [Fact]
    public async Task ArtifactEndpoints_WithInvalidJwt_Return401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.jwt.token");

        var createResponse = await client.PostAsJsonAsync("/artifacts", CreateValidRequest());
        var getResponse = await client.GetAsync($"/artifacts/{Guid.NewGuid()}");
        var listResponse = await client.GetAsync("/artifacts");

        Assert.Equal(HttpStatusCode.Unauthorized, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);
    }

    [Fact]
    public async Task ArtifactEndpoints_WithRevokedApiKey_Return401()
    {
        var (client, participant) = await RegisterParticipantAsync();
        
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant.ApiKey}");
        var createResponse1 = await client.PostAsJsonAsync("/artifacts", CreateValidRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse1.StatusCode);

        var revokeRequest = new RevokeRequest { ApiKey = participant.ApiKey };
        await client.PostAsJsonAsync("/auth/revoke", revokeRequest);

        var createResponse2 = await client.PostAsJsonAsync("/artifacts", CreateValidRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, createResponse2.StatusCode);
    }

    [Fact]
    public async Task Health_WithoutAuth_Returns200()
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
}
