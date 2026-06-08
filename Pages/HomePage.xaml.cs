using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TaskbarTransparency.Pages;

public sealed partial class HomePage : Page
{
    private readonly AppState _state = ((App)Microsoft.UI.Xaml.Application.Current).State;
    private readonly DispatcherQueueTimer _opacityCommitTimer;
    private double _pendingOpacity;
    private string _recentEventsSignature = string.Empty;
    private bool _loading = true;

    public HomePage()
    {
        InitializeComponent();
        _opacityCommitTimer = DispatcherQueue.CreateTimer();
        _opacityCommitTimer.Interval = TimeSpan.FromMilliseconds(350);
        _opacityCommitTimer.Tick += CommitPendingOpacity;
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
        if (_opacityCommitTimer.IsRunning)
        {
            _opacityCommitTimer.Stop();
            _state.SetOpacity(_pendingOpacity);
        }
    }

    private void State_Changed(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        _loading = true;
        ProfileText.Text = _state.Settings.ActiveProfile.Name;
        OpacityText.Text = $"{_state.Settings.ActiveProfile.Opacity}%";
        OpacityValueBoxText.Text = $"{_state.Settings.ActiveProfile.Opacity}%";
        TaskbarsText.Text = _state.Runtime.TaskbarsUpdated.ToString();
        ServiceStatusText.Text = _state.Runtime.TaskbarsUpdated > 0 ? "Running" : "Waiting for taskbar";
        CurrentMaterialText.Text = _state.Settings.ActiveProfile.Mode.ToString();
        CurrentTriggerText.Text = RuntimeTriggerText.Label(_state.Runtime.State);
        ResolvedOpacityText.Text = $"{_state.Runtime.ResolvedOpacity}%";
        SensorDetailText.Text = _state.Settings.AutomationEnabled ? NextSensorHint(_state.Runtime.State) : "Automation is paused";
        AutomationStatusText.Text = _state.Settings.AutomationEnabled ? "Enabled" : "Paused";
        AutomationDetailText.Text = _state.Settings.AutomationEnabled ? "Sensors are live" : "Manual apply only";
        RuntimeMessageText.Text = _state.Runtime.LastMessage;
        RuntimeTimeText.Text = _state.Runtime.LastAppliedAt.ToString("MMM d, h:mm tt");
        SyncStateText.Text = _state.Monitors.Count <= 1 ? "Primary taskbar in sync" : "All taskbars in sync";
        var recentEventsSignature = RuntimeEventsSignature();
        if (_recentEventsSignature != recentEventsSignature)
        {
            _recentEventsSignature = recentEventsSignature;
            RecentEventsList.ItemsSource = _state.Runtime.RecentEvents
                .Select(item => new RuntimeEventRow(
                    item.Time.ToString("h:mm:ss tt"),
                    RuntimeTriggerText.Label(item.State),
                    $"{item.Profile} - {item.Message} - {item.TaskbarsUpdated} taskbar{(item.TaskbarsUpdated == 1 ? string.Empty : "s")}",
                    $"{item.Opacity}%"))
                .ToList();
        }

        OpacitySlider.Value = _state.Settings.ActiveProfile.Opacity;
        _loading = false;
    }

    private void OpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            _pendingOpacity = e.NewValue;
            OpacityText.Text = $"{e.NewValue:0}%";
            OpacityValueBoxText.Text = $"{e.NewValue:0}%";
            _state.PreviewOpacity(e.NewValue);
            _opacityCommitTimer.Stop();
            _opacityCommitTimer.Start();
        }
    }

    private void CommitPendingOpacity(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        _state.SetOpacity(_pendingOpacity);
    }

    private void ApplyClear_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => _state.SetProfile(TaskbarProfile.OxygenClear);
    private void ApplyGlass_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => _state.SetProfile(TaskbarProfile.FocusGlass);
    private void ApplySolid_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => _state.SetProfile(TaskbarProfile.NightSolid);
    private void ApplyNow_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => _state.ReapplyCurrentRuntimeState();

    private static string NextSensorHint(string state)
    {
        return state switch
        {
            nameof(AutomationTrigger.WindowVisible) => "Watching maximize, fullscreen, and hover",
            nameof(AutomationTrigger.WindowMaximized) => "Watching fullscreen and hover",
            nameof(AutomationTrigger.Fullscreen) => "Fullscreen overlap policy is active",
            nameof(AutomationTrigger.Hover) => "Pointer is near a taskbar edge",
            _ => "Watching windows, fullscreen, and hover"
        };
    }

    private string RuntimeEventsSignature()
    {
        return string.Join('|', _state.Runtime.RecentEvents.Select(item =>
            $"{item.Time.UtcTicks}:{item.State}:{item.Profile}:{item.Opacity}:{item.TaskbarsUpdated}:{item.Message}"));
    }

    private sealed record RuntimeEventRow(string TimeText, string State, string Detail, string OpacityText);
}
