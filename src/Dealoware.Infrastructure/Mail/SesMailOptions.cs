using Dealoware.Domain.Mail;

namespace Dealoware.Infrastructure.Mail;

/// <summary>
/// Runtime mail settings read from environment variable names only.
/// Values are never invented in source, docs, or tests as account, region, or identity facts.
/// </summary>
public sealed class SesMailOptions
{
    public const string FromEnvironmentVariable = "DEALOWARE_MAIL_FROM";
    public const string RegionEnvironmentVariable = "DEALOWARE_MAIL_SES_REGION";
    public const string AccessKeyEnvironmentVariable = "DEALOWARE_MAIL_SES_ACCESS_KEY_ID";
    public const string SecretKeyEnvironmentVariable = "DEALOWARE_MAIL_SES_SECRET_ACCESS_KEY";

    public string From { get; }
    public string Region { get; }
    public string AccessKeyId { get; }
    public string SecretAccessKey { get; }

    public SesMailOptions(string from, string region, string accessKeyId, string secretAccessKey)
    {
        From = MailHeaderText.NormalizeSingleAddress(from, AdminMailMessage.MaxToLength, nameof(from));
        if (string.IsNullOrWhiteSpace(region) || !IsSafeToken(region))
            throw new ArgumentException("Region must be a lowercase token of letters, digits, and hyphens.", nameof(region));
        if (string.IsNullOrWhiteSpace(accessKeyId))
            throw new ArgumentException("Access key id is required.", nameof(accessKeyId));
        if (string.IsNullOrWhiteSpace(secretAccessKey))
            throw new ArgumentException("Secret access key is required.", nameof(secretAccessKey));

        Region = region.Trim();
        AccessKeyId = accessKeyId.Trim();
        SecretAccessKey = secretAccessKey;
    }

    public static SesMailOptions? TryCreate(Func<string, string?> getEnv)
    {
        ArgumentNullException.ThrowIfNull(getEnv);

        var from = getEnv(FromEnvironmentVariable);
        var region = getEnv(RegionEnvironmentVariable);
        var accessKey = getEnv(AccessKeyEnvironmentVariable);
        var secret = getEnv(SecretKeyEnvironmentVariable);

        if (IsBlank(from) || IsBlank(region) || IsBlank(accessKey) || IsBlank(secret))
            return null;

        return new SesMailOptions(from!, region!, accessKey!, secret!);
    }

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
