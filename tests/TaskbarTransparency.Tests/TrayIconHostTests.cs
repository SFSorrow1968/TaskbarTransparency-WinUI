using TaskbarTransparency.Services;

namespace TaskbarTransparency.Tests;

public sealed class TrayIconHostTests
{
    [Fact]
    public void CommandText_UsesCurrentTuningLanguage()
    {
        Assert.Equal("Open Dashboard", TrayIconHost.OpenCommandText);
        Assert.Equal("Apply Now", TrayIconHost.ApplyCommandText);
        Assert.Equal("Toggle Transparency", TrayIconHost.ToggleCommandText);
        Assert.Equal("Open Tuning", TrayIconHost.TuningCommandText);
        Assert.Equal("Exit", TrayIconHost.ExitCommandText);
    }

    [Theory]
    [InlineData(TrayIconHost.OpenCommand, "open")]
    [InlineData(TrayIconHost.ApplyCommand, "apply")]
    [InlineData(TrayIconHost.ToggleCommand, "toggle")]
    [InlineData(TrayIconHost.TuningCommand, "tuning")]
    [InlineData(TrayIconHost.ExitCommand, "exit")]
    public void ExecuteCommandForTest_DispatchesExpectedAction(int command, string expected)
    {
        var calls = new List<string>();
        var host = new TrayIconHost();
        host.ConfigureCommandsForTest(
            () => calls.Add("open"),
            () => calls.Add("tuning"),
            () => calls.Add("apply"),
            () => calls.Add("toggle"),
            () => calls.Add("exit"));

        var handled = host.ExecuteCommandForTest(command);

        Assert.True(handled);
        Assert.Equal([expected], calls);
    }

    [Fact]
    public void ExecuteCommandForTest_IgnoresUnknownCommands()
    {
        var calls = new List<string>();
        var host = new TrayIconHost();
        host.ConfigureCommandsForTest(
            () => calls.Add("open"),
            () => calls.Add("tuning"),
            () => calls.Add("apply"),
            () => calls.Add("toggle"),
            () => calls.Add("exit"));

        var handled = host.ExecuteCommandForTest(999);

        Assert.False(handled);
        Assert.Empty(calls);
    }
}
