using System.Text;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Dealoware.Api.Tests;

/// <summary>
/// Every environment except Development (strict mode) must refuse to start with a missing, blank,
/// placeholder, or too-short JWT signing key. Development keeps the placeholder fallback.
/// </summary>
public class JwtSigningKeyValidatorTests
{
    // Test-only values; not real secrets.
    private static readonly string Key31Bytes = new('k', 31);
    private static readonly string Key32Bytes = new('k', 32);
    private static readonly string Key64Bytes = new('z', 64);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Production_MissingOrBlankKey_Throws(string? key)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => JwtSigningKeyValidator.Validate(key, requireStrictKey: true));

        Assert.Contains(JwtSigningKeyValidator.EnvironmentVariableName, ex.Message);
        Assert.Contains("not set", ex.Message);
    }

    [Theory]
    [InlineData(JwtSigningKeyValidator.DevelopmentPlaceholderKey)]
    [InlineData(" " + JwtSigningKeyValidator.DevelopmentPlaceholderKey + " ")]
    [InlineData("development_placeholder_key_change_in_production_32chars")]
    public void Production_PlaceholderKey_Throws_WithoutLeakingKey(string key)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => JwtSigningKeyValidator.Validate(key, requireStrictKey: true));

        Assert.Contains("placeholder", ex.Message);
        Assert.DoesNotContain(JwtSigningKeyValidator.DevelopmentPlaceholderKey, ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CHANGE_IN_PRODUCTION", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_31ByteKey_Throws_WithoutLeakingKey()
    {
        Assert.Equal(31, Encoding.UTF8.GetByteCount(Key31Bytes));

        var ex = Assert.Throws<InvalidOperationException>(() => JwtSigningKeyValidator.Validate(Key31Bytes, requireStrictKey: true));

        Assert.Contains("too short", ex.Message);
        Assert.DoesNotContain("kkkk", ex.Message);
    }

    [Fact]
    public void Production_KeyShortInBytes_CountsUtf8Bytes()
    {
        // 15 two-byte characters = 30 bytes even though 15 chars; 16 = 32 bytes.
        var thirtyBytes = new string('é', 15);
        var thirtyTwoBytes = new string('é', 16);

        Assert.Throws<InvalidOperationException>(() => JwtSigningKeyValidator.Validate(thirtyBytes, requireStrictKey: true));
        Assert.Equal(thirtyTwoBytes, JwtSigningKeyValidator.Validate(thirtyTwoBytes, requireStrictKey: true));
    }

    [Fact]
    public void Production_32ByteKey_Passes()
    {
        Assert.Equal(Key32Bytes, JwtSigningKeyValidator.Validate(Key32Bytes, requireStrictKey: true));
    }

    [Fact]
    public void Production_LongKey_Passes()
    {
        Assert.Equal(Key64Bytes, JwtSigningKeyValidator.Validate(Key64Bytes, requireStrictKey: true));
    }

    [Fact]
    public void Development_MissingKey_FallsBackToPlaceholder()
    {
        Assert.Equal(
            JwtSigningKeyValidator.DevelopmentPlaceholderKey,
            JwtSigningKeyValidator.Validate(null, requireStrictKey: false));
    }

    [Theory]
    [InlineData(JwtSigningKeyValidator.DevelopmentPlaceholderKey)]
    [InlineData("short")]
    [InlineData("")]
    public void Development_KeepsProvidedValueUnchanged(string key)
    {
        Assert.Equal(key, JwtSigningKeyValidator.Validate(key, requireStrictKey: false));
    }

    [Theory]
    [InlineData("Staging", true)]
    [InlineData("Production", true)]
    [InlineData("Testing", true)]
    [InlineData("SomeCustomEnvironment", true)]
    [InlineData("Development", false)]
    [InlineData("development", false)]
    public void StrictMode_IsEveryEnvironmentExceptDevelopment(string environmentName, bool expectedStrict)
    {
        IHostEnvironment env = new HostingEnvironment { EnvironmentName = environmentName };

        Assert.Equal(expectedStrict, !env.IsDevelopment());
    }

    [Fact]
    public void Staging_MissingKey_Throws()
    {
        IHostEnvironment staging = new HostingEnvironment { EnvironmentName = "Staging" };

        var ex = Assert.Throws<InvalidOperationException>(
            () => JwtSigningKeyValidator.Validate(null, requireStrictKey: !staging.IsDevelopment()));

        Assert.Contains(JwtSigningKeyValidator.EnvironmentVariableName, ex.Message);
        Assert.Contains("not set", ex.Message);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Host_NonDevelopment_WithoutSigningKey_FailsAtStartup(string environment)
    {
        using var factory = new EnvironmentWebApplicationFactory(environment, signingKey: null);

        var ex = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(ex);
        var messages = ExceptionMessages(ex!);
        Assert.Contains(messages, m => m.Contains(JwtSigningKeyValidator.EnvironmentVariableName) && m.Contains("not set"));
    }

    [Fact]
    public void Host_Production_WithPlaceholderKey_FailsAtStartup_WithoutLeakingKey()
    {
        using var factory = new EnvironmentWebApplicationFactory("Production", JwtSigningKeyValidator.DevelopmentPlaceholderKey);

        var ex = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(ex);
        var messages = ExceptionMessages(ex!);
        Assert.Contains(messages, m => m.Contains("placeholder"));
        Assert.DoesNotContain(messages, m => m.Contains(JwtSigningKeyValidator.DevelopmentPlaceholderKey, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Host_Production_With64CharKey_StartsAndHealthReturns200()
    {
        Assert.Equal(64, EnvironmentWebApplicationFactory.TestSigningKey64.Length);
        Assert.True(EnvironmentWebApplicationFactory.TestSigningKey64.All(char.IsAsciiLetterOrDigit));

        using var factory = new EnvironmentWebApplicationFactory("Production", EnvironmentWebApplicationFactory.TestSigningKey64);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Host_Development_WithoutSigningKey_Starts()
    {
        using var factory = new EnvironmentWebApplicationFactory("Development", signingKey: null);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    private static List<string> ExceptionMessages(Exception ex)
    {
        var messages = new List<string>();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        if (ex is AggregateException aggregate)
        {
            messages.AddRange(aggregate.Flatten().InnerExceptions.Select(e => e.Message));
        }

        return messages;
    }
}
