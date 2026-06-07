namespace TaskbarTransparency.Models;

public sealed record TaskbarProfile(
    string Name,
    TaskbarVisualMode Mode,
    byte Opacity,
    string AccentHex,
    int FadeMilliseconds,
    string Easing)
{
    public static TaskbarProfile OxygenClear { get; } = new("Oxygen Clear", TaskbarVisualMode.Clear, 32, "#FFFFFF", 180, "CubicOut");
    public static TaskbarProfile FocusGlass { get; } = new("Focus Glass", TaskbarVisualMode.Acrylic, 72, "#101318", 220, "QuintOut");
    public static TaskbarProfile NightSolid { get; } = new("Night Solid", TaskbarVisualMode.Solid, 92, "#111827", 120, "Linear");
}
