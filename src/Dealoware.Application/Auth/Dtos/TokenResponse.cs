namespace Dealoware.Application.Auth.Dtos;

/// <summary>
/// Response containing an issued JWT token.
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// The JWT access token.
    /// Use in Authorization header as: "Bearer {token}".
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type (always "Bearer").
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Seconds until the token expires.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// The participant's subject identifier.
    /// </summary>
    public string Sub { get; set; } = string.Empty;
}
