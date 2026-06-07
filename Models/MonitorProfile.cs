namespace TaskbarTransparency.Models;

public sealed class MonitorProfile
{
    public string DeviceName { get; set; } = "Primary display";
    public string FriendlyName { get; set; } = "Primary display";
    public bool IsPrimary { get; set; } = true;
    public bool SyncWithPrimary { get; set; } = true;
    public byte OverrideOpacity { get; set; } = 32;
}
