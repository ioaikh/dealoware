using System.Net;
using Dealoware.Api.Admin;
using Dealoware.Application.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dealoware.Api.Tests;

/// <summary>
/// T59 spec §2.8 (answer 4): the mail sender signs with the running service's own role
/// credentials from the container credentials endpoint (a session token, fetched again before
/// expiry); static keys only in Development. The signing checks use AWS's published SigV4
/// test-suite values. No test touches the network.
/// </summary>
public class SesMailRoleCredentialTests
{
    // AWS's published SigV4 test-suite credentials and session token (documentation values).
    private const string SuiteAccessKey = "AKIDEXAMPLE";
    private const string SuiteSigningKey = "wJalrXUtnFEMI/K7MDENG+bPxRfiCYEXAMPLEKEY";
    private const string SuiteSessionToken =
        "AQoDYXdzEPT//////////wEXAMPLEtc764bNrC9SAPBSM22wDOk4x4HIZ8j4FZTwdQWLWsKWHGBuFqwAeMicRXmxfpSPfIeoIYRqTflfKD8YUuwthAx7mSEI/qkPpKPi/kMcGdQrmGdeehM4IC1NtBmUpp2wUE8phUZampKsburEDy0KPkyQDYwT7WZ0wq5VSXDvp75YU9HFvlRd8Tx6q6fE8YQcHNVXAkiY9q6d+xo0rKwT38xVqr7ZD0u0iPPkUL64lIZbqBAz+scqKmlzm8FDrypNC9Yjc8fPOLn9FX9KSYvKTr4rvx3iSIlTJabIQwj2ICCR/oLxBA==";
    private static readonly DateTimeOffset SuiteTime = new(2015, 8, 30, 12, 36, 0, TimeSpan.Zero);
    private const string EmptyPayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    private const string From = "sender@example.test";
    private const string To = "recipient@example.test";
    private const string Region = "test-region";
    private const string RelativeUri = "/v2/credentials/role-test-path-id";
    private const string RoleKeyId = "ROLETESTKEYID";
    private const string RoleSigningValue = "role-test-signing-value";
    private const string RoleSessionToken = "role-test-session-token";

    private static readonly DateTimeOffset T0 = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    // ---- SigV4 -----------------------------------------------------------------------------

    [Fact]
    public void SigV4_PostVanilla_MatchesAwsTestSuite()
    {
        var authorization = AwsSigV4Signer.BuildAuthorization(
            "POST",
            "/",
            string.Empty,
            new Dictionary<string, string>
            {
                ["host"] = "example.amazonaws.com",
                ["x-amz-date"] = "20150830T123600Z"
            },
            EmptyPayloadHash,
            new AwsCredentials(SuiteAccessKey, SuiteSigningKey),
            "us-east-1",
            "service",
            SuiteTime);

        Assert.Equal(
            "AWS4-HMAC-SHA256 Credential=AKIDEXAMPLE/20150830/us-east-1/service/aws4_request, "
            + "SignedHeaders=host;x-amz-date, "
            + "Signature=5da7c1a2acd57cee7505fc6676e4e544621c30862966e37dddb68e92efbe5d6b",
            authorization);
    }

    [Fact]
    public void SigV4_PostWithSignedSessionToken_MatchesAwsTestSuite()
    {
        // The suite's "post-sts-header-after" case: the token is a signed header.
        var authorization = AwsSigV4Signer.BuildAuthorization(
            "POST",
            "/",
            string.Empty,
            new Dictionary<string, string>
            {
                ["host"] = "example.amazonaws.com",
                ["x-amz-date"] = "20150830T123600Z",
                ["x-amz-security-token"] = SuiteSessionToken
            },
            EmptyPayloadHash,
            new AwsCredentials(SuiteAccessKey, SuiteSigningKey, SuiteSessionToken),
            "us-east-1",
            "service",
            SuiteTime);

        Assert.Equal(
            "AWS4-HMAC-SHA256 Credential=AKIDEXAMPLE/20150830/us-east-1/service/aws4_request, "
            + "SignedHeaders=host;x-amz-date;x-amz-security-token, "
            + "Signature=85d96828115b5dc0cfc3bd16ad9e210dd772bbebba041836c64533a82be05ead",
            authorization);
    }

    [Fact]
    public void SignPost_WithSessionToken_SendsAndSignsTheTokenHeader()
    {
        using var request = SesShapedRequest();
        AwsSigV4Signer.SignPost(
            request,
            "{\"probe\":true}"u8,
            new AwsCredentials(SuiteAccessKey, SuiteSigningKey, SuiteSessionToken, T0),
            Region,
            "ses",
            new DateTimeOffset(2026, 10, 6, 1, 0, 0, TimeSpan.Zero));

        Assert.Equal(SuiteSessionToken, Assert.Single(request.Headers.GetValues("x-amz-security-token")));
        // Worked out independently of this code, from the SigV4 steps.
        Assert.Equal(
            "AWS4-HMAC-SHA256 Credential=AKIDEXAMPLE/20261006/test-region/ses/aws4_request, "
            + "SignedHeaders=content-type;host;x-amz-content-sha256;x-amz-date;x-amz-security-token, "
            + "Signature=c595dfe5c9fd37e7643d1947abf64a27dc6c38efa0d34cc49aa0d5609acd3b61",
            Assert.Single(request.Headers.GetValues("Authorization")));
    }

    [Fact]
    public void SignPost_WithStaticKeys_IsUnchangedAndSendsNoTokenHeader()
    {
        using var request = SesShapedRequest();
        AwsSigV4Signer.SignPost(
            request,
            "{\"probe\":true}"u8,
            new AwsCredentials(SuiteAccessKey, SuiteSigningKey),
            Region,
            "ses",
            new DateTimeOffset(2026, 10, 6, 1, 0, 0, TimeSpan.Zero));

        Assert.False(request.Headers.Contains("x-amz-security-token"));
        Assert.Equal(
            "AWS4-HMAC-SHA256 Credential=AKIDEXAMPLE/20261006/test-region/ses/aws4_request, "
            + "SignedHeaders=content-type;host;x-amz-content-sha256;x-amz-date, "
            + "Signature=7b790045f156c6d8236a7fcb08b829d145513a40b4606cbbef7c7bdc5fb645e7",
            Assert.Single(request.Headers.GetValues("Authorization")));
    }

    // ---- The container credentials endpoint ------------------------------------------------

    [Fact]
    public async Task ContainerFetch_RelativeUri_ReadsTheEcsEndpointAndParsesTheAnswer()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(CredentialsJson(RoleKeyId, RoleSessionToken, T0.AddHours(6))));
        var endpoint = ContainerCredentialsEndpoint.TryCreate(Env(
            (ContainerCredentialsEndpoint.RelativeUriEnvironmentVariable, RelativeUri)))!;
        var source = new ContainerAwsCredentialSource(endpoint, new HttpClient(handler), () => T0);

        var credentials = await source.GetCredentialsAsync(CancellationToken.None);

        var seen = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, seen.Method);
        Assert.Equal("http://169.254.170.2" + RelativeUri, seen.Uri.ToString());
        Assert.Equal("application/json", seen.Headers["Accept"]);
        Assert.False(seen.Headers.ContainsKey("Authorization"));
        Assert.Equal(RoleKeyId, credentials.AccessKeyId);
        Assert.Equal(RoleSigningValue, credentials.SecretAccessKey);
        Assert.Equal(RoleSessionToken, credentials.SessionToken);
        Assert.Equal(T0.AddHours(6), credentials.Expiration);
        Assert.Equal(nameof(AwsCredentials), credentials.ToString());
    }

    [Fact]
    public async Task ContainerFetch_FullUriWithToken_SendsTheTokenAsAuthorization()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(CredentialsJson(RoleKeyId, RoleSessionToken, T0.AddHours(1))));
        var endpoint = ContainerCredentialsEndpoint.TryCreate(Env(
            (ContainerCredentialsEndpoint.FullUriEnvironmentVariable, "http://127.0.0.1:51679/credentials"),
            (ContainerCredentialsEndpoint.AuthorizationTokenEnvironmentVariable, "fixed-auth-value")))!;
        var source = new ContainerAwsCredentialSource(endpoint, new HttpClient(handler), () => T0);

        await source.GetCredentialsAsync(CancellationToken.None);

        var seen = Assert.Single(handler.Requests);
        Assert.Equal("http://127.0.0.1:51679/credentials", seen.Uri.ToString());
        Assert.Equal("fixed-auth-value", seen.Headers["Authorization"]);
    }

    [Fact]
    public async Task ContainerFetch_TokenFile_WinsOverTheFixedToken_AndIsReadAgainOnRefresh()
    {
        var tokenFile = Path.Combine(Path.GetTempPath(), "dealoware-role-token-" + Guid.NewGuid().ToString("N"));
        await File.WriteAllTextAsync(tokenFile, "file-auth-value-one\n");
        try
        {
            var now = T0;
            var handler = new StubHttpHandler(_ => StubHttpHandler.Json(CredentialsJson(RoleKeyId, RoleSessionToken, now.AddHours(1))));
            var endpoint = ContainerCredentialsEndpoint.TryCreate(Env(
                (ContainerCredentialsEndpoint.FullUriEnvironmentVariable, "https://credentials.example.test/role"),
                (ContainerCredentialsEndpoint.AuthorizationTokenEnvironmentVariable, "fixed-auth-value"),
                (ContainerCredentialsEndpoint.AuthorizationTokenFileEnvironmentVariable, tokenFile)))!;
            var source = new ContainerAwsCredentialSource(endpoint, new HttpClient(handler), () => now);

            await source.GetCredentialsAsync(CancellationToken.None);
            await File.WriteAllTextAsync(tokenFile, "file-auth-value-two");
            now = T0.AddMinutes(58);
            await source.GetCredentialsAsync(CancellationToken.None);

            Assert.Equal(
                new[] { "file-auth-value-one", "file-auth-value-two" },
                handler.Requests.Select(r => r.Headers["Authorization"]).ToArray());
        }
        finally
        {
            File.Delete(tokenFile);
        }
    }

    [Fact]
    public async Task ContainerCredentials_AreKept_ThenFetchedAgainBeforeTheyExpire()
    {
        var now = T0;
        var expiry = T0.AddHours(1);
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(CredentialsJson("KEYONE", "token-one", expiry)));
        var source = new ContainerAwsCredentialSource(RelativeEndpoint(), new HttpClient(handler), () => now);

        var first = await source.GetCredentialsAsync(CancellationToken.None);
        Assert.Equal(1, handler.Count);

        // Six minutes before the expiry: still outside the five-minute window, so no fetch.
        now = expiry - TimeSpan.FromMinutes(6);
        Assert.Same(first, await source.GetCredentialsAsync(CancellationToken.None));
        Assert.Equal(1, handler.Count);

        // Five minutes before the expiry: the next send fetches new ones, while the old still work.
        handler.Respond = _ => StubHttpHandler.Json(CredentialsJson("KEYTWO", "token-two", T0.AddHours(2)));
        now = expiry - ContainerAwsCredentialSource.DefaultRefreshBeforeExpiry;
        var second = await source.GetCredentialsAsync(CancellationToken.None);

        Assert.Equal(2, handler.Count);
        Assert.True(now < first.Expiration);
        Assert.Equal("KEYTWO", second.AccessKeyId);
        Assert.Equal("token-two", second.SessionToken);
        Assert.Same(second, await source.GetCredentialsAsync(CancellationToken.None));
        Assert.Equal(2, handler.Count);
    }

    [Fact]
    public async Task ContainerCredentials_FailedEarlyFetch_KeepsWorkingOnes_ButNeverExpiredOnes()
    {
        var now = T0;
        var expiry = T0.AddHours(1);
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(CredentialsJson(RoleKeyId, RoleSessionToken, expiry)));
        var source = new ContainerAwsCredentialSource(RelativeEndpoint(), new HttpClient(handler), () => now);
        var first = await source.GetCredentialsAsync(CancellationToken.None);

        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);
        now = expiry - TimeSpan.FromMinutes(2);
        Assert.Same(first, await source.GetCredentialsAsync(CancellationToken.None));
        Assert.Equal(2, handler.Count);

        now = expiry;
        await Assert.ThrowsAsync<AwsContainerCredentialsException>(
            async () => await source.GetCredentialsAsync(CancellationToken.None));
        Assert.Equal(3, handler.Count);
    }

    [Fact]
    public async Task ContainerCredentials_ManySendsAtOnce_FetchOnce()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(CredentialsJson(RoleKeyId, RoleSessionToken, T0.AddHours(1))))
        {
            Delay = TimeSpan.FromMilliseconds(100)
        };
        var source = new ContainerAwsCredentialSource(RelativeEndpoint(), new HttpClient(handler), () => T0);

        var all = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => source.GetCredentialsAsync(CancellationToken.None).AsTask()));

        Assert.Equal(1, handler.Count);
        Assert.All(all, c => Assert.Same(all[0], c));
    }

    [Fact]
    public async Task ContainerFetch_Failure_SaysNothingOfTheAddressTokenOrAnswer()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(
            "{\"message\":\"sentinel-answer-text\"}", HttpStatusCode.Forbidden));
        var endpoint = ContainerCredentialsEndpoint.TryCreate(Env(
            (ContainerCredentialsEndpoint.FullUriEnvironmentVariable, "http://169.254.170.23/sentinel-address-path"),
            (ContainerCredentialsEndpoint.AuthorizationTokenEnvironmentVariable, "sentinel-auth-value")))!;
        var source = new ContainerAwsCredentialSource(endpoint, new HttpClient(handler), () => T0);

        var ex = await Assert.ThrowsAsync<AwsContainerCredentialsException>(
            async () => await source.GetCredentialsAsync(CancellationToken.None));

        Assert.Contains("403", ex.Message, StringComparison.Ordinal);
        Assert.Equal(AwsContainerCredentialsException.HttpReason(403), ex.Reason);
        Assert.DoesNotContain("sentinel", ex.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Null(ex.InnerException);
        Assert.Equal(nameof(ContainerCredentialsEndpoint), endpoint.ToString());
        Assert.Equal(nameof(ContainerAwsCredentialSource), source.ToString());
    }

    [Theory]
    [InlineData("{\"AccessKeyId\":\"K\",\"SecretAccessKey\":\"S\",\"Expiration\":\"2026-11-27T13:00:00Z\"}")]
    [InlineData("{\"AccessKeyId\":\"K\",\"SecretAccessKey\":\"S\",\"Token\":\"T\"}")]
    [InlineData("{\"AccessKeyId\":\"K\",\"SecretAccessKey\":\"S\",\"Token\":\"T\",\"Expiration\":\"not a time\"}")]
    [InlineData("{\"AccessKeyId\":\"K\",\"SecretAccessKey\":\"S\",\"Token\":\"T\",\"Expiration\":\"2026-11-27T11:59:59Z\"}")]
    [InlineData("{\"AccessKeyId\":\"K\",\"SecretAccessKey\":\"S\",\"Token\":\"T\\r\\nX-Injected: 1\",\"Expiration\":\"2026-11-27T13:00:00Z\"}")]
    [InlineData("{\"AccessKeyId\":\"\",\"SecretAccessKey\":\"S\",\"Token\":\"T\",\"Expiration\":\"2026-11-27T13:00:00Z\"}")]
    [InlineData("[]")]
    [InlineData("not json")]
    public async Task ContainerFetch_UnusableAnswer_IsRefused(string answer)
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(answer));
        var source = new ContainerAwsCredentialSource(RelativeEndpoint(), new HttpClient(handler), () => T0);

        var ex = await Assert.ThrowsAsync<AwsContainerCredentialsException>(
            async () => await source.GetCredentialsAsync(CancellationToken.None));
        Assert.Contains(ex.Reason, new[] { AwsContainerCredentialsException.BadJson, AwsContainerCredentialsException.Expired });
    }

    [Fact]
    public async Task ContainerFetch_OversizedAnswer_IsRefused()
    {
        var padding = new string('a', ContainerAwsCredentialSource.MaxResponseBytes);
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json("{\"Pad\":\"" + padding + "\"}"));
        var source = new ContainerAwsCredentialSource(RelativeEndpoint(), new HttpClient(handler), () => T0);

        var ex = await Assert.ThrowsAsync<AwsContainerCredentialsException>(
            async () => await source.GetCredentialsAsync(CancellationToken.None));
        Assert.Equal(AwsContainerCredentialsException.TooLarge, ex.Reason);
    }

    [Fact]
    public async Task ContainerFetch_AnswerThatStallsAfterItsHeaders_TimesOut_AndNeverHoldsTheNextCaller()
    {
        // Headers, then 10 of the 200 bytes promised, then nothing, and the stream does not even
        // watch its cancellation token. HttpClient's own timeout covers only the headers here.
        var handler = new StubHttpHandler(_ => StalledAnswer());
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };
        var source = new ContainerAwsCredentialSource(
            RelativeEndpoint(), client, () => T0, fetchTimeout: TimeSpan.FromMilliseconds(400));

        var first = source.GetCredentialsAsync(CancellationToken.None).AsTask();
        var second = source.GetCredentialsAsync(CancellationToken.None).AsTask();
        var all = Task.WhenAll(first, second);
        await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(10)));

        Assert.True(first.IsCompleted, "the first fetch was still waiting on a stalled answer");
        Assert.True(second.IsCompleted, "the second caller was still blocked behind the first");
        foreach (var call in new[] { first, second })
        {
            var ex = await Assert.ThrowsAsync<AwsContainerCredentialsException>(() => call);
            Assert.Equal(AwsContainerCredentialsException.Timeout, ex.Reason);
        }

        // Each caller made its own bounded try; the source is free for the next one.
        Assert.Equal(2, handler.Count);
        handler.Respond = _ => StubHttpHandler.Json(CredentialsJson(RoleKeyId, RoleSessionToken, T0.AddHours(1)));
        var later = await source.GetCredentialsAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(RoleKeyId, later.AccessKeyId);
    }

    [Fact]
    public async Task ContainerFetch_HandlerThatNeverAnswers_TimesOut()
    {
        var never = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new HttpClient(new NeverAnsweringHandler(never.Task)) { Timeout = Timeout.InfiniteTimeSpan };
        var source = new ContainerAwsCredentialSource(
            RelativeEndpoint(), client, () => T0, fetchTimeout: TimeSpan.FromMilliseconds(300));

        var call = source.GetCredentialsAsync(CancellationToken.None).AsTask();
        await Task.WhenAny(call, Task.Delay(TimeSpan.FromSeconds(10)));

        Assert.True(call.IsCompleted, "the fetch was still waiting on a handler that never answers");
        var ex = await Assert.ThrowsAsync<AwsContainerCredentialsException>(() => call);
        Assert.Equal(AwsContainerCredentialsException.Timeout, ex.Reason);
        never.TrySetResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    [Fact]
    public async Task ContainerFetch_CallerCancellation_IsNotReportedAsATimeout()
    {
        var handler = new StubHttpHandler(_ => StalledAnswer());
        using var client = new HttpClient(handler);
        var source = new ContainerAwsCredentialSource(
            RelativeEndpoint(), client, () => T0, fetchTimeout: TimeSpan.FromSeconds(30));
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await source.GetCredentialsAsync(cancel.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Theory]
    [InlineData("/v2/credentials/abc", null, "http://169.254.170.2/v2/credentials/abc")]
    [InlineData("/v2/credentials/abc", "https://other.example.test/x", "http://169.254.170.2/v2/credentials/abc")]
    [InlineData(null, "https://credentials.example.test/role", "https://credentials.example.test/role")]
    [InlineData(null, "http://127.0.0.1:8080/creds", "http://127.0.0.1:8080/creds")]
    [InlineData(null, "http://localhost/creds", "http://localhost/creds")]
    [InlineData(null, "http://169.254.170.2/creds", "http://169.254.170.2/creds")]
    [InlineData(null, "http://169.254.170.23/v1/credentials", "http://169.254.170.23/v1/credentials")]
    [InlineData(null, "http://[fd00:ec2::23]/v1/credentials", "http://[fd00:ec2::23]/v1/credentials")]
    [InlineData("//elsewhere.example.test/creds", null, null)]
    [InlineData("v2/credentials/abc", null, null)]
    [InlineData("/\\elsewhere", null, null)]
    [InlineData(null, "http://credentials.example.test/role", null)]
    [InlineData(null, "http://10.0.0.5/role", null)]
    [InlineData(null, "https://user:pw@credentials.example.test/role", null)]
    [InlineData(null, "file:///etc/creds", null)]
    [InlineData(null, "not a uri", null)]
    [InlineData(null, null, null)]
    public void ContainerEndpoint_AcceptsOnlyTheAddressesTheSdksAccept(string? relative, string? full, string? expected)
    {
        Assert.Equal(expected, ContainerCredentialsEndpoint.ResolveUri(relative, full)?.ToString());
    }

    // ---- Choosing the credential source -----------------------------------------------------

    [Fact]
    public void Options_OutsideDevelopment_UseTheRoleCredentials()
    {
        var options = SesMailOptions.TryCreate(MailEnv(withRole: true, withStaticKeys: false), allowStaticKeys: false);

        Assert.NotNull(options);
        Assert.Equal(SesMailCredentialSource.ContainerRole, options!.CredentialSource);
        Assert.Equal("http://169.254.170.2" + RelativeUri, options.ContainerEndpoint!.Uri.ToString());
        Assert.Null(options.AccessKeyId);
        Assert.Null(options.SecretAccessKey);
    }

    [Fact]
    public void Options_OutsideDevelopment_NeverUseStaticKeys()
    {
        Assert.Null(SesMailOptions.TryCreate(MailEnv(withRole: false, withStaticKeys: true), allowStaticKeys: false));

        var both = SesMailOptions.TryCreate(MailEnv(withRole: true, withStaticKeys: true), allowStaticKeys: false);
        Assert.Equal(SesMailCredentialSource.ContainerRole, both!.CredentialSource);
        Assert.Null(both.AccessKeyId);
        Assert.Null(both.SecretAccessKey);
    }

    [Fact]
    public void Options_InDevelopment_MayUseStaticKeys()
    {
        var options = SesMailOptions.TryCreate(MailEnv(withRole: false, withStaticKeys: true), allowStaticKeys: true);

        Assert.Equal(SesMailCredentialSource.StaticKeys, options!.CredentialSource);
        Assert.Equal("DEVKEYID", options.AccessKeyId);
        Assert.Null(options.ContainerEndpoint);
        Assert.Equal(nameof(SesMailOptions), options.ToString());
    }

    [Fact]
    public void Options_WithNeitherSource_LeaveMailOff()
    {
        Assert.Null(SesMailOptions.TryCreate(MailEnv(withRole: false, withStaticKeys: false), allowStaticKeys: true));
        Assert.Null(SesMailOptions.TryCreate(MailEnv(withRole: false, withStaticKeys: false), allowStaticKeys: false));
        Assert.Null(SesMailOptions.TryCreate(Env(
            (SesMailOptions.FromEnvironmentVariable, From),
            (SesMailOptions.RegionEnvironmentVariable, Region),
            (ContainerCredentialsEndpoint.FullUriEnvironmentVariable, "http://credentials.example.test/role")), allowStaticKeys: false));
    }

    [Fact]
    public void DisabledMessage_NamesBothCredentialSources()
    {
        foreach (var name in new[]
                 {
                     SesMailOptions.FromEnvironmentVariable,
                     SesMailOptions.RegionEnvironmentVariable,
                     ContainerCredentialsEndpoint.RelativeUriEnvironmentVariable,
                     ContainerCredentialsEndpoint.FullUriEnvironmentVariable,
                     SesMailOptions.AccessKeyEnvironmentVariable,
                     SesMailOptions.SecretKeyEnvironmentVariable
                 })
        {
            Assert.Contains(name, SesMailOptions.NotConfiguredMessage, StringComparison.Ordinal);
        }

        Assert.Contains("Development", SesMailOptions.NotConfiguredMessage, StringComparison.Ordinal);
    }

    // ---- Sending with the role's credentials -------------------------------------------------

    [Fact]
    public async Task Send_WithRoleCredentials_SignsWithTheSessionToken()
    {
        var credentialsHandler = new StubHttpHandler(_ => StubHttpHandler.Json(CredentialsJson(RoleKeyId, RoleSessionToken, T0.AddHours(1))));
        var sesHandler = new StubHttpHandler(_ => StubHttpHandler.Json("{}"));
        var options = SesMailOptions.ForContainerRole(From, Region, RelativeEndpoint());
        var sender = new SesMailSender(
            options,
            new HttpClient(sesHandler),
            options.CreateCredentialSource(new HttpClient(credentialsHandler), () => T0),
            () => T0);

        await sender.SendAsync(Message());
        await sender.SendAsync(Message());

        Assert.Equal(1, credentialsHandler.Count);
        Assert.Equal(2, sesHandler.Count);
        foreach (var seen in sesHandler.Requests)
        {
            Assert.Equal(RoleSessionToken, seen.Headers["x-amz-security-token"]);
            Assert.StartsWith("AWS4-HMAC-SHA256 Credential=" + RoleKeyId + "/20261127/test-region/ses/aws4_request, ", seen.Headers["Authorization"]);
            Assert.Contains("SignedHeaders=content-type;host;x-amz-content-sha256;x-amz-date;x-amz-security-token,", seen.Headers["Authorization"]);
            Assert.DoesNotContain(RoleSigningValue, string.Join("\n", seen.Headers.Values) + seen.Body, StringComparison.Ordinal);
        }
    }

    // ---- The service registration -------------------------------------------------------------

    [Fact]
    public async Task AddAdminMail_Production_SendsWithTheRoleCredentials_AndLogsNoneOfThem()
    {
        var logs = new CollectingLoggerProvider();
        var (provider, credentialsHandler, sesHandler) = BuildMail("Production", withRole: true, withStaticKeys: true, logs);
        await using (provider)
        {
            var sender = provider.GetRequiredService<IAdminMailSender>();
            Assert.IsType<SesMailSender>(sender);

            await sender.SendAsync(Message(), CancellationToken.None);

            var fetch = Assert.Single(credentialsHandler.Requests);
            Assert.Equal("http://169.254.170.2" + RelativeUri, fetch.Uri.ToString());
            var send = Assert.Single(sesHandler.Requests);
            Assert.Equal(RoleSessionToken, send.Headers["x-amz-security-token"]);
            Assert.StartsWith("AWS4-HMAC-SHA256 Credential=" + RoleKeyId + "/", send.Headers["Authorization"]);
            Assert.DoesNotContain("DEVKEYID", send.Headers["Authorization"], StringComparison.Ordinal);

            var logText = string.Join("\n", logs.Lines);
            // Request logging is on (the SES client's request is logged), so the silence below is real.
            Assert.Contains("email.test-region.amazonaws.com", logText, StringComparison.Ordinal);
            Assert.DoesNotContain("169.254.170.2", logText, StringComparison.Ordinal);
            foreach (var hidden in new[] { RoleKeyId, RoleSigningValue, RoleSessionToken, "role-test-path-id", "DEVKEYID", "dev-signing-value" })
            {
                Assert.DoesNotContain(hidden, logText, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public async Task AddAdminMail_Production_WithOnlyStaticKeys_LeavesMailOff()
    {
        var (provider, credentialsHandler, sesHandler) = BuildMail("Production", withRole: false, withStaticKeys: true);
        await using (provider)
        {
            var sender = provider.GetRequiredService<IAdminMailSender>();
            Assert.IsType<DisabledMailSender>(sender);
            await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync(Message(), CancellationToken.None));
            Assert.Equal(0, credentialsHandler.Count);
            Assert.Equal(0, sesHandler.Count);
        }
    }

    [Fact]
    public async Task AddAdminMail_Development_MayUseStaticKeys()
    {
        var (provider, credentialsHandler, sesHandler) = BuildMail("Development", withRole: false, withStaticKeys: true);
        await using (provider)
        {
            var sender = provider.GetRequiredService<IAdminMailSender>();
            Assert.IsType<SesMailSender>(sender);

            await sender.SendAsync(Message(), CancellationToken.None);

            Assert.Equal(0, credentialsHandler.Count);
            var send = Assert.Single(sesHandler.Requests);
            Assert.StartsWith("AWS4-HMAC-SHA256 Credential=DEVKEYID/", send.Headers["Authorization"]);
            Assert.False(send.Headers.ContainsKey("x-amz-security-token"));
        }
    }

    // ---- Helpers -------------------------------------------------------------------------------

    private static (ServiceProvider Provider, StubHttpHandler Credentials, StubHttpHandler Ses) BuildMail(
        string environment,
        bool withRole,
        bool withStaticKeys,
        CollectingLoggerProvider? logs = null)
    {
        var env = MailEnv(withRole, withStaticKeys);
        // A stray "environment" setting must not decide it: the host environment does.
        var settings = new Dictionary<string, string?>
        {
            ["environment"] = environment == "Development" ? "Production" : "Development"
        };
        foreach (var name in new[]
                 {
                     SesMailOptions.FromEnvironmentVariable,
                     SesMailOptions.RegionEnvironmentVariable,
                     SesMailOptions.AccessKeyEnvironmentVariable,
                     SesMailOptions.SecretKeyEnvironmentVariable,
                     ContainerCredentialsEndpoint.RelativeUriEnvironmentVariable
                 })
        {
            // Blank values shadow anything the test process itself may have set.
            settings[name] = env(name) ?? string.Empty;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var credentialsHandler = new StubHttpHandler(_ => StubHttpHandler.Json(CredentialsJson(RoleKeyId, RoleSessionToken, DateTimeOffset.UtcNow.AddHours(1))));
        var sesHandler = new StubHttpHandler(_ => StubHttpHandler.Json("{}"));

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            if (logs is not null)
                builder.AddProvider(logs);
        });
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(environment));
        services.AddAdminMail(configuration);
        services.AddHttpClient(AdminMailServiceCollectionExtensions.CredentialsHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => credentialsHandler);
        services.AddHttpClient(AdminMailServiceCollectionExtensions.SesHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => sesHandler);
        return (services.BuildServiceProvider(), credentialsHandler, sesHandler);
    }

    private static Func<string, string?> MailEnv(bool withRole, bool withStaticKeys)
    {
        var pairs = new List<(string, string)>
        {
            (SesMailOptions.FromEnvironmentVariable, From),
            (SesMailOptions.RegionEnvironmentVariable, Region)
        };
        if (withRole)
            pairs.Add((ContainerCredentialsEndpoint.RelativeUriEnvironmentVariable, RelativeUri));
        if (withStaticKeys)
        {
            pairs.Add((SesMailOptions.AccessKeyEnvironmentVariable, "DEVKEYID"));
            pairs.Add((SesMailOptions.SecretKeyEnvironmentVariable, "dev-signing-value"));
        }

        return Env(pairs.ToArray());
    }

    private static Func<string, string?> Env(params (string Name, string Value)[] pairs)
    {
        var map = pairs.ToDictionary(p => p.Name, p => p.Value, StringComparer.Ordinal);
        return name => map.TryGetValue(name, out var value) ? value : null;
    }

    private static ContainerCredentialsEndpoint RelativeEndpoint()
        => ContainerCredentialsEndpoint.TryCreate(Env((ContainerCredentialsEndpoint.RelativeUriEnvironmentVariable, RelativeUri)))!;

    private static string CredentialsJson(string keyId, string sessionToken, DateTimeOffset expiration)
        => "{\"AccessKeyId\":\"" + keyId + "\",\"SecretAccessKey\":\"" + RoleSigningValue
           + "\",\"Token\":\"" + sessionToken + "\",\"Expiration\":\""
           + expiration.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture) + "\"}";

    private static HttpRequestMessage SesShapedRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://email.test-region.amazonaws.com/v2/email/outbound-emails")
        {
            Content = new ByteArrayContent("{\"probe\":true}"u8.ToArray())
        };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return request;
    }

    private static HttpResponseMessage StalledAnswer()
    {
        var content = new StreamContent(new StalledStream("{\"AccessKe"u8.ToArray()));
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        content.Headers.ContentLength = 200;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    /// <summary>Gives its first bytes, then never another one; it ignores cancellation until disposed.</summary>
    private sealed class StalledStream(byte[] start) : Stream
    {
        private readonly TaskCompletionSource<int> _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _position;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_position < start.Length)
            {
                var count = Math.Min(buffer.Length, start.Length - _position);
                start.AsMemory(_position, count).CopyTo(buffer);
                _position += count;
                return count;
            }

            return await _disposed.Task.ConfigureAwait(false);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            _disposed.TrySetResult(0);
            base.Dispose(disposing);
        }
    }

    /// <summary>A handler that ignores cancellation and answers only when the test says so.</summary>
    private sealed class NeverAnsweringHandler(Task<HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => answer;
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Dealoware.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static AdminMailMessage Message()
        => new(To, AdminMailDispatcher.BootstrapSubject,
            "Use this one-time link to finish Core admin bootstrap.\n\n"
            + AdminMailPagePaths.Build(MailLinkKind.Bootstrap, "fixture") + "\n");
}
