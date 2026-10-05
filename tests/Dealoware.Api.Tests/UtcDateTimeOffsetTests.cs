using System.Net;
using System.Net.Http.Json;
using Dealoware.Application.Artifacts.Dtos;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Application.Negotiations.Dtos;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// DateTimeOffset values are normalized to UTC before persistence
/// (PostgreSQL timestamptz via Npgsql only accepts offset zero).
/// </summary>
public class UtcDateTimeOffsetUnitTests
{
    private static readonly DateTimeOffset PlusTwo = new(2030, 6, 1, 12, 30, 0, TimeSpan.FromHours(2));

    [Fact]
    public void Converter_NormalizesToUtc_SameInstant()
    {
        var converter = new UtcDateTimeOffsetConverter();

        var stored = (DateTimeOffset)converter.ConvertToProvider(PlusTwo)!;
        var read = (DateTimeOffset)converter.ConvertFromProvider(PlusTwo)!;

        Assert.Equal(TimeSpan.Zero, stored.Offset);
        Assert.Equal(PlusTwo.UtcDateTime, stored.UtcDateTime);
        Assert.Equal(new DateTimeOffset(2030, 6, 1, 10, 30, 0, TimeSpan.Zero), stored);
        Assert.Equal(TimeSpan.Zero, read.Offset);
    }

    [Fact]
    public void TimePeriod_Create_NormalizesToUtc()
    {
        var period = TimePeriod.Create(PlusTwo, PlusTwo.AddDays(1));

        Assert.Equal(TimeSpan.Zero, period.Start.Offset);
        Assert.Equal(TimeSpan.Zero, period.End.Offset);
        Assert.Equal(PlusTwo, period.Start);
    }

    [Fact]
    public void Negotiation_Create_NormalizesToUtc()
    {
        var (negotiation, errors) = Negotiation.Create(
            Guid.NewGuid(), "party-a", "party-b", "sell", "buy", PlusTwo, PlusTwo.AddDays(1));

        Assert.Empty(errors);
        Assert.Equal(TimeSpan.Zero, negotiation!.StartsAt!.Value.Offset);
        Assert.Equal(TimeSpan.Zero, negotiation.EndsAt!.Value.Offset);
        Assert.Equal(PlusTwo, negotiation.StartsAt.Value);
    }

    [Fact]
    public void RevokedToken_Create_NormalizesToUtc()
    {
        var token = RevokedToken.Create("jti", "sub", PlusTwo);

        Assert.Equal(TimeSpan.Zero, token.ExpiresAt.Offset);
        Assert.Equal(PlusTwo, token.ExpiresAt);
    }

    [Theory]
    [InlineData("Data Source=dealoware.db")]
    [InlineData("Host=localhost;Database=dealoware")]
    public void Model_AppliesUtcConverter_ToEveryDateTimeOffsetProperty(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(connectionString, _ => null, requireVerifiedTls: false);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        var dateProperties = db.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?))
            .ToList();

        Assert.NotEmpty(dateProperties);
        Assert.Contains(dateProperties, p => p.DeclaringType.ClrType == typeof(Negotiation) && p.Name == nameof(Negotiation.StartsAt));
        Assert.Contains(dateProperties, p => p.DeclaringType.ClrType == typeof(TimePeriod) && p.Name == nameof(TimePeriod.Start));
        Assert.All(dateProperties, p => Assert.IsType<UtcDateTimeOffsetConverter>(p.GetValueConverter()));
    }
}

[Collection("WebAppTests")]
public class UtcDateTimeOffsetEndpointTests
{
    private readonly IsolatedWebApplicationFactory _factory;

    public UtcDateTimeOffsetEndpointTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, string Sub)> CreateAuthenticatedClientAsync(string suffix)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/auth/register",
            new RegisterRequest { DisplayName = $"participant-{suffix}" });
        var participant = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {participant!.ApiKey}");
        return (client, participant.Sub);
    }

    private static void AssertUtcInstant(DateTimeOffset expected, DateTimeOffset? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(TimeSpan.Zero, actual!.Value.Offset);
        Assert.Equal(expected.UtcDateTime, actual.Value.UtcDateTime);
    }

    [Fact]
    public async Task Negotiation_WithPlusTwoOffsets_IsStoredAndReturnedAsUtc()
    {
        var (clientA, _) = await CreateAuthenticatedClientAsync($"utc-neg-a-{Guid.NewGuid()}");
        var (_, subB) = await CreateAuthenticatedClientAsync($"utc-neg-b-{Guid.NewGuid()}");

        var artifactResponse = await clientA.PostAsJsonAsync("/artifacts", new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto> { new() { Name = "Test Item", Description = "UTC test" } },
            Intent = "sell"
        });
        Assert.Equal(HttpStatusCode.Created, artifactResponse.StatusCode);
        var artifact = await artifactResponse.Content.ReadFromJsonAsync<ArtifactResponse>();

        var startsAt = new DateTimeOffset(2099, 3, 15, 9, 0, 0, TimeSpan.FromHours(2));
        var endsAt = new DateTimeOffset(2099, 3, 20, 18, 45, 0, TimeSpan.FromHours(2));

        var createResponse = await clientA.PostAsJsonAsync("/negotiations", new CreateNegotiationRequest
        {
            ArtifactId = artifact!.Id,
            CounterpartyParticipantId = subB,
            CallerIntent = "sell",
            CounterpartyIntent = "buy",
            StartsAt = startsAt,
            EndsAt = endsAt
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<NegotiationResponse>();
        AssertUtcInstant(startsAt, created!.StartsAt);
        AssertUtcInstant(endsAt, created.EndsAt);

        // Fresh request => fresh DbContext => values read back from the database.
        var getResponse = await clientA.GetAsync($"/negotiations/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<NegotiationResponse>();
        AssertUtcInstant(startsAt, fetched!.StartsAt);
        AssertUtcInstant(endsAt, fetched.EndsAt);
        Assert.Equal("Open", fetched.Status);
    }

    [Fact]
    public async Task Artifact_WithPlusTwoTimePeriod_IsStoredAndReturnedAsUtc()
    {
        var (client, _) = await CreateAuthenticatedClientAsync($"utc-art-{Guid.NewGuid()}");

        var start = new DateTimeOffset(2099, 7, 1, 8, 0, 0, TimeSpan.FromHours(2));
        var end = new DateTimeOffset(2099, 7, 31, 20, 0, 0, TimeSpan.FromHours(2));

        var createResponse = await client.PostAsJsonAsync("/artifacts", new CreateArtifactRequest
        {
            Entities = new List<CreateSubjectEntityDto> { new() { Name = "Test Item", Description = "UTC test" } },
            Intent = "sell",
            TimePeriods = new List<CreateTimePeriodDto> { new() { Start = start, End = end } }
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ArtifactResponse>();
        var createdPeriod = Assert.Single(created!.TimePeriods);
        AssertUtcInstant(start, createdPeriod.Start);
        AssertUtcInstant(end, createdPeriod.End);

        var getResponse = await client.GetAsync($"/artifacts/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ArtifactResponse>();
        var fetchedPeriod = Assert.Single(fetched!.TimePeriods);
        AssertUtcInstant(start, fetchedPeriod.Start);
        AssertUtcInstant(end, fetchedPeriod.End);
    }
}
