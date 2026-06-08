using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class AutomationPage : Page
{
    private readonly AppState _state = ((App)Microsoft.UI.Xaml.Application.Current).State;

    public AutomationPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
        _state.Changed += (_, _) => DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        var settings = _state.Settings;
        AutomationRuleText.Text = settings.AutomationEnabled ? "Automation" : "Paused";
        HoverRuleText.Text = settings.HoverReveal ? "Hover sensor" : "Hover off";
        FullscreenRuleText.Text = settings.FullscreenOverlap ? "Fullscreen sensor" : "Fullscreen off";
        SetRule(DesktopOpacityBar, DesktopOpacityText, RuleOpacity(settings, AutomationTrigger.Desktop));
        SetRule(HoverOpacityBar, HoverOpacityText, RuleOpacity(settings, AutomationTrigger.Hover));
        SetRule(WindowVisibleOpacityBar, WindowVisibleOpacityText, RuleOpacity(settings, AutomationTrigger.WindowVisible));
        SetRule(WindowMaximizedOpacityBar, WindowMaximizedOpacityText, RuleOpacity(settings, AutomationTrigger.WindowMaximized));
        SetRule(FullscreenOpacityBar, FullscreenOpacityText, RuleOpacity(settings, AutomationTrigger.Fullscreen));

        var runtimeState = RuntimeTriggerText.Label(_state.Runtime.State);
        PreviewStateText.Text = runtimeState;
        PreviewStateDetailText.Text = RuntimeTriggerText.Detail(_state.Runtime.State);
        PreviewOpacityText.Text = $"{_state.Runtime.ResolvedOpacity}%";
        PreviewMatchedRuleText.Text = settings.AutomationEnabled
            ? $"Matched rule: {runtimeState} ({_state.Runtime.ResolvedOpacity}% actual)"
            : "Automation paused; the active profile opacity is applied.";

        RuleConflictInfo.IsOpen = !settings.AutomationEnabled;
        ResolveConflictButton.IsEnabled = !settings.AutomationEnabled;
        ResolveConflictButton.Visibility = settings.AutomationEnabled
            ? Microsoft.UI.Xaml.Visibility.Collapsed
            : Microsoft.UI.Xaml.Visibility.Visible;
        RuleHealthText.Text = settings.AutomationEnabled ? "No conflicts" : "Rules paused";
        RuleHealthDetailText.Text = settings.AutomationEnabled
            ? "Rules are ready for live evaluation."
            : "Enable automation to let sensor matches apply rule opacity.";
    }

    private void Preview_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _state.ApplyNow(AutomationTrigger.WindowVisible);
    }

    private void OpenTuning_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _state.RequestView(AppView.Tuning);
    }

    private void ResolveConflict_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _state.SetAutomation(true);
    }

    private static byte RuleOpacity(AppSettings settings, AutomationTrigger trigger)
    {
        return OpacityPolicy.Resolve(settings.ActiveProfile, trigger, automationEnabled: true);
    }

    private static void SetRule(ProgressBar bar, TextBlock text, byte opacity)
    {
        bar.Value = opacity;
        text.Text = $"{opacity}%";
    }

}
