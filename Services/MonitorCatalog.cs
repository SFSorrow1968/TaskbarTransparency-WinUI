using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

public sealed class MonitorCatalog
{
    public IReadOnlyList<MonitorProfile> GetCurrent()
    {
        var taskbars = TaskbarWindowCatalog.GetCurrent();
        if (taskbars.Count == 0)
        {
            return [];
        }

        return BuildProfiles(taskbars);
    }

    public static IReadOnlyList<MonitorProfile> BuildProfilesForTest(IReadOnlyList<TaskbarWindowInfo> taskbars) => BuildProfiles(taskbars);

    private static IReadOnlyList<MonitorProfile> BuildProfiles(IReadOnlyList<TaskbarWindowInfo> taskbars)
    {
        var profiles = new List<MonitorProfile>(taskbars.Count);
        for (var index = 0; index < taskbars.Count; index++)
        {
            var taskbar = taskbars[index];
            var profile = new MonitorProfile
            {
                DeviceName = taskbar.DeviceName,
                FriendlyName = taskbar.IsPrimary ? "Primary display" : $"Display {index + 1}",
                IsPrimary = taskbar.IsPrimary,
                SyncWithPrimary = taskbar.IsPrimary,
                OverrideOpacity = taskbar.IsPrimary ? (byte)32 : (byte)64
            };

            if (profile.IsPrimary && profiles.Count > 0)
            {
                profiles.Insert(0, profile);
                continue;
            }

            profiles.Add(profile);
        }

        return profiles;
    }
}
