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
}
