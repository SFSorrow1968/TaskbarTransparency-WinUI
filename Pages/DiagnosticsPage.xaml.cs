using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class DiagnosticsPage : Page
{
    private readonly AppState _state = ((App)Application.Current).State;

    public DiagnosticsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
        _state.Changed += (_, _) => DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        var runtime = _state.Runtime;
        MessageText.Text = runtime.LastMessage;
        DetailsText.Text = $"State: {runtime.State}\nProfile: {runtime.AppliedProfile}\nTaskbars updated: {runtime.TaskbarsUpdated}\nLast applied: {runtime.LastAppliedAt:O}";
        SensorTimelineList.ItemsSource = runtime.RecentEvents
            .Select(item => new SensorTimelineRow(
                item.Time.ToString("h:mm:ss tt"),
                FormatTrigger(item.State),
                $"{item.Profile} applied {item.Opacity}% opacity and updated {item.TaskbarsUpdated} taskbar{(item.TaskbarsUpdated == 1 ? string.Empty : "s")}."))
            .ToList();

        var openValid = GlobalHotkeyService.TryParse(_state.Settings.OpenHotkey, out _);
        var toggleValid = GlobalHotkeyService.TryParse(_state.Settings.ToggleHotkey, out _);
        var hotkeysReady = openValid && toggleValid;
        HotkeyRecoveryInfo.Severity = hotkeysReady ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        HotkeyRecoveryInfo.Message = hotkeysReady
            ? "Shortcut formats are valid. If Windows rejects one, reset to defaults and try again."
            : "One or more shortcuts cannot be parsed. Reset to restore the default registrations.";
        HotkeyStatusText.Text = hotkeysReady ? "Formats ready" : "Needs reset";
        HotkeyDetailText.Text = $"Open: {_state.Settings.OpenHotkey}\nToggle: {_state.Settings.ToggleHotkey}";
    }

    private void ApplyDesktop_Click(object sender, RoutedEventArgs e) => _state.ApplyNow(AutomationTrigger.Desktop);
    private void ApplyHover_Click(object sender, RoutedEventArgs e) => _state.ApplyNow(AutomationTrigger.Hover);
    private void ApplyFullscreen_Click(object sender, RoutedEventArgs e) => _state.ApplyNow(AutomationTrigger.Fullscreen);
    private void ResetHotkeys_Click(object sender, RoutedEventArgs e) => _state.ResetHotkeys();

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

    private sealed record SensorTimelineRow(string TimeText, string State, string Detail);
}
