using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"OxygenSettingsTests-{Guid.NewGuid():N}");

    [Fact]
    public void SaveAndLoad_RoundTripsTuningAndAppSettings()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new SettingsStore(path);
        var expected = new AppSettings
        {
            FirstRunCompleted = true,
            AutomationEnabled = false,
            StartWithWindows = true,
            ShowTrayIcon = false,
            FullscreenOverlap = false,
            HoverReveal = false,
            HoverDistance = 31,
            ActiveProfile = TaskbarProfile.OxygenClear.WithTuningValues("Persistence Check", 41, 260, 140, "Linear"),
            Monitors =
            [
                new MonitorProfile
                {
                    DeviceName = @"\\.\DISPLAY2",
                    FriendlyName = "Display 2",
                    IsPrimary = false,
                    SyncWithPrimary = false,
                    OverrideOpacity = 83
                }
            ]
        };

        store.Save(expected);
        var loaded = store.Load();

        Assert.True(loaded.FirstRunCompleted);
        Assert.False(loaded.AutomationEnabled);
        Assert.True(loaded.StartWithWindows);
        Assert.False(loaded.ShowTrayIcon);
        Assert.False(loaded.FullscreenOverlap);
        Assert.False(loaded.HoverReveal);
        Assert.Equal(31, loaded.HoverDistance);
        Assert.Equal("Persistence Check", loaded.ActiveProfile.Name);
        Assert.Equal(41, loaded.ActiveProfile.Opacity);
        Assert.Equal(260, loaded.ActiveProfile.FadeInMilliseconds);
        Assert.Equal(140, loaded.ActiveProfile.FadeOutMilliseconds);
        Assert.Equal("Linear", loaded.ActiveProfile.Easing);
        Assert.Single(loaded.Monitors);
        Assert.Equal(@"\\.\DISPLAY2", loaded.Monitors[0].DeviceName);
        Assert.False(loaded.Monitors[0].SyncWithPrimary);
        Assert.Equal(83, loaded.Monitors[0].OverrideOpacity);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void Load_NormalizesLegacyFadeMilliseconds()
    {
        var path = Path.Combine(_directory, "settings.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, """
            {
              "firstRunCompleted": true,
              "activeProfile": {
                "name": "Legacy",
                "mode": 0,
                "opacity": 58,
                "accentHex": "#FFFFFF",
                "fadeMilliseconds": 240,
                "easing": "CubicOut",
                "fadeInMilliseconds": 0,
                "fadeOutMilliseconds": 0
              }
            }
            """);

        var loaded = new SettingsStore(path).Load();

        Assert.Equal(240, loaded.ActiveProfile.FadeInMilliseconds);
        Assert.Equal(240, loaded.ActiveProfile.FadeOutMilliseconds);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenSettingsFileIsCorrupt()
    {
        var path = Path.Combine(_directory, "settings.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, "{ nope");

        var loaded = new SettingsStore(path).Load();

        Assert.False(loaded.FirstRunCompleted);
        Assert.True(loaded.AutomationEnabled);
        Assert.True(loaded.ShowTrayIcon);
        Assert.Equal(TaskbarProfile.OxygenClear, loaded.ActiveProfile);
    }

    [Fact]
    public void Save_SkipsRewrite_WhenSerializedSettingsAreUnchanged()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new SettingsStore(path);
        var settings = new AppSettings
        {
            FirstRunCompleted = true,
            ActiveProfile = TaskbarProfile.FocusGlass
        };

        store.Save(settings);
        var firstWrite = File.GetLastWriteTimeUtc(path);
        Thread.Sleep(1200);
        store.Save(settings);

        Assert.Equal(firstWrite, File.GetLastWriteTimeUtc(path));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void Save_RewritesSettings_WhenFileChangesAfterCachedSave()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new SettingsStore(path);
        var settings = new AppSettings
        {
            FirstRunCompleted = true,
            ActiveProfile = TaskbarProfile.FocusGlass
        };

        store.Save(settings);
        Thread.Sleep(1200);
        File.WriteAllText(path, "{}");

        store.Save(settings);
        var loaded = store.Load();

        Assert.True(loaded.FirstRunCompleted);
        Assert.Equal(TaskbarProfile.FocusGlass.Name, loaded.ActiveProfile.Name);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
