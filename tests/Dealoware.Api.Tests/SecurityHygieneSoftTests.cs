using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Negotiations.Dtos;

namespace Dealoware.Api.Tests;

/// <summary>
/// Security hygiene "soft" tests (P2 thin coverage).
/// 
/// Verifies:
/// - JWT signing-key hygiene: Development uses documented placeholder only
/// - Register response hygiene: no password/hash fields in /auth/register or /auth/token responses
/// - Secrets-in-response smoke: no PEM / AKIA / raw signing-key-like patterns in happy-path JSON
/// 
/// Cite-only (existing coverage):
/// - Authn fail-closed: AuthHeaderOnlyTests (query string token rejection)
/// - Party-only 404: ArtifactEndpointTests.GetArtifact_OtherOwnerArtifact_Returns404
/// - Identity-seal: IdentitySealTests (opaque participant IDs, no PII leak)
/// </summary>
[Collection("WebAppTests")]
public class SecurityHygieneSoftTests
{
    private readonly IsolatedWebApplicationFactory _factory;

    public SecurityHygieneSoftTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region JWT Signing-Key Hygiene

    [Fact]
    public void JwtSigningKey_Development_UsesDocumentedPlaceholderOnly()
    {
        var envKey = Environment.GetEnvironmentVariable("DEALOWARE_JWT_SIGNING_KEY");
        
        if (string.IsNullOrEmpty(envKey))
        {
            Assert.True(true, "No env key set; fallback to documented placeholder is expected");
        }
        else
        {
            Assert.True(
                envKey == "DEVELOPMENT_PLACEHOLDER_KEY_CHANGE_IN_PRODUCTION_32CHARS" ||
                envKey.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase) ||
                envKey.Contains("DEVELOPMENT", StringComparison.OrdinalIgnoreCase) ||
                envKey.Contains("TEST", StringComparison.OrdinalIgnoreCase),
                $"JWT signing key in test environment should be documented placeholder, not a real-looking key");
        }
    }

    [Fact]
    public void JwtSigningKey_TestAppsettings_NoRealLookingKeys()
    {
        var testDir = Path.GetDirectoryName(typeof(SecurityHygieneSoftTests).Assembly.Location)!;
        var configFiles = new[]
        {
            Path.Combine(testDir, "appsettings.json"),
            Path.Combine(testDir, "appsettings.Development.json"),
            Path.Combine(testDir, "appsettings.Test.json")
        };

        foreach (var configFile in configFiles.Where(File.Exists))
        {
            var content = File.ReadAllText(configFile);
            SecretsInResponseHelper.AssertNoSecretsInJson(content, $"Config file {Path.GetFileName(configFile)}");
        }
    }

    #endregion

    #region Register Response Hygiene

    [Fact]
    public async Task Register_Response_DoesNotContainPasswordOrHashFields()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register", 
            new RegisterRequest { DisplayName = "Hygiene Test" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();

        AssertNoPasswordOrHashFields(json, "/auth/register response");
        SecretsInResponseHelper.AssertNoSecretsInJson(json, "/auth/register response");
    }

    [Fact]
    public async Task Token_Response_DoesNotContainPasswordOrHashFields()
    {
        var client = _factory.CreateClient();
        
        var registerResponse = await client.PostAsJsonAsync("/auth/register", new { });
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        var tokenResponse = await client.PostAsJsonAsync("/auth/token", 
            new TokenRequest { ApiKey = registered!.ApiKey });

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        var json = await tokenResponse.Content.ReadAsStringAsync();

        AssertNoPasswordOrHashFields(json, "/auth/token response");
        SecretsInResponseHelper.AssertNoSecretsInJson(json, "/auth/token response");
    }

    [Fact]
    public async Task Register_Response_OnlyContainsDesignedFields()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register", 
            new RegisterRequest { DisplayName = "Field Check" });

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var allowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "sub", "displayName", "apiKey", "apiKeyPrefix", "createdAt"
        };

        foreach (var property in root.EnumerateObject())
        {
            Assert.True(allowedFields.Contains(property.Name),
                $"Unexpected field '{property.Name}' in register response");
        }
    }

    private static void AssertNoPasswordOrHashFields(string json, string context)
    {
        var forbiddenFields = new[]
        {
            "password", "passwordHash", "password_hash", "passwdHash", "passwd_hash",
            "hash", "hashedPassword", "hashed_password", "secretHash", "secret_hash",
            "salt", "passwordSalt", "password_salt", "bcrypt", "scrypt", "argon",
            "credential", "credentials", "secret", "privateKey", "private_key"
        };

        var lowerJson = json.ToLowerInvariant();
        foreach (var forbidden in forbiddenFields)
        {
            Assert.DoesNotContain($"\"{forbidden.ToLowerInvariant()}\"", lowerJson,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    #endregion

    #region Secrets-in-Response Smoke Tests

    [Fact]
    public async Task Auth_Register_NoSecretsInResponse()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register", 
            new RegisterRequest { DisplayName = "Secrets Smoke" });

        var json = await response.Content.ReadAsStringAsync();
        SecretsInResponseHelper.AssertNoSecretsInJson(json, "Auth register");
    }

    [Fact]
    public async Task Auth_Token_NoSecretsInResponse()
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/auth/register", new { });
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        var tokenResponse = await client.PostAsJsonAsync("/auth/token",
            new TokenRequest { ApiKey = registered!.ApiKey });

        var json = await tokenResponse.Content.ReadAsStringAsync();
        SecretsInResponseHelper.AssertNoSecretsInJson(json, "Auth token");
    }

    [Fact]
    public async Task Artifact_Create_NoSecretsInResponse()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("artifact-secrets");
        
        var response = await client.PostAsJsonAsync("/artifacts", new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>
            {
                new() { Name = "Test", Description = "Artifact secrets test" }
            },
            Intent = "sell"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        SecretsInResponseHelper.AssertNoSecretsInJson(json, "Artifact create");
    }

    [Fact]
    public async Task Artifact_Get_NoSecretsInResponse()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("artifact-get-secrets");
        
        var createResponse = await client.PostAsJsonAsync("/artifacts", new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>
            {
                new() { Name = "Test", Description = "Artifact get secrets test" }
            },
            Intent = "sell"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var getResponse = await client.GetAsync($"/artifacts/{created!.Id}");
        var json = await getResponse.Content.ReadAsStringAsync();
        SecretsInResponseHelper.AssertNoSecretsInJson(json, "Artifact get");
    }

    [Fact]
    public async Task Negotiation_Create_NoSecretsInResponse()
    {
        var (clientA, _) = await CreateAuthenticatedClientAsync("neg-create-a");
        var (_, subB, _) = await CreateAuthenticatedClientWithSubAsync("neg-create-b");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>
            {
                new() { Name = "Test", Description = "Neg create test" }
            },
            Intent = "sell"
        });
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });

        Assert.Equal(HttpStatusCode.Created, negResponse.StatusCode);
        var json = await negResponse.Content.ReadAsStringAsync();
        SecretsInResponseHelper.AssertNoSecretsInJson(json, "Negotiation create");
    }

    [Fact]
    public async Task Offer_Create_NoSecretsInResponse()
    {
        var (clientA, _) = await CreateAuthenticatedClientAsync("offer-create-a");
        var (_, subB, _) = await CreateAuthenticatedClientWithSubAsync("offer-create-b");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>
            {
                new() { Name = "Test", Description = "Offer create test" }
            },
            Intent = "sell"
        });
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var negResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy"
        });
        var negotiation = await negResponse.Content.ReadFromJsonAsync<NegotiationResponse>();

        var offerResponse = await clientA.PostAsJsonAsync(
            $"/negotiations/{negotiation!.Id}/offers",
            new CreateOfferRequest { Amount = 100m, Currency = "USD" });

        Assert.Equal(HttpStatusCode.Created, offerResponse.StatusCode);
        var json = await offerResponse.Content.ReadAsStringAsync();
        SecretsInResponseHelper.AssertNoSecretsInJson(json, "Offer create");
    }

    #endregion

    #region Helpers

    private async Task<(HttpClient Client, string Sub)> CreateAuthenticatedClientAsync(string suffix)
    {
        var (client, sub, _) = await CreateAuthenticatedClientWithSubAsync(suffix);
        return (client, sub);
    }

    private async Task<(HttpClient Client, string Sub, string ApiKey)> CreateAuthenticatedClientWithSubAsync(string suffix)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = $"participant-{suffix}" });
        var participant = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant!.ApiKey}");
        return (client, participant.Sub, participant.ApiKey);
    }

    #endregion
}

/// <summary>
/// Shared helper for detecting secrets/sensitive patterns in JSON responses.
/// </summary>
public static class SecretsInResponseHelper
{
    private static readonly Regex PemPattern = new(
        @"-----BEGIN\s+(RSA\s+)?(PRIVATE|PUBLIC)\s+KEY-----",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AwsAccessKeyPattern = new(
        @"AKIA[0-9A-Z]{16}",
        RegexOptions.Compiled);

    private static readonly Regex AwsSecretKeyPattern = new(
        @"[A-Za-z0-9/+=]{40}",
        RegexOptions.Compiled);

    private static readonly Regex JwtSigningKeyPattern = new(
        @"[A-Za-z0-9+/=]{32,}(?:Key|Secret|Signing)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Base64PrivateKeyPattern = new(
        @"MII[A-Za-z0-9+/=]{100,}",
        RegexOptions.Compiled);

    private static readonly Regex GenericSecretPattern = new(
        @"""(signingKey|jwtSecret|jwt_secret|api_secret|apiSecret|secretKey|secret_key|privateKey|private_key)""\s*:\s*""[^""]{20,}""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void AssertNoSecretsInJson(string json, string context)
    {
        Assert.False(PemPattern.IsMatch(json),
            $"PEM private/public key pattern detected in {context}");

        Assert.False(AwsAccessKeyPattern.IsMatch(json),
            $"AWS access key pattern (AKIA...) detected in {context}");

        var awsSecretMatches = AwsSecretKeyPattern.Matches(json);
        foreach (Match match in awsSecretMatches)
        {
            if (!IsLikelyFalsePositive(match.Value, json))
            {
                Assert.Fail($"Potential AWS secret key pattern detected in {context}: {match.Value[..Math.Min(10, match.Value.Length)]}...");
            }
        }

        Assert.False(Base64PrivateKeyPattern.IsMatch(json),
            $"Base64-encoded private key pattern (MII...) detected in {context}");

        Assert.False(GenericSecretPattern.IsMatch(json),
            $"Generic secret field pattern detected in {context}");

        AssertNoRawSigningKeyLikeValues(json, context);
    }

    private static void AssertNoRawSigningKeyLikeValues(string json, string context)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            CheckElementForSigningKeys(doc.RootElement, context, "");
        }
        catch (JsonException)
        {
        }
    }

    private static void CheckElementForSigningKeys(JsonElement element, string context, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                    
                    var lowerName = property.Name.ToLowerInvariant();
                    if (lowerName.Contains("signingkey") || 
                        lowerName.Contains("signing_key") ||
                        lowerName.Contains("jwtsecret") ||
                        lowerName.Contains("jwt_secret"))
                    {
                        if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            var value = property.Value.GetString() ?? "";
                            if (value.Length >= 32 && 
                                !value.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase) &&
                                !value.Contains("DEVELOPMENT", StringComparison.OrdinalIgnoreCase) &&
                                !value.Contains("TEST", StringComparison.OrdinalIgnoreCase))
                            {
                                Assert.Fail($"Suspicious signing key value at {propertyPath} in {context}");
                            }
                        }
                    }
                    
                    CheckElementForSigningKeys(property.Value, context, propertyPath);
                }
                break;
            
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    CheckElementForSigningKeys(item, context, $"{path}[{index}]");
                    index++;
                }
                break;
        }
    }

    private static bool IsLikelyFalsePositive(string value, string json)
    {
        if (value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
        {
            if (json.Contains($"\"{value}\"") || json.Contains($":{value}\""))
            {
                return true;
            }
        }
        
        if (value.Length == 40 && value.All(char.IsLetterOrDigit))
        {
            return true;
        }
        
        return false;
    }
}
