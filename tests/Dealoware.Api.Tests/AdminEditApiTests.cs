using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// Step 9 CoreOwner edit API, Expire, If-Match, audit, and session-gated edit UI.
/// </summary>
[Collection("WebAppTests")]
public class AdminEditApiTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminEditApiTests(IsolatedWebApplicationFactory factory)
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

    private static HttpRequestMessage AdminRequest(HttpMethod method, string path, Guid? sessionId = null, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Host = AdminHost;
        if (sessionId is { } id)
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={id:D}");
        return request;
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static string CacheControl(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Cache-Control", out var values))
            return string.Join(",", values).ToLowerInvariant();
        return string.Empty;
    }

    private static void AssertSafeUnauthorized(HttpResponseMessage response, string body)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("\"error\":\"Unauthorized\"", body);
        Assert.DoesNotContain(CoreOwnerEmail, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("strategyBody", body, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertGenericDeny(HttpResponseMessage response, string body)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"error\":\"BadRequest\"", body);
        Assert.DoesNotContain("Withdrawn", body);
        Assert.DoesNotContain("Open", body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(Participant A, Participant B, Artifact Artifact, Negotiation Negotiation, Offer Offer)> SeedOpenGraphAsync(string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var a = Participant.Create($"{prefix}-A");
        var b = Participant.Create($"{prefix}-B");
        db.Participants.AddRange(a, b);
        var artifact = Artifact.Create(a.Sub, [SubjectEntity.Create($"{prefix}-Name", $"{prefix}-Desc")], "sell");
        db.Artifacts.Add(artifact);
        var (n, nErr) = Negotiation.Create(artifact.Id, a.Sub, b.Sub, "sell", "buy", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(2));
        Assert.Empty(nErr);
        db.Negotiations.Add(n!);
        var (o, oErr) = Offer.Create(n!.Id, a.Sub, b.Sub, 10m, "USD", $"{prefix}-terms");
        Assert.Empty(oErr);
        db.Offers.Add(o!);
        await db.SaveChangesAsync();
        return (a, b, artifact, n, o!);
    }

    [Theory]
    [InlineData("/admin/api/participants/{id}")]
    [InlineData("/admin/api/artifacts/{id}")]
    [InlineData("/admin/api/negotiations/{id}")]
    [InlineData("/admin/api/offers/{id}")]
    public async Task TD_ADM_003_MissingSession_DeniesPatch(string template)
    {
        var client = CreateClient();
        var path = template.Replace("{id}", Guid.NewGuid().ToString("D"));
        using var request = AdminRequest(HttpMethod.Patch, path, content: JsonBody(new { }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        AssertSafeUnauthorized(response, body);
    }

    [Theory]
    [InlineData("/admin/participants/{id}/edit")]
    [InlineData("/admin/artifacts/{id}/edit")]
    [InlineData("/admin/negotiations/{id}/edit")]
    [InlineData("/admin/offers/{id}/edit")]
    [InlineData("/admin/ui/edit.js")]
    [InlineData("/admin/ui/edit.css")]
    public async Task TD_ADM_131_UnauthenticatedEditUiAndSignedInAssets_DenyWithNoData(string template)
    {
        var client = CreateClient();
        var path = template.Replace("{id}", Guid.NewGuid().ToString("D"));
        using var response = await client.SendAsync(AdminRequest(HttpMethod.Get, path));
        var body = await response.Content.ReadAsStringAsync();
        AssertSafeUnauthorized(response, body);
        Assert.DoesNotContain("displayName", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Expire negotiation", body);
    }

    [Fact]
    public async Task TD_ADM_131_EditPage_WithSession_HasNoEntityDataInHtml()
    {
        var prefix = $"ui-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var response = await client.SendAsync(
            AdminRequest(HttpMethod.Get, $"/admin/participants/{graph.A.Id:D}/edit", session));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", CacheControl(response));
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Participant · Dealoware admin", html);
        Assert.Contains("/admin/ui/edit.js", html);
        Assert.DoesNotContain(graph.A.DisplayName!, html);
        Assert.DoesNotContain("password", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("type=\"password\"", html);
        Assert.Contains("role=\"dialog\"", html);
        Assert.Contains("Expire negotiation", html);
    }

    [Fact]
    public async Task TD_ADM_131_SignedInShellAssets_RequireSession_AndHaveNoStore()
    {
        var session = await SeedSessionAsync();
        var client = CreateClient();
        foreach (var path in new[] { "/admin/ui/edit.js", "/admin/ui/edit.css" })
        {
            using var response = await client.SendAsync(AdminRequest(HttpMethod.Get, path, session));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("no-store", CacheControl(response));
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task TD_ADM_060_E1_E2_ParticipantActivateAndDisplayName()
    {
        var prefix = $"p-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        graph.A.Deactivate();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var row = await db.Participants.FirstAsync(p => p.Id == graph.A.Id);
            row.Deactivate();
            await db.SaveChangesAsync();
        }

        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/participants/{graph.A.Id:D}",
            session,
            JsonBody(new { displayName = $"{prefix}-Renamed", isActive = true }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ReadJson(response);
        Assert.Equal($"{prefix}-Renamed", payload.GetProperty("displayName").GetString());
        Assert.True(payload.GetProperty("isActive").GetBoolean());
        Assert.True(payload.GetProperty("version").GetUInt32() >= 1);
        Assert.True(payload.TryGetProperty("updatedAt", out var updated) && updated.ValueKind != JsonValueKind.Null);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var saved = await db2.Participants.FirstAsync(p => p.Id == graph.A.Id);
        Assert.True(saved.IsActive);
        Assert.Equal($"{prefix}-Renamed", saved.DisplayName);
        Assert.NotNull(saved.UpdatedAt);
    }

    [Fact]
    public async Task TD_ADM_061_E3_E5_ArtifactNameDescriptionAndOwner()
    {
        var prefix = $"a-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/artifacts/{graph.Artifact.Id:D}",
            session,
            JsonBody(new
            {
                name = $"{prefix}-NewName",
                description = $"{prefix}-NewDesc",
                ownerParticipantId = graph.B.Sub
            }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ReadJson(response);
        Assert.Equal($"{prefix}-NewName", payload.GetProperty("name").GetString());
        Assert.Equal(graph.B.Sub, payload.GetProperty("ownerParticipantId").GetString());
    }

    [Fact]
    public async Task TD_ADM_080_EditWritesAuditBeforeAfter_NoSecrets()
    {
        var prefix = $"aud-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var beforeCount = await CountAuditAsync();
        var client = CreateClient();
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/participants/{graph.A.Id:D}",
            session,
            JsonBody(new { displayName = $"{prefix}-After" }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var afterCount = await db.AdminAuditLog.CountAsync();
        Assert.Equal(beforeCount + 1, afterCount);
        var row = await db.AdminAuditLog.OrderByDescending(a => a.Timestamp).FirstAsync();
        Assert.Equal("Edit", row.Action);
        Assert.Equal("Participant", row.EntityType);
        Assert.Equal(graph.A.Id, row.EntityId);
        Assert.Contains(prefix + "-A", row.BeforeSnapshot);
        Assert.Contains(prefix + "-After", row.AfterSnapshot);
        Assert.DoesNotContain("password", row.BeforeSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", row.AfterSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("loginEmail", row.BeforeSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("totp", row.AfterSnapshot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TD_ADM_081_StaleIfMatch_Returns409_NoOverwrite()
    {
        var prefix = $"c-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/participants/{graph.A.Id:D}",
            session,
            JsonBody(new { displayName = "stale-write" }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"99\"");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("\"error\":\"Conflict\"", body);
        Assert.DoesNotContain("stale-write", body);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var saved = await db.Participants.FirstAsync(p => p.Id == graph.A.Id);
        Assert.Equal($"{prefix}-A", saved.DisplayName);
    }

    [Fact]
    public async Task TD_ADM_081_MissingIfMatch_Returns428()
    {
        var prefix = $"m-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/participants/{graph.A.Id:D}",
            session,
            JsonBody(new { displayName = "no-match" }));
        using var response = await client.SendAsync(request);
        Assert.Equal((HttpStatusCode)428, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("PreconditionRequired", body);
        Assert.DoesNotContain("no-match", body);
    }

    [Fact]
    public async Task TD_ADM_005_DeniedFieldInBody_GenericDeny_NoWrite()
    {
        var prefix = $"d-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/participants/{graph.A.Id:D}",
            session,
            JsonBody(new { displayName = "ok", loginEmail = "secret@example.test", password = "not-a-real-secret" }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        AssertGenericDeny(response, body);
        Assert.DoesNotContain("secret@example.test", body);
        Assert.DoesNotContain("not-a-real-secret", body);
    }

    [Fact]
    public async Task TD_ADM_141_ValidationSafe_DeniedValueNotEchoed()
    {
        var prefix = $"v-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        var tooLong = new string('x', 257);
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/participants/{graph.A.Id:D}",
            session,
            JsonBody(new { displayName = tooLong }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Validation", body);
        Assert.DoesNotContain(tooLong, body);
        Assert.Contains("displayName", body);
    }

    [Fact]
    public async Task TD_ADM_178_Negotiation_OpenToClosed_CancelsOpenOffers()
    {
        var prefix = $"cl-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/negotiations/{graph.Negotiation.Id:D}",
            session,
            JsonBody(new { status = "Closed" }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ReadJson(response);
        Assert.Equal("Closed", payload.GetProperty("status").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var offer = await db.Offers.FirstAsync(o => o.Id == graph.Offer.Id);
        Assert.Equal(OfferStatus.Cancelled, offer.Status);
    }

    [Fact]
    public async Task TD_ADM_178_Negotiation_OpenToExpired_CancelsOpenOffers()
    {
        var prefix = $"ex-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        var extraAccepted = await SeedAcceptedOfferAsync(graph.Negotiation.Id, graph.B.Sub, graph.A.Sub, prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/negotiations/{graph.Negotiation.Id:D}",
            session,
            JsonBody(new { status = "Expired" }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ReadJson(response);
        Assert.Equal("Expired", payload.GetProperty("status").GetString());
        Assert.Equal(0, payload.GetProperty("openOfferCount").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var open = await db.Offers.FirstAsync(o => o.Id == graph.Offer.Id);
        var accepted = await db.Offers.FirstAsync(o => o.Id == extraAccepted);
        Assert.Equal(OfferStatus.Cancelled, open.Status);
        Assert.Equal(OfferStatus.Accepted, accepted.Status);
    }

    [Theory]
    [InlineData("Open")]
    [InlineData("Withdrawn")]
    [InlineData("Accepted")]
    public async Task TD_ADM_178_DisallowedNegotiationTransition_GenericDeny(string status)
    {
        var prefix = $"dn-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var n = await db.Negotiations.FirstAsync(x => x.Id == graph.Negotiation.Id);
            n.Close();
            await db.SaveChangesAsync();
        }

        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/negotiations/{graph.Negotiation.Id:D}",
            session,
            JsonBody(new { status }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        AssertGenericDeny(response, body);
    }

    [Theory]
    [InlineData("Accepted")]
    [InlineData("Declined")]
    [InlineData("Withdrawn")]
    [InlineData("Superseded")]
    public async Task TD_ADM_178_AdminCannotSetOfferStatusExceptCancelled(string status)
    {
        var prefix = $"os-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/offers/{graph.Offer.Id:D}",
            session,
            JsonBody(new { status }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        AssertGenericDeny(response, body);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var offer = await db.Offers.FirstAsync(o => o.Id == graph.Offer.Id);
        Assert.Equal(OfferStatus.Open, offer.Status);
    }

    [Fact]
    public async Task TD_ADM_178_E12_OfferOpenToCancelled()
    {
        var prefix = $"oc-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/offers/{graph.Offer.Id:D}",
            session,
            JsonBody(new { status = "Cancelled" }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Cancelled", (await ReadJson(response)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task TD_ADM_178_E6_E9_EndsAtAndOfferAmount()
    {
        var prefix = $"et-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        var ends = DateTimeOffset.UtcNow.AddDays(5);
        using var nReq = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/negotiations/{graph.Negotiation.Id:D}",
            session,
            JsonBody(new { endsAt = ends }));
        nReq.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var nRes = await client.SendAsync(nReq);
        Assert.Equal(HttpStatusCode.OK, nRes.StatusCode);

        using var oReq = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/offers/{graph.Offer.Id:D}",
            session,
            JsonBody(new { amount = 12.34m, currency = "usd", terms = $"{prefix}-updated" }));
        oReq.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var oRes = await client.SendAsync(oReq);
        Assert.Equal(HttpStatusCode.OK, oRes.StatusCode);
        var offer = await ReadJson(oRes);
        Assert.Equal(12.34m, offer.GetProperty("amount").GetDecimal());
        Assert.Equal("USD", offer.GetProperty("currency").GetString());
    }

    [Fact]
    public async Task TD_ADM_178_OwnerMustBeActiveNonDeleted()
    {
        var prefix = $"ow-{Guid.NewGuid():N}"[..8];
        var graph = await SeedOpenGraphAsync(prefix);
        Guid deletedId;
        string deletedSub;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var deleted = Participant.Create($"{prefix}-Gone");
            db.Participants.Add(deleted);
            await db.SaveChangesAsync();
            deletedId = deleted.Id;
            deletedSub = deleted.Sub;
            await db.Participants.Where(p => p.Id == deletedId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, DateTimeOffset.UtcNow));
        }

        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var request = AdminRequest(
            HttpMethod.Patch,
            $"/admin/api/artifacts/{graph.Artifact.Id:D}",
            session,
            JsonBody(new { ownerParticipantId = deletedSub }));
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        AssertGenericDeny(response, body);
    }

    private async Task<int> CountAuditAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminAuditLog.CountAsync();
    }

    private async Task<Guid> SeedAcceptedOfferAsync(Guid negotiationId, string from, string to, string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var (offer, errors) = Offer.Create(negotiationId, from, to, 1m, "USD", $"{prefix}-accepted");
        Assert.Empty(errors);
        offer!.Accept(to);
        db.Offers.Add(offer);
        await db.SaveChangesAsync();
        return offer.Id;
    }
}

[Collection("WebAppTests")]
public class AdminEditAuditFaultTests
{
    private const string AdminHost = "admin.core.dealoware.com";

    [Fact]
    public async Task TD_ADM_102_AuditInsertFailure_RollsBackEdit()
    {
        using var factory = new FaultingAuditWebApplicationFactory();
        var client = factory.CreateClient();

        Guid participantId;
        Guid sessionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var participant = Participant.Create("fault-before");
            db.Participants.Add(participant);
            var session = AdminSession.Create("io@aiknowhow.com", "testhmac-not-an-ip");
            session.MarkTotpVerified();
            db.AdminSessions.Add(session);
            await db.SaveChangesAsync();
            participantId = participant.Id;
            sessionId = session.Id;
        }

        factory.Latch.ThrowOnAdd = true;
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/admin/api/participants/{participantId:D}")
        {
            Content = new StringContent("""{"displayName":"fault-after"}""", Encoding.UTF8, "application/json")
        };
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={sessionId:D}");
        request.Headers.TryAddWithoutValidation("If-Match", "\"0\"");
        using var response = await client.SendAsync(request);
        Assert.True((int)response.StatusCode >= 400);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var saved = await db.Participants.FirstAsync(p => p.Id == participantId);
            Assert.Equal("fault-before", saved.DisplayName);
            Assert.Equal(0, await db.AdminAuditLog.CountAsync());
        }
    }

    private sealed class AdminAuditFaultLatch
    {
        public bool ThrowOnAdd { get; set; }
    }

    private sealed class FaultingAdminAuditRepository : IAdminAuditRepository
    {
        private readonly IAdminAuditRepository _inner;
        private readonly AdminAuditFaultLatch _latch;

        public FaultingAdminAuditRepository(IAdminAuditRepository inner, AdminAuditFaultLatch latch)
        {
            _inner = inner;
            _latch = latch;
        }

        public Task AddAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
        {
            if (_latch.ThrowOnAdd)
                throw new InvalidOperationException("forced audit failure");
            return _inner.AddAsync(entry, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _inner.SaveChangesAsync(cancellationToken);
    }

    private sealed class FaultingAuditWebApplicationFactory : IsolatedWebApplicationFactory
    {
        public AdminAuditFaultLatch Latch { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(Latch);
                services.AddScoped<IAdminAuditRepository>(sp =>
                    new FaultingAdminAuditRepository(
                        new AdminAuditRepository(sp.GetRequiredService<DealowareDbContext>()),
                        sp.GetRequiredService<AdminAuditFaultLatch>()));
            });
        }
    }
}
