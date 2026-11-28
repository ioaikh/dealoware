using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Dealoware.Infrastructure.Mail;

/// <summary>
/// The address of the standard container credentials endpoint, read from the settings the
/// container runtime sets. On ECS, a task with a task role gets
/// <c>AWS_CONTAINER_CREDENTIALS_RELATIVE_URI</c>, a path on the fixed ECS credentials host.
/// Other runtimes set <c>AWS_CONTAINER_CREDENTIALS_FULL_URI</c>, optionally with an
/// authorization token (<c>AWS_CONTAINER_AUTHORIZATION_TOKEN_FILE</c>, else
/// <c>AWS_CONTAINER_AUTHORIZATION_TOKEN</c>). As in the AWS SDKs, the relative address wins
/// when both are set, and a full address over plain HTTP must point at a loopback address or a
/// container credentials host. Neither the address nor the token is ever logged.
/// </summary>
public sealed class ContainerCredentialsEndpoint
{
    public const string RelativeUriEnvironmentVariable = "AWS_CONTAINER_CREDENTIALS_RELATIVE_URI";
    public const string FullUriEnvironmentVariable = "AWS_CONTAINER_CREDENTIALS_FULL_URI";
    public const string AuthorizationTokenEnvironmentVariable = "AWS_CONTAINER_AUTHORIZATION_TOKEN";
    public const string AuthorizationTokenFileEnvironmentVariable = "AWS_CONTAINER_AUTHORIZATION_TOKEN_FILE";

    /// <summary>The ECS container credentials host that a relative address is a path on.</summary>
    public const string EcsCredentialsHost = "169.254.170.2";

    private static readonly IPAddress[] ContainerHosts =
    [
        IPAddress.Parse(EcsCredentialsHost),
        IPAddress.Parse("169.254.170.23"),
        IPAddress.Parse("fd00:ec2::23")
    ];

    private ContainerCredentialsEndpoint(Uri uri, string? authorizationToken, string? authorizationTokenFile)
    {
        Uri = uri;
        AuthorizationToken = authorizationToken;
        AuthorizationTokenFile = authorizationTokenFile;
    }

    public Uri Uri { get; }

    /// <summary>A fixed token, sent as the <c>Authorization</c> header. Not used when a token file is set.</summary>
    public string? AuthorizationToken { get; }

    /// <summary>A file holding the token. Read again on every fetch, so a rotated token is picked up.</summary>
    public string? AuthorizationTokenFile { get; }

    /// <summary>True when either address setting is present, valid or not.</summary>
    public static bool IsConfigured(Func<string, string?> getEnv)
    {
        ArgumentNullException.ThrowIfNull(getEnv);
        return !string.IsNullOrWhiteSpace(getEnv(RelativeUriEnvironmentVariable))
               || !string.IsNullOrWhiteSpace(getEnv(FullUriEnvironmentVariable));
    }

    /// <summary>
    /// The endpoint, or null when no address is set or the address is not one the SDKs accept.
    /// </summary>
    public static ContainerCredentialsEndpoint? TryCreate(Func<string, string?> getEnv)
    {
        ArgumentNullException.ThrowIfNull(getEnv);

        var uri = ResolveUri(getEnv(RelativeUriEnvironmentVariable), getEnv(FullUriEnvironmentVariable));
        if (uri is null)
            return null;

        var tokenFile = getEnv(AuthorizationTokenFileEnvironmentVariable);
        var token = getEnv(AuthorizationTokenEnvironmentVariable);
        return Create(
            uri,
            string.IsNullOrWhiteSpace(token) ? null : token.Trim(),
            string.IsNullOrWhiteSpace(tokenFile) ? null : tokenFile.Trim());
    }

    /// <summary>An endpoint from parts; the token, when given, must be a legal header value.</summary>
    public static ContainerCredentialsEndpoint Create(Uri uri, string? authorizationToken = null, string? authorizationTokenFile = null)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
            throw new ArgumentException("The credentials address must be absolute.", nameof(uri));
        if (authorizationToken is not null && !IsLegalHeaderValue(authorizationToken))
            throw new ArgumentException("The authorization token is not a legal header value.", nameof(authorizationToken));

        return new ContainerCredentialsEndpoint(uri, authorizationToken, authorizationTokenFile);
    }

    /// <summary>
    /// The relative address on the ECS host when set (it wins, as in the SDKs); else the full
    /// address when it is HTTPS, or HTTP to a loopback address or a container credentials host.
    /// </summary>
    public static Uri? ResolveUri(string? relativeUri, string? fullUri)
    {
        if (!string.IsNullOrWhiteSpace(relativeUri))
        {
            var path = relativeUri.Trim();
            if (!path.StartsWith('/') || path.StartsWith("//", StringComparison.Ordinal) || path.Contains('\\'))
                return null;

            if (!Uri.TryCreate("http://" + EcsCredentialsHost + path, UriKind.Absolute, out var relative)
                || !string.Equals(relative.Host, EcsCredentialsHost, StringComparison.Ordinal)
                || !relative.IsDefaultPort
                || !string.IsNullOrEmpty(relative.UserInfo))
            {
                return null;
            }

            return relative;
        }

        if (string.IsNullOrWhiteSpace(fullUri)
            || !Uri.TryCreate(fullUri.Trim(), UriKind.Absolute, out var full)
            || !string.IsNullOrEmpty(full.UserInfo))
        {
            return null;
        }

        if (full.Scheme == Uri.UriSchemeHttps)
            return full;

        return full.Scheme == Uri.UriSchemeHttp && IsAllowedHttpHost(full) ? full : null;
    }

    public override string ToString() => nameof(ContainerCredentialsEndpoint);

    internal static bool IsLegalHeaderValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var c in value)
        {
            if (c is < ' ' or > '~')
                return false;
        }

        return true;
    }

    private static bool IsAllowedHttpHost(Uri uri)
    {
        if (uri.IsLoopback)
            return true;

        if (!IPAddress.TryParse(uri.DnsSafeHost, out var address))
            return false;

        return IPAddress.IsLoopback(address) || ContainerHosts.Any(h => h.Equals(address));
    }
}

/// <summary>
/// The credentials endpoint did not give usable credentials. The message says only what kind
/// of failure it was: never the address, the token, the response body or a credential.
/// </summary>
public sealed class AwsContainerCredentialsException : InvalidOperationException
{
    public AwsContainerCredentialsException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// The running service's own role credentials (on ECS, the task role), from the standard
/// container credentials endpoint. They are temporary and carry a session token. They are kept
/// in memory and fetched again before they expire: once inside
/// <see cref="DefaultRefreshBeforeExpiry"/> of the expiry, the next send fetches new ones, and
/// if that fetch fails while the old ones still work, the old ones are used. Nothing this class
/// reads or sends is logged.
/// </summary>
public sealed class ContainerAwsCredentialSource : IAwsCredentialSource
{
    /// <summary>How long before the expiry the next send fetches new credentials.</summary>
    public static readonly TimeSpan DefaultRefreshBeforeExpiry = TimeSpan.FromMinutes(5);

    /// <summary>The largest credentials answer read; a real one is under 2 KB.</summary>
    internal const int MaxResponseBytes = 64 * 1024;

    private readonly ContainerCredentialsEndpoint _endpoint;
    private readonly HttpClient _httpClient;
    private readonly Func<DateTimeOffset> _clock;
    private readonly TimeSpan _refreshBeforeExpiry;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AwsCredentials? _current;

    public ContainerAwsCredentialSource(
        ContainerCredentialsEndpoint endpoint,
        HttpClient httpClient,
        Func<DateTimeOffset>? clock = null,
        TimeSpan? refreshBeforeExpiry = null)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _refreshBeforeExpiry = refreshBeforeExpiry ?? DefaultRefreshBeforeExpiry;
        if (_refreshBeforeExpiry < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(refreshBeforeExpiry));
    }

    public async ValueTask<AwsCredentials> GetCredentialsAsync(CancellationToken cancellationToken)
    {
        var current = Volatile.Read(ref _current);
        if (current is not null && !NeedsRefresh(current, _clock()))
            return current;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Another send may have fetched them while this one waited.
            current = _current;
            var now = _clock();
            if (current is not null && !NeedsRefresh(current, now))
                return current;

            try
            {
                var fresh = await FetchAsync(now, cancellationToken).ConfigureAwait(false);
                Volatile.Write(ref _current, fresh);
                return fresh;
            }
            catch (AwsContainerCredentialsException) when (current?.Expiration is { } expiry && expiry > now)
            {
                // The early fetch failed, but the credentials in hand still work until they expire.
                // The next send tries again.
                return current;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public override string ToString() => nameof(ContainerAwsCredentialSource);

    private bool NeedsRefresh(AwsCredentials credentials, DateTimeOffset now)
        => credentials.Expiration is not { } expiry || now >= expiry - _refreshBeforeExpiry;

    private async Task<AwsCredentials> FetchAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _endpoint.Uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var token = ReadAuthorizationToken();
        if (token is not null)
            request.Headers.TryAddWithoutValidation("Authorization", token);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            throw new AwsContainerCredentialsException("The container credentials request failed.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AwsContainerCredentialsException("The container credentials request timed out.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new AwsContainerCredentialsException(
                    "The container credentials request failed with HTTP "
                    + ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture) + ".");
            }

            byte[] body;
            try
            {
                body = await ReadLimitedAsync(response.Content, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException
                                       || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                throw new AwsContainerCredentialsException("The container credentials answer could not be read.");
            }

            var credentials = Parse(body);
            if (credentials.Expiration is not { } expiry || expiry <= now)
                throw new AwsContainerCredentialsException("The container credentials had already expired.");

            return credentials;
        }
    }

    private string? ReadAuthorizationToken()
    {
        if (_endpoint.AuthorizationTokenFile is { } path)
        {
            string fromFile;
            try
            {
                fromFile = File.ReadAllText(path).Trim();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                throw new AwsContainerCredentialsException("The container authorization token file could not be read.");
            }

            if (!ContainerCredentialsEndpoint.IsLegalHeaderValue(fromFile))
                throw new AwsContainerCredentialsException("The container authorization token is not a legal header value.");

            return fromFile;
        }

        return _endpoint.AuthorizationToken;
    }

    private static async Task<byte[]> ReadLimitedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is > MaxResponseBytes)
            throw new AwsContainerCredentialsException("The container credentials answer was too large.");

        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > MaxResponseBytes)
                throw new AwsContainerCredentialsException("The container credentials answer was too large.");
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private static AwsCredentials Parse(byte[] body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw Unusable();

            var accessKeyId = ReadString(root, "AccessKeyId");
            var secretAccessKey = ReadString(root, "SecretAccessKey");
            var sessionToken = ReadString(root, "Token");
            var expirationText = ReadString(root, "Expiration");
            if (!DateTimeOffset.TryParse(
                    expirationText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var expiration))
            {
                throw Unusable();
            }

            if (!AwsCredentials.IsHeaderSafe(accessKeyId)
                || !AwsCredentials.IsHeaderSafe(secretAccessKey)
                || !AwsCredentials.IsHeaderSafe(sessionToken))
            {
                throw Unusable();
            }

            return new AwsCredentials(accessKeyId, secretAccessKey, sessionToken, expiration);
        }
        catch (JsonException)
        {
            throw Unusable();
        }
    }

    private static string ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? throw Unusable()
            : throw Unusable();

    private static AwsContainerCredentialsException Unusable()
        => new("The container credentials answer was not usable.");
}
