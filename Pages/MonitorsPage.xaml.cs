using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class MonitorsPage : Page
{
    private readonly AppState _state = ((App)Application.Current).State;
    private bool _loading;

    public MonitorsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
        _state.Changed += (_, _) => DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        _loading = true;
        var monitor = SelectedMonitor();
        TotalDisplaysText.Text = _state.Monitors.Count.ToString();
        SyncedDisplaysText.Text = _state.Monitors.Count(item => item.SyncWithPrimary).ToString();
        TaskbarsUpdatedText.Text = _state.Runtime.TaskbarsUpdated.ToString();
        MonitorOverviewList.ItemsSource = _state.Monitors
            .Select(item => new MonitorOverviewRow(
                item.FriendlyName,
                item.DeviceName,
                item.SyncWithPrimary ? "Synced" : "Override",
                $"{item.OverrideOpacity}%"))
            .ToList();
        RecentMonitorActionsList.ItemsSource = _state.Runtime.RecentEvents
            .Select(item => new MonitorActionRow(
                $"{RuntimeTriggerText.Label(item.State)} - {item.Opacity}%",
                $"{item.TaskbarsUpdated} taskbar{(item.TaskbarsUpdated == 1 ? string.Empty : "s")} updated at {item.Time:h:mm:ss tt}"))
            .ToList();

        if (monitor is null)
        {
            PageTitleText.Text = "No display detected";
            MonitorNameText.Text = "No display detected";
            MonitorDeviceText.Text = "No taskbar window is currently available.";
            DetectedText.Text = "Missing";
            OverrideScopeText.Text = "No display override can be applied until a taskbar is detected.";
            _loading = false;
            return;
        }

        PageTitleText.Text = monitor.FriendlyName;
        MonitorNameText.Text = monitor.FriendlyName;
        MonitorDeviceText.Text = monitor.DeviceName;
        DisplayBadgeText.Text = monitor.FriendlyName;
        BoundToText.Text = $"Bound to: {monitor.DeviceName}";
        TaskbarWindowText.Text = $"Taskbar window: {(monitor.IsPrimary ? "Shell_TrayWnd" : "Shell_SecondaryTrayWnd")}";
        SyncSwitch.IsOn = monitor.SyncWithPrimary;
        OverrideOpacitySlider.Value = monitor.OverrideOpacity;
        OverrideOpacityText.Text = $"{monitor.OverrideOpacity}%";
        OverrideScopeText.Text = monitor.SyncWithPrimary
            ? $"{monitor.FriendlyName} follows the primary display until sync is turned off and applied."
            : $"This override affects {monitor.FriendlyName} only.";
        UpdateOverrideControlState();
        _loading = false;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        Refresh();
    }

    private void OverrideOpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (OverrideOpacityText is not null)
        {
            OverrideOpacityText.Text = $"{e.NewValue:0}%";
        }
    }

    private void SyncSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            UpdateOverrideControlState();
        }
    }

    private void ApplyOverride_Click(object sender, RoutedEventArgs e)
    {
        var monitor = SelectedMonitor();
        if (monitor is not null)
        {
            _state.SetMonitorOverride(monitor.DeviceName, OverrideOpacitySlider.Value, SyncSwitch.IsOn);
        }
    }

    private MonitorProfile? SelectedMonitor()
    {
        return _state.Monitors.FirstOrDefault(item => !item.IsPrimary)
            ?? _state.Monitors.FirstOrDefault();
    }

    private void UpdateOverrideControlState()
    {
        OverrideOpacitySlider.IsEnabled = !SyncSwitch.IsOn;
    }

    private sealed record MonitorOverviewRow(string Name, string Device, string SyncState, string OpacityText);
    private sealed record MonitorActionRow(string Title, string Detail);
}
