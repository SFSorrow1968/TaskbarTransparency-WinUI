using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TaskbarTransparency.Pages;

public sealed partial class MonitorsPage : Page
{
    public MonitorsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh()
    {
        var state = ((App)Application.Current).State;
        var monitor = state.Monitors.FirstOrDefault();
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
        var state = ((App)Application.Current).State;
        state.RefreshMonitors();
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
        ((App)Application.Current).State.SetOpacity(OverrideOpacitySlider.Value);
    }
}
