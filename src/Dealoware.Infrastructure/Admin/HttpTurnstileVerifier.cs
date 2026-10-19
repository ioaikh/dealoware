using System.Text.Json;
using Dealoware.Domain.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// HTTP Turnstile siteverify. Fail-closed on missing secret, non-success, or transport errors.
/// Secret is read from <see cref="TurnstileOptions.SecretEnvironmentVariable"/> only.
/// On fail-closed, logs Cloudflare's error-codes array only — never the secret, token, or remote IP.
/// </summary>
public sealed class HttpTurnstileVerifier : ITurnstileVerifier
{
    public const string HttpClientName = "Turnstile";
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
    public const string FailClosedLogMessage = "Turnstile siteverify failed closed. error-codes={ErrorCodes}";

    private readonly HttpClient _httpClient;
    private readonly TurnstileOptions _options;
    private readonly ILogger<HttpTurnstileVerifier> _logger;
    private readonly string? _secret;

    public HttpTurnstileVerifier(
        HttpClient httpClient,
        IOptions<TurnstileOptions> options,
        Func<string, string?> getEnv,
        ILogger<HttpTurnstileVerifier> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
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
            if (!doc.RootElement.TryGetProperty("success", out var success)
                || success.ValueKind != JsonValueKind.True)
            {
                LogErrorCodesOnly(doc.RootElement);
                return false;
            }

            if (!HostnameMatches(doc.RootElement) || !ActionMatches(doc.RootElement))
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void LogErrorCodesOnly(JsonElement root)
    {
        var codes = new List<string>();
        if (root.TryGetProperty("error-codes", out var array)
            && array.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var code = item.GetString();
                    if (!string.IsNullOrEmpty(code))
                    {
                        codes.Add(code);
                    }
                }
            }
        }

        _logger.LogWarning(FailClosedLogMessage, string.Join(",", codes));
    }

    private bool HostnameMatches(JsonElement root)
    {
        var expected = _options.ExpectedHostname;
        if (string.IsNullOrWhiteSpace(expected))
        {
            expected = AdminHostOptions.ProductionAdminHost;
        }

        return root.TryGetProperty("hostname", out var hostname)
            && hostname.ValueKind == JsonValueKind.String
            && string.Equals(hostname.GetString(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private bool ActionMatches(JsonElement root)
    {
        var expected = _options.ExpectedAction;
        if (string.IsNullOrWhiteSpace(expected))
        {
            return true;
        }

        return root.TryGetProperty("action", out var action)
            && action.ValueKind == JsonValueKind.String
            && string.Equals(action.GetString(), expected, StringComparison.Ordinal);
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
