namespace TaskbarTransparency.Models;

public sealed class AppSettings
{
    public bool AutomationEnabled { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool ShowTrayIcon { get; set; } = true;
    public bool FullscreenOverlap { get; set; } = true;
    public bool HoverReveal { get; set; } = true;
    public int HoverDistance { get; set; } = 8;
    public string OpenHotkey { get; set; } = "Ctrl+Alt+G";
    public string ToggleHotkey { get; set; } = "Ctrl+Alt+T";
    public TaskbarProfile ActiveProfile { get; set; } = TaskbarProfile.OxygenClear;
    public List<MonitorProfile> Monitors { get; set; } = [];
}
