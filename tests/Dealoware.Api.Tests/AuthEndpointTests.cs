using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Application.Auth.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dealoware.Api.Tests;

[Collection("WebAppTests")]
public class AuthEndpointTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ReturnsCreatedWithSubAndApiKey()
    {
        var client = _factory.CreateClient();
        var request = new RegisterRequest { DisplayName = "Test Participant" };

        var response = await client.PostAsJsonAsync("/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.StartsWith("participant:", result.Sub);
        Assert.StartsWith("dlw_", result.ApiKey);
        Assert.Equal(8, result.ApiKeyPrefix.Length);
        Assert.Equal("Test Participant", result.DisplayName);
    }

    [Fact]
    public async Task Register_WithoutDisplayName_StillSucceeds()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/register", new { });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.StartsWith("participant:", result.Sub);
        Assert.Null(result.DisplayName);
    }

    [Fact]
    public async Task Register_SubIsUniquePerRegistration()
    {
        var client = _factory.CreateClient();

        var response1 = await client.PostAsJsonAsync("/auth/register", new { });
        var response2 = await client.PostAsJsonAsync("/auth/register", new { });

        var result1 = await response1.Content.ReadFromJsonAsync<RegisterResponse>();
        var result2 = await response2.Content.ReadFromJsonAsync<RegisterResponse>();

        Assert.NotEqual(result1!.Sub, result2!.Sub);
        Assert.NotEqual(result1.ApiKey, result2.ApiKey);
    }

    [Fact]
    public async Task IssueToken_WithValidApiKey_ReturnsJwt()
    {
        var client = _factory.CreateClient();
        
        var registerResponse = await client.PostAsJsonAsync("/auth/register", new { });
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        var tokenRequest = new TokenRequest { ApiKey = registered!.ApiKey };
        var tokenResponse = await client.PostAsJsonAsync("/auth/token", tokenRequest);

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        
        var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);
        Assert.NotEmpty(token.AccessToken);
        Assert.Equal("Bearer", token.TokenType);
        Assert.Equal(registered.Sub, token.Sub);
        Assert.True(token.ExpiresIn > 0);
    }

    [Fact]
    public async Task IssueToken_WithInvalidApiKey_Returns401()
    {
        var client = _factory.CreateClient();

        var tokenRequest = new TokenRequest { ApiKey = "dlw_invalid_key123456" };
        var response = await client.PostAsJsonAsync("/auth/token", tokenRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IssueToken_WithEmptyApiKey_Returns401()
    {
        var client = _factory.CreateClient();

        var tokenRequest = new TokenRequest { ApiKey = "" };
        var response = await client.PostAsJsonAsync("/auth/token", tokenRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_ApiKey_PreventsSubsequentAuth()
    {
        var client = _factory.CreateClient();
        
        var registerResponse = await client.PostAsJsonAsync("/auth/register", new { });
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {registered!.ApiKey}");
        
        var revokeRequest = new RevokeRequest { ApiKey = registered.ApiKey };
        var revokeResponse = await client.PostAsJsonAsync("/auth/revoke", revokeRequest);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        var tokenRequest = new TokenRequest { ApiKey = registered.ApiKey };
        client.DefaultRequestHeaders.Remove("Authorization");
        var tokenResponse = await client.PostAsJsonAsync("/auth/token", tokenRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, tokenResponse.StatusCode);
    }

    [Fact]
    public async Task RotateKey_ReturnsNewKey_AndRevokesOld()
    {
        var client = _factory.CreateClient();
        
        var registerResponse = await client.PostAsJsonAsync("/auth/register", new { });
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {registered!.ApiKey}");
        
        var rotateResponse = await client.PostAsync("/auth/rotate-key", null);
        Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
        
        var rotated = await rotateResponse.Content.ReadFromJsonAsync<RotateKeyResponse>();
        Assert.NotNull(rotated);
        Assert.NotEqual(registered.ApiKey, rotated.ApiKey);
        Assert.Equal(registered.ApiKeyPrefix, rotated.RevokedKeyPrefix);

        var tokenRequest = new TokenRequest { ApiKey = registered.ApiKey };
        client.DefaultRequestHeaders.Remove("Authorization");
        var tokenResponse = await client.PostAsJsonAsync("/auth/token", tokenRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, tokenResponse.StatusCode);

        tokenRequest = new TokenRequest { ApiKey = rotated.ApiKey };
        tokenResponse = await client.PostAsJsonAsync("/auth/token", tokenRequest);
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
    }

    [Fact]
    public async Task Revoke_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var revokeRequest = new RevokeRequest { ApiKey = "dlw_test_1234567890" };
        var response = await client.PostAsJsonAsync("/auth/revoke", revokeRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RotateKey_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/auth/rotate-key", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
