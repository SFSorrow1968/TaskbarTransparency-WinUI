using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class HomePage : Page
{
    private readonly AppState _state = ((App)Application.Current).State;
    private readonly RefreshCoalescer _refreshCoalescer = new();
    private readonly CommitTimer _baseOpacityCommit;
    private readonly CommitTimer _fadeInCommit;
    private readonly CommitTimer _fadeOutCommit;
    private readonly CommitTimer _ruleCommit;
    private readonly CommitTimer _hoverDistanceCommit;
    private readonly CommitTimer _monitorCommit;
    private int _monitorListVersion = -1;
    private bool _loading = true;

    public HomePage()
    {
        InitializeComponent();
        _baseOpacityCommit = new CommitTimer(DispatcherQueue);
        _fadeInCommit = new CommitTimer(DispatcherQueue);
        _fadeOutCommit = new CommitTimer(DispatcherQueue);
        _ruleCommit = new CommitTimer(DispatcherQueue);
        _hoverDistanceCommit = new CommitTimer(DispatcherQueue);
        _monitorCommit = new CommitTimer(DispatcherQueue);
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
        _baseOpacityCommit.Flush();
        _fadeInCommit.Flush();
        _fadeOutCommit.Flush();
        _ruleCommit.Flush();
        _hoverDistanceCommit.Flush();
        _monitorCommit.Flush();
    }

    private void State_Changed(object? sender, EventArgs e)
    {
        _refreshCoalescer.Request(action => DispatcherQueue.TryEnqueue(() => action()), Refresh);
    }

    private void Refresh()
    {
        _loading = true;
        var runtime = _state.Runtime;
        var settings = _state.Settings;

        AppliedOpacityText.Text = $"{runtime.ResolvedOpacity}%";
        AppliedSourceText.Text = runtime.OpacitySource;
        AppliedStateText.Text = $"Current state: {RuntimeTriggerText.Label(runtime.State)}";
        AppliedDetailText.Text = runtime.TaskbarsUpdated == 0
            ? "No taskbar windows were found"
            : $"Applied to {runtime.TaskbarsUpdated} taskbar{(runtime.TaskbarsUpdated == 1 ? string.Empty : "s")} at {runtime.LastAppliedAt:h:mm tt}";
        PauseButton.Content = _state.TransparencyPaused ? "Resume transparency" : "Pause transparency";
        AutomationSwitch.IsOn = settings.AutomationEnabled;
        AutomationDetailText.Text = settings.AutomationEnabled
            ? "Rules react to hover, fullscreen, and windows."
            : "Automation is off; the base opacity is used everywhere.";

        OpacitySlider.Value = settings.BaseOpacity;
        OpacityValueBoxText.Text = $"{settings.BaseOpacity}%";
        OverrideInfo.IsOpen = !_state.TransparencyPaused && runtime.OpacitySource != OpacityPolicy.BaseSource;
        FadeInSlider.Value = settings.FadeInMilliseconds;
        FadeInText.Text = $"{settings.FadeInMilliseconds} ms";
        FadeOutSlider.Value = settings.FadeOutMilliseconds;
        FadeOutText.Text = $"{settings.FadeOutMilliseconds} ms";

        RefreshRules(settings);
        RefreshMonitors();
        _loading = false;
    }

    private void RefreshRules(AppSettings settings)
    {
        HoverSwitch.IsOn = settings.HoverRule.Enabled;
        HoverOpacitySlider.Value = settings.HoverRule.Opacity;
        HoverOpacityText.Text = $"{settings.HoverRule.Opacity}%";
        HoverOpacitySlider.IsEnabled = settings.HoverRule.Enabled;
        HoverDistanceSlider.Value = settings.HoverDistance;
        HoverDistanceText.Text = $"{settings.HoverDistance} px";
        HoverDistanceSlider.IsEnabled = settings.HoverRule.Enabled;

        FullscreenSwitch.IsOn = settings.FullscreenRule.Enabled;
        FullscreenOpacitySlider.Value = settings.FullscreenRule.Opacity;
        FullscreenOpacityText.Text = $"{settings.FullscreenRule.Opacity}%";
        FullscreenOpacitySlider.IsEnabled = settings.FullscreenRule.Enabled;

        MaximizedSwitch.IsOn = settings.MaximizedRule.Enabled;
        MaximizedOpacitySlider.Value = settings.MaximizedRule.Opacity;
        MaximizedOpacityText.Text = $"{settings.MaximizedRule.Opacity}%";
        MaximizedOpacitySlider.IsEnabled = settings.MaximizedRule.Enabled;

        WindowSwitch.IsOn = settings.WindowRule.Enabled;
        WindowOpacitySlider.Value = settings.WindowRule.Opacity;
        WindowOpacityText.Text = $"{settings.WindowRule.Opacity}%";
        WindowOpacitySlider.IsEnabled = settings.WindowRule.Enabled;
    }

    private void RefreshMonitors()
    {
        var monitors = _state.Monitors;
        var synced = MonitorProfile.CountSynced(monitors);
        MonitorsSummaryText.Text = monitors.Count == 0
            ? "Each display follows the base opacity unless you give it its own value."
            : $"{monitors.Count} display{(monitors.Count == 1 ? string.Empty : "s")} detected · {synced} following base opacity.";
        NoDisplayInfo.IsOpen = monitors.Count == 0;

        if (_monitorListVersion != _state.MonitorsVersion && !_monitorCommit.IsPending)
        {
            _monitorListVersion = _state.MonitorsVersion;
            var rows = new List<MonitorRow>(monitors.Count);
            foreach (var monitor in monitors)
            {
                rows.Add(new MonitorRow(monitor.DeviceName, monitor.FriendlyName, monitor.IsPrimary, monitor.SyncWithPrimary, monitor.OverrideOpacity));
            }

            MonitorList.ItemsSource = rows;
        }
    }

    private void OpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            var value = e.NewValue;
            OpacityValueBoxText.Text = $"{value:0}%";
            _state.PreviewBaseOpacity(value);
            _baseOpacityCommit.Schedule("base", () => _state.SetBaseOpacity(value));
        }
    }

    private void FadeInSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            var value = e.NewValue;
            FadeInText.Text = $"{value:0} ms";
            _fadeInCommit.Schedule("fadeIn", () => _state.SetFadeInMilliseconds(value));
        }
    }

    private void FadeOutSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            var value = e.NewValue;
            FadeOutText.Text = $"{value:0} ms";
            _fadeOutCommit.Schedule("fadeOut", () => _state.SetFadeOutMilliseconds(value));
        }
    }

    private void AutomationSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetAutomationEnabled(AutomationSwitch.IsOn);
        }
    }

    private void RuleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading && sender is ToggleSwitch toggle && TryParseTrigger(toggle.Tag, out var trigger))
        {
            _state.SetRuleEnabled(trigger, toggle.IsOn);
        }
    }

    private void RuleSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_loading || sender is not Slider slider || !TryParseTrigger(slider.Tag, out var trigger))
        {
            return;
        }

        var value = e.NewValue;
        UpdateRuleValueText(trigger, value);
        _state.PreviewRuleOpacity(trigger, value);
        _ruleCommit.Schedule(trigger, () => _state.SetRuleOpacity(trigger, value));
    }

    private void UpdateRuleValueText(AutomationTrigger trigger, double value)
    {
        var text = $"{value:0}%";
        switch (trigger)
        {
            case AutomationTrigger.Hover:
                HoverOpacityText.Text = text;
                break;
            case AutomationTrigger.Fullscreen:
                FullscreenOpacityText.Text = text;
                break;
            case AutomationTrigger.WindowMaximized:
                MaximizedOpacityText.Text = text;
                break;
            case AutomationTrigger.WindowVisible:
                WindowOpacityText.Text = text;
                break;
        }
    }

    private void HoverDistanceSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            var value = e.NewValue;
            HoverDistanceText.Text = $"{value:0} px";
            _state.PreviewHoverDistance(value);
            _hoverDistanceCommit.Schedule("hoverDistance", () => _state.SetHoverDistance(value));
        }
    }

    private void SyncSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading && sender is ToggleSwitch { DataContext: MonitorRow row } toggle)
        {
            row.SyncWithPrimary = toggle.IsOn;
            row.NotifySyncChanged();
            _monitorCommit.Flush();
            _state.SetMonitorOverride(row.DeviceName, row.Opacity, toggle.IsOn);
        }
    }

    private void OverrideSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_loading || sender is not Slider { DataContext: MonitorRow row })
        {
            return;
        }

        row.Opacity = e.NewValue;
        row.NotifyOpacityChanged();
        _state.PreviewMonitorOverride(row.DeviceName, row.Opacity, row.SyncWithPrimary);
        _monitorCommit.Schedule(row.DeviceName, () => _state.SetMonitorOverride(row.DeviceName, row.Opacity, row.SyncWithPrimary));
    }

    private void Detect_Click(object sender, RoutedEventArgs e) => _state.RefreshMonitors();
    private void Pause_Click(object sender, RoutedEventArgs e) => _state.ToggleTransparency();
    private void Reapply_Click(object sender, RoutedEventArgs e) => _state.ReapplyNow();

    private static bool TryParseTrigger(object? tag, out AutomationTrigger trigger)
    {
        if (tag is string text && Enum.TryParse(text, out trigger))
        {
            return true;
        }

        trigger = AutomationTrigger.Desktop;
        return false;
    }

    private sealed class CommitTimer
    {
        private readonly DispatcherQueueTimer _timer;
        private object? _key;
        private Action? _commit;

        public CommitTimer(DispatcherQueue queue)
        {
            _timer = queue.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(350);
            _timer.Tick += (_, _) =>
            {
                _timer.Stop();
                var commit = _commit;
                _commit = null;
                _key = null;
                commit?.Invoke();
            };
        }

        public bool IsPending => _timer.IsRunning;

        public void Schedule(object key, Action commit)
        {
            if (_timer.IsRunning && !Equals(_key, key))
            {
                Flush();
            }

            _key = key;
            _commit = commit;
            _timer.Stop();
            _timer.Start();
        }

        public void Flush()
        {
            if (!_timer.IsRunning)
            {
                return;
            }

            _timer.Stop();
            var commit = _commit;
            _commit = null;
            _key = null;
            commit?.Invoke();
        }
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
