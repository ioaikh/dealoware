namespace Dealoware.Application.Auth.Dtos;

/// <summary>
/// Response from participant registration.
/// Contains the principal sub and initial API key (shown once only).
/// </summary>
public class RegisterResponse
{
    /// <summary>
    /// The participant's unique subject identifier (OIDC sub claim).
    /// This is used in the Authorization header JWT sub claim.
    /// Maps to Artifact.OwnerParticipantId.
    /// </summary>
    public string Sub { get; set; } = string.Empty;

    /// <summary>
    /// Display name if provided.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The API key credential. This is the ONLY time it is returned.
    /// Store it securely - it cannot be retrieved again.
    /// Use in Authorization header as: "Bearer {apiKey}" or "ApiKey {apiKey}".
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Key prefix for identifying this key without exposing the full value.
    /// </summary>
    public string ApiKeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// When the participant was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
