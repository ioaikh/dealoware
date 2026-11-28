namespace Dealoware.Infrastructure.Mail;

/// <summary>
/// One set of AWS credentials for signing a mail request. Never logged: no member goes into a
/// log line, an exception message or a response, and <see cref="ToString"/> names the type only.
/// </summary>
public sealed class AwsCredentials
{
    public AwsCredentials(
        string accessKeyId,
        string secretAccessKey,
        string? sessionToken = null,
        DateTimeOffset? expiration = null)
    {
        if (!IsHeaderSafe(accessKeyId))
            throw new ArgumentException("Access key id is required and must be one printable token.", nameof(accessKeyId));
        if (!IsHeaderSafe(secretAccessKey))
            throw new ArgumentException("Secret access key is required and must be one printable token.", nameof(secretAccessKey));
        if (sessionToken is not null && !IsHeaderSafe(sessionToken))
            throw new ArgumentException("Session token must be one printable token.", nameof(sessionToken));

        AccessKeyId = accessKeyId;
        SecretAccessKey = secretAccessKey;
        SessionToken = sessionToken;
        Expiration = expiration;
    }

    public string AccessKeyId { get; }

    public string SecretAccessKey { get; }

    /// <summary>Set for temporary (role) credentials. Signed as <c>x-amz-security-token</c>.</summary>
    public string? SessionToken { get; }

    /// <summary>When temporary credentials stop working. Null for static keys.</summary>
    public DateTimeOffset? Expiration { get; }

    public override string ToString() => nameof(AwsCredentials);

    /// <summary>
    /// A value that goes into a signed header or the signing key: printable ASCII, no spaces,
    /// so it can never split a header or change what is signed.
    /// </summary>
    internal static bool IsHeaderSafe(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var c in value)
        {
            if (c is < '!' or > '~')
                return false;
        }

        return true;
    }
}

/// <summary>Where the mail sender gets the credentials it signs with.</summary>
public interface IAwsCredentialSource
{
    ValueTask<AwsCredentials> GetCredentialsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Fixed access keys. Development only: <see cref="SesMailOptions.TryCreate"/> never picks them
/// in any other environment, and the hosted service is never given them.
/// </summary>
public sealed class StaticAwsCredentialSource : IAwsCredentialSource
{
    private readonly AwsCredentials _credentials;

    public StaticAwsCredentialSource(string accessKeyId, string secretAccessKey)
    {
        _credentials = new AwsCredentials(accessKeyId, secretAccessKey);
    }

    public ValueTask<AwsCredentials> GetCredentialsAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(_credentials);

    public override string ToString() => nameof(StaticAwsCredentialSource);
}
