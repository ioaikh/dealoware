using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// T04 piece 5: how long the session has left, and "Stay signed in"
/// (<see cref="AdminSessionEndpoints"/>; Spec §8.7; TD-ADM-176, the server
/// side of TD-ADM-UI-auth-14 and TD-ADM-UI-auth-24). The status read is the
/// one accepted request that does not renew the idle window; Stay signed in
/// renews the idle window only, behind the Origin and anti-forgery checks.
/// Every time here comes from the test clock. What the page does with them is
/// covered by e2e/console/session-timer.spec.ts.
/// </summary>
[Collection("WebAppTests")]
public sealed class AdminSessionTimerTests : IDisposable
{
    private const string AdminHost = AdminHostOptions.ProductionAdminHost;
    private const string OwnerEmail = "io@aiknowhow.com";
    private const string Status = AdminSessionEndpoints.StatusPath;
    private const string StaySignedIn = AdminSessionEndpoints.StaySignedInPath;

    private static readonly DateTimeOffset T0 = new(2026, 11, 26, 9, 0, 0, TimeSpan.Zero);

    private readonly IsolatedWebApplicationFactory _factory;
    private readonly TestAdminClock _clock;
    private readonly DateTimeOffset _clockBefore;

    public AdminSessionTimerTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
        _clock = factory.Services.GetRequiredService<TestAdminClock>();
        _clockBefore = _clock.UtcNow;
        _clock.UtcNow = T0;
    }

    public void Dispose() => _clock.UtcNow = _clockBefore;

    [Fact]
    public void TheTwoLimits_AreSpec_8_7()
    {
        Assert.Equal(TimeSpan.FromMinutes(30), AdminSession.IdleTimeout);
        Assert.Equal(TimeSpan.FromHours(8), AdminSession.AbsoluteLifetime);
        Assert.Equal("/admin/api/session", Status);
        Assert.Equal("/admin/api/session/stay-signed-in", StaySignedIn);
    }

    // ---- the status read ----

    [Fact]
    public async Task StatusRead_SaysBothEnds_AndDoesNotRenewTheIdleWindow()
    {
        var session = await SeedSessionAsync(createdAgo: TimeSpan.FromHours(1), lastActiveAgo: TimeSpan.FromMinutes(10));
        var before = await RowAsync(session);

        using var response = await SendAsync(HttpMethod.Get, Status, session);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("no-store", string.Join(",", response.Headers.GetValues("Cache-Control")));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            new[] { "idleExpiresAt", "absoluteExpiresAt", "idleSecondsLeft", "absoluteSecondsLeft" },
            body.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.Equal("2026-11-26T09:20:00.0000000Z", body.GetProperty("idleExpiresAt").GetString());
        Assert.Equal("2026-11-26T16:00:00.0000000Z", body.GetProperty("absoluteExpiresAt").GetString());
        Assert.Equal(20 * 60, body.GetProperty("idleSecondsLeft").GetInt64());
        Assert.Equal(7 * 3600, body.GetProperty("absoluteSecondsLeft").GetInt64());
        // Nothing about the owner or the session id.
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(OwnerEmail, raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(session.ToString("D"), raw, StringComparison.OrdinalIgnoreCase);
        Assert.False(response.Headers.Contains("Set-Cookie"));

        var after = await RowAsync(session);
        Assert.Equal(before.LastActivityAt, after.LastActivityAt);
        Assert.Equal(before.AbsoluteExpiresAt, after.AbsoluteExpiresAt);
    }

    [Fact]
    public async Task StatusReads_EveryMinute_NeverKeepTheSessionAlive_ItEndsAt30MinutesIdle()
    {
        var session = await SeedSessionAsync();
        for (var minute = 0; minute < 30; minute++)
        {
            using var read = await SendAsync(HttpMethod.Get, Status, session);
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            var body = await read.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((30 - minute) * 60, body.GetProperty("idleSecondsLeft").GetInt64());
            _clock.Advance(TimeSpan.FromMinutes(1));
        }

        // 30 minutes after the last activity: the read itself is refused, and so is everything else.
        using var last = await SendAsync(HttpMethod.Get, Status, session);
        AssertGate401(last);
        using var me = await SendAsync(HttpMethod.Get, "/admin/api/me", session);
        AssertGate401(me);
        using var page = await SendAsync(HttpMethod.Get, "/admin/participants", session);
        Assert.Equal(HttpStatusCode.Found, page.StatusCode);
        Assert.Equal("/admin/sign-in#ended", Assert.Single(page.Headers.GetValues("Location")));
    }

    [Fact]
    public async Task StatusRead_AtTheEdges_OneSecondBeforeEachEndAndAtIt()
    {
        var idle = await SeedSessionAsync(lastActiveAgo: TimeSpan.FromMinutes(30) - TimeSpan.FromSeconds(1));
        using (var response = await SendAsync(HttpMethod.Get, Status, idle))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idleSecondsLeft").GetInt64());
        }

        _clock.Advance(TimeSpan.FromSeconds(1));
        using (var response = await SendAsync(HttpMethod.Get, Status, idle))
            AssertGate401(response);

        _clock.UtcNow = T0;
        var absolute = await SeedSessionAsync(createdAgo: TimeSpan.FromHours(8) - TimeSpan.FromSeconds(1), lastActiveAgo: TimeSpan.Zero);
        using (var response = await SendAsync(HttpMethod.Get, Status, absolute))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, body.GetProperty("absoluteSecondsLeft").GetInt64());
            Assert.Equal(30 * 60, body.GetProperty("idleSecondsLeft").GetInt64());
        }

        _clock.Advance(TimeSpan.FromSeconds(1));
        using (var response = await SendAsync(HttpMethod.Get, Status, absolute))
            AssertGate401(response);
    }

    public static TheoryData<string> NoValidSession => new()
    {
        "none", "not-a-guid", "unknown", "idle-expired", "absolute-expired", "second-step-not-done", "another-email"
    };

    [Theory]
    [MemberData(nameof(NoValidSession))]
    public async Task StatusRead_WithoutAValidSession_IsTheGates401_NeverARedirect(string state)
    {
        using var response = await SendAsync(HttpMethod.Get, Status, cookie: await SessionCookieForAsync(state));
        AssertGate401(response);
    }

    /// <summary>
    /// Only the exact path and GET skip the renewal. Every other request the
    /// gate accepts renews, as before: another spelling of the path, another
    /// method on it, the Stay signed in path, and any other route or page.
    /// </summary>
    [Theory]
    [InlineData("GET", "/admin/api/SESSION")]
    [InlineData("GET", "/admin/api/session/")]
    [InlineData("HEAD", "/admin/api/session")]
    [InlineData("GET", "/admin/api/session/stay-signed-in")]
    [InlineData("GET", "/admin/api/me")]
    [InlineData("GET", "/admin/")]
    [InlineData("GET", "/admin/assets/session-timer.js")]
    public async Task EveryOtherAcceptedRequest_StillRenewsTheIdleWindow(string method, string path)
    {
        var session = await SeedSessionAsync(lastActiveAgo: TimeSpan.FromMinutes(10));
        using var response = await SendAsync(new HttpMethod(method), path, session);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(T0, (await RowAsync(session)).LastActivityAt);
    }

    [Fact]
    public async Task StatusRead_WithAQuery_IsStillTheNonRenewingRead()
    {
        // The query is not part of the path: the read is the same read, and renews nothing.
        var session = await SeedSessionAsync(lastActiveAgo: TimeSpan.FromMinutes(10));
        using var response = await SendAsync(HttpMethod.Get, Status + "?idleExpiresAt=2099-01-01", session);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(T0 - TimeSpan.FromMinutes(10), (await RowAsync(session)).LastActivityAt);
    }

    // ---- Stay signed in ----

    [Fact]
    public async Task StaySignedIn_At28MinutesIdle_RenewsTheIdleWindowOnly_TheEightHourEndStays()
    {
        var session = await SeedSessionAsync(createdAgo: TimeSpan.FromHours(2), lastActiveAgo: TimeSpan.FromMinutes(28));
        var before = await RowAsync(session);
        var (pair, token) = await AntiForgeryAsync();

        using var response = await PostStayAsync(session, pair, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2026-11-26T09:30:00.0000000Z", body.GetProperty("idleExpiresAt").GetString());
        Assert.Equal("2026-11-26T15:00:00.0000000Z", body.GetProperty("absoluteExpiresAt").GetString());
        Assert.Equal(30 * 60, body.GetProperty("idleSecondsLeft").GetInt64());
        Assert.Equal(6 * 3600, body.GetProperty("absoluteSecondsLeft").GetInt64());

        var after = await RowAsync(session);
        Assert.Equal(T0, after.LastActivityAt);
        Assert.Equal(before.AbsoluteExpiresAt, after.AbsoluteExpiresAt);
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        // The same session, not a new one: no session cookie is issued, nor any row added.
        Assert.DoesNotContain(SetCookies(response), c => c.StartsWith(AdminSessionCookie.Name + "=", StringComparison.Ordinal));
        Assert.Equal(1, await SessionRowCountAsync(session));

        // 29 minutes on, the session is still alive: 31 minutes after the activity before the renewal.
        _clock.Advance(TimeSpan.FromMinutes(29));
        using var later = await SendAsync(HttpMethod.Get, Status, session);
        Assert.Equal(HttpStatusCode.OK, later.StatusCode);
    }

    [Fact]
    public async Task StaySignedIn_IgnoresAnyExpiryTheClientSends_InTheBodyAHeaderOrACookie()
    {
        var session = await SeedSessionAsync(createdAgo: TimeSpan.FromHours(1), lastActiveAgo: TimeSpan.FromMinutes(20));
        var before = await RowAsync(session);
        var (pair, token) = await AntiForgeryAsync();
        const string farFuture = "2099-12-31T23:59:59Z";

        using var post = Request(HttpMethod.Post, StaySignedIn, Join(Join(SessionPair(session), pair), "dw_admin_session_expires=" + farFuture));
        post.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        post.Headers.TryAddWithoutValidation("X-Session-Expires", farFuture);
        post.Content = JsonContent.Create(new
        {
            idleExpiresAt = farFuture,
            absoluteExpiresAt = farFuture,
            expiresAt = farFuture,
            idleSecondsLeft = int.MaxValue,
            absoluteSecondsLeft = int.MaxValue
        });
        using var response = await Client().SendAsync(post);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var after = await RowAsync(session);
        Assert.Equal(T0, after.LastActivityAt);
        Assert.Equal(before.AbsoluteExpiresAt, after.AbsoluteExpiresAt);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(30 * 60, body.GetProperty("idleSecondsLeft").GetInt64());
        Assert.Equal(7 * 3600, body.GetProperty("absoluteSecondsLeft").GetInt64());
    }

    [Fact]
    public async Task StaySignedIn_FromSevenHours55_EveryMinute_NeverGoesPastEightHours()
    {
        var session = await SeedSessionAsync(createdAgo: TimeSpan.FromHours(8) - TimeSpan.FromMinutes(5), lastActiveAgo: TimeSpan.Zero);
        var absoluteEnd = (await RowAsync(session)).AbsoluteExpiresAt;
        Assert.Equal(T0 + TimeSpan.FromMinutes(5), absoluteEnd);
        var (pair, token) = await AntiForgeryAsync();

        for (var minute = 0; minute <= 7; minute++)
        {
            using var response = await PostStayAsync(session, pair, token);
            if (minute < 5)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal((5 - minute) * 60, body.GetProperty("absoluteSecondsLeft").GetInt64());
                Assert.Equal("2026-11-26T09:05:00.0000000Z", body.GetProperty("absoluteExpiresAt").GetString());
                Assert.Equal(absoluteEnd, (await RowAsync(session)).AbsoluteExpiresAt);
            }
            else
            {
                // At 8 hours and after: the gate's 401, and the row is not touched.
                AssertGate401(response);
                Assert.Equal(absoluteEnd, (await RowAsync(session)).AbsoluteExpiresAt);
            }

            _clock.Advance(TimeSpan.FromMinutes(1));
        }

        using var page = await SendAsync(HttpMethod.Get, "/admin/", session);
        Assert.Equal(HttpStatusCode.Found, page.StatusCode);
        Assert.Equal("/admin/sign-in#ended", Assert.Single(page.Headers.GetValues("Location")));
    }

    [Theory]
    [InlineData("no-token")]
    [InlineData("wrong-token")]
    [InlineData("token-without-its-cookie")]
    [InlineData("another-site")]
    [InlineData("no-origin")]
    [InlineData("null-origin")]
    public async Task StaySignedIn_WithoutTheOriginOrTheAntiForgeryToken_IsRefused_AndRenewsNothing(string fault)
    {
        var session = await SeedSessionAsync(lastActiveAgo: TimeSpan.FromMinutes(28));
        var before = await RowAsync(session);
        var (pair, token) = await AntiForgeryAsync();

        var cookies = fault == "token-without-its-cookie" ? SessionPair(session) : Join(SessionPair(session), pair);
        using var post = Request(HttpMethod.Post, StaySignedIn, cookies);
        if (fault != "no-token")
            post.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, fault == "wrong-token" ? new string('A', 64) : token);
        post.Content = JsonContent.Create(new { });
        // A client that adds no Origin of its own.
        using var client = _factory.Server.CreateClient();
        var origin = fault switch
        {
            "another-site" => "https://evil.example",
            "no-origin" => null,
            "null-origin" => "null",
            _ => AdminHostOptions.ProductionAdminOrigin
        };
        if (origin is not null)
            post.Headers.TryAddWithoutValidation("Origin", origin);
        using var response = await client.SendAsync(post);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var after = await RowAsync(session);
        Assert.Equal(before.LastActivityAt, after.LastActivityAt);
        Assert.Equal(before.AbsoluteExpiresAt, after.AbsoluteExpiresAt);
    }

    [Theory]
    [MemberData(nameof(NoValidSession))]
    public async Task StaySignedIn_WithoutAValidSession_IsTheGates401(string state)
    {
        var (pair, token) = await AntiForgeryAsync();
        using var post = Request(HttpMethod.Post, StaySignedIn, Join(await SessionCookieForAsync(state), pair));
        post.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        post.Content = JsonContent.Create(new { });
        using var response = await Client().SendAsync(post);
        AssertGate401(response);
    }

    [Fact]
    public async Task StaySignedIn_ByGet_Answers405()
    {
        var session = await SeedSessionAsync(lastActiveAgo: TimeSpan.FromMinutes(5));
        using var response = await SendAsync(HttpMethod.Get, StaySignedIn, session);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    // ---- the page's script, read as text ----

    [Fact]
    public void TimerScript_AsksOnlyTheTwoPaths_NavigatesOnlyOnTheGates401_AndKeepsToTheHouseRules()
    {
        var script = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Dealoware.Api", AdminSignedInPage.AssetFolder, "session-timer.js"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains($"var STATUS = \"{Status}\";", script, StringComparison.Ordinal);
        Assert.Contains($"var STAY_SIGNED_IN = \"{StaySignedIn}\";", script, StringComparison.Ordinal);
        Assert.Contains("var GATE_DENIED = \"Unauthorized\";", script, StringComparison.Ordinal);
        Assert.Contains("return reply.status === 401 && (reply.body || {}).error === GATE_DENIED;", script, StringComparison.Ordinal);
        // One GET of the status, one POST of Stay signed in, through the shared helper only.
        Assert.Single(Regex.Matches(script, @"AdminRequest\.get\("));
        Assert.Contains("AdminRequest.get(STATUS)", script, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(script, @"AdminRequest\.post\("));
        Assert.Contains("AdminRequest.post(STAY_SIGNED_IN, {})", script, StringComparison.Ordinal);
        // The one navigation: the page asks for itself anew, and only in end(), after the gate's 401.
        Assert.Single(Regex.Matches(script, @"location\.reload\(\)"));
        Assert.Contains("ending = true;\n    clearTimeout(timer);\n    location.reload();", script, StringComparison.Ordinal);
        // Copy from the test design (titles, buttons); the two sentences are new.
        foreach (var copy in new[] { "\"Still there?\"", "\"Stay signed in\"", "\"Sign out\"", "\"Your session is ending\"", "\"OK\"" })
            Assert.Contains(copy, script, StringComparison.Ordinal);
        Assert.Contains("dialog.setAttribute(\"role\", \"alertdialog\");", script, StringComparison.Ordinal);

        foreach (var banned in new[]
                 {
                     "innerHTML", "outerHTML", "insertAdjacentHTML", "document.write", "localStorage", "sessionStorage",
                     "indexedDB", "document.cookie", "mode:", "fetch(", "eval(", "new Function", "location.href",
                     "location.assign", "location.replace", ".submit()", "setInterval", "XMLHttpRequest", "sendBeacon"
                 })
        {
            Assert.DoesNotContain(banned, script, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task NeitherIsAudited()
    {
        var session = await SeedSessionAsync(lastActiveAgo: TimeSpan.FromMinutes(28));
        var before = await AuditCountAsync();
        var (pair, token) = await AntiForgeryAsync();
        using (var read = await SendAsync(HttpMethod.Get, Status, session))
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using (var stay = await PostStayAsync(session, pair, token))
            Assert.Equal(HttpStatusCode.OK, stay.StatusCode);
        Assert.Equal(before, await AuditCountAsync());
    }

    // ---- helpers ----

    private static void AssertGate401(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Location"));
        Assert.Equal("{\"error\":\"Unauthorized\"}", response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
    }

    private HttpClient Client() => _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false
    });

    private static HttpRequestMessage Request(HttpMethod method, string path, string? cookie = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, Guid? session = null, string? cookie = null)
    {
        using var request = Request(method, path, Join(session is { } id ? SessionPair(id) : null, cookie));
        return await Client().SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostStayAsync(Guid session, string pair, string token)
    {
        using var post = Request(HttpMethod.Post, StaySignedIn, Join(SessionPair(session), pair));
        post.Headers.TryAddWithoutValidation(AdminAntiForgeryService.HeaderName, token);
        post.Content = JsonContent.Create(new { });
        return await Client().SendAsync(post);
    }

    /// <summary>
    /// An anti-forgery pair from the sign-in page, asked for without the
    /// session cookie, so that getting it renews nothing.
    /// </summary>
    private async Task<(string Pair, string Token)> AntiForgeryAsync()
    {
        using var page = await Client().SendAsync(Request(HttpMethod.Get, AdminSignedOutExemptions.SignIn));
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var cookie = Assert.Single(SetCookies(page), c => c.StartsWith(AdminAntiForgeryService.CookieName + "=", StringComparison.Ordinal));
        return (cookie.Split(';', 2)[0], AdminPageAntiForgery.ReadToken(await page.Content.ReadAsStringAsync()));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static IReadOnlyList<string> SetCookies(HttpResponseMessage response)
        => response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : [];

    private static string SessionPair(Guid session) => $"{AdminSessionCookie.Name}={session:D}";

    private static string? Join(string? a, string? b)
        => string.IsNullOrEmpty(a) ? b : string.IsNullOrEmpty(b) ? a : a + "; " + b;

    private async Task<Guid> SeedSessionAsync(
        string email = OwnerEmail,
        bool secondStepDone = true,
        TimeSpan? createdAgo = null,
        TimeSpan? lastActiveAgo = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var now = _clock.UtcNow;
        var session = AdminSession.Create(email, ipHmac: "testhmac-not-an-ip", now: now - (createdAgo ?? TimeSpan.Zero));
        session.UpdateActivity(now - (lastActiveAgo ?? createdAgo ?? TimeSpan.Zero));
        if (secondStepDone)
            session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private async Task<string?> SessionCookieForAsync(string state) => state switch
    {
        "none" => null,
        "not-a-guid" => AdminSessionCookie.Name + "=not-a-session",
        "unknown" => SessionPair(Guid.NewGuid()),
        "idle-expired" => SessionPair(await SeedSessionAsync(createdAgo: TimeSpan.FromMinutes(31))),
        "absolute-expired" => SessionPair(await SeedSessionAsync(createdAgo: TimeSpan.FromHours(8) + TimeSpan.FromMinutes(1), lastActiveAgo: TimeSpan.FromMinutes(1))),
        "second-step-not-done" => SessionPair(await SeedSessionAsync(secondStepDone: false)),
        "another-email" => SessionPair(await SeedSessionAsync(email: "someone.else@example.test")),
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };

    private async Task<AdminSession> RowAsync(Guid session)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminSessions.AsNoTracking().SingleAsync(s => s.Id == session);
    }

    private async Task<int> SessionRowCountAsync(Guid session)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminSessions.CountAsync(s => s.Id == session);
    }

    private async Task<int> AuditCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        return await db.AdminAuditLog.CountAsync();
    }
}
