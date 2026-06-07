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
        HoverDistanceSlider.Value = settings.HoverDistance;
        HoverDistanceText.Text = $"{settings.HoverDistance} px";
        FullscreenCalibrationText.Text = settings.FullscreenOverlap ? "On" : "Off";
        LastSensorText.Text = FormatTrigger(_state.Runtime.State);
        RuleConflictInfo.IsOpen = !settings.AutomationEnabled;
        RuleHealthText.Text = settings.AutomationEnabled ? "No conflicts" : "Rules paused";
        RuleHealthDetailText.Text = settings.AutomationEnabled
            ? "Rules are ready for live evaluation."
            : "Enable automation to let sensor matches apply rule opacity.";
        AutomationHistoryList.ItemsSource = _state.Runtime.RecentEvents
            .Select(item => new AutomationHistoryRow(
                item.Time.ToString("h:mm:ss tt"),
                FormatTrigger(item.State),
                $"{item.Profile} matched {item.TaskbarsUpdated} taskbar{(item.TaskbarsUpdated == 1 ? string.Empty : "s")}",
                $"{item.Opacity}%"))
            .ToList();
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

    private void HoverDistanceSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetHoverDistance(e.NewValue);
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

    private sealed record AutomationHistoryRow(string TimeText, string State, string Detail, string OpacityText);
}
