using Microsoft.AspNetCore.HttpOverrides;
using NetIPNetwork = System.Net.IPNetwork;

namespace Dealoware.Api.Admin;

/// <summary>
/// F3 (limiter ruling bbf4bc51): KnownNetworks is one comma-separated CIDR
/// string at <see cref="ConfigurationKey"/> (env
/// <see cref="EnvironmentVariableName"/>). ForwardLimit stays 1. Defaults
/// are cleared, then only those CIDRs are trusted. KnownProxies stays empty.
/// </summary>
public static class ForwardedHeadersTrust
{
    public const string ConfigurationKey = "Admin:ForwardedHeaders:KnownNetworks";
    public const string EnvironmentVariableName = "Admin__ForwardedHeaders__KnownNetworks";

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
