using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class MonitorsPage : Page
{
    private readonly AppState _state = ((App)Application.Current).State;

    public MonitorsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
        _state.Changed += (_, _) => DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        var monitor = _state.Monitors.FirstOrDefault();
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
                $"{FormatTrigger(item.State)} · {item.Opacity}%",
                $"{item.TaskbarsUpdated} taskbar{(item.TaskbarsUpdated == 1 ? string.Empty : "s")} updated at {item.Time:h:mm:ss tt}"))
            .ToList();

        if (monitor is null)
        {
            MonitorNameText.Text = "No display detected";
            MonitorDeviceText.Text = "No taskbar window is currently available.";
            DetectedText.Text = "Missing";
            return;
        }

        MonitorNameText.Text = monitor.FriendlyName;
        MonitorDeviceText.Text = monitor.DeviceName;
        BoundToText.Text = $"Bound to: {monitor.DeviceName}";
        TaskbarWindowText.Text = $"Taskbar window: {(monitor.IsPrimary ? "Shell_TrayWnd" : "Shell_SecondaryTrayWnd")}";
        SyncSwitch.IsOn = monitor.SyncWithPrimary;
        OverrideOpacitySlider.Value = monitor.OverrideOpacity;
        OverrideOpacityText.Text = $"{monitor.OverrideOpacity}%";
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _state.RefreshMonitors();
        Refresh();
    }

    private void OverrideOpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (OverrideOpacityText is not null)
        {
            OverrideOpacityText.Text = $"{e.NewValue:0}%";
        }
    }

    private void ApplyOverride_Click(object sender, RoutedEventArgs e)
    {
        _state.SetOpacity(OverrideOpacitySlider.Value);
    }

    private static string FormatTrigger(string state)
    {
        return state switch
        {
            nameof(AutomationTrigger.WindowVisible) => "Visible",
            nameof(AutomationTrigger.WindowMaximized) => "Maximized",
            nameof(AutomationTrigger.Fullscreen) => "Fullscreen",
            nameof(AutomationTrigger.Hover) => "Hover",
            _ => "Desktop"
        };
    }

    private sealed record MonitorOverviewRow(string Name, string Device, string SyncState, string OpacityText);
    private sealed record MonitorActionRow(string Title, string Detail);
}
