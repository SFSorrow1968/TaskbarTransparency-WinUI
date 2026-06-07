namespace TaskbarTransparency.Models;

public sealed record TaskbarProfile(
    string Name,
    TaskbarVisualMode Mode,
    byte Opacity,
    string AccentHex,
    int FadeMilliseconds,
    string Easing)
{
    public int FadeInMilliseconds { get; init; } = FadeMilliseconds;
    public int FadeOutMilliseconds { get; init; } = FadeMilliseconds;

    public TaskbarProfile NormalizeTransitions()
    {
        if (FadeMilliseconds <= 0)
        {
            return this;
        }

        return this with
        {
            FadeInMilliseconds = FadeInMilliseconds <= 0 ? FadeMilliseconds : FadeInMilliseconds,
            FadeOutMilliseconds = FadeOutMilliseconds <= 0 ? FadeMilliseconds : FadeOutMilliseconds
        };
    }

    public static TaskbarProfile OxygenClear { get; } = new("Oxygen Clear", TaskbarVisualMode.Clear, 32, "#FFFFFF", 180, "CubicOut")
    {
        FadeInMilliseconds = 180,
        FadeOutMilliseconds = 180
    };

    public static TaskbarProfile FocusGlass { get; } = new("Focus Glass", TaskbarVisualMode.Acrylic, 72, "#101318", 220, "QuintOut")
    {
        FadeInMilliseconds = 220,
        FadeOutMilliseconds = 180
    };

    public static TaskbarProfile NightSolid { get; } = new("Night Solid", TaskbarVisualMode.Solid, 92, "#111827", 120, "Linear")
    {
        FadeInMilliseconds = 120,
        FadeOutMilliseconds = 120
    };
}
