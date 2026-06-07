using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Tests;

public sealed class OpacityPolicyTests
{
    [Fact]
    public void Resolve_ReturnsBaseOpacity_WhenAutomationIsDisabled()
    {
        var profile = TaskbarProfile.OxygenClear with { Opacity = 42 };

        var opacity = OpacityPolicy.Resolve(profile, AutomationTrigger.Fullscreen, automationEnabled: false);

        Assert.Equal(42, opacity);
    }

    [Theory]
    [InlineData(AutomationTrigger.Desktop, 20)]
    [InlineData(AutomationTrigger.WindowVisible, 40)]
    [InlineData(AutomationTrigger.WindowMaximized, 54)]
    [InlineData(AutomationTrigger.Hover, 68)]
    [InlineData(AutomationTrigger.Fullscreen, 82)]
    public void Resolve_AdjustsOpacity_ForRuntimeState(AutomationTrigger trigger, int expected)
    {
        var profile = TaskbarProfile.OxygenClear with { Opacity = 32 };

        var opacity = OpacityPolicy.Resolve(profile, trigger, automationEnabled: true);

        Assert.Equal(expected, opacity);
    }

    [Fact]
    public void Resolve_ClampsOpacityToOneHundred()
    {
        var profile = TaskbarProfile.NightSolid with { Opacity = 92 };

        var opacity = OpacityPolicy.Resolve(profile, AutomationTrigger.Fullscreen, automationEnabled: true);

        Assert.Equal(100, opacity);
    }
}
