using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Dealoware.Api.RateLimiting;

/// <summary>
/// Partitioning and rejection behavior shared by the auth rate-limit policies.
/// </summary>
public static class AuthRateLimiting
{
    /// <summary>Error code returned in the JSON body of a 429 response.</summary>
    public const string RateLimitedErrorCode = "rate_limited";

    /// <summary>
    /// Partition key: the client IP. UseForwardedHeaders runs before UseRateLimiter, so behind the
    /// load balancer RemoteIpAddress is already the client address taken from X-Forwarded-For,
    /// not the load balancer hop.
    /// </summary>
    public static string GetPartitionKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>
    /// Builds the OnRejected callback: 429 with a Retry-After header (whole seconds until the
    /// fixed window resets) and a small JSON body.
    /// </summary>
    /// <param name="fallbackRetryAfterSeconds">Used only if the limiter lease carries no retry-after metadata.</param>
    public static Func<OnRejectedContext, CancellationToken, ValueTask> CreateOnRejected(int fallbackRetryAfterSeconds) =>
        async (context, cancellationToken) =>
        {
            var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? (int)Math.Ceiling(retryAfter.TotalSeconds)
                : fallbackRetryAfterSeconds;
            retryAfterSeconds = Math.Max(1, retryAfterSeconds);

            var response = context.HttpContext.Response;
            response.StatusCode = StatusCodes.Status429TooManyRequests;
            response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

            await response.WriteAsJsonAsync(
                new RateLimitRejectedResponse(
                    RateLimitedErrorCode,
                    "Too many requests. Retry after the number of seconds in the Retry-After header.",
                    retryAfterSeconds),
                cancellationToken);
        };
}

/// <summary>JSON body of a 429 response from the auth rate limiter.</summary>
public sealed record RateLimitRejectedResponse(string Error, string Message, int RetryAfterSeconds);
