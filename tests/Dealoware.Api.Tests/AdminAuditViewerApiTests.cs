using System.Net;
using System.Reflection;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// T07 (A7 Step 15), the server side of the audit viewer: one entry by id,
/// the action filter with several names, the outcome of each entry, and no
/// way to change an entry. Case ids: TD-ADM-100, 101, 053, 150 and
/// TD-ADM-UI-na-X02 (what the viewer reads). The pages are in
/// AdminAuditPageTests.
/// </summary>
[Collection("WebAppTests")]
public class AdminAuditViewerApiTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string OwnerEmail = "io@aiknowhow.com";
    private const string ClientIp = "203.0.113.91";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminAuditViewerApiTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<Guid> SessionAsync(string email = OwnerEmail, bool verified = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(email, ipHmac: "testhmac-not-an-ip");
        if (verified)
            session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private async Task<AdminAuditEntry> SeedAsync(Func<IAdminAuditRecorder, AdminAuditEntry> make)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var entry = make(scope.ServiceProvider.GetRequiredService<IAdminAuditRecorder>());
        db.AdminAuditLog.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    /// <summary>A moment no other test writes at: its own second, long ago.</summary>
    private static DateTimeOffset OwnMoment()
        => new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(Random.Shared.Next(0, 400_000_000));

    private static string Iso(DateTimeOffset at) => Uri.EscapeDataString(at.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"));

    private static HttpRequestMessage Request(
        HttpMethod method,
        string path,
        Guid? session = null,
        string? csrf = null,
        string? csrfCookie = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        var cookies = new List<string>();
        if (session is { } id)
            cookies.Add($"{AdminSessionCookie.Name}={id:D}");
        if (!string.IsNullOrWhiteSpace(csrfCookie))
            cookies.Add(csrfCookie);
        if (cookies.Count > 0)
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        if (!string.IsNullOrWhiteSpace(csrf))
            request.Headers.TryAddWithoutValidation(AdminAntiForgeryCookie.HeaderName, csrf);
        return request;
    }

    private static async Task<(string Token, string CookieHeader)> IssueCsrfAsync(HttpClient client)
    {
        using var response = await client.SendAsync(Request(HttpMethod.Get, AdminSignedOutExemptions.SignIn));
        var token = await AdminPageAntiForgery.ReadTokenAsync(response);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookies));
        var af = setCookies.Single(c => c.StartsWith(AdminAntiForgeryCookie.Name + "=", StringComparison.Ordinal));
        return (token, af.Split(';', 2)[0]);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    // ---- one entry by id ----

    [Fact]
    public async Task TdAdm101_GetAuditEntry_GivesTheEntryAsTheListGivesIt()
    {
        var entityId = Guid.NewGuid();
        var correlation = Guid.NewGuid();
        var at = OwnMoment();
        var seeded = await SeedAsync(r => r.Entity(
            AdminAuditActions.EntityEdit, OwnerEmail, ClientIp, "Participant", entityId,
            beforeSnapshot: "{\"DisplayName\":\"Old name\"}",
            afterSnapshot: "{\"DisplayName\":\"New name\"}",
            correlationId: correlation,
            timestamp: at));
        var session = await SessionAsync();
        var client = _factory.CreateClient();

        using var response = await client.SendAsync(Request(HttpMethod.Get, $"/admin/api/audit/{seeded.Id:D}", session));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var entry = JsonDocument.Parse(body).RootElement;
        Assert.Equal(
            new[]
            {
                "id", "timestamp", "action", "actorEmail", "ipHmacPrefix", "entityType", "entityId", "reasonClass",
                "outcome", "correlationId", "beforeSnapshot", "afterSnapshot"
            },
            entry.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.Equal(seeded.Id, entry.GetProperty("id").GetGuid());
        Assert.Equal(at, entry.GetProperty("timestamp").GetDateTimeOffset());
        Assert.Equal(AdminAuditActions.EntityEdit, entry.GetProperty("action").GetString());
        Assert.Equal(OwnerEmail, entry.GetProperty("actorEmail").GetString());
        Assert.Equal("Participant", entry.GetProperty("entityType").GetString());
        Assert.Equal(entityId, entry.GetProperty("entityId").GetGuid());
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("reasonClass").ValueKind);
        Assert.Equal("success", entry.GetProperty("outcome").GetString());
        Assert.Equal(correlation, entry.GetProperty("correlationId").GetGuid());
        Assert.Equal("{\"DisplayName\":\"Old name\"}", entry.GetProperty("beforeSnapshot").GetString());
        Assert.Equal("{\"DisplayName\":\"New name\"}", entry.GetProperty("afterSnapshot").GetString());

        // TD-ADM-053: the first 12 characters of the keyed hash; never the hash, never the address.
        var prefix = entry.GetProperty("ipHmacPrefix").GetString()!;
        Assert.Equal(AdminAuditEndpoints.IpHmacPrefixLength, prefix.Length);
        Assert.StartsWith(prefix, seeded.IpHmac, StringComparison.Ordinal);
        Assert.True(seeded.IpHmac.Length > prefix.Length);
        Assert.DoesNotContain(seeded.IpHmac, body, StringComparison.Ordinal);
        Assert.DoesNotContain(ClientIp, body, StringComparison.Ordinal);
        Assert.DoesNotContain("203.0.113", body, StringComparison.Ordinal);

        // The same entry, field for field, in the list.
        using var list = await client.SendAsync(Request(HttpMethod.Get, $"/admin/api/audit?entityId={entityId:D}", session));
        var item = Assert.Single((await JsonAsync(list)).GetProperty("items").EnumerateArray());
        Assert.Equal(entry.GetRawText(), item.GetRawText());
    }

    [Fact]
    public async Task TdAdm101_GetAuditEntry_NeedsAVerifiedOwner_AndAnUnknownIdIsAPlain404()
    {
        var marker = $"only-with-a-session-{Guid.NewGuid():N}";
        var seeded = await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, marker, ClientIp, AdminAuditActions.ReasonBadPassword));
        var client = _factory.CreateClient();
        var path = $"/admin/api/audit/{seeded.Id:D}";

        foreach (var session in new Guid?[]
                 {
                     null,
                     Guid.NewGuid(),
                     await SessionAsync(verified: false),
                     await SessionAsync(email: "other@example.com")
                 })
        {
            using var refused = await client.SendAsync(Request(HttpMethod.Get, path, session));
            Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
            var text = await refused.Content.ReadAsStringAsync();
            Assert.Equal("{\"error\":\"Unauthorized\"}", text);
            Assert.DoesNotContain(marker, text, StringComparison.Ordinal);
        }

        var owner = await SessionAsync();
        using var found = await client.SendAsync(Request(HttpMethod.Get, path, owner));
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal("failure", (await JsonAsync(found)).GetProperty("outcome").GetString());

        foreach (var missing in new[]
                 {
                     $"/admin/api/audit/{Guid.NewGuid():D}",
                     "/admin/api/audit/not-an-id",
                     $"/admin/api/audit/{seeded.Id:D}/before",
                     "/admin/api/audit/00000000-0000-0000-0000-00000000000"
                 })
        {
            using var none = await client.SendAsync(Request(HttpMethod.Get, missing, owner));
            Assert.Equal(HttpStatusCode.NotFound, none.StatusCode);
            Assert.Equal(string.Empty, await none.Content.ReadAsStringAsync());
            // Without a session the same address says nothing about what exists.
            using var gate = await client.SendAsync(Request(HttpMethod.Get, missing));
            Assert.Equal(HttpStatusCode.Unauthorized, gate.StatusCode);
        }

        // The admin host only.
        var elsewhere = new HttpRequestMessage(HttpMethod.Get, path);
        elsewhere.Headers.Host = "core.dealoware.com";
        elsewhere.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={owner:D}");
        using var otherHost = await client.SendAsync(elsewhere);
        Assert.Equal(HttpStatusCode.NotFound, otherHost.StatusCode);
    }

    [Fact]
    public async Task TdAdm101_NoMethodChangesAnEntry_EveryOtherMethodIs405_AndTheEntryStaysAsItWas()
    {
        var seeded = await SeedAsync(r => r.Entity(
            AdminAuditActions.EntityDelete, OwnerEmail, ClientIp, "Offer", Guid.NewGuid(),
            beforeSnapshot: "{\"OfferTerms\":\"as written\"}"));
        var session = await SessionAsync();
        var client = _factory.CreateClient();
        var (csrf, csrfCookie) = await IssueCsrfAsync(client);

        foreach (var path in new[] { "/admin/api/audit", $"/admin/api/audit/{seeded.Id:D}" })
        {
            foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete })
            {
                var request = Request(method, path, session, csrf, csrfCookie);
                request.Content = new StringContent("{\"action\":\"login_success\",\"beforeSnapshot\":null}", System.Text.Encoding.UTF8, "application/json");
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            }
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var stored = await db.AdminAuditLog.AsNoTracking().SingleAsync(e => e.Id == seeded.Id);
        Assert.Equal(AdminAuditActions.EntityDelete, stored.Action);
        Assert.Equal("{\"OfferTerms\":\"as written\"}", stored.BeforeSnapshot);

        // The whole audit surface: two reads, and a 405 for everything else.
        var routes = scope.ServiceProvider.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? string.Empty).StartsWith("/admin/api/audit", StringComparison.Ordinal))
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])
                .Select(m => m + " " + e.RoutePattern.RawText + " " + e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName))
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "DELETE /admin/api/audit AdminAuditMutationsDenied",
                "DELETE /admin/api/audit/{id:guid} AdminAuditEntryMutationsDenied",
                "GET /admin/api/audit AdminAuditList",
                "GET /admin/api/audit/{id:guid} AdminAuditEntry",
                "PATCH /admin/api/audit AdminAuditMutationsDenied",
                "PATCH /admin/api/audit/{id:guid} AdminAuditEntryMutationsDenied",
                "POST /admin/api/audit AdminAuditMutationsDenied",
                "POST /admin/api/audit/{id:guid} AdminAuditEntryMutationsDenied",
                "PUT /admin/api/audit AdminAuditMutationsDenied",
                "PUT /admin/api/audit/{id:guid} AdminAuditEntryMutationsDenied"
            },
            routes);
    }

    // ---- the action filter ----

    [Fact]
    public async Task ActionFilter_TakesSeveralNames_AnEntryWithAnyOneOfThem()
    {
        var at = OwnMoment();
        var failure = await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, OwnerEmail, ClientIp, AdminAuditActions.ReasonBadPassword, at));
        var success = await SeedAsync(r => r.Auth(AdminAuditActions.LoginSuccess, OwnerEmail, ClientIp, null, at));
        var signOut = await SeedAsync(r => r.Auth(AdminAuditActions.SignOut, OwnerEmail, ClientIp, null, at));
        var changed = await SeedAsync(r => r.Auth(AdminAuditActions.PasswordChanged, OwnerEmail, ClientIp, null, at));
        var session = await SessionAsync();
        var client = _factory.CreateClient();
        var window = $"from={Iso(at)}&to={Iso(at)}";

        async Task<Guid[]> IdsAsync(string action)
        {
            using var response = await client.SendAsync(Request(HttpMethod.Get, $"/admin/api/audit?{window}&action={action}", session));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payload = await JsonAsync(response);
            var ids = payload.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).OrderBy(i => i).ToArray();
            Assert.Equal(ids.Length, payload.GetProperty("total").GetInt32());
            return ids;
        }

        static Guid[] Of(params AdminAuditEntry[] entries) => entries.Select(e => e.Id).OrderBy(i => i).ToArray();

        Assert.Equal(Of(failure, success, signOut, changed), await IdsAsync(string.Empty));
        Assert.Equal(Of(success), await IdsAsync("login_success"));
        Assert.Equal(Of(success, signOut), await IdsAsync("login_success,sign_out"));
        Assert.Equal(Of(success, signOut), await IdsAsync("sign_out%2Clogin_success"));
        // A name with a dot in it, a name given twice, a name nothing here has.
        Assert.Equal(Of(failure, changed), await IdsAsync("account.password_changed,login_failure,login_failure"));
        Assert.Equal(Of(), await IdsAsync("entity_edit,entity_delete"));
        // Every name at once is every entry.
        Assert.Equal(Of(failure, success, signOut, changed), await IdsAsync(string.Join(',', AdminAuditOutcome.Actions)));
    }

    [Theory]
    [InlineData("nope")]
    [InlineData("login_success,nope")]
    [InlineData("login_success,")]
    [InlineData(",login_success")]
    [InlineData("login_success,,sign_out")]
    [InlineData("LOGIN_SUCCESS")]
    [InlineData("login_success%20")]
    [InlineData("login_success;sign_out")]
    [InlineData("login_success%27%20OR%201%3D1--")]
    [InlineData("%3Cscript%3Ealert(1)%3C/script%3E")]
    public async Task ActionFilter_ANameOffTheFixedList_IsTheGeneric400(string action)
    {
        var session = await SessionAsync();
        using var response = await _factory.CreateClient().SendAsync(Request(HttpMethod.Get, $"/admin/api/audit?action={action}", session));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("{\"error\":\"BadRequest\"}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ActionFilter_OverItsLength_IsTheGeneric400()
    {
        var session = await SessionAsync();
        var many = string.Join(',', Enumerable.Repeat(AdminAuditActions.LoginSuccess, 80));
        Assert.True(many.Length > AdminAuditListCriteria.MaxActionsLength);
        using var response = await _factory.CreateClient().SendAsync(Request(HttpMethod.Get, $"/admin/api/audit?action={many}", session));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // The whole list fits.
        Assert.True(string.Join(',', AdminAuditOutcome.Actions).Length <= AdminAuditListCriteria.MaxActionsLength);
    }

    // ---- the outcome ----

    [Theory]
    // What was asked for was done.
    [InlineData("login_success", null, "success")]
    [InlineData("sign_out", null, "success")]
    [InlineData("recovery_code_use", null, "success")]
    [InlineData("reset_request", null, "success")]
    [InlineData("reset_complete", null, "success")]
    [InlineData("totp_enroll", null, "success")]
    [InlineData("totp_change", null, "success")]
    [InlineData("account.password_changed", null, "success")]
    [InlineData("account.recovery_codes_regenerated", null, "success")]
    [InlineData("bootstrap_link_issued", null, "success")]
    [InlineData("bootstrap_complete", null, "success")]
    [InlineData("entity_edit", null, "success")]
    [InlineData("entity_delete", null, "success")]
    // Judged, and did not pass.
    [InlineData("login_failure", "bad_password", "failure")]
    [InlineData("login_failure", "bad_2fa", "failure")]
    [InlineData("signin.second_factor_failed", "bad_2fa", "failure")]
    [InlineData("reset_complete_failed", "invalid_link", "failure")]
    [InlineData("bootstrap_complete_failed", "invalid_link", "failure")]
    [InlineData("account.password_change_failed", "bad_password", "failure")]
    [InlineData("account.step_up_failed", "bad_2fa", "failure")]
    [InlineData("account.totp_change_failed", "bad_2fa", "failure")]
    // Refused before it was judged.
    [InlineData("login_failure", "locked", "denied")]
    [InlineData("login_failure", "rate-limited", "denied")]
    [InlineData("login_failure", "captcha-failed", "denied")]
    [InlineData("lock_start", "locked", "denied")]
    [InlineData("reset_request_failed", "captcha-failed", "denied")]
    [InlineData("bootstrap_complete_failed", "captcha-failed", "denied")]
    [InlineData("account.password_change_failed", "locked", "denied")]
    [InlineData("account.totp_change_failed", "locked", "denied")]
    // A failure action written without its reason is still not a success.
    [InlineData("login_failure", null, "failure")]
    [InlineData("signin.second_factor_failed", null, "failure")]
    [InlineData("reset_request_failed", null, "failure")]
    [InlineData("reset_complete_failed", "", "failure")]
    [InlineData("account.step_up_failed", null, "failure")]
    [InlineData("lock_start", null, "denied")]
    // A reason the table does not name is a failure; an action it does not name follows its reason.
    [InlineData("login_failure", "something_new", "failure")]
    [InlineData("something_new", "locked", "denied")]
    [InlineData("something_new", "bad_password", "failure")]
    [InlineData("something_new", null, "success")]
    [InlineData("something_new", "", "success")]
    // An action the table does not name, written without a reason, is a failure when its name says so.
    [InlineData("something_new_failed", null, "failure")]
    [InlineData("something_new_failure", null, "failure")]
    [InlineData("account.something_failed", "", "failure")]
    [InlineData("something_failed", "rate-limited", "denied")]
    // Only at the end of the name, and only those two endings.
    [InlineData("failed_something", null, "success")]
    [InlineData("something_failed_over", null, "success")]
    [InlineData("something_FAILED", null, "success")]
    [InlineData(null, null, "success")]
    public void Outcome_ComesFromActionAndReasonClass_ByTheOneTable(string? action, string? reason, string expected)
        => Assert.Equal(expected, AdminAuditOutcome.Of(action, reason));

    [Fact]
    public void EveryActionTheAppWrites_IsInTheTable_WhichIsTheFiltersFixedList()
    {
        static IEnumerable<string> Names(Type type) => type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string) && !f.Name.StartsWith("Reason", StringComparison.Ordinal))
            .Select(f => (string)f.GetRawConstantValue()!);

        var written = Names(typeof(AdminAuditActions))
            .Concat(Names(typeof(AdminAuthEvents)))
            .Concat(Names(typeof(AdminAuthAction)))
            .Append(AdminPasswordResetEndpoints.RequestFailedEvent)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(a => a, StringComparer.Ordinal)
            .ToArray();

        // The two reset failures are named once, in the domain's list; the reset routes write them by that name.
        Assert.Equal("reset_request_failed", AdminAuditActions.ResetRequestFailed);
        Assert.Equal("reset_complete_failed", AdminAuditActions.ResetCompleteFailed);
        Assert.Equal(AdminAuditActions.ResetRequestFailed, AdminPasswordResetEndpoints.RequestFailedEvent);
        var resetRoutes = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Dealoware.Api", "Admin", "AdminPasswordResetEndpoints.cs"));
        Assert.DoesNotContain("\"reset_complete_failed\"", resetRoutes, StringComparison.Ordinal);
        Assert.DoesNotContain("\"reset_request_failed\"", resetRoutes, StringComparison.Ordinal);
        Assert.Contains("AdminAuditActions.ResetCompleteFailed,", resetRoutes, StringComparison.Ordinal);

        // A new action name belongs in AdminAuditOutcome's table, with its outcome.
        Assert.Equal(written, AdminAuditOutcome.Actions.OrderBy(a => a, StringComparer.Ordinal).ToArray());
        Assert.Equal(AdminAuditOutcome.Actions.Count, AdminAuditOutcome.Actions.Distinct(StringComparer.Ordinal).Count());
        Assert.All(AdminAuditOutcome.Actions, a => Assert.True(AdminAuditOutcome.IsKnownAction(a)));
        Assert.False(AdminAuditOutcome.IsKnownAction(null));
        Assert.False(AdminAuditOutcome.IsKnownAction("Login_Success"));
        // Each fits the stored column and the address a filter writes.
        Assert.All(AdminAuditOutcome.Actions, a => Assert.Matches("^[a-z0-9._]{1,64}$", a));
        Assert.All(
            AdminAuditOutcome.Actions,
            a => Assert.Contains(AdminAuditOutcome.Of(a, null), new[] { "success", "failure", "denied" }));
    }

    [Fact]
    public async Task TdAdm100_TheReadModel_CarriesTheOutcome_BesideTheReasonClass()
    {
        var at = OwnMoment();
        await SeedAsync(r => r.Auth(AdminAuditActions.LoginSuccess, OwnerEmail, ClientIp, null, at));
        await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, OwnerEmail, ClientIp, AdminAuditActions.ReasonBadPassword, at));
        await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, OwnerEmail, ClientIp, AdminAuditActions.ReasonLocked, at));
        await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, OwnerEmail, ClientIp, AdminAuditActions.ReasonCaptchaFailed, at));
        var session = await SessionAsync();

        using var response = await _factory.CreateClient().SendAsync(
            Request(HttpMethod.Get, $"/admin/api/audit?from={Iso(at)}&to={Iso(at)}", session));
        var seen = (await JsonAsync(response)).GetProperty("items").EnumerateArray()
            .Select(i => $"{i.GetProperty("action").GetString()}/{i.GetProperty("reasonClass").GetString()}/{i.GetProperty("outcome").GetString()}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "login_failure/bad_password/failure",
                "login_failure/captcha-failed/denied",
                "login_failure/locked/denied",
                "login_success//success"
            },
            seen);
    }

    // ---- what a snapshot may show ----

    [Fact]
    public async Task ASnapshotFieldTheOwnerMayNotRead_IsLeftOutOfTheReadModel_AtAnyDepth()
    {
        var entityId = Guid.NewGuid();
        const string denied = "NEVER-SHOWN-STRATEGY-TEXT";
        var seeded = await SeedAsync(r => r.Entity(
            AdminAuditActions.EntityEdit, OwnerEmail, ClientIp, "Participant", entityId,
            beforeSnapshot: "{\"DisplayName\":\"Before name\",\"StrategyBody\":\"" + denied + " one\","
                            + "\"nested\":{\"strategyBody\":\"" + denied + " two\",\"kept\":1},"
                            + "\"list\":[{\"StrategyBody\":\"" + denied + " three\",\"ContactEmail\":\"kept@example.test\"}]}",
            // The same field under other spellings: letter case, and "_", "-", "." and spaces between its words.
            afterSnapshot: "{\"DisplayName\":\"After name\",\"strategy_body\":\"" + denied + " four\",\"STRATEGYBODY\":\"" + denied + " five\","
                           + "\"Strategy-Body\":\"" + denied + " six\",\"strategy.body\":\"" + denied + " seven\",\"Strategy Body\":\"" + denied + " eight\","
                           + "\"STRATEGY_BODY\":\"" + denied + " nine\","
                           // Not that field: a name that only begins with it, and fields the owner may read, spelled as freely.
                           + "\"StrategyBodyNote\":\"kept note\",\"display_name\":\"kept display\",\"CONTACT-EMAIL\":\"kept-too@example.test\"}"));
        Assert.Contains(denied + " four", seeded.AfterSnapshot, StringComparison.Ordinal);
        // The row itself holds it: the read model is what leaves it out.
        Assert.Contains(denied, seeded.BeforeSnapshot, StringComparison.Ordinal);
        var session = await SessionAsync();
        var client = _factory.CreateClient();

        foreach (var path in new[] { $"/admin/api/audit/{seeded.Id:D}", $"/admin/api/audit?entityId={entityId:D}" })
        {
            using var response = await client.SendAsync(Request(HttpMethod.Get, path, session));
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain(denied, body, StringComparison.Ordinal);
            // As a key of either snapshot (each travels as a JSON string, read back here as written), under every spelling.
            var root = JsonDocument.Parse(body).RootElement;
            var entry = root.TryGetProperty("items", out var listed) ? Assert.Single(listed.EnumerateArray()) : root;
            var snapshots = entry.GetProperty("beforeSnapshot").GetString() + entry.GetProperty("afterSnapshot").GetString();
            foreach (var spelling in new[] { "strategyBody\"", "strategy_body", "Strategy-Body", "strategy.body", "Strategy Body" })
                Assert.DoesNotContain(spelling, snapshots, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"StrategyBodyNote\":\"kept note\"", snapshots, StringComparison.Ordinal);
            Assert.Contains("kept note", body, StringComparison.Ordinal);
            Assert.Contains("kept display", body, StringComparison.Ordinal);
            Assert.Contains("kept-too@example.test", body, StringComparison.Ordinal);
            Assert.Contains("Before name", body, StringComparison.Ordinal);
            Assert.Contains("kept@example.test", body, StringComparison.Ordinal);
            Assert.Contains("After name", body, StringComparison.Ordinal);
        }

        using var one = await client.SendAsync(Request(HttpMethod.Get, $"/admin/api/audit/{seeded.Id:D}", session));
        var before = JsonDocument.Parse((await JsonAsync(one)).GetProperty("beforeSnapshot").GetString()!).RootElement;
        Assert.Equal(new[] { "DisplayName", "nested", "list" }, before.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.Equal("{\"kept\":1}", before.GetProperty("nested").GetRawText());
        var after = JsonDocument.Parse((await JsonAsync(one)).GetProperty("afterSnapshot").GetString()!).RootElement;
        Assert.Equal(
            new[] { "DisplayName", "StrategyBodyNote", "display_name", "CONTACT-EMAIL" },
            after.EnumerateObject().Select(p => p.Name).ToArray());
    }

    // ---- a reason no read gives ----

    [Fact]
    public async Task TdAdm150_AFailedStepUp_CarriesNoReason_InTheListOrById_AndNoReasonFilterFindsIt()
    {
        var at = OwnMoment();
        var window = $"from={Iso(at)}&to={Iso(at)}";
        var stepUpCode = await SeedAsync(r => r.Auth(AdminAuditActions.StepUpFailed, OwnerEmail, ClientIp, AdminAuthReason.Bad2Fa, at));
        var stepUpPassword = await SeedAsync(r => r.Auth(AdminAuditActions.StepUpFailed, OwnerEmail, ClientIp, AdminAuthReason.BadPassword, at));
        var changeCode = await SeedAsync(r => r.Auth(AdminAuditActions.PasswordChangeFailed, OwnerEmail, ClientIp, AdminAuthReason.Bad2Fa, at));
        var changePassword = await SeedAsync(r => r.Auth(AdminAuditActions.PasswordChangeFailed, OwnerEmail, ClientIp, AdminAuthReason.BadPassword, at));
        // Shown as before: a step-up refused by a lock, a sign-in and an authenticator change whose code was wrong.
        var stepUpLocked = await SeedAsync(r => r.Auth(AdminAuditActions.StepUpFailed, OwnerEmail, ClientIp, AdminAuthReason.Locked, at));
        var signInCode = await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, OwnerEmail, ClientIp, AdminAuthReason.Bad2Fa, at));
        var signInPassword = await SeedAsync(r => r.Auth(AdminAuditActions.LoginFailure, OwnerEmail, ClientIp, AdminAuthReason.BadPassword, at));
        var totpCode = await SeedAsync(r => r.Auth(AdminAuditActions.TotpChangeFailed, OwnerEmail, ClientIp, AdminAuthReason.Bad2Fa, at));
        var withheld = new[] { stepUpCode, stepUpPassword, changeCode, changePassword };
        var session = await SessionAsync();
        var client = _factory.CreateClient();

        // By id: no reason, and the outcome is the same for a wrong password and a wrong code.
        foreach (var entry in withheld)
        {
            using var response = await client.SendAsync(Request(HttpMethod.Get, $"/admin/api/audit/{entry.Id:D}", session));
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body).RootElement;
            Assert.Equal(JsonValueKind.Null, json.GetProperty("reasonClass").ValueKind);
            Assert.Equal("failure", json.GetProperty("outcome").GetString());
            Assert.DoesNotContain("bad_2fa", body, StringComparison.Ordinal);
            Assert.DoesNotContain("bad_password", body, StringComparison.Ordinal);
        }

        // In the list: the same; the others keep theirs.
        using var list = await client.SendAsync(Request(HttpMethod.Get, $"/admin/api/audit?{window}", session));
        var items = (await JsonAsync(list)).GetProperty("items").EnumerateArray()
            .ToDictionary(i => i.GetProperty("id").GetGuid(), i => $"{i.GetProperty("reasonClass").GetString()}/{i.GetProperty("outcome").GetString()}");
        Assert.Equal(8, items.Count);
        Assert.All(withheld, e => Assert.Equal("/failure", items[e.Id]));
        Assert.Equal("locked/denied", items[stepUpLocked.Id]);
        Assert.Equal("bad_2fa/failure", items[signInCode.Id]);
        Assert.Equal("bad_password/failure", items[signInPassword.Id]);
        Assert.Equal("bad_2fa/failure", items[totpCode.Id]);

        // The reason filter does not find them either, alone or with their action; it still finds the others.
        async Task<Guid[]> FoundAsync(string query)
        {
            using var response = await client.SendAsync(Request(HttpMethod.Get, $"/admin/api/audit?{window}&{query}", session));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payload = await JsonAsync(response);
            var ids = payload.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).OrderBy(i => i).ToArray();
            Assert.Equal(ids.Length, payload.GetProperty("total").GetInt32());
            return ids;
        }

        static Guid[] Of(params AdminAuditEntry[] entries) => entries.Select(e => e.Id).OrderBy(i => i).ToArray();

        Assert.Equal(Of(signInCode, totpCode), await FoundAsync("reasonClass=bad_2fa"));
        Assert.Equal(Of(signInPassword), await FoundAsync("reasonClass=bad_password"));
        Assert.Equal(Of(), await FoundAsync("reasonClass=bad_2fa&action=account.step_up_failed"));
        Assert.Equal(Of(), await FoundAsync("reasonClass=bad_password&action=account.step_up_failed,account.password_change_failed"));
        Assert.Equal(Of(stepUpLocked), await FoundAsync("reasonClass=locked&action=account.step_up_failed"));
        Assert.Equal(Of(stepUpCode, stepUpPassword, stepUpLocked), await FoundAsync("action=account.step_up_failed"));

        // The stored rows keep their reasons: the log itself is whole.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var ids = withheld.Select(e => e.Id).ToArray();
        var stored = await db.AdminAuditLog.AsNoTracking().Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.ReasonClass);
        Assert.Equal(AdminAuthReason.Bad2Fa, stored[stepUpCode.Id]);
        Assert.Equal(AdminAuthReason.BadPassword, stored[stepUpPassword.Id]);
        Assert.Equal(AdminAuthReason.Bad2Fa, stored[changeCode.Id]);
        Assert.Equal(AdminAuthReason.BadPassword, stored[changePassword.Id]);

        Assert.True(AdminAuditOutcome.IsWithheldReason(AdminAuditActions.StepUpFailed, AdminAuthReason.Bad2Fa));
        Assert.False(AdminAuditOutcome.IsWithheldReason(AdminAuditActions.StepUpFailed, AdminAuthReason.Locked));
        Assert.False(AdminAuditOutcome.IsWithheldReason(AdminAuditActions.LoginFailure, AdminAuthReason.Bad2Fa));
        Assert.False(AdminAuditOutcome.IsWithheldReason(AdminAuditActions.TotpChangeFailed, AdminAuthReason.Bad2Fa));
        Assert.False(AdminAuditOutcome.IsWithheldReason(AdminAuditActions.StepUpFailed, null));
        Assert.False(AdminAuditOutcome.IsWithheldReason(null, AdminAuthReason.Bad2Fa));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
