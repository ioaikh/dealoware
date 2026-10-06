using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Domain.Strategies;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// A7 Step 7 admin read API. Case IDs match the test design (TD-ADM-*).
/// TD-ADM-130 is a UI case and is deferred to the UI PR after UX1.
/// </summary>
[Collection("WebAppTests")]
public class AdminReadApiTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminReadApiTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private async Task<Guid> SeedSessionAsync(
        string email = CoreOwnerEmail,
        bool totpVerified = true,
        DateTimeOffset? createdAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(email, ipHmac: "testhmac-not-an-ip", now: createdAt);
        if (totpVerified)
            session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private static HttpRequestMessage AdminGet(string path, Guid? sessionId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        if (sessionId is { } id)
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={id:D}");
        return request;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>();

    private static void AssertSafeUnauthorized(HttpResponseMessage response, string body)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("\"error\":\"Unauthorized\"", body);
        Assert.DoesNotContain(CoreOwnerEmail, body);
        Assert.DoesNotContain("TOTP", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("strategyBody", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertNoDeniedFields(string body)
    {
        Assert.DoesNotContain("strategyBody", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("totpSecret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recoveryCode", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hmacKey", body, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<int> CountAuditAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminAuditLog.CountAsync();
    }

    private sealed record SeededGraph(
        Participant PartyA,
        Participant PartyB,
        Artifact Artifact,
        Negotiation OpenNegotiation,
        Negotiation ClosedNegotiation,
        Negotiation ExpiredNegotiation,
        Offer OpenOffer,
        Offer AcceptedOffer,
        Offer DeclinedOffer,
        Offer WithdrawnOffer,
        Offer SupersededOffer,
        Offer CancelledOffer,
        Participant DeletedParticipant,
        Artifact DeletedArtifact,
        Negotiation DeletedNegotiation,
        Offer DeletedOffer);

    private async Task<SeededGraph> SeedGraphAsync(string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        var partyA = Participant.Create($"{prefix}-Alpha");
        var partyB = Participant.Create($"{prefix}-Beta");
        var deletedParty = Participant.Create($"{prefix}-DeletedPerson");
        db.Participants.AddRange(partyA, partyB, deletedParty);

        var artifact = Artifact.Create(
            partyA.Sub,
            [SubjectEntity.Create($"{prefix}-RedWidget", "widget-desc")],
            "sell");
        var deletedArtifact = Artifact.Create(
            partyA.Sub,
            [SubjectEntity.Create($"{prefix}-GoneWidget", "gone")],
            "sell");
        db.Artifacts.AddRange(artifact, deletedArtifact);

        var (openN, e1) = Negotiation.Create(artifact.Id, partyA.Sub, partyB.Sub, "sell", "buy");
        var (closedN, e2) = Negotiation.Create(artifact.Id, partyA.Sub, partyB.Sub, "sell", "buy");
        var (expiredN, e3) = Negotiation.Create(
            artifact.Id, partyA.Sub, partyB.Sub, "sell", "buy",
            endsAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        var (deletedN, e4) = Negotiation.Create(artifact.Id, partyA.Sub, partyB.Sub, "sell", "buy");
        Assert.Empty(e1);
        Assert.Empty(e2);
        Assert.Empty(e3);
        Assert.Empty(e4);
        closedN!.Close();
        expiredN!.CheckAndApplyExpiration();
        db.Negotiations.AddRange(openN!, closedN, expiredN, deletedN!);

        var (openO, _) = Offer.Create(openN!.Id, partyA.Sub, partyB.Sub, 100m, "USD", $"{prefix}-open-terms");
        var (acceptedO, _) = Offer.Create(openN.Id, partyB.Sub, partyA.Sub, 200m, "USD", $"{prefix}-accepted-terms");
        acceptedO!.Accept(partyA.Sub);
        var (declinedO, _) = Offer.Create(closedN.Id, partyA.Sub, partyB.Sub, 50m, "USD", $"{prefix}-declined-terms");
        declinedO!.Decline(partyB.Sub);
        var (withdrawnO, _) = Offer.Create(closedN.Id, partyB.Sub, partyA.Sub, 75m, "USD", $"{prefix}-withdrawn-terms");
        withdrawnO!.Withdraw(partyB.Sub);
        var (supersededO, _) = Offer.Create(expiredN.Id, partyA.Sub, partyB.Sub, 30m, "USD", $"{prefix}-superseded-terms");
        supersededO!.Supersede();
        var (cancelledO, _) = Offer.Create(expiredN.Id, partyB.Sub, partyA.Sub, 40m, "USD", $"{prefix}-cancelled-terms");
        cancelledO!.Cancel();
        var (deletedO, _) = Offer.Create(deletedN!.Id, partyA.Sub, partyB.Sub, 9m, "USD", $"{prefix}-deleted-terms");
        db.Offers.AddRange(openO!, acceptedO, declinedO, withdrawnO, supersededO, cancelledO, deletedO!);

        var (secretStrategy, strategyErrors) = Strategy.Create(partyA.Sub, $"{prefix}-strat", $"{prefix}-SECRET-STRATEGY-BODY");
        Assert.Empty(strategyErrors);
        db.Strategies.Add(secretStrategy!);

        await db.SaveChangesAsync();

        await db.Participants.Where(p => p.Id == deletedParty.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, DateTimeOffset.UtcNow));
        await db.Artifacts.Where(a => a.Id == deletedArtifact.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.DeletedAt, DateTimeOffset.UtcNow));
        await db.Negotiations.Where(n => n.Id == deletedN.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, DateTimeOffset.UtcNow));
        await db.Offers.Where(o => o.Id == deletedO!.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.DeletedAt, DateTimeOffset.UtcNow));

        return new SeededGraph(
            partyA, partyB, artifact, openN, closedN, expiredN,
            openO!, acceptedO, declinedO, withdrawnO, supersededO, cancelledO,
            deletedParty, deletedArtifact, deletedN, deletedO!);
    }

    [Theory]
    [InlineData("/admin/api/participants")]
    [InlineData("/admin/api/artifacts")]
    [InlineData("/admin/api/negotiations")]
    [InlineData("/admin/api/offers")]
    [InlineData("/admin/api/stats")]
    public async Task TD_ADM_003_MissingSession_DeniesNewReadRoutes(string path)
    {
        var client = CreateClient();
        using var response = await client.SendAsync(AdminGet(path));
        var body = await response.Content.ReadAsStringAsync();
        AssertSafeUnauthorized(response, body);
    }

    [Theory]
    [InlineData("/admin/api/participants")]
    [InlineData("/admin/api/artifacts")]
    [InlineData("/admin/api/negotiations")]
    [InlineData("/admin/api/offers")]
    [InlineData("/admin/api/stats")]
    public async Task TD_ADM_003_ForgedCookie_DeniesNewReadRoutes(string path)
    {
        var client = CreateClient();
        using var response = await client.SendAsync(AdminGet(path, Guid.NewGuid()));
        var body = await response.Content.ReadAsStringAsync();
        AssertSafeUnauthorized(response, body);
    }

    [Theory]
    [InlineData("/admin/api/participants")]
    [InlineData("/admin/api/stats")]
    public async Task TD_ADM_003_ExpiredSession_DeniesNewReadRoutes(string path)
    {
        var id = await SeedSessionAsync(createdAt: DateTimeOffset.UtcNow.AddHours(-9));
        var client = CreateClient();
        using var response = await client.SendAsync(AdminGet(path, id));
        var body = await response.Content.ReadAsStringAsync();
        AssertSafeUnauthorized(response, body);
    }

    [Fact]
    public async Task TD_ADM_003_MissingSession_DeniesDetailRoutes()
    {
        var client = CreateClient();
        var id = Guid.NewGuid();
        foreach (var path in new[]
                 {
                     $"/admin/api/participants/{id:D}",
                     $"/admin/api/artifacts/{id:D}",
                     $"/admin/api/negotiations/{id:D}",
                     $"/admin/api/offers/{id:D}"
                 })
        {
            using var response = await client.SendAsync(AdminGet(path));
            var body = await response.Content.ReadAsStringAsync();
            AssertSafeUnauthorized(response, body);
        }
    }

    [Fact]
    public async Task TD_ADM_006_ParticipantsList_HasNoHumanUserSurface()
    {
        var prefix = $"p6-{Guid.NewGuid():N}";
        var graph = await SeedGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();

        using var list = await client.SendAsync(AdminGet($"/admin/api/participants?q={prefix}-Alpha", session));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var payload = await ReadJson(list);
        Assert.True(payload.TryGetProperty("items", out var items));
        Assert.True(items.GetArrayLength() >= 1);
        var first = items[0];
        Assert.Equal(graph.PartyA.Id.ToString(), first.GetProperty("id").GetGuid().ToString());
        Assert.False(payload.TryGetProperty("users", out _));
        Assert.False(payload.TryGetProperty("humanUsers", out _));
        Assert.False(first.TryGetProperty("password", out _));

        using var users = await client.SendAsync(AdminGet("/admin/api/users", session));
        using var humans = await client.SendAsync(AdminGet("/admin/api/registered-users", session));
        Assert.Equal(HttpStatusCode.NotFound, users.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, humans.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_060_Participants_ListViewAndSearch()
    {
        var prefix = $"p60-{Guid.NewGuid():N}";
        var graph = await SeedGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();

        using var list = await client.SendAsync(AdminGet($"/admin/api/participants?q={prefix}-Alpha", session));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listJson = await ReadJson(list);
        Assert.Equal(50, listJson.GetProperty("limit").GetInt32());
        Assert.Equal(0, listJson.GetProperty("offset").GetInt32());
        Assert.True(listJson.GetProperty("total").GetInt32() >= 1);
        var listed = listJson.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(listed, i => i.GetProperty("displayName").GetString() == $"{prefix}-Alpha");
        Assert.DoesNotContain(listed, i => i.GetProperty("displayName").GetString() == $"{prefix}-DeletedPerson");

        using var caseSearch = await client.SendAsync(
            AdminGet($"/admin/api/participants?q={prefix}-aLpHa", session));
        var caseJson = await ReadJson(caseSearch);
        Assert.Contains(
            caseJson.GetProperty("items").EnumerateArray(),
            i => i.GetProperty("id").GetGuid() == graph.PartyA.Id);

        using var detail = await client.SendAsync(
            AdminGet($"/admin/api/participants/{graph.PartyA.Id:D}", session));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var detailJson = await ReadJson(detail);
        Assert.Equal($"{prefix}-Alpha", detailJson.GetProperty("displayName").GetString());
        Assert.True(detailJson.TryGetProperty("version", out _));
        Assert.False(listJson.GetProperty("items")[0].TryGetProperty("version", out _));
        AssertNoDeniedFields(await detail.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TD_ADM_061_Artifacts_ListViewAndSearch()
    {
        var prefix = $"p61-{Guid.NewGuid():N}";
        var graph = await SeedGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();

        using var list = await client.SendAsync(AdminGet($"/admin/api/artifacts?q={prefix}-redwidget", session));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listJson = await ReadJson(list);
        Assert.Contains(
            listJson.GetProperty("items").EnumerateArray(),
            i => i.GetProperty("id").GetGuid() == graph.Artifact.Id);
        Assert.DoesNotContain(
            listJson.GetProperty("items").EnumerateArray(),
            i => i.GetProperty("id").GetGuid() == graph.DeletedArtifact.Id);

        using var detail = await client.SendAsync(
            AdminGet($"/admin/api/artifacts/{graph.Artifact.Id:D}", session));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var detailJson = await ReadJson(detail);
        Assert.Equal($"{prefix}-RedWidget", detailJson.GetProperty("name").GetString());
        Assert.True(detailJson.TryGetProperty("version", out _));
        AssertNoDeniedFields(await detail.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TD_ADM_062_Negotiations_AllStatusesSortFilterPageSearch()
    {
        var prefix = $"p62-{Guid.NewGuid():N}";
        var graph = await SeedGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();

        using var list = await client.SendAsync(
            AdminGet($"/admin/api/negotiations?q={prefix}-RedWidget&sort=created&dir=asc", session));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listJson = await ReadJson(list);
        var items = listJson.GetProperty("items").EnumerateArray().ToList();
        var statuses = items.Select(i => i.GetProperty("status").GetString()).ToHashSet();
        Assert.Contains("Open", statuses);
        Assert.Contains("Closed", statuses);
        Assert.Contains("Expired", statuses);
        Assert.DoesNotContain("Withdrawn", statuses);
        Assert.DoesNotContain("Accepted", statuses);
        Assert.DoesNotContain("Declined", statuses);
        Assert.DoesNotContain(items, i => i.GetProperty("id").GetGuid() == graph.DeletedNegotiation.Id);

        using var byStatus = await client.SendAsync(
            AdminGet("/admin/api/negotiations?status=Closed&sort=status", session));
        var statusJson = await ReadJson(byStatus);
        Assert.All(
            statusJson.GetProperty("items").EnumerateArray(),
            i => Assert.Equal("Closed", i.GetProperty("status").GetString()));

        using var byParticipant = await client.SendAsync(
            AdminGet($"/admin/api/negotiations?participant={Uri.EscapeDataString(graph.PartyA.Sub)}&q={prefix}-Alpha", session));
        Assert.Equal(HttpStatusCode.OK, byParticipant.StatusCode);

        using var byArtifact = await client.SendAsync(
            AdminGet($"/admin/api/negotiations?artifact={graph.Artifact.Id:D}&sort=artifact", session));
        Assert.Equal(HttpStatusCode.OK, byArtifact.StatusCode);

        using var byId = await client.SendAsync(
            AdminGet($"/admin/api/negotiations?negotiationId={graph.OpenNegotiation.Id:D}&sort=id", session));
        var byIdJson = await ReadJson(byId);
        Assert.Equal(1, byIdJson.GetProperty("total").GetInt32());

        using var amountSort = await client.SendAsync(
            AdminGet("/admin/api/negotiations?sort=amount", session));
        Assert.Equal(HttpStatusCode.BadRequest, amountSort.StatusCode);
        AssertNoDeniedFields(await amountSort.Content.ReadAsStringAsync());

        using var detail = await client.SendAsync(
            AdminGet($"/admin/api/negotiations/{graph.OpenNegotiation.Id:D}", session));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.True((await ReadJson(detail)).TryGetProperty("version", out _));
    }

    [Fact]
    public async Task TD_ADM_063_Offers_AllStatusesSortFilterPageSearch()
    {
        var prefix = $"p63-{Guid.NewGuid():N}";
        var graph = await SeedGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();

        using var list = await client.SendAsync(
            AdminGet($"/admin/api/offers?q={prefix}-RedWidget&sort=amount&dir=asc", session));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var items = (await ReadJson(list)).GetProperty("items").EnumerateArray().ToList();
        var statuses = items.Select(i => i.GetProperty("status").GetString()).ToHashSet();
        Assert.Contains("Open", statuses);
        Assert.Contains("Accepted", statuses);
        Assert.Contains("Declined", statuses);
        Assert.Contains("Withdrawn", statuses);
        Assert.Contains("Superseded", statuses);
        Assert.Contains("Cancelled", statuses);
        Assert.DoesNotContain(items, i => i.GetProperty("id").GetGuid() == graph.DeletedOffer.Id);

        using var byAmount = await client.SendAsync(
            AdminGet("/admin/api/offers?amountMin=100&amountMax=200&sort=price", session));
        Assert.Equal(HttpStatusCode.OK, byAmount.StatusCode);

        using var byNegotiation = await client.SendAsync(
            AdminGet($"/admin/api/offers?negotiationId={graph.OpenNegotiation.Id:D}&q={graph.OpenNegotiation.Id:D}", session));
        Assert.Equal(HttpStatusCode.OK, byNegotiation.StatusCode);
        var negItems = (await ReadJson(byNegotiation)).GetProperty("items").EnumerateArray().ToList();
        Assert.All(negItems, i => Assert.Equal(graph.OpenNegotiation.Id, i.GetProperty("negotiationId").GetGuid()));

        using var byOffering = await client.SendAsync(
            AdminGet($"/admin/api/offers?q={prefix}-Alpha&sort=participant", session));
        Assert.Equal(HttpStatusCode.OK, byOffering.StatusCode);

        using var detail = await client.SendAsync(
            AdminGet($"/admin/api/offers/{graph.WithdrawnOffer.Id:D}", session));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var detailJson = await ReadJson(detail);
        Assert.Equal("Withdrawn", detailJson.GetProperty("status").GetString());
        Assert.True(detailJson.TryGetProperty("version", out _));
    }

    [Fact]
    public async Task TD_ADM_064_IncludeDeleted_IsReadAndNotAudited()
    {
        var prefix = $"p64-{Guid.NewGuid():N}";
        var graph = await SeedGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        var before = await CountAuditAsync();

        using var hidden = await client.SendAsync(
            AdminGet($"/admin/api/participants?q={prefix}-DeletedPerson", session));
        var hiddenJson = await ReadJson(hidden);
        Assert.DoesNotContain(
            hiddenJson.GetProperty("items").EnumerateArray(),
            i => i.GetProperty("id").GetGuid() == graph.DeletedParticipant.Id);

        using var shown = await client.SendAsync(
            AdminGet($"/admin/api/participants?q={prefix}-DeletedPerson&includeDeleted=true", session));
        var shownJson = await ReadJson(shown);
        Assert.Contains(
            shownJson.GetProperty("items").EnumerateArray(),
            i => i.GetProperty("id").GetGuid() == graph.DeletedParticipant.Id
                 && i.GetProperty("deletedAt").ValueKind != JsonValueKind.Null);

        foreach (var path in new[]
                 {
                     $"/admin/api/artifacts?includeDeleted=true&q={prefix}-GoneWidget",
                     $"/admin/api/negotiations?includeDeleted=true&negotiationId={graph.DeletedNegotiation.Id:D}",
                     $"/admin/api/offers?includeDeleted=true&q={prefix}-deleted-terms"
                 })
        {
            using var response = await client.SendAsync(AdminGet(path, session));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True((await ReadJson(response)).GetProperty("total").GetInt32() >= 1);
        }

        var after = await CountAuditAsync();
        Assert.Equal(before, after);

        var other = await SeedSessionAsync(email: "other@example.com");
        using var denied = await client.SendAsync(
            AdminGet("/admin/api/participants?includeDeleted=true", other));
        var deniedBody = await denied.Content.ReadAsStringAsync();
        AssertSafeUnauthorized(denied, deniedBody);
    }

    [Fact]
    public async Task TD_ADM_065_Paging_Default50_Max200_Clamped()
    {
        var prefix = $"p65-{Guid.NewGuid():N}";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            for (var i = 0; i < 201; i++)
                db.Participants.Add(Participant.Create($"{prefix}-{i:D3}"));
            await db.SaveChangesAsync();
        }

        var session = await SeedSessionAsync();
        var client = CreateClient();

        using var def = await client.SendAsync(AdminGet($"/admin/api/participants?q={prefix}-", session));
        var defJson = await ReadJson(def);
        Assert.Equal(50, defJson.GetProperty("limit").GetInt32());
        Assert.Equal(50, defJson.GetProperty("items").GetArrayLength());
        Assert.True(defJson.GetProperty("total").GetInt32() >= 201);

        using var twoHundred = await client.SendAsync(
            AdminGet($"/admin/api/participants?q={prefix}-&limit=200", session));
        var twoJson = await ReadJson(twoHundred);
        Assert.Equal(200, twoJson.GetProperty("limit").GetInt32());
        Assert.Equal(200, twoJson.GetProperty("items").GetArrayLength());
        Assert.True(twoJson.GetProperty("total").GetInt32() >= 201);

        using var clamped = await client.SendAsync(
            AdminGet($"/admin/api/participants?q={prefix}-&limit=201", session));
        var clampJson = await ReadJson(clamped);
        Assert.Equal(HttpStatusCode.OK, clamped.StatusCode);
        Assert.Equal(200, clampJson.GetProperty("limit").GetInt32());
        Assert.Equal(200, clampJson.GetProperty("items").GetArrayLength());

        using var huge = await client.SendAsync(
            AdminGet($"/admin/api/participants?q={prefix}-&limit=10000", session));
        var hugeJson = await ReadJson(huge);
        Assert.Equal(200, hugeJson.GetProperty("limit").GetInt32());
        Assert.True(hugeJson.GetProperty("items").GetArrayLength() <= 200);

        using var page2 = await client.SendAsync(
            AdminGet($"/admin/api/participants?q={prefix}-&limit=50&offset=50", session));
        var page2Json = await ReadJson(page2);
        Assert.Equal(50, page2Json.GetProperty("offset").GetInt32());
        Assert.Equal(50, page2Json.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task TD_ADM_065_NegativeOrNonNumericLimit_Safe400()
    {
        var session = await SeedSessionAsync();
        var client = CreateClient();

        foreach (var path in new[]
                 {
                     "/admin/api/participants?limit=-1",
                     "/admin/api/participants?limit=abc",
                     "/admin/api/offers?offset=-4",
                     "/admin/api/artifacts?offset=nope"
                 })
        {
            using var response = await client.SendAsync(AdminGet(path, session));
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("\"error\":\"BadRequest\"", body);
            AssertNoDeniedFields(body);
            Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task TD_ADM_066_InjectionInSortFilterSearch_IsSafe()
    {
        var prefix = $"p66-{Guid.NewGuid():N}";
        await SeedGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();

        using var unknownSort = await client.SendAsync(
            AdminGet("/admin/api/participants?sort=created;DROP%20TABLE%20Participants", session));
        var unknownBody = await unknownSort.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, unknownSort.StatusCode);
        Assert.Contains("\"error\":\"BadRequest\"", unknownBody);
        AssertNoDeniedFields(unknownBody);

        using var qInject = await client.SendAsync(
            AdminGet("/admin/api/participants?q='+OR+1%3D1--", session));
        Assert.Equal(HttpStatusCode.OK, qInject.StatusCode);
        var qBody = await qInject.Content.ReadAsStringAsync();
        AssertNoDeniedFields(qBody);
        Assert.DoesNotContain("SQLITE_ERROR", qBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", qBody, StringComparison.OrdinalIgnoreCase);

        using var union = await client.SendAsync(
            AdminGet("/admin/api/offers?q=1%20UNION%20SELECT%20password%20FROM%20Participants", session));
        Assert.Equal(HttpStatusCode.OK, union.StatusCode);
        AssertNoDeniedFields(await union.Content.ReadAsStringAsync());

        using var commentSort = await client.SendAsync(
            AdminGet("/admin/api/negotiations?sort=created%23", session));
        Assert.Equal(HttpStatusCode.BadRequest, commentSort.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_005_DeniedFieldsAbsentFromListDetailSearch()
    {
        var prefix = $"p05-{Guid.NewGuid():N}";
        var graph = await SeedGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();

        var secret = $"{prefix}-SECRET-STRATEGY-BODY";
        using var search = await client.SendAsync(
            AdminGet($"/admin/api/participants?q={secret}", session));
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        var searchJson = await ReadJson(search);
        Assert.Equal(0, searchJson.GetProperty("total").GetInt32());

        using var list = await client.SendAsync(AdminGet($"/admin/api/participants?q={prefix}-Alpha", session));
        using var detail = await client.SendAsync(
            AdminGet($"/admin/api/participants/{graph.PartyA.Id:D}", session));
        using var artifacts = await client.SendAsync(AdminGet($"/admin/api/artifacts?q={prefix}-RedWidget", session));
        foreach (var response in new[] { list, detail, artifacts, search })
            AssertNoDeniedFields(await response.Content.ReadAsStringAsync());

        var detailJson = await ReadJson(detail);
        Assert.False(detailJson.TryGetProperty("strategyBody", out _));
        Assert.False(detailJson.TryGetProperty("passwordHash", out _));
    }

    [Fact]
    public async Task TD_ADM_070_Stats_ExcludeSoftDeleted()
    {
        var session = await SeedSessionAsync();
        var client = CreateClient();

        using var beforeResponse = await client.SendAsync(AdminGet("/admin/api/stats", session));
        Assert.Equal(HttpStatusCode.OK, beforeResponse.StatusCode);
        var before = await ReadJson(beforeResponse);
        var p0 = before.GetProperty("participants").GetInt32();
        var n0 = before.GetProperty("openNegotiations").GetInt32();
        var o0 = before.GetProperty("offers").GetInt32();
        var a0 = before.GetProperty("accepts").GetInt32();
        var d0 = before.GetProperty("declines").GetInt32();

        var prefix = $"p70-{Guid.NewGuid():N}";
        var graph = await SeedGraphAsync(prefix);

        using var afterAdd = await client.SendAsync(AdminGet("/admin/api/stats", session));
        var after = await ReadJson(afterAdd);
        Assert.Equal(p0 + 2, after.GetProperty("participants").GetInt32());
        Assert.Equal(n0 + 1, after.GetProperty("openNegotiations").GetInt32());
        Assert.Equal(o0 + 6, after.GetProperty("offers").GetInt32());
        Assert.Equal(a0 + 1, after.GetProperty("accepts").GetInt32());
        Assert.Equal(d0 + 1, after.GetProperty("declines").GetInt32());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            await db.Participants.Where(p => p.Id == graph.PartyA.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, DateTimeOffset.UtcNow));
            await db.Negotiations.Where(n => n.Id == graph.OpenNegotiation.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, DateTimeOffset.UtcNow));
            await db.Offers.Where(o => o.Id == graph.AcceptedOffer.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.DeletedAt, DateTimeOffset.UtcNow));
            await db.Offers.Where(o => o.Id == graph.DeclinedOffer.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.DeletedAt, DateTimeOffset.UtcNow));
        }

        using var afterDelete = await client.SendAsync(AdminGet("/admin/api/stats", session));
        var gone = await ReadJson(afterDelete);
        Assert.Equal(p0 + 1, gone.GetProperty("participants").GetInt32());
        Assert.Equal(n0, gone.GetProperty("openNegotiations").GetInt32());
        Assert.Equal(o0 + 4, gone.GetProperty("offers").GetInt32());
        Assert.Equal(a0, gone.GetProperty("accepts").GetInt32());
        Assert.Equal(d0, gone.GetProperty("declines").GetInt32());
        Assert.False(gone.TryGetProperty("charts", out _));
        Assert.False(gone.TryGetProperty("warehouse", out _));

        using var charts = await client.SendAsync(AdminGet("/admin/api/stats/charts", session));
        Assert.Equal(HttpStatusCode.NotFound, charts.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_140_ApiEmptyStates_AreSafe()
    {
        var session = await SeedSessionAsync();
        var client = CreateClient();
        var miss = $"nomatch-{Guid.NewGuid():N}";

        foreach (var path in new[]
                 {
                     $"/admin/api/participants?q={miss}",
                     $"/admin/api/artifacts?q={miss}",
                     $"/admin/api/negotiations?q={miss}",
                     $"/admin/api/offers?q={miss}"
                 })
        {
            using var response = await client.SendAsync(AdminGet(path, session));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await ReadJson(response);
            Assert.Equal(0, json.GetProperty("total").GetInt32());
            Assert.Equal(0, json.GetProperty("items").GetArrayLength());
            Assert.Equal(50, json.GetProperty("limit").GetInt32());
            var body = await response.Content.ReadAsStringAsync();
            AssertNoDeniedFields(body);
            Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
        }

        using var missing = await client.SendAsync(
            AdminGet($"/admin/api/participants/{Guid.NewGuid():D}", session));
        var missingBody = await missing.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Contains("\"error\":\"NotFound\"", missingBody);
        AssertNoDeniedFields(missingBody);
    }

    [Fact]
    public async Task TD_ADM_062_UpdatedSort_FallsBackToCreatedAt()
    {
        var prefix = $"p62u-{Guid.NewGuid():N}";
        var older = Participant.Create($"{prefix}-Old");
        var newer = Participant.Create($"{prefix}-New");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            db.Participants.AddRange(older, newer);
            await db.SaveChangesAsync();
            var t0 = DateTimeOffset.UtcNow.AddDays(-2);
            var t1 = DateTimeOffset.UtcNow.AddDays(-1);
            await db.Participants.Where(p => p.Id == older.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.CreatedAt, t0)
                    .SetProperty(p => p.UpdatedAt, (DateTimeOffset?)null));
            await db.Participants.Where(p => p.Id == newer.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.CreatedAt, t1)
                    .SetProperty(p => p.UpdatedAt, DateTimeOffset.UtcNow));
        }

        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var response = await client.SendAsync(
            AdminGet($"/admin/api/participants?q={prefix}-&sort=updated&dir=desc", session));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ids = (await ReadJson(response)).GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("id").GetGuid())
            .ToList();
        Assert.True(ids.IndexOf(newer.Id) < ids.IndexOf(older.Id));
    }

    [Fact(Skip = "TD-ADM-130 is a UI smoke case. Deferred to the UI PR after UX1 approval.")]
    public void TD_ADM_130_UiSmoke_DeferredToUiPrAfterUx1()
    {
    }
}
