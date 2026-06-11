using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"OxygenSettingsTests-{Guid.NewGuid():N}");

    [Fact]
    public void SaveAndLoad_RoundTripsSettings()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new SettingsStore(path);
        var expected = new AppSettings
        {
            BaseOpacity = 41,
            FadeInMilliseconds = 260,
            FadeOutMilliseconds = 140,
            AutomationEnabled = false,
            HoverRule = new OpacityRule { Enabled = false, Opacity = 90 },
            HoverSyncAcrossMonitors = true,
            FullscreenRule = new OpacityRule { Enabled = true, Opacity = 99 },
            MaximizedRule = new OpacityRule { Enabled = false, Opacity = 55 },
            WindowRule = new OpacityRule { Enabled = true, Opacity = 47 },
            HoverDistance = 31,
            StartWithWindows = true,
            ShowTrayIcon = false,
            OpenHotkey = "Ctrl+Alt+O",
            ToggleHotkey = "Ctrl+Alt+P",
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

        Assert.Equal(41, loaded.BaseOpacity);
        Assert.Equal(260, loaded.FadeInMilliseconds);
        Assert.Equal(140, loaded.FadeOutMilliseconds);
        Assert.False(loaded.AutomationEnabled);
        Assert.False(loaded.HoverRule.Enabled);
        Assert.Equal(90, loaded.HoverRule.Opacity);
        Assert.True(loaded.HoverSyncAcrossMonitors);
        Assert.True(loaded.FullscreenRule.Enabled);
        Assert.Equal(99, loaded.FullscreenRule.Opacity);
        Assert.False(loaded.MaximizedRule.Enabled);
        Assert.Equal(55, loaded.MaximizedRule.Opacity);
        Assert.True(loaded.WindowRule.Enabled);
        Assert.Equal(47, loaded.WindowRule.Opacity);
        Assert.Equal(31, loaded.HoverDistance);
        Assert.True(loaded.StartWithWindows);
        Assert.False(loaded.ShowTrayIcon);
        Assert.Equal("Ctrl+Alt+O", loaded.OpenHotkey);
        Assert.Equal("Ctrl+Alt+P", loaded.ToggleHotkey);
        Assert.Single(loaded.Monitors);
        Assert.Equal(@"\\.\DISPLAY2", loaded.Monitors[0].DeviceName);
        Assert.False(loaded.Monitors[0].SyncWithPrimary);
        Assert.Equal(83, loaded.Monitors[0].OverrideOpacity);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void Load_IgnoresLegacyProfileProperties()
    {
        var path = Path.Combine(_directory, "settings.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, """
            {
              "firstRunCompleted": true,
              "baseOpacity": 58,
              "activeProfile": {
                "name": "Legacy",
                "mode": 0,
                "opacity": 58,
                "accentHex": "#FFFFFF",
                "fadeMilliseconds": 240,
                "easing": "CubicOut"
              }
            }
            """);

        var loaded = new SettingsStore(path).Load();

        Assert.Equal(58, loaded.BaseOpacity);
        Assert.True(loaded.AutomationEnabled);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenSettingsFileIsCorrupt()
    {
        var path = Path.Combine(_directory, "settings.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, "{ nope");

        var loaded = new SettingsStore(path).Load();

        Assert.Equal(30, loaded.BaseOpacity);
        Assert.True(loaded.AutomationEnabled);
        Assert.True(loaded.ShowTrayIcon);
        Assert.True(loaded.HoverRule.Enabled);
    }

    [Fact]
    public void Save_SkipsRewrite_WhenSerializedSettingsAreUnchanged()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new SettingsStore(path);
        var settings = new AppSettings { BaseOpacity = 55 };

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
        var settings = new AppSettings { BaseOpacity = 55 };

        store.Save(settings);
        Thread.Sleep(1200);
        File.WriteAllText(path, "{}");

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(55, loaded.BaseOpacity);
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
