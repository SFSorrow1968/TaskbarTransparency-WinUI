using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Tests;

public sealed class FullscreenOverlayServiceTests
{
    [Fact]
    public void ShouldOverlay_OnlyWhenFullscreenRuleIsLiveAndStateIsFullscreen()
    {
        Assert.True(FullscreenOverlayService.ShouldOverlay(
            transparencyPaused: false,
            automationEnabled: true,
            fullscreenRuleEnabled: true,
            AutomationTrigger.Fullscreen));
    }

    [Theory]
    [InlineData(true, true, true, AutomationTrigger.Fullscreen)]
    [InlineData(false, false, true, AutomationTrigger.Fullscreen)]
    [InlineData(false, true, false, AutomationTrigger.Fullscreen)]
    [InlineData(false, true, true, AutomationTrigger.Desktop)]
    [InlineData(false, true, true, AutomationTrigger.WindowMaximized)]
    [InlineData(false, true, true, AutomationTrigger.Hover)]
    public void ShouldOverlay_IsFalse_WhenPausedDisabledOrNotFullscreen(bool paused, bool automation, bool ruleEnabled, AutomationTrigger trigger)
    {
        Assert.False(FullscreenOverlayService.ShouldOverlay(paused, automation, ruleEnabled, trigger));
    }
}
