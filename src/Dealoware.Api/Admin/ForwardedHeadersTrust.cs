using Microsoft.AspNetCore.HttpOverrides;
using NetIPNetwork = System.Net.IPNetwork;

namespace Dealoware.Api.Admin;

/// <summary>
/// F3 (limiter ruling bbf4bc51): KnownNetworks is one comma-separated CIDR
/// string at <see cref="ConfigurationKey"/>. ForwardLimit stays 1. No CIDRs
/// or account IDs are hard-coded. Development may trust loopback.
/// </summary>
public static class ForwardedHeadersTrust
{
    public const string ConfigurationKey = "Admin:ForwardedHeaders:KnownNetworks";

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

            if (!NetIPNetwork.TryParse(cidr, out var network))
            {
                error = "contains an invalid CIDR";
                return false;
            }

            parsed.Add(network);
        }

        networks = parsed;
        return parsed.Count > 0;
    }

    public static IReadOnlyList<NetIPNetwork> Resolve(string? raw, bool requireConfigured)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            if (requireConfigured)
            {
                throw new InvalidOperationException(
                    $"{ConfigurationKey} must be set to one or more comma-separated CIDRs (for example 10.0.0.0/16). Deploy injects this before rollout.");
            }

            return Array.Empty<NetIPNetwork>();
        }

        if (TryParse(raw, out var networks, out var error))
        {
            return networks;
        }

        if (requireConfigured)
        {
            throw new InvalidOperationException(
                $"{ConfigurationKey} {error}. Each entry must be a CIDR. Deploy injects this before rollout.");
        }

        return ParseLenient(raw);
    }

    public static void Apply(
        ForwardedHeadersOptions options,
        IReadOnlyList<NetIPNetwork> networks,
        bool allowLoopback)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.ForwardLimit = 1;

        foreach (var network in networks)
        {
            options.KnownIPNetworks.Add(network);
        }

        if (allowLoopback)
        {
            options.KnownProxies.Add(System.Net.IPAddress.Loopback);
            options.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback);
        }
    }

    private static IReadOnlyList<NetIPNetwork> ParseLenient(string raw)
    {
        var parsed = new List<NetIPNetwork>();
        foreach (var part in raw.Split(','))
        {
            var cidr = part.Trim();
            if (cidr.Length > 0 && NetIPNetwork.TryParse(cidr, out var network))
            {
                parsed.Add(network);
            }
        }

        return parsed;
    }
}
