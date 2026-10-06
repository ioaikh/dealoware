using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-053 / 064 / 101: GET /admin/api/audit is CoreOwner-only, paged,
/// append-only, and never returns raw IPs or secrets. No admin UI in this PR.
/// </summary>
[Collection("WebAppTests")]
public class AdminAuditApiTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";
    private const string ClientIp = "203.0.113.77";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminAuditApiTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private async Task<Guid> SeedSessionAsync(
        string email = CoreOwnerEmail,
        bool totpVerified = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(email, ipHmac: "testhmac-not-an-ip");
        if (totpVerified)
            session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private async Task<AdminAuditEntry> SeedAuthAsync(string action, string? reason = null, DateTimeOffset? at = null)
    {
        using var scope = _factory.Services.CreateScope();
        var recorder = scope.ServiceProvider.GetRequiredService<IAdminAuditRecorder>();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var entry = recorder.Auth(action, CoreOwnerEmail, ClientIp, reason, at);
        db.AdminAuditLog.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    private static HttpRequestMessage AdminRequest(HttpMethod method, string path, Guid? sessionId = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        if (sessionId is { } id)
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={id:D}");
        return request;
    }

    private static async Task AssertGenericUnauthorized(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"error\":\"Unauthorized\"", body);
        Assert.DoesNotContain(CoreOwnerEmail, body);
        Assert.DoesNotContain("TOTP", body, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssertGenericBadRequest(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"error\":\"Bad request\"", body);
        Assert.DoesNotContain(ClientIp, body);
    }

    [Fact]
    public async Task TdAdm101_GetAudit_RequiresVerifiedCoreOwner()
    {
        var client = CreateClient();
        using var missing = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/audit"));
        await AssertGenericUnauthorized(missing);

        var unverified = await SeedSessionAsync(totpVerified: false);
        using var totp = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/audit", unverified));
        await AssertGenericUnauthorized(totp);

        var other = await SeedSessionAsync(email: "other@example.com");
        using var denied = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/audit", other));
        await AssertGenericUnauthorized(denied);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, denied.StatusCode);
    }

    [Fact]
    public async Task TdAdm101_GetAudit_WrongHost_Returns404()
    {
        var id = await SeedSessionAsync();
        var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/api/audit");
        request.Headers.Host = "core.dealoware.com";
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={id:D}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TdAdm101_PatchAndDeleteAudit_AreRejected()
    {
        var id = await SeedSessionAsync();
        var client = CreateClient();
        using var patch = await client.SendAsync(AdminRequest(HttpMethod.Patch, "/admin/api/audit", id));
        using var delete = await client.SendAsync(AdminRequest(HttpMethod.Delete, "/admin/api/audit", id));
        using var put = await client.SendAsync(AdminRequest(HttpMethod.Put, "/admin/api/audit", id));
        Assert.Equal(HttpStatusCode.MethodNotAllowed, patch.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, delete.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, put.StatusCode);
    }

    [Fact]
    public async Task TdAdm101_CoreOwnerCanReadAudit_NoRawIpOrSecrets()
    {
        var seeded = await SeedAuthAsync(AdminAuditActions.LoginFailure, AdminAuditActions.ReasonBadPassword);
        var id = await SeedSessionAsync();
        var client = CreateClient();
        using var response = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/audit", id));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ClientIp, body);
        Assert.DoesNotContain("totpSecret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recoveryCode", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"ip\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("rawIp", body, StringComparison.OrdinalIgnoreCase);

        var payload = JsonDocument.Parse(body).RootElement;
        Assert.True(payload.TryGetProperty("items", out var items));
        Assert.True(payload.TryGetProperty("total", out var total));
        Assert.True(total.GetInt32() >= 1);
        Assert.Equal(0, payload.GetProperty("offset").GetInt32());
        Assert.Equal(50, payload.GetProperty("limit").GetInt32());

        var match = items.EnumerateArray().First(e => e.GetProperty("id").GetGuid() == seeded.Id);
        Assert.Equal(AdminAuditActions.LoginFailure, match.GetProperty("action").GetString());
        Assert.Equal(AdminAuditActions.ReasonBadPassword, match.GetProperty("reasonClass").GetString());
        var prefix = match.GetProperty("ipHmacPrefix").GetString();
        Assert.False(string.IsNullOrWhiteSpace(prefix));
        Assert.True(prefix!.Length <= AdminAuditEndpoints.IpHmacPrefixLength);
        Assert.DoesNotContain(ClientIp, prefix);
        Assert.False(match.TryGetProperty("ipHmac", out _));
        Assert.False(match.TryGetProperty("ip", out _));
    }

    [Fact]
    public async Task TdAdm101_OversizedSnapshot_IsTruncatedWithLengthAndHash()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var recorder = scope.ServiceProvider.GetRequiredService<IAdminAuditRecorder>();
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var huge = "{\"displayName\":\"" + new string('x', 5000) + "\"}";
            var entry = recorder.Entity(
                AdminAuditActions.EntityEdit,
                CoreOwnerEmail,
                ClientIp,
                "Participant",
                Guid.NewGuid(),
                beforeSnapshot: huge,
                afterSnapshot: huge);
            db.AdminAuditLog.Add(entry);
            await db.SaveChangesAsync();
            Assert.Contains("\"originalLength\":", entry.BeforeSnapshot);
            Assert.Contains("\"sha256\":", entry.BeforeSnapshot);
            Assert.Contains("_invalid", entry.BeforeSnapshot);
            Assert.DoesNotContain(new string('x', 32), entry.BeforeSnapshot);
        }

        var id = await SeedSessionAsync();
        var client = CreateClient();
        using var response = await client.SendAsync(
            AdminRequest(HttpMethod.Get, "/admin/api/audit?action=entity_edit", id));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("originalLength", body);
        Assert.Contains("sha256", body);
        Assert.Contains("_invalid", body);
        Assert.DoesNotContain(new string('x', 32), body);
        Assert.DoesNotContain(ClientIp, body);
    }

    [Fact]
    public async Task TdAdm053_AuditApiNeverEchoesRawIp()
    {
        await SeedAuthAsync(AdminAuditActions.LoginSuccess);
        var id = await SeedSessionAsync();
        var client = CreateClient();
        using var response = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/audit", id));
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("203.0.113", body);
        Assert.DoesNotContain(ClientIp, body);
    }

    [Fact]
    public async Task TdAdm064_IncludeDeletedToggle_IsNotAudited()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var before = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(db.AdminAuditLog);

        var id = await SeedSessionAsync();
        var client = CreateClient();
        using var response = await client.SendAsync(
            AdminRequest(HttpMethod.Get, "/admin/api/audit?includeDeleted=true", id));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var after = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(db.AdminAuditLog);
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task TdAdm101_DefaultLimit50_ClampsAt200()
    {
        var id = await SeedSessionAsync();
        var client = CreateClient();
        using var def = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/audit", id));
        var defPayload = await def.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(50, defPayload.GetProperty("limit").GetInt32());

        using var clamped = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/audit?limit=500", id));
        var clampedPayload = await clamped.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, clampedPayload.GetProperty("limit").GetInt32());
    }

    [Fact]
    public async Task TdAdm101_InvalidPagingAndOverlongFilters_ReturnGeneric400()
    {
        var id = await SeedSessionAsync();
        var client = CreateClient();
        using var negative = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/audit?limit=-1", id));
        await AssertGenericBadRequest(negative);

        using var badSort = await client.SendAsync(AdminRequest(HttpMethod.Get, "/admin/api/audit?sort=ip", id));
        await AssertGenericBadRequest(badSort);

        var longQ = new string('a', 101);
        using var q = await client.SendAsync(AdminRequest(HttpMethod.Get, $"/admin/api/audit?q={longQ}", id));
        await AssertGenericBadRequest(q);

        var longActor = new string('b', 201);
        using var actor = await client.SendAsync(
            AdminRequest(HttpMethod.Get, $"/admin/api/audit?actorEmail={longActor}", id));
        await AssertGenericBadRequest(actor);
    }

    [Fact]
    public async Task TdAdm101_FilterAndSortStayInSqlResult()
    {
        var older = await SeedAuthAsync(
            AdminAuditActions.LoginSuccess,
            at: DateTimeOffset.UtcNow.AddMinutes(-2));
        var newer = await SeedAuthAsync(
            AdminAuditActions.LoginFailure,
            AdminAuditActions.ReasonLocked,
            at: DateTimeOffset.UtcNow);
        var id = await SeedSessionAsync();
        var client = CreateClient();
        using var response = await client.SendAsync(
            AdminRequest(HttpMethod.Get, $"/admin/api/audit?action={AdminAuditActions.LoginFailure}&sort=-timestamp", id));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = payload.GetProperty("items").EnumerateArray().ToList();
        Assert.All(items, item =>
            Assert.Equal(AdminAuditActions.LoginFailure, item.GetProperty("action").GetString()));
        Assert.Contains(items, item => item.GetProperty("id").GetGuid() == newer.Id);
        Assert.DoesNotContain(items, item => item.GetProperty("id").GetGuid() == older.Id);
    }
}
