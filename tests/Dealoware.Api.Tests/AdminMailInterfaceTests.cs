using System.Net;
using System.Text;
using Dealoware.Application.Admin;
using Dealoware.Domain.Mail;
using Dealoware.Infrastructure.Mail;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-110 / TD-ADM-150: admin mail port, SES HTTP adapter, no AWS SDK, no invented account details.
/// Token links are built by the mail layer (route note r3 b079a814; fragment form b6194918 item 6).
/// </summary>
public class AdminMailInterfaceTests
{
    private const string TestRecipient = "recipient@test.local";
    private const string TestFrom = "sender@test.local";
    private const string TestRegion = "test-region";
    private const string TestAccessKey = "TESTACCESSKEYID";
    private const string TestSecret = "test-secret-not-a-real-key";
    private const string FixtureToken = "fixture";

    [Fact]
    public async Task TD_ADM_110_BootstrapAndReset_UseMailInterfaceOnly()
    {
        var recorder = new RecordingMailSender();
        var dispatcher = new AdminMailDispatcher(recorder);

        await dispatcher.SendBootstrapLinkAsync(TestRecipient, FixtureToken);
        await dispatcher.SendPasswordResetLinkAsync(TestRecipient, FixtureToken);

        Assert.Equal(2, recorder.Sent.Count);
        Assert.Contains(recorder.Sent, m => m.Purpose == MailPurpose.Bootstrap && m.Subject == AdminMailDispatcher.BootstrapSubject);
        Assert.Contains(recorder.Sent, m => m.Purpose == MailPurpose.PasswordReset && m.Subject == AdminMailDispatcher.PasswordResetSubject);
        Assert.All(recorder.Sent, m =>
        {
            Assert.Equal(TestRecipient, m.To);
            AssertBuiltLink(m.TextBody, expectReset: m.Purpose == MailPurpose.PasswordReset, FixtureToken);
            Assert.DoesNotContain("/admin/api/", m.TextBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(TestSecret, m.TextBody);
        });
    }

    [Fact]
    public void TD_ADM_110_MailLayer_BuildsFixedOriginPaths()
    {
        Assert.Equal(
            ExpectedLink(MailLinkKind.Reset, FixtureToken),
            AdminMailPagePaths.Build(MailLinkKind.Reset, FixtureToken));
        Assert.Equal(
            ExpectedLink(MailLinkKind.Bootstrap, FixtureToken),
            AdminMailPagePaths.Build(MailLinkKind.Bootstrap, FixtureToken));
        Assert.Throws<ArgumentOutOfRangeException>(() => AdminMailPagePaths.Build((MailLinkKind)0, FixtureToken));
        Assert.Throws<ArgumentException>(() => AdminMailPagePaths.Build(MailLinkKind.Reset, ""));
        Assert.Throws<ArgumentException>(() => AdminMailPagePaths.Build(MailLinkKind.Reset, new string('a', AdminMailPagePaths.MaxTokenLength + 1)));

        var source = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Dealoware.Application", "Admin", "AdminMailPagePaths.cs"));
        Assert.Contains("public const string Origin = \"https://admin.core.dealoware.com\";", source);
        Assert.DoesNotContain("?token=", source);
        Assert.DoesNotContain("Request.", source);
    }

    [Fact]
    public async Task TD_ADM_110_EmailLinks_HaveNoQueryAndExactlyOneTokenFragment()
    {
        var recorder = new RecordingMailSender();
        var dispatcher = new AdminMailDispatcher(recorder);
        const string token = "a?b#c&d";

        await dispatcher.SendBootstrapLinkAsync(TestRecipient, token);
        await dispatcher.SendPasswordResetLinkAsync(TestRecipient, token);

        Assert.Equal(2, recorder.Sent.Count);
        Assert.All(recorder.Sent, m =>
        {
            var url = ExtractMailLink(m.TextBody);
            AssertLinkHasNoQueryAndOneTokenFragment(url);
            Assert.StartsWith(AdminMailPagePaths.Origin, url, StringComparison.Ordinal);
        });
        Assert.Equal(
            "https://admin.core.dealoware.com/admin/reset/confirm#token=" + Uri.EscapeDataString(token),
            AdminMailPagePaths.Build(MailLinkKind.Reset, token));
        Assert.Equal(
            "https://admin.core.dealoware.com/admin/bootstrap#token=" + Uri.EscapeDataString(token),
            AdminMailPagePaths.Build(MailLinkKind.Bootstrap, token));
    }

    [Theory]
    [InlineData("&")]
    [InlineData("?")]
    [InlineData("#")]
    [InlineData("/")]
    [InlineData("%")]
    [InlineData("+")]
    [InlineData("=")]
    [InlineData(" ")]
    [InlineData("a b&c?#/%+=d")]
    [InlineData("café")]
    [InlineData("トークン")]
    public async Task TD_ADM_110_TokenSpecialCharacters_AreEncoded_AndBodyUsesFixedOrigin(string token)
    {
        var recorder = new RecordingMailSender();
        var dispatcher = new AdminMailDispatcher(recorder);

        await dispatcher.SendPasswordResetLinkAsync(TestRecipient, token);
        await dispatcher.SendBootstrapLinkAsync(TestRecipient, token);

        Assert.Equal(2, recorder.Sent.Count);
        foreach (var message in recorder.Sent)
        {
            var uri = AssertBuiltLink(message.TextBody, expectReset: message.Purpose == MailPurpose.PasswordReset, token);
            Assert.Equal(Uri.EscapeDataString(token), TokenPlacementValue(uri));
            Assert.True(string.IsNullOrEmpty(uri.UserInfo));
            Assert.True(uri.IsDefaultPort);
            Assert.DoesNotContain(":" + uri.Port, message.TextBody, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task TD_ADM_110_SesAdapter_PostsSignedJson_WithoutSdkTypes()
    {
        var handler = new CaptureHandler();
        using var client = new HttpClient(handler);
        var options = new SesMailOptions(TestFrom, TestRegion, TestAccessKey, TestSecret);
        var sender = new SesMailSender(options, client, () => new DateTimeOffset(2026, 10, 6, 1, 0, 0, TimeSpan.Zero));
        var body = "Use this one-time link to reset the Core admin password.\n\n"
            + AdminMailPagePaths.Build(MailLinkKind.Reset, FixtureToken) + "\n";

        await sender.SendAsync(new AdminMailMessage(TestRecipient, MailPurpose.PasswordReset, "Core admin password reset", body));

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("email.test-region.amazonaws.com", handler.Request.RequestUri!.Host);
        Assert.Equal("/v2/email/outbound-emails", handler.Request.RequestUri.AbsolutePath);
        Assert.Contains("application/json", handler.Request.Content!.Headers.ContentType!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(TestFrom, handler.Body);
        Assert.Contains(TestRecipient, handler.Body);
        Assert.Contains("/admin/reset/confirm", handler.Body, StringComparison.Ordinal);
        Assert.Contains(AdminMailPagePaths.Origin, handler.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(TestSecret, handler.Body);
        Assert.DoesNotContain(TestSecret, handler.Request.Headers.ToString());
        Assert.DoesNotContain("Amazon.", handler.Request.GetType().FullName);
        Assert.StartsWith("AWS4-HMAC-SHA256 Credential=" + TestAccessKey, handler.Authorization);
        Assert.DoesNotContain("AWSSDK", typeof(SesMailSender).Assembly.GetName().Name);
    }

    [Fact]
    public async Task TD_ADM_110_SesAdapter_HttpFailure_IsSafe()
    {
        var handler = new CaptureHandler { Status = HttpStatusCode.Forbidden };
        using var client = new HttpClient(handler);
        var sender = new SesMailSender(
            new SesMailOptions(TestFrom, TestRegion, TestAccessKey, TestSecret),
            client);
        var body = "Use this one-time link to finish Core admin bootstrap.\n\n"
            + AdminMailPagePaths.Build(MailLinkKind.Bootstrap, FixtureToken) + "\n";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync(new AdminMailMessage(TestRecipient, MailPurpose.Bootstrap, "Core admin bootstrap", body)));

        Assert.Contains("403", ex.Message);
        Assert.DoesNotContain(TestSecret, ex.Message);
        Assert.DoesNotContain(TestAccessKey, ex.Message);
        Assert.DoesNotContain(TestRecipient, ex.Message);
    }

    [Fact]
    public async Task TD_ADM_110_MissingMailConfig_UsesDisabledSender()
    {
        Assert.Null(SesMailOptions.TryCreate(_ => null));
        var sender = new DisabledMailSender();
        var body = "Use this one-time link to finish Core admin bootstrap.\n\n"
            + AdminMailPagePaths.Build(MailLinkKind.Bootstrap, FixtureToken) + "\n";
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync(new AdminMailMessage(TestRecipient, MailPurpose.Bootstrap, "Core admin bootstrap", body)));
        Assert.Contains(SesMailOptions.FromEnvironmentVariable, ex.Message);
        Assert.Contains(SesMailOptions.RegionEnvironmentVariable, ex.Message);
        Assert.Contains(SesMailOptions.AccessKeyEnvironmentVariable, ex.Message);
        Assert.Contains(SesMailOptions.SecretKeyEnvironmentVariable, ex.Message);
        Assert.DoesNotContain(TestSecret, ex.Message);
    }

    [Fact]
    public void TD_ADM_110_CoreSources_HaveNoAwsSesSdkTypes()
    {
        var root = RepoRoot();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("AWSSDK", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Amazon.SimpleEmail", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Amazon.SES", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Amazon.", text, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("to", "a@b\r")]
    [InlineData("to", "a@b\n")]
    [InlineData("to", "a@b\t")]
    [InlineData("to", "a@b\0")]
    [InlineData("to", "a@b\u007f")]
    [InlineData("to", "a@b\u200b")]
    [InlineData("to", "a@b\u202e")]
    [InlineData("subject", "hello\r")]
    [InlineData("subject", "hello\n")]
    [InlineData("subject", "hello\t")]
    [InlineData("subject", "hello\0")]
    [InlineData("subject", "hello\u007f")]
    [InlineData("subject", "hello\u200b")]
    [InlineData("subject", "hello\u202e")]
    public void TD_ADM_150_AdminMailMessage_RejectsControlAndFormatCharacters(string field, string value)
    {
        if (field == "to")
        {
            Assert.Throws<ArgumentException>(() =>
                new AdminMailMessage(value, MailPurpose.Bootstrap, "Core admin bootstrap", "body"));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
                new AdminMailMessage(TestRecipient, MailPurpose.Bootstrap, value, "body"));
        }
    }

    [Theory]
    [InlineData("a@b,c@d")]
    [InlineData("a@b;c@d")]
    [InlineData("Friends: a@b;")]
    [InlineData("Name <a@b>")]
    [InlineData("a@b@c")]
    [InlineData("a@")]
    [InlineData("@b")]
    [InlineData("a b@c")]
    public void TD_ADM_150_AdminMailMessage_To_RejectsListsAndGroups(string to)
    {
        Assert.Throws<ArgumentException>(() =>
            new AdminMailMessage(to, MailPurpose.Bootstrap, "Core admin bootstrap", "body"));
    }

    [Theory]
    [InlineData("sender@test.local\r")]
    [InlineData("sender@test.local\n")]
    [InlineData("sender@test.local\t")]
    [InlineData("sender@test.local\0")]
    [InlineData("sender@test.local\u007f")]
    [InlineData("sender@test.local\u200b")]
    [InlineData("sender@test.local\u202e")]
    [InlineData("a@b,c@d")]
    [InlineData("a@b;c@d")]
    [InlineData("Friends: a@b;")]
    [InlineData("Name <a@b>")]
    public void TD_ADM_150_SesMailOptions_From_RejectsControlsListsAndGroups(string from)
    {
        Assert.Throws<ArgumentException>(() =>
            new SesMailOptions(from, TestRegion, TestAccessKey, TestSecret));
    }

    [Fact]
    public void TD_ADM_150_MailSources_OmitSecretsAndAwsAccountDetails()
    {
        var root = RepoRoot();
        var files = Directory.GetFiles(Path.Combine(root, "src", "Dealoware.Domain", "Mail"), "*.cs")
            .Concat(Directory.GetFiles(Path.Combine(root, "src", "Dealoware.Application", "Admin"), "*.cs"))
            .Concat(Directory.GetFiles(Path.Combine(root, "src", "Dealoware.Infrastructure", "Mail"), "*.cs"))
            .Append(Path.Combine(root, "src", "Dealoware.Api", "Admin", "AdminMailServiceCollectionExtensions.cs"))
            .Append(Path.Combine(root, "src", "Dealoware.Api", "Program.cs"));

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("AKIA", text, StringComparison.Ordinal);
            Assert.DoesNotContain("arn:aws", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("us-east-1", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("eu-west-1", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("f03a8c68", text, StringComparison.OrdinalIgnoreCase);
        }

        var envNames = File.ReadAllText(Path.Combine(root, "src", "Dealoware.Infrastructure", "Mail", "SesMailOptions.cs"));
        Assert.Contains(SesMailOptions.FromEnvironmentVariable, envNames);
        Assert.Contains(SesMailOptions.RegionEnvironmentVariable, envNames);
        Assert.DoesNotContain("@dealoware.com", envNames, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TD_ADM_150_FactoryHost_UsesRecordingMailSender()
    {
        using var factory = new IsolatedWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IAdminMailSender>();
        Assert.IsType<RecordingMailSender>(sender);
        var dispatcher = scope.ServiceProvider.GetRequiredService<AdminMailDispatcher>();
        Assert.NotNull(dispatcher);
    }

    private static Uri AssertBuiltLink(string body, bool expectReset, string token)
    {
        var url = ExtractMailLink(body);
        AssertLinkHasNoQueryAndOneTokenFragment(url);
        Assert.True(Uri.TryCreate(url, UriKind.Absolute, out var uri));
        Assert.Equal(Uri.UriSchemeHttps, uri!.Scheme);
        Assert.Equal(AdminMailPagePaths.Host, uri.Host);
        Assert.True(uri.IsDefaultPort);
        Assert.True(string.IsNullOrEmpty(uri.UserInfo));
        Assert.Equal(
            expectReset ? AdminMailPagePaths.ResetConfirmPath : AdminMailPagePaths.BootstrapPath,
            uri.AbsolutePath);
        Assert.Equal(AdminMailPagePaths.Origin, uri.GetLeftPart(UriPartial.Authority));
        Assert.True(string.IsNullOrEmpty(uri.Query));
        Assert.Equal(token, Uri.UnescapeDataString(TokenPlacementValue(uri)));
        return uri;
    }

    private static string ExtractMailLink(string body)
    {
        Assert.Contains(AdminMailPagePaths.Origin, body, StringComparison.Ordinal);
        var start = body.IndexOf(AdminMailPagePaths.Origin, StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = body.IndexOfAny(['\r', '\n'], start);
        return end < 0 ? body[start..] : body[start..end];
    }

    private static void AssertLinkHasNoQueryAndOneTokenFragment(string url)
    {
        Assert.DoesNotContain('?', url);
        Assert.Equal(1, CountOccurrences(url, "#token="));
    }

    private static int CountOccurrences(string text, string marker)
    {
        var count = 0;
        for (var i = 0; (i = text.IndexOf(marker, i, StringComparison.Ordinal)) >= 0; i += marker.Length)
            count++;
        return count;
    }

    private static string ExpectedLink(MailLinkKind kind, string token)
        => AdminMailPagePaths.Origin + AdminMailPagePaths.PathFor(kind) + "#token=" + Uri.EscapeDataString(token);

    private static string TokenPlacementValue(Uri uri)
    {
        var fragment = uri.Fragment.TrimStart('#');
        const string prefix = "token=";
        Assert.StartsWith(prefix, fragment);
        Assert.True(string.IsNullOrEmpty(uri.Query));
        return fragment[prefix.Length..];
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dealoware.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Dealoware.sln");
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = string.Empty;
        public string Authorization { get; private set; } = string.Empty;
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Authorization = request.Headers.TryGetValues("Authorization", out var values)
                ? string.Join(",", values)
                : string.Empty;
            return new HttpResponseMessage(Status)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }
    }
}
