using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// Security R1: forbidden keys are stripped on the write path (TruncateSnapshot
/// and the recorder), recursively and case-insensitively, including arrays.
/// </summary>
[Collection("WebAppTests")]
public class AdminAuditSecretStripTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";
    private const string ClientIp = "198.51.100.44";
    private const string VisibleMarker = "visible-audit-field";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminAuditSecretStripTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void TdAdm101_CreateEntityEvent_StripsNestedAndArraySecretsWithoutHelpers()
    {
        var raw = BuildSecretJson();
        var entry = AdminAuditEntry.CreateEntityEvent(
            AdminAuditActions.EntityEdit,
            CoreOwnerEmail,
            "hmac-not-an-ip",
            "Participant",
            Guid.NewGuid(),
            beforeSnapshot: raw,
            afterSnapshot: raw);

        AssertNoSecrets(entry.BeforeSnapshot);
        AssertNoSecrets(entry.AfterSnapshot);
        Assert.Contains(VisibleMarker, entry.BeforeSnapshot);
        Assert.Contains(VisibleMarker, entry.AfterSnapshot);
    }

    [Fact]
    public async Task TdAdm101_RecorderRawJson_StoresNoSecretsInDb()
    {
        await using var connection = new SqliteConnection("Data Source=AuditStrip_Db;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        await using var db = new DealowareDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var hasher = new IpHasher(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        var recorder = new AdminAuditRecorder(hasher);
        var raw = BuildSecretJson();
        var entry = recorder.Entity(
            AdminAuditActions.EntityEdit,
            CoreOwnerEmail,
            ClientIp,
            "Participant",
            Guid.NewGuid(),
            beforeSnapshot: raw,
            afterSnapshot: raw);

        db.AdminAuditLog.Add(entry);
        await db.SaveChangesAsync();

        var stored = await db.AdminAuditLog.AsNoTracking().SingleAsync();
        AssertNoSecrets(stored.BeforeSnapshot);
        AssertNoSecrets(stored.AfterSnapshot);
        Assert.DoesNotContain(ClientIp, stored.BeforeSnapshot ?? "");
        Assert.DoesNotContain(ClientIp, stored.AfterSnapshot ?? "");
        Assert.Contains(VisibleMarker, stored.BeforeSnapshot);
    }

    [Fact]
    public async Task TdAdm101_RecorderRawJson_GetAuditReturnsNoSecrets()
    {
        var raw = BuildSecretJson();
        var entityId = Guid.NewGuid();
        Guid entryId;
        using (var scope = _factory.Services.CreateScope())
        {
            var recorder = scope.ServiceProvider.GetRequiredService<IAdminAuditRecorder>();
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var entry = recorder.Entity(
                AdminAuditActions.EntityEdit,
                CoreOwnerEmail,
                ClientIp,
                "Participant",
                entityId,
                beforeSnapshot: raw,
                afterSnapshot: raw);
            db.AdminAuditLog.Add(entry);
            await db.SaveChangesAsync();
            entryId = entry.Id;

            var stored = await db.AdminAuditLog.AsNoTracking().SingleAsync(e => e.Id == entryId);
            AssertNoSecrets(stored.BeforeSnapshot);
            AssertNoSecrets(stored.AfterSnapshot);
        }

        var sessionId = await SeedSessionAsync();
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/api/audit?entityId={entityId:D}");
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={sessionId:D}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        AssertNoSecrets(body);
        Assert.DoesNotContain(ClientIp, body);
        Assert.Contains(VisibleMarker, body);
        Assert.Contains(entryId.ToString(), body);
    }

    [Fact]
    public void TdAdm101_Strip_IsCaseInsensitiveAndWalksArrays()
    {
        const string raw = """
            {
              "displayName": "ok",
              "PASSWORD": "SECRET-password-value",
              "nested": {
                "NewPassword": "SECRET-newPassword-value",
                "items": [
                  { "TOKEN": "SECRET-token-value", "name": "child-ok" },
                  [ { "refreshToken": "SECRET-refreshToken-value" } ]
                ]
              }
            }
            """;

        var stripped = AdminAuditSnapshots.StripForbiddenKeys(raw);
        using var doc = JsonDocument.Parse(stripped);
        Assert.Equal("ok", doc.RootElement.GetProperty("displayName").GetString());
        Assert.False(HasPropertyIgnoreCase(doc.RootElement, "password"));
        Assert.False(HasPropertyIgnoreCase(doc.RootElement.GetProperty("nested"), "newPassword"));
        var child = doc.RootElement.GetProperty("nested").GetProperty("items")[0];
        Assert.Equal("child-ok", child.GetProperty("name").GetString());
        Assert.False(HasPropertyIgnoreCase(child, "token"));
        Assert.DoesNotContain("SECRET-", stripped, StringComparison.Ordinal);
    }

    private async Task<Guid> SeedSessionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(CoreOwnerEmail, ipHmac: "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private static string BuildSecretJson()
    {
        var root = new JsonObject { ["displayName"] = VisibleMarker };
        var nested = new JsonObject();
        var array = new JsonArray();
        foreach (var key in AdminAuditSnapshots.ForbiddenKeys)
        {
            var secret = $"SECRET-{key}-value";
            root[key] = secret;
            root[key.ToUpperInvariant()] = secret;
            nested[key] = secret;
            array.Add(new JsonObject { [key] = secret, ["label"] = VisibleMarker });
        }

        nested["items"] = array;
        root["nested"] = nested;
        return root.ToJsonString();
    }

    private static void AssertNoSecrets(string? text)
    {
        Assert.False(string.IsNullOrEmpty(text));
        foreach (var key in AdminAuditSnapshots.ForbiddenKeys)
        {
            Assert.DoesNotContain($"SECRET-{key}-value", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain($"\"{key}\":", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool HasPropertyIgnoreCase(JsonElement obj, string name)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
