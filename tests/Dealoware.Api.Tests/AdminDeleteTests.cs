using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

public sealed class MutableAdminClock : IAdminClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AdminDeleteFactory : IsolatedWebApplicationFactory
{
    public MutableAdminClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IAdminClock>(Clock);
        });
    }
}

public sealed class FailingAdminAuditRepository : IAdminAuditRepository
{
    public Task AddAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("audit insert failed");

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class FailingAuditAdminDeleteFactory : IsolatedWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddScoped<IAdminAuditRepository, FailingAdminAuditRepository>();
        });
    }
}

public sealed class AdminDeleteGraph
{
    public required Participant Alice { get; init; }
    public required Participant Bob { get; init; }
    public required Artifact Lamp { get; init; }
    public required Artifact Lonely { get; init; }
    public required Artifact Blocked { get; init; }
    public required Negotiation OpenNeg { get; init; }
    public required Negotiation BlockNeg { get; init; }
    public required Offer Offer { get; init; }
    public required Offer SecondOffer { get; init; }
}

[Collection("AdminDeleteTests")]
public class AdminDeleteTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";
    private readonly AdminDeleteFactory _factory;

    public AdminDeleteTests(AdminDeleteFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private async Task<Guid> SeedSessionAsync(string email = CoreOwnerEmail)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(email, "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private async Task<AdminDeleteGraph> SeedGraphAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var alice = Participant.Create($"Alice-{Guid.NewGuid():N}");
        var bob = Participant.Create($"Bob-{Guid.NewGuid():N}");
        var lamp = Artifact.Create(alice.Sub, [SubjectEntity.Create($"Lamp-{Guid.NewGuid():N}", "desc")], "sell");
        var lonely = Artifact.Create(bob.Sub, [SubjectEntity.Create($"Lonely-{Guid.NewGuid():N}", "desc")], "sell");
        var blocked = Artifact.Create(alice.Sub, [SubjectEntity.Create($"Blocked-{Guid.NewGuid():N}", "desc")], "sell");
        var (openNeg, e1) = Negotiation.Create(lamp.Id, alice.Sub, bob.Sub, "sell", "buy");
        var (blockNeg, e2) = Negotiation.Create(blocked.Id, alice.Sub, bob.Sub, "sell", "buy");
        Assert.Empty(e1);
        Assert.Empty(e2);
        Assert.NotNull(openNeg);
        Assert.NotNull(blockNeg);
        var (offer, e3) = Offer.Create(openNeg.Id, alice.Sub, bob.Sub, 11m, "USD", "terms");
        offer!.Supersede();
        var (second, e4) = Offer.Create(openNeg.Id, bob.Sub, alice.Sub, 12m, "USD", "counter");
        Assert.Empty(e3);
        Assert.Empty(e4);
        Assert.NotNull(second);
        db.Participants.AddRange(alice, bob);
        db.Artifacts.AddRange(lamp, lonely, blocked);
        db.Negotiations.AddRange(openNeg, blockNeg);
        db.Offers.AddRange(offer, second);
        await db.SaveChangesAsync();
        return new AdminDeleteGraph
        {
            Alice = alice,
            Bob = bob,
            Lamp = lamp,
            Lonely = lonely,
            Blocked = blocked,
            OpenNeg = openNeg,
            BlockNeg = blockNeg,
            Offer = offer,
            SecondOffer = second
        };
    }

    private static HttpRequestMessage AdminRequest(
        HttpMethod method,
        string path,
        Guid? sessionId,
        string? token = null,
        string? ifMatch = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        if (sessionId is { } id)
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={id:D}");
        if (token is not null)
            request.Headers.TryAddWithoutValidation(AdminDeleteService.ConfirmTokenHeader, token);
        if (ifMatch is not null)
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        return request;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

    [Fact]
    public async Task TD_ADM_003_IntentAndDelete_DenyWithoutSession()
    {
        var client = CreateClient();
        var id = Guid.NewGuid();
        using var intent = await client.SendAsync(
            AdminRequest(HttpMethod.Post, $"/admin/api/offers/{id}/delete-intent", null));
        using var del = await client.SendAsync(
            AdminRequest(HttpMethod.Delete, $"/admin/api/offers/{id}", null));
        Assert.Equal(HttpStatusCode.Unauthorized, intent.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, del.StatusCode);
        var body = await intent.Content.ReadAsStringAsync();
        Assert.Contains("Unauthorized", body);
        Assert.DoesNotContain(CoreOwnerEmail, body);
    }

    [Fact]
    public async Task TD_ADM_090_BlindDeleteRejected_IntentConfirmReplay()
    {
        var graph = await SeedGraphAsync();
        var session = await SeedSessionAsync();
        var client = CreateClient();

        using var blind = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/offers/{graph.SecondOffer.Id}", session, ifMatch: "\"0\""));
        Assert.Equal(HttpStatusCode.BadRequest, blind.StatusCode);

        using var intent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/offers/{graph.SecondOffer.Id}/delete-intent", session));
        Assert.Equal(HttpStatusCode.OK, intent.StatusCode);
        var payload = await ReadJson(intent);
        var token = payload.GetProperty("confirmToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal("offers", payload.GetProperty("entityType").GetString());
        Assert.Equal(1, payload.GetProperty("cascade").GetProperty("offers").GetInt32());
        Assert.Equal(0, payload.GetProperty("cascade").GetProperty("artifacts").GetInt32());

        using var confirm = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/offers/{graph.SecondOffer.Id}", session, token, "\"0\""));
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        using var replay = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/offers/{graph.SecondOffer.Id}", session, token, "\"1\""));
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var offer = await db.Offers.FindAsync(graph.SecondOffer.Id);
        Assert.NotNull(offer!.DeletedAt);
        var parent = await db.Negotiations.FindAsync(graph.OpenNeg.Id);
        Assert.Null(parent!.DeletedAt);
        Assert.Equal(1, db.AdminAuditLog.Count(a => a.Action == "Delete" && a.EntityId == graph.SecondOffer.Id));
    }

    [Fact]
    public async Task TD_ADM_090_CancelNeverConfirm_NoAudit()
    {
        var graph = await SeedGraphAsync();
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var intent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/offers/{graph.Offer.Id}/delete-intent", session));
        Assert.Equal(HttpStatusCode.OK, intent.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Null((await db.Offers.FindAsync(graph.Offer.Id))!.DeletedAt);
        Assert.Equal(0, db.AdminAuditLog.Count(a => a.Action == "Delete" && a.EntityId == graph.Offer.Id));
    }

    [Fact]
    public async Task TD_ADM_090_ExpiredWrongActorWrongEntityChangedCascade()
    {
        var graph = await SeedGraphAsync();
        var sessionA = await SeedSessionAsync();
        var sessionB = await SeedSessionAsync();
        var client = CreateClient();

        using var intent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/negotiations/{graph.OpenNeg.Id}/delete-intent", sessionA));
        var token = (await ReadJson(intent)).GetProperty("confirmToken").GetString();

        _factory.Clock.UtcNow = _factory.Clock.UtcNow.AddMinutes(6);
        using var expired = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/negotiations/{graph.OpenNeg.Id}", sessionA, token, "\"0\""));
        Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);
        _factory.Clock.UtcNow = DateTimeOffset.UtcNow;

        using var intent2 = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/negotiations/{graph.OpenNeg.Id}/delete-intent", sessionA));
        var token2 = (await ReadJson(intent2)).GetProperty("confirmToken").GetString();
        using var stolen = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/negotiations/{graph.OpenNeg.Id}", sessionB, token2, "\"0\""));
        Assert.Equal(HttpStatusCode.BadRequest, stolen.StatusCode);

        using var wrongEntity = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/negotiations/{graph.BlockNeg.Id}", sessionA, token2, "\"0\""));
        Assert.Equal(HttpStatusCode.BadRequest, wrongEntity.StatusCode);

        using var childIntent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/offers/{graph.SecondOffer.Id}/delete-intent", sessionA));
        var childToken = (await ReadJson(childIntent)).GetProperty("confirmToken").GetString();
        using var childDel = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/offers/{graph.SecondOffer.Id}", sessionA, childToken, "\"0\""));
        Assert.Equal(HttpStatusCode.NoContent, childDel.StatusCode);

        using var drifted = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/negotiations/{graph.OpenNeg.Id}", sessionA, token2, "\"0\""));
        Assert.Equal(HttpStatusCode.BadRequest, drifted.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Null((await db.Negotiations.FindAsync(graph.OpenNeg.Id))!.DeletedAt);
    }

    [Fact]
    public async Task TD_ADM_090_IfMatch_Missing428_Stale409()
    {
        var graph = await SeedGraphAsync();
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var intent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/offers/{graph.SecondOffer.Id}/delete-intent", session));
        var token = (await ReadJson(intent)).GetProperty("confirmToken").GetString();

        using var missing = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/offers/{graph.SecondOffer.Id}", session, token));
        Assert.Equal((HttpStatusCode)428, missing.StatusCode);

        using var stale = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/offers/{graph.SecondOffer.Id}", session, token, "\"99\""));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Null((await db.Offers.FindAsync(graph.SecondOffer.Id))!.DeletedAt);
    }

    [Fact]
    public async Task TD_ADM_091_SoftDelete_NoHardDelete()
    {
        var graph = await SeedGraphAsync();
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var intent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/artifacts/{graph.Lonely.Id}/delete-intent", session));
        var token = (await ReadJson(intent)).GetProperty("confirmToken").GetString();
        using var del = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/artifacts/{graph.Lonely.Id}", session, token, "\"0\""));
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var row = await db.Artifacts.FindAsync(graph.Lonely.Id);
        Assert.NotNull(row);
        Assert.NotNull(row.DeletedAt);
        Assert.Equal(1u, row.Version);
        using var hard = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/artifacts/{graph.Lonely.Id}/hard", session));
        Assert.Equal(HttpStatusCode.NotFound, hard.StatusCode);
        using var restore = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"/admin/api/artifacts/{graph.Lonely.Id}/restore")
        {
            Headers = { { "Host", AdminHost }, { "Cookie", $"{AdminSessionCookie.Name}={session:D}" } }
        });
        Assert.Equal(HttpStatusCode.NotFound, restore.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_092_OfferDelete_LeavesParent()
    {
        var graph = await SeedGraphAsync();
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var intent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/offers/{graph.SecondOffer.Id}/delete-intent", session));
        var token = (await ReadJson(intent)).GetProperty("confirmToken").GetString();
        using var del = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/offers/{graph.SecondOffer.Id}", session, token, "\"0\""));
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.NotNull((await db.Offers.FindAsync(graph.SecondOffer.Id))!.DeletedAt);
        Assert.Null((await db.Negotiations.FindAsync(graph.OpenNeg.Id))!.DeletedAt);
        Assert.Null((await db.Artifacts.FindAsync(graph.Lamp.Id))!.DeletedAt);
    }

    [Fact]
    public async Task TD_ADM_093_NegotiationCascade_OpenWarning_SharedCorrelation()
    {
        var graph = await SeedGraphAsync();
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var intent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/negotiations/{graph.OpenNeg.Id}/delete-intent", session));
        var payload = await ReadJson(intent);
        Assert.True(payload.GetProperty("isOpen").GetBoolean());
        Assert.Equal(2, payload.GetProperty("cascade").GetProperty("offers").GetInt32());
        var token = payload.GetProperty("confirmToken").GetString();
        using var del = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/negotiations/{graph.OpenNeg.Id}", session, token, "\"0\""));
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.NotNull((await db.Negotiations.FindAsync(graph.OpenNeg.Id))!.DeletedAt);
        Assert.NotNull((await db.Offers.FindAsync(graph.Offer.Id))!.DeletedAt);
        Assert.NotNull((await db.Offers.FindAsync(graph.SecondOffer.Id))!.DeletedAt);
        Assert.Null((await db.Artifacts.FindAsync(graph.Lamp.Id))!.DeletedAt);
        var rows = db.AdminAuditLog.Where(a => a.Action == "Delete" &&
            (a.EntityId == graph.OpenNeg.Id || a.EntityId == graph.Offer.Id || a.EntityId == graph.SecondOffer.Id)).ToList();
        Assert.Equal(3, rows.Count);
        Assert.Single(rows.Select(r => r.CorrelationId).Distinct());
        Assert.Equal(0, db.Negotiations.Count(n => n.DeletedAt == null && n.Status == NegotiationStatus.Open && n.Id == graph.OpenNeg.Id));
    }

    [Fact]
    public async Task TD_ADM_094_ArtifactBlocked_UntilReferencesGone()
    {
        var graph = await SeedGraphAsync();
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var blocked = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/artifacts/{graph.Blocked.Id}/delete-intent", session));
        var payload = await ReadJson(blocked);
        Assert.True(payload.GetProperty("blocked").GetBoolean());
        Assert.Contains(graph.BlockNeg.Id.ToString(), payload.GetProperty("blockedByNegotiations").ToString());

        using var negIntent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/negotiations/{graph.BlockNeg.Id}/delete-intent", session));
        var negToken = (await ReadJson(negIntent)).GetProperty("confirmToken").GetString();
        using var negDel = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/negotiations/{graph.BlockNeg.Id}", session, negToken, "\"0\""));
        Assert.Equal(HttpStatusCode.NoContent, negDel.StatusCode);

        using var retry = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/artifacts/{graph.Blocked.Id}/delete-intent", session));
        var retryPayload = await ReadJson(retry);
        Assert.False(retryPayload.GetProperty("blocked").GetBoolean());
        var token = retryPayload.GetProperty("confirmToken").GetString();
        using var del = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/artifacts/{graph.Blocked.Id}", session, token, "\"0\""));
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_095_ParticipantCascade_NeverArtifacts()
    {
        var graph = await SeedGraphAsync();
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var intent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/participants/{graph.Alice.Id}/delete-intent", session));
        var payload = await ReadJson(intent);
        Assert.True(payload.GetProperty("cascade").GetProperty("negotiations").GetInt32() >= 1);
        Assert.Equal(0, payload.GetProperty("cascade").GetProperty("artifacts").GetInt32());
        var token = payload.GetProperty("confirmToken").GetString();
        using var del = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/participants/{graph.Alice.Id}", session, token, "\"0\""));
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.NotNull((await db.Participants.FindAsync(graph.Alice.Id))!.DeletedAt);
        Assert.NotNull((await db.Negotiations.FindAsync(graph.OpenNeg.Id))!.DeletedAt);
        Assert.Null((await db.Artifacts.FindAsync(graph.Lamp.Id))!.DeletedAt);
        Assert.Null((await db.Artifacts.FindAsync(graph.Blocked.Id))!.DeletedAt);
        using var humans = await client.SendAsync(AdminRequest(HttpMethod.Delete, "/admin/api/users/1", session, "x", "\"0\""));
        Assert.Equal(HttpStatusCode.NotFound, humans.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_060_061_ParticipantAndArtifactDelete()
    {
        var graph = await SeedGraphAsync();
        var session = await SeedSessionAsync();
        var client = CreateClient();

        using var pIntent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/participants/{graph.Bob.Id}/delete-intent", session));
        var pToken = (await ReadJson(pIntent)).GetProperty("confirmToken").GetString();
        using var pDel = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/participants/{graph.Bob.Id}", session, pToken, "\"0\""));
        Assert.Equal(HttpStatusCode.NoContent, pDel.StatusCode);

        using var aIntent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/artifacts/{graph.Lonely.Id}/delete-intent", session));
        var aToken = (await ReadJson(aIntent)).GetProperty("confirmToken").GetString();
        using var aDel = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/artifacts/{graph.Lonely.Id}", session, aToken, "\"0\""));
        Assert.Equal(HttpStatusCode.NoContent, aDel.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_064_070_ListHidesDeleted_StatsDrop()
    {
        var graph = await SeedGraphAsync();
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var before = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/delete-ui/stats", session));
        var beforeStats = await ReadJson(before);
        var openBefore = beforeStats.GetProperty("openNegotiations").GetInt32();

        using var intent = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/admin/api/negotiations/{graph.OpenNeg.Id}/delete-intent", session));
        var token = (await ReadJson(intent)).GetProperty("confirmToken").GetString();
        using var del = await client.SendAsync(AdminRequest(
            HttpMethod.Delete, $"/admin/api/negotiations/{graph.OpenNeg.Id}", session, token, "\"0\""));
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        using var hidden = await client.SendAsync(AdminRequest(
            HttpMethod.Get, "/admin/api/delete-ui/negotiations", session));
        var hiddenBody = await hidden.Content.ReadAsStringAsync();
        Assert.DoesNotContain(graph.OpenNeg.Id.ToString("D"), hiddenBody);

        using var shown = await client.SendAsync(AdminRequest(
            HttpMethod.Get, "/admin/api/delete-ui/negotiations?includeDeleted=true", session));
        var shownBody = await shown.Content.ReadAsStringAsync();
        Assert.Contains(graph.OpenNeg.Id.ToString("D"), shownBody);

        using var after = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/delete-ui/stats", session));
        var afterStats = await ReadJson(after);
        Assert.Equal(openBefore - 1, afterStats.GetProperty("openNegotiations").GetInt32());
    }

    [Fact]
    public async Task TD_ADM_130_SignedInShell_HasNoEntityData()
    {
        var graph = await SeedGraphAsync();
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var page = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/participants", session));
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("Participants", html);
        Assert.DoesNotContain(graph.Alice.DisplayName!, html);
        Assert.DoesNotContain(graph.Alice.Id.ToString("D"), html);
        Assert.Contains("/admin/assets/admin-delete.js", html);
        var cache = page.Headers.CacheControl?.ToString()
                    ?? page.Headers.GetValues("Cache-Control").FirstOrDefault()
                    ?? string.Empty;
        Assert.Contains("no-store", cache);
    }

    [Fact]
    public async Task SignedInAssets_RequireSession_AuthStaticMayLoadSignedOut()
    {
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var anonJs = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/assets/admin-delete.js", null));
        Assert.Equal(HttpStatusCode.Unauthorized, anonJs.StatusCode);

        using var authJs = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/assets/admin-delete.js", session));
        Assert.Equal(HttpStatusCode.OK, authJs.StatusCode);
        var js = await authJs.Content.ReadAsStringAsync();
        Assert.Contains("delete-dialog", js);

        using var authStatic = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/auth/sign-in", null));
        Assert.NotEqual(HttpStatusCode.Unauthorized, authStatic.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_131_UnauthenticatedAdminPages_DenyNoData()
    {
        var client = CreateClient();
        using var page = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/participants", null));
        Assert.Equal(HttpStatusCode.Unauthorized, page.StatusCode);
        var body = await page.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Alice", body);
        Assert.DoesNotContain("confirmToken", body);
    }

    [Fact]
    public void TD_ADM_ReturnPath_UsesAdminPrefix_RejectsTopLevel()
    {
        Assert.True(AdminPageRoutes.IsAllowedReturnPath("/admin"));
        Assert.True(AdminPageRoutes.IsAllowedReturnPath("/admin/participants"));
        Assert.True(AdminPageRoutes.IsAllowedReturnPath($"/admin/offers/{Guid.NewGuid():D}"));
        Assert.False(AdminPageRoutes.IsAllowedReturnPath("/participants"));
        Assert.False(AdminPageRoutes.IsAllowedReturnPath("/offers"));
        Assert.False(AdminPageRoutes.IsAllowedReturnPath("https://evil.example/admin"));
        Assert.Equal("/admin", AdminSessionCookie.CreateOptions().Path);
    }
}

[CollectionDefinition("AdminDeleteTests")]
public class AdminDeleteTestCollection : ICollectionFixture<AdminDeleteFactory>;

public class AdminDeleteAuditRollbackTests
{
    [Fact]
    public async Task TD_ADM_102_AuditFailure_RollsBackDelete()
    {
        using var factory = new FailingAuditAdminDeleteFactory();
        var client = factory.CreateClient();
        Guid sessionId;
        Guid offerId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var alice = Participant.Create("Audit Alice");
            var bob = Participant.Create("Audit Bob");
            var artifact = Artifact.Create(alice.Sub, [SubjectEntity.Create("Audit lamp", "d")], "sell");
            var (neg, _) = Negotiation.Create(artifact.Id, alice.Sub, bob.Sub, "sell", "buy");
            var (offer, _) = Offer.Create(neg!.Id, alice.Sub, bob.Sub, 1m, "USD", "t");
            db.Participants.AddRange(alice, bob);
            db.Artifacts.Add(artifact);
            db.Negotiations.Add(neg);
            db.Offers.Add(offer!);
            var session = AdminSession.Create("io@aiknowhow.com", "testhmac-not-an-ip");
            session.MarkTotpVerified();
            db.AdminSessions.Add(session);
            await db.SaveChangesAsync();
            sessionId = session.Id;
            offerId = offer!.Id;
        }

        var intentReq = new HttpRequestMessage(HttpMethod.Post, $"/admin/api/offers/{offerId}/delete-intent");
        intentReq.Headers.Host = "admin.core.dealoware.com";
        intentReq.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={sessionId:D}");
        using var intent = await client.SendAsync(intentReq);
        var token = (await intent.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("confirmToken").GetString();

        var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/admin/api/offers/{offerId}");
        delReq.Headers.Host = "admin.core.dealoware.com";
        delReq.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={sessionId:D}");
        delReq.Headers.TryAddWithoutValidation(AdminDeleteService.ConfirmTokenHeader, token);
        delReq.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var del = await client.SendAsync(delReq);
        Assert.Equal(HttpStatusCode.InternalServerError, del.StatusCode);

        using var scope2 = factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Null((await db2.Offers.FindAsync(offerId))!.DeletedAt);
        Assert.Equal(0, db2.AdminAuditLog.Count(a => a.Action == "Delete"));
    }
}

public class AdminDeleteAssetGateTests
{
    [Fact]
    public void SignedInAdminAssets_AreNotAnonymousWwwrootFiles()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        var adminWww = Path.Combine(dir.FullName, "src", "Dealoware.Api", "wwwroot", "admin");
        if (!Directory.Exists(adminWww))
            return;
        foreach (var file in Directory.GetFiles(adminWww, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(adminWww, file).Replace('\\', '/');
            Assert.StartsWith("auth/", rel, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task UiTestSeedRoute_Production_Returns404()
    {
        using var factory = new EnvironmentWebApplicationFactory(
            "Production",
            EnvironmentWebApplicationFactory.TestSigningKey64);
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, AdminUiTestSeedOptions.Route);
        request.Headers.Host = "admin.core.dealoware.com";
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("confirmToken", body);
        Assert.DoesNotContain("sessionId", body);
    }

    [Fact]
    public void UiTestSeedFlag_Production_IsRefused()
    {
        using var factory = new ProductionSeedEnabledFactory();
        var ex = Record.Exception(() => factory.CreateClient());
        Assert.NotNull(ex);
        Assert.Contains(AdminUiTestSeedGuard.RefusedMessage, Flatten(ex));
    }

    private static string Flatten(Exception ex)
    {
        var parts = new List<string>();
        for (var current = ex; current is not null; current = current.InnerException)
            parts.Add(current.Message);
        return string.Join(" ", parts);
    }

    [Fact]
    public void FrozenSpecs_DoNotIncludeConfirmTokenTable()
    {
        Assert.DoesNotContain("AdminDeleteConfirmTokens", DatabaseMigrationBaseline.BaselineTableNames);
        Assert.DoesNotContain("AdminDeleteConfirmTokens", DatabaseMigrationBaseline.Pre20TableNames);
        var schema = File.ReadAllText(Path.Combine(FindRoot(), "src", "Dealoware.Infrastructure", "Persistence", "BaselineSchema.cs"));
        Assert.DoesNotContain("AdminDeleteConfirmTokens", schema);
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root");
    }
}

public sealed class ProductionSeedEnabledFactory : EnvironmentWebApplicationFactory
{
    public ProductionSeedEnabledFactory()
        : base("Production", TestSigningKey64)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Admin:UiTestSeed:Enabled", "true");
    }
}
