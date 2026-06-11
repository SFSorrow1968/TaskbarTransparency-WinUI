using System.Collections.ObjectModel;
using System.Security;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

public sealed class AppState
{
    private readonly SettingsStore _store = new();
    private readonly MonitorCatalog _monitors = new();
    private readonly TaskbarAppearanceService _taskbar = new();
    private readonly TrayIconHost _tray = new();
    private readonly StartupRegistrationService _startup = new();
    private readonly GlobalHotkeyService _hotkeys = new();
    private readonly RuntimeStateSensorService _sensors;
    private AutomationTrigger _currentTrigger = AutomationTrigger.Desktop;
    private bool _previewPending;

    public AppSettings Settings { get; private set; } = new();
    public RuntimeSnapshot Runtime { get; } = new();
    public ObservableCollection<MonitorProfile> Monitors { get; } = [];
    public int MonitorsVersion { get; private set; }
    public string SettingsPath => _store.SettingsPath;
    public bool StartupRegistrationFailed { get; private set; }
    public string StartupStatusMessage { get; private set; } = "Startup registration is ready.";
    public bool ExitRequested { get; private set; }
    public bool TransparencyPaused { get; private set; }
    public AutomationTrigger CurrentTrigger => _currentTrigger;
    public HotkeyConfigurationStatus HotkeyStatus => _hotkeys.Status;

    public event EventHandler? Changed;
    public event EventHandler? ShowWindowRequested;

    public AppState()
    {
        _sensors = new RuntimeStateSensorService(() => Settings);
    }

    public void Initialize()
    {
        Settings = _store.Load();
        Settings.Normalize();
        Settings.StartWithWindows = _startup.IsEnabled();
        var startupTaskbars = TaskbarWindowCatalog.GetCurrent();
        RefreshMonitors(startupTaskbars);
        _tray.Start(
            RequestShowWindow,
            ReapplyNow,
            ToggleTransparency,
            RequestExit);
        _tray.SetVisible(Settings.ShowTrayIcon);
        ApplyNow(persistSettings: false, taskbarTargets: startupTaskbars);
        _sensors.Start(OnTriggerChanged);
    }

    public void AttachWindow(IntPtr hwnd)
    {
        _hotkeys.Attach(
            hwnd,
            Settings.OpenHotkey,
            Settings.ToggleHotkey,
            RequestShowWindow,
            ToggleTransparency);
    }

    public void RequestShowWindow()
    {
        ShowWindowRequested?.Invoke(this, EventArgs.Empty);
    }

    public void RequestExit()
    {
        ExitRequested = true;
        _tray.Dispose();
        _hotkeys.Dispose();
        _sensors.Dispose();
        Environment.Exit(0);
    }

    public void RefreshMonitors()
    {
        RefreshMonitors(TaskbarWindowCatalog.GetCurrent());
        ApplyNow(persistSettings: false);
    }

    private void RefreshMonitors(IReadOnlyList<TaskbarWindowInfo> detectedTaskbars)
    {
        var detected = _monitors.GetCurrent(detectedTaskbars);
        var merged = MonitorProfile.MergeDetectedList(detected, Settings.Monitors);

        if (!MonitorProfile.SequenceMatches(Monitors, merged))
        {
            Monitors.Clear();
            foreach (var monitor in merged)
            {
                Monitors.Add(monitor);
            }

            MonitorsVersion++;
        }

        if (!MonitorProfile.SequenceMatches(Settings.Monitors, merged))
        {
            Settings.Monitors = merged;
            Save();
        }
    }

    public void SetBaseOpacity(double value) => SetBaseOpacityCore(value, persistSettings: true);

    public void PreviewBaseOpacity(double value) => SetBaseOpacityCore(value, persistSettings: false);

    private void SetBaseOpacityCore(double value, bool persistSettings)
    {
        var opacity = ClampOpacity(value);
        if (Settings.BaseOpacity == opacity)
        {
            CommitPendingPreview(persistSettings);
            return;
        }

        Settings.BaseOpacity = opacity;
        _previewPending = !persistSettings;
        ApplyNow(persistSettings);
    }

    public void SetAutomationEnabled(bool enabled)
    {
        if (Settings.AutomationEnabled == enabled)
        {
            return;
        }

        Settings.AutomationEnabled = enabled;
        ApplyNow(persistSettings: true);
    }

    public void SetRuleEnabled(AutomationTrigger trigger, bool enabled)
    {
        var rule = Settings.RuleFor(trigger);
        if (rule is null || rule.Enabled == enabled)
        {
            return;
        }

        rule.Enabled = enabled;
        ApplyNow(persistSettings: true);
    }

    public void SetRuleOpacity(AutomationTrigger trigger, double value) => SetRuleOpacityCore(trigger, value, persistSettings: true);

    public void PreviewRuleOpacity(AutomationTrigger trigger, double value) => SetRuleOpacityCore(trigger, value, persistSettings: false);

    private void SetRuleOpacityCore(AutomationTrigger trigger, double value, bool persistSettings)
    {
        var rule = Settings.RuleFor(trigger);
        if (rule is null)
        {
            return;
        }

        var opacity = ClampOpacity(value);
        if (rule.Opacity == opacity)
        {
            CommitPendingPreview(persistSettings);
            return;
        }

        rule.Opacity = opacity;
        _previewPending = !persistSettings;
        ApplyNow(persistSettings);
    }

    public void SetHoverDistance(double value) => SetHoverDistanceCore(value, persistSettings: true);

    public void PreviewHoverDistance(double value) => SetHoverDistanceCore(value, persistSettings: false);

    private void SetHoverDistanceCore(double value, bool persistSettings)
    {
        var distance = Math.Clamp((int)Math.Round(value), 0, 48);
        if (Settings.HoverDistance == distance)
        {
            CommitPendingPreview(persistSettings);
            return;
        }

        Settings.HoverDistance = distance;
        _previewPending = !persistSettings;
        if (persistSettings)
        {
            SaveAndNotify();
            return;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetFadeMilliseconds(double value)
    {
        var fade = Math.Clamp((int)Math.Round(value), 0, 1000);
        if (Settings.FadeMilliseconds == fade)
        {
            return;
        }

        Settings.FadeMilliseconds = fade;
        SaveAndNotify();
    }

    public void SetMonitorOverride(string deviceName, double opacity, bool syncWithPrimary) => SetMonitorOverrideCore(deviceName, opacity, syncWithPrimary, persistSettings: true);

    public void PreviewMonitorOverride(string deviceName, double opacity, bool syncWithPrimary) => SetMonitorOverrideCore(deviceName, opacity, syncWithPrimary, persistSettings: false);

    private void SetMonitorOverrideCore(string deviceName, double opacity, bool syncWithPrimary, bool persistSettings)
    {
        var monitor = MonitorProfile.FindByDeviceName(Settings.Monitors, deviceName, StringComparison.OrdinalIgnoreCase);
        if (monitor is null)
        {
            return;
        }

        var nextOpacity = ClampOpacity(opacity);
        if (monitor.OverrideOpacity == nextOpacity && monitor.SyncWithPrimary == syncWithPrimary)
        {
            CommitPendingPreview(persistSettings);
            return;
        }

        monitor.OverrideOpacity = nextOpacity;
        monitor.SyncWithPrimary = syncWithPrimary;

        var liveMonitor = MonitorProfile.FindByDeviceName(Monitors, deviceName, StringComparison.OrdinalIgnoreCase);
        if (liveMonitor is not null && !ReferenceEquals(liveMonitor, monitor))
        {
            liveMonitor.OverrideOpacity = monitor.OverrideOpacity;
            liveMonitor.SyncWithPrimary = syncWithPrimary;
        }

        MonitorsVersion++;
        _previewPending = !persistSettings;
        ApplyNow(persistSettings);
    }

    public void SetTrayVisible(bool enabled)
    {
        if (Settings.ShowTrayIcon == enabled)
        {
            return;
        }

        Settings.ShowTrayIcon = enabled;
        _tray.SetVisible(enabled);
        SaveAndNotify();
    }

    public void SetStartWithWindows(bool enabled)
    {
        if (Settings.StartWithWindows == enabled && !StartupRegistrationFailed)
        {
            return;
        }

        try
        {
            _startup.SetEnabled(enabled);
            Settings.StartWithWindows = enabled;
            StartupRegistrationFailed = false;
            StartupStatusMessage = enabled
                ? "Oxygen Taskbar will start when you sign in."
                : "Oxygen Taskbar will not start automatically.";
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            Settings.StartWithWindows = _startup.IsEnabled();
            StartupRegistrationFailed = true;
            StartupStatusMessage = "Windows blocked the startup registration. Check account permissions or retry after restarting Explorer.";
        }

        SaveAndNotify();
    }

    public void ResetHotkeys()
    {
        SetHotkeys("Ctrl+Alt+G", "Ctrl+Alt+T");
    }

    public void SetHotkeys(string openHotkey, string toggleHotkey)
    {
        var normalizedOpen = NormalizeHotkey(openHotkey, "Ctrl+Alt+G");
        var normalizedToggle = NormalizeHotkey(toggleHotkey, "Ctrl+Alt+T");
        if (GlobalHotkeyService.CanSkipReconfigure(Settings.OpenHotkey, Settings.ToggleHotkey, normalizedOpen, normalizedToggle, HotkeyStatus.IsReady))
        {
            return;
        }

        Settings.OpenHotkey = normalizedOpen;
        Settings.ToggleHotkey = normalizedToggle;
        _hotkeys.Reconfigure(Settings.OpenHotkey, Settings.ToggleHotkey);
        SaveAndNotify();
    }

    public void ToggleTransparency()
    {
        TransparencyPaused = !TransparencyPaused;
        ApplyNow(persistSettings: false);
    }

    public void ReapplyNow() => ApplyNow(persistSettings: false);

    private void OnTriggerChanged(AutomationTrigger trigger)
    {
        _currentTrigger = trigger;
        ApplyNow(persistSettings: false);
    }

    private void ApplyNow(bool persistSettings)
    {
        ApplyNow(persistSettings, taskbarTargets: null);
    }

    private void ApplyNow(bool persistSettings, IReadOnlyList<TaskbarWindowInfo>? taskbarTargets)
    {
        var resolution = TransparencyPaused
            ? new OpacityResolution(100, "Transparency paused")
            : OpacityPolicy.Resolve(Settings, _currentTrigger);
        var previousState = Runtime.State;
        var previousOpacity = Runtime.ResolvedOpacity;
        Runtime.TaskbarsUpdated = taskbarTargets is null
            ? _taskbar.Apply(resolution.Opacity, Settings.FadeMilliseconds, !TransparencyPaused, Settings.Monitors)
            : _taskbar.Apply(resolution.Opacity, Settings.FadeMilliseconds, !TransparencyPaused, Settings.Monitors, taskbarTargets);
        Runtime.LastAppliedAt = DateTimeOffset.Now;
        Runtime.State = _currentTrigger.ToString();
        Runtime.ResolvedOpacity = resolution.Opacity;
        Runtime.OpacitySource = resolution.Source;
        Runtime.LastMessage = Runtime.TaskbarsUpdated == 0
            ? "No taskbar windows were found"
            : TransparencyPaused
                ? "Transparency paused; taskbars restored to normal"
                : $"Applied {resolution.Opacity}% ({resolution.Source})";
        if (Runtime.RecentEvents.Count == 0 || previousState != Runtime.State || previousOpacity != resolution.Opacity)
        {
            Runtime.RecordEvent(new RuntimeEvent
            {
                Time = Runtime.LastAppliedAt,
                State = Runtime.State,
                Source = Runtime.OpacitySource,
                Opacity = Runtime.ResolvedOpacity,
                TaskbarsUpdated = Runtime.TaskbarsUpdated
            });
        }

        if (persistSettings)
        {
            SaveAndNotify();
            return;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void CommitPendingPreview(bool persistSettings)
    {
        if (persistSettings && _previewPending)
        {
            _previewPending = false;
            SaveAndNotify();
        }
    }

    private void SaveAndNotify()
    {
        _previewPending = false;
        Save();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Save()
    {
        _store.Save(Settings);
    }

    private static byte ClampOpacity(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value), 0, 100);
    }

    private static string NormalizeHotkey(string hotkey, string fallback)
    {
        return string.IsNullOrWhiteSpace(hotkey) ? fallback : hotkey.Trim();
    }
}
