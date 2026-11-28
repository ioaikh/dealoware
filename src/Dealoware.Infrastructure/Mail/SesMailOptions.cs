namespace Dealoware.Infrastructure.Mail;

/// <summary>Which credentials the mail sender signs with.</summary>
public enum SesMailCredentialSource
{
    /// <summary>Fixed access keys. Development only.</summary>
    StaticKeys,

    /// <summary>The running service's own role credentials, from the container credentials endpoint.</summary>
    ContainerRole
}

/// <summary>
/// Runtime mail settings read from environment variable names only.
/// Values are never invented in source, docs, or tests as account, region, or identity facts.
/// The sender signs with the running service's own role credentials, read from the standard
/// container credentials endpoint (<see cref="ContainerCredentialsEndpoint"/>). The two fixed
/// key settings are read only in Development: the hosted service is never given them, and
/// <see cref="TryCreate"/> ignores them everywhere else. With neither source, mail stays off.
/// </summary>
public sealed class SesMailOptions
{
    public const string FromEnvironmentVariable = "DEALOWARE_MAIL_FROM";
    public const string RegionEnvironmentVariable = "DEALOWARE_MAIL_SES_REGION";

    /// <summary>Development only.</summary>
    public const string AccessKeyEnvironmentVariable = "DEALOWARE_MAIL_SES_ACCESS_KEY_ID";

    /// <summary>Development only.</summary>
    public const string SecretKeyEnvironmentVariable = "DEALOWARE_MAIL_SES_SECRET_ACCESS_KEY";

    /// <summary>Names both credential sources and no value.</summary>
    public const string NotConfiguredMessage =
        "Mail sender is not configured. Set "
        + FromEnvironmentVariable + " and " + RegionEnvironmentVariable
        + ", and run with the service's own role credentials, which the container runtime announces in "
        + ContainerCredentialsEndpoint.RelativeUriEnvironmentVariable + " or "
        + ContainerCredentialsEndpoint.FullUriEnvironmentVariable
        + ". Only in Development may " + AccessKeyEnvironmentVariable + " and "
        + SecretKeyEnvironmentVariable + " be used instead.";

    public const string StaticKeysIgnoredMessage =
        AccessKeyEnvironmentVariable + " and " + SecretKeyEnvironmentVariable
        + " are read only in Development and were ignored. Mail is sent with the service's own role credentials.";

    private SesMailOptions(
        string from,
        string region,
        SesMailCredentialSource credentialSource,
        string? accessKeyId,
        string? secretAccessKey,
        ContainerCredentialsEndpoint? containerEndpoint)
    {
        From = MailHeaderText.NormalizeSingleAddress(from, MailHeaderText.MaxToLength, nameof(from));
        if (string.IsNullOrWhiteSpace(region) || !IsSafeToken(region))
            throw new ArgumentException("Region must be a lowercase token of letters, digits, and hyphens.", nameof(region));

        Region = region.Trim();
        CredentialSource = credentialSource;
        AccessKeyId = accessKeyId;
        SecretAccessKey = secretAccessKey;
        ContainerEndpoint = containerEndpoint;
    }

    /// <summary>
    /// Fixed keys. Development only: <see cref="TryCreate"/> never picks them in any other environment.
    /// </summary>
    public SesMailOptions(string from, string region, string accessKeyId, string secretAccessKey)
        : this(
            from,
            region,
            SesMailCredentialSource.StaticKeys,
            RequireKey(accessKeyId, "Access key id is required.", nameof(accessKeyId)).Trim(),
            RequireKey(secretAccessKey, "Secret access key is required.", nameof(secretAccessKey)),
            containerEndpoint: null)
    {
    }

    public string From { get; }

    public string Region { get; }

    public SesMailCredentialSource CredentialSource { get; }

    /// <summary>Set only for <see cref="SesMailCredentialSource.StaticKeys"/>.</summary>
    public string? AccessKeyId { get; }

    /// <summary>Set only for <see cref="SesMailCredentialSource.StaticKeys"/>.</summary>
    public string? SecretAccessKey { get; }

    /// <summary>Set only for <see cref="SesMailCredentialSource.ContainerRole"/>.</summary>
    public ContainerCredentialsEndpoint? ContainerEndpoint { get; }

    /// <summary>The service's own role credentials, from the given container credentials endpoint.</summary>
    public static SesMailOptions ForContainerRole(string from, string region, ContainerCredentialsEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return new SesMailOptions(from, region, SesMailCredentialSource.ContainerRole, null, null, endpoint);
    }

    /// <summary>
    /// The mail settings, or null when mail stays off. In Development
    /// (<paramref name="allowStaticKeys"/>), both fixed keys win over the container endpoint, as in
    /// the AWS SDKs' order; everywhere else they are never read, and only the role credentials count.
    /// </summary>
    public static SesMailOptions? TryCreate(Func<string, string?> getEnv, bool allowStaticKeys)
    {
        ArgumentNullException.ThrowIfNull(getEnv);

        var from = getEnv(FromEnvironmentVariable);
        var region = getEnv(RegionEnvironmentVariable);
        if (IsBlank(from) || IsBlank(region))
            return null;

        if (allowStaticKeys)
        {
            var accessKey = getEnv(AccessKeyEnvironmentVariable);
            var secret = getEnv(SecretKeyEnvironmentVariable);
            if (!IsBlank(accessKey) && !IsBlank(secret))
                return new SesMailOptions(from!, region!, accessKey!, secret!);
        }

        var endpoint = ContainerCredentialsEndpoint.TryCreate(getEnv);
        return endpoint is null ? null : ForContainerRole(from!, region!, endpoint);
    }

    /// <summary>True when either fixed key setting is present.</summary>
    public static bool HasStaticKeys(Func<string, string?> getEnv)
    {
        ArgumentNullException.ThrowIfNull(getEnv);
        return !IsBlank(getEnv(AccessKeyEnvironmentVariable)) || !IsBlank(getEnv(SecretKeyEnvironmentVariable));
    }

    /// <summary>
    /// The credentials these settings name. <paramref name="credentialsClient"/> is used only for
    /// the role credentials, to fetch them from the container endpoint.
    /// </summary>
    public IAwsCredentialSource CreateCredentialSource(HttpClient credentialsClient, Func<DateTimeOffset>? clock = null)
        => CredentialSource == SesMailCredentialSource.StaticKeys
            ? new StaticAwsCredentialSource(AccessKeyId!, SecretAccessKey!)
            : new ContainerAwsCredentialSource(ContainerEndpoint!, credentialsClient, clock);

    public override string ToString() => nameof(SesMailOptions);

    private static string RequireKey(string value, string message, string paramName)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException(message, paramName) : value;

    private static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);

    private static bool IsSafeToken(string value)
    {
        foreach (var c in value)
        {
            if (c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-')
                continue;
            return false;
        }

        return value.Length > 0;
    }
}
