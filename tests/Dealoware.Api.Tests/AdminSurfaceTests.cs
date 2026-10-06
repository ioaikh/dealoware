using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// Host gating and fail-closed admin session (TD-ADM-001..007, cookie flags).
/// Backend only — TD-ADM-131 UI smoke waits for the UI/UX gate.
/// </summary>
[Collection("WebAppTests")]
public class AdminSurfaceTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminSurfaceTests(IsolatedWebApplicationFactory factory)
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
        {
            session.MarkTotpVerified();
        }

        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private static HttpRequestMessage AdminGet(string path, string host, Guid? sessionId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = host;
        if (sessionId is { } id)
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={id:D}");
        }

        return request;
    }

    private static async Task AssertGenericUnauthorized(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"error\":\"Unauthorized\"", body);
        Assert.DoesNotContain(CoreOwnerEmail, body);
        Assert.DoesNotContain("TOTP", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expired", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not found", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("participant:", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TdAdm001_AdminHost_AdminMe_IsNot404()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(AdminGet("/admin/api/me", AdminHost));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        await AssertGenericUnauthorized(response);
    }

    [Fact]
    public async Task TdAdm001_NonAdminHost_AdminMe_Returns404()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(AdminGet("/admin/api/me", "core.dealoware.com"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(CoreOwnerEmail, body);
        Assert.DoesNotContain("Unauthorized", body);
    }

    [Fact]
    public async Task TdAdm001_AdminHost_DoesNotServeNonAdminRoutes()
    {
        var client = CreateClient();
        using var auth = await client.SendAsync(AdminGet("/auth/register", AdminHost));
        using var artifacts = await client.SendAsync(AdminGet("/artifacts", AdminHost));
        Assert.Equal(HttpStatusCode.NotFound, auth.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, artifacts.StatusCode);
    }

    [Fact]
    public async Task TdAdm001_Health_OkOnAdminAndNonAdminHosts()
    {
        var client = CreateClient();
        using var adminHealth = await client.SendAsync(AdminGet("/health", AdminHost));
        using var coreHealth = await client.SendAsync(AdminGet("/health", "localhost"));
        Assert.Equal(HttpStatusCode.OK, adminHealth.StatusCode);
        Assert.Equal(HttpStatusCode.OK, coreHealth.StatusCode);
    }

    [Fact]
    public async Task TdAdm003_MissingSession_DeniesAdminMe()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(AdminGet("/admin/api/me", AdminHost));
        await AssertGenericUnauthorized(response);
    }

    [Fact]
    public async Task TdAdm003_ForgedCookie_DeniesAdminMe()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(AdminGet("/admin/api/me", AdminHost, Guid.NewGuid()));
        await AssertGenericUnauthorized(response);
    }

    [Fact]
    public async Task TdAdm003_ExpiredAbsoluteSession_DeniesAdminMe()
    {
        var id = await SeedSessionAsync(createdAt: DateTimeOffset.UtcNow.AddHours(-9));
        var client = CreateClient();
        using var response = await client.SendAsync(AdminGet("/admin/api/me", AdminHost, id));
        await AssertGenericUnauthorized(response);
    }

    [Fact]
    public async Task TdAdm003_ExpiredIdleSession_DeniesAdminMe()
    {
        var id = await SeedSessionAsync(createdAt: DateTimeOffset.UtcNow.AddMinutes(-31));
        var client = CreateClient();
        using var response = await client.SendAsync(AdminGet("/admin/api/me", AdminHost, id));
        await AssertGenericUnauthorized(response);
    }

    [Fact]
    public async Task TdAdm004_TotpNotVerified_DeniesAdminMe()
    {
        var id = await SeedSessionAsync(totpVerified: false);
        var client = CreateClient();
        using var response = await client.SendAsync(AdminGet("/admin/api/me", AdminHost, id));
        await AssertGenericUnauthorized(response);
    }

    [Fact]
    public async Task TdAdm002_NonCoreOwnerEmail_DeniesAdminMe()
    {
        var id = await SeedSessionAsync(email: "other@example.com");
        var client = CreateClient();
        using var response = await client.SendAsync(AdminGet("/admin/api/me", AdminHost, id));
        await AssertGenericUnauthorized(response);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("other@example.com", body);
    }

    [Fact]
    public async Task TdAdm002_ValidCoreOwnerSession_ReturnsMeJson()
    {
        var id = await SeedSessionAsync();
        var client = CreateClient();
        using var response = await client.SendAsync(AdminGet("/admin/api/me", AdminHost, id));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CoreOwner", payload.GetProperty("principal").GetString());
        Assert.Equal(CoreOwnerEmail, payload.GetProperty("email").GetString());
    }

    [Fact]
    public async Task TdAdm006_NoHumanUserListEndpoint()
    {
        var id = await SeedSessionAsync();
        var client = CreateClient();
        using var users = await client.SendAsync(AdminGet("/admin/api/users", AdminHost, id));
        using var humans = await client.SendAsync(AdminGet("/admin/api/human-users", AdminHost, id));
        Assert.Equal(HttpStatusCode.NotFound, users.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, humans.StatusCode);
    }

    [Fact]
    public async Task TdAdm007_NoOperatorAclEndpoints()
    {
        var id = await SeedSessionAsync();
        var client = CreateClient();
        using var operators = await client.SendAsync(AdminGet("/admin/api/operators", AdminHost, id));
        using var acl = await client.SendAsync(AdminGet("/admin/api/acl", AdminHost, id));
        Assert.Equal(HttpStatusCode.NotFound, operators.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, acl.StatusCode);
    }

    [Fact]
    public void TdAdm052_CookieOptions_HttpOnlySecureSameSiteStrict()
    {
        var options = AdminSessionCookie.CreateOptions();
        Assert.True(options.HttpOnly);
        Assert.True(options.Secure);
        Assert.Equal(SameSiteMode.Strict, options.SameSite);
        Assert.Equal("/admin", options.Path);
    }

    [Fact]
    public void TdAdm101_AuditRepository_HasNoUpdateOrDelete()
    {
        var methods = typeof(IAdminAuditRepository).GetMethods().Select(m => m.Name).ToHashSet();
        Assert.Contains("AddAsync", methods);
        Assert.DoesNotContain("UpdateAsync", methods);
        Assert.DoesNotContain("DeleteAsync", methods);
        Assert.DoesNotContain("Remove", methods);
    }
}
