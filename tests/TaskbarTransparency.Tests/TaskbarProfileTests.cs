using TaskbarTransparency.Models;

namespace TaskbarTransparency.Tests;

public sealed class TaskbarProfileTests
{
    [Fact]
    public void NormalizeTransitions_MigratesLegacyFadeMilliseconds()
    {
        var profile = TaskbarProfile.OxygenClear with
        {
            FadeMilliseconds = 180,
            FadeInMilliseconds = 0,
            FadeOutMilliseconds = 0
        };

        var normalized = profile.NormalizeTransitions();

        Assert.Equal(180, normalized.FadeInMilliseconds);
        Assert.Equal(180, normalized.FadeOutMilliseconds);
    }

    [Fact]
    public void NormalizeTransitions_PreservesExplicitInstantFades()
    {
        var profile = TaskbarProfile.OxygenClear with
        {
            FadeMilliseconds = 0,
            FadeInMilliseconds = 0,
            FadeOutMilliseconds = 0
        };

        var normalized = profile.NormalizeTransitions();

        Assert.Equal(0, normalized.FadeInMilliseconds);
        Assert.Equal(0, normalized.FadeOutMilliseconds);
    }
}
