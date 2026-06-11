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
            fullscreenOverlapEnabled: true,
            hasForegroundWindow: true,
            isForegroundMaximized: true,
            isForegroundFullscreen: true);

        Assert.Equal(AutomationTrigger.Desktop, trigger);
    }

    [Fact]
    public void ResolveTrigger_DetectsFullscreen_WhenOverlapIsEnabled()
    {
        var trigger = RuntimeStateSensorService.ResolveTrigger(
            automationEnabled: true,
            fullscreenOverlapEnabled: true,
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
            fullscreenOverlapEnabled: false,
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
            fullscreenOverlapEnabled: false,
            hasForegroundWindow: true,
            isForegroundMaximized: false,
            isForegroundFullscreen: false);

        Assert.Equal(AutomationTrigger.WindowVisible, trigger);
    }

    [Fact]
    public void SensorSnapshot_Matches_ComparesTriggerAndHoveredTaskbars()
    {
        var first = new SensorSnapshot(AutomationTrigger.Desktop, [new IntPtr(1)]);
        var sameValues = new SensorSnapshot(AutomationTrigger.Desktop, [new IntPtr(1)]);
        var differentHover = new SensorSnapshot(AutomationTrigger.Desktop, [new IntPtr(2)]);
        var differentTrigger = new SensorSnapshot(AutomationTrigger.WindowVisible, [new IntPtr(1)]);
        var emptyHover = new SensorSnapshot(AutomationTrigger.Desktop, []);

        Assert.True(first.Matches(sameValues));
        Assert.False(first.Matches(differentHover));
        Assert.False(first.Matches(differentTrigger));
        Assert.False(first.Matches(emptyHover));
        Assert.False(first.Matches(null));
    }

    [Fact]
    public void IsPointNearRectForTest_UsesConfiguredHoverDistance()
    {
        var nearWithLargerDistance = RuntimeStateSensorService.IsPointNearRectForTest(
            x: 50,
            y: 86,
            left: 0,
            top: 100,
            right: 100,
            bottom: 120,
            distance: 16);

        var outsideWithSmallerDistance = RuntimeStateSensorService.IsPointNearRectForTest(
            x: 50,
            y: 86,
            left: 0,
            top: 100,
            right: 100,
            bottom: 120,
            distance: 8);

        Assert.True(nearWithLargerDistance);
        Assert.False(outsideWithSmallerDistance);
    }

    [Fact]
    public void IsPointNearRectForTest_ClampsHoverDistanceToSupportedRange()
    {
        var outsideAtMaximumDistance = RuntimeStateSensorService.IsPointNearRectForTest(
            x: 50,
            y: 51,
            left: 0,
            top: 100,
            right: 100,
            bottom: 120,
            distance: 999);

        var insideAtMaximumDistance = RuntimeStateSensorService.IsPointNearRectForTest(
            x: 50,
            y: 52,
            left: 0,
            top: 100,
            right: 100,
            bottom: 120,
            distance: 999);

        Assert.False(outsideAtMaximumDistance);
        Assert.True(insideAtMaximumDistance);
    }

    [Fact]
    public void IsPointNearRectForTest_ZeroDistanceOnlyMatchesInsideTaskbar()
    {
        var onePixelOutside = RuntimeStateSensorService.IsPointNearRectForTest(
            x: 50,
            y: 99,
            left: 0,
            top: 100,
            right: 100,
            bottom: 120,
            distance: 0);

        var onTaskbarEdge = RuntimeStateSensorService.IsPointNearRectForTest(
            x: 50,
            y: 100,
            left: 0,
            top: 100,
            right: 100,
            bottom: 120,
            distance: 0);

        Assert.False(onePixelOutside);
        Assert.True(onTaskbarEdge);
    }

    [Fact]
    public void IsPointNearRectForTest_ClampsNegativeHoverDistanceToZero()
    {
        var onePixelOutside = RuntimeStateSensorService.IsPointNearRectForTest(
            x: 50,
            y: 99,
            left: 0,
            top: 100,
            right: 100,
            bottom: 120,
            distance: -10);

        var onTaskbarEdge = RuntimeStateSensorService.IsPointNearRectForTest(
            x: 50,
            y: 100,
            left: 0,
            top: 100,
            right: 100,
            bottom: 120,
            distance: -10);

        Assert.False(onePixelOutside);
        Assert.True(onTaskbarEdge);
    }

    [Theory]
    [InlineData("Shell_TrayWnd", true)]
    [InlineData("Shell_SecondaryTrayWnd", true)]
    [InlineData("Progman", true)]
    [InlineData("WorkerW", true)]
    [InlineData("Notepad", false)]
    public void IsShellClassNameForTest_IdentifiesShellWindowClasses(string className, bool expected)
    {
        Assert.Equal(expected, RuntimeStateSensorService.IsShellClassNameForTest(className));
    }
}
