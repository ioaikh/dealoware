using System.Diagnostics;

namespace Dealoware.Api.Observability;

/// <summary>
/// The request ID (T09 spec §4.1). Every request has one: the trace ID of a valid incoming W3C
/// <c>traceparent</c> header, or a new one. It is returned in the reply's <c>traceparent</c>
/// header, and <see cref="JsonLineConsoleFormatter"/> writes it as <c>TraceId</c> on every log
/// line written while the request runs.
/// <para>
/// The ASP.NET Core host already starts an <see cref="Activity"/> for each request and takes its
/// parent from a valid <c>traceparent</c> (an invalid one is ignored, so a new ID is made). This
/// middleware only makes sure an activity exists, and answers with its IDs. It runs first, so a
/// request refused by a later gate still gets its ID.
/// </para>
/// <para>
/// The header is set when the answer starts (<c>Response.OnStarting</c>), so it survives a later
/// <c>Response.Clear()</c>, such as the admin host's own 500. Outside Development an exception that
/// reaches this middleware (the engine host has no other handler; the admin host catches its own)
/// becomes a bare 500 with no body, so the ID is still returned and nothing of the fault is shown.
/// Two exceptions are not faults of the engine, and must not become a 500 or an Error line (any
/// client could otherwise raise the server-error and log-error alarms): a bad request
/// (<see cref="BadHttpRequestException"/>, such as a body over the limit) is answered with its own
/// status code (413, 400), no body, and the ID, which the server's own answer would drop; a request
/// the client gave up on (the cancellation of <c>RequestAborted</c>) goes on to the server, which
/// logs nothing for it, as before.
/// The exception is logged (its type and stack only: see <see cref="JsonLineConsoleFormatter"/>).
/// In Development the developer exception page keeps it.
/// </para>
/// </summary>
public sealed class RequestIdMiddleware
{
    public const string HeaderName = "traceparent";

    private const string FallbackActivityName = "Dealoware.Request";

    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IHostEnvironment environment, ILogger<RequestIdMiddleware> logger)
    {
        Activity? own = null;
        if (Activity.Current is null)
        {
            // No host activity (no listener at all): make one, with the same parent rule.
            own = new Activity(FallbackActivityName);
            var incoming = context.Request.Headers[HeaderName].ToString();
            if (!string.IsNullOrEmpty(incoming)
                && ActivityContext.TryParse(incoming, traceState: null, isRemote: true, out var parent))
            {
                own.SetParentId(parent.TraceId, parent.SpanId, parent.TraceFlags);
            }

            own.SetIdFormat(ActivityIdFormat.W3C);
            own.Start();
        }

        try
        {
            var value = Format(Activity.Current!);
            context.Response.OnStarting(static state =>
            {
                var (http, header) = ((HttpContext, string))state;
                http.Response.Headers[HeaderName] = header;
                return Task.CompletedTask;
            }, (context, value));

            try
            {
                await _next(context);
            }
            catch (BadHttpRequestException bad) when (!environment.IsDevelopment() && !context.Response.HasStarted)
            {
                logger.LogDebug("Bad request: answered {StatusCode}", bad.StatusCode);
                context.Response.Clear();
                context.Response.StatusCode = bad.StatusCode;
            }
            catch (Exception ex) when (!environment.IsDevelopment() && !context.Response.HasStarted && !ClientGaveUp(ex, context))
            {
                logger.LogError(ex, "Unhandled exception: answered 500 with no detail");
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }
        finally
        {
            own?.Stop();
        }
    }

    /// <summary>The client gave up on the request: nobody is there to answer, and it is no fault.</summary>
    private static bool ClientGaveUp(Exception ex, HttpContext context)
        => ex is OperationCanceledException && context.RequestAborted.IsCancellationRequested;

    /// <summary>The current request's ID, or null outside a request.</summary>
    public static string? CurrentTraceId()
    {
        var activity = Activity.Current;
        if (activity is null || activity.IdFormat != ActivityIdFormat.W3C)
        {
            return null;
        }

        return activity.TraceId.ToHexString();
    }

    private static string Format(Activity activity)
    {
        var flags = (activity.ActivityTraceFlags & ActivityTraceFlags.Recorded) != 0 ? "01" : "00";
        return $"00-{activity.TraceId.ToHexString()}-{activity.SpanId.ToHexString()}-{flags}";
    }
}
