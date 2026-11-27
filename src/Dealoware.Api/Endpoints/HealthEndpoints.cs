using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Endpoints;

/// <summary>
/// The two health answers. Both are answered signed out, on the engine's address.
/// The admin address answers <see cref="LivePath"/> only, as before
/// (<see cref="Admin.AdminHostMiddleware"/>).
/// <list type="bullet">
/// <item><see cref="LivePath"/>: the process is up. It does not touch the database, so a
/// database fault does not make the load balancer restart every copy in a loop.</item>
/// <item><see cref="ReadyPath"/>: the process, plus <c>SELECT 1</c> on the database within
/// <see cref="ReadyLimit"/>. 200 when it answers, 503 when it does not.</item>
/// </list>
/// Each answer is <c>{"status":...,"version":...}</c> and nothing else: no host, no error
/// text, no timing. Each carries <c>Cache-Control: no-store</c>.
/// </summary>
public static class HealthEndpoints
{
    public const string LivePath = "/health";
    public const string ReadyPath = "/health/ready";

    public const string StatusOk = "ok";
    public const string StatusUnavailable = "unavailable";

    public static readonly TimeSpan ReadyLimit = TimeSpan.FromSeconds(2);

    public sealed record HealthAnswer(string Status, string Version);

    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet(LivePath, (HttpContext http) =>
            {
                NoStore(http);
                return Results.Ok(new HealthAnswer(StatusOk, ReleaseVersion.Short));
            })
            .WithName("Health")
            .WithTags("Health")
            .Produces<HealthAnswer>(StatusCodes.Status200OK);

        app.MapGet(ReadyPath, ReadyAsync)
            .WithName("HealthReady")
            .WithTags("Health")
            .Produces<HealthAnswer>(StatusCodes.Status200OK)
            .Produces<HealthAnswer>(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> ReadyAsync(
        HttpContext http,
        DealowareDbContext db,
        ILoggerFactory loggers)
    {
        NoStore(http);
        if (await DatabaseAnswersAsync(db, http.RequestAborted, loggers))
        {
            return Results.Ok(new HealthAnswer(StatusOk, ReleaseVersion.Short));
        }

        return Results.Json(
            new HealthAnswer(StatusUnavailable, ReleaseVersion.Short),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>
    /// True only if <c>SELECT 1</c> completes within <see cref="ReadyLimit"/>. The limit is
    /// held twice: the query is cancelled at the limit, and the answer does not wait for a
    /// driver that ignores the cancellation.
    /// </summary>
    private static async Task<bool> DatabaseAnswersAsync(
        DealowareDbContext db,
        CancellationToken requestAborted,
        ILoggerFactory loggers)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        limit.CancelAfter(ReadyLimit);

        var query = db.Database.ExecuteSqlRawAsync("SELECT 1", limit.Token);
        var finished = await Task.WhenAny(query, Task.Delay(ReadyLimit, requestAborted)).ConfigureAwait(false);
        if (finished != query)
        {
            // Observe the abandoned query so its later failure is not an unobserved exception.
            _ = query.ContinueWith(t => _ = t.Exception, TaskScheduler.Default);
            Log(loggers, "timeout");
            return false;
        }

        try
        {
            await query.ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !requestAborted.IsCancellationRequested)
        {
            // The kind of fault only: an exception message can name the database host.
            Log(loggers, ex.GetType().Name);
            return false;
        }
    }

    private static void Log(ILoggerFactory loggers, string reasonClass)
        => loggers.CreateLogger(typeof(HealthEndpoints).FullName!)
            .LogWarning("Readiness check failed: {ReasonClass}", reasonClass);

    private static void NoStore(HttpContext http)
        => http.Response.Headers.CacheControl = "no-store";
}
