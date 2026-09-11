namespace Dealoware.Application.Auth.Dtos;

/// <summary>
/// Request to issue a JWT token using API key authentication.
/// </summary>
public class TokenRequest
{
    /// <summary>
    /// The API key to authenticate with.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
