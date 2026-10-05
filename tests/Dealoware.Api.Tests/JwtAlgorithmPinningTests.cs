using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Infrastructure.Auth;
using Microsoft.IdentityModel.Tokens;

namespace Dealoware.Api.Tests;

/// <summary>
/// JWT validation accepts HS256 only. Uses a host with a known test-only key so forged tokens
/// can be signed with exactly the key the app uses.
/// </summary>
public sealed class JwtAlgorithmPinningTests : IDisposable
{
    private readonly EnvironmentWebApplicationFactory _factory =
        new("Development", EnvironmentWebApplicationFactory.TestSigningKey64);

    public void Dispose() => _factory.Dispose();

    private async Task<(HttpClient Client, RegisterResponse Participant)> RegisterAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = $"alg-pin-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var participant = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        return (client, participant!);
    }

    private static Claim[] ValidClaims(string sub, DateTimeOffset now) => new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, sub),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
    };

    private static string SignedToken(string sub, string algorithm)
    {
        var now = DateTimeOffset.UtcNow;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(EnvironmentWebApplicationFactory.TestSigningKey64));
        var token = new JwtSecurityToken(
            issuer: JwtService.Issuer,
            audience: JwtService.Audience,
            claims: ValidClaims(sub, now),
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(10).UtcDateTime,
            signingCredentials: new SigningCredentials(key, algorithm));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string UnsignedAlgNoneToken(string sub)
    {
        var now = DateTimeOffset.UtcNow;
        static string B64(string json) => Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));
        var header = B64("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = B64(
            "{" +
            $"\"sub\":\"{sub}\"," +
            $"\"jti\":\"{Guid.NewGuid()}\"," +
            $"\"iat\":{now.ToUnixTimeSeconds()}," +
            $"\"nbf\":{now.ToUnixTimeSeconds()}," +
            $"\"exp\":{now.AddMinutes(10).ToUnixTimeSeconds()}," +
            $"\"iss\":\"{JwtService.Issuer}\"," +
            $"\"aud\":\"{JwtService.Audience}\"" +
            "}");
        return $"{header}.{payload}.";
    }

    private static async Task<HttpStatusCode> GetProfileWithBearer(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    [Fact]
    public async Task AppIssuedHs256Token_IsAccepted()
    {
        var (client, participant) = await RegisterAsync();
        var tokenResponse = await client.PostAsJsonAsync("/auth/token", new TokenRequest { ApiKey = participant.ApiKey });
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        var token = (await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;

        Assert.Equal(SecurityAlgorithms.HmacSha256, new JwtSecurityTokenHandler().ReadJwtToken(token).Header.Alg);
        Assert.Equal(HttpStatusCode.OK, await GetProfileWithBearer(client, token));
    }

    [Fact]
    public async Task SelfSignedHs256Token_WithSameKey_IsAccepted()
    {
        // Control for the HS512 case: same claims and key, only the algorithm differs.
        var (client, participant) = await RegisterAsync();

        Assert.Equal(HttpStatusCode.OK, await GetProfileWithBearer(client, SignedToken(participant.Sub, SecurityAlgorithms.HmacSha256)));
    }

    [Fact]
    public async Task Hs512Token_WithSameKey_Returns401()
    {
        var (client, participant) = await RegisterAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, await GetProfileWithBearer(client, SignedToken(participant.Sub, SecurityAlgorithms.HmacSha512)));
    }

    [Fact]
    public async Task UnsignedAlgNoneToken_Returns401()
    {
        var (client, participant) = await RegisterAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, await GetProfileWithBearer(client, UnsignedAlgNoneToken(participant.Sub)));
    }
}
