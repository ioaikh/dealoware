using System.Net;
using System.Net.Http.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// R1 parallel reserve/consume tests. Names match the postgres-sec filter
/// <c>FullyQualifiedName~SecR|FullyQualifiedName~SecSc</c>.
/// </summary>
[Collection("WebAppTests")]
public class AdminLockoutReserveTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string OwnerEmail = "io@aiknowhow.com";

    private readonly IsolatedWebApplicationFactory _factory;
    private readonly string _password;

    public AdminLockoutReserveTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
        _password = "Pw-" + Guid.NewGuid().ToString("N") + "-xx";
        var directory = _factory.Services.GetRequiredService<InMemoryAdminCredentialDirectory>();
        directory.SetPasswordAsync(OwnerEmail, _password, CancellationToken.None).GetAwaiter().GetResult();
        directory.SeedTotpCode("246813");
    }

    [Fact]
    public async Task SecR1_ParallelPasswordAttempts_CannotExceedFive()
    {
        await ClearAsync();
        var client = CreateClient();
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 12).Select(_ => SendWrongAsync(client)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
        Assert.Equal(5, await CountAsync(AdminAuthScopes.Account, OwnerEmail));
        Assert.NotNull(await ActiveAsync(AdminAuthScopes.Account, OwnerEmail));
    }

    [Fact]
    public async Task SecR1_CorrectPasswordAfterFifthReserve_ReturnsLocked()
    {
        await ClearAsync();
        var client = CreateClient();
        for (var i = 0; i < 5; i++)
        {
            using var fail = await SendWrongAsync(client);
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        using var correct = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, correct.StatusCode);
        Assert.Contains(AdminAuthCopy.SignInFailure, await correct.Content.ReadAsStringAsync());
        Assert.NotNull(await ActiveAsync(AdminAuthScopes.Account, OwnerEmail));
    }

    [Fact]
    public async Task SecR1_NormalizedAccountKey_IsSharedAcrossCasing()
    {
        await ClearAsync();
        var client = CreateClient();
        using var upper = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail.ToUpperInvariant(), password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, upper.StatusCode);
        Assert.Equal(1, await CountAsync(AdminAuthScopes.Account, OwnerEmail.ToLowerInvariant()));
    }

    [Fact]
    public async Task SecR1_ParallelPendingConsume_IsSingleUse()
    {
        await ClearAsync();
        var client = CreateClient();
        using var step1 = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        step1.EnsureSuccessStatusCode();
        var pending = ReadCookie(step1, AdminSessionExemptions.PendingCookieName);
        Assert.False(string.IsNullOrEmpty(pending));

        var now = DateTimeOffset.UtcNow;
        var hash = AdminAuthService.HashToken(pending!);
        var results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => ConsumeOnNewScopeAsync(hash, now)));
        Assert.Equal(1, results.Count(ok => ok));
    }

    [Fact]
    public async Task SecR1_ParallelLinkConsume_IsSingleUse()
    {
        await ClearAsync();
        using var scope = _factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IAdminAuthTokenStore>();
        var raw = AdminAuthService.CreateOpaqueToken();
        var now = DateTimeOffset.UtcNow;
        await tokens.AddAsync(
            AdminAuthToken.Create(
                AdminAuthToken.KindReset,
                AdminAuthService.HashToken(raw),
                OwnerEmail.ToLowerInvariant(),
                now,
                TimeSpan.FromMinutes(30)),
            CancellationToken.None);
        await tokens.SaveChangesAsync(CancellationToken.None);

        var hash = AdminAuthService.HashToken(raw);
        var results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => ConsumeOnNewScopeAsync(hash, now)));
        Assert.Equal(1, results.Count(ok => ok));
    }

    [Fact]
    public async Task SecSc6_ParallelFailures_DoNotPassTheLimit()
    {
        await SecR1_ParallelPasswordAttempts_CannotExceedFive();
    }

    [Fact]
    public async Task SecR1_FourWrongThenCorrect_NoLock_SessionAlive()
    {
        await ClearAsync();
        var client = CreateClient();
        var sessionId = await SignInFullFallbackAsync(client);
        for (var i = 0; i < 4; i++)
        {
            using var fail = await SendWrongAsync(client);
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        using var correct = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, correct.StatusCode);
        Assert.Null(await ActiveAsync(AdminAuthScopes.Account, OwnerEmail));
        using var me = await AdminGetAsync(client, sessionId);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task SecR1_AccountPath_FourWrongThenCorrect_NoLock_SessionAlive()
    {
        await ClearAsync();
        var (password, secret) = await SeedEnrolledAccountAsync();
        var client = CreateClient();
        var sessionId = await SignInFullAccountAsync(client, password, secret);
        for (var i = 0; i < 4; i++)
        {
            using var fail = await SendAuthAsync(
                client,
                HttpMethod.Post,
                AdminAuthEndpoints.SignInPath,
                new { email = OwnerEmail, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        using var correct = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, correct.StatusCode);
        Assert.Null(await ActiveAsync(AdminAuthScopes.Account, OwnerEmail));
        using var me = await AdminGetAsync(client, sessionId);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task SecR1_FiveParallel_OneCorrect_AtMostFiveEvaluations()
    {
        await ClearAsync();
        var client = CreateClient();
        var directory = _factory.Services.GetRequiredService<InMemoryAdminCredentialDirectory>();
        var before = directory.PasswordEvaluations;
        var tasks = Enumerable.Range(0, 5).Select(i =>
            SendAuthAsync(
                client,
                HttpMethod.Post,
                AdminAuthEndpoints.SignInPath,
                new
                {
                    email = OwnerEmail,
                    password = i == 0 ? _password : "wrong",
                    turnstileToken = FakeTurnstileVerifier.ValidToken
                }));
        var responses = await Task.WhenAll(tasks);
        Assert.All(responses, r => Assert.True(
            r.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized));
        Assert.True(directory.PasswordEvaluations - before <= 5);
        Assert.True(await CountAsync(AdminAuthScopes.Account, OwnerEmail) <= 5);
        if (await ActiveAsync(AdminAuthScopes.Account, OwnerEmail) is not null)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Equal(0, db.AdminSessions.Count(s => s.Email == OwnerEmail && s.TotpVerified));
        }
    }

    [Fact]
    public async Task SecR1_ParallelIpBurst_StopsAtTwenty()
    {
        await ClearAsync();
        var client = CreateClient();
        var directory = _factory.Services.GetRequiredService<InMemoryAdminCredentialDirectory>();
        var before = directory.PasswordEvaluations;
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 25).Select(i => SendAuthAsync(
                client,
                HttpMethod.Post,
                AdminAuthEndpoints.SignInPath,
                new
                {
                    email = $"burst{i}@example.com",
                    password = "wrong",
                    turnstileToken = FakeTurnstileVerifier.ValidToken
                })));
        Assert.Equal(20, responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized));
        Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests));
        Assert.True(directory.PasswordEvaluations - before <= 20);
        Assert.Equal(20, await CountAsync(AdminAuthScopes.LoginIp, "127.0.0.1"));
    }

    [Fact]
    public async Task SecR1_AccountPath_Step1_IpThrottle()
    {
        await ClearAsync();
        var (password, _) = await SeedEnrolledAccountAsync();
        var client = CreateClient();
        await FillLoginIpAsync(client);
        using var owner = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.TooManyRequests, owner.StatusCode);
        Assert.False(owner.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task SecR1_AccountPath_Step2_IpThrottle()
    {
        await ClearAsync();
        var (password, secret) = await SeedEnrolledAccountAsync();
        var client = CreateClient();
        using var step1 = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);
        var pending = ReadCookie(step1, AdminPendingAuthCookie.Name)
            ?? ReadCookie(step1, AdminSessionExemptions.PendingCookieName);
        Assert.False(string.IsNullOrEmpty(pending));
        await FillLoginIpAsync(client);
        var code = Rfc6238Totp.ComputeCode(secret!, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var step2 = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInCodePath,
            new { code, turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminPendingAuthCookie.Name}={pending}");
        Assert.Equal(HttpStatusCode.TooManyRequests, step2.StatusCode);
    }

    [Fact]
    public async Task SecR1_Fallback_ReservesIpBeforeEvaluate()
    {
        await ClearAsync();
        var client = CreateClient();
        var directory = _factory.Services.GetRequiredService<InMemoryAdminCredentialDirectory>();
        var before = directory.PasswordEvaluations;
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => SendWrongAsync(client)));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
        Assert.Equal(directory.PasswordEvaluations - before, await CountAsync(AdminAuthScopes.Account, OwnerEmail));
        Assert.True(directory.PasswordEvaluations - before <= 5);
    }

    [Fact]
    public async Task SecR1_ResetIp_ParallelBurst_StopsAtTwenty()
    {
        await ClearAsync();
        var client = CreateClient();
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 25).Select(i => SendAuthAsync(
                client,
                HttpMethod.Post,
                AdminAuthEndpoints.ResetPath,
                new { email = $"reset{i}@example.com", turnstileToken = FakeTurnstileVerifier.ValidToken })));
        Assert.True(responses.Count(r => r.StatusCode == HttpStatusCode.OK) <= 20);
        Assert.True(responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests) >= 5);
        Assert.True(await CountAsync(AdminAuthScopes.ResetIp, "127.0.0.1") <= 20);
    }

    [Fact]
    public async Task SecR6_SignIn_UnknownEmail_WritesAccountCounter()
    {
        await ClearAsync();
        var client = CreateClient();
        using var unknown = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = "nobody@example.com", password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(1, await CountAsync(AdminAuthScopes.Account, "nobody@example.com"));
        Assert.Equal(1, await CountAsync(AdminAuthScopes.LoginIp, "127.0.0.1"));
    }

    [Fact]
    public async Task SecR6_Reset_OwnerAndUnknown_SameRequestPathWork()
    {
        await ClearAsync();
        var client = CreateClient();
        using var owner = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.ResetPath,
            new { email = OwnerEmail, turnstileToken = FakeTurnstileVerifier.ValidToken });
        using var unknown = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.ResetPath,
            new { email = "nobody@example.com", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
        Assert.Equal(await owner.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Equal(2, db.AdminAuthTokens.Count(t => t.Kind == AdminAuthToken.KindReset));
        Assert.Equal(1, db.AdminAuthTokens.Count(t => t.Kind == AdminAuthToken.KindReset && t.ConsumedAt == null));
    }

    [Fact]
    public async Task SecR7_Step1AndStep2_ShareAccountCounter()
    {
        await ClearAsync();
        var client = CreateClient();
        for (var i = 0; i < 3; i++)
        {
            using var fail = await SendWrongAsync(client);
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        using var step1 = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        step1.EnsureSuccessStatusCode();
        var pending = ReadCookie(step1, AdminSessionExemptions.PendingCookieName);
        for (var i = 0; i < 2; i++)
        {
            using var totp = await SendAuthAsync(
                client,
                HttpMethod.Post,
                AdminAuthEndpoints.SignInCodePath,
                new { totpCode = "000000", turnstileToken = FakeTurnstileVerifier.ValidToken },
                extraCookie: $"{AdminSessionExemptions.PendingCookieName}={pending}");
            Assert.Equal(HttpStatusCode.Unauthorized, totp.StatusCode);
        }

        Assert.NotNull(await ActiveAsync(AdminAuthScopes.Account, OwnerEmail));
        using var locked = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public async Task SecR11_ParallelWrongAfterFour_RevokesSessionPendingAndWritesOneLockStart(int parallel)
    {
        await ClearAsync();
        await SeedEnrolledAccountAsync();
        Guid sessionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var session = AdminSession.Create(OwnerEmail, "r11-session-hmac");
            session.MarkTotpVerified();
            db.AdminSessions.Add(session);
            db.AdminPendingAuths.Add(AdminPendingAuth.Create(
                OwnerEmail,
                AdminAuthService.HashToken(AdminAuthService.CreateOpaqueToken())));
            await db.SaveChangesAsync();
            sessionId = session.Id;
        }

        var client = CreateClient();
        for (var i = 0; i < 4; i++)
        {
            using var fail = await SendAuthAsync(
                client,
                HttpMethod.Post,
                AdminAuthEndpoints.SignInPath,
                new { email = OwnerEmail, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        var responses = await Task.WhenAll(
            Enumerable.Range(0, parallel).Select(_ => SendAuthAsync(
                client,
                HttpMethod.Post,
                AdminAuthEndpoints.SignInPath,
                new { email = OwnerEmail, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken })));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
        Assert.NotNull(await ActiveAsync(AdminAuthScopes.Account, OwnerEmail));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Equal(0, db.AdminSessions.Count(s => s.Email == OwnerEmail));
            Assert.Equal(0, db.AdminPendingAuths.Count(p => p.Email == OwnerEmail && p.ConsumedAt == null));
            Assert.Equal(1, db.AdminAuditLog.Count(e => e.Action == AdminAuthAction.LockStart && e.ActorEmail == OwnerEmail));
            Assert.Equal(0, db.AdminSessions.Count(s => s.Id == sessionId));
        }
    }

    [Fact]
    public void SecR10b_IsShortPassword_CountsTextElementsAfterStrippingFormat()
    {
        Assert.True(AdminAuthService.IsShortPassword(string.Concat(Enumerable.Repeat("\U0001F600", 8))));
        Assert.True(AdminAuthService.IsShortPassword("e" + new string('\u0301', 14)));
        Assert.True(AdminAuthService.IsShortPassword(new string('\u200B', 15)));
        Assert.True(AdminAuthService.IsShortPassword(string.Concat(Enumerable.Repeat("\U0001F600", 7)) + "a"));
        Assert.False(AdminAuthService.IsShortPassword(new string('a', 15)));
    }

    [Fact]
    public async Task SecR10b_Reset_RejectsEmojiCombiningAndZeroWidthPasswords()
    {
        await ClearAsync();
        var client = CreateClient();
        foreach (var password in new[]
                 {
                     string.Concat(Enumerable.Repeat("\U0001F600", 8)),
                     "e" + new string('\u0301', 14),
                     new string('\u200B', 15)
                 })
        {
            var raw = AdminAuthService.CreateOpaqueToken();
            using (var scope = _factory.Services.CreateScope())
            {
                var tokens = scope.ServiceProvider.GetRequiredService<IAdminAuthTokenStore>();
                await tokens.AddAsync(
                    AdminAuthToken.Create(
                        AdminAuthToken.KindReset,
                        AdminAuthService.HashToken(raw),
                        OwnerEmail.ToLowerInvariant(),
                        DateTimeOffset.UtcNow,
                        TimeSpan.FromMinutes(30)),
                    CancellationToken.None);
                await tokens.SaveChangesAsync(CancellationToken.None);
            }

            using var reset = await SendAuthAsync(
                client,
                HttpMethod.Post,
                AdminAuthEndpoints.ResetConfirmPath,
                new { token = raw, email = OwnerEmail, password, turnstileToken = FakeTurnstileVerifier.ValidToken });
            Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
            Assert.Contains(AdminAuthCopy.PasswordTooShort, await reset.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task SecR13_Step2Success_LeavesInFlightAndPostStep1Failures()
    {
        await ClearAsync();
        var (password, secret) = await SeedEnrolledAccountAsync();
        var client = CreateClient();
        using var step1 = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        step1.EnsureSuccessStatusCode();
        var pending = ReadCookie(step1, AdminPendingAuthCookie.Name)
            ?? ReadCookie(step1, AdminSessionExemptions.PendingCookieName);
        Assert.False(string.IsNullOrEmpty(pending));

        using var afterStep1 = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = "wrong-after-step1", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterStep1.StatusCode);
        Guid afterStep1Id;
        using (var seed = _factory.Services.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var afterStep1Rows = db.AdminAuthFailureEvents
                .Where(e => e.Scope == AdminAuthScopes.Account && e.SubjectKey == OwnerEmail)
                .Select(e => e.Id)
                .ToList();
            Assert.Single(afterStep1Rows);
            afterStep1Id = afterStep1Rows[0];
        }

        var code = Rfc6238Totp.ComputeCode(secret, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        var inFlight = SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = "wrong-in-flight", turnstileToken = FakeTurnstileVerifier.ValidToken });
        var step2 = SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInCodePath,
            new { code, turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminPendingAuthCookie.Name}={pending}");
        await Task.WhenAll(inFlight, step2);
        using var inFlightResponse = await inFlight;
        using var step2Response = await step2;
        Assert.Equal(HttpStatusCode.Unauthorized, inFlightResponse.StatusCode);
        step2Response.EnsureSuccessStatusCode();

        Assert.Equal(2, await CountAsync(AdminAuthScopes.Account, OwnerEmail));
        using var after = _factory.Services.CreateScope();
        var remaining = after.ServiceProvider.GetRequiredService<DealowareDbContext>()
            .AdminAuthFailureEvents
            .Where(e => e.Scope == AdminAuthScopes.Account && e.SubjectKey == OwnerEmail)
            .Select(e => e.Id)
            .ToList();
        Assert.Contains(afterStep1Id, remaining);
        Assert.Equal(2, remaining.Count);
    }

    [Fact]
    public async Task SecN8_NonOwnerAccountRows_StoreHmacNotRawEmail()
    {
        await ClearAsync();
        var client = CreateClient();
        const string unknown = "nobody@example.com";
        using var response = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = unknown, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, await CountAsync(AdminAuthScopes.Account, unknown));
        using var reset = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.ResetPath,
            new { email = unknown, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IIpHasher>();
        var expected = AdminAuthService.AccountSubjectKey(
            AdminAuthService.NormalizeEmail(unknown),
            isOwner: false,
            hasher);
        Assert.DoesNotContain(unknown, db.AdminAuthFailureEvents.Select(e => e.SubjectKey));
        Assert.DoesNotContain(unknown, db.AdminAuthLockouts.Select(l => l.SubjectKey));
        Assert.Contains(expected, db.AdminAuthFailureEvents.Select(e => e.SubjectKey));
        Assert.Contains(expected, db.AdminAuthLockouts.Select(l => l.SubjectKey));
        Assert.DoesNotContain(unknown, db.AdminAuthTokens.Select(t => t.Email));
        Assert.Contains(expected, db.AdminAuthTokens.Select(t => t.Email));
        Assert.DoesNotContain(unknown, db.AdminAuditLog.Select(e => e.ActorEmail));
        Assert.Contains(expected, db.AdminAuditLog.Select(e => e.ActorEmail));
    }

    [Fact]
    public async Task SecN8_FallbackWrongPassword_AuditActorIsHmacNotRaw()
    {
        await ClearAsync();
        const string typed = "fallback-wrong@example.com";
        var client = CreateClient();
        using var response = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = typed, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertAuditActorIsHmac(typed);
    }

    [Fact]
    public async Task SecN8_RateLimitedSignIn_AuditActorIsHmacNotRaw()
    {
        await ClearAsync();
        const string typed = "rate-signin@example.com";
        var client = CreateClient();
        await FillLoginIpAsync(client);
        using var limited = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = typed, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        AssertAuditActorIsHmac(typed, AdminAuthReason.RateLimited);
    }

    [Fact]
    public async Task SecN8_RateLimitedReset_AuditActorIsHmacNotRaw()
    {
        await ClearAsync();
        const string typed = "rate-reset@example.com";
        var client = CreateClient();
        await FillResetIpAsync(client);
        using var limited = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.ResetPath,
            new { email = typed, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        AssertAuditActorIsHmac(typed, AdminAuthReason.RateLimited);
    }

    [Fact]
    public async Task SecN8_RejectedLink_AuditActorIsHmacNotRaw()
    {
        await ClearAsync();
        const string typed = "rejected-link@example.com";
        var client = CreateClient();
        using var rejected = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.ResetConfirmPath,
            new
            {
                token = AdminAuthService.CreateOpaqueToken(),
                email = typed,
                password = "Long-enough-pass-1",
                turnstileToken = FakeTurnstileVerifier.ValidToken
            });
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        AssertAuditActorIsHmac(typed);
    }

    [Fact]
    public async Task SecR10_ResetAndBootstrap_RejectPasswordShorterThan15()
    {
        await ClearAsync();
        var client = CreateClient();
        var raw = AdminAuthService.CreateOpaqueToken();
        using (var scope = _factory.Services.CreateScope())
        {
            var tokens = scope.ServiceProvider.GetRequiredService<IAdminAuthTokenStore>();
            var now = DateTimeOffset.UtcNow;
            await tokens.AddAsync(
                AdminAuthToken.Create(
                    AdminAuthToken.KindReset,
                    AdminAuthService.HashToken(raw),
                    OwnerEmail.ToLowerInvariant(),
                    now,
                    TimeSpan.FromMinutes(30)),
                CancellationToken.None);
            await tokens.SaveChangesAsync(CancellationToken.None);
        }

        using var reset = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.ResetConfirmPath,
            new { token = raw, email = OwnerEmail, password = "short", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
        Assert.Contains(AdminAuthCopy.PasswordTooShort, await reset.Content.ReadAsStringAsync());

        var bootstrapRaw = AdminAuthService.CreateOpaqueToken();
        using (var scope = _factory.Services.CreateScope())
        {
            var tokens = scope.ServiceProvider.GetRequiredService<IAdminAuthTokenStore>();
            await tokens.AddAsync(
                AdminAuthToken.Create(
                    AdminAuthToken.KindBootstrap,
                    AdminAuthService.HashToken(bootstrapRaw),
                    OwnerEmail.ToLowerInvariant(),
                    DateTimeOffset.UtcNow,
                    TimeSpan.FromHours(24)),
                CancellationToken.None);
            await tokens.SaveChangesAsync(CancellationToken.None);
        }

        using var bootstrap = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.BootstrapPath,
            new
            {
                token = bootstrapRaw,
                email = OwnerEmail,
                password = "12345678901234",
                turnstileToken = FakeTurnstileVerifier.ValidToken
            });
        Assert.Equal(HttpStatusCode.BadRequest, bootstrap.StatusCode);
        Assert.Contains(AdminAuthCopy.PasswordTooShort, await bootstrap.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SecSc9_ResetMail_LeavesRequestBeforeSend()
    {
        await ClearAsync();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                foreach (var descriptor in services
                             .Where(d => d.ImplementationType == typeof(AdminMailQueueHostedService))
                             .ToList())
                {
                    services.Remove(descriptor);
                }
            });
        });

        var mailer = factory.Services.GetRequiredService<RecordingMailSender>();
        var before = mailer.SendCount;
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var owner = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.ResetPath,
            new { email = OwnerEmail, turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
        Assert.Equal(before, mailer.SendCount);

        using var unknown = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.ResetPath,
            new { email = "nobody@example.com", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        Assert.Equal(before, mailer.SendCount);
        Assert.Equal(await owner.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());

        var queue = factory.Services.GetRequiredService<AdminMailQueue>();
        Assert.True(queue.Reader.TryRead(out var ownerMail));
        Assert.NotNull(ownerMail);
        Assert.True(queue.Reader.TryRead(out var unknownMail));
        Assert.Null(unknownMail);
    }

    private async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        await db.AdminAuthFailureEvents.ExecuteDeleteAsync();
        await db.AdminAuthLockouts.ExecuteDeleteAsync();
        await db.AdminAuthTokens.ExecuteDeleteAsync();
        await db.AdminSessions.ExecuteDeleteAsync();
        await db.AdminPendingAuths.ExecuteDeleteAsync();
        await db.AdminRecoveryCodes.ExecuteDeleteAsync();
        await db.AdminCoreOwnerAccounts.ExecuteDeleteAsync();
        await db.AdminAuditLog.ExecuteDeleteAsync();
        _factory.Services.GetRequiredService<TestAdminClock>().UtcNow = DateTimeOffset.UtcNow;
    }

    private async Task<bool> ConsumeOnNewScopeAsync(string hash, DateTimeOffset now)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAdminAuthTokenStore>()
            .TryConsumeAsync(hash, now, CancellationToken.None);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

    private Task<HttpResponseMessage> SendWrongAsync(HttpClient client) =>
        SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });

    private async Task FillLoginIpAsync(HttpClient client)
    {
        for (var i = 0; i < AdminAuthService.IpFailureLimit; i++)
        {
            using var fail = await SendAuthAsync(
                client,
                HttpMethod.Post,
                AdminAuthEndpoints.SignInPath,
                new
                {
                    email = $"fill{i}@example.com",
                    password = "wrong",
                    turnstileToken = FakeTurnstileVerifier.ValidToken
                });
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }
    }

    private async Task FillResetIpAsync(HttpClient client)
    {
        for (var i = 0; i < AdminAuthService.IpFailureLimit; i++)
        {
            using var reset = await SendAuthAsync(
                client,
                HttpMethod.Post,
                AdminAuthEndpoints.ResetPath,
                new
                {
                    email = $"reset-fill{i}@example.com",
                    turnstileToken = FakeTurnstileVerifier.ValidToken
                });
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        }
    }

    private void AssertAuditActorIsHmac(string typedEmail, string? reason = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IIpHasher>();
        var expected = AdminAuthService.AccountSubjectKey(
            AdminAuthService.NormalizeEmail(typedEmail),
            isOwner: false,
            hasher);
        var actors = db.AdminAuditLog
            .Where(e => reason == null || e.ReasonClass == reason)
            .Select(e => e.ActorEmail)
            .ToList();
        Assert.DoesNotContain(actors, actor => actor.Contains(typedEmail, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(expected, actors);
    }

    private async Task<(string Password, byte[] Secret)> SeedEnrolledAccountAsync()
    {
        var password = $"Pw-{Guid.NewGuid():N}-xY9!";
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<TotpSecretProtector>();
        var hasher = scope.ServiceProvider.GetRequiredService<AdminRecoveryCodeHasher>();
        var existing = db.AdminCoreOwnerAccounts.SingleOrDefault(a => a.Email == OwnerEmail);
        if (existing is not null)
        {
            db.AdminRecoveryCodes.RemoveRange(db.AdminRecoveryCodes.Where(c => c.AccountId == existing.Id));
            db.AdminPendingAuths.RemoveRange(db.AdminPendingAuths.Where(p => p.Email == OwnerEmail));
            db.AdminSessions.RemoveRange(db.AdminSessions.Where(s => s.Email == OwnerEmail));
            db.AdminCoreOwnerAccounts.Remove(existing);
            await db.SaveChangesAsync();
        }

        var account = AdminCoreOwnerAccount.Create(OwnerEmail);
        account.SetPasswordHash(AdminPasswordHasher.Hash(password));
        var secret = Rfc6238Totp.GenerateSecret();
        foreach (var code in AdminRecoveryCodes.Generate())
        {
            db.AdminRecoveryCodes.Add(AdminRecoveryCode.Create(account.Id, hasher.Hash(code)));
        }

        account.MarkRecoveryCodesIssued();
        account.CompleteEnrollment(protector.Encrypt(secret));
        db.AdminCoreOwnerAccounts.Add(account);
        await db.SaveChangesAsync();
        return (password, secret);
    }

    private async Task<Guid> SignInFullFallbackAsync(HttpClient client)
    {
        using var step1 = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password = _password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        step1.EnsureSuccessStatusCode();
        var pending = ReadCookie(step1, AdminSessionExemptions.PendingCookieName);
        using var step2 = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInCodePath,
            new { totpCode = "246813", turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminSessionExemptions.PendingCookieName}={pending}");
        step2.EnsureSuccessStatusCode();
        return Guid.Parse(ReadCookie(step2, AdminSessionCookie.Name)!);
    }

    private async Task<Guid> SignInFullAccountAsync(HttpClient client, string password, byte[] secret)
    {
        using var step1 = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        step1.EnsureSuccessStatusCode();
        var pending = ReadCookie(step1, AdminPendingAuthCookie.Name)
            ?? ReadCookie(step1, AdminSessionExemptions.PendingCookieName);
        var code = Rfc6238Totp.ComputeCode(secret, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var step2 = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInCodePath,
            new { code, turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminPendingAuthCookie.Name}={pending}");
        step2.EnsureSuccessStatusCode();
        return Guid.Parse(ReadCookie(step2, AdminSessionCookie.Name)!);
    }

    private static async Task<HttpResponseMessage> AdminGetAsync(HttpClient client, Guid sessionId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/api/me");
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={sessionId:D}");
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendAuthAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object body,
        string? extraCookie = null)
    {
        var af = new HttpRequestMessage(HttpMethod.Get, "/admin/sign-in");
        af.Headers.Host = AdminHost;
        using var issued = await client.SendAsync(af);
        issued.EnsureSuccessStatusCode();
        var token = issued.Headers.TryGetValues(AdminAntiForgery.HeaderName, out var values)
            ? values.First()
            : string.Empty;
        var cookie = issued.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.First(c => c.StartsWith(AdminAntiForgery.CookieName + "=", StringComparison.Ordinal))
            : string.Empty;
        var cookieValue = cookie[(AdminAntiForgery.CookieName.Length + 1)..];
        var end = cookieValue.IndexOf(';');
        if (end >= 0)
        {
            cookieValue = cookieValue[..end];
        }

        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "127.0.0.1");
        request.Headers.TryAddWithoutValidation(AdminAntiForgery.HeaderName, token);
        var cookieHeader = $"{AdminAntiForgery.CookieName}={cookieValue}";
        if (!string.IsNullOrEmpty(extraCookie))
        {
            cookieHeader += $"; {extraCookie}";
        }

        request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private async Task<int> CountAsync(string scope, string key)
    {
        using var scopeSvc = _factory.Services.CreateScope();
        return await scopeSvc.ServiceProvider.GetRequiredService<AdminAuthService>()
            .CountFailuresAsync(scope, key, CancellationToken.None);
    }

    private async Task<AdminAuthLockout?> ActiveAsync(string scope, string key)
    {
        using var scopeSvc = _factory.Services.CreateScope();
        return await scopeSvc.ServiceProvider.GetRequiredService<AdminAuthService>()
            .GetActiveLockoutAsync(scope, key, CancellationToken.None);
    }

    private static string? ReadCookie(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        foreach (var cookie in cookies)
        {
            if (cookie.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
            {
                var value = cookie[(name.Length + 1)..];
                var cut = value.IndexOf(';');
                return cut < 0 ? value : value[..cut];
            }
        }

        return null;
    }
}
