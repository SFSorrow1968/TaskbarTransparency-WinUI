using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Tests;

public sealed class RuntimeStateSensorServiceTests
{
    [Fact]
    public void ResolveTrigger_ReturnsDesktop_WhenAutomationIsDisabled()
    {
        var trigger = RuntimeStateSensorService.ResolveTrigger(
            automationEnabled: false,
            hoverRevealEnabled: true,
            fullscreenOverlapEnabled: true,
            isMouseNearTaskbar: true,
            hasForegroundWindow: true,
            isForegroundMaximized: true,
            isForegroundFullscreen: true);

        Assert.Equal(AutomationTrigger.Desktop, trigger);
    }

    [Fact]
    public void ResolveTrigger_PrioritizesHover_WhenEnabled()
    {
        var trigger = RuntimeStateSensorService.ResolveTrigger(
            automationEnabled: true,
            hoverRevealEnabled: true,
            fullscreenOverlapEnabled: true,
            isMouseNearTaskbar: true,
            hasForegroundWindow: true,
            isForegroundMaximized: false,
            isForegroundFullscreen: true);

        Assert.Equal(AutomationTrigger.Hover, trigger);
    }

    [Fact]
    public void ResolveTrigger_DetectsFullscreen_WhenOverlapIsEnabled()
    {
        var trigger = RuntimeStateSensorService.ResolveTrigger(
            automationEnabled: true,
            hoverRevealEnabled: false,
            fullscreenOverlapEnabled: true,
            isMouseNearTaskbar: false,
            hasForegroundWindow: true,
            isForegroundMaximized: true,
            isForegroundFullscreen: true);

        Assert.Equal(AutomationTrigger.Fullscreen, trigger);
    }

    [Fact]
    public void ResolveTrigger_DetectsMaximizedBeforeVisibleWindow()
    {
        var trigger = RuntimeStateSensorService.ResolveTrigger(
            automationEnabled: true,
            hoverRevealEnabled: false,
            fullscreenOverlapEnabled: false,
            isMouseNearTaskbar: false,
            hasForegroundWindow: true,
            isForegroundMaximized: true,
            isForegroundFullscreen: false);

        Assert.Equal(AutomationTrigger.WindowMaximized, trigger);
    }

    [Fact]
    public void ResolveTrigger_ReturnsVisibleWindow_WhenForegroundExists()
    {
        var trigger = RuntimeStateSensorService.ResolveTrigger(
            automationEnabled: true,
            hoverRevealEnabled: false,
            fullscreenOverlapEnabled: false,
            isMouseNearTaskbar: false,
            hasForegroundWindow: true,
            isForegroundMaximized: false,
            isForegroundFullscreen: false);

        Assert.Equal(AutomationTrigger.WindowVisible, trigger);
    }
}
