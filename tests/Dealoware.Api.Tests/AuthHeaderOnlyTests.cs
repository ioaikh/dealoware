using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dealoware.Api.Tests;

/// <summary>
/// Tests to verify Authorization header-only authentication.
/// Tokens in query strings or request bodies must NOT work.
/// This is a security requirement to prevent token leakage via logs/referrers.
/// </summary>
public class AuthHeaderOnlyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthHeaderOnlyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static CreateArtifactRequest CreateValidRequest() => new()
    {
        Entities = new List<CreateSubjectEntityDto>
        {
            new() { Name = "Test", Description = "Test" }
        },
        Intent = "sell"
    };

    private async Task<(HttpClient Client, RegisterResponse Participant)> RegisterParticipantAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register", new { });
        var participant = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        return (client, participant!);
    }

    [Fact]
    public async Task CreateArtifact_TokenInQueryString_Returns401()
    {
        var (client, participant) = await RegisterParticipantAsync();
        
        var request = CreateValidRequest();
        var response = await client.PostAsJsonAsync($"/artifacts?token={participant.ApiKey}", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateArtifact_TokenInQueryString_AccessToken_Returns401()
    {
        var (client, participant) = await RegisterParticipantAsync();
        
        var tokenResponse = await client.PostAsJsonAsync("/auth/token", 
            new TokenRequest { ApiKey = participant.ApiKey });
        var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        
        var request = CreateValidRequest();
        var response = await client.PostAsJsonAsync($"/artifacts?access_token={token!.AccessToken}", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateArtifact_TokenInQueryString_Authorization_Returns401()
    {
        var (client, participant) = await RegisterParticipantAsync();
        
        var request = CreateValidRequest();
        var response = await client.PostAsJsonAsync($"/artifacts?authorization={participant.ApiKey}", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetArtifact_TokenInQueryString_Returns401()
    {
        var (client, participant) = await RegisterParticipantAsync();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant.ApiKey}");
        
        var createResponse = await client.PostAsJsonAsync("/artifacts", CreateValidRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        client.DefaultRequestHeaders.Remove("Authorization");
        var response = await client.GetAsync($"/artifacts/{created!.Id}?token={participant.ApiKey}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListArtifacts_TokenInQueryString_Returns401()
    {
        var (client, participant) = await RegisterParticipantAsync();

        var response = await client.GetAsync($"/artifacts?token={participant.ApiKey}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateArtifact_TokenInBody_Returns401()
    {
        var (client, participant) = await RegisterParticipantAsync();

        var requestWithToken = new
        {
            Entities = new[]
            {
                new { Name = "Test", Description = "Test" }
            },
            Intent = "sell",
            Token = participant.ApiKey,
            Authorization = participant.ApiKey,
            AccessToken = participant.ApiKey
        };

        var response = await client.PostAsJsonAsync("/artifacts", requestWithToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationHeader_Required_NotQueryParam()
    {
        var (client, participant) = await RegisterParticipantAsync();

        var response1 = await client.GetAsync($"/artifacts?Authorization=ApiKey%20{participant.ApiKey}");
        Assert.Equal(HttpStatusCode.Unauthorized, response1.StatusCode);

        var response2 = await client.GetAsync($"/artifacts?Bearer={participant.ApiKey}");
        Assert.Equal(HttpStatusCode.Unauthorized, response2.StatusCode);

        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant.ApiKey}");
        var response3 = await client.GetAsync("/artifacts");
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
    }

    [Fact]
    public async Task CreateArtifact_HeaderAuth_Works_QueryAuth_Ignored()
    {
        var (client1, participant1) = await RegisterParticipantAsync();
        var (_, participant2) = await RegisterParticipantAsync();

        client1.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant1.ApiKey}");
        var response = await client1.PostAsJsonAsync(
            $"/artifacts?token={participant2.ApiKey}",
            CreateValidRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var artifact = await response.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.Equal(participant1.Sub, artifact!.OwnerParticipantId);
    }
}
