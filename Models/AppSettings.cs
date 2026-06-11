namespace TaskbarTransparency.Models;

public sealed class AppSettings
{
    public byte BaseOpacity { get; set; } = 30;
    public int FadeInMilliseconds { get; set; } = 200;
    public int FadeOutMilliseconds { get; set; } = 200;
    public bool AutomationEnabled { get; set; } = true;
    public OpacityRule HoverRule { get; set; } = new() { Enabled = true, Opacity = 100 };
    public bool HoverSyncAcrossMonitors { get; set; }
    public OpacityRule FullscreenRule { get; set; } = new() { Enabled = true, Opacity = 100 };
    public OpacityRule MaximizedRule { get; set; } = new() { Enabled = true, Opacity = 60 };
    public OpacityRule WindowRule { get; set; } = new() { Enabled = false, Opacity = 45 };
    public int HoverDistance { get; set; } = 8;
    public bool StartWithWindows { get; set; }
    public bool ShowTrayIcon { get; set; } = true;
    public string OpenHotkey { get; set; } = "Ctrl+Alt+G";
    public string ToggleHotkey { get; set; } = "Ctrl+Alt+T";
    public List<MonitorProfile> Monitors { get; set; } = [];

    public OpacityRule? RuleFor(AutomationTrigger trigger)
    {
        return trigger switch
        {
            AutomationTrigger.Hover => HoverRule,
            AutomationTrigger.Fullscreen => FullscreenRule,
            AutomationTrigger.WindowMaximized => MaximizedRule,
            AutomationTrigger.WindowVisible => WindowRule,
            _ => null
        };
    }

    public void Normalize()
    {
        BaseOpacity = Math.Clamp(BaseOpacity, (byte)0, (byte)100);
        FadeInMilliseconds = Math.Clamp(FadeInMilliseconds, 0, 1000);
        FadeOutMilliseconds = Math.Clamp(FadeOutMilliseconds, 0, 1000);
        HoverDistance = Math.Clamp(HoverDistance, 0, 48);
        HoverRule.Normalize();
        FullscreenRule.Normalize();
        MaximizedRule.Normalize();
        WindowRule.Normalize();
    }
}
