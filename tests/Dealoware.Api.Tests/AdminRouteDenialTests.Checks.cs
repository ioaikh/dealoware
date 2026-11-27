using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Tests;

/// <summary>
/// The denial checks (T08 spec, "The denial checks"), one theory per class of
/// row, each run over every row of the route table it applies to. Each
/// refusal is checked for its status, that the reply has no record data,
/// and that the four entity tables are as they were.
/// </summary>
public partial class AdminRouteDenialTests
{
    // ---- Signed in: no session, a lapsed one, one without its second step, another address, an engine token ----

    [Theory]
    [MemberData(nameof(GatedRows))]
    public async Task TdAdm002_TdAdm003_TdAdm004_ASignedInRoute_WithoutAGoodSession_Is401OrSignIn_WithNoRecordData(string key)
    {
        var row = Row(key);
        var seed = await SeedAsync();
        await EngineTokenAsync();
        var tables = await EntityTablesAsync();
        var audits = await AuditCountAsync();

        foreach (var state in BadSessions)
        {
            // Its own methods, and OPTIONS, which no route maps: the gate answers before routing.
            foreach (var method in row.Methods.Append("OPTIONS"))
            {
                var ask = await BadSessionAsync(state, new Ask { Body = IsChange(method) ? "{}" : null });
                var reply = await SendAsync(method, seed.PathOf(row), ask);
                var what = $"{method} {row.Pattern} with {state}";

                if (row.IsPage && method == "GET")
                {
                    // A browser's GET of a page: to sign-in with the "session ended" note, and no page.
                    Assert.True(reply.Status == 302, $"{what}: {reply}");
                    Assert.Equal(AdminSignInPages.SessionEndedLocation, reply.Location);
                    Assert.Equal(string.Empty, reply.Body);
                }
                else
                {
                    Assert.True(reply.Status == 401, $"{what}: {reply}");
                    Assert.Equal(UnauthorizedBody, reply.Body);
                    Assert.Null(reply.Location);
                }

                Assert.DoesNotContain(reply.SetCookies, c => c.StartsWith(AdminSessionCookie.Name + "=", StringComparison.Ordinal));
                AssertNoRecordData(reply, seed, what);
            }
        }

        Assert.Equal(tables, await EntityTablesAsync());
        Assert.Equal(audits, await AuditCountAsync());
    }

    // ---- Sign-in in progress: no pending token, or one that ran out or was used ----

    [Theory]
    [MemberData(nameof(SignInInProgressRows))]
    public async Task TdAdm004_ASignInInProgressRoute_WithoutAUsablePendingToken_GoesBackToStep1OrIs401(string key)
    {
        var row = Row(key);
        var seed = await SeedAsync();
        var tables = await EntityTablesAsync();

        foreach (var state in new[] { "none", "forged", "run-out", "used" })
        {
            foreach (var method in row.Methods)
            {
                var cookie = state switch
                {
                    "none" => null,
                    "forged" => AdminPendingAuthCookie.Name + "=" + new string('0', 64),
                    _ => await NewPendingAsync(state)
                };
                var reply = await SendAsync(method, row.Pattern, new Ask { Cookie = cookie, Body = IsChange(method) ? "{}" : null });
                var what = $"{method} {row.Pattern} with {state}";

                if (row.IsPage && method == "GET")
                {
                    Assert.True(reply.Status == 302, $"{what}: {reply}");
                    Assert.Equal(AdminSignInPages.SentBackLocation, reply.Location);
                    Assert.Equal(string.Empty, reply.Body);
                }
                else
                {
                    Assert.True(reply.Status == 401, $"{what}: {reply}");
                    Assert.Equal(UnauthorizedBody, reply.Body);
                }

                AssertNoRecordData(reply, seed, what);
            }
        }

        Assert.Equal(tables, await EntityTablesAsync());

        // The cookie these states are made of is the real one: a usable pending token opens step 2.
        if (row.Pattern == AdminSignedOutExemptions.SignInCode)
        {
            var usable = await SendAsync("GET", row.Pattern, new Ask { Cookie = await NewPendingAsync("usable") });
            Assert.True(usable.Status == 200, $"GET {row.Pattern} with a usable pending token: {usable}");
        }
    }

    // ---- Signed out: answers without a session, and is on the exemption list ----

    [Theory]
    [MemberData(nameof(SignedOutRows))]
    public async Task TdAdm003_ASignedOutRoute_IsOnTheExemptionList_AndItsPageAnswersWithoutASession(string key)
    {
        var row = Row(key);
        foreach (var method in row.Methods)
            Assert.NotNull(AdminSignedOutExemptions.FindRule(row.Pattern, method));

        if (!row.Methods.Contains("GET"))
            return;

        var reply = await SendAsync("GET", row.Pattern);
        Assert.True(reply.Status == 200, $"GET {row.Pattern}: {reply}");
        Assert.Equal("no-store", reply.CacheControl);
    }

    // ---- Any change method: another site, no origin, no or a wrong anti-forgery token ----

    public static readonly string[] CrossSiteKinds =
    [
        "no-origin-no-referer", "another-sites-origin", "origin-null", "another-sites-referer",
        "no-anti-forgery-header", "wrong-anti-forgery-header", "header-without-its-cookie"
    ];

    [Theory]
    [MemberData(nameof(ChangeRows))]
    public async Task TdAdm003_TdAdm175_AChange_FromAnotherSite_OrWithoutItsAntiForgeryToken_Is403_AndChangesNothing(string key)
    {
        var row = Row(key);
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var tables = await EntityTablesAsync();
        var audits = await AuditCountAsync();

        foreach (var method in row.Methods.Where(IsChange))
        {
            foreach (var kind in CrossSiteKinds)
            {
                var ask = kind switch
                {
                    "no-origin-no-referer" => new Ask { Session = session, Origin = null },
                    "another-sites-origin" => new Ask { Session = session, Origin = "https://evil.example" },
                    "origin-null" => new Ask { Session = session, Origin = "null" },
                    "another-sites-referer" => new Ask { Session = session, Origin = null, Referer = "https://evil.example/admin/" },
                    "no-anti-forgery-header" => new Ask { Session = session, AntiForgery = AntiForgery.CookieWithoutHeader },
                    "wrong-anti-forgery-header" => new Ask { Session = session, AntiForgery = AntiForgery.WrongHeader },
                    _ => new Ask { Session = session, AntiForgery = AntiForgery.HeaderWithoutCookie }
                };
                var path = seed.PathOf(row);
                var reply = await SendAsync(method, path, new Ask
                {
                    Session = ask.Session, Origin = ask.Origin, Referer = ask.Referer, AntiForgery = ask.AntiForgery,
                    Body = "{}", ConfirmToken = new string('0', 64), IfMatch = "\"0\""
                });
                var what = $"{method} {row.Pattern}, {kind}";
                AssertForbidden(reply, path, what);
                AssertNoRecordData(reply, seed, what);
            }
        }

        Assert.Equal(tables, await EntityTablesAsync());
        Assert.Equal(audits, await AuditCountAsync());
        // Sign-out among them: the session is still good.
        Assert.True(await IsSignedInAsync(session));
    }

    // ---- Any route, signed in: a method it does not map is 405, or S-D3's 404. Never a success. ----

    [Theory]
    [MemberData(nameof(Patterns))]
    public async Task TdAdm003_AnyRoute_SignedIn_AMethodItDoesNotMap_Is405OrNotFound_NeverASuccess(string pattern)
    {
        var mapped = RowsHere.Where(r => r.Pattern == pattern).SelectMany(r => r.Methods).ToHashSet();
        var row = RowsHere.First(r => r.Pattern == pattern);
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var tables = await EntityTablesAsync();
        var audits = await AuditCountAsync();

        foreach (var method in EveryMethod.Where(m => !mapped.Contains(m)))
        {
            var reply = await SendAsync(method, seed.PathOf(row), new Ask { Session = session, Body = IsChange(method) ? "{}" : null });
            var what = $"{method} {pattern}";
            if (reply.Status == 404)
            {
                // Only a page's GET may get S-D3; nothing else gets a 404 for a route that is there.
                Assert.True(method == "GET" && row.IsPage, $"{what}: {reply}");
                Assert.Contains(AdminConsoleCopy.NotFoundHeading, reply.Body, StringComparison.Ordinal);
            }
            else
            {
                Assert.True(reply.Status == 405, $"{what}: {reply}");
            }

            AssertNoRecordData(reply, seed, what);
        }

        Assert.Equal(tables, await EntityTablesAsync());
        Assert.Equal(audits, await AuditCountAsync());
        Assert.True(await IsSignedInAsync(session));
    }

    // ---- The audit routes: every change method is 405, and the log stays as it was (TD-ADM-101) ----

    [Fact]
    public async Task TdAdm101_TheAuditRoutes_Answer405_ToEveryChange_AndTheLogStaysAsItWas()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var entry = await AuditEntryAsync(seed.AuditEntry);
        var audits = await AuditCountAsync();
        var rows = RowsHere.Where(r => r.OnlyRefuses).ToList();
        Assert.Equal(["/admin/api/audit", "/admin/api/audit/{id:guid}"], rows.Select(r => r.Pattern));

        foreach (var row in rows)
        {
            Assert.Equal(ChangeMethods, row.Methods);
            foreach (var method in row.Methods)
            {
                var reply = await SendAsync(method, seed.PathOf(row), new Ask { Session = session, Body = "{\"action\":\"x\"}" });
                Assert.True(reply.Status == 405, $"{method} {row.Pattern}: {reply}");
                AssertNoRecordData(reply, seed, $"{method} {row.Pattern}");
            }
        }

        Assert.Equal(audits, await AuditCountAsync());
        Assert.Equal(entry, await AuditEntryAsync(seed.AuditEntry));
    }

    // ---- Sign-out by GET signs nobody out (TD-ADM-175) ----

    [Fact]
    public async Task TdAdm175_SignOutByGetOrHead_SignsNobodyOut()
    {
        var session = await NewSessionAsync();
        var audits = await AuditCountAsync();
        foreach (var path in new[] { AdminSignedOutExemptions.SignOut, AdminSignedOutExemptions.ApiSignOut })
        {
            foreach (var method in new[] { "GET", "HEAD" })
            {
                var reply = await SendAsync(method, path, new Ask { Session = session });
                Assert.True(reply.Status == 405, $"{method} {path}: {reply}");
                Assert.DoesNotContain(reply.SetCookies, c => c.StartsWith(AdminSessionCookie.Name + "=", StringComparison.Ordinal));
                Assert.True(await IsSignedInAsync(session), $"{method} {path} signed the session out.");
            }
        }

        Assert.Equal(audits, await AuditCountAsync());
    }

    // ---- Every signed-in page and read, with a good session: Cache-Control: no-store (TD-ADM-173) ----

    [Fact]
    public async Task TdAdm173_EverySignedInPageAndRead_WithAGoodSession_IsNoStore()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var reads = RowsHere.Where(r => r.Class == AdminRouteClass.SignedIn && r.Methods.Contains("GET")).ToList();
        Assert.NotEmpty(reads);
        foreach (var row in reads)
        {
            var reply = await SendAsync("GET", seed.PathOf(row), new Ask { Session = session });
            Assert.True(reply.Status == 200, $"GET {row.Pattern}: {reply}");
            Assert.Equal("no-store", reply.CacheControl);
        }

        foreach (var file in FilesOf(AdminRouteTable.StaticFolders.Single(f => f.Class == AdminRouteClass.SignedIn)))
        {
            var reply = await SendAsync("GET", file, new Ask { Session = session });
            Assert.True(reply.Status == 200, $"GET {file}: {reply}");
            Assert.Equal("no-store", reply.CacheControl);
        }
    }

    // ---- The static folders: the signed-in one only with a session, the signed-out one without ----

    [Fact]
    public async Task TdAdm003_TheSignedInFiles_WithoutAGoodSession_Are401_AndTheSignedOutFiles_AnswerWithoutOne()
    {
        var seed = await SeedAsync();
        var signedIn = FilesOf(AdminRouteTable.StaticFolders.Single(f => f.Class == AdminRouteClass.SignedIn)).ToList();
        var signedOut = FilesOf(AdminRouteTable.StaticFolders.Single(f => f.Class == AdminRouteClass.SignedOut)).ToList();
        Assert.NotEmpty(signedIn);
        Assert.NotEmpty(signedOut);

        foreach (var state in BadSessions)
        {
            foreach (var file in signedIn)
            {
                var reply = await SendAsync("GET", file, await BadSessionAsync(state, new Ask()));
                Assert.True(reply.Status == 401, $"GET {file} with {state}: {reply}");
                Assert.Equal(UnauthorizedBody, reply.Body);
                AssertNoRecordData(reply, seed, file);
            }
        }

        foreach (var file in signedOut)
        {
            var reply = await SendAsync("GET", file);
            Assert.True(reply.Status == 200, $"GET {file}: {reply}");
        }
    }

    // ---- Where the policy hides whether a record exists: missing and deleted look the same (TD-ADM-179) ----

    public static TheoryData<string> RecordRows
        => KeysOf(RowsHere.Where(r => r.Class == AdminRouteClass.SignedIn && r.HasId && !r.OnlyRefuses && TypeOf(r) != "audit"));

    [Theory]
    [MemberData(nameof(RecordRows))]
    public async Task TdAdm179_AMissingAndADeletedRecord_GetTheSameStatusBodyAndSize(string key)
    {
        var row = Row(key);
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var tables = await EntityTablesAsync();
        var etag = await ETagAsync($"/admin/api/{TypeOf(row)}/{seed.LiveId(row)}", session);

        foreach (var method in row.Methods)
        {
            var ask = new Ask
            {
                Session = session,
                Body = method == "PATCH" ? GoodEditOf(TypeOf(row)) : IsChange(method) ? "{}" : null,
                IfMatch = method == "PATCH" ? etag : null,
                ConfirmToken = method == "DELETE" ? new string('0', 64) : null
            };
            var missing = await SendAsync(method, row.PathFor(AdminRouteTable.NoSuchId), ask);
            var deleted = await SendAsync(method, row.PathFor(seed.DeletedId(row)), ask);
            var what = $"{method} {row.Pattern}";

            // Read, edit and the delete intent find no record: 404. The delete itself never reaches the
            // record: it stops earlier, at its confirm token, which only an intent for a record that is
            // there can give. So the DELETE's answer (the token refusal, 409) cannot tell missing from
            // deleted either. A record "hidden" by the policy is not a third case here: the policy gives
            // the Core owner every record of the four types, and hides fields, never whether a record exists.
            Assert.True(missing.Status == (method == "DELETE" ? 409 : 404), $"{what}, missing: {missing}");
            Assert.True(missing.Status == deleted.Status, $"{what}: missing {missing} / deleted {deleted}");
            Assert.Equal(WithoutPerReplyValues(missing.Body), WithoutPerReplyValues(deleted.Body));
            Assert.Equal(missing.Length, deleted.Length);
            AssertNoRecordData(deleted, seed, what);
        }

        Assert.Equal(tables, await EntityTablesAsync());
    }

    /// <summary>An edit the record's type takes, so that a refusal can only be about the record.</summary>
    private static string GoodEditOf(string type) => type switch
    {
        "participants" => """{"displayName":"Renamed"}""",
        "artifacts" => """{"name":"Renamed"}""",
        "negotiations" => """{"endsAt":"2031-01-01T00:00:00+00:00"}""",
        _ => """{"status":"Cancelled"}"""
    };

    // ---- A write never repeats what was sent, nor a field the policy hides (TD-ADM-005, TD-ADM-141) ----

    public static TheoryData<string> WriteRows
        => KeysOf(RowsHere.Where(r => r.Class is AdminRouteClass.SignedIn or AdminRouteClass.TestOnly && r.Methods.Any(IsChange) && !r.OnlyRefuses));

    [Theory]
    [MemberData(nameof(WriteRows))]
    public async Task TdAdm005_TdAdm141_AWriteWithBadOrForbiddenValues_IsRefusedOrIgnored_RepeatsNoneOfThem_AndChangesNoRecord(string key)
    {
        var row = Row(key);
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var tables = await EntityTablesAsync();
        var probe = "probe" + Guid.NewGuid().ToString("N");
        var etag = TypeOf(row) is "participants" or "artifacts" or "negotiations" or "offers"
            ? await ETagAsync($"/admin/api/{TypeOf(row)}/{seed.LiveId(row)}", session)
            : "\"0\"";

        // Editable fields with values that break their rules, and every field the owner may not write.
        var body = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["displayName"] = probe + new string('n', 300),
            ["name"] = probe + new string('n', 4100),
            ["description"] = probe + new string('d', 4100),
            ["isActive"] = probe,
            ["status"] = probe,
            ["endsAt"] = probe,
            ["ownerParticipantId"] = probe,
            ["amount"] = probe,
            ["currency"] = probe,
            ["terms"] = probe,
            ["sub"] = probe,
            ["loginEmail"] = probe + "@example.test",
            ["contactEmail"] = probe + "@example.test",
            ["createdAt"] = probe,
            ["fromParticipantId"] = probe,
            ["toParticipantId"] = probe,
            ["currentPassword"] = probe,
            ["newPassword"] = probe,
            ["password"] = probe,
            ["code"] = probe,
            ["recoveryCode"] = probe
        });

        foreach (var method in row.Methods.Where(IsChange))
        {
            var reply = await SendAsync(method, seed.PathOf(row), new Ask { Session = session, Body = body, IfMatch = etag });
            var what = $"{method} {row.Pattern}";
            Assert.False(reply.Body.Contains(probe, StringComparison.Ordinal), $"{what} repeats what was sent: {reply}");
            Assert.DoesNotContain(seed.Party.LoginEmail!, reply.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(seed.Party.ContactEmail!, reply.Body, StringComparison.OrdinalIgnoreCase);
            if (reply.Status == 400)
                AssertNoRecordData(reply, seed, what);
            Assert.True(reply.Status == ExpectedWriteStatus(method, row.Pattern), $"{what}: {reply}");
        }

        Assert.Equal(tables, await EntityTablesAsync());
    }

    /// <summary>
    /// What each write answers to that body. A route that reads a body refuses
    /// it; only a route that reads no body at all may succeed, because the
    /// values were never looked at. A new write route needs its line here.
    /// </summary>
    private static int ExpectedWriteStatus(string method, string pattern)
    {
        if (method == "PATCH")
            return 400; // the generic refusal: a field the owner may not write, or a value that breaks its rule
        if (method == "DELETE")
            return 428; // no confirm token: refused before anything is read
        if (pattern.EndsWith("/delete-intent", StringComparison.Ordinal))
            return pattern.StartsWith("/admin/api/artifacts/", StringComparison.Ordinal)
                ? 409  // reads no body; the seeded artifact has a negotiation, so its delete is blocked
                : 200; // reads no body: asks for a confirm token, changes no record
        return pattern switch
        {
            AdminSecuritySettingsEndpoints.PasswordPath => 400,
            AdminSecuritySettingsEndpoints.StepUpPath => 400,
            AdminSecuritySettingsEndpoints.RecoveryCodesPath => 400,
            AdminSecuritySettingsEndpoints.AuthenticatorPath => 400,
            AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath => 403, // no step-up done: "step-up required"
            AdminSessionEndpoints.StaySignedInPath => 200,                  // reads no body
            AdminRecoveryNotice.DismissPath => 200,                         // reads no body
            AdminCsrfProbeEndpoint.Path => 200,                             // test only; reads no body
            _ => throw new InvalidOperationException($"{method} {pattern}: a write route with no expected answer; add one.")
        };
    }

    // ---- The two addresses: admin routes only on the admin address, engine routes only on the engine's (TD-ADM-001) ----

    [Fact]
    public async Task TdAdm001_EveryAdminRouteAndFile_OnTheEnginesAddress_Is404()
    {
        var seed = await SeedAsync();
        var session = await NewSessionAsync();
        var paths = RowsHere.SelectMany(r => r.Methods.Select(m => (Method: m, Path: seed.PathOf(r))))
            .Concat(AdminRouteTable.StaticFolders.SelectMany(FilesOf).Select(f => (Method: "GET", Path: f)))
            .ToList();

        foreach (var (method, path) in paths)
        {
            var reply = await SendAsync(method, path, new Ask { Host = EngineHost, Session = session, Body = IsChange(method) ? "{}" : null });
            Assert.True(reply.Status == 404, $"{method} {path} on the engine's address: {reply}");
            Assert.Equal(string.Empty, reply.Body);
        }
    }

    [Fact]
    public async Task TdAdm001_EveryEngineRoute_OnTheAdminAddress_Is404()
    {
        var session = await NewSessionAsync();
        var engine = EngineEndpoints();
        Assert.NotEmpty(engine);
        foreach (var (method, path) in engine)
        {
            var reply = await SendAsync(method, path, new Ask { Session = session, Bearer = await EngineTokenAsync(), Body = IsChange(method) ? "{}" : null });
            Assert.True(reply.Status == 404, $"{method} {path} on the admin address: {reply}");
            Assert.Equal(string.Empty, reply.Body);
        }
    }

    [Fact]
    public async Task TdAdm002_EveryEngineRoute_WithTheAdminSessionCookie_AnswersAsWithoutIt()
    {
        var session = await NewSessionAsync();
        var engine = EngineEndpoints();
        Assert.NotEmpty(engine);
        foreach (var (method, path) in engine)
        {
            var body = IsChange(method) ? "{}" : null;
            var without = await SendAsync(method, path, new Ask { Host = EngineHost, AntiForgery = AntiForgery.None, Body = body });
            var with = await SendAsync(method, path, new Ask { Host = EngineHost, AntiForgery = AntiForgery.None, Body = body, Session = session });
            var what = $"{method} {path} on the engine's address";
            Assert.True(without.Status == with.Status, $"{what}: without the cookie {without}, with it {with}");
            if (without.Status is >= 200 and < 300)
            {
                // A route that makes something new each time (registration) answers with new values: the same fields.
                Assert.Equal(FieldNames(without.Body), FieldNames(with.Body));
            }
            else
            {
                Assert.Equal(without.Body, with.Body);
            }
        }
    }

    private static string FieldNames(string json)
        => string.Join(",", JsonDocument.Parse(json).RootElement.EnumerateObject().Select(p => p.Name));

    /// <summary>
    /// Every engine route the app maps (anything not under /admin, but /health,
    /// which both addresses answer), each method, with any route value filled.
    /// </summary>
    private List<(string Method, string Path)> EngineEndpoints()
        => MappedEndpoints()
            .Where(e => !IsAdminPattern(e.Pattern) && e.Pattern != "/health")
            .Select(e => (e.Method, Path: System.Text.RegularExpressions.Regex.Replace(e.Pattern, "\\{[^}]+\\}", AdminRouteTable.NoSuchId.ToString("D"))))
            .Distinct()
            .OrderBy(e => e.Path, StringComparer.Ordinal).ThenBy(e => e.Method, StringComparer.Ordinal)
            .ToList();

    // ---- Test-only routes: absent in Production ----

    [Fact]
    public async Task TdAdm001_TheTestOnlyRoutes_AreNotThereInProduction()
    {
        var testOnly = AdminRouteTable.Routes.Where(r => r.Class == AdminRouteClass.TestOnly).ToList();
        Assert.NotEmpty(testOnly);

        using var production = new EnvironmentWebApplicationFactory("Production", EnvironmentWebApplicationFactory.TestSigningKey64);
        var owner = production.Services.GetRequiredService<IOptions<CoreOwnerOptions>>().Value.Email;
        Guid session;
        using (var scope = production.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            // Production makes no schema at start (the migrate step does); this test host's own store needs one.
            await db.Database.EnsureCreatedAsync();
            var row = AdminSession.Create(owner, ipHmac: "testhmac-not-an-ip", now: scope.ServiceProvider.GetRequiredService<IAdminClock>().UtcNow);
            row.MarkTotpVerified();
            db.AdminSessions.Add(row);
            await db.SaveChangesAsync();
            session = row.Id;
        }

        var endpoints = production.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText).ToList();
        foreach (var row in testOnly)
        {
            Assert.DoesNotContain(row.Pattern, endpoints);
            foreach (var method in row.Methods)
            {
                var reply = await SendAsync(method, row.Pattern, new Ask { Session = session, Body = "{}" }, production);
                Assert.True(reply.Status == 404, $"{method} {row.Pattern} in Production: {reply}");
            }
        }

        // The session is good there: the same request reaches a real route.
        Assert.Equal(200, (await SendAsync("GET", "/admin/api/me", new Ask { Session = session }, production)).Status);
    }
}
