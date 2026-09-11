namespace Dealoware.Application.Auth.Dtos;

/// <summary>
/// Response from API key rotation.
/// Old key is revoked, new key is issued.
/// </summary>
public class RotateKeyResponse
{
    /// <summary>
    /// The new API key. This is the ONLY time it is returned.
    /// Store it securely - it cannot be retrieved again.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Key prefix for identifying the new key.
    /// </summary>
    public string ApiKeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Prefix of the old (now revoked) key.
    /// </summary>
    public string RevokedKeyPrefix { get; set; } = string.Empty;
}
