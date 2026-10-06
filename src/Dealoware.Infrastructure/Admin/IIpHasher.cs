namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// Service for computing keyed HMAC-SHA256 of IP addresses.
/// Raw IP addresses must never be stored or logged.
/// </summary>
public interface IIpHasher
{
    /// <summary>
    /// Computes a keyed HMAC-SHA256 of the given IP address.
    /// </summary>
    /// <param name="ipAddress">The raw IP address to hash. Must not be null or empty.</param>
    /// <returns>Base64-encoded HMAC-SHA256 hash of the IP address.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the HMAC key is not configured in non-Development environments.</exception>
    string Hash(string ipAddress);
}
