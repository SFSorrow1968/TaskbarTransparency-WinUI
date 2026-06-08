using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Tests;

public sealed class RuntimeTriggerTextTests
{
    [Theory]
    [InlineData(nameof(AutomationTrigger.Desktop), "Desktop")]
    [InlineData(nameof(AutomationTrigger.WindowVisible), "Visible window")]
    [InlineData(nameof(AutomationTrigger.WindowMaximized), "Maximized window")]
    [InlineData(nameof(AutomationTrigger.Fullscreen), "Fullscreen")]
    [InlineData(nameof(AutomationTrigger.Hover), "Hover")]
    public void Label_UsesSharedRuntimeVocabulary(string state, string expected)
    {
        Assert.Equal(expected, RuntimeTriggerText.Label(state));
    }

    [Fact]
    public void Detail_ExplainsHoverProximityConsistently()
    {
        Assert.Equal(
            "The pointer is inside the saved taskbar hover proximity.",
            RuntimeTriggerText.Detail(nameof(AutomationTrigger.Hover)));
    }

    [Theory]
    [InlineData(nameof(AutomationTrigger.Hover), AutomationTrigger.Hover)]
    [InlineData(nameof(AutomationTrigger.Fullscreen), AutomationTrigger.Fullscreen)]
    [InlineData("NotAState", AutomationTrigger.Desktop)]
    public void Parse_ReturnsKnownRuntimeTriggerOrDesktopFallback(string state, AutomationTrigger expected)
    {
        Assert.Equal(expected, RuntimeTriggerText.Parse(state));
    }
}
