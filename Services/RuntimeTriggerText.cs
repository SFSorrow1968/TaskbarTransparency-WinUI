using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

public static class RuntimeTriggerText
{
    public static AutomationTrigger Parse(string state)
    {
        return Enum.TryParse<AutomationTrigger>(state, ignoreCase: false, out var trigger)
            ? trigger
            : AutomationTrigger.Desktop;
    }

    public static string Label(string state)
    {
        return state switch
        {
            nameof(AutomationTrigger.WindowVisible) => "Visible window",
            nameof(AutomationTrigger.WindowMaximized) => "Maximized window",
            nameof(AutomationTrigger.Fullscreen) => "Fullscreen",
            nameof(AutomationTrigger.Hover) => "Hover",
            _ => "Desktop"
        };
    }

    public static string Detail(string state)
    {
        return state switch
        {
            nameof(AutomationTrigger.WindowVisible) => "A window is open and not maximized.",
            nameof(AutomationTrigger.WindowMaximized) => "The active window is maximized on a detected monitor.",
            nameof(AutomationTrigger.Fullscreen) => "The active window is covering the monitor as fullscreen.",
            nameof(AutomationTrigger.Hover) => "The pointer is inside the saved taskbar hover proximity.",
            _ => "No foreground window rule is currently taking priority."
        };
    }
}
