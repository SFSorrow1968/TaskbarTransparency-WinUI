using System.Reflection;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Tests;

public sealed class AboutMetadataTests
{
    [Theory]
    [InlineData("0.1.33", "v0.1.33")]
    [InlineData("v0.1.33", "v0.1.33")]
    [InlineData("0.1.33+abc123", "v0.1.33")]
    public void NormalizeVersion_ReturnsSnapshotStyleVersion(string input, string expected)
    {
        Assert.Equal(expected, AboutMetadata.NormalizeVersion(input));
    }

    [Fact]
    public void FromAssembly_UsesInformationalVersion()
    {
        var metadata = AboutMetadata.FromAssembly(Assembly.GetExecutingAssembly());

        Assert.StartsWith("v", metadata.Version);
        Assert.Equal(metadata.Version, metadata.LatestVersion);
    }
}
