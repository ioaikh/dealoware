using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dealoware.Api.Tests;

/// <summary>
/// Tests for Artifact endpoints (D1-D5 properties).
/// All tests use proper Authorization header authentication.
/// </summary>
public class ArtifactEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ArtifactEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, string Sub)> CreateAuthenticatedClientAsync(string participantSuffix)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/auth/register", 
            new RegisterRequest { DisplayName = $"participant-{participantSuffix}" });
        var participant = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant!.ApiKey}");
        return (client, participant.Sub);
    }

    private static CreateArtifactRequest CreateValidRequest() => new()
    {
        Entities = new List<CreateSubjectEntityDto>
        {
            new()
            {
                Name = "Test Entity",
                Description = "A test entity description",
                Properties = new List<CreatePropertyDto>
                {
                    new() { Name = "color", Type = "string", Value = "red" }
                },
                Facts = new List<string> { "Fact one", "Fact two" }
            }
        },
        Intent = "sell",
        Values = new List<CreateValueDto>
        {
            new() { Amount = 100.00m, Currency = "USD" }
        },
        Locations = new List<string> { "New York", "Los Angeles" },
        TimePeriods = new List<CreateTimePeriodDto>
        {
            new() { Start = DateTimeOffset.UtcNow, End = DateTimeOffset.UtcNow.AddDays(30) }
        }
    };

    [Fact]
    public async Task CreateArtifact_ValidRequest_Returns201WithArtifact()
    {
        var (client, sub) = await CreateAuthenticatedClientAsync("1");
        var request = CreateValidRequest();
        
        var response = await client.PostAsJsonAsync("/artifacts", request);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var artifact = await response.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.NotNull(artifact);
        Assert.NotEqual(Guid.Empty, artifact.Id);
        Assert.Equal(sub, artifact.OwnerParticipantId);
        Assert.Equal("sell", artifact.Intent);
        Assert.Single(artifact.Entities);
        Assert.Equal("Test Entity", artifact.Entities[0].Name);
        Assert.Single(artifact.Values);
        Assert.Equal(100.00m, artifact.Values[0].Amount);
        Assert.Equal("USD", artifact.Values[0].Currency);
        Assert.Equal(2, artifact.Locations.Count);
        Assert.Single(artifact.TimePeriods);
    }

    [Fact]
    public async Task CreateArtifact_MissingPrincipal_Returns401()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();
        
        var response = await client.PostAsJsonAsync("/artifacts", request);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateArtifact_InvalidAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var request = CreateValidRequest();
        
        client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid");
        
        var response = await client.PostAsJsonAsync("/artifacts", request);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateArtifact_NoEntities_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("no-entities");
        var request = new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>(),
            Intent = "sell"
        };
        
        var response = await client.PostAsJsonAsync("/artifacts", request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateArtifact_MissingIntent_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("missing-intent");
        var request = new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>
            {
                new() { Name = "Test", Description = "Test" }
            },
            Intent = null
        };
        
        var response = await client.PostAsJsonAsync("/artifacts", request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateArtifact_DuplicateCurrency_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("dup-currency");
        var request = new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>
            {
                new() { Name = "Test", Description = "Test" }
            },
            Intent = "sell",
            Values = new List<CreateValueDto>
            {
                new() { Amount = 100.00m, Currency = "USD" },
                new() { Amount = 200.00m, Currency = "usd" }
            }
        };
        
        var response = await client.PostAsJsonAsync("/artifacts", request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Duplicate currency", content);
    }

    [Fact]
    public async Task GetArtifact_OwnArtifact_Returns200()
    {
        var (client, sub) = await CreateAuthenticatedClientAsync("get-test");
        var request = CreateValidRequest();
        
        var createResponse = await client.PostAsJsonAsync("/artifacts", request);
        var created = await createResponse.Content.ReadFromJsonAsync<ArtifactResponse>();
        
        var response = await client.GetAsync($"/artifacts/{created!.Id}");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var artifact = await response.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.NotNull(artifact);
        Assert.Equal(created.Id, artifact.Id);
    }

    [Fact]
    public async Task GetArtifact_OtherOwnerArtifact_Returns404()
    {
        var (client1, _) = await CreateAuthenticatedClientAsync("owner");
        var (client2, _) = await CreateAuthenticatedClientAsync("other");
        var request = CreateValidRequest();
        
        var createResponse = await client1.PostAsJsonAsync("/artifacts", request);
        var created = await createResponse.Content.ReadFromJsonAsync<ArtifactResponse>();
        
        var response = await client2.GetAsync($"/artifacts/{created!.Id}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetArtifact_NonExistent_Returns404()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("nonexistent");
        
        var response = await client.GetAsync($"/artifacts/{Guid.NewGuid()}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetArtifact_MissingPrincipal_Returns401()
    {
        var client = _factory.CreateClient();
        
        var response = await client.GetAsync($"/artifacts/{Guid.NewGuid()}");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListOwnArtifacts_Returns200WithOwnArtifactsOnly()
    {
        var (client, sub) = await CreateAuthenticatedClientAsync("list-test");
        var request = CreateValidRequest();
        
        await client.PostAsJsonAsync("/artifacts", request);
        await client.PostAsJsonAsync("/artifacts", request);
        
        var response = await client.GetAsync("/artifacts");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var artifacts = await response.Content.ReadFromJsonAsync<List<ArtifactResponse>>();
        Assert.NotNull(artifacts);
        Assert.True(artifacts.Count >= 2);
        Assert.All(artifacts, a => Assert.Equal(sub, a.OwnerParticipantId));
    }

    [Fact]
    public async Task ListOwnArtifacts_NoArtifacts_Returns200WithEmptyList()
    {
        var (client, _) = await CreateAuthenticatedClientAsync($"no-artifacts-{Guid.NewGuid()}");
        
        var response = await client.GetAsync("/artifacts");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var artifacts = await response.Content.ReadFromJsonAsync<List<ArtifactResponse>>();
        Assert.NotNull(artifacts);
        Assert.Empty(artifacts);
    }

    [Fact]
    public async Task ListOwnArtifacts_MissingPrincipal_Returns401()
    {
        var client = _factory.CreateClient();
        
        var response = await client.GetAsync("/artifacts");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListOwnArtifacts_FiltersOutOtherOwners()
    {
        var (client1, sub1) = await CreateAuthenticatedClientAsync("filter-owner-1");
        var (client2, sub2) = await CreateAuthenticatedClientAsync("filter-owner-2");
        var request = CreateValidRequest();
        
        await client1.PostAsJsonAsync("/artifacts", request);
        await client2.PostAsJsonAsync("/artifacts", request);
        
        var response = await client2.GetAsync("/artifacts");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var artifacts = await response.Content.ReadFromJsonAsync<List<ArtifactResponse>>();
        Assert.NotNull(artifacts);
        Assert.All(artifacts, a => Assert.Equal(sub2, a.OwnerParticipantId));
    }

    [Fact]
    public async Task CreateArtifact_MultipleEntities_StoresAll()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("multi-entity");
        var request = new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>
            {
                new() { Name = "Entity 1", Description = "First" },
                new() { Name = "Entity 2", Description = "Second" },
                new() { Name = "Entity 3", Description = "Third" }
            },
            Intent = "exchange"
        };
        
        var response = await client.PostAsJsonAsync("/artifacts", request);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var artifact = await response.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.NotNull(artifact);
        Assert.Equal(3, artifact.Entities.Count);
    }

    [Fact]
    public async Task CreateArtifact_CurrencyNormalized_StoredUppercase()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("currency-test");
        var request = new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>
            {
                new() { Name = "Test", Description = "Test" }
            },
            Intent = "sell",
            Values = new List<CreateValueDto>
            {
                new() { Amount = 100.00m, Currency = "eur" }
            }
        };
        
        var response = await client.PostAsJsonAsync("/artifacts", request);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var artifact = await response.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.NotNull(artifact);
        Assert.Equal("EUR", artifact.Values[0].Currency);
    }

    [Fact]
    public async Task CreateArtifact_WithAllD1ToD5Properties_PersistsCorrectly()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("full-d1-d5");
        var startDate = DateTimeOffset.UtcNow;
        var endDate = startDate.AddDays(7);
        
        var request = new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto>
            {
                new()
                {
                    Name = "Complete Entity",
                    Description = "Full description with all fields",
                    Properties = new List<CreatePropertyDto>
                    {
                        new() { Name = "prop1", Type = "string", Value = "value1" },
                        new() { Name = "prop2", Type = "number", Value = "42" }
                    },
                    Facts = new List<string> { "Fact A", "Fact B", "Fact C" }
                }
            },
            Intent = "provide",
            Values = new List<CreateValueDto>
            {
                new() { Amount = 500.50m, Currency = "USD" },
                new() { Amount = 450.25m, Currency = "EUR" }
            },
            Locations = new List<string> { "Location 1", "Location 2", "Location 3" },
            TimePeriods = new List<CreateTimePeriodDto>
            {
                new() { Start = startDate, End = endDate }
            }
        };
        
        var createResponse = await client.PostAsJsonAsync("/artifacts", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        
        var created = await createResponse.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.NotNull(created);
        
        var getResponse = await client.GetAsync($"/artifacts/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        
        var artifact = await getResponse.Content.ReadFromJsonAsync<ArtifactResponse>();
        Assert.NotNull(artifact);
        
        Assert.Single(artifact.Entities);
        Assert.Equal("Complete Entity", artifact.Entities[0].Name);
        Assert.Equal(2, artifact.Entities[0].Properties.Count);
        Assert.Equal(3, artifact.Entities[0].Facts.Count);
        
        Assert.Equal("provide", artifact.Intent);
        
        Assert.Equal(2, artifact.Values.Count);
        
        Assert.Equal(3, artifact.Locations.Count);
        
        Assert.Single(artifact.TimePeriods);
    }
}
