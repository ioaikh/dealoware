using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Dealoware.Infrastructure.Mail;

/// <summary>
/// AWS Signature Version 4 for the SES HTTP adapter. Protocol only — no account or region defaults.
/// </summary>
internal static class AwsSigV4Signer
{
    public static void SignPost(
        HttpRequestMessage request,
        ReadOnlySpan<byte> payload,
        string accessKeyId,
        string secretAccessKey,
        string region,
        string service,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessKeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretAccessKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        ArgumentException.ThrowIfNullOrWhiteSpace(service);

        var amzDate = nowUtc.UtcDateTime.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var dateStamp = nowUtc.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var host = request.RequestUri?.Host ?? throw new InvalidOperationException("Request URI is required.");
        var path = string.IsNullOrEmpty(request.RequestUri.AbsolutePath) ? "/" : request.RequestUri.AbsolutePath;
        var payloadHash = HexLower(SHA256.HashData(payload));

        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        request.Headers.Host = host;

        const string signedHeaders = "content-type;host;x-amz-content-sha256;x-amz-date";
        var canonicalHeaders =
            "content-type:application/json\n"
            + "host:" + host + "\n"
            + "x-amz-content-sha256:" + payloadHash + "\n"
            + "x-amz-date:" + amzDate + "\n";

        var canonicalRequest =
            "POST\n"
            + path + "\n"
            + "\n"
            + canonicalHeaders + "\n"
            + signedHeaders + "\n"
            + payloadHash;

        var credentialScope = dateStamp + "/" + region + "/" + service + "/aws4_request";
        var stringToSign =
            "AWS4-HMAC-SHA256\n"
            + amzDate + "\n"
            + credentialScope + "\n"
            + HexLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));

        var signingKey = GetSigningKey(secretAccessKey, dateStamp, region, service);
        var signature = HexLower(HmacSha256(signingKey, stringToSign));
        var authorization =
            "AWS4-HMAC-SHA256 Credential=" + accessKeyId + "/" + credentialScope
            + ", SignedHeaders=" + signedHeaders
            + ", Signature=" + signature;

        request.Headers.TryAddWithoutValidation("Authorization", authorization);
    }

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
