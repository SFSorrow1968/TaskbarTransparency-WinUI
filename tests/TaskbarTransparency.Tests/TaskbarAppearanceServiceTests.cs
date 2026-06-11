using TaskbarTransparency.Services;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Tests;

public sealed class TaskbarAppearanceServiceTests
{
    [Fact]
    public void ComposeColorForTest_UsesAbgrOrderingExpectedByAccentPolicy()
    {
        var color = TaskbarAppearanceService.ComposeColorForTest("#112233", 50);

        Assert.Equal(unchecked((int)0x7F332211), color);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(32, 81)]
    [InlineData(50, 127)]
    [InlineData(100, 255)]
    public void ConvertOpacityToAlphaForTest_MapsPercentToLayeredWindowAlpha(byte opacity, byte expected)
    {
        var alpha = TaskbarAppearanceService.ConvertOpacityToAlphaForTest(opacity);

        Assert.Equal(expected, alpha);
    }

    [Fact]
    public void EaseProgressForTest_UsesCubicOutForSmoothFade()
    {
        Assert.Equal(0.875, TaskbarAppearanceService.EaseProgressForTest(0.5), precision: 3);
        Assert.Equal(0d, TaskbarAppearanceService.EaseProgressForTest(0d), precision: 3);
        Assert.Equal(1d, TaskbarAppearanceService.EaseProgressForTest(1d), precision: 3);
    }

    [Fact]
    public void BuildAlphaAnimationDurationsForTest_SkipsUnchangedTaskbarsAndPicksDirection()
    {
        var unchanged = new IntPtr(1);
        var fadingOut = new IntPtr(2);
        var fadingIn = new IntPtr(3);
        var current = new Dictionary<IntPtr, byte>
        {
            [unchanged] = 80,
            [fadingOut] = 160,
            [fadingIn] = 40
        };
        var targets = new Dictionary<IntPtr, byte>
        {
            [unchanged] = 80,
            [fadingOut] = 120,
            [fadingIn] = 200
        };

        var durations = TaskbarAppearanceService.BuildAlphaAnimationDurationsForTest(320, 90, current, targets);

        Assert.Equal([0, 90, 320], durations);
    }

    [Fact]
    public void ResolveMonitorOpacityForTest_UsesGlobalOpacity_WhenMonitorIsSynced()
    {
        var opacity = TaskbarAppearanceService.ResolveMonitorOpacityForTest(41, new MonitorProfile
        {
            SyncWithPrimary = true,
            OverrideOpacity = 83
        });

        Assert.Equal(41, opacity);
    }

    [Fact]
    public void ResolveMonitorOpacityForTest_UsesOverrideOpacity_WhenMonitorIsNotSynced()
    {
        var opacity = TaskbarAppearanceService.ResolveMonitorOpacityForTest(41, new MonitorProfile
        {
            SyncWithPrimary = false,
            OverrideOpacity = 83
        });

        Assert.Equal(83, opacity);
    }

    [Fact]
    public void BuildMonitorOverrideLookupForTest_PreservesFirstUnsyncedMonitorPerDevice()
    {
        var first = new MonitorProfile { DeviceName = "Display2", SyncWithPrimary = false, OverrideOpacity = 44 };
        var duplicate = new MonitorProfile { DeviceName = "display2", SyncWithPrimary = false, OverrideOpacity = 88 };
        var lookup = TaskbarAppearanceService.BuildMonitorOverrideLookupForTest([
            new MonitorProfile { DeviceName = "Primary", SyncWithPrimary = true, OverrideOpacity = 12 },
            first,
            duplicate
        ]);

        Assert.NotNull(lookup);
        Assert.Single(lookup);
        Assert.Same(first, lookup["DISPLAY2"]);
    }

    [Fact]
    public void BuildMonitorOverrideLookupForTest_ReturnsNull_WhenAllMonitorsAreSynced()
    {
        Assert.Null(TaskbarAppearanceService.BuildMonitorOverrideLookupForTest(null));
        Assert.Null(TaskbarAppearanceService.BuildMonitorOverrideLookupForTest([
            new MonitorProfile { DeviceName = "Primary", SyncWithPrimary = true },
            new MonitorProfile { DeviceName = "Secondary", SyncWithPrimary = true }
        ]));
    }

    [Fact]
    public void DistinctByHandleForTest_PreservesFirstTargetPerHandle()
    {
        var first = new TaskbarWindowInfo(new IntPtr(1), "Shell_TrayWnd", "Primary", true);
        var duplicate = new TaskbarWindowInfo(new IntPtr(1), "Shell_SecondaryTrayWnd", "Duplicate", false);
        var second = new TaskbarWindowInfo(new IntPtr(2), "Shell_SecondaryTrayWnd", "Secondary", false);

        var distinct = TaskbarAppearanceService.DistinctByHandleForTest([first, duplicate, second]);

        Assert.Equal([first, second], distinct);
    }

    [Fact]
    public void ShouldApplyLayeredAlphaForTest_SkipsUnchangedAlpha()
    {
        Assert.False(TaskbarAppearanceService.ShouldApplyLayeredAlphaForTest(128, 128));
        Assert.True(TaskbarAppearanceService.ShouldApplyLayeredAlphaForTest(127, 128));
        Assert.True(TaskbarAppearanceService.ShouldApplyLayeredAlphaForTest(null, 128));
    }

    [Fact]
    public void ShouldReadLayeredStyleForTest_SkipsKnownLayeredHandles()
    {
        Assert.False(TaskbarAppearanceService.ShouldReadLayeredStyleForTest((byte?)128, 128, layeredStyleKnown: false));
        Assert.True(TaskbarAppearanceService.ShouldReadLayeredStyleForTest((byte?)127, 128, layeredStyleKnown: false));
        Assert.False(TaskbarAppearanceService.ShouldReadLayeredStyleForTest((byte?)127, 128, layeredStyleKnown: true));
        Assert.True(TaskbarAppearanceService.ShouldReadLayeredStyleForTest(null, 128, layeredStyleKnown: false));
    }

    [Fact]
    public void AppearanceRequestMatchesForTest_ChangesForOpacityAndActiveState()
    {
        Assert.True(TaskbarAppearanceService.AppearanceRequestMatchesForTest(72, true, 72, true));
        Assert.False(TaskbarAppearanceService.AppearanceRequestMatchesForTest(72, true, 73, true));
        Assert.False(TaskbarAppearanceService.AppearanceRequestMatchesForTest(72, true, 72, false));
        Assert.True(TaskbarAppearanceService.AppearanceRequestMatchesForTest(72, false, 30, false));
    }

    [Fact]
    public void TaskbarWindowCatalog_IdentifiesPrimaryTaskbarClass()
    {
        Assert.True(TaskbarWindowCatalog.IsPrimaryClassForTest(TaskbarWindowCatalog.PrimaryTaskbarClassName));
        Assert.False(TaskbarWindowCatalog.IsPrimaryClassForTest(TaskbarWindowCatalog.SecondaryTaskbarClassName));
    }

    [Fact]
    public void MonitorCatalogBuildProfilesForTest_PreservesPrimaryFirstWithoutSorting()
    {
        var secondary = new TaskbarWindowInfo(new IntPtr(2), TaskbarWindowCatalog.SecondaryTaskbarClassName, "Secondary", false);
        var primary = new TaskbarWindowInfo(new IntPtr(1), TaskbarWindowCatalog.PrimaryTaskbarClassName, "Primary", true);

        var profiles = MonitorCatalog.BuildProfilesForTest([secondary, primary]);

        Assert.Equal(2, profiles.Count);
        Assert.True(profiles[0].IsPrimary);
        Assert.Equal("Primary display", profiles[0].FriendlyName);
        Assert.Equal("Primary", profiles[0].DeviceName);
        Assert.False(profiles[1].IsPrimary);
        Assert.Equal("Display 1", profiles[1].FriendlyName);
        Assert.Equal("Secondary", profiles[1].DeviceName);
        Assert.All(profiles, profile => Assert.True(profile.SyncWithPrimary));
    }

    [Fact]
    public void MonitorCatalogGetCurrent_UsesProvidedTaskbarSnapshot()
    {
        var catalog = new MonitorCatalog();
        var profiles = catalog.GetCurrent([
            new TaskbarWindowInfo(new IntPtr(1), TaskbarWindowCatalog.PrimaryTaskbarClassName, "Primary", true),
            new TaskbarWindowInfo(new IntPtr(2), TaskbarWindowCatalog.SecondaryTaskbarClassName, "Secondary", false)
        ]);

        Assert.Equal(2, profiles.Count);
        Assert.Equal("Primary", profiles[0].DeviceName);
        Assert.Equal("Secondary", profiles[1].DeviceName);
    }

    [Fact]
    public void FindStaleHandlesForTest_ReturnsCachedHandlesMissingFromLiveSet()
    {
        var stale = TaskbarAppearanceService.FindStaleHandlesForTest(
            [new IntPtr(1), new IntPtr(2), new IntPtr(2), new IntPtr(3)],
            [new IntPtr(2), new IntPtr(4)]);

        Assert.Equal([new IntPtr(1), new IntPtr(3)], stale.OrderBy(item => item.ToInt64()).ToArray());
    }
}
