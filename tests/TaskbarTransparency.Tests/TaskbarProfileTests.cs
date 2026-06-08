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

    [Fact]
    public void WithTuningValues_PreservesCurrentMaterialAndAccent()
    {
        var tuned = TaskbarProfile.OxygenClear.WithTuningValues("  Work Clear  ", 44, 310, 90, "Linear");

        Assert.Equal("Work Clear", tuned.Name);
        Assert.Equal(TaskbarVisualMode.Clear, tuned.Mode);
        Assert.Equal("#FFFFFF", tuned.AccentHex);
        Assert.Equal(44, tuned.Opacity);
        Assert.Equal(310, tuned.FadeMilliseconds);
        Assert.Equal(310, tuned.FadeInMilliseconds);
        Assert.Equal(90, tuned.FadeOutMilliseconds);
        Assert.Equal("Linear", tuned.Easing);
    }

    [Fact]
    public void WithTuningValues_KeepsExistingName_WhenNameIsBlank()
    {
        var tuned = TaskbarProfile.FocusGlass.WithTuningValues(" ", 55, 120, 80, "CubicOut");

        Assert.Equal(TaskbarProfile.FocusGlass.Name, tuned.Name);
    }

    [Fact]
    public void WithVisualMode_PreservesTuningValues()
    {
        var profile = TaskbarProfile.OxygenClear.WithTuningValues("Quiet", 63, 260, 140, "Linear");

        var mica = profile.WithVisualMode(TaskbarVisualMode.Mica);

        Assert.Equal("Quiet", mica.Name);
        Assert.Equal(TaskbarVisualMode.Mica, mica.Mode);
        Assert.Equal(63, mica.Opacity);
        Assert.Equal(260, mica.FadeInMilliseconds);
        Assert.Equal(140, mica.FadeOutMilliseconds);
        Assert.Equal("Linear", mica.Easing);
    }
}
