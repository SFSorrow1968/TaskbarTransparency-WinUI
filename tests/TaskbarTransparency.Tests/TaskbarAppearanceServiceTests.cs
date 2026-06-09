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
    public void EaseProgressForTest_UsesLinearProgress_WhenRequested()
    {
        var progress = TaskbarAppearanceService.EaseProgressForTest(0.5, "Linear");

        Assert.Equal(0.5, progress, precision: 3);
    }

    [Fact]
    public void EaseProgressForTest_UsesCubicOutFallback_ForSmoothFade()
    {
        var progress = TaskbarAppearanceService.EaseProgressForTest(0.5, "CubicOut");

        Assert.Equal(0.875, progress, precision: 3);
    }

    [Fact]
    public void SelectFadeMillisecondsForTest_UsesFadeIn_WhenOpacityIncreases()
    {
        var profile = TaskbarProfile.FocusGlass with
        {
            FadeInMilliseconds = 400,
            FadeOutMilliseconds = 90
        };

        var duration = TaskbarAppearanceService.SelectFadeMillisecondsForTest(profile, startAlpha: 80, targetAlpha: 160);

        Assert.Equal(400, duration);
    }

    [Fact]
    public void SelectFadeMillisecondsForTest_UsesFadeOut_WhenOpacityDecreases()
    {
        var profile = TaskbarProfile.FocusGlass with
        {
            FadeInMilliseconds = 400,
            FadeOutMilliseconds = 90
        };

        var duration = TaskbarAppearanceService.SelectFadeMillisecondsForTest(profile, startAlpha: 160, targetAlpha: 80);

        Assert.Equal(90, duration);
    }

    [Fact]
    public void SelectFadeDurationsForTest_UsesEachTaskbarDirection()
    {
        var profile = TaskbarProfile.FocusGlass with
        {
            FadeInMilliseconds = 400,
            FadeOutMilliseconds = 90
        };
        var primary = new IntPtr(1);
        var secondary = new IntPtr(2);
        var starts = new Dictionary<IntPtr, byte>
        {
            [primary] = 80,
            [secondary] = 160
        };
        var targets = new Dictionary<IntPtr, byte>
        {
            [primary] = 80,
            [secondary] = 70
        };

        var durations = TaskbarAppearanceService.SelectFadeDurationsForTest(profile, starts, targets);

        Assert.Equal(0, durations[primary]);
        Assert.Equal(90, durations[secondary]);
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
    public void NeedsMonitorOverrideLookupForTest_SkipsDefaultSyncedMonitorSets()
    {
        Assert.False(TaskbarAppearanceService.NeedsMonitorOverrideLookupForTest(null));
        Assert.False(TaskbarAppearanceService.NeedsMonitorOverrideLookupForTest([
            new MonitorProfile { DeviceName = "Primary", SyncWithPrimary = true },
            new MonitorProfile { DeviceName = "Secondary", SyncWithPrimary = true }
        ]));
        Assert.True(TaskbarAppearanceService.NeedsMonitorOverrideLookupForTest([
            new MonitorProfile { DeviceName = "Secondary", SyncWithPrimary = false }
        ]));
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
    public void CreateDiagnosticsForTest_ComputesSkipRatiosAndClampsCounts()
    {
        var diagnostics = TaskbarAppearanceService.CreateDiagnosticsForTest(
            targetCount: 2,
            compositionApplied: 1,
            compositionSkipped: 3,
            layeredAlphaChanges: -5,
            layeredAlphaNoOps: 2,
            monitorLookupBuilt: true,
            animationStarted: false);

        Assert.Equal(2, diagnostics.TargetCount);
        Assert.Equal(0.75, diagnostics.CompositionSkipRatio, precision: 3);
        Assert.Equal(1d, diagnostics.LayeredAlphaSkipRatio, precision: 3);
        Assert.True(diagnostics.MonitorLookupBuilt);
        Assert.False(diagnostics.AnimationStarted);
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
    public void AppearanceRequestMatchesForTest_ChangesOnlyForNativeCompositionInputs()
    {
        var first = TaskbarProfile.FocusGlass with { FadeInMilliseconds = 50, FadeOutMilliseconds = 100, Easing = "Linear" };
        var sameNativeRequest = first with { FadeInMilliseconds = 500, FadeOutMilliseconds = 900, Easing = "QuintOut" };
        var differentOpacity = first with { Opacity = 73 };
        var differentMode = first with { Mode = TaskbarVisualMode.Solid };

        Assert.True(TaskbarAppearanceService.AppearanceRequestMatchesForTest(first, 72, sameNativeRequest, 72));
        Assert.False(TaskbarAppearanceService.AppearanceRequestMatchesForTest(first, 72, differentOpacity, 73));
        Assert.False(TaskbarAppearanceService.AppearanceRequestMatchesForTest(first, 72, differentMode, 72));
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
