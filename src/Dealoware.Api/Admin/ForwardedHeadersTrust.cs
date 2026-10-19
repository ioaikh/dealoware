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
/// IPv4 prefixes shorter than /8, IPv6 prefixes shorter than /32, and
/// IPv4-mapped IPv6 prefixes shorter than /104 are rejected so
/// 0.0.0.0/0 and ::/0 cannot trust every peer.
/// </summary>
public static class ForwardedHeadersTrust
{
    public const string ConfigurationKey = "Admin:ForwardedHeaders:KnownNetworks";
    public const string EnvironmentVariableName = "Admin__ForwardedHeaders__KnownNetworks";
    public const int MinimumIpv4Prefix = 8;
    public const int MinimumIpv6Prefix = 32;
    public const int MinimumIpv4MappedPrefix = 104;

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

            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            if (prefix < MinimumIpv4Prefix)
            {
                error = "contains an over-broad CIDR";
                return false;
            }

            return true;
        }

        if (prefix < MinimumIpv6Prefix)
        {
            error = "contains an over-broad CIDR";
            return false;
        }

        return true;
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
