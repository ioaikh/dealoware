using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Dealoware.Domain.Admin;
using Microsoft.Extensions.Options;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Server-side Turnstile siteverify. Fail closed when the secret is missing,
/// the provider is unreachable, or the token is missing/invalid/expired.
/// The Turnstile token is never written to logs or audit.
/// </summary>
public sealed class CloudflareTurnstileVerifier : ITurnstileVerifier
{
    public const string SiteverifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private readonly HttpClient _http;
    private readonly TurnstileOptions _options;

    public CloudflareTurnstileVerifier(HttpClient http, IOptions<TurnstileOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<TurnstileVerifyResult> VerifyAsync(
        string? token,
        string? remoteIp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_options.Secret))
        {
            return new TurnstileVerifyResult(false);
        }

        try
        {
            using var content = new FormUrlEncodedContent(BuildFields(token, remoteIp, _options.Secret));
            using var response = await _http.PostAsync(SiteverifyUrl, content, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new TurnstileVerifyResult(false);
            }

            var body = await response.Content
                .ReadFromJsonAsync<SiteverifyResponse>(cancellationToken)
                .ConfigureAwait(false);
            return new TurnstileVerifyResult(body?.Success == true);
        }
        catch (Exception)
        {
            return new TurnstileVerifyResult(false);
        }
    }

    private static List<KeyValuePair<string, string>> BuildFields(string token, string? remoteIp, string secret)
    {
        var fields = new List<KeyValuePair<string, string>>
        {
            new("secret", secret),
            new("response", token)
        };
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            fields.Add(new("remoteip", remoteIp));
        }

        return fields;
    }

    private sealed class SiteverifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
