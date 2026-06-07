using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

public sealed class MonitorCatalog
{
    public IReadOnlyList<MonitorProfile> GetCurrent()
    {
        return
        [
            new MonitorProfile
            {
                DeviceName = "Shell_TrayWnd",
                FriendlyName = "Primary display",
                IsPrimary = true,
                SyncWithPrimary = true,
                OverrideOpacity = 32
            }
        ];
    }
}
