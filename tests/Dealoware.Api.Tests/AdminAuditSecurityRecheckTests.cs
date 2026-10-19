using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// Security recheck c89f7ece: every leaking input is a Truncate() regression.
/// Double-encoding, separator-less prose, and "sms code X" are accepted limits.
/// </summary>
[Collection("WebAppTests")]
public class AdminAuditSecurityRecheckTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";
    private const string SampleJwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJtaWQifQ.dGVzdHNpZ25hdHVyZQ";
    private const string SampleJwe = "eyJhbGciOiJSU0EtT0FFUCJ9.LEAK-key.iv-123.LEAKcipher.tag_12";
    private const string DirJwe = "eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..iv12345.LEAKcipher.tag12";
    private const string EyAiJwt = "eyAiYWxnIjoibm9uZSJ9.eyJzdWIiOiJleWFpIn0.dGVzdA";
    private const string KidUnderscoreJwt =
        "eyJhbGciOiJIUzI1NiIsImtpZCI6ImFiKy8_PyJ9.eyJzdWIiOiJraWQxIn0.dGVzdA";
    private const string KidDashJwt =
        "eyJhbGciOiJIUzI1NiIsImtpZCI6IiA-In0.eyJzdWIiOiJraWQxIn0.dGVzdA";
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
    [InlineData("F2g", "dir JWE eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..iv12345.LEAKcipher.tag12 mid")]
    [InlineData("F2h", "eyJhbGciOiJub25lIn0.eyJzdWIiOiJ1In0.")]
    [InlineData("F2i", SampleJwt + ".Next sentence")]
    [InlineData("F5e", "why? password=ab&LEAK")]
    [InlineData("F5f", "https://x.test/?q=1 then password=ab&LEAK")]
    [InlineData("F6j", "passw0rd=LEAK")]
    [InlineData("F6k", "password\uA789LEAK")]
    [InlineData("F6l", "password\u02D0LEAK")]
    [InlineData("F6q", "password\u2236LEAK")]
    [InlineData("F8i", "codes=LEAK")]
    [InlineData("F8k", "code_verifier=LEAK")]
    [InlineData("NA16", "x-" + SampleJwt)]
    [InlineData("NA17", "id_" + SampleJwt)]
    [InlineData("NA30", "token_" + SampleJwt)]
    [InlineData("NA24", EyAiJwt)]
    [InlineData("B3a", "my_id_" + SampleJwt)]
    [InlineData("B3b", "access_token_" + SampleJwt)]
    [InlineData("B3c", "x-y-" + SampleJwt)]
    [InlineData("B3d", "__" + SampleJwt)]
    [InlineData("B3e", "x--" + SampleJwt)]
    [InlineData("B3f", "a" + SampleJwt)]
    [InlineData("B3g", "my-x-" + EyAiJwt)]
    [InlineData("B3h", "pre." + SampleJwt)]
    [InlineData("B3i", "a.b." + SampleJwt)]
    [InlineData("B3j", "a" + EyAiJwt)]
    [InlineData("B3k", "prefix_" + DirJwe)]
    [InlineData("B3l", KidUnderscoreJwt)]
    [InlineData("B3m", "x-" + KidUnderscoreJwt)]
    [InlineData("B3n", "id_" + KidDashJwt)]
    [InlineData("B3o", SampleJwt + "." + EyAiJwt)]
    [InlineData("CAP8seg", "a.b.c.d.e.f.g.h." + SampleJwt)]
    [InlineData("CAP8delim", "a-b-c-d-e-f-g-h-" + SampleJwt)]
    [InlineData("NX02", "backup_codes=LEAK")]
    [InlineData("NX03", "mfa_codes=LEAK")]
    [InlineData("NX04", "pa55word=LEAK")]
    public void Recheck_FreeTextLeak_IsRedactedByTruncate(string id, string note)
    {
        var output = AdminAuditSnapshots.Truncate(new JsonObject { ["note"] = note }.ToJsonString());
        Assert.DoesNotContain("LEAK", output, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJzdWIiOiJtaWQifQ", output, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJzdWIiOiJ1In0", output, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJzdWIiOiJleWFpIn0", output, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJzdWIiOiJraWQxIn0", output, StringComparison.Ordinal);
        Assert.DoesNotContain("LEAKcipher", output, StringComparison.Ordinal);
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
    public void Recheck_F8l_SmsCodeWithoutSeparator_IsAcceptedLimit()
    {
        // Accepted limit: "sms code X" has no assignment separator (same class as P21).
        var output = AdminAuditSnapshots.Truncate(
            new JsonObject { ["note"] = "sms code LEAK" }.ToJsonString());
        Assert.Contains("LEAK", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Recheck_F2i_KeepsTextAfterJwtPrefix()
    {
        var output = AdminAuditSnapshots.Truncate(
            new JsonObject { ["note"] = SampleJwt + ".Next sentence" }.ToJsonString());
        Assert.DoesNotContain("eyJzdWIiOiJtaWQifQ", output, StringComparison.Ordinal);
        Assert.Contains(".Next sentence", output, StringComparison.Ordinal);
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
    public void Recheck_ProseAmpersand_StaysUnchanged()
    {
        const string prose = "Tom & Jerry met Smith & Sons at 5 & 6 Main St.";
        var output = AdminAuditSnapshots.Truncate(new JsonObject { ["note"] = prose }.ToJsonString());
        Assert.Contains("Tom", output, StringComparison.Ordinal);
        Assert.Contains("Jerry", output, StringComparison.Ordinal);
        Assert.Contains("Smith", output, StringComparison.Ordinal);
        Assert.Contains("Sons", output, StringComparison.Ordinal);
        Assert.DoesNotContain("[redacted]", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Recheck_Nx07_BackupCodesJsonKey_IsStrippedByTruncate()
    {
        var output = AdminAuditSnapshots.Truncate("""{"backup_codes":"LEAK"}""");
        Assert.DoesNotContain("LEAK", output, StringComparison.Ordinal);
        Assert.DoesNotContain("backup_codes", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Recheck_AmpersandScan_IsLinearUnder64Kb()
    {
        var note = new string('/', 32_000) + "password=" + new string('&', 32_000);
        var raw = "{\"note\":\"" + note + "\"}";
        Assert.True(Encoding.UTF8.GetByteCount(raw) <= AdminAuditSnapshots.MaxInputUtf8Bytes);

        AdminAuditSnapshots.Truncate(raw);
        var clock = Stopwatch.StartNew();
        var output = AdminAuditSnapshots.Truncate(raw);
        clock.Stop();

        Assert.DoesNotContain("password=", output, StringComparison.Ordinal);
        Assert.True(clock.ElapsedMilliseconds < 200, $"ampersand scan took {clock.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Recheck_JwtRescan_IsLinearAt60Kb()
    {
        var unit = "eyJa.b.c..";
        var note = string.Concat(Enumerable.Repeat(unit, 6_000));
        var raw = new JsonObject { ["note"] = note }.ToJsonString();
        Assert.True(note.Length >= 60_000);
        Assert.True(Encoding.UTF8.GetByteCount(raw) <= AdminAuditSnapshots.MaxInputUtf8Bytes);

        AdminAuditSnapshots.Truncate(raw);
        var clock = Stopwatch.StartNew();
        var output = AdminAuditSnapshots.Truncate(raw);
        clock.Stop();

        Assert.DoesNotContain("eyJa.b.c", output, StringComparison.Ordinal);
        Assert.True(clock.ElapsedMilliseconds < 200, $"JWT rescan took {clock.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Recheck_B2_JwtThenLongSegment_IsLinearAt60Kb()
    {
        var output = AssertLinearJwtScan(SampleJwt + ".a", 'a', "B2 JWT+a");
        Assert.DoesNotContain("eyJzdWIiOiJtaWQifQ", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Recheck_B2_GenericThenLongSegment_IsLinearAt60Kb()
    {
        AssertLinearJwtScan("aaaa.b.c.", 'a', "B2 aaaa.b.c.");
    }

    [Fact]
    public void Recheck_Cap16Decodes_ThenEyAi_IsRedacted()
    {
        var note = string.Concat(Enumerable.Repeat("eyAaaaa.bbbb.cccc.", 20)) + EyAiJwt;
        var output = AdminAuditSnapshots.Truncate(new JsonObject { ["note"] = note }.ToJsonString());
        Assert.DoesNotContain("eyJzdWIiOiJleWFpIn0", output, StringComparison.Ordinal);
        Assert.Contains("[redacted]", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Recheck_B3b_AccessTokenJwtJsonValue_IsRedacted()
    {
        var output = AdminAuditSnapshots.Truncate(
            new JsonObject { ["note"] = "access_token_" + SampleJwt }.ToJsonString());
        Assert.DoesNotContain("eyJzdWIiOiJtaWQifQ", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Recheck_B3_CandidateSearch_IsLinearAt60Kb()
    {
        var unit = "x-y-eyJa.b.c.";
        var note = string.Concat(Enumerable.Repeat(unit, 5_000));
        var raw = new JsonObject { ["note"] = note }.ToJsonString();
        Assert.True(note.Length >= 60_000);
        Assert.True(Encoding.UTF8.GetByteCount(raw) <= AdminAuditSnapshots.MaxInputUtf8Bytes);

        AdminAuditSnapshots.Truncate(raw);
        var clock = Stopwatch.StartNew();
        var output = AdminAuditSnapshots.Truncate(raw);
        clock.Stop();

        Assert.DoesNotContain("eyJa.b.c", output, StringComparison.Ordinal);
        Assert.True(clock.ElapsedMilliseconds < 200, $"B3 candidate search took {clock.ElapsedMilliseconds}ms");
    }

    [Theory]
    [InlineData("admin.core.dealoware.com")]
    [InlineData("mail.example.com")]
    [InlineData("api.platform.dealoware.com")]
    [InlineData("a.b.c")]
    [InlineData("a.b.c.d.e.f.g.h.i.j.k.example.com")]
    [InlineData("report.final.v2.pdf")]
    [InlineData("alex.doe@example.test")]
    [InlineData("price.list.updated")]
    [InlineData("Release.2026.10")]
    public void Recheck_DottedNames_StayUnredacted(string note)
    {
        var output = AdminAuditSnapshots.Truncate(new JsonObject { ["note"] = note }.ToJsonString());
        Assert.Contains(note, output, StringComparison.Ordinal);
        Assert.DoesNotContain("[redacted]", output, StringComparison.Ordinal);
    }

    private static string AssertLinearJwtScan(string prefix, char fill, string label)
    {
        var fillLen = 60_000 - prefix.Length;
        Assert.True(fillLen > 0);
        var note = prefix + new string(fill, fillLen);
        var raw = "{\"note\":\"" + note + "\"}";
        Assert.True(note.Length >= 60_000);
        Assert.True(Encoding.UTF8.GetByteCount(raw) <= AdminAuditSnapshots.MaxInputUtf8Bytes);

        AdminAuditSnapshots.Truncate(raw);
        var clock = Stopwatch.StartNew();
        var output = AdminAuditSnapshots.Truncate(raw);
        clock.Stop();
        Assert.True(clock.ElapsedMilliseconds < 200, $"{label} took {clock.ElapsedMilliseconds}ms");
        return output;
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
