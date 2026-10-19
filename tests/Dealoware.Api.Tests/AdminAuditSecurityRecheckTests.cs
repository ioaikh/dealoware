using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// Security recheck 5eee1ded: every leaking input is a Truncate() regression.
/// Double-encoding and separator-less prose are documented accepted limits.
/// </summary>
[Collection("WebAppTests")]
public class AdminAuditSecurityRecheckTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";
    private const string SampleJwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJtaWQifQ.dGVzdHNpZ25hdHVyZQ";
    private const string SampleJwe = "eyJhbGciOiJSU0EtT0FFUCJ9.LEAKkey.iv123.LEAKcipher.tag12";
    private const string MathBoldPassword =
        "\U0001D429\U0001D41A\U0001D42C\U0001D42C\U0001D430\U0001D428\U0001D42B\U0001D41D";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminAuditSecurityRecheckTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("A04", "Digest username=bob, response=LEAKdig")]
    [InlineData("J03", "the user pasted " + SampleJwt + ".")]
    [InlineData("J08", "pasted " + SampleJwe)]
    [InlineData("P05", "pwd=''LEAKx''")]
    [InlineData("P17", "password=hunter,LEAK")]
    [InlineData("P18", "password=ab>LEAK")]
    [InlineData("P19", "password=ab;LEAK")]
    [InlineData("H10", "pass\u051Dord=LEAK")]
    [InlineData("H13", "passwor\u0501=LEAK")]
    [InlineData("K09", "passphrase=LEAK")]
    [InlineData("K10", "aws access_key=LEAK")]
    [InlineData("K11", "mfa_code=LEAK")]
    [InlineData("K12", "verification_code=LEAK")]
    [InlineData("K13", "pass=LEAK")]
    [InlineData("K15", "auth=LEAK")]
    [InlineData("K16", "pin=LEAK")]
    public void Recheck_FreeTextLeak_IsRedactedByTruncate(string id, string note)
    {
        var output = AdminAuditSnapshots.Truncate(new JsonObject { ["note"] = note }.ToJsonString());
        Assert.DoesNotContain("LEAK", output, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJzdWIiOiJtaWQifQ", output, StringComparison.Ordinal);
        Assert.True(
            output.Contains("[redacted]", StringComparison.Ordinal)
            || output.Contains("_invalid", StringComparison.Ordinal),
            $"{id} produced {output}");
    }

    [Fact]
    public void Recheck_H11_MathBoldPassword_IsRedactedByTruncate()
    {
        var output = AdminAuditSnapshots.Truncate(
            new JsonObject { ["note"] = MathBoldPassword + "=LEAK" }.ToJsonString());
        Assert.DoesNotContain("LEAK", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Recheck_JK2_KomiWeJsonKey_IsStrippedByTruncate()
    {
        var output = AdminAuditSnapshots.Truncate("{\"pass\u051Dord\":\"LEAK\"}");
        Assert.DoesNotContain("LEAK", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Recheck_JK6_PassphraseJsonKey_IsStrippedByTruncate()
    {
        var output = AdminAuditSnapshots.Truncate("""{"passphrase":"LEAK"}""");
        Assert.DoesNotContain("LEAK", output, StringComparison.Ordinal);
        Assert.DoesNotContain("passphrase", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Recheck_JK8_DuplicatePasswordKey_StoresPlaceholder()
    {
        var output = AdminAuditSnapshots.Truncate("""{"x":1,"password":"LEAK","password":"LEAK2"}""");
        Assert.Equal(AdminAuditSnapshots.FailClosedPlaceholder, output);
        Assert.DoesNotContain("LEAK", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Recheck_P20_DoubleEncodedPercent_IsAcceptedLimit()
    {
        // Accepted limit: %253D is not decoded a second time.
        var output = AdminAuditSnapshots.Truncate(
            new JsonObject { ["note"] = "password%253DLEAK" }.ToJsonString());
        Assert.Contains("LEAK", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Recheck_P21_ProseWithoutSeparator_IsAcceptedLimit()
    {
        // Accepted limit: "my password is X" has no assignment separator.
        var output = AdminAuditSnapshots.Truncate(
            new JsonObject { ["note"] = "my password is LEAK" }.ToJsonString());
        Assert.Contains("LEAK", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Recheck_UrlQuery_StopsAtAmpersandOnly()
    {
        var output = AdminAuditSnapshots.Truncate(
            new JsonObject { ["note"] = "https://ex.test/x?foo=1&token=LEAK&keep=ok" }.ToJsonString());
        Assert.DoesNotContain("LEAK", output, StringComparison.Ordinal);
        Assert.Contains("keep=ok", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Recheck_GetPath_ReredactsStoredSnapshot()
    {
        const string leak = "LEAK-get-path-v1";
        var entityId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            db.AdminAuditLog.Add(LeakEntry(entityId, """{"note":"password=""" + leak + "\"}"));
            await db.SaveChangesAsync();
        }

        var sessionId = await SeedSessionAsync();
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/api/audit?entityId={entityId:D}");
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={sessionId:D}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(leak, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("items").GetArrayLength() >= 1);
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

    private static AdminAuditEntry LeakEntry(Guid entityId, string snapshot)
    {
        var entry = (AdminAuditEntry)Activator.CreateInstance(typeof(AdminAuditEntry), nonPublic: true)!;
        Set(entry, nameof(AdminAuditEntry.Id), Guid.NewGuid());
        Set(entry, nameof(AdminAuditEntry.Timestamp), DateTimeOffset.UtcNow);
        Set(entry, nameof(AdminAuditEntry.Action), AdminAuditActions.EntityEdit);
        Set(entry, nameof(AdminAuditEntry.ActorEmail), CoreOwnerEmail);
        Set(entry, nameof(AdminAuditEntry.IpHmac), "hmac-not-an-ip");
        Set(entry, nameof(AdminAuditEntry.EntityType), "Participant");
        Set(entry, nameof(AdminAuditEntry.EntityId), entityId);
        Set(entry, nameof(AdminAuditEntry.BeforeSnapshot), snapshot);
        Set(entry, nameof(AdminAuditEntry.AfterSnapshot), snapshot);
        return entry;
    }

    private static void Set(AdminAuditEntry entry, string name, object? value)
        => typeof(AdminAuditEntry).GetProperty(name)!.SetValue(entry, value);
}
