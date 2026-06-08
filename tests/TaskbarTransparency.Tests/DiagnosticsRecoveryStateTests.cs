using TaskbarTransparency.Models;

namespace TaskbarTransparency.Tests;

public sealed class DiagnosticsRecoveryStateTests
{
    [Fact]
    public void FromTaskbarUpdateCount_ShowsHealthGuidance_WhenTaskbarsUpdated()
    {
        var state = DiagnosticsRecoveryState.FromTaskbarUpdateCount(2);

        Assert.Equal("System health", state.Title);
        Assert.Equal("Recheck now", state.PrimaryAction);
        Assert.False(state.ShowSecondaryAction);
        Assert.Contains("detected and responding", state.PrimaryDetail);
    }

    [Fact]
    public void FromTaskbarUpdateCount_ShowsRecoveryGuidance_WhenNoTaskbarsUpdated()
    {
        var state = DiagnosticsRecoveryState.FromTaskbarUpdateCount(0);

        Assert.Equal("Probable causes", state.Title);
        Assert.Equal("Retry detection", state.PrimaryAction);
        Assert.True(state.ShowSecondaryAction);
        Assert.Contains("Explorer", state.PrimaryDetail);
    }
}
