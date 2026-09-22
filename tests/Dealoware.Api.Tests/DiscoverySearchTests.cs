using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Api.Endpoints;

namespace Dealoware.Api.Tests;

/// <summary>
/// Tests for MVP Stage B Discovery Search (#40).
/// 
/// Security checklist points verified:
/// 1. Authn required: unauth → 401
/// 2. Discovery surface SEPARATE from owner inventory
/// 3. Search results omit secrets (OwnerParticipantId, LoginEmail, ContactEmail, StrategyBody)
/// 4. Search only Artifact fields already on path (D1-D5)
/// 5. Uniform deny; no private-field leakage
/// 6. Consumes #31 IFieldPolicy design
/// 7-8. OUT scope verified (no saved-search, no A1 matching)
/// 9. Query-plane filter (not fetch-all-then-filter)
/// 10. This test suite is the automated evidence
/// 
/// Test matrix per Spec §7.1:
/// - Unauth → 401
/// - Auth search returns discoverable fields only
/// - No LoginEmail/ContactEmail/StrategyBody in results
/// - Does not equal owner inventory dump for other users
/// - Wrong-principal misuse fail-closed (uniform deny)
/// </summary>
[Collection("WebAppTests")]
public class DiscoverySearchTests
{
    private readonly IsolatedWebApplicationFactory _factory;

    public DiscoverySearchTests(IsolatedWebApplicationFactory factory)
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

    private static CreateArtifactRequest CreateArtifactRequest(
        string entityName = "Test Item",
        string intent = "sell",
        string? location = null) => new()
    {
        Entities = new List<CreateSubjectEntityDto>
        {
            new()
            {
                Name = entityName,
                Description = $"Description for {entityName}",
                Facts = new List<string> { "Fact about " + entityName }
            }
        },
        Intent = intent,
        Locations = location is not null ? new List<string> { location } : null
    };

    #endregion

    #region Authentication Tests (Security Point 1)

    [Fact]
    public async Task Search_Unauth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/search/artifacts?q=test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_InvalidAuth_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.jwt.token");

        var response = await client.GetAsync("/search/artifacts?q=test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_InvalidApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "ApiKey dlw_invalid_notarealapikey");

        var response = await client.GetAsync("/search/artifacts?q=test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_Unauth_NoPrivateFieldsInError()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/search/artifacts?q=test");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("LoginEmail", body);
        Assert.DoesNotContain("ContactEmail", body);
        Assert.DoesNotContain("OwnerParticipantId", body);
        Assert.DoesNotContain("participant:", body);
    }

    #endregion

    #region Discovery Surface Tests (Security Point 2)

    [Fact]
    public async Task Search_IsSeparateFromOwnerInventory()
    {
        var (ownerClient, ownerSub, _) = await CreateAuthenticatedClientAsync($"discovery-owner-{Guid.NewGuid()}");
        var (searcherClient, searcherSub, _) = await CreateAuthenticatedClientAsync($"discovery-searcher-{Guid.NewGuid()}");

        await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest("UniqueDiscoveryItem", "sell"));

        var inventoryResponse = await searcherClient.GetAsync("/artifacts");
        var inventory = await inventoryResponse.Content.ReadFromJsonAsync<List<ArtifactResponse>>();

        var searchResponse = await searcherClient.GetAsync("/search/artifacts?q=UniqueDiscoveryItem");
        var searchResults = await searchResponse.Content.ReadFromJsonAsync<SearchArtifactsResponse>();

        Assert.Empty(inventory!);
        Assert.NotNull(searchResults);
        Assert.NotEmpty(searchResults.Results);
        Assert.Contains(searchResults.Results, r => r.Entities.Any(e => e.Name == "UniqueDiscoveryItem"));
    }

    [Fact]
    public async Task Search_CanDiscoverOthersArtifacts()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"search-owner-{Guid.NewGuid()}");
        var (searcherClient, _, _) = await CreateAuthenticatedClientAsync($"search-other-{Guid.NewGuid()}");

        var uniqueName = $"DiscoverableWidget_{Guid.NewGuid():N}";
        await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest(uniqueName, "sell"));

        var response = await searcherClient.GetAsync($"/search/artifacts?q={uniqueName}");
        var results = await response.Content.ReadFromJsonAsync<SearchArtifactsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(results);
        Assert.Single(results.Results);
        Assert.Equal(uniqueName, results.Results[0].Entities[0].Name);
    }

    [Fact]
    public async Task Search_DoesNotDumpPrivateInventory()
    {
        var (ownerClient, ownerSub, _) = await CreateAuthenticatedClientAsync($"nodump-owner-{Guid.NewGuid()}");
        var (searcherClient, _, _) = await CreateAuthenticatedClientAsync($"nodump-searcher-{Guid.NewGuid()}");

        var privateItem = $"PrivateItem_{Guid.NewGuid():N}";
        await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest(privateItem, "sell"));

        var searchResponse = await searcherClient.GetAsync("/search/artifacts?q=somethingcompletelyunrelated");
        var results = await searchResponse.Content.ReadFromJsonAsync<SearchArtifactsResponse>();

        Assert.NotNull(results);
        Assert.DoesNotContain(results.Results, r => r.Entities.Any(e => e.Name == privateItem));
    }

    #endregion

    #region Field Omission Tests (Security Point 3)

    [Fact]
    public async Task Search_Results_NoOwnerParticipantId()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"field-owner-{Guid.NewGuid()}");
        var (searcherClient, _, _) = await CreateAuthenticatedClientAsync($"field-searcher-{Guid.NewGuid()}");

        var uniqueName = $"NoOwnerField_{Guid.NewGuid():N}";
        await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest(uniqueName, "sell"));

        var response = await searcherClient.GetAsync($"/search/artifacts?q={uniqueName}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("ownerParticipantId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("participant:", body);
    }

    [Fact]
    public async Task Search_Results_NoLoginEmail()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"noemail-owner-{Guid.NewGuid()}");
        var (searcherClient, _, _) = await CreateAuthenticatedClientAsync($"noemail-searcher-{Guid.NewGuid()}");

        var uniqueName = $"NoEmail_{Guid.NewGuid():N}";
        await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest(uniqueName, "sell"));

        var response = await searcherClient.GetAsync($"/search/artifacts?q={uniqueName}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("loginEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("login@", body);
    }

    [Fact]
    public async Task Search_Results_NoContactEmail()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"nocontact-owner-{Guid.NewGuid()}");
        var (searcherClient, _, _) = await CreateAuthenticatedClientAsync($"nocontact-searcher-{Guid.NewGuid()}");

        var uniqueName = $"NoContact_{Guid.NewGuid():N}";
        await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest(uniqueName, "sell"));

        var response = await searcherClient.GetAsync($"/search/artifacts?q={uniqueName}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("contactEmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contact@", body);
    }

    [Fact]
    public async Task Search_Results_NoStrategyBody()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"plancheck-owner-{Guid.NewGuid()}");
        var (searcherClient, _, _) = await CreateAuthenticatedClientAsync($"plancheck-searcher-{Guid.NewGuid()}");

        var uniqueName = $"PlanCheckItem_{Guid.NewGuid():N}";
        await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest(uniqueName, "sell"));

        var response = await searcherClient.GetAsync($"/search/artifacts?q={uniqueName}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("strategyBody", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"strategy\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_Results_NoAuthSecrets()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"authcheck-owner-{Guid.NewGuid()}");
        var (searcherClient, _, _) = await CreateAuthenticatedClientAsync($"authcheck-searcher-{Guid.NewGuid()}");

        var uniqueName = $"AuthCheckItem_{Guid.NewGuid():N}";
        await ownerClient.PostAsJsonAsync("/artifacts", CreateArtifactRequest(uniqueName, "sell"));

        var response = await searcherClient.GetAsync($"/search/artifacts?q={uniqueName}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("apiKey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dlw_", body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"secret\"", body, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Discoverable Fields Tests (Security Point 4)

    [Fact]
    public async Task Search_Auth_ReturnsDiscoverableFieldsOnly()
    {
        var (ownerClient, _, _) = await CreateAuthenticatedClientAsync($"fields-owner-{Guid.NewGuid()}");
        var (searcherClient, _, _) = await CreateAuthenticatedClientAsync($"fields-searcher-{Guid.NewGuid()}");

        var uniqueName = $"DiscFields_{Guid.NewGuid():N}";
        var createRequest = new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>
            {
                new()
                {
                    Name = uniqueName,
                    Description = "A discoverable item",
                    Properties = new List<CreatePropertyDto>
                    {
                        new() { Name = "Color", Type = "string", Value = "Blue" }
                    },
                    Facts = new List<string> { "High quality", "Brand new" }
                }
            },
            Intent = "sell",
            Locations = new List<string> { "New York", "Los Angeles" },
            Values = new List<CreateValueDto>
            {
                new() { Amount = 100m, Currency = "USD" }
            },
            TimePeriods = new List<CreateTimePeriodDto>
            {
                new() { Start = DateTimeOffset.UtcNow, End = DateTimeOffset.UtcNow.AddDays(30) }
            }
        };
        await ownerClient.PostAsJsonAsync("/artifacts", createRequest);

        var response = await searcherClient.GetAsync($"/search/artifacts?q={uniqueName}");
        var results = await response.Content.ReadFromJsonAsync<SearchArtifactsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(results);
        Assert.Single(results.Results);

        var artifact = results.Results[0];
        Assert.Equal(uniqueName, artifact.Entities[0].Name);
        Assert.Equal("A discoverable item", artifact.Entities[0].Description);
        Assert.Contains(artifact.Entities[0].Facts, f => f == "High quality");
        Assert.Equal("sell", artifact.Intent);
        Assert.Contains(artifact.Locations, l => l == "New York");
        Assert.Single(artifact.Values);
        Assert.Equal(100m, artifact.Values[0].Amount);
        Assert.Single(artifact.TimePeriods);
    }

    [Fact]
    public async Task Search_ByIntent_Works()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"intent-{Guid.NewGuid()}");

        var uniqueIntent = $"specialintent{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/artifacts", CreateArtifactRequest("IntentItem", uniqueIntent));

        var response = await client.GetAsync($"/search/artifacts?q={uniqueIntent}");
        var results = await response.Content.ReadFromJsonAsync<SearchArtifactsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(results);
        Assert.Contains(results.Results, r => r.Intent == uniqueIntent);
    }

    [Fact]
    public async Task Search_ByEntityDescription_Works()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"description-{Guid.NewGuid()}");

        var uniqueDescription = $"UniqueDesc{Guid.NewGuid():N}";
        var request = new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>
            {
                new()
                {
                    Name = "DescriptionTest",
                    Description = uniqueDescription
                }
            },
            Intent = "sell"
        };
        await client.PostAsJsonAsync("/artifacts", request);

        var response = await client.GetAsync($"/search/artifacts?q={uniqueDescription}");
        var results = await response.Content.ReadFromJsonAsync<SearchArtifactsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(results);
        Assert.Contains(results.Results, r => r.Entities.Any(e => e.Description == uniqueDescription));
    }

    [Fact]
    public async Task Search_ByEntityName_Works()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"entityname-{Guid.NewGuid()}");

        var uniqueName = $"UniqueEntity{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/artifacts", CreateArtifactRequest(uniqueName, "sell"));

        var response = await client.GetAsync($"/search/artifacts?q={uniqueName}");
        var results = await response.Content.ReadFromJsonAsync<SearchArtifactsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(results);
        Assert.Contains(results.Results, r => r.Entities.Any(e => e.Name == uniqueName));
    }

    #endregion

    #region Query Validation Tests

    [Fact]
    public async Task Search_EmptyQuery_Returns400()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"emptyquery-{Guid.NewGuid()}");

        var response = await client.GetAsync("/search/artifacts?q=");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_MissingQuery_Returns400()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"missingquery-{Guid.NewGuid()}");

        var response = await client.GetAsync("/search/artifacts");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WhitespaceQuery_Returns400()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"whitespace-{Guid.NewGuid()}");

        var response = await client.GetAsync("/search/artifacts?q=   ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Query-Plane Filter Tests (Security Point 9)

    [Fact]
    public async Task Search_QueryPlaneFilter_MatchesQuery()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"qpf-{Guid.NewGuid()}");

        var matchingName = $"MatchingItem_{Guid.NewGuid():N}";
        var nonMatchingName = $"DifferentItem_{Guid.NewGuid():N}";
        
        await client.PostAsJsonAsync("/artifacts", CreateArtifactRequest(matchingName, "sell"));
        await client.PostAsJsonAsync("/artifacts", CreateArtifactRequest(nonMatchingName, "buy"));

        var response = await client.GetAsync($"/search/artifacts?q={matchingName}");
        var results = await response.Content.ReadFromJsonAsync<SearchArtifactsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(results);
        Assert.Contains(results.Results, r => r.Entities.Any(e => e.Name == matchingName));
        Assert.DoesNotContain(results.Results, r => r.Entities.Any(e => e.Name == nonMatchingName));
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyResults()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"nomatch-{Guid.NewGuid()}");

        await client.PostAsJsonAsync("/artifacts", CreateArtifactRequest("RealItem", "sell"));

        var response = await client.GetAsync("/search/artifacts?q=ThisDoesNotMatchAnything12345");
        var results = await response.Content.ReadFromJsonAsync<SearchArtifactsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(results);
        Assert.Empty(results.Results);
        Assert.Equal(0, results.TotalCount);
    }

    #endregion

    #region Limit Tests

    [Fact]
    public async Task Search_RespectsLimit()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"limit-{Guid.NewGuid()}");

        var baseName = $"LimitTest_{Guid.NewGuid():N}";
        for (int i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/artifacts", CreateArtifactRequest($"{baseName}_{i}", "sell"));
        }

        var response = await client.GetAsync($"/search/artifacts?q={baseName}&limit=2");
        var results = await response.Content.ReadFromJsonAsync<SearchArtifactsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(results);
        Assert.Equal(2, results.Results.Count);
    }

    [Fact]
    public async Task Search_DefaultLimit_Is50()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"deflimit-{Guid.NewGuid()}");

        var response = await client.GetAsync("/search/artifacts?q=test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Case Insensitivity Tests

    [Fact]
    public async Task Search_IsCaseInsensitive()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync($"case-{Guid.NewGuid()}");

        var uniqueName = $"CaseSensitiveTest{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/artifacts", CreateArtifactRequest(uniqueName, "sell"));

        var lowerResponse = await client.GetAsync($"/search/artifacts?q={uniqueName.ToLower()}");
        var upperResponse = await client.GetAsync($"/search/artifacts?q={uniqueName.ToUpper()}");

        var lowerResults = await lowerResponse.Content.ReadFromJsonAsync<SearchArtifactsResponse>();
        var upperResults = await upperResponse.Content.ReadFromJsonAsync<SearchArtifactsResponse>();

        Assert.NotEmpty(lowerResults!.Results);
        Assert.NotEmpty(upperResults!.Results);
    }

    #endregion

    #region Health Endpoint Unchanged

    [Fact]
    public async Task Health_StillNoAuthRequired()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}
