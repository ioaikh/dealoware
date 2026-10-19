using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.HttpOverrides;
using NetIPNetwork = System.Net.IPNetwork;

namespace Dealoware.Api.Admin;

/// <summary>
/// F3 (limiter ruling bbf4bc51): KnownNetworks is one comma-separated CIDR
/// string at <see cref="ConfigurationKey"/> (env
/// <see cref="EnvironmentVariableName"/>). ForwardLimit stays 1. Defaults
/// are cleared, then only those CIDRs are trusted. KnownProxies stays empty.
/// IPv4 must be RFC 1918 and /16 or narrower. IPv6 must be ULA (fc00::/7)
/// and /48 or narrower. Loopback, unspecified, link-local, and public
/// ranges are refused.
/// </summary>
public static class ForwardedHeadersTrust
{
    public const string ConfigurationKey = "Admin:ForwardedHeaders:KnownNetworks";
    public const string EnvironmentVariableName = "Admin__ForwardedHeaders__KnownNetworks";
    public const int MinimumIpv4Prefix = 16;
    public const int MinimumIpv6Prefix = 48;
    public const int Ipv4MappedPrefixBias = 96;
    public const int MinimumIpv4MappedPrefix = Ipv4MappedPrefixBias + MinimumIpv4Prefix;

    public static bool TryParse(string? raw, out IReadOnlyList<NetIPNetwork> networks, out string? error)
    {
        networks = Array.Empty<NetIPNetwork>();
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "value is missing or empty";
            return false;
        }

        var parsed = new List<NetIPNetwork>();
        foreach (var part in raw.Split(','))
        {
            var cidr = part.Trim();
            if (cidr.Length == 0)
            {
                error = "contains an empty CIDR entry";
                return false;
            }

            if (!TryParseCidr(cidr, out var network, out error))
            {
                return false;
            }

            parsed.Add(network);
        }

        networks = parsed;
        return parsed.Count > 0;
    }

    private static bool TryParseCidr(string cidr, out NetIPNetwork network, out string? error)
    {
        error = null;
        if (!NetIPNetwork.TryParse(cidr, out network))
        {
            if (!IPAddress.TryParse(cidr, out var host))
            {
                error = "contains an invalid CIDR";
                return false;
            }

            var hostPrefix = host.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
            network = new NetIPNetwork(host, hostPrefix);
        }

        var prefix = network.PrefixLength;
        var address = network.BaseAddress;
        if (address.IsIPv4MappedToIPv6)
        {
            if (prefix < MinimumIpv4MappedPrefix)
            {
                error = "contains an over-broad CIDR";
                return false;
            }

            return IsAllowedIpv4(address.MapToIPv4(), out error);
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            if (prefix < MinimumIpv4Prefix)
            {
                error = "contains an over-broad CIDR";
                return false;
            }

            return IsAllowedIpv4(address, out error);
        }

        if (prefix < MinimumIpv6Prefix)
        {
            error = "contains an over-broad CIDR";
            return false;
        }

        return IsAllowedIpv6(address, out error);
    }

    private static bool IsAllowedIpv4(IPAddress address, out string? error)
    {
        error = null;
        if (IPAddress.IsLoopback(address)
            || address.Equals(IPAddress.Any)
            || IsIpv4LinkLocal(address)
            || !IsRfc1918(address))
        {
            error = "contains a public, loopback, unspecified, or link-local CIDR";
            return false;
        }

        return true;
    }

    private static bool IsAllowedIpv6(IPAddress address, out string? error)
    {
        error = null;
        if (IPAddress.IsLoopback(address)
            || address.Equals(IPAddress.IPv6Any)
            || address.IsIPv6LinkLocal
            || !IsUniqueLocal(address))
        {
            error = "contains a public, loopback, unspecified, or link-local CIDR";
            return false;
        }

        return true;
    }

    private static bool IsRfc1918(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
               || bytes[0] == 172 && bytes[1] is >= 16 and <= 31
               || bytes[0] == 192 && bytes[1] == 168;
    }

    private static bool IsIpv4LinkLocal(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 169 && bytes[1] == 254;
    }

    private static bool IsUniqueLocal(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return (bytes[0] & 0xfe) == 0xfc;
    }

    public static IReadOnlyList<NetIPNetwork> Resolve(string? raw, bool requireConfigured)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            if (requireConfigured)
            {
                throw MissingOrInvalid();
            }

            return Array.Empty<NetIPNetwork>();
        }

        if (TryParse(raw, out var networks, out _))
        {
            return networks;
        }

        if (requireConfigured)
        {
            throw MissingOrInvalid();
        }

        return Array.Empty<NetIPNetwork>();
    }

    public static void Apply(ForwardedHeadersOptions options, IReadOnlyList<NetIPNetwork> networks)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.ForwardLimit = 1;

        foreach (var network in networks)
        {
            options.KnownIPNetworks.Add(network);
        }
    }

    private static InvalidOperationException MissingOrInvalid() =>
        new($"{ConfigurationKey} is missing, empty, or invalid.");
}
