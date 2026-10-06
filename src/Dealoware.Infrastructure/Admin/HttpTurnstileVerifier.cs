using System.Text.Json;
using Dealoware.Domain.Admin;
using Microsoft.Extensions.Options;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// HTTP Turnstile siteverify. Fail-closed on missing secret, non-success, or transport errors.
/// Secret is read from <see cref="TurnstileOptions.SecretEnvironmentVariable"/> only.
/// </summary>
public sealed class HttpTurnstileVerifier : ITurnstileVerifier
{
    public const string HttpClientName = "Turnstile";

    private readonly HttpClient _httpClient;
    private readonly TurnstileOptions _options;
    private readonly string? _secret;

    public HttpTurnstileVerifier(
        HttpClient httpClient,
        IOptions<TurnstileOptions> options,
        Func<string, string?> getEnv)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _secret = getEnv(TurnstileOptions.SecretEnvironmentVariable);
    }

    public async Task<bool> VerifyAsync(string token, string? remoteIp, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_secret))
        {
            return false;
        }

        try
        {
            using var content = new FormUrlEncodedContent(BuildFields(token, remoteIp));
            using var response = await _httpClient.PostAsync(_options.VerifyUrl, content, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            return doc.RootElement.TryGetProperty("success", out var success)
                   && success.ValueKind == JsonValueKind.True;
        }
        catch
        {
            return false;
        }
    }

    private IEnumerable<KeyValuePair<string, string>> BuildFields(string token, string? remoteIp)
    {
        yield return new KeyValuePair<string, string>("secret", _secret!);
        yield return new KeyValuePair<string, string>("response", token);
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            yield return new KeyValuePair<string, string>("remoteip", remoteIp);
        }
    }
}
