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
/// <c>FullyQualifiedName~SecR|FullyQualifiedName~SecSc|FullyQualifiedName~SecN8</c>.
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
                AdminAuthToken.KindPending,
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

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IIpHasher>();
        var auditHasher = scope.ServiceProvider.GetRequiredService<IAuditActorHasher>();
        var expectedSubject = AdminAuthService.AccountSubjectKey(
            AdminAuthService.NormalizeEmail(unknown),
            isOwner: false,
            hasher);
        var expectedActor = auditHasher.HashEmail(AdminAuthService.NormalizeEmail(unknown));
        Assert.NotEqual(expectedSubject, expectedActor);
        Assert.DoesNotContain(unknown, db.AdminAuthFailureEvents.Select(e => e.SubjectKey));
        Assert.DoesNotContain(unknown, db.AdminAuthLockouts.Select(l => l.SubjectKey));
        Assert.Contains(expectedSubject, db.AdminAuthFailureEvents.Select(e => e.SubjectKey));
        Assert.Contains(expectedSubject, db.AdminAuthLockouts.Select(l => l.SubjectKey));
        Assert.DoesNotContain(unknown, db.AdminAuditLog.Select(e => e.ActorEmail));
        Assert.Contains(expectedActor, db.AdminAuditLog.Select(e => e.ActorEmail));
        Assert.DoesNotContain(expectedSubject, db.AdminAuditLog.Select(e => e.ActorEmail));
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
    public async Task SecN8_NonOwnerLockStart_AuditActorIsHmacNotRaw()
    {
        await ClearAsync();
        await SeedEnrolledAccountAsync();
        const string typed = "made-up-lock@example.com";
        var client = CreateClient();
        for (var i = 0; i < 7; i++)
        {
            using var fail = await SendAuthAsync(
                client,
                HttpMethod.Post,
                AdminAuthEndpoints.SignInPath,
                new { email = typed, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var auditHasher = scope.ServiceProvider.GetRequiredService<IAuditActorHasher>();
            var expected = auditHasher.HashEmail(AdminAuthService.NormalizeEmail(typed));
            var actors = db.AdminAuditLog
                .Where(e => e.Action == AdminAuthAction.LockStart)
                .Select(e => e.ActorEmail)
                .ToList();
            Assert.DoesNotContain(actors, actor => actor.Contains(typed, StringComparison.OrdinalIgnoreCase));
            Assert.Contains(expected, actors);
            Assert.Single(actors);
        }

        for (var i = 0; i < 5; i++)
        {
            using var fail = await SendWrongAsync(client);
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Equal(
                1,
                db.AdminAuditLog.Count(e => e.Action == AdminAuthAction.LockStart && e.ActorEmail == OwnerEmail));
            Assert.DoesNotContain(
                typed,
                db.AdminAuditLog.Select(e => e.ActorEmail),
                StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task SecN8_AuditActor_NeverEqualsFailureOrLockSubjectKey()
    {
        await ClearAsync();
        const string typed = "distinct-audit@example.com";
        var client = CreateClient();
        using var response = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = typed, password = "wrong", turnstileToken = FakeTurnstileVerifier.ValidToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var subjects = db.AdminAuthFailureEvents.Select(e => e.SubjectKey)
            .Concat(db.AdminAuthLockouts.Select(l => l.SubjectKey))
            .AsEnumerable()
            .ToHashSet(StringComparer.Ordinal);
        var actors = db.AdminAuditLog
            .Select(e => e.ActorEmail)
            .AsEnumerable()
            .Where(actor =>
                !string.Equals(actor, OwnerEmail, StringComparison.Ordinal)
                && !string.Equals(actor, "anonymous", StringComparison.Ordinal))
            .ToList();
        Assert.NotEmpty(actors);
        Assert.DoesNotContain(typed, actors, StringComparer.OrdinalIgnoreCase);
        Assert.All(actors, actor => Assert.DoesNotContain(actor, subjects));
    }

    [Fact]
    public async Task SecR16_ResetSideFactorLock_BlocksCorrectTotpAndRecovery()
    {
        await ClearAsync();
        var (password, secret, recovery) = await SeedEnrolledAccountWithRecoveryAsync();
        await TripFactorLockAsync();

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

        var code = Rfc6238Totp.ComputeCode(secret, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var totp = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInCodePath,
            new { code, turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminPendingAuthCookie.Name}={pending}");
        Assert.Equal(HttpStatusCode.Unauthorized, totp.StatusCode);
        Assert.Contains(AdminAuthCopy.SignInFailure, await totp.Content.ReadAsStringAsync());
        Assert.True(await IsFactorLockedAsync());
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            Assert.Contains(db.AdminAuditLog, e => e.ReasonClass == AdminAuthReason.Locked);
        }

        using var recoveryResponse = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInRecoveryPath,
            new { recoveryCode = recovery, turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminPendingAuthCookie.Name}={pending}");
        Assert.Equal(HttpStatusCode.Unauthorized, recoveryResponse.StatusCode);
        Assert.Contains(AdminAuthCopy.SignInFailure, await recoveryResponse.Content.ReadAsStringAsync());
        Assert.True(await IsFactorLockedAsync());
    }

    [Fact]
    public async Task SecR16_WrongSignInCodes_TripAccountLock_DoNotWriteFactorLock()
    {
        await ClearAsync();
        var (password, _) = await SeedEnrolledAccountAsync();
        var client = CreateClient();
        using var step1 = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInPath,
            new { email = OwnerEmail, password, turnstileToken = FakeTurnstileVerifier.ValidToken });
        step1.EnsureSuccessStatusCode();
        var pending = ReadCookie(step1, AdminPendingAuthCookie.Name)
            ?? ReadCookie(step1, AdminSessionExemptions.PendingCookieName);

        for (var i = 0; i < AdminAuthService.AccountFailureLimit; i++)
        {
            using var wrong = await SendAuthAsync(
                client,
                HttpMethod.Post,
                AdminAuthEndpoints.SignInCodePath,
                new { code = "000000", turnstileToken = FakeTurnstileVerifier.ValidToken },
                extraCookie: $"{AdminPendingAuthCookie.Name}={pending}");
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        Assert.NotNull(await ActiveAsync(AdminAuthScopes.Account, OwnerEmail));
        Assert.False(await IsFactorLockedAsync());
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == OwnerEmail);
        Assert.Null(account.FactorLockedUntil);
        Assert.Equal(0, account.FailedFactorAttempts);
    }

    [Fact]
    public async Task SecR2_ThrottledIp_PendingCookieVariants_Same429()
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

        await FillLoginIpAsync(client);
        var code = Rfc6238Totp.ComputeCode(secret, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var valid = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInCodePath,
            new { code, turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminPendingAuthCookie.Name}={pending}");
        using var garbage = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInCodePath,
            new { code, turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminPendingAuthCookie.Name}=not-a-pending-token");
        using var missing = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInCodePath,
            new { code, turnstileToken = FakeTurnstileVerifier.ValidToken });

        Assert.Equal(HttpStatusCode.TooManyRequests, valid.StatusCode);
        Assert.Equal(valid.StatusCode, garbage.StatusCode);
        Assert.Equal(valid.StatusCode, missing.StatusCode);
        var body = await valid.Content.ReadAsStringAsync();
        Assert.Equal(body, await garbage.Content.ReadAsStringAsync());
        Assert.Equal(body, await missing.Content.ReadAsStringAsync());
        Assert.Contains(AdminAuthCopy.SignInFailure, body);
        Assert.False(valid.Headers.Contains("Retry-After"));
        Assert.False(garbage.Headers.Contains("Retry-After"));
        Assert.False(missing.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task SecR2_ThrottledIp_CountAtLimitWithoutGate_PendingCookieVariants_Same429()
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

        await FillLoginIpAbandonedReservationsAsync();
        Assert.Null(await ActiveAsync(AdminAuthScopes.LoginIp, "127.0.0.1"));
        Assert.Equal(AdminAuthService.IpFailureLimit, await CountAsync(AdminAuthScopes.LoginIp, "127.0.0.1"));

        var code = Rfc6238Totp.ComputeCode(secret, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var valid = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInCodePath,
            new { code, turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminPendingAuthCookie.Name}={pending}");
        using var garbage = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInCodePath,
            new { code, turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminPendingAuthCookie.Name}=not-a-pending-token");
        using var missing = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInCodePath,
            new { code, turnstileToken = FakeTurnstileVerifier.ValidToken });

        Assert.Equal(HttpStatusCode.TooManyRequests, valid.StatusCode);
        Assert.Equal(valid.StatusCode, garbage.StatusCode);
        Assert.Equal(valid.StatusCode, missing.StatusCode);
        var body = await valid.Content.ReadAsStringAsync();
        Assert.Equal(body, await garbage.Content.ReadAsStringAsync());
        Assert.Equal(body, await missing.Content.ReadAsStringAsync());
        Assert.Contains(AdminAuthCopy.SignInFailure, body);
        Assert.False(valid.Headers.Contains("Retry-After"));
        Assert.False(garbage.Headers.Contains("Retry-After"));
        Assert.False(missing.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task SecR_PendingCapRefusal_DoesNotGrowLoginIpOrAccountCounters()
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
        using (var scope = _factory.Services.CreateScope())
        {
            var tokens = scope.ServiceProvider.GetRequiredService<IAdminAuthTokenStore>();
            var row = await tokens.GetByHashAsync(hash, CancellationToken.None);
            Assert.NotNull(row);
            for (var i = 0; i < AdminAuthService.PendingAttemptLimit; i++)
            {
                Assert.True(await tokens.TryIncrementPendingAttemptAsync(
                    row.Id, now, AdminAuthService.PendingAttemptLimit, CancellationToken.None));
            }

            Assert.False(await tokens.TryIncrementPendingAttemptAsync(
                row.Id, now, AdminAuthService.PendingAttemptLimit, CancellationToken.None));
        }

        var ipBefore = await CountAsync(AdminAuthScopes.LoginIp, "127.0.0.1");
        var accountBefore = await CountAsync(AdminAuthScopes.Account, OwnerEmail);

        using var refused = await SendAuthAsync(
            client,
            HttpMethod.Post,
            AdminAuthEndpoints.SignInCodePath,
            new { totpCode = "246813", turnstileToken = FakeTurnstileVerifier.ValidToken },
            extraCookie: $"{AdminSessionExemptions.PendingCookieName}={pending}");
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        Assert.Contains(AdminAuthCopy.SignInFailure, await refused.Content.ReadAsStringAsync());
        Assert.Equal(ipBefore, await CountAsync(AdminAuthScopes.LoginIp, "127.0.0.1"));
        Assert.Equal(accountBefore, await CountAsync(AdminAuthScopes.Account, OwnerEmail));
        Assert.Null(await ActiveAsync(AdminAuthScopes.Account, OwnerEmail));
        Assert.Null(await ActiveAsync(AdminAuthScopes.LoginIp, "127.0.0.1"));
    }

    [Fact]
    public async Task SecR12_AccountLockedRefusal_DoesNotGrowLoginIpCounter()
    {
        await ClearAsync();
        await SeedEnrolledAccountAsync();
        var client = CreateClient();
        for (var i = 0; i < AdminAuthService.AccountFailureLimit; i++)
        {
            using var fail = await SendWrongAsync(client);
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        Assert.NotNull(await ActiveAsync(AdminAuthScopes.Account, OwnerEmail));
        var ipBefore = await CountAsync(AdminAuthScopes.LoginIp, "127.0.0.1");
        Assert.Equal(AdminAuthService.AccountFailureLimit, ipBefore);

        using var locked = await SendWrongAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Contains(AdminAuthCopy.SignInFailure, await locked.Content.ReadAsStringAsync());
        Assert.Equal(ipBefore, await CountAsync(AdminAuthScopes.LoginIp, "127.0.0.1"));
        Assert.Equal(AdminAuthService.AccountFailureLimit, await CountAsync(AdminAuthScopes.Account, OwnerEmail));
        Assert.NotNull(await ActiveAsync(AdminAuthScopes.Account, OwnerEmail));
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

    private async Task FillLoginIpAbandonedReservationsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var lockouts = scope.ServiceProvider.GetRequiredService<IAdminLockoutStore>();
        var clock = scope.ServiceProvider.GetRequiredService<IAdminClock>();
        var now = clock.UtcNow;
        for (var i = 0; i < AdminAuthService.IpFailureLimit; i++)
        {
            var reserved = await AdminLockoutFlow.ReserveAsync(
                lockouts,
                AdminAuthScopes.LoginIp,
                "127.0.0.1",
                now,
                AdminAuthService.IpFailureLimit,
                CancellationToken.None);
            Assert.True(reserved.Incremented);
        }
    }

    private void AssertAuditActorIsHmac(string typedEmail, string? reason = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IAuditActorHasher>();
        var expected = hasher.HashEmail(AdminAuthService.NormalizeEmail(typedEmail));
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

    private async Task<(string Password, byte[] Secret, string Recovery)> SeedEnrolledAccountWithRecoveryAsync()
    {
        var password = $"Pw-{Guid.NewGuid():N}-xY9!";
        var recovery = AdminRecoveryCodes.Generate()[0];
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
        db.AdminRecoveryCodes.Add(AdminRecoveryCode.Create(account.Id, hasher.Hash(recovery)));
        account.MarkRecoveryCodesIssued();
        account.CompleteEnrollment(protector.Encrypt(secret));
        db.AdminCoreOwnerAccounts.Add(account);
        await db.SaveChangesAsync();
        return (password, secret, recovery);
    }

    private async Task TripFactorLockAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IAdminCoreOwnerAccountRepository>();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IAdminClock>();
        var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == OwnerEmail);
        var now = clock.UtcNow;
        for (var i = 0; i < AdminCoreOwnerAccount.MaxFailedFactorAttempts; i++)
        {
            Assert.True(await accounts.TryRecordFailedFactorAttemptAsync(account.Id, now));
        }

        Assert.True(await accounts.IsFactorLockedAsync(account.Id, now));
    }

    private async Task<bool> IsFactorLockedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IAdminCoreOwnerAccountRepository>();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IAdminClock>();
        var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == OwnerEmail);
        return await accounts.IsFactorLockedAsync(account.Id, clock.UtcNow);
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
