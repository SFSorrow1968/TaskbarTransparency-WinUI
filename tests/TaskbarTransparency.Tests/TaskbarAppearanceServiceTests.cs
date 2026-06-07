using TaskbarTransparency.Services;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Tests;

public sealed class TaskbarAppearanceServiceTests
{
    [Fact]
    public void ComposeColorForTest_UsesAbgrOrderingExpectedByAccentPolicy()
    {
        var color = TaskbarAppearanceService.ComposeColorForTest("#112233", 50);

        Assert.Equal(unchecked((int)0x7F332211), color);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(32, 81)]
    [InlineData(50, 127)]
    [InlineData(100, 255)]
    public void ConvertOpacityToAlphaForTest_MapsPercentToLayeredWindowAlpha(byte opacity, byte expected)
    {
        var alpha = TaskbarAppearanceService.ConvertOpacityToAlphaForTest(opacity);

        Assert.Equal(expected, alpha);
    }

    [Fact]
    public void EaseProgressForTest_UsesLinearProgress_WhenRequested()
    {
        var progress = TaskbarAppearanceService.EaseProgressForTest(0.5, "Linear");

        Assert.Equal(0.5, progress, precision: 3);
    }

    [Fact]
    public void EaseProgressForTest_UsesCubicOutFallback_ForSmoothFade()
    {
        var progress = TaskbarAppearanceService.EaseProgressForTest(0.5, "CubicOut");

        Assert.Equal(0.875, progress, precision: 3);
    }

    [Fact]
    public void SelectFadeMillisecondsForTest_UsesFadeIn_WhenOpacityIncreases()
    {
        var profile = TaskbarProfile.FocusGlass with
        {
            FadeInMilliseconds = 400,
            FadeOutMilliseconds = 90
        };

        var duration = TaskbarAppearanceService.SelectFadeMillisecondsForTest(profile, startAlpha: 80, targetAlpha: 160);

        Assert.Equal(400, duration);
    }

    [Fact]
    public void SelectFadeMillisecondsForTest_UsesFadeOut_WhenOpacityDecreases()
    {
        var profile = TaskbarProfile.FocusGlass with
        {
            FadeInMilliseconds = 400,
            FadeOutMilliseconds = 90
        };

        var duration = TaskbarAppearanceService.SelectFadeMillisecondsForTest(profile, startAlpha: 160, targetAlpha: 80);

        Assert.Equal(90, duration);
    }
}
