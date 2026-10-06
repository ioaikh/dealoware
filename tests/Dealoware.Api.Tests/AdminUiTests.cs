using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// A7 Step 7 admin UI shell (route-prefix note r3, sha b079a814).
/// HTML is a data-free shell; lists load through /admin/api after the session gate.
/// </summary>
[Collection("WebAppTests")]
public class AdminUiTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminUiTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(bool allowRedirect = true)
        => _factory.CreateClient(new() { AllowAutoRedirect = allowRedirect });

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

    private async Task<Participant> SeedNamedParticipantAsync(string name, bool active = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var party = Participant.Create(name);
        if (!active)
            party.Deactivate();
        db.Participants.Add(party);
        await db.SaveChangesAsync();
        return party;
    }

    private static HttpRequestMessage AdminReq(HttpMethod method, string path, Guid? sessionId = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        if (sessionId is { } id)
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={id:D}");
        return request;
    }

    private static void AssertNoSecretsOrDeniedFields(string body)
    {
        Assert.DoesNotContain("strategyBody", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("totpSecret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recoveryCode", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hmacKey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(CoreOwnerEmail, body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/admin/")]
    [InlineData("/admin/participants")]
    [InlineData("/admin/artifacts")]
    [InlineData("/admin/negotiations")]
    [InlineData("/admin/offers")]
    [InlineData("/admin/ui/admin.js")]
    [InlineData("/admin/ui/admin.css")]
    public async Task TD_ADM_131_SignedInShell_RequiresSession(string path)
    {
        var client = CreateClient();
        using var response = await client.SendAsync(AdminReq(HttpMethod.Get, path));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"error\":\"Unauthorized\"", body);
        AssertNoSecretsOrDeniedFields(body);
    }

    [Fact]
    public async Task RouteNoteR3_AdminWithoutSlash_RedirectsToCanonicalStats_WhenSignedIn()
    {
        var session = await SeedSessionAsync();
        var client = CreateClient(allowRedirect: false);
        using var response = await client.SendAsync(AdminReq(HttpMethod.Get, "/admin", session));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AdminUiRoutes.Stats, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task RouteNoteR3_AdminWithoutSlash_Unsigned_StillRedirectsToCanonical()
    {
        var client = CreateClient(allowRedirect: false);
        using var response = await client.SendAsync(AdminReq(HttpMethod.Get, "/admin"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AdminUiRoutes.Stats, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task TD_ADM_130_CanonicalStatsShell_HasNoEntityData()
    {
        var marker = $"UniqueShellPerson-{Guid.NewGuid():N}";
        var party = await SeedNamedParticipantAsync(marker);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var response = await client.SendAsync(AdminReq(HttpMethod.Get, "/admin/", session));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/admin/ui/admin.js", html);
        Assert.Contains("/admin/ui/admin.css", html);
        Assert.Contains("href=\"/admin/\"", html);
        Assert.Contains("href=\"/admin/participants\"", html);
        Assert.DoesNotContain(marker, html);
        Assert.DoesNotContain(party.Id.ToString("D"), html);
        Assert.DoesNotContain("Withdrawn", html);
        AssertNoSecretsOrDeniedFields(html);
    }

    [Theory]
    [InlineData("/admin/participants")]
    [InlineData("/admin/artifacts")]
    [InlineData("/admin/negotiations")]
    [InlineData("/admin/offers")]
    public async Task TD_ADM_131_ListHtml_IsDataFreeShell(string path)
    {
        var marker = $"ListShell-{Guid.NewGuid():N}";
        var party = await SeedNamedParticipantAsync(marker);
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var response = await client.SendAsync(AdminReq(HttpMethod.Get, path, session));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(marker, html);
        Assert.DoesNotContain(party.Id.ToString("D"), html);
        AssertNoSecretsOrDeniedFields(html);
    }

    [Fact]
    public async Task RouteNoteR3_SignedInAssets_AfterSessionGate()
    {
        var session = await SeedSessionAsync();
        var client = CreateClient();

        using var js = await client.SendAsync(AdminReq(HttpMethod.Get, "/admin/ui/admin.js", session));
        Assert.Equal(HttpStatusCode.OK, js.StatusCode);
        var script = await js.Content.ReadAsStringAsync();
        Assert.Contains("Open", script);
        Assert.Contains("Closed", script);
        Assert.Contains("Expired", script);
        Assert.Contains("Withdrawn", script);
        Assert.DoesNotContain("statuses: [\"Open\", \"Closed\", \"Expired\", \"Withdrawn\"]", script);

        using var css = await client.SendAsync(AdminReq(HttpMethod.Get, "/admin/ui/admin.css", session));
        Assert.Equal(HttpStatusCode.OK, css.StatusCode);
        var styles = await css.Content.ReadAsStringAsync();
        Assert.Contains(":focus-visible", styles);
    }

    [Fact]
    public async Task RouteNoteR3_SignedOutAuthFolder_DoesNotServeSignedInShell()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(AdminReq(HttpMethod.Get, "/admin/auth/admin.js"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("function", body);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RouteNoteR3_Wwwroot_HasNoSignedInAdminAssets()
    {
        var roots = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "admin"),
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "admin"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Dealoware.Api", "wwwroot", "admin")
        };
        foreach (var root in roots.Where(Directory.Exists))
        {
            var forbidden = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Where(p => !p.Replace('\\', '/').Contains("/admin/auth/", StringComparison.OrdinalIgnoreCase));
            Assert.Empty(forbidden);
        }
    }

    [Fact]
    public async Task TD_ADM_UI_na_X01_TopLevelAdminPages_Are404OnAdminHost()
    {
        var session = await SeedSessionAsync();
        var client = CreateClient();
        foreach (var path in new[] { "/participants", "/artifacts", "/negotiations", "/offers", "/stats" })
        {
            using var response = await client.SendAsync(AdminReq(HttpMethod.Get, path, session));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task RouteNoteR3_PostSignOut_IsIdempotentWithoutSession()
    {
        var client = CreateClient(allowRedirect: false);
        using var response = await client.SendAsync(AdminReq(HttpMethod.Post, "/admin/sign-out"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AdminUiRoutes.Stats, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task RouteNoteR3_GetSignOut_DoesNotClearAndRequiresSession()
    {
        var client = CreateClient();
        using var response = await client.SendAsync(AdminReq(HttpMethod.Get, "/admin/sign-out"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RouteNoteR3_ReturnPathAllowlist_UsesCanonicalStats()
    {
        Assert.Contains("/admin/", AdminUiRoutes.ReturnPathAllowlist);
        Assert.DoesNotContain("/admin", AdminUiRoutes.ReturnPathAllowlist);
        Assert.Equal("/admin/", AdminUiRoutes.Stats);
    }

    [Fact]
    public async Task ParticipantStatusFilter_ActiveAndSuspended()
    {
        var prefix = $"st-{Guid.NewGuid():N}";
        await SeedNamedParticipantAsync($"{prefix}-On");
        await SeedNamedParticipantAsync($"{prefix}-Off", active: false);
        var session = await SeedSessionAsync();
        var client = CreateClient();

        using var active = await client.SendAsync(
            AdminReq(HttpMethod.Get, $"/admin/api/participants?q={prefix}&status=Active", session));
        Assert.Equal(HttpStatusCode.OK, active.StatusCode);
        var activeJson = await active.Content.ReadFromJsonAsync<JsonElement>();
        var activeNames = activeJson.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("displayName").GetString()).ToList();
        Assert.Contains($"{prefix}-On", activeNames);
        Assert.DoesNotContain($"{prefix}-Off", activeNames);

        using var suspended = await client.SendAsync(
            AdminReq(HttpMethod.Get, $"/admin/api/participants?q={prefix}&status=Suspended", session));
        var suspendedJson = await suspended.Content.ReadFromJsonAsync<JsonElement>();
        var suspendedNames = suspendedJson.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("displayName").GetString()).ToList();
        Assert.Contains($"{prefix}-Off", suspendedNames);
        Assert.DoesNotContain($"{prefix}-On", suspendedNames);

        using var bad = await client.SendAsync(
            AdminReq(HttpMethod.Get, $"/admin/api/participants?status=Withdrawn", session));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task TD_ADM_062_NegotiationStatus_Withdrawn_IsBadRequest()
    {
        var session = await SeedSessionAsync();
        var client = CreateClient();
        using var response = await client.SendAsync(
            AdminReq(HttpMethod.Get, "/admin/api/negotiations?status=Withdrawn", session));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
