using System.Diagnostics;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class DiagnosticsPage : Page
{
    private readonly AppState _state = ((App)Application.Current).State;
    private readonly RefreshCoalescer _refreshCoalescer = new();
    private int _timelineVersion = -1;

    public DiagnosticsPage()
    {
        InitializeComponent();
        Loaded += Page_Loaded;
        Unloaded += Page_Unloaded;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _state.Changed += State_Changed;
        Refresh();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _state.Changed -= State_Changed;
    }

    private void State_Changed(object? sender, EventArgs e)
    {
        _refreshCoalescer.Request(action => DispatcherQueue.TryEnqueue(() => action()), Refresh);
    }

    private void Refresh()
    {
        var runtime = _state.Runtime;
        var taskbarsFound = runtime.TaskbarsUpdated > 0;
        MessageText.Text = runtime.LastMessage;
        SubtitleText.Text = taskbarsFound
            ? $"Oxygen Taskbar updated {runtime.TaskbarsUpdated} taskbar{(runtime.TaskbarsUpdated == 1 ? string.Empty : "s")} and recorded the result below."
            : "Oxygen Taskbar could not find a valid taskbar window to attach to.";
        HeaderIcon.Glyph = taskbarsFound ? "\uE930" : "\uE783";
        HeaderIcon.Foreground = (Brush)Application.Current.Resources[taskbarsFound ? "OxygenMintBrush" : "OxygenDangerBrush"];
        HeaderIconBorder.Background = new SolidColorBrush(taskbarsFound
            ? Color.FromArgb(0x33, 0x23, 0xC5, 0x8E)
            : Color.FromArgb(0x33, 0xFF, 0x6B, 0x78));
        var recovery = DiagnosticsRecoveryState.FromTaskbarUpdateCount(runtime.TaskbarsUpdated);
        RecoveryTitleText.Text = recovery.Title;
        RecoveryPrimaryDetailText.Text = recovery.PrimaryDetail;
        RecoverySecondaryDetailText.Text = recovery.SecondaryDetail;
        RecoveryTertiaryDetailText.Text = recovery.TertiaryDetail;
        RecoveryPrimaryButton.Content = recovery.PrimaryAction;
        RecoverySecondaryButton.Content = recovery.SecondaryAction;
        RecoverySecondaryButton.Visibility = recovery.ShowSecondaryAction ? Visibility.Visible : Visibility.Collapsed;
        RecoveryHelperText.Text = recovery.HelperText;
        DetailsText.Text = $"State: {runtime.State}\nProfile: {runtime.AppliedProfile}\nTaskbars updated: {runtime.TaskbarsUpdated}\nLast applied: {runtime.LastAppliedAt:O}\n{ApplyDiagnosticsText(_state.ApplyDiagnostics)}";
        if (_timelineVersion != runtime.RecentEventsVersion)
        {
            _timelineVersion = runtime.RecentEventsVersion;
            SensorTimelineList.ItemsSource = runtime.RecentEvents
                .Select(item => new SensorTimelineRow(
                    item.Time.ToString("h:mm:ss tt"),
                    RuntimeTriggerText.Label(item.State),
                    $"{item.Profile} applied {item.Opacity}% opacity and updated {item.TaskbarsUpdated} taskbar{(item.TaskbarsUpdated == 1 ? string.Empty : "s")}."))
                .ToList();
        }

        var hotkeyStatus = _state.HotkeyStatus;
        var hotkeysReady = hotkeyStatus.IsReady;
        HotkeyRecoveryInfo.Severity = hotkeysReady ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        HotkeyRecoveryInfo.Message = hotkeysReady
            ? "Both shortcuts are registered with Windows and ready."
            : "One or more shortcuts could not be registered. Reset to defaults, then retry after closing conflicting apps.";
        HotkeyStatusText.Text = hotkeysReady ? "Registered" : "Needs attention";
        HotkeyDetailText.Text = $"Open: {hotkeyStatus.Open.Summary}\nToggle: {hotkeyStatus.Toggle.Summary}";
    }

    private void ApplyDesktop_Click(object sender, RoutedEventArgs e) => _state.ApplyNow(AutomationTrigger.Desktop);
    private void ApplyHover_Click(object sender, RoutedEventArgs e) => _state.ApplyNow(AutomationTrigger.Hover);
    private void ApplyFullscreen_Click(object sender, RoutedEventArgs e) => _state.ApplyNow(AutomationTrigger.Fullscreen);
    private void ResetHotkeys_Click(object sender, RoutedEventArgs e) => _state.ResetHotkeys();
    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = FindLauncherLogFolder();
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }

    private static string FindLauncherLogFolder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "launcher-logs");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "launcher-logs");
    }

    private static string ApplyDiagnosticsText(TaskbarApplyDiagnostics diagnostics)
    {
        return $"Apply diagnostics: targets {diagnostics.TargetCount}, composition {diagnostics.CompositionApplied} applied/{diagnostics.CompositionSkipped} skipped ({diagnostics.CompositionSkipRatio:P0} skipped), alpha {diagnostics.LayeredAlphaChanges} queued/{diagnostics.LayeredAlphaNoOps} skipped ({diagnostics.LayeredAlphaSkipRatio:P0} skipped), monitor lookup {(diagnostics.MonitorLookupBuilt ? "built" : "skipped")}, animation {(diagnostics.AnimationStarted ? "started" : "not started")}.";
    }

    private sealed record SensorTimelineRow(string TimeText, string State, string Detail);
}
