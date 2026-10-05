using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

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
    /// Masks an IP address for logging: zeroes the last octet of IPv4, or last 80 bits of IPv6.
    /// Returns "unknown" if the input is null/empty.
    /// </summary>
    public static string MaskIp(string? ip)
    {
        if (string.IsNullOrEmpty(ip) || ip == "unknown")
            return "unknown";
        
        // IPv4: mask last octet
        var lastDot = ip.LastIndexOf('.');
        if (lastDot > 0)
            return ip[..lastDot] + ".0";
        
        // IPv6: mask last 80 bits (5 groups)
        var colonCount = ip.Count(c => c == ':');
        if (colonCount >= 2)
            return ip.Split(':').Take(3).Aggregate((a, b) => a + ":" + b) + "::0";
        
        return "masked";
    }

    /// <summary>
    /// Builds the OnRejected callback: 429 with a Retry-After header (whole seconds until the
    /// fixed window resets) and a small JSON body. Logs the rejection at Information level.
    /// </summary>
    /// <param name="fallbackRetryAfterSeconds">Used only if the limiter lease carries no retry-after metadata.</param>
    public static Func<OnRejectedContext, CancellationToken, ValueTask> CreateOnRejected(int fallbackRetryAfterSeconds) =>
        async (context, cancellationToken) =>
        {
            var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? (int)Math.Ceiling(retryAfter.TotalSeconds)
                : fallbackRetryAfterSeconds;
            retryAfterSeconds = Math.Max(1, retryAfterSeconds);

            var httpContext = context.HttpContext;
            var logger = httpContext.RequestServices.GetService<ILoggerFactory>()
                ?.CreateLogger("Dealoware.Api.RateLimiting");
            
            var partitionKey = GetPartitionKey(httpContext);
            var xff = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            
            logger?.LogInformation(
                "Rate limit rejected: Path={Path}, Policy={Policy}, PartitionKey={PartitionKey}, " +
                "XFF={XffPresent}, RetryAfter={RetryAfter}s",
                httpContext.Request.Path,
                context.Lease.TryGetMetadata(MetadataName.ReasonPhrase, out var reason) ? reason : "unknown",
                MaskIp(partitionKey),
                xff is not null,
                retryAfterSeconds);

            var response = httpContext.Response;
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
