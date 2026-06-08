using System.Reflection;

namespace TaskbarTransparency.Models;

public sealed record AboutMetadata(string Version, string LatestVersion)
{
    public static AboutMetadata FromAssembly(Assembly assembly)
    {
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        var version = NormalizeVersion(informationalVersion)
            ?? NormalizeVersion(assembly.GetName().Version?.ToString())
            ?? "local";

        return new AboutMetadata(version, version);
    }

    public static string? NormalizeVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        var trimmed = version.Trim();
        var metadataIndex = trimmed.IndexOf('+');
        if (metadataIndex >= 0)
        {
            trimmed = trimmed[..metadataIndex];
        }

        return trimmed.StartsWith('v') ? trimmed : $"v{trimmed}";
    }
}
