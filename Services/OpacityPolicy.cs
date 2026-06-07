using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

public static class OpacityPolicy
{
    public static byte Resolve(TaskbarProfile profile, AutomationTrigger trigger, bool automationEnabled)
    {
        if (!automationEnabled)
        {
            return profile.Opacity;
        }

        var adjustment = trigger switch
        {
            AutomationTrigger.Desktop => -12,
            AutomationTrigger.Hover => 36,
            AutomationTrigger.WindowVisible => 8,
            AutomationTrigger.WindowMaximized => 22,
            AutomationTrigger.Fullscreen => 50,
            _ => 0
        };

        return (byte)Math.Clamp(profile.Opacity + adjustment, 0, 100);
    }
}
