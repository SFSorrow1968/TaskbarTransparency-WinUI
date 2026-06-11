using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Tests;

public sealed class OpacityPolicyTests
{
    [Fact]
    public void Resolve_ReturnsBaseOpacity_WhenAutomationIsDisabled()
    {
        var settings = new AppSettings
        {
            BaseOpacity = 42,
            AutomationEnabled = false,
            FullscreenRule = new OpacityRule { Enabled = true, Opacity = 100 }
        };

        var resolution = OpacityPolicy.Resolve(settings, AutomationTrigger.Fullscreen);

        Assert.Equal(42, resolution.Opacity);
        Assert.Equal(OpacityPolicy.BaseSource, resolution.Source);
    }

    [Fact]
    public void Resolve_UsesExactRuleOpacity_ForMatchingState()
    {
        var settings = new AppSettings
        {
            BaseOpacity = 30,
            HoverRule = new OpacityRule { Enabled = true, Opacity = 95 },
            FullscreenRule = new OpacityRule { Enabled = true, Opacity = 100 },
            MaximizedRule = new OpacityRule { Enabled = true, Opacity = 60 },
            WindowRule = new OpacityRule { Enabled = true, Opacity = 45 }
        };

        Assert.Equal((byte)95, OpacityPolicy.Resolve(settings, AutomationTrigger.Hover).Opacity);
        Assert.Equal((byte)100, OpacityPolicy.Resolve(settings, AutomationTrigger.Fullscreen).Opacity);
        Assert.Equal((byte)60, OpacityPolicy.Resolve(settings, AutomationTrigger.WindowMaximized).Opacity);
        Assert.Equal((byte)45, OpacityPolicy.Resolve(settings, AutomationTrigger.WindowVisible).Opacity);
        Assert.Equal((byte)30, OpacityPolicy.Resolve(settings, AutomationTrigger.Desktop).Opacity);
    }

    [Fact]
    public void Resolve_FallsBackToBase_WhenRuleIsDisabled()
    {
        var settings = new AppSettings
        {
            BaseOpacity = 30,
            HoverRule = new OpacityRule { Enabled = false, Opacity = 95 },
            WindowRule = new OpacityRule { Enabled = false, Opacity = 45 }
        };

        var hover = OpacityPolicy.Resolve(settings, AutomationTrigger.Hover);
        var window = OpacityPolicy.Resolve(settings, AutomationTrigger.WindowVisible);

        Assert.Equal((byte)30, hover.Opacity);
        Assert.Equal(OpacityPolicy.BaseSource, hover.Source);
        Assert.Equal((byte)30, window.Opacity);
        Assert.Equal(OpacityPolicy.BaseSource, window.Source);
    }

    [Fact]
    public void Resolve_CascadesMaximizedToWindowRule_WhenMaximizedIsDisabled()
    {
        var settings = new AppSettings
        {
            BaseOpacity = 30,
            MaximizedRule = new OpacityRule { Enabled = false, Opacity = 60 },
            WindowRule = new OpacityRule { Enabled = true, Opacity = 45 }
        };

        var resolution = OpacityPolicy.Resolve(settings, AutomationTrigger.WindowMaximized);

        Assert.Equal((byte)45, resolution.Opacity);
        Assert.Equal(OpacityPolicy.WindowSource, resolution.Source);
    }

    [Fact]
    public void Resolve_CascadesFullscreenToMaximizedRule_WhenFullscreenIsDisabled()
    {
        var settings = new AppSettings
        {
            BaseOpacity = 30,
            FullscreenRule = new OpacityRule { Enabled = false, Opacity = 100 },
            MaximizedRule = new OpacityRule { Enabled = true, Opacity = 60 }
        };

        var resolution = OpacityPolicy.Resolve(settings, AutomationTrigger.Fullscreen);

        Assert.Equal((byte)60, resolution.Opacity);
        Assert.Equal(OpacityPolicy.MaximizedSource, resolution.Source);
    }

    [Fact]
    public void Resolve_ReportsRuleSource_ForMatchedRule()
    {
        var settings = new AppSettings
        {
            HoverRule = new OpacityRule { Enabled = true, Opacity = 100 }
        };

        Assert.Equal(OpacityPolicy.HoverSource, OpacityPolicy.Resolve(settings, AutomationTrigger.Hover).Source);
        Assert.Equal(OpacityPolicy.BaseSource, OpacityPolicy.Resolve(settings, AutomationTrigger.Desktop).Source);
    }
}
