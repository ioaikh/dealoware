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
/// </summary>
public sealed class RequestIdMiddleware
{
    public const string HeaderName = "traceparent";

    private const string FallbackActivityName = "Dealoware.Request";

    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
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
            var activity = Activity.Current!;
            context.Response.Headers[HeaderName] = Format(activity);
            await _next(context);
        }
        finally
        {
            own?.Stop();
        }
    }

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
