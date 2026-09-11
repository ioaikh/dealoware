namespace Dealoware.Application.Auth.Dtos;

/// <summary>
/// Request to revoke a credential (API key or JWT).
/// </summary>
public class RevokeRequest
{
    /// <summary>
    /// The API key to revoke (optional, specify this OR TokenJti).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The JWT ID (jti claim) to revoke (optional, specify this OR ApiKey).
    /// </summary>
    public string? TokenJti { get; set; }
}
