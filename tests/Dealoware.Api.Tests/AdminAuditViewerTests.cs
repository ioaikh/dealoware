using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;

namespace Dealoware.Api.Tests;

/// <summary>
/// Step 15 audit viewer: host/session deny, no-data HTML, no mutate, no secrets/IPs.
/// GET /admin/api/audit is a test-host stub until Step 8 merges.
/// </summary>
[Collection("AdminUiWebAppTests")]
public class AdminAuditViewerTests
{
    private const string AdminHost = AdminUiWebApplicationFactory.AdminHost;
    private readonly AdminUiWebApplicationFactory _factory;

    public AdminAuditViewerTests(AdminUiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TD_ADM_001_AuditViewer_WrongHost_Returns404()
    {
        var id = await _factory.SeedCoreOwnerSessionAsync();
        var client = _factory.CreateAdminClient();
        foreach (var path in new[]
                 {
                     "/admin/audit",
                     $"/admin/audit/{AdminAuditApiStub.EditId:D}",
                     AdminAuditViewerEndpoints.ShellJsPath,
                     AdminAuditViewerEndpoints.ShellCssPath,
                     "/admin/api/audit"
                 })
        {
            using var response = await client.SendAsync(
                AdminUiWebApplicationFactory.AdminGet(path, "core.dealoware.com", id));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(AdminUiWebApplicationFactory.CoreOwnerEmail, body);
            Assert.DoesNotContain("Audit log", body);
        }
    }

    [Fact]
    public async Task TD_ADM_003_TD_ADM_131_AuditViewer_NoSession_Unauthorized_NoEntityData()
    {
        var client = _factory.CreateAdminClient();
        foreach (var path in new[]
                 {
                     "/admin/audit",
                     $"/admin/audit/{AdminAuditApiStub.EditId:D}",
                     AdminAuditViewerEndpoints.ShellJsPath,
                     AdminAuditViewerEndpoints.ShellCssPath,
                     "/admin/api/audit"
                 })
        {
            using var response = await client.SendAsync(
                AdminUiWebApplicationFactory.AdminGet(path, AdminHost));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"error\":\"Unauthorized\"", body);
            Assert.DoesNotContain(AdminUiWebApplicationFactory.CoreOwnerEmail, body);
            Assert.DoesNotContain(AdminAuditApiStub.ExistingParticipantId.ToString(), body);
            Assert.DoesNotContain("Ada Lovelace", body);
            Assert.DoesNotContain("203.0.113.10", body);
        }
    }

    [Fact]
    public async Task TD_ADM_131_SignedInHtml_IsShellWithoutEntityData()
    {
        var id = await _factory.SeedCoreOwnerSessionAsync();
        var client = _factory.CreateAdminClient();
        using var response = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet("/admin/audit", AdminHost, id));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoStore(response);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Audit log · Dealoware admin", html);
        Assert.Contains("data-admin-screen=\"S-D1\"", html);
        Assert.DoesNotContain("Ada Lovelace", html);
        Assert.DoesNotContain(AdminAuditApiStub.ExistingParticipantId.ToString(), html);
        Assert.DoesNotContain("203.0.113.10", html);
        Assert.DoesNotContain("should-never-render", html);
        Assert.DoesNotContain("name=\"q\"", html);
        Assert.DoesNotContain("type=\"search\"", html);
        Assert.DoesNotContain("Edit record", html);
        Assert.DoesNotContain("Delete entry", html);
        Assert.Contains("method=\"post\"", html);
        Assert.Contains("action=\"/admin/sign-out\"", html);
        Assert.DoesNotContain("action=\"/admin/api/audit\"", html);
    }

    [Fact]
    public async Task TD_ADM_101_AuditViewer_ReadOk_NoMutatingControlsOrRoutes()
    {
        var id = await _factory.SeedCoreOwnerSessionAsync();
        var client = _factory.CreateAdminClient();
        using var list = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet("/admin/audit", AdminHost, id));
        using var detail = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet($"/admin/audit/{AdminAuditApiStub.EditId:D}", AdminHost, id));
        using var api = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet("/admin/api/audit", AdminHost, id));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, api.StatusCode);

        foreach (var method in new[] { HttpMethod.Patch, HttpMethod.Delete, HttpMethod.Put, HttpMethod.Post })
        {
            using var mutate = new HttpRequestMessage(method, "/admin/api/audit");
            mutate.Headers.Host = AdminHost;
            mutate.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={id:D}");
            using var mutateResponse = await client.SendAsync(mutate);
            Assert.True(
                mutateResponse.StatusCode is HttpStatusCode.NotFound
                    or HttpStatusCode.MethodNotAllowed
                    or HttpStatusCode.Unauthorized,
                $"{method} /admin/api/audit was {mutateResponse.StatusCode}");
        }

        var js = AdminAuditViewerEndpoints.LoadUiResource("audit.js");
        var listHtml = AdminAuditViewerEndpoints.LoadUiResource("audit-list.html");
        var detailHtml = AdminAuditViewerEndpoints.LoadUiResource("audit-detail.html");
        foreach (var source in new[] { js, listHtml, detailHtml })
        {
            Assert.DoesNotContain("innerHTML", source);
            Assert.DoesNotContain("PATCH", source);
            Assert.DoesNotContain("method=\"delete\"", source);
            Assert.DoesNotContain("contenteditable", source);
        }
    }

    [Fact]
    public async Task TD_ADM_053_TD_ADM_150_ViewerAndStub_NeverExposeSecretsOrRawIp()
    {
        var id = await _factory.SeedCoreOwnerSessionAsync();
        var client = _factory.CreateAdminClient();
        using var api = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet(
                $"/admin/api/audit/{AdminAuditApiStub.EditId:D}",
                AdminHost,
                id));
        Assert.Equal(HttpStatusCode.OK, api.StatusCode);
        var json = await api.Content.ReadAsStringAsync();
        Assert.DoesNotContain("203.0.113.10", json);
        Assert.DoesNotContain("passwordHash", json);
        Assert.DoesNotContain("should-never-render", json);
        Assert.Contains("Ada Lovelace", json);
        Assert.Contains("ipHmacPrefix", json);

        using var list = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet("/admin/api/audit?action=entity_edit", AdminHost, id));
        var listJson = await list.Content.ReadFromJsonAsync<JsonElement>();
        var item = listJson.GetProperty("items")[0];
        Assert.Equal("entity_edit", item.GetProperty("action").GetString());
        Assert.Equal(AdminAuditApiStub.Actor, item.GetProperty("actorEmail").GetString());
        Assert.Equal("a1b2c3d4e5f6", item.GetProperty("ipHmacPrefix").GetString());
        Assert.False(item.TryGetProperty("actor", out _));

        var js = AdminAuditViewerEndpoints.LoadUiResource("audit.js");
        Assert.Contains("SECRET_KEYS", js);
        Assert.Contains("looksLikeIp", js);
        Assert.DoesNotContain("203.0.113.", js);
        Assert.DoesNotContain("DEALOWARE_ADMIN_IP_HMAC_KEY=", js);

        foreach (var file in Directory.GetFiles(
                     Path.Combine(RepoRoot(), "src", "Dealoware.Api", "wwwroot"),
                     "*",
                     SearchOption.AllDirectories))
        {
            Assert.DoesNotContain("audit.js", file, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("audit.css", file, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task TD_ADM_140_AuditApiStub_EmptyFilter_And_PagingContract()
    {
        var id = await _factory.SeedCoreOwnerSessionAsync();
        var client = _factory.CreateAdminClient();
        using var empty = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet(
                "/admin/api/audit?action=totp_change",
                AdminHost,
                id));
        var emptyPayload = await empty.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, emptyPayload.GetProperty("total").GetInt32());
        Assert.Equal(JsonValueKind.Array, emptyPayload.GetProperty("items").ValueKind);
        Assert.Equal(0, emptyPayload.GetProperty("items").GetArrayLength());

        using var page = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet(
                "/admin/api/audit?limit=50&offset=0",
                AdminHost,
                id));
        var pagePayload = await page.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(50, pagePayload.GetProperty("limit").GetInt32());
        Assert.True(pagePayload.GetProperty("total").GetInt32() > 50);
        Assert.Equal(50, pagePayload.GetProperty("items").GetArrayLength());

        using var clamped = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet(
                "/admin/api/audit?limit=10000",
                AdminHost,
                id));
        var clampedPayload = await clamped.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, clampedPayload.GetProperty("limit").GetInt32());
    }

    [Fact]
    public async Task TD_ADM_101_ReturnPaths_AdminAuditAndGuidDetail_AreRecognized()
    {
        var id = await _factory.SeedCoreOwnerSessionAsync();
        var client = _factory.CreateAdminClient();
        using var list = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet("/admin/audit", AdminHost, id));
        using var detail = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet(
                $"/admin/audit/{AdminAuditApiStub.EditId:D}",
                AdminHost,
                id));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        AssertNoStore(list);
        AssertNoStore(detail);
        var detailHtml = await detail.Content.ReadAsStringAsync();
        Assert.Contains("data-admin-screen=\"S-D2\"", detailHtml);
        Assert.Contains("Audit entry · Dealoware admin", detailHtml);

        // Route note r3 (b079a814; r2 void): Spec §5 return paths are the two pages only.
        Assert.True(AdminAuditViewerEndpoints.IsAllowedReturnPath("/admin/audit"));
        Assert.True(AdminAuditViewerEndpoints.IsAllowedReturnPath("/admin/audit/"));
        Assert.True(AdminAuditViewerEndpoints.IsAllowedReturnPath("/admin/audit?action=entity_edit"));
        Assert.True(AdminAuditViewerEndpoints.IsAllowedReturnPath($"/admin/audit/{AdminAuditApiStub.EditId:D}"));
        Assert.True(AdminAuditViewerEndpoints.IsAllowedReturnPath($"/admin/audit/{AdminAuditApiStub.EditId:D}?from=2026-10-01"));
        Assert.False(AdminAuditViewerEndpoints.IsAllowedReturnPath(AdminAuditViewerEndpoints.ShellJsPath));
        Assert.False(AdminAuditViewerEndpoints.IsAllowedReturnPath("/admin/audit/shell/audit.css"));
        Assert.False(AdminAuditViewerEndpoints.IsAllowedReturnPath("/admin/audit/shell"));
        Assert.False(AdminAuditViewerEndpoints.IsAllowedReturnPath("/admin/audit/not-a-guid"));
        Assert.False(AdminAuditViewerEndpoints.IsAllowedReturnPath("/admin/api/audit"));
        Assert.False(AdminAuditViewerEndpoints.IsAllowedReturnPath("/admin"));
        Assert.False(AdminAuditViewerEndpoints.IsAllowedReturnPath("/audit"));
        Assert.False(AdminAuditViewerEndpoints.IsAllowedReturnPath(null));
        Assert.False(AdminAuditViewerEndpoints.IsAllowedReturnPath(""));
    }

    [Fact]
    public void TD_ADM_101_EmbeddedShell_IsNotAnonymousStatic()
    {
        var names = AdminAuditViewerEndpoints.EmbeddedUiResourceNames();
        Assert.Contains(names, name => name.EndsWith("audit.js", StringComparison.Ordinal));
        Assert.Contains(names, name => name.EndsWith("audit.css", StringComparison.Ordinal));
        var wwwroot = Path.Combine(RepoRoot(), "src", "Dealoware.Api", "wwwroot");
        Assert.False(File.Exists(Path.Combine(wwwroot, "admin", "audit.js")));
        Assert.False(File.Exists(Path.Combine(wwwroot, "admin", "audit", "shell", "audit.js")));
    }

    [Fact]
    public void TD_ADM_064_Viewer_HasNoSoftDeletedToggle()
    {
        var html = AdminAuditViewerEndpoints.LoadUiResource("audit-list.html");
        Assert.DoesNotContain("Show deleted", html);
        Assert.DoesNotContain("includeDeleted", html);
    }

    [Fact]
    public async Task TD_ADM_131_SignedInShellAssets_RequireSession_AndNoStore()
    {
        var id = await _factory.SeedCoreOwnerSessionAsync();
        var client = _factory.CreateAdminClient();
        using var css = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet(AdminAuditViewerEndpoints.ShellCssPath, AdminHost, id));
        using var js = await client.SendAsync(
            AdminUiWebApplicationFactory.AdminGet(AdminAuditViewerEndpoints.ShellJsPath, AdminHost, id));
        Assert.Equal(HttpStatusCode.OK, css.StatusCode);
        Assert.Equal(HttpStatusCode.OK, js.StatusCode);
        AssertNoStore(css);
        AssertNoStore(js);
        Assert.Equal("text/css", css.Content.Headers.ContentType?.MediaType);
        Assert.Equal("text/javascript", js.Content.Headers.ContentType?.MediaType);
        var jsBody = await js.Content.ReadAsStringAsync();
        Assert.Contains("SECRET_KEYS", jsBody);
        Assert.DoesNotContain("203.0.113.10", jsBody);
    }

    private static void AssertNoStore(HttpResponseMessage response)
    {
        var raw = response.Headers.TryGetValues("Cache-Control", out var values)
            ? string.Join(",", values)
            : "";
        if (string.IsNullOrEmpty(raw)
            && response.Content.Headers.TryGetValues("Cache-Control", out var contentValues))
        {
            raw = string.Join(",", contentValues);
        }
        Assert.Contains("no-store", raw);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Dealoware.sln");
    }
}
