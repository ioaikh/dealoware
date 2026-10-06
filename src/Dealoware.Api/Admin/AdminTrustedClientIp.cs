using System.Net;

namespace Dealoware.Api.Admin;

/// <summary>
/// SC-6: IP key material is the trusted client address after ForwardedHeaders.
/// ForwardLimit is 1, so only the right-most ALB hop is used. A spoofed
/// left-most X-Forwarded-For entry never changes this value.
/// </summary>
public static class AdminTrustedClientIp
{
    public const string Unknown = "0.0.0.0";

    public static string Get(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip is null)
        {
            return Unknown;
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        return ip.ToString();
    }
}
