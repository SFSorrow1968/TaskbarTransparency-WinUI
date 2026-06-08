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

        var profiles = new List<MonitorProfile>();
        for (var index = 0; index < taskbars.Count; index++)
        {
            var taskbar = taskbars[index];
            profiles.Add(new MonitorProfile
            {
                DeviceName = taskbar.DeviceName,
                FriendlyName = taskbar.IsPrimary ? "Primary display" : $"Display {index + 1}",
                IsPrimary = taskbar.IsPrimary,
                SyncWithPrimary = taskbar.IsPrimary,
                OverrideOpacity = taskbar.IsPrimary ? (byte)32 : (byte)64
            });
        }

        return profiles
            .OrderByDescending(profile => profile.IsPrimary)
            .ThenBy(profile => profile.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
