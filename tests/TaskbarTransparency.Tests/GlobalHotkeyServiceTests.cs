using TaskbarTransparency.Services;

namespace TaskbarTransparency.Tests;

public sealed class GlobalHotkeyServiceTests
{
    [Fact]
    public void TryParse_ParsesCtrlAltLetterHotkey()
    {
        var parsed = GlobalHotkeyService.TryParse("Ctrl+Alt+T", out var registration);

        Assert.True(parsed);
        Assert.Equal(0x0003u, registration.Modifiers);
        Assert.Equal((uint)'T', registration.VirtualKey);
    }

    [Fact]
    public void TryParse_ParsesFunctionKeyHotkey()
    {
        var parsed = GlobalHotkeyService.TryParse("Ctrl+Shift+F12", out var registration);

        Assert.True(parsed);
        Assert.Equal(0x0006u, registration.Modifiers);
        Assert.Equal(0x7Bu, registration.VirtualKey);
    }

    [Fact]
    public void TryParse_RejectsUnknownKeys()
    {
        var parsed = GlobalHotkeyService.TryParse("Ctrl+Alt+Space", out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryParse_RejectsUnknownModifiers()
    {
        var parsed = GlobalHotkeyService.TryParse("Ctrl+Banana+T", out _);

        Assert.False(parsed);
    }

    [Fact]
    public void RegistrationStatus_SummarizesRegistrationFailure()
    {
        var status = new HotkeyRegistrationStatus("Ctrl+Alt+T", true, false, 1409);

        Assert.Equal("Ctrl+Alt+T: registration failed (1409)", status.Summary);
    }
}
