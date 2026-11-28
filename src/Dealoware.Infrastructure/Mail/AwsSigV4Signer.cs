using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Dealoware.Infrastructure.Mail;

/// <summary>
/// AWS Signature Version 4 for the SES HTTP adapter. Protocol only — no account or region defaults.
/// Temporary (role) credentials add their session token as <c>x-amz-security-token</c>, which is
/// sent and signed.
/// </summary>
internal static class AwsSigV4Signer
{
    public const string SessionTokenHeader = "x-amz-security-token";

    public static void SignPost(
        HttpRequestMessage request,
        ReadOnlySpan<byte> payload,
        AwsCredentials credentials,
        string region,
        string service,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        ArgumentException.ThrowIfNullOrWhiteSpace(service);

        var amzDate = AmzDate(nowUtc);
        var host = request.RequestUri?.Host ?? throw new InvalidOperationException("Request URI is required.");
        var path = string.IsNullOrEmpty(request.RequestUri.AbsolutePath) ? "/" : request.RequestUri.AbsolutePath;
        var payloadHash = HexLower(SHA256.HashData(payload));

        var signedHeaders = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["content-type"] = "application/json",
            ["host"] = host,
            ["x-amz-content-sha256"] = payloadHash,
            ["x-amz-date"] = amzDate
        };

        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        if (credentials.SessionToken is { } sessionToken)
        {
            signedHeaders[SessionTokenHeader] = sessionToken;
            request.Headers.TryAddWithoutValidation(SessionTokenHeader, sessionToken);
        }

        request.Headers.Host = host;

        var authorization = BuildAuthorization(
            "POST",
            path,
            canonicalQuery: string.Empty,
            signedHeaders,
            payloadHash,
            credentials,
            region,
            service,
            nowUtc);
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
    }

    /// <summary>
    /// The <c>Authorization</c> value for a request whose signed headers are given with lower-case
    /// names and trimmed values. Separate from <see cref="SignPost"/> so it can be checked against
    /// AWS's published test suite.
    /// </summary>
    internal static string BuildAuthorization(
        string method,
        string canonicalUri,
        string canonicalQuery,
        IReadOnlyDictionary<string, string> headers,
        string payloadHash,
        AwsCredentials credentials,
        string region,
        string service,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(credentials);

        var amzDate = AmzDate(nowUtc);
        var dateStamp = nowUtc.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var ordered = headers.OrderBy(h => h.Key, StringComparer.Ordinal).ToList();
        var canonicalHeaders = string.Concat(ordered.Select(h => h.Key + ":" + h.Value + "\n"));
        var signedHeaders = string.Join(";", ordered.Select(h => h.Key));

        var canonicalRequest =
            method + "\n"
            + canonicalUri + "\n"
            + canonicalQuery + "\n"
            + canonicalHeaders + "\n"
            + signedHeaders + "\n"
            + payloadHash;

        var credentialScope = dateStamp + "/" + region + "/" + service + "/aws4_request";
        var stringToSign =
            "AWS4-HMAC-SHA256\n"
            + amzDate + "\n"
            + credentialScope + "\n"
            + HexLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));

        var signingKey = GetSigningKey(credentials.SecretAccessKey, dateStamp, region, service);
        var signature = HexLower(HmacSha256(signingKey, stringToSign));
        return "AWS4-HMAC-SHA256 Credential=" + credentials.AccessKeyId + "/" + credentialScope
            + ", SignedHeaders=" + signedHeaders
            + ", Signature=" + signature;
    }

    private static string AmzDate(DateTimeOffset nowUtc)
        => nowUtc.UtcDateTime.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);

    private static byte[] GetSigningKey(string secret, string dateStamp, string region, string service)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + secret), dateStamp);
        var kRegion = HmacSha256(kDate, region);
        var kService = HmacSha256(kRegion, service);
        return HmacSha256(kService, "aws4_request");
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string HexLower(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
}
