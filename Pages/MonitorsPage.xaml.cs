using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class MonitorsPage : Page
{
    private readonly AppState _state = ((App)Application.Current).State;
    private readonly RefreshCoalescer _refreshCoalescer = new();
    private readonly DispatcherQueueTimer _overrideCommitTimer;
    private MonitorRow? _pendingOverrideRow;
    private int _monitorListVersion = -1;
    private bool _loading;

    public MonitorsPage()
    {
        InitializeComponent();
        _overrideCommitTimer = DispatcherQueue.CreateTimer();
        _overrideCommitTimer.Interval = TimeSpan.FromMilliseconds(350);
        _overrideCommitTimer.Tick += CommitPendingOverride;
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
        if (_overrideCommitTimer.IsRunning)
        {
            _overrideCommitTimer.Stop();
            CommitOverride();
        }
    }

    private void State_Changed(object? sender, EventArgs e)
    {
        _refreshCoalescer.Request(action => DispatcherQueue.TryEnqueue(() => action()), Refresh);
    }

    private void Refresh()
    {
        _loading = true;
        var monitors = _state.Monitors;
        var synced = MonitorProfile.CountSynced(monitors);
        SummaryText.Text = monitors.Count == 0
            ? "Each display follows the base opacity unless you give it its own value."
            : $"{monitors.Count} display{(monitors.Count == 1 ? string.Empty : "s")} detected · {synced} following base opacity.";
        NoDisplayInfo.IsOpen = monitors.Count == 0;

        if (_monitorListVersion != _state.MonitorsVersion && !_overrideCommitTimer.IsRunning)
        {
            _monitorListVersion = _state.MonitorsVersion;
            var rows = new List<MonitorRow>(monitors.Count);
            foreach (var monitor in monitors)
            {
                rows.Add(new MonitorRow(monitor.DeviceName, monitor.FriendlyName, monitor.IsPrimary, monitor.SyncWithPrimary, monitor.OverrideOpacity));
            }

            MonitorList.ItemsSource = rows;
        }

        _loading = false;
    }

    private void SyncSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading && sender is FrameworkElement { DataContext: MonitorRow row })
        {
            row.NotifySyncChanged();
            _state.SetMonitorOverride(row.DeviceName, row.Opacity, row.SyncWithPrimary);
        }
    }

    private void OverrideSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_loading || sender is not FrameworkElement { DataContext: MonitorRow row })
        {
            return;
        }

        if (_overrideCommitTimer.IsRunning && !ReferenceEquals(_pendingOverrideRow, row))
        {
            _overrideCommitTimer.Stop();
            CommitOverride();
        }

        _pendingOverrideRow = row;
        row.NotifyOpacityChanged();
        _state.PreviewMonitorOverride(row.DeviceName, row.Opacity, row.SyncWithPrimary);
        _overrideCommitTimer.Stop();
        _overrideCommitTimer.Start();
    }

    private void CommitPendingOverride(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        CommitOverride();
        Refresh();
    }

    private void CommitOverride()
    {
        if (_pendingOverrideRow is { } row)
        {
            _pendingOverrideRow = null;
            _state.SetMonitorOverride(row.DeviceName, row.Opacity, row.SyncWithPrimary);
        }
    }

    private void Detect_Click(object sender, RoutedEventArgs e)
    {
        _state.RefreshMonitors();
    }
}

public sealed class MonitorRow(string deviceName, string name, bool isPrimary, bool syncWithPrimary, byte opacity) : INotifyPropertyChanged
{
    public string DeviceName { get; } = deviceName;
    public string Name { get; } = name;
    public string Device { get; } = deviceName;
    public bool IsPrimary { get; } = isPrimary;
    public Visibility PrimaryBadgeVisibility { get; } = isPrimary ? Visibility.Visible : Visibility.Collapsed;
    public bool SyncWithPrimary { get; set; } = syncWithPrimary;
    public double Opacity { get; set; } = opacity;

    public string OpacityText => $"{Opacity:0}%";
    public Visibility OverrideVisibility => SyncWithPrimary ? Visibility.Collapsed : Visibility.Visible;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyOpacityChanged() => Notify(nameof(OpacityText));

    public void NotifySyncChanged() => Notify(nameof(OverrideVisibility));

    private void Notify(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
