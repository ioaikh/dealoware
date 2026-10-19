using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
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
/// Security / Dev Code QA: write-path secret strip with a hard-coded reviewer
/// key list that is independent of AdminAuditSnapshots' own fragments.
/// </summary>
[Collection("WebAppTests")]
public class AdminAuditSecretStripTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";
    private const string ClientIp = "198.51.100.44";
    private const string VisibleMarker = "visible-audit-field";
    private const string EmbeddedSecret = "LEAK-embedded-json-v1";
    private const string MalformedEmbedSecret = "LEAK-malformed-embed-v1";
    private const string MalformedRootSecret = "LEAK-malformed-root-v1";
    private const string NonJsonSecret = "LEAK-plain-v1";
    private const string FreeTextSecret = "LEAK-freetext-v1";
    private const string MidStringSecret = "LEAK-mid";
    private const string EqSecret = "LEAK-eq-v1";
    private const string ColonSecret = "LEAK-colon-v1";
    private const string PasswordEqSecret = "LEAK-password-eq";
    private const string PasswordColonSecret = "LEAK-password-colon";
    private const string DontShareSecret = "LEAK-dont-share";
    private const string SayHiSecret = "LEAK-say-hi";
    private const string LogQuoteSecret = "LEAK-log-quote";
    private const string ApostropheKeySecret = "LEAK-apostrophe-key";
    private const string QuoteWrappedSecret = "LEAK-quote-wrapped";
    private const string PercentEqSecret = "LEAK-percent-eq";
    private const string FullwidthEqSecret = "LEAK-fullwidth-eq";
    private const string SingleQuoteSecret = "LEAK-singlequote-v1";
    private const string UnquotedSecret = "LEAK-unquoted-v1";
    private const string BearerMidSecret = "LEAK-bearer-mid";
    private const string BasicMidSecret = "LEAK-basic-mid";
    private const string DigestMidSecret = "LEAK-digest-mid";
    private const string TokenSchemeSecret = "LEAK-token-scheme";
    private const string AuthBearerSecret = "LEAK-auth-bearer";
    private const string MidJwtSecret = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJtaWQifQ.dGVzdHNpZ25hdHVyZQ";
    private const string TwoWordSecret = "LEAK two words";
    private const string ArrowEqSecret = "LEAK-arrow-eq";
    private const string ArrowDashSecret = "LEAK-arrow-dash";
    private const string DoubledQuoteSecret = "LEAK-doubled-quote";
    private const string CyrillicKeySecret = "LEAK-cyrillic-a";
    private const string GreekKeySecret = "LEAK-greek-o";
    private const string AccentKeySecret = "LEAK-accent";
    private const string ZeroWidthKeySecret = "LEAK-zwsp";

    /// <summary>
    /// Reviewer-required keys. Do not read this from production code.
    /// </summary>
    private static readonly (string Key, string Secret)[] ReviewerSecrets =
    [
        ("sessionToken", "LEAK-sessionToken-v1"),
        ("jwt", "LEAK-jwt-v1"),
        ("bearer", "LEAK-bearer-v1"),
        ("apiKeyHash", "LEAK-apiKeyHash-v1"),
        ("passwordSalt", "LEAK-passwordSalt-v1"),
        ("recoveryCodeHash", "LEAK-recoveryCodeHash-v1"),
        ("hmac", "LEAK-hmac-v1"),
        ("recovery", "LEAK-recovery-v1"),
        ("turnstile", "LEAK-turnstile-v1"),
        ("turnstileToken", "LEAK-turnstileToken-v1"),
        ("password", "LEAK-password-v1"),
        ("totpSecret", "LEAK-totpSecret-v1"),
        ("resetToken", "LEAK-resetToken-v1"),
        ("Authorization", "LEAK-Authorization-v1"),
        (" password ", "LEAK-padded-password-v1"),
        ("PASSWORD", "LEAK-PASSWORD-v1")
    ];

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminAuditSecretStripTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void TdAdm101_CreateEntityEvent_StripsReviewerKeysWithoutHelpers()
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
        Assert.Equal("[redacted]", JsonNode.Parse(entry.BeforeSnapshot!)!["broken"]!.GetValue<string>());
    }

    [Fact]
    public async Task TdAdm101_RecorderRawJson_StoresNoSecretsInDbOrGet()
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
            Assert.Contains(VisibleMarker, stored.BeforeSnapshot);
        }

        var body = await GetAuditBodyAsync(entityId);
        AssertNoSecrets(body);
        Assert.DoesNotContain(ClientIp, body);
        Assert.Contains(VisibleMarker, body);
        Assert.Contains(entryId.ToString(), body);
    }

    [Fact]
    public async Task TdAdm101_MalformedAndNonJson_FailClosedInDbAndGet()
    {
        var malformed = "{\"password\":\"" + MalformedRootSecret;
        const string nonJson = "password=" + NonJsonSecret;
        var singleQuoted = "{'password':'" + SingleQuoteSecret + "'}";
        var unquoted = "{password:\"" + UnquotedSecret + "\"}";
        var entityMalformed = Guid.NewGuid();
        var entityPlain = Guid.NewGuid();
        var entitySingle = Guid.NewGuid();
        var entityUnquoted = Guid.NewGuid();
        var cases = new (Guid Id, string Snapshot)[]
        {
            (entityMalformed, malformed),
            (entityPlain, nonJson),
            (entitySingle, singleQuoted),
            (entityUnquoted, unquoted)
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var recorder = scope.ServiceProvider.GetRequiredService<IAdminAuditRecorder>();
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            foreach (var (id, snapshot) in cases)
            {
                db.AdminAuditLog.Add(recorder.Entity(
                    AdminAuditActions.EntityEdit,
                    CoreOwnerEmail,
                    ClientIp,
                    "Participant",
                    id,
                    beforeSnapshot: snapshot,
                    afterSnapshot: snapshot));
            }

            await db.SaveChangesAsync();

            var ids = cases.Select(c => c.Id).ToArray();
            var rows = await db.AdminAuditLog.AsNoTracking()
                .Where(e => ids.Contains(e.EntityId!.Value))
                .ToListAsync();
            Assert.Equal(4, rows.Count);
            foreach (var row in rows)
            {
                Assert.Equal(AdminAuditSnapshots.FailClosedPlaceholder, row.BeforeSnapshot);
                AssertNoSecrets(row.BeforeSnapshot);
                AssertNoSecrets(row.AfterSnapshot ?? AdminAuditSnapshots.FailClosedPlaceholder);
            }
        }

        foreach (var (id, _) in cases)
        {
            var body = await GetAuditBodyAsync(id);
            AssertNoSecrets(body);
            Assert.Contains("_invalid", body);
            Assert.DoesNotContain(ClientIp, body);
        }
    }

    [Fact]
    public async Task TdAdm101_MidStringJson_StrippedInDbAndGet()
    {
        var note = "see {\"password\":\"" + MidStringSecret + "\"} later";
        var raw = new JsonObject
        {
            ["note"] = note,
            ["displayName"] = VisibleMarker
        }.ToJsonString();

        var (stored, body) = await WriteAndReadAsync(raw);
        AssertNoSecrets(stored);
        AssertNoSecrets(body);
        Assert.Contains("see", stored);
        Assert.Contains("later", stored);
        Assert.Contains(VisibleMarker, stored);
        Assert.DoesNotContain(MidStringSecret, stored);
        Assert.DoesNotContain(MidStringSecret, body);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("see {} later", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_PasswordEqualsAssignment_RedactedInDbAndGet()
    {
        var raw = new JsonObject
        {
            ["note"] = "password=" + PasswordEqSecret,
            ["displayName"] = VisibleMarker
        }.ToJsonString();

        var (stored, body) = await WriteAndReadAsync(raw);
        AssertNoSecrets(stored);
        AssertNoSecrets(body);
        Assert.DoesNotContain(PasswordEqSecret, stored);
        Assert.DoesNotContain(PasswordEqSecret, body);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("note").GetString());
        Assert.Equal(VisibleMarker, doc.RootElement.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task TdAdm101_PasswordColonAssignment_RedactedInDbAndGet()
    {
        var raw = new JsonObject
        {
            ["note"] = "password: " + PasswordColonSecret,
            ["displayName"] = VisibleMarker
        }.ToJsonString();

        var (stored, body) = await WriteAndReadAsync(raw);
        AssertNoSecrets(stored);
        AssertNoSecrets(body);
        Assert.DoesNotContain(PasswordColonSecret, stored);
        Assert.DoesNotContain(PasswordColonSecret, body);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("note").GetString());
        Assert.Equal(VisibleMarker, doc.RootElement.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task TdAdm101_ProseQuotesAndEncodedSeparators_RedactedInDbAndGet()
    {
        var cases = new (string Note, string Leak)[]
        {
            ("don't share password=" + DontShareSecret, DontShareSecret),
            ("say \"hi password: " + SayHiSecret, SayHiSecret),
            ("log: \"password=" + LogQuoteSecret, LogQuoteSecret),
            ("x'" + "password=" + ApostropheKeySecret, ApostropheKeySecret),
            ("\"password=" + QuoteWrappedSecret + "\"", QuoteWrappedSecret),
            ("password%3D" + PercentEqSecret, PercentEqSecret),
            ("password＝" + FullwidthEqSecret, FullwidthEqSecret)
        };

        foreach (var (note, leak) in cases)
        {
            var raw = new JsonObject
            {
                ["note"] = note,
                ["displayName"] = VisibleMarker
            }.ToJsonString();

            var (stored, body) = await WriteAndReadAsync(raw);
            Assert.DoesNotContain(leak, stored, StringComparison.Ordinal);
            Assert.DoesNotContain(leak, body, StringComparison.Ordinal);
            Assert.Contains(VisibleMarker, stored);
            Assert.Contains("[redacted]", stored);
        }
    }

    [Fact]
    public void TdAdm101_EmbeddedStringJson_IsParsedAndStripped()
    {
        var raw = """{"details":"{\"password\":\"LEAK-embedded-json-v1\",\"displayName\":\"ok\"}"}""";
        var stripped = AdminAuditSnapshots.StripForbiddenKeys(raw);
        using var doc = JsonDocument.Parse(stripped!);
        var details = doc.RootElement.GetProperty("details");
        Assert.Equal(JsonValueKind.Object, details.ValueKind);
        Assert.Equal("ok", details.GetProperty("displayName").GetString());
        Assert.False(details.TryGetProperty("password", out _));
        Assert.DoesNotContain(EmbeddedSecret, stripped, StringComparison.Ordinal);
    }

    [Fact]
    public void TdAdm101_DeepNestedJsonStrings_StoresPlaceholderFast()
    {
        var raw = NestedJsonStrings(25);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var clock = Stopwatch.StartNew();
        var entry = AdminAuditEntry.CreateEntityEvent(
            AdminAuditActions.EntityEdit,
            CoreOwnerEmail,
            "hmac-not-an-ip",
            "Participant",
            Guid.NewGuid(),
            beforeSnapshot: raw,
            afterSnapshot: raw);
        clock.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(AdminAuditSnapshots.FailClosedPlaceholder, entry.BeforeSnapshot);
        Assert.Equal(AdminAuditSnapshots.FailClosedPlaceholder, entry.AfterSnapshot);
        Assert.DoesNotContain("LEAK-deep-v1", entry.BeforeSnapshot);
        Assert.True(clock.ElapsedMilliseconds < 200, $"deep nest took {clock.ElapsedMilliseconds}ms");
        Assert.True(allocated < 2_000_000, $"deep nest allocated {allocated} bytes");
    }

    [Fact]
    public void TdAdm101_InputOver64Kb_StoresPlaceholderWithoutParse()
    {
        var raw = new string('x', (64 * 1024) + 1);
        Assert.True(Encoding.UTF8.GetByteCount(raw) > AdminAuditSnapshots.MaxInputUtf8Bytes);

        var clock = Stopwatch.StartNew();
        var entry = AdminAuditEntry.CreateEntityEvent(
            AdminAuditActions.EntityEdit,
            CoreOwnerEmail,
            "hmac-not-an-ip",
            "Participant",
            Guid.NewGuid(),
            beforeSnapshot: raw);
        clock.Stop();

        Assert.Equal(AdminAuditSnapshots.FailClosedPlaceholder, entry.BeforeSnapshot);
        Assert.DoesNotContain("xxxx", entry.BeforeSnapshot);
        Assert.True(clock.ElapsedMilliseconds < 200, $"oversize took {clock.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void TdAdm101_Depth8WithinLimit_StillStripsSecrets()
    {
        var raw = NestedObjects(8, "{\"password\":\"LEAK-depth8-v1\",\"displayName\":\"visible-audit-field\"}");
        var stripped = AdminAuditSnapshots.StripForbiddenKeys(raw);
        Assert.NotEqual(AdminAuditSnapshots.FailClosedPlaceholder, stripped);
        Assert.Contains(VisibleMarker, stripped);
        Assert.DoesNotContain("LEAK-depth8-v1", stripped);
        Assert.DoesNotContain("password", stripped, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TdAdm101_JwtAndBearerValues_AreRedactedUnderAnyKey()
    {
        const string jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJpc2FhYyJ9.dGVzdHNpZ25hdHVyZQ";
        const string bearer = "Bearer LEAK-bearer-value-v1";
        var raw = "{\"note\":\"" + jwt + "\",\"auth\":\"" + bearer + "\",\"displayName\":\"visible-audit-field\"}";
        var stripped = AdminAuditSnapshots.StripForbiddenKeys(raw);
        using var doc = JsonDocument.Parse(stripped!);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("note").GetString());
        Assert.Equal("Bearer [redacted]", doc.RootElement.GetProperty("auth").GetString());
        Assert.Equal(VisibleMarker, doc.RootElement.GetProperty("displayName").GetString());
        Assert.DoesNotContain(jwt, stripped);
        Assert.DoesNotContain("LEAK-bearer-value-v1", stripped);
    }

    [Fact]
    public async Task TdAdm101_BearerAnywhere_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("see Bearer " + BearerMidSecret + " later");
        Assert.DoesNotContain(BearerMidSecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(BearerMidSecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("see Bearer [redacted] later", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_BasicAnywhere_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("see Basic " + BasicMidSecret + " later");
        Assert.DoesNotContain(BasicMidSecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(BasicMidSecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("see Basic [redacted] later", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_DigestAnywhere_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("see Digest " + DigestMidSecret + " later");
        Assert.DoesNotContain(DigestMidSecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(DigestMidSecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("see Digest [redacted] later", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_TokenSchemeAnywhere_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("see Token " + TokenSchemeSecret + " later");
        Assert.DoesNotContain(TokenSchemeSecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(TokenSchemeSecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("see Token [redacted] later", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_AuthorizationBearer_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("Authorization: Bearer " + AuthBearerSecret);
        Assert.DoesNotContain(AuthBearerSecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(AuthBearerSecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_MidSentenceJwt_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("hello " + MidJwtSecret + " world");
        Assert.DoesNotContain(MidJwtSecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(MidJwtSecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("hello [redacted] world", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_PasswordTwoWords_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("password=" + TwoWordSecret);
        Assert.DoesNotContain(TwoWordSecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(TwoWordSecret, body, StringComparison.Ordinal);
        Assert.DoesNotContain("two words", stored, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_ArrowEqSeparator_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("password => " + ArrowEqSecret);
        Assert.DoesNotContain(ArrowEqSecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(ArrowEqSecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_ArrowDashSeparator_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("password -> " + ArrowDashSecret);
        Assert.DoesNotContain(ArrowDashSecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(ArrowDashSecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_DoubledSingleQuote_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("password='it''s " + DoubledQuoteSecret + "'");
        Assert.DoesNotContain(DoubledQuoteSecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(DoubledQuoteSecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_CyrillicLookalikeKey_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("p\u0430ssword=" + CyrillicKeySecret);
        Assert.DoesNotContain(CyrillicKeySecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(CyrillicKeySecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_GreekLookalikeKey_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("passw\u03BFrd=" + GreekKeySecret);
        Assert.DoesNotContain(GreekKeySecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(GreekKeySecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_AccentedKey_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("p\u00E1ssword=" + AccentKeySecret);
        Assert.DoesNotContain(AccentKeySecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(AccentKeySecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_ZeroWidthKey_RedactedInDbAndGet()
    {
        var (stored, body) = await WriteNoteAsync("pass\u200bword=" + ZeroWidthKeySecret);
        Assert.DoesNotContain(ZeroWidthKeySecret, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(ZeroWidthKeySecret, body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(stored);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public async Task TdAdm101_SqliteRecorder_StoresNoReviewerSecrets()
    {
        await using var connection = new SqliteConnection("Data Source=AuditStrip_Db;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealowareDbContext>().UseSqlite(connection).Options;
        await using var db = new DealowareDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var hasher = new IpHasher(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        var recorder = new AdminAuditRecorder(hasher);
        var raw = BuildSecretJson();
        db.AdminAuditLog.Add(recorder.Entity(
            AdminAuditActions.EntityEdit,
            CoreOwnerEmail,
            ClientIp,
            "Participant",
            Guid.NewGuid(),
            beforeSnapshot: raw,
            afterSnapshot: raw));
        await db.SaveChangesAsync();

        var stored = await db.AdminAuditLog.AsNoTracking().SingleAsync();
        AssertNoSecrets(stored.BeforeSnapshot);
        AssertNoSecrets(stored.AfterSnapshot);
        Assert.Contains(VisibleMarker, stored.BeforeSnapshot);
    }

    private Task<(string Snapshot, string GetBody)> WriteNoteAsync(string note)
        => WriteAndReadAsync(new JsonObject
        {
            ["note"] = note,
            ["displayName"] = VisibleMarker
        }.ToJsonString());

    private async Task<(string Snapshot, string GetBody)> WriteAndReadAsync(string snapshot)
    {
        var entityId = Guid.NewGuid();
        string stored;
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
                beforeSnapshot: snapshot,
                afterSnapshot: snapshot);
            db.AdminAuditLog.Add(entry);
            await db.SaveChangesAsync();
            stored = (await db.AdminAuditLog.AsNoTracking().SingleAsync(e => e.Id == entry.Id))
                .BeforeSnapshot!;
            AssertNoSecrets(stored);
            AssertNoSecrets((await db.AdminAuditLog.AsNoTracking().SingleAsync(e => e.Id == entry.Id))
                .AfterSnapshot);
        }

        var body = await GetAuditBodyAsync(entityId);
        AssertNoSecrets(body);
        Assert.DoesNotContain(ClientIp, body);
        return (stored, body);
    }

    private async Task<string> GetAuditBodyAsync(Guid entityId)
    {
        var sessionId = await SeedSessionAsync();
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/api/audit?entityId={entityId:D}");
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={sessionId:D}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
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

    private static string NestedJsonStrings(int wraps)
    {
        var inner = "{\"password\":\"LEAK-deep-v1\",\"displayName\":\"ok\"}";
        for (var i = 0; i < wraps; i++)
            inner = "{\"details\":" + JsonSerializer.Serialize(inner) + "}";
        return inner;
    }

    private static string NestedObjects(int depth, string innermost)
    {
        var json = innermost;
        for (var i = 1; i < depth; i++)
            json = "{\"l" + i + "\":" + json + "}";
        return json;
    }

    private static string BuildSecretJson()
    {
        var root = new JsonObject { ["displayName"] = VisibleMarker };
        var nested = new JsonObject();
        var array = new JsonArray();
        foreach (var (key, secret) in ReviewerSecrets)
        {
            root[key] = secret;
            nested[key] = secret;
            array.Add(new JsonObject { [key] = secret, ["label"] = VisibleMarker });
        }

        nested["items"] = array;
        root["nested"] = nested;
        root["details"] = """{"password":"LEAK-embedded-json-v1","displayName":"ok"}""";
        root["broken"] = "{not-json " + MalformedEmbedSecret;
        return root.ToJsonString();
    }

    private static void AssertNoSecrets(string? text)
    {
        Assert.False(string.IsNullOrEmpty(text));
        foreach (var (_, secret) in ReviewerSecrets)
            Assert.DoesNotContain(secret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(EmbeddedSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(MalformedEmbedSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(MalformedRootSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(NonJsonSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(FreeTextSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(MidStringSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(EqSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(ColonSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(PasswordEqSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(PasswordColonSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(DontShareSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(SayHiSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(LogQuoteSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(ApostropheKeySecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(QuoteWrappedSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(PercentEqSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullwidthEqSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(SingleQuoteSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(UnquotedSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(BearerMidSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(BasicMidSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(DigestMidSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(TokenSchemeSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(AuthBearerSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(MidJwtSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(TwoWordSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(ArrowEqSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(ArrowDashSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(DoubledQuoteSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(CyrillicKeySecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(GreekKeySecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(AccentKeySecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(ZeroWidthKeySecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(ClientIp, text, StringComparison.Ordinal);
    }
}
