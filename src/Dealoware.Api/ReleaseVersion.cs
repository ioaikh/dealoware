using System.Reflection;
using System.Text.RegularExpressions;

namespace Dealoware.Api;

/// <summary>
/// The release's short commit hash, as both health answers give it.
/// <para>
/// It is built in, not read at run time: the image build passes the commit as the
/// <c>GIT_SHA</c> build argument, which the <c>Dockerfile</c> hands to
/// <c>dotnet publish -p:DealowareGitSha=...</c>, and the project file writes it into
/// the assembly as metadata. A build without it (a local build, the tests) answers
/// <see cref="Unknown"/>.
/// </para>
/// Only a hex commit hash of 7 to 40 characters is accepted; it is answered as its
/// first 7 characters. Anything else is answered as <see cref="Unknown"/>, so no other
/// text can reach the health answers.
/// </summary>
public static partial class ReleaseVersion
{
    public const string Unknown = "dev";

    public const string MetadataKey = "DealowareGitSha";

    public const int ShortLength = 7;

    public static string Short { get; } = FromBuildValue(ReadBuildValue());

    public static string FromBuildValue(string? value)
    {
        var v = value?.Trim().ToLowerInvariant();
        return v is not null && CommitHash().IsMatch(v) ? v[..ShortLength] : Unknown;
    }

    private static string? ReadBuildValue()
        => typeof(ReleaseVersion).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(a.Key, MetadataKey, StringComparison.Ordinal))
            ?.Value;

    [GeneratedRegex("^[0-9a-f]{7,40}$", RegexOptions.CultureInvariant)]
    private static partial Regex CommitHash();
}
