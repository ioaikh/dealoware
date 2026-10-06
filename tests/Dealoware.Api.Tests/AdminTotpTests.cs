using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;
using Dealoware.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-004 / 012 / 020 / 021 / 022 plus r2 §2 exemption denies.
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

    private HttpClient CreateClient() => _factory.CreateClient();

    private async Task<(string Password, byte[]? Secret, IReadOnlyList<string>? Codes)> SeedAccountAsync(
        bool enrolled)
    {
        var password = $"Pw-{Guid.NewGuid():N}-xY9!";
        byte[]? secret = null;
        IReadOnlyList<string>? codes = null;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<TotpSecretProtector>();

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
                db.AdminRecoveryCodes.Add(AdminRecoveryCode.Create(account.Id, AdminRecoveryCodes.Hash(code)));
            }

            account.MarkRecoveryCodesIssued();
            account.CompleteEnrollment(protector.Encrypt(secret));
        }

        db.AdminCoreOwnerAccounts.Add(account);
        await db.SaveChangesAsync();
        return (password, secret, codes);
    }

    private static HttpRequestMessage Json(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Host = AdminHost;
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
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

    private static async Task AssertGenericSignIn(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(GenericSignInCopy, body);
        Assert.DoesNotContain("TOTP", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recovery", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("locked", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("2FA", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TdAdm012_TotpEnrollmentRequiredBeforeFirstSession()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: false);
        var client = CreateClient();

        using (var step1 = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password })))
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

        using var setup = await client.SendAsync(Json(HttpMethod.Get, SetupPath));
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        var enrolled = await setup.Content.ReadFromJsonAsync<JsonElement>();
        var secret = enrolled.GetProperty("secret").GetString();
        Assert.False(string.IsNullOrWhiteSpace(secret));

        var totp = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(secret!), Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var confirm = await client.SendAsync(Json(HttpMethod.Post, SetupPath, new { code = totp }));
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        using var codes = await client.SendAsync(Json(HttpMethod.Get, RecoveryCodesPath));
        Assert.Equal(HttpStatusCode.OK, codes.StatusCode);
        var shown = await codes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(AdminRecoveryCodes.Count, shown.GetProperty("recoveryCodes").GetArrayLength());

        using (var meAfterEnroll = await client.SendAsync(AdminGet("/admin/api/me")))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, meAfterEnroll.StatusCode);
        }

        using var step1Again = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1Again.StatusCode);
        var next = await step1Again.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("totp", next.GetProperty("next").GetString());

        var loginCode = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(secret!), Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var verify = await client.SendAsync(Json(HttpMethod.Post, CodePath, new { code = loginCode }));
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        using var meOk = await client.SendAsync(AdminGet("/admin/api/me"));
        Assert.Equal(HttpStatusCode.OK, meOk.StatusCode);
    }

    [Fact]
    public async Task TdAdm020_TotpRequiredOnEveryPostEnrollmentLogin()
    {
        var (password, secret, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();

        using (var omit = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password })))
            Assert.Equal(HttpStatusCode.OK, omit.StatusCode);

        using (var me = await client.SendAsync(AdminGet("/admin/api/me")))
            Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        using (var wrongTotp = await client.SendAsync(Json(HttpMethod.Post, CodePath, new { code = "000000" })))
            await AssertGenericSignIn(wrongTotp);

        using var step1 = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using (var wrongRecovery = await client.SendAsync(Json(HttpMethod.Post, RecoveryPath, new { recoveryCode = "AAAA-BBBB-CCCC" })))
            await AssertGenericSignIn(wrongRecovery);

        using var step1b = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        var code = Rfc6238Totp.ComputeCode(secret!, Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var ok = await client.SendAsync(Json(HttpMethod.Post, CodePath, new { code }));
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
            Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password = "not-the-password" }));
        using var step1 = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        using var badTotp = await client.SendAsync(Json(HttpMethod.Post, CodePath, new { code = "111111" }));

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

        using var step1 = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using var setup = await client.SendAsync(Json(HttpMethod.Get, SetupPath));
        var first = await setup.Content.ReadFromJsonAsync<JsonElement>();
        var secret = first.GetProperty("secret").GetString()!;
        Assert.False(first.TryGetProperty("recoveryCodes", out _));

        var totp = Rfc6238Totp.ComputeCode(Rfc6238Totp.FromBase32(secret), Rfc6238Totp.TimestepAt(DateTimeOffset.UtcNow));
        using var confirm = await client.SendAsync(Json(HttpMethod.Post, SetupPath, new { code = totp }));
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        using var reveal = await client.SendAsync(Json(HttpMethod.Get, RecoveryCodesPath));
        var shown = await reveal.Content.ReadFromJsonAsync<JsonElement>();
        var codes = shown.GetProperty("recoveryCodes").EnumerateArray().Select(e => e.GetString()!).ToList();
        Assert.Equal(AdminRecoveryCodes.Count, codes.Count);

        using var revealAgain = await client.SendAsync(Json(HttpMethod.Get, RecoveryCodesPath));
        var second = await revealAgain.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(second.TryGetProperty("recoveryCodes", out _));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        foreach (var stored in db.AdminRecoveryCodes)
        {
            foreach (var plain in codes)
            {
                Assert.DoesNotContain(plain, stored.CodeHash, StringComparison.OrdinalIgnoreCase);
            }
        }

        using var step1b = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        using var useCode = await client.SendAsync(Json(HttpMethod.Post, RecoveryPath, new { recoveryCode = codes[0].ToLowerInvariant() }));
        Assert.Equal(HttpStatusCode.OK, useCode.StatusCode);

        using var step1c = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        using var reuse = await client.SendAsync(Json(HttpMethod.Post, RecoveryPath, new { recoveryCode = codes[0] }));
        await AssertGenericSignIn(reuse);
    }

    [Fact]
    public async Task TdAdm004_PendingAuthDoesNotOpenAdminRoutes()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

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

        using var otherHost = Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password = "x" });
        otherHost.Headers.Host = "api.core.dealoware.com";
        using var otherHostResponse = await client.SendAsync(otherHost);
        Assert.Equal(HttpStatusCode.NotFound, otherHostResponse.StatusCode);
    }

    [Fact]
    public async Task R2_ExtraSegmentOnOwnedPath_IsDenied()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using var extra = await client.SendAsync(Json(HttpMethod.Post, CodePath + "/extra", new { code = "000000" }));
        Assert.Equal(HttpStatusCode.Unauthorized, extra.StatusCode);
        Assert.Contains("\"error\":\"Unauthorized\"", await extra.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task R2_WrongMethodOnOwnedPath_IsDenied()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        using var get = await client.SendAsync(Json(HttpMethod.Get, CodePath));
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Contains("\"error\":\"Unauthorized\"", await get.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task R2_MissingPendingToken_IsDenied()
    {
        var client = CreateClient();
        using var code = await client.SendAsync(Json(HttpMethod.Post, CodePath, new { code = "123456" }));
        Assert.Equal(HttpStatusCode.Unauthorized, code.StatusCode);
        Assert.Contains("\"error\":\"Unauthorized\"", await code.Content.ReadAsStringAsync());

        using var setup = await client.SendAsync(Json(HttpMethod.Get, SetupPath));
        Assert.Equal(HttpStatusCode.Unauthorized, setup.StatusCode);
    }

    [Fact]
    public async Task R2_PendingCookie_HasAdminPathHostOnlyFlags()
    {
        var (password, _, _) = await SeedAccountAsync(enrolled: true);
        var client = CreateClient();
        using var step1 = await client.SendAsync(Json(HttpMethod.Post, SignInPath, new { email = CoreOwnerEmail, password }));
        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);

        Assert.True(step1.Headers.TryGetValues("Set-Cookie", out var cookies));
        var pending = cookies.Single(c => c.StartsWith(AdminPendingAuthCookie.Name + "=", StringComparison.Ordinal));
        Assert.Contains("path=/admin", pending, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", pending, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", pending, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", pending, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", pending, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task R2_QueryStringDoesNotCreateExemption()
    {
        var client = CreateClient();
        using var request = Json(HttpMethod.Post, CodePath + "?next=/admin/", new { code = "123456" });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
