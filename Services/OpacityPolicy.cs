using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

public sealed record OpacityResolution(byte Opacity, string Source);

public static class OpacityPolicy
{
    public const string BaseSource = "Base opacity";
    public const string HoverSource = "Hover rule";
    public const string FullscreenSource = "Fullscreen rule";
    public const string MaximizedSource = "Maximized window rule";
    public const string WindowSource = "Open window rule";

    public static OpacityResolution Resolve(AppSettings settings, AutomationTrigger trigger)
    {
        if (!settings.AutomationEnabled)
        {
            return new OpacityResolution(ClampOpacity(settings.BaseOpacity), BaseSource);
        }

        return trigger switch
        {
            AutomationTrigger.Hover when settings.HoverRule.Enabled
                => FromRule(settings.HoverRule, HoverSource),
            AutomationTrigger.Fullscreen when settings.FullscreenRule.Enabled
                => FromRule(settings.FullscreenRule, FullscreenSource),
            AutomationTrigger.Fullscreen or AutomationTrigger.WindowMaximized when settings.MaximizedRule.Enabled
                => FromRule(settings.MaximizedRule, MaximizedSource),
            AutomationTrigger.Fullscreen or AutomationTrigger.WindowMaximized or AutomationTrigger.WindowVisible when settings.WindowRule.Enabled
                => FromRule(settings.WindowRule, WindowSource),
            _ => new OpacityResolution(ClampOpacity(settings.BaseOpacity), BaseSource)
        };
    }

    private static OpacityResolution FromRule(OpacityRule rule, string source)
    {
        return new OpacityResolution(ClampOpacity(rule.Opacity), source);
    }

    private static byte ClampOpacity(byte opacity)
    {
        return Math.Clamp(opacity, (byte)0, (byte)100);
    }
}
