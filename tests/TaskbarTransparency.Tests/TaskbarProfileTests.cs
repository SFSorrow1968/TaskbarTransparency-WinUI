using TaskbarTransparency.Models;

namespace TaskbarTransparency.Tests;

public sealed class TaskbarProfileTests
{
    [Fact]
    public void NormalizeTransitions_MigratesLegacyFadeMilliseconds()
    {
        var profile = TaskbarProfile.OxygenClear with
        {
            FadeMilliseconds = 180,
            FadeInMilliseconds = 0,
            FadeOutMilliseconds = 0
        };

        var normalized = profile.NormalizeTransitions();

        Assert.Equal(180, normalized.FadeInMilliseconds);
        Assert.Equal(180, normalized.FadeOutMilliseconds);
    }

    [Fact]
    public void NormalizeTransitions_PreservesExplicitInstantFades()
    {
        var profile = TaskbarProfile.OxygenClear with
        {
            FadeMilliseconds = 0,
            FadeInMilliseconds = 0,
            FadeOutMilliseconds = 0
        };

        var normalized = profile.NormalizeTransitions();

        Assert.Equal(0, normalized.FadeInMilliseconds);
        Assert.Equal(0, normalized.FadeOutMilliseconds);
    }

    [Fact]
    public void WithTuningValues_PreservesCurrentMaterialAndAccent()
    {
        var tuned = TaskbarProfile.OxygenClear.WithTuningValues("  Work Clear  ", 44, 310, 90, "Linear");

        Assert.Equal("Work Clear", tuned.Name);
        Assert.Equal(TaskbarVisualMode.Clear, tuned.Mode);
        Assert.Equal("#FFFFFF", tuned.AccentHex);
        Assert.Equal(44, tuned.Opacity);
        Assert.Equal(310, tuned.FadeMilliseconds);
        Assert.Equal(310, tuned.FadeInMilliseconds);
        Assert.Equal(90, tuned.FadeOutMilliseconds);
        Assert.Equal("Linear", tuned.Easing);
    }

    [Fact]
    public void WithTuningValues_KeepsExistingName_WhenNameIsBlank()
    {
        var tuned = TaskbarProfile.FocusGlass.WithTuningValues(" ", 55, 120, 80, "CubicOut");

        Assert.Equal(TaskbarProfile.FocusGlass.Name, tuned.Name);
    }

    [Fact]
    public void WithVisualMode_PreservesTuningValues()
    {
        var profile = TaskbarProfile.OxygenClear.WithTuningValues("Quiet", 63, 260, 140, "Linear");

        var mica = profile.WithVisualMode(TaskbarVisualMode.Mica);

        Assert.Equal("Quiet", mica.Name);
        Assert.Equal(TaskbarVisualMode.Mica, mica.Mode);
        Assert.Equal(63, mica.Opacity);
        Assert.Equal(260, mica.FadeInMilliseconds);
        Assert.Equal(140, mica.FadeOutMilliseconds);
        Assert.Equal("Linear", mica.Easing);
    }

    [Fact]
    public void MergeDetected_PreservesSavedOverrideSettings()
    {
        var detected = new MonitorProfile
        {
            DeviceName = @"\\.\DISPLAY2",
            FriendlyName = "Detected display",
            IsPrimary = false,
            SyncWithPrimary = true,
            OverrideOpacity = 64
        };
        var saved = new MonitorProfile
        {
            DeviceName = @"\\.\DISPLAY2",
            FriendlyName = "Old display",
            IsPrimary = true,
            SyncWithPrimary = false,
            OverrideOpacity = 81
        };

        var merged = MonitorProfile.MergeDetected(detected, saved);

        Assert.Equal(@"\\.\DISPLAY2", merged.DeviceName);
        Assert.Equal("Detected display", merged.FriendlyName);
        Assert.False(merged.IsPrimary);
        Assert.False(merged.SyncWithPrimary);
        Assert.Equal(81, merged.OverrideOpacity);
    }

    [Fact]
    public void SequenceMatches_DetectsMonitorListChanges()
    {
        var left = new[]
        {
            new MonitorProfile { DeviceName = "A", FriendlyName = "Primary", IsPrimary = true, SyncWithPrimary = true, OverrideOpacity = 32 }
        };
        var same = new[]
        {
            new MonitorProfile { DeviceName = "A", FriendlyName = "Primary", IsPrimary = true, SyncWithPrimary = true, OverrideOpacity = 32 }
        };
        var different = new[]
        {
            new MonitorProfile { DeviceName = "A", FriendlyName = "Primary", IsPrimary = true, SyncWithPrimary = false, OverrideOpacity = 32 }
        };

        Assert.True(MonitorProfile.SequenceMatches(left, same));
        Assert.False(MonitorProfile.SequenceMatches(left, different));
    }

    [Fact]
    public void SequenceMatches_SupportsDeferredEnumerables()
    {
        static IEnumerable<MonitorProfile> Enumerate(params MonitorProfile[] monitors)
        {
            foreach (var monitor in monitors)
            {
                yield return monitor;
            }
        }

        var left = Enumerate(new MonitorProfile { DeviceName = "A", FriendlyName = "Primary", IsPrimary = true });
        var same = Enumerate(new MonitorProfile { DeviceName = "A", FriendlyName = "Primary", IsPrimary = true });
        var extra = Enumerate(
            new MonitorProfile { DeviceName = "A", FriendlyName = "Primary", IsPrimary = true },
            new MonitorProfile { DeviceName = "B", FriendlyName = "Display 2", IsPrimary = false });

        Assert.True(MonitorProfile.SequenceMatches(left, same));
        Assert.False(MonitorProfile.SequenceMatches(same, extra));
    }
}
