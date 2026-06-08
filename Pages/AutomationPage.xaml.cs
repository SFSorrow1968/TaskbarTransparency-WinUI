using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class AutomationPage : Page
{
    private readonly AppState _state = ((App)Microsoft.UI.Xaml.Application.Current).State;
    private bool _loading;

    public AutomationPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
        _state.Changed += (_, _) => DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        var settings = _state.Settings;
        _loading = true;
        AutomationSwitch.IsOn = settings.AutomationEnabled;
        HoverSwitch.IsOn = settings.HoverReveal;
        FullscreenSwitch.IsOn = settings.FullscreenOverlap;
        SetRule(DesktopOpacitySlider, DesktopOpacityText, RuleOpacity(settings, AutomationTrigger.Desktop));
        SetRule(HoverOpacitySlider, HoverOpacityText, RuleOpacity(settings, AutomationTrigger.Hover));
        SetRule(WindowVisibleOpacitySlider, WindowVisibleOpacityText, RuleOpacity(settings, AutomationTrigger.WindowVisible));
        SetRule(WindowMaximizedOpacitySlider, WindowMaximizedOpacityText, RuleOpacity(settings, AutomationTrigger.WindowMaximized));
        SetRule(FullscreenOpacitySlider, FullscreenOpacityText, RuleOpacity(settings, AutomationTrigger.Fullscreen));

        var runtimeState = FormatTrigger(_state.Runtime.State);
        PreviewStateText.Text = runtimeState;
        PreviewStateDetailText.Text = StateDetail(_state.Runtime.State);
        PreviewOpacityText.Text = $"{_state.Runtime.ResolvedOpacity}%";
        PreviewMatchedRuleText.Text = settings.AutomationEnabled
            ? $"Matched rule: {runtimeState} ({_state.Runtime.ResolvedOpacity}% actual)"
            : "Automation paused; the active profile opacity is applied.";

        RuleConflictInfo.IsOpen = !settings.AutomationEnabled;
        RuleHealthText.Text = settings.AutomationEnabled ? "No conflicts" : "Rules paused";
        RuleHealthDetailText.Text = settings.AutomationEnabled
            ? "Rules are ready for live evaluation."
            : "Enable automation to let sensor matches apply rule opacity.";
        _loading = false;
    }

    private void AutomationSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetAutomation(AutomationSwitch.IsOn);
        }
    }

    private void HoverSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetHoverReveal(HoverSwitch.IsOn);
        }
    }

    private void FullscreenSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetFullscreenOverlap(FullscreenSwitch.IsOn);
        }
    }

    private void Preview_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _state.ApplyNow(AutomationTrigger.WindowVisible);
    }

    private void Save_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _state.ApplyNow(AutomationTrigger.WindowVisible);
    }

    private void ResolveConflict_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _state.SetAutomation(true);
    }

    private static byte RuleOpacity(AppSettings settings, AutomationTrigger trigger)
    {
        return OpacityPolicy.Resolve(settings.ActiveProfile, trigger, automationEnabled: true);
    }

    private static void SetRule(Slider slider, TextBlock text, byte opacity)
    {
        slider.Value = opacity;
        text.Text = $"{opacity}%";
    }

    private static string FormatTrigger(string state)
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

    private static string StateDetail(string state)
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
