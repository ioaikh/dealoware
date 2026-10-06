using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-004 / 012 / 020 / 021 / 022 / 023 / 025 / 027 / 028 plus r3 §10 C1–C8.
/// </summary>
[Collection("WebAppTests")]
public class AdminTotpTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = "io@aiknowhow.com";
    private const string SignInPath = AdminSignedOutExemptions.ApiSignIn;
    private const string CodePath = AdminSignedOutExemptions.ApiSignInCode;
    private const string RecoveryPath = AdminSignedOutExemptions.ApiSignInRecovery;
    private const string SetupPath = AdminSignedOutExemptions.SetupAuthenticator;
    private const string RecoveryCodesPath = AdminSignedOutExemptions.SetupRecoveryCodes;
    private const string GenericSignInCopy =
        "We couldn't sign you in. Check your details and try again later. You can also reset your password.";

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminTotpTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(IsolatedWebApplicationFactory? factory = null) =>
        (factory ?? _factory).CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    private async Task<(string Password, byte[]? Secret, IReadOnlyList<string>? Codes)> SeedAccountAsync(
        bool enrolled,
        IsolatedWebApplicationFactory? factory = null)
    {
        factory ??= _factory;
        var password = $"Pw-{Guid.NewGuid():N}-xY9!";
        byte[]? secret = null;
        IReadOnlyList<string>? codes = null;

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<TotpSecretProtector>();
        var hasher = scope.ServiceProvider.GetRequiredService<AdminRecoveryCodeHasher>();

        var existing = db.AdminCoreOwnerAccounts.SingleOrDefault(a => a.Email == CoreOwnerEmail);
        if (existing is not null)
        {
            db.AdminRecoveryCodes.RemoveRange(db.AdminRecoveryCodes.Where(c => c.AccountId == existing.Id));
            db.AdminPendingAuths.RemoveRange(db.AdminPendingAuths.Where(p => p.Email == CoreOwnerEmail));
            db.AdminSessions.RemoveRange(db.AdminSessions.Where(s => s.Email == CoreOwnerEmail));
            db.AdminCoreOwnerAccounts.Remove(existing);
            await db.SaveChangesAsync();
        }

        var account = AdminCoreOwnerAccount.Create(CoreOwnerEmail);
        account.SetPasswordHash(AdminPasswordHasher.Hash(password));
        if (enrolled)
        {
            secret = Rfc6238Totp.GenerateSecret();
            codes = AdminRecoveryCodes.Generate();
            foreach (var code in codes)
            {
                db.AdminRecoveryCodes.Add(AdminRecoveryCode.Create(account.Id, hasher.Hash(code)));
            }

            account.MarkRecoveryCodesIssued();
            account.CompleteEnrollment(protector.Encrypt(secret));
        }

        db.AdminCoreOwnerAccounts.Add(account);
        await db.SaveChangesAsync();
        return (password, secret, codes);
    }

    private static HttpRequestMessage AdminGet(string path, Guid? sessionId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        if (sessionId is { } id)
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={id:D}");
        }

        return request;
    }

    private async Task<string> IssueCsrfAsync(HttpClient client)
    {
        using var request = AdminGet(AdminSignedOutExemptions.SignIn);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = payload.GetProperty("csrfToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var af = cookies.Single(c => c.StartsWith(AdminAntiForgeryCookie.Name + "=", StringComparison.Ordinal));
        Assert.Contains("path=/admin", af, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", af, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", af, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", af, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", af, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("__Host-", af, StringComparison.Ordinal);
        return token!;
    }

    private async Task<HttpRequestMessage> JsonAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object? body = null,
        bool includeCsrf = true,
        string? csrf = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        if (includeCsrf && HttpMethod.Post.Equals(method))
        {
            request.Headers.TryAddWithoutValidation(
                AdminAntiForgeryCookie.HeaderName,
                csrf ?? await IssueCsrfAsync(client));
        }

        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    private static void AssertNoStore(HttpResponseMessage response)
    {
        var cache = response.Headers.CacheControl?.ToString();
        if (string.IsNullOrEmpty(cache)
            && response.Headers.TryGetValues("Cache-Control", out var headerValues))
        {
            cache = string.Join(",", headerValues);
        }

        if (string.IsNullOrEmpty(cache)
            && response.Content.Headers.TryGetValues("Cache-Control", out var contentValues))
        {
            cache = string.Join(",", contentValues);
        }

        Assert.Contains("no-store", cache ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssertGenericSignIn(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.RetryAfter);
        Assert.False(response.Headers.Contains("Retry-After"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(GenericSignInCopy, body);
        Assert.DoesNotContain("TOTP", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recovery", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("locked", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("2FA", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Retry-After", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("attempt", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("remaining", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TdAdm012_TotpEnrollmentRequiredBeforeFirstSession()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: false);
        var client = CreateClient();

        using (var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password })))
        {
            Assert.Equal(HttpStatusCode.OK, step1.StatusCode);
            var payload = await step1.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("enroll", payload.GetProperty("next").GetString());
        }

        using (var me = await client.SendAsync(AdminGet("/admin/api/me")))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
            Assert.Contains("\"error\":\"Unauthorized\"", await me.Content.ReadAsStringAsync());
        }

        using var setup = await client.SendAsync(AdminGet(SetupPath));
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        AssertNoStore(setup);
        var enrolled = await setup.Content.ReadFromJsonAsync<JsonElement>();
        var secret = enrolled.GetProperty("secret").GetString();
        Assert.False(string.IsNullOrWhiteSpace(secret));

        var totp = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(secret!), Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var confirm = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SetupPath, new { code = totp }));
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        using var codes = await client.SendAsync(AdminGet(RecoveryCodesPath));
        Assert.Equal(HttpStatusCode.OK, codes.StatusCode);
        AssertNoStore(codes);
        var shown = await codes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(AdminRecoveryCodes.Count, shown.GetProperty("recoveryCodes").GetArrayLength());

        using (var meAfterEnroll = await client.SendAsync(AdminGet("/admin/api/me")))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, meAfterEnroll.StatusCode);
        }

        using var step1Again = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1Again.StatusCode);
        var next = await step1Again.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("totp", next.GetProperty("next").GetString());

        var loginStep = Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow) + 1;
        var loginCode = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(secret!), loginStep);
        using var verify = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code = loginCode }));
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        using var meOk = await client.SendAsync(AdminGet("/admin/api/me"));
        Assert.Equal(HttpStatusCode.OK, meOk.StatusCode);
    }

    [Fact]
    public async Task TdAdm020_TotpRequiredOnEveryPostEnrollmentLogin()
    {
        var (password, secret, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();

        using (var omit = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password })))
            Assert.Equal(HttpStatusCode.OK, omit.StatusCode);

        using (var me = await client.SendAsync(AdminGet("/admin/api/me")))
            Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        using (var wrongTotp = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code = "000000" })))
            await AssertGenericSignIn(wrongTotp);

        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using (var wrongRecovery = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, RecoveryPath, new { recoveryCode = "AAAA-BBBB-CCCC" })))
            await AssertGenericSignIn(wrongRecovery);

        using var step1b = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        var code = Rfc6238Totp.ComputeCode(secret!, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var ok = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code }));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        using var meOk = await client.SendAsync(AdminGet("/admin/api/me"));
        Assert.Equal(HttpStatusCode.OK, meOk.StatusCode);
    }

    [Fact]
    public async Task TdAdm020_WrongPassword_SameBodyAsWrongTotp()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();

        using var badPassword = await client.SendAsync(
            await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password = "not-the-password" }));
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        using var badTotp = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code = "111111" }));

        var a = await badPassword.Content.ReadAsStringAsync();
        var b = await badTotp.Content.ReadAsStringAsync();
        Assert.Equal(a, b);
        Assert.Contains(GenericSignInCopy, a);
    }

    [Fact]
    public async Task TdAdm021_EmailOtpFallbackPathAbsent()
    {
        var client = CreateClient();
        foreach (var path in new[]
                 {
                     "/admin/api/auth/otp",
                     "/admin/api/auth/email-otp",
                     "/admin/api/auth/otp/send",
                     "/admin/api/auth/otp/verify",
                     "/auth/otp",
                     "/otp"
                 })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Host = AdminHost;
            using var response = await client.SendAsync(request);
            Assert.True(
                response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed or HttpStatusCode.Unauthorized,
                $"{path} returned {response.StatusCode}");
            if (path.StartsWith("/admin/api/auth/", StringComparison.Ordinal))
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task TdAdm022_RecoveryCodesShownOnceHashedSingleUse()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: false);
        var client = CreateClient();

        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using var setup = await client.SendAsync(AdminGet(SetupPath));
        var first = await setup.Content.ReadFromJsonAsync<JsonElement>();
        var secret = first.GetProperty("secret").GetString()!;
        Assert.False(first.TryGetProperty("recoveryCodes", out _));

        var totp = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(secret), Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var confirm = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SetupPath, new { code = totp }));
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        using var reveal = await client.SendAsync(AdminGet(RecoveryCodesPath));
        var shown = await reveal.Content.ReadFromJsonAsync<JsonElement>();
        var codes = shown.GetProperty("recoveryCodes").EnumerateArray().Select(e => e.GetString()!).ToList();
        Assert.Equal(AdminRecoveryCodes.Count, codes.Count);

        // C3: GET is side-effect free, so a refresh in the same pending window still shows codes.
        using var revealAgain = await client.SendAsync(AdminGet(RecoveryCodesPath));
        var stillShown = await revealAgain.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(AdminRecoveryCodes.Count, stillShown.GetProperty("recoveryCodes").GetArrayLength());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        foreach (var stored in db.AdminRecoveryCodes)
        {
            foreach (var plain in codes)
            {
                Assert.DoesNotContain(plain, stored.CodeHash, StringComparison.OrdinalIgnoreCase);
            }
        }

        using var step1b = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        using var afterWindow = await client.SendAsync(AdminGet(RecoveryCodesPath));
        var hidden = await afterWindow.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(hidden.TryGetProperty("recoveryCodes", out _));

        using var useCode = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, RecoveryPath, new { recoveryCode = codes[0].ToLowerInvariant() }));
        Assert.Equal(HttpStatusCode.OK, useCode.StatusCode);

        using var step1c = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        using var reuse = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, RecoveryPath, new { recoveryCode = codes[0] }));
        await AssertGenericSignIn(reuse);
    }

    [Fact]
    public async Task TdAdm004_PendingAuthDoesNotOpenAdminRoutes()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);
        Assert.True(step1.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.DoesNotContain(cookies, c => c.StartsWith(AdminSessionCookie.Name + "=", StringComparison.Ordinal));

        using var me = await client.SendAsync(AdminGet("/admin/api/me"));
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Contains("\"error\":\"Unauthorized\"", await me.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TdAdm021_SignInLivesOnlyOnAdminApiPrefix()
    {
        var client = CreateClient();
        using var topLevel = new HttpRequestMessage(HttpMethod.Post, "/sign-in");
        topLevel.Headers.Host = AdminHost;
        using var topLevelResponse = await client.SendAsync(topLevel);
        Assert.Equal(HttpStatusCode.NotFound, topLevelResponse.StatusCode);

        using var otherHost = await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password = "x" });
        otherHost.Headers.Host = "api.core.dealoware.com";
        using var otherHostResponse = await client.SendAsync(otherHost);
        Assert.Equal(HttpStatusCode.NotFound, otherHostResponse.StatusCode);
        Assert.Empty(await otherHostResponse.Content.ReadAsStringAsync());
        Assert.False(otherHostResponse.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task TdAdm023_PendingTokenIsOpaqueSingleUseAndCookieOnly()
    {
        var (password, secret, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        var body = await step1.Content.ReadAsStringAsync();
        Assert.DoesNotContain("dw_admin_pending", body, StringComparison.OrdinalIgnoreCase);
        Assert.True(step1.Headers.TryGetValues("Set-Cookie", out var cookies));
        var pending = cookies.Single(c => c.StartsWith(AdminPendingAuthCookie.Name + "=", StringComparison.Ordinal));
        var raw = pending.Split(';', 2)[0][(AdminPendingAuthCookie.Name.Length + 1)..];
        Assert.True(raw.Length >= 32, "pending token must carry ≥128 bits");
        Assert.Contains("path=/admin", pending, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("__Host-", pending, StringComparison.Ordinal);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var stored = db.AdminPendingAuths.Single(p => p.Email == CoreOwnerEmail && p.ConsumedAt == null);
        Assert.Equal(TimeSpan.FromMinutes(AdminPendingAuth.LifetimeMinutes), stored.ExpiresAt - stored.CreatedAt);
        Assert.NotEqual(raw, stored.TokenHash, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("/admin/", stored.ReturnPath);

        var code = Rfc6238Totp.ComputeCode(secret!, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var verify = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code }));
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        using var reuse = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code }));
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task TdAdm025_FiveFailedCodesVoidPendingToken()
    {
        var (password, secret, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        for (var i = 0; i < AdminCoreOwnerAccount.MaxFailedFactorAttempts; i++)
        {
            using var wrong = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code = "000000" }));
            await AssertGenericSignIn(wrong);
        }

        var valid = Rfc6238Totp.ComputeCode(secret!, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var sixth = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code = valid }));
        Assert.Equal(HttpStatusCode.Unauthorized, sixth.StatusCode);
    }

    [Fact]
    public async Task TdAdm027_SessionIssuedOnlyAfterStep2()
    {
        var (password, secret, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);
        using (var me = await client.SendAsync(AdminGet("/admin/api/me")))
            Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        var code = Rfc6238Totp.ComputeCode(secret!, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var verify = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code }));
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        Assert.True(verify.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, c => c.StartsWith(AdminSessionCookie.Name + "=", StringComparison.Ordinal));

        using var meOk = await client.SendAsync(AdminGet("/admin/api/me"));
        Assert.Equal(HttpStatusCode.OK, meOk.StatusCode);
    }

    [Fact]
    public async Task TdAdm028_SecondFactorFailureIsAuditedNotEchoed()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        using var wrong = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code = "123456" }));
        var body = await wrong.Content.ReadAsStringAsync();
        Assert.Contains(GenericSignInCopy, body);
        Assert.DoesNotContain(AdminAuthEvents.SecondFactorFailed, body, StringComparison.Ordinal);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Contains(db.AdminAuditLog, e => e.Action == AdminAuthEvents.SecondFactorFailed && e.ReasonClass == AdminAuthEvents.ReasonBad2Fa);
    }

    [Fact]
    public async Task R3_ExtraSegmentOnOwnedPath_IsDenied()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using var extra = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath + "/extra", new { code = "000000" }));
        Assert.Equal(HttpStatusCode.Unauthorized, extra.StatusCode);
        Assert.Contains("\"error\":\"Unauthorized\"", await extra.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task R3_WrongMethodOnOwnedPath_IsDenied()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using var getPage = await client.SendAsync(AdminGet(AdminSignedOutExemptions.SignInCode));
        Assert.Equal(HttpStatusCode.OK, getPage.StatusCode);

        using var getApi = await client.SendAsync(AdminGet(CodePath));
        Assert.Equal(HttpStatusCode.Unauthorized, getApi.StatusCode);
        Assert.Contains("\"error\":\"Unauthorized\"", await getApi.Content.ReadAsStringAsync());

        using var delete = await client.SendAsync(await JsonAsync(client, new HttpMethod("DELETE"), CodePath));
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
        Assert.Contains("\"error\":\"Unauthorized\"", await delete.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task R3_MissingPendingToken_IsDenied()
    {
        var client = CreateClient();
        using var code = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code = "123456" }));
        Assert.Equal(HttpStatusCode.Unauthorized, code.StatusCode);
        Assert.Contains("\"error\":\"Unauthorized\"", await code.Content.ReadAsStringAsync());

        using var setup = await client.SendAsync(AdminGet(SetupPath));
        Assert.Equal(HttpStatusCode.Unauthorized, setup.StatusCode);
    }

    [Fact]
    public async Task R3_PendingCookie_HasAdminPathHostOnlyFlags()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        Assert.True(step1.Headers.TryGetValues("Set-Cookie", out var cookies));
        var pending = cookies.Single(c => c.StartsWith(AdminPendingAuthCookie.Name + "=", StringComparison.Ordinal));
        Assert.Contains("path=/admin", pending, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", pending, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", pending, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", pending, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", pending, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("__Host-", pending, StringComparison.Ordinal);
        Assert.Equal("dw_admin_pending", AdminPendingAuthCookie.Name);
        Assert.Equal("dw_admin_session", AdminSessionCookie.Name);
        Assert.Equal("dw_admin_af", AdminAntiForgeryCookie.Name);
    }

    [Fact]
    public async Task R3_QueryStringDoesNotCreateExemption()
    {
        var client = CreateClient();
        using var request = await JsonAsync(client, HttpMethod.Post, CodePath + "?next=/admin/", new { code = "123456" });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task R3_C1_TrailingSlashAndEncodedSeparator_AreDenied()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using var trailing = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath + "/", new { code = "000000" }));
        Assert.Equal(HttpStatusCode.Unauthorized, trailing.StatusCode);

        using var encoded = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, "/admin/api/auth/sign-in%2fcode", new { code = "000000" }));
        Assert.Equal(HttpStatusCode.Unauthorized, encoded.StatusCode);
    }

    [Fact]
    public async Task R3_C3_MissingAntiForgery_IsDeniedWithoutAuthAction()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var request = await JsonAsync(
            client,
            HttpMethod.Post,
            SignInPath,
            new { email = CoreOwnerEmail, password },
            includeCsrf: false);
        using var response = await client.SendAsync(request);
        await AssertGenericSignIn(response);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.DoesNotContain(db.AdminPendingAuths, p => p.Email == CoreOwnerEmail && p.ConsumedAt == null);
    }

    [Fact]
    public async Task R3_C3_GetSignOutDoesNotClearSession()
    {
        var (password, secret, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        var code = Rfc6238Totp.ComputeCode(secret!, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var verify = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code }));
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        using var getSignOut = await client.SendAsync(AdminGet(AdminSignedOutExemptions.SignOut));
        Assert.True(
            getSignOut.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
            getSignOut.StatusCode.ToString());

        using var me = await client.SendAsync(AdminGet("/admin/api/me"));
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task R3_C3_PostSignOutClearsSession()
    {
        var (password, secret, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        var code = Rfc6238Totp.ComputeCode(secret!, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var verify = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code }));
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        using var signOut = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, AdminSignedOutExemptions.ApiSignOut));
        Assert.Equal(HttpStatusCode.OK, signOut.StatusCode);

        using var me = await client.SendAsync(AdminGet("/admin/api/me"));
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task R3_C7_NonAdminHostNeverRunsExemptions()
    {
        var client = CreateClient();
        using var request = AdminGet(AdminSignedOutExemptions.SignIn);
        request.Headers.Host = "api.core.dealoware.com";
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public void R3_C8_NoHandlersUnderAdminAuthStaticPrefix()
    {
        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints;
        Assert.DoesNotContain(
            endpoints,
            e => e is RouteEndpoint route
                 && route.RoutePattern.RawText is { } raw
                 && raw.StartsWith("/admin/auth/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task R3_C8_PostToAdminAuthStaticPrefixIsDenied()
    {
        var client = CreateClient();
        using var request = await JsonAsync(client, HttpMethod.Post, "/admin/auth/app.js", new { x = 1 });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SecR1_RecoveryCodesAreHmacSha256NotBareSha256()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: false);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using var setup = await client.SendAsync(AdminGet(SetupPath));
        var secret = (await setup.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("secret").GetString()!;
        var totp = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(secret), Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var confirm = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SetupPath, new { code = totp }));
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        using var reveal = await client.SendAsync(AdminGet(RecoveryCodesPath));
        var codes = (await reveal.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("recoveryCodes").EnumerateArray().Select(e => e.GetString()!).ToList();

        using var scope = _factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<AdminRecoveryCodeHasher>();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        foreach (var stored in db.AdminRecoveryCodes)
        {
            foreach (var plain in codes)
            {
                var sha256 = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(AdminRecoveryCodes.Normalize(plain))));
                Assert.False(AdminRecoveryCodes.FixedTimeEquals(stored.CodeHash, sha256));
            }
        }

        Assert.Contains(db.AdminRecoveryCodes, c => hasher.FixedTimeEquals(c.CodeHash, codes[0]));
    }

    [Fact]
    public async Task SecR2_ParallelRedeemOfSameCode_SucceedsOnce()
    {
        var (password, _, codes) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        var csrf = await IssueCsrfAsync(client);
        using var step1 = await client.SendAsync(
            await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }, csrf: csrf));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        var first = await JsonAsync(client, HttpMethod.Post, RecoveryPath, new { recoveryCode = codes![0] }, csrf: csrf);
        var second = await JsonAsync(client, HttpMethod.Post, RecoveryPath, new { recoveryCode = codes[0] }, csrf: csrf);
        var results = await Task.WhenAll(client.SendAsync(first), client.SendAsync(second));
        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.Unauthorized));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Equal(1, db.AdminSessions.Count(s => s.Email == CoreOwnerEmail && s.TotpVerified));
        Assert.Equal(1, db.AdminRecoveryCodes.Count(c => c.UsedAt != null));
    }

    [Fact]
    public async Task SecR2_ParallelBadAttempts_CannotExceedFive()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        var csrf = await IssueCsrfAsync(client);
        using var step1 = await client.SendAsync(
            await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }, csrf: csrf));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        var requests = Enumerable.Range(0, 8)
            .Select(_ => JsonAsync(client, HttpMethod.Post, CodePath, new { code = "000000" }, csrf: csrf));
        var messages = await Task.WhenAll(requests);
        var responses = await Task.WhenAll(messages.Select(client.SendAsync));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail);
        Assert.Equal(AdminCoreOwnerAccount.MaxFailedFactorAttempts, account.FailedFactorAttempts);
        Assert.True(account.IsFactorLocked());
        Assert.Equal(0, db.AdminSessions.Count(s => s.Email == CoreOwnerEmail && s.TotpVerified));
        var pending = db.AdminPendingAuths.Single(p => p.Email == CoreOwnerEmail);
        Assert.Equal(AdminPendingAuth.MaxFailedCodeAttempts, pending.FailedCodeAttempts);
        Assert.NotNull(pending.ConsumedAt);
    }

    [Fact]
    public async Task SecR2a_ParallelPendingFailureCount_StopsAtFive()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        var csrf = await IssueCsrfAsync(client);
        using var step1 = await client.SendAsync(
            await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }, csrf: csrf));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        var messages = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => JsonAsync(client, HttpMethod.Post, CodePath, new { code = "000000" }, csrf: csrf)));
        var responses = await Task.WhenAll(messages.Select(client.SendAsync));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var pending = db.AdminPendingAuths.Single(p => p.Email == CoreOwnerEmail);
        Assert.Equal(AdminPendingAuth.MaxFailedCodeAttempts, pending.FailedCodeAttempts);
        Assert.True(pending.FailedCodeAttempts <= AdminPendingAuth.MaxFailedCodeAttempts);
        Assert.NotNull(pending.ConsumedAt);
    }

    [Fact]
    public async Task SecR2b_ParallelConsumePending_MintsOneSession()
    {
        var (password, _, codes) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        var csrf = await IssueCsrfAsync(client);
        using var step1 = await client.SendAsync(
            await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }, csrf: csrf));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        var first = await JsonAsync(client, HttpMethod.Post, RecoveryPath, new { recoveryCode = codes![0] }, csrf: csrf);
        var second = await JsonAsync(client, HttpMethod.Post, RecoveryPath, new { recoveryCode = codes[1] }, csrf: csrf);
        var results = await Task.WhenAll(client.SendAsync(first), client.SendAsync(second));
        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.Unauthorized));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Equal(1, db.AdminSessions.Count(s => s.Email == CoreOwnerEmail && s.TotpVerified));
        Assert.Equal(1, db.AdminPendingAuths.Count(p => p.Email == CoreOwnerEmail && p.ConsumedAt != null));
    }

    [Fact]
    public async Task SecR2c_ParallelSameTotpStep_SucceedsOnce()
    {
        var (password, secret, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        var csrf = await IssueCsrfAsync(client);
        using var step1 = await client.SendAsync(
            await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }, csrf: csrf));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        var step = Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow);
        var code = Rfc6238Totp.ComputeCode(secret!, step);
        var first = await JsonAsync(client, HttpMethod.Post, CodePath, new { code }, csrf: csrf);
        var second = await JsonAsync(client, HttpMethod.Post, CodePath, new { code }, csrf: csrf);
        var results = await Task.WhenAll(client.SendAsync(first), client.SendAsync(second));
        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.Unauthorized));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Equal(1, db.AdminSessions.Count(s => s.Email == CoreOwnerEmail && s.TotpVerified));
        Assert.Equal(step, db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail).LastUsedTotpTimestep);
    }

    [Fact]
    public async Task SecR5_ParallelEnrollConfirm_LeavesExactlyTenCodes()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: false);
        var client = CreateClient();
        using var step1 = await client.SendAsync(
            await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using var setup = await client.SendAsync(AdminGet(SetupPath));
        var secret = (await setup.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("secret").GetString()!;
        var csrf = await IssueCsrfAsync(client);
        var code = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(secret), Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        var first = await JsonAsync(client, HttpMethod.Post, SetupPath, new { code }, csrf: csrf);
        var second = await JsonAsync(client, HttpMethod.Post, SetupPath, new { code }, csrf: csrf);
        var results = await Task.WhenAll(client.SendAsync(first), client.SendAsync(second));
        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.Unauthorized));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Equal(AdminRecoveryCodes.Count, db.AdminRecoveryCodes.Count());
        Assert.Equal(AdminRecoveryCodes.Count, db.AdminRecoveryCodes.Count(c => c.UsedAt == null));
        Assert.True(db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail).IsTotpEnrolled);
    }

    [Fact]
    public async Task SecR7_CorrectCodeAfterFifthSlot_DoesNotEvaluateOrMintSession()
    {
        var (password, secret, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        var csrf = await IssueCsrfAsync(client);
        using var step1 = await client.SendAsync(
            await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }, csrf: csrf));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        Guid pendingId;
        Guid accountId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var accounts = scope.ServiceProvider.GetRequiredService<IAdminCoreOwnerAccountRepository>();
            var pending = db.AdminPendingAuths.Single(p => p.Email == CoreOwnerEmail && p.ConsumedAt == null);
            var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail);
            pendingId = pending.Id;
            accountId = account.Id;
            var now = DateTimeOffset.UtcNow;
            for (var i = 0; i < AdminPendingAuth.MaxFailedCodeAttempts; i++)
            {
                Assert.True(await accounts.TryReserveSecondFactorAttemptAsync(pendingId, accountId, now));
            }

            Assert.False(await accounts.TryReserveSecondFactorAttemptAsync(pendingId, accountId, now));
        }

        Rfc6238Totp.ResetEvaluationCount();
        var code = Rfc6238Totp.ComputeCode(secret!, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        var messages = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ =>
                JsonAsync(client, HttpMethod.Post, CodePath, new { code }, csrf: csrf)));
        var responses = await Task.WhenAll(messages.Select(client.SendAsync));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
        Assert.True(Rfc6238Totp.EvaluationCount <= AdminCoreOwnerAccount.MaxFailedFactorAttempts);
        Assert.Equal(0, Rfc6238Totp.EvaluationCount);

        using var scopeAfter = _factory.Services.CreateScope();
        var dbAfter = scopeAfter.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Equal(0, dbAfter.AdminSessions.Count(s => s.Email == CoreOwnerEmail && s.TotpVerified));
        Assert.Equal(
            AdminPendingAuth.MaxFailedCodeAttempts,
            dbAfter.AdminPendingAuths.Single(p => p.Id == pendingId).FailedCodeAttempts);
        Assert.Equal(
            AdminCoreOwnerAccount.MaxFailedFactorAttempts,
            dbAfter.AdminCoreOwnerAccounts.Single(a => a.Id == accountId).FailedFactorAttempts);
    }

    [Fact]
    public async Task SecR3_PasswordLoginDoesNotResetFactorLock()
    {
        var (password, secret, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        for (var i = 0; i < AdminCoreOwnerAccount.MaxFailedFactorAttempts; i++)
        {
            using var wrong = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code = "000000" }));
            await AssertGenericSignIn(wrong);
        }

        using var step1Again = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1Again.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail);
        Assert.Equal(AdminCoreOwnerAccount.MaxFailedFactorAttempts, account.FailedFactorAttempts);
        Assert.True(account.IsFactorLocked());

        var valid = Rfc6238Totp.ComputeCode(secret!, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var stillLocked = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code = valid }));
        await AssertGenericSignIn(stillLocked);
        using var me = await client.SendAsync(AdminGet("/admin/api/me"));
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task SecR4_EnrollConfirmRecordsTimestep_SameCodeFailsAtLogin()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: false);
        var client = CreateClient();
        using var step1 = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using var setup = await client.SendAsync(AdminGet(SetupPath));
        var secret = (await setup.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("secret").GetString()!;
        var bytes = Rfc6238Totp.FromBase32(secret);
        var step = Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow);
        var code = Rfc6238Totp.ComputeCode(bytes, step);
        using var confirm = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SetupPath, new { code }));
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        Assert.Equal(step, db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail).LastUsedTotpTimestep);

        using var step1Again = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1Again.StatusCode);
        using var replay = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code }));
        await AssertGenericSignIn(replay);
    }

    [Fact]
    public async Task SecSc6_NewServiceInstanceKeepsFactorLock()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"dw-sc6-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        var totpKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var recoveryKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var ipKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        string? password = null;
        byte[]? secret = null;

        try
        {
            using (var first = new SharedStoreWebApplicationFactory(connectionString, totpKey, recoveryKey, ipKey))
            {
                (password, secret, _) = await SeedAccountAsync(enrolled: true, first);
                var client = CreateClient(first);
                using var step1 = await client.SendAsync(
                    await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
                Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

                for (var i = 0; i < AdminCoreOwnerAccount.MaxFailedFactorAttempts; i++)
                {
                    using var wrong = await client.SendAsync(
                        await JsonAsync(client, HttpMethod.Post, CodePath, new { code = "000000" }));
                    await AssertGenericSignIn(wrong);
                }

                using var scope = first.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
                var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail);
                Assert.Equal(AdminCoreOwnerAccount.MaxFailedFactorAttempts, account.FailedFactorAttempts);
                Assert.True(account.IsFactorLocked());
            }

            using var restarted = new SharedStoreWebApplicationFactory(connectionString, totpKey, recoveryKey, ipKey);
            using (var scope = restarted.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
                var account = db.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail);
                Assert.Equal(AdminCoreOwnerAccount.MaxFailedFactorAttempts, account.FailedFactorAttempts);
                Assert.True(account.IsFactorLocked());
            }

            var client2 = CreateClient(restarted);
            using var step1Again = await client2.SendAsync(
                await JsonAsync(client2, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
            Assert.Equal(HttpStatusCode.OK, step1Again.StatusCode);

            Assert.False(string.IsNullOrEmpty(password));
            Assert.NotNull(secret);
            var valid = Rfc6238Totp.ComputeCode(secret, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
            using var stillLocked = await client2.SendAsync(
                await JsonAsync(client2, HttpMethod.Post, CodePath, new { code = valid }));
            await AssertGenericSignIn(stillLocked);
            using var me = await client2.SendAsync(AdminGet("/admin/api/me"));
            Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        }
        finally
        {
            foreach (var leftover in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
            {
                if (File.Exists(leftover))
                    File.Delete(leftover);
            }
        }
    }

    [Fact]
    public async Task SecSc6_LockEndsPendingAndEverySession()
    {
        var (password, secret, _) = await SeedAccountAsync(enrolled: true);
        Guid existingSessionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            var session = AdminSession.Create(CoreOwnerEmail, "test-ip-hmac");
            session.MarkTotpVerified();
            db.AdminSessions.Add(session);
            await db.SaveChangesAsync();
            existingSessionId = session.Id;
        }

        var client = CreateClient();
        using (var meBefore = await client.SendAsync(AdminGet("/admin/api/me", existingSessionId)))
            Assert.Equal(HttpStatusCode.OK, meBefore.StatusCode);

        using var step1 = await client.SendAsync(
            await JsonAsync(client, HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        HttpResponseMessage? last = null;
        for (var i = 0; i < AdminCoreOwnerAccount.MaxFailedFactorAttempts; i++)
        {
            last?.Dispose();
            last = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code = "000000" }));
            await AssertGenericSignIn(last);
        }

        Assert.NotNull(last);
        await AssertGenericSignIn(last);
        last.Dispose();

        using var scopeAfter = _factory.Services.CreateScope();
        var dbAfter = scopeAfter.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var account = dbAfter.AdminCoreOwnerAccounts.Single(a => a.Email == CoreOwnerEmail);
        Assert.True(account.IsFactorLocked());
        Assert.Equal(0, dbAfter.AdminPendingAuths.Count(p => p.Email == CoreOwnerEmail && p.ConsumedAt == null));
        Assert.Equal(0, dbAfter.AdminSessions.Count(s => s.Email == CoreOwnerEmail));

        using var meAfter = await client.SendAsync(AdminGet("/admin/api/me", existingSessionId));
        Assert.Equal(HttpStatusCode.Unauthorized, meAfter.StatusCode);
        Assert.Null(meAfter.Headers.RetryAfter);
        var meBody = await meAfter.Content.ReadAsStringAsync();
        Assert.DoesNotContain("locked", meBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("attempt", meBody, StringComparison.OrdinalIgnoreCase);

        var valid = Rfc6238Totp.ComputeCode(secret!, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var replayPending = await client.SendAsync(await JsonAsync(client, HttpMethod.Post, CodePath, new { code = valid }));
        Assert.Equal(HttpStatusCode.Unauthorized, replayPending.StatusCode);
        Assert.Null(replayPending.Headers.RetryAfter);
        var replayBody = await replayPending.Content.ReadAsStringAsync();
        Assert.DoesNotContain("locked", replayBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Retry-After", replayBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("attempt", replayBody, StringComparison.OrdinalIgnoreCase);
    }
}
