using System.Data.Common;
using System.Net;
using System.Text.Json;
using Dealoware.Api.Endpoints;
using Dealoware.Infrastructure.Admin;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dealoware.Api.Tests;

/// <summary>
/// The two health answers (T09 spec §3.1): <c>/health</c> (the process is up) and
/// <c>/health/ready</c> (and the database answers <c>SELECT 1</c> within 2 seconds).
/// Both answer signed out, with the release's short commit hash, no other detail, and
/// <c>Cache-Control: no-store</c>. The admin address keeps answering <c>/health</c> only.
/// <para>
/// Runs on SQLite, and in CI's PostgreSQL job too (.github/workflows/ci.yml), where the
/// fixture's database is a real PostgreSQL 16.
/// </para>
/// </summary>
public class HealthEndpointTests : IClassFixture<HealthWebApplicationFactory>
{
    private const string EngineHost = "api.core.dealoware.com";

    private readonly HealthWebApplicationFactory _factory;

    public HealthEndpointTests(HealthWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Database.Behaviour = DatabaseSwitch.Mode.Works;
    }

    [Fact]
    public async Task Health_ReturnsOkWithStatusOk()
    {
        var (status, body, _) = await GetAsync(HealthEndpoints.LivePath);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Ready_WithTheDatabase_Answers200_StatusOk()
    {
        var (status, body, _) = await GetAsync(HealthEndpoints.ReadyPath);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Ready_WithoutTheDatabase_Answers503_StatusUnavailable()
    {
        _factory.Database.Behaviour = DatabaseSwitch.Mode.Refuses;

        var (status, body, raw) = await GetAsync(HealthEndpoints.ReadyPath);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
        Assert.Equal("unavailable", body.GetProperty("status").GetString());
        AssertOnlyStatusAndVersion(body);
        // No fault detail: not the exception's words, not its kind, not the database.
        Assert.DoesNotContain(DatabaseSwitch.RefusalText, raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sqlite", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("npgsql", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("host", raw, StringComparison.OrdinalIgnoreCase);

        // The process itself is still up: /health does not touch the database.
        var (liveStatus, _, _) = await GetAsync(HealthEndpoints.LivePath);
        Assert.Equal(HttpStatusCode.OK, liveStatus);
    }

    [Fact]
    public async Task Ready_WhenTheDatabaseDoesNotAnswer_Answers503_AtTheLimit()
    {
        // The connection never opens and ignores the cancellation: the answer must not wait for it.
        _factory.Database.Behaviour = DatabaseSwitch.Mode.Hangs;
        try
        {
            using var client = _factory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60); // a failure is reported, not hung
            var (status, body, _) = await GetAsync(HealthEndpoints.ReadyPath, client);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
            Assert.Equal("unavailable", body.GetProperty("status").GetString());
            AssertOnlyStatusAndVersion(body);
        }
        finally
        {
            _factory.Database.ReleaseHung();
        }
    }

    [Fact]
    public async Task Ready_AfterTheDatabaseComesBack_Answers200_Again()
    {
        _factory.Database.Behaviour = DatabaseSwitch.Mode.Refuses;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await GetAsync(HealthEndpoints.ReadyPath)).Status);

        _factory.Database.Behaviour = DatabaseSwitch.Mode.Works;
        Assert.Equal(HttpStatusCode.OK, (await GetAsync(HealthEndpoints.ReadyPath)).Status);
    }

    [Theory]
    [InlineData(HealthEndpoints.LivePath)]
    [InlineData(HealthEndpoints.ReadyPath)]
    public async Task BothAnswers_CarryTheVersion_AndNothingElse(string path)
    {
        var (_, body, _) = await GetAsync(path);

        AssertOnlyStatusAndVersion(body);
        var version = body.GetProperty("version").GetString();
        Assert.Equal(ReleaseVersion.Short, version);
        Assert.Matches("^(dev|[0-9a-f]{7})$", version);
    }

    [Fact]
    public async Task BothAnswers_GiveTheSameVersion()
    {
        var (_, live, _) = await GetAsync(HealthEndpoints.LivePath);
        var (_, ready, _) = await GetAsync(HealthEndpoints.ReadyPath);

        Assert.Equal(live.GetProperty("version").GetString(), ready.GetProperty("version").GetString());
    }

    [Theory]
    [InlineData(HealthEndpoints.LivePath, DatabaseSwitch.Mode.Works)]
    [InlineData(HealthEndpoints.ReadyPath, DatabaseSwitch.Mode.Works)]
    [InlineData(HealthEndpoints.ReadyPath, DatabaseSwitch.Mode.Refuses)]
    public async Task EveryAnswer_IsNoStore(string path, DatabaseSwitch.Mode mode)
    {
        _factory.Database.Behaviour = mode;
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = EngineHost;

        using var response = await client.SendAsync(request);

        Assert.True(response.Headers.CacheControl?.NoStore, $"{path} ({mode}): Cache-Control is '{response.Headers.CacheControl}'");
    }

    [Theory]
    [InlineData(HealthEndpoints.LivePath)]
    [InlineData(HealthEndpoints.ReadyPath)]
    public async Task BothAnswers_NeedNoSignIn_OnTheEnginesAddress(string path)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = EngineHost;
        Assert.Null(request.Headers.Authorization);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TheAdminAddress_KeepsItsList_HealthYes_ReadyNo()
    {
        using var client = _factory.CreateClient();

        using var live = new HttpRequestMessage(HttpMethod.Get, HealthEndpoints.LivePath);
        live.Headers.Host = AdminHostOptions.ProductionAdminHost;
        using var liveResponse = await client.SendAsync(live);
        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);

        using var ready = new HttpRequestMessage(HttpMethod.Get, HealthEndpoints.ReadyPath);
        ready.Headers.Host = AdminHostOptions.ProductionAdminHost;
        using var readyResponse = await client.SendAsync(ready);
        Assert.Equal(HttpStatusCode.NotFound, readyResponse.StatusCode);
        Assert.Equal(string.Empty, await readyResponse.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef01234567", "0123456")]
    [InlineData("0123456789ABCDEF0123456789ABCDEF01234567", "0123456")]
    [InlineData("  abcdef1  ", "abcdef1")]
    [InlineData("abcdef1", "abcdef1")]
    [InlineData("dev", "dev")]
    [InlineData("", "dev")]
    [InlineData(null, "dev")]
    [InlineData("abcdef", "dev")]
    [InlineData("0123456789abcdef0123456789abcdef012345678", "dev")]
    [InlineData("not-a-hash", "dev")]
    [InlineData("abcdefg", "dev")]
    [InlineData("<script>1234567", "dev")]
    public void TheVersion_IsTheFirstSevenOfAHexCommit_OrDev(string? built, string answered)
    {
        Assert.Equal(answered, ReleaseVersion.FromBuildValue(built));
    }

    private static void AssertOnlyStatusAndVersion(JsonElement body)
    {
        var names = body.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(["status", "version"], names);
    }

    private Task<(HttpStatusCode Status, JsonElement Body, string Raw)> GetAsync(string path)
        => GetAsync(path, null);

    private async Task<(HttpStatusCode Status, JsonElement Body, string Raw)> GetAsync(string path, HttpClient? given)
    {
        var client = given ?? _factory.CreateClient();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Host = EngineHost;
            using var response = await client.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(raw);
            return (response.StatusCode, doc.RootElement.Clone(), raw);
        }
        finally
        {
            if (given is null)
            {
                client.Dispose();
            }
        }
    }
}

/// <summary>
/// The health tests' own host, on its own PostgreSQL database in the CI job, with a switch
/// that makes every database connection work, be refused, or never open.
/// </summary>
public sealed class HealthWebApplicationFactory : IsolatedWebApplicationFactory
{
    public DatabaseSwitch Database { get; } = new();

    protected override string? PostgresDatabaseName => "dealoware_ci_health";

    protected override IEnumerable<IInterceptor> ExtraDbInterceptors => [Database];
}

/// <summary>Stands between EF and the database's connections. Works by default.</summary>
public sealed class DatabaseSwitch : DbConnectionInterceptor
{
    public const string RefusalText = "test switch: the database refused the connection";

    public enum Mode
    {
        Works,
        Refuses,
        Hangs
    }

    private CancellationTokenSource _hung = new();

    public volatile Mode Behaviour = Mode.Works;

    public void ReleaseHung()
    {
        var old = Interlocked.Exchange(ref _hung, new CancellationTokenSource());
        old.Cancel();
        old.Dispose();
    }

    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        if (Behaviour == Mode.Refuses)
        {
            throw new InvalidOperationException(RefusalText);
        }

        if (Behaviour == Mode.Hangs)
        {
            _hung.Token.WaitHandle.WaitOne();
            throw new InvalidOperationException(RefusalText);
        }

        return result;
    }

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        if (Behaviour == Mode.Refuses)
        {
            throw new InvalidOperationException(RefusalText);
        }

        if (Behaviour == Mode.Hangs)
        {
            // Ignores the caller's cancellation on purpose: a driver that does not give up.
            try
            {
                await Task.Delay(Timeout.Infinite, _hung.Token);
            }
            catch (OperationCanceledException)
            {
            }

            throw new InvalidOperationException(RefusalText);
        }

        return result;
    }
}
