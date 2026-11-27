using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Application.Auth.Dtos;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>The requests, the seed and the readings the denial checks share.</summary>
public partial class AdminRouteDenialTests
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string EngineHost = "api.core.dealoware.com";
    private const string AdminOrigin = AdminHostOptions.ProductionAdminOrigin;
    private const string OwnerEmail = DenialWebApplicationFactory.OwnerEmail;

    private const string UnauthorizedBody = """{"error":"Unauthorized"}""";
    private const string ForbiddenJson = """{"error":"Forbidden"}""";
    private const string ForbiddenPage =
        """<!DOCTYPE html><html lang="en"><head><meta charset="utf-8"><title>Forbidden</title></head><body><p>Forbidden</p></body></html>""";

    private static readonly string[] EveryMethod = ["GET", "HEAD", "OPTIONS", "POST", "PUT", "PATCH", "DELETE"];
    private static readonly string[] ChangeMethods = ["POST", "PUT", "PATCH", "DELETE"];

    private static bool IsChange(string method) => ChangeMethods.Contains(method);

    // ---- the rows, as theory data ----

    private static IEnumerable<AdminRoute> RowsHere => AdminRouteTable.Routes.Where(MappedInThisBuild);

    private static TheoryData<string> KeysOf(IEnumerable<AdminRoute> rows)
    {
        var data = new TheoryData<string>();
        foreach (var row in rows)
            data.Add(row.ToString());
        return data;
    }

    private static AdminRoute Row(string key) => AdminRouteTable.Routes.Single(r => r.ToString() == key);

    /// <summary>Behind the gate: signed-in rows and the test-only rows.</summary>
    public static TheoryData<string> GatedRows
        => KeysOf(RowsHere.Where(r => r.Class is AdminRouteClass.SignedIn or AdminRouteClass.TestOnly));

    public static TheoryData<string> SignInInProgressRows
        => KeysOf(RowsHere.Where(r => r.Class == AdminRouteClass.SignInInProgress));

    public static TheoryData<string> SignedOutRows
        => KeysOf(RowsHere.Where(r => r.Class == AdminRouteClass.SignedOut));

    public static TheoryData<string> ChangeRows
        => KeysOf(RowsHere.Where(r => r.Methods.Any(IsChange)));

    public static TheoryData<string> Patterns()
    {
        var data = new TheoryData<string>();
        foreach (var pattern in RowsHere.Select(r => r.Pattern).Distinct())
            data.Add(pattern);
        return data;
    }

    // ---- requests ----

    private sealed record Reply(int Status, string Body, long Length, string? Location, string? CacheControl, IReadOnlyList<string> SetCookies)
    {
        public override string ToString() => $"{Status} {Body[..Math.Min(Body.Length, 300)]}";
    }

    private sealed class Ask
    {
        public string Host { get; init; } = AdminHost;
        public Guid? Session { get; init; }
        public string? Cookie { get; init; }
        public string? Origin { get; init; } = AdminOrigin;
        public string? Referer { get; init; }
        public string? Bearer { get; init; }

        /// <summary>A good anti-forgery pair on a change method (the default), or none, or one of the wrong kinds.</summary>
        public AntiForgery AntiForgery { get; init; } = AntiForgery.Good;

        public string? Body { get; init; }
        public string? IfMatch { get; init; }
        public string? ConfirmToken { get; init; }
    }

    private enum AntiForgery
    {
        Good,
        None,
        CookieWithoutHeader,
        WrongHeader,
        HeaderWithoutCookie
    }

    private HttpClient Client(WebApplicationFactory<Program>? factory = null)
        => (factory ?? _factory).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

    private async Task<Reply> SendAsync(string method, string path, Ask? ask = null, WebApplicationFactory<Program>? factory = null)
    {
        ask ??= new Ask();
        factory ??= _factory;
        using var client = Client(factory);
        client.DefaultRequestHeaders.Remove("Origin");

        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Host = ask.Host;
        var cookies = new List<string>();
        if (ask.Session is { } id)
            cookies.Add($"{AdminSessionCookie.Name}={id:D}");
        if (!string.IsNullOrEmpty(ask.Cookie))
            cookies.Add(ask.Cookie);

        if (IsChange(method) && ask.AntiForgery != AntiForgery.None)
        {
            var service = factory.Services.GetRequiredService<AdminAntiForgeryService>();
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            if (ask.AntiForgery != AntiForgery.HeaderWithoutCookie)
                cookies.Add($"{AdminAntiForgeryCookie.Name}={nonce}");
            var header = ask.AntiForgery switch
            {
                AntiForgery.Good or AntiForgery.HeaderWithoutCookie => service.ComputeRequestToken(nonce),
                AntiForgery.WrongHeader => service.ComputeRequestToken(Convert.ToHexString(RandomNumberGenerator.GetBytes(32))),
                _ => null
            };
            if (header is not null)
                request.Headers.TryAddWithoutValidation(AdminAntiForgeryCookie.HeaderName, header);
        }

        if (cookies.Count > 0)
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        if (ask.Origin is not null)
            request.Headers.TryAddWithoutValidation("Origin", ask.Origin);
        if (ask.Referer is not null)
            request.Headers.TryAddWithoutValidation("Referer", ask.Referer);
        if (ask.Bearer is not null)
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + ask.Bearer);
        if (ask.IfMatch is not null)
            request.Headers.TryAddWithoutValidation("If-Match", ask.IfMatch);
        if (ask.ConfirmToken is not null)
            request.Headers.TryAddWithoutValidation(AdminDeleteEndpoints.ConfirmTokenHeader, ask.ConfirmToken);
        if (ask.Body is not null)
            request.Content = new StringContent(ask.Body, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var location = response.Headers.Location?.OriginalString;
        var cacheControl = response.Headers.CacheControl?.ToString();
        var setCookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : [];
        return new Reply((int)response.StatusCode, Encoding.UTF8.GetString(bytes), bytes.LongLength, location, cacheControl, setCookies);
    }

    // ---- sessions ----

    private IAdminClock AdminClock() => _factory.Services.GetRequiredService<IAdminClock>();

    private async Task<Guid> NewSessionAsync(
        string email = OwnerEmail,
        bool secondStepDone = true,
        TimeSpan? createdAgo = null,
        TimeSpan? lastActiveAgo = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var now = AdminClock().UtcNow;
        var session = AdminSession.Create(email, ipHmac: "testhmac-not-an-ip", now: now - (createdAgo ?? TimeSpan.Zero));
        if (lastActiveAgo is { } idle)
            session.UpdateActivity(now - idle);
        if (secondStepDone)
            session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    /// <summary>A pending sign-in token (the step-1 cookie value): usable, run out, or already used.</summary>
    private async Task<string> NewPendingAsync(string state)
    {
        var raw = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var created = state == "run-out" ? DateTimeOffset.UtcNow.AddMinutes(-(AdminPendingAuth.LifetimeMinutes + 1)) : DateTimeOffset.UtcNow;
        var pending = AdminPendingAuth.Create(OwnerEmail, AdminPendingToken.Hash(raw), created);
        if (state == "used")
            pending.Consume();
        db.AdminPendingAuths.Add(pending);
        await db.SaveChangesAsync();
        return $"{AdminPendingAuthCookie.Name}={raw}";
    }

    /// <summary>
    /// The ways a request can come without a good session (T08 spec, the first
    /// two rows of the denial table). Each is a cookie, a token, or nothing.
    /// </summary>
    private static readonly string[] BadSessions =
    [
        "none", "forged", "unknown", "idle-over-30-minutes", "over-8-hours", "second-step-not-done",
        "another-address", "signed-out", "engine-token", "sign-in-in-progress-only"
    ];

    private async Task<Ask> BadSessionAsync(string state, Ask template)
    {
        string? cookie = null;
        string? bearer = null;
        switch (state)
        {
            case "none":
                break;
            case "forged":
                cookie = AdminSessionCookie.Name + "=not-a-session";
                break;
            case "unknown":
                cookie = $"{AdminSessionCookie.Name}={Guid.NewGuid():D}";
                break;
            case "idle-over-30-minutes":
                cookie = $"{AdminSessionCookie.Name}={await NewSessionAsync(createdAgo: TimeSpan.FromMinutes(31)):D}";
                break;
            case "over-8-hours":
                cookie = $"{AdminSessionCookie.Name}={await NewSessionAsync(createdAgo: TimeSpan.FromHours(8) + TimeSpan.FromMinutes(1), lastActiveAgo: TimeSpan.FromMinutes(1)):D}";
                break;
            case "second-step-not-done":
                cookie = $"{AdminSessionCookie.Name}={await NewSessionAsync(secondStepDone: false):D}";
                break;
            case "another-address":
                cookie = $"{AdminSessionCookie.Name}={await NewSessionAsync(email: "someone.else@example.test"):D}";
                break;
            case "signed-out":
                var id = await NewSessionAsync();
                using (var scope = _factory.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
                    await db.AdminSessions.Where(s => s.Id == id).ExecuteDeleteAsync();
                }

                cookie = $"{AdminSessionCookie.Name}={id:D}";
                break;
            case "engine-token":
                bearer = await EngineTokenAsync();
                break;
            case "sign-in-in-progress-only":
                cookie = await NewPendingAsync("usable");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state));
        }

        return new Ask
        {
            Host = template.Host,
            Cookie = cookie,
            Bearer = bearer,
            Origin = template.Origin,
            AntiForgery = template.AntiForgery,
            Body = template.Body,
            IfMatch = template.IfMatch,
            ConfirmToken = template.ConfirmToken
        };
    }

    /// <summary>A real engine access token for a registered participant, issued once per host.</summary>
    private async Task<string> EngineTokenAsync()
    {
        if (_factory.EngineToken is { } cached)
            return cached;

        using var client = Client();
        using var register = new HttpRequestMessage(HttpMethod.Post, "/auth/register") { Content = JsonContent.Create(new { }) };
        register.Headers.Host = EngineHost;
        using var registered = await client.SendAsync(register);
        Assert.True(registered.IsSuccessStatusCode, $"register: {(int)registered.StatusCode}");
        var apiKey = (await registered.Content.ReadFromJsonAsync<RegisterResponse>())!.ApiKey;

        using var issue = new HttpRequestMessage(HttpMethod.Post, "/auth/token") { Content = JsonContent.Create(new { apiKey }) };
        issue.Headers.Host = EngineHost;
        using var issued = await client.SendAsync(issue);
        Assert.True(issued.IsSuccessStatusCode, $"token: {(int)issued.StatusCode}");
        var token = (await issued.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;

        // It is a good engine token: the engine takes it.
        using var check = new HttpRequestMessage(HttpMethod.Get, "/artifacts");
        check.Headers.Host = EngineHost;
        check.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
        using var checkedReply = await client.SendAsync(check);
        Assert.True(checkedReply.IsSuccessStatusCode, $"engine GET /artifacts with the token: {(int)checkedReply.StatusCode}");

        _factory.EngineToken = token;
        return token;
    }

    private async Task<bool> IsSignedInAsync(Guid session)
        => (await SendAsync("GET", "/admin/api/me", new Ask { Session = session })).Status == 200;

    // ---- the seed: records with a tag no refusal may show ----

    private sealed record Seed(
        string Tag,
        Participant Party,
        Participant OtherParty,
        Participant DeletedParty,
        Artifact Artifact,
        Artifact DeletedArtifact,
        Negotiation Open,
        Negotiation DeletedNegotiation,
        Offer OpenOffer,
        Offer DeletedOffer,
        Guid AuditEntry)
    {
        /// <summary>Strings that are record data: names, both e-mail addresses, the terms, and every id.</summary>
        public IEnumerable<string> RecordData =>
        [
            Tag,
            .. new[] { Party, OtherParty, DeletedParty }.Select(p => p.Id.ToString("D")),
            .. new[] { Artifact, DeletedArtifact }.Select(a => a.Id.ToString("D")),
            Open.Id.ToString("D"), DeletedNegotiation.Id.ToString("D"),
            OpenOffer.Id.ToString("D"), DeletedOffer.Id.ToString("D"),
            Party.Sub
        ];

        /// <summary>A live record of the type the route's pattern names.</summary>
        public Guid LiveId(AdminRoute row) => TypeOf(row) switch
        {
            "participants" => Party.Id,
            "artifacts" => Artifact.Id,
            "negotiations" => Open.Id,
            "offers" => OpenOffer.Id,
            "audit" => AuditEntry,
            _ => AdminRouteTable.NoSuchId
        };

        /// <summary>A soft-deleted record of the type the route's pattern names.</summary>
        public Guid DeletedId(AdminRoute row) => TypeOf(row) switch
        {
            "participants" => DeletedParty.Id,
            "artifacts" => DeletedArtifact.Id,
            "negotiations" => DeletedNegotiation.Id,
            "offers" => DeletedOffer.Id,
            _ => throw new ArgumentOutOfRangeException(nameof(row))
        };

        public string PathOf(AdminRoute row) => row.HasId ? row.PathFor(LiveId(row)) : row.Pattern;
    }

    private static string TypeOf(AdminRoute row)
    {
        foreach (var type in new[] { "participants", "artifacts", "negotiations", "offers", "audit" })
        {
            if (row.Pattern.Contains($"/{type}/{{id:guid}}", StringComparison.Ordinal))
                return type;
        }

        return string.Empty;
    }

    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        // The owner's account, with a password and an enrolled authenticator: the security settings read it.
        if (!await db.AdminCoreOwnerAccounts.AnyAsync(a => a.Email == OwnerEmail))
        {
            var account = AdminCoreOwnerAccount.Create(OwnerEmail);
            account.SetPasswordHash(AdminPasswordHasher.Hash($"Pw-{Guid.NewGuid():N}-xY9!"));
            account.MarkRecoveryCodesIssued();
            account.CompleteEnrollment(scope.ServiceProvider.GetRequiredService<TotpSecretProtector>().Encrypt(Rfc6238Totp.GenerateSecret()));
            db.AdminCoreOwnerAccounts.Add(account);
            await db.SaveChangesAsync();
        }

        var tag = "rec" + Guid.NewGuid().ToString("N")[..10];

        var party = Participant.Create($"{tag}-Alpha");
        party.UpdateLoginEmail($"{tag}-login@example.test");
        party.UpdateContactEmail($"{tag}-contact@example.test");
        var other = Participant.Create($"{tag}-Beta");
        var deletedParty = Participant.Create($"{tag}-Gone");
        db.Participants.AddRange(party, other, deletedParty);

        var artifact = Artifact.Create(party.Sub, [SubjectEntity.Create($"{tag}-Widget", $"{tag}-described")], "sell");
        var deletedArtifact = Artifact.Create(party.Sub, [SubjectEntity.Create($"{tag}-OldWidget", "gone")], "sell");
        db.Artifacts.AddRange(artifact, deletedArtifact);

        var (open, _) = Negotiation.Create(artifact.Id, party.Sub, other.Sub, "sell", "buy", null, null);
        var (deletedNegotiation, _) = Negotiation.Create(artifact.Id, party.Sub, other.Sub, "sell", "buy", null, null);
        db.Negotiations.AddRange(open!, deletedNegotiation!);

        var (openOffer, _) = Offer.Create(open!.Id, party.Sub, other.Sub, 123.45m, "usd", $"{tag}-terms");
        var (deletedOffer, _) = Offer.Create(deletedNegotiation!.Id, party.Sub, other.Sub, 9m, "usd", $"{tag}-old-terms");
        db.Offers.AddRange(openOffer!, deletedOffer!);
        await db.SaveChangesAsync();

        var at = DateTimeOffset.UtcNow;
        await db.Participants.Where(p => p.Id == deletedParty.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, at));
        await db.Artifacts.Where(a => a.Id == deletedArtifact.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.DeletedAt, at));
        await db.Negotiations.Where(n => n.Id == deletedNegotiation.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, at));
        await db.Offers.Where(o => o.Id == deletedOffer!.Id).ExecuteUpdateAsync(s => s.SetProperty(o => o.DeletedAt, at));

        // One audit entry about the live participant, written by a real edit.
        var session = await NewSessionAsync();
        var etag = await ETagAsync($"/admin/api/participants/{party.Id}", session);
        var edit = await SendAsync("PATCH", $"/admin/api/participants/{party.Id}", new Ask
        {
            Session = session,
            IfMatch = etag,
            Body = JsonSerializer.Serialize(new { displayName = $"{tag}-Alpha-edited" })
        });
        Assert.True(edit.Status == 200, edit.ToString());
        var audit = await db.AdminAuditLog.AsNoTracking()
            .Where(a => a.EntityId == party.Id)
            .Select(a => a.Id)
            .SingleAsync();

        return new Seed(tag, party, other, deletedParty, artifact, deletedArtifact, open, deletedNegotiation, openOffer!, deletedOffer!, audit);
    }

    private async Task<string> ETagAsync(string apiPath, Guid session)
    {
        using var client = Client();
        using var request = new HttpRequestMessage(HttpMethod.Get, apiPath);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={session:D}");
        using var response = await client.SendAsync(request);
        Assert.True(response.Headers.TryGetValues("ETag", out var values), $"No ETag on {apiPath}: {(int)response.StatusCode}");
        return values!.Single();
    }

    // ---- readings ----

    /// <summary>
    /// Every row of the four entity tables, every column the admin can see or
    /// change, as one string. Equal strings: nothing in them changed.
    /// </summary>
    private async Task<string> EntityTablesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var participants = (await db.Participants.AsNoTracking().ToListAsync())
            .Select(p => new { p.Id, p.Sub, p.DisplayName, p.LoginEmail, p.ContactEmail, p.IsActive, p.CreatedAt, p.UpdatedAt, p.DeletedAt, p.Version })
            .OrderBy(p => p.Id);
        var artifacts = (await db.Artifacts.AsNoTracking().Include(a => a.Entities).ToListAsync())
            .Select(a => new
            {
                a.Id, a.OwnerParticipantId, a.Intent, a.CreatedAt, a.UpdatedAt, a.DeletedAt, a.Version,
                Entities = a.Entities.OrderBy(e => e.Id).Select(e => new { e.Id, e.Name, e.Description })
            })
            .OrderBy(a => a.Id);
        var negotiations = (await db.Negotiations.AsNoTracking().ToListAsync())
            .Select(n => new { n.Id, n.ArtifactId, n.PartyAParticipantId, n.PartyBParticipantId, n.Status, n.StartsAt, n.EndsAt, n.CreatedAt, n.UpdatedAt, n.DeletedAt, n.Version })
            .OrderBy(n => n.Id);
        var offers = (await db.Offers.AsNoTracking().ToListAsync())
            .Select(o => new { o.Id, o.NegotiationId, o.FromParticipantId, o.ToParticipantId, o.Status, o.Amount, o.Currency, o.Terms, o.CreatedAt, o.UpdatedAt, o.DeletedAt, o.Version })
            .OrderBy(o => o.Id);
        return JsonSerializer.Serialize(new { participants, artifacts, negotiations, offers });
    }

    private async Task<int> AuditCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<DealowareDbContext>().AdminAuditLog.CountAsync();
    }

    private async Task<string> AuditEntryAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var entry = await scope.ServiceProvider.GetRequiredService<DealowareDbContext>().AdminAuditLog.AsNoTracking().SingleAsync(a => a.Id == id);
        return JsonSerializer.Serialize(new
        {
            entry.Id, entry.Action, entry.EntityType, entry.EntityId, entry.ActorEmail, entry.BeforeSnapshot, entry.AfterSnapshot,
            entry.CorrelationId
        });
    }

    /// <summary>No name, address, terms or id of a seeded record anywhere in the reply.</summary>
    private static void AssertNoRecordData(Reply reply, Seed seed, string what)
    {
        foreach (var value in seed.RecordData)
            Assert.False(reply.Body.Contains(value, StringComparison.OrdinalIgnoreCase), $"{what}: the reply shows record data ({value}): {reply}");
        foreach (var cookie in reply.SetCookies)
            Assert.DoesNotContain(seed.Tag, cookie, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertForbidden(Reply reply, string path, string what)
    {
        Assert.True(reply.Status == 403, $"{what}: {reply}");
        Assert.Equal(path.StartsWith("/admin/api/", StringComparison.Ordinal) ? ForbiddenJson : ForbiddenPage, reply.Body);
    }

    /// <summary>A page's own per-reply values (the script nonce, the anti-forgery token in the meta and in the sign-out form), masked.</summary>
    private static string WithoutPerReplyValues(string body)
        => PerReplyValues().Replace(body, m => m.Groups[1].Value + new string('x', m.Groups[2].Length) + m.Groups[3].Value);

    [GeneratedRegex("""(nonce="|<meta name="dw-csrf" content="|name="dw_csrf" value=")([^"]+)(")""")]
    private static partial Regex PerReplyValues();

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    /// <summary>Every file of a static folder, as the address it is served at.</summary>
    private static IEnumerable<string> FilesOf(AdminStaticFolder folder)
    {
        var root = Path.Combine(RepoRoot(), "src", "Dealoware.Api", folder.Folder);
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != ".gitkeep")
            .Select(f => folder.UrlPrefix + "/" + Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal);
    }
}
