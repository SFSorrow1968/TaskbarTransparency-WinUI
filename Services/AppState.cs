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
    private byte? _opacityBeforeToggle;
    private bool _opacityPreviewPending;
    private bool _hoverDistancePreviewPending;

    public AppSettings Settings { get; private set; } = new();
    public RuntimeSnapshot Runtime { get; } = new();
    public ObservableCollection<MonitorProfile> Monitors { get; } = [];
    public string SettingsPath => _store.SettingsPath;
    public bool StartupRegistrationFailed { get; private set; }
    public string StartupStatusMessage { get; private set; } = "Startup registration is ready.";
    public bool ExitRequested { get; private set; }
    public HotkeyConfigurationStatus HotkeyStatus => _hotkeys.Status;

    public event EventHandler? Changed;
    public event EventHandler<AppViewRequestedEventArgs>? ShowWindowRequested;

    public AppState()
    {
        _sensors = new RuntimeStateSensorService(() => Settings);
    }

    public void Initialize()
    {
        Settings = _store.Load();
        Settings.StartWithWindows = _startup.IsEnabled();
        RefreshMonitors();
        _tray.Start(
            () => RequestView(AppView.Dashboard),
            () => RequestView(AppView.Tuning),
            ReapplyCurrentRuntimeState,
            ToggleTransparency,
            RequestExit);
        _tray.SetVisible(Settings.ShowTrayIcon);
        ApplyNow(persistSettings: false);
        _sensors.Start(trigger => ApplyNow(trigger, persistSettings: false));
    }

    public void AttachWindow(IntPtr hwnd)
    {
        _hotkeys.Attach(
            hwnd,
            Settings.OpenHotkey,
            Settings.ToggleHotkey,
            () => RequestView(AppView.Dashboard),
            ToggleTransparency);
    }

    public void RequestView(AppView view)
    {
        ShowWindowRequested?.Invoke(this, new AppViewRequestedEventArgs(view));
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
        var detected = _monitors.GetCurrent();
        var merged = detected
            .Select(monitor => MonitorProfile.MergeDetected(
                monitor,
                Settings.Monitors.FirstOrDefault(item => string.Equals(item.DeviceName, monitor.DeviceName, StringComparison.Ordinal))))
            .ToList();

        if (!MonitorProfile.SequenceMatches(Monitors, merged))
        {
            Monitors.Clear();
            foreach (var monitor in merged)
            {
                Monitors.Add(monitor);
            }
        }

        if (!MonitorProfile.SequenceMatches(Settings.Monitors, merged))
        {
            Settings.Monitors = merged;
            Save();
        }
    }

    public void SetProfile(TaskbarProfile profile)
    {
        if (Settings.ActiveProfile == profile)
        {
            return;
        }

        _opacityBeforeToggle = null;
        _opacityPreviewPending = false;
        Settings.ActiveProfile = profile;
        ApplyNow(persistSettings: true);
    }

    public void SetOpacity(double value)
    {
        SetOpacityCore(value, persistSettings: true);
    }

    public void PreviewOpacity(double value)
    {
        SetOpacityCore(value, persistSettings: false);
    }

    private void SetOpacityCore(double value, bool persistSettings)
    {
        var opacity = (byte)Math.Clamp((int)Math.Round(value), 0, 100);
        if (Settings.ActiveProfile.Opacity == opacity)
        {
            if (persistSettings && _opacityPreviewPending)
            {
                _opacityPreviewPending = false;
                SaveAndNotify();
            }

            return;
        }

        _opacityBeforeToggle = null;
        Settings.ActiveProfile = Settings.ActiveProfile with { Opacity = opacity };
        _opacityPreviewPending = !persistSettings;
        ApplyNow(persistSettings);
    }

    public void SetMonitorOverride(string deviceName, double opacity, bool syncWithPrimary)
    {
        var monitor = Settings.Monitors.FirstOrDefault(item => string.Equals(item.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
        if (monitor is null)
        {
            return;
        }

        var nextOpacity = (byte)Math.Clamp((int)Math.Round(opacity), 0, 100);
        if (monitor.OverrideOpacity == nextOpacity && monitor.SyncWithPrimary == syncWithPrimary)
        {
            return;
        }

        monitor.OverrideOpacity = nextOpacity;
        monitor.SyncWithPrimary = syncWithPrimary;

        var liveMonitor = Monitors.FirstOrDefault(item => string.Equals(item.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
        if (liveMonitor is not null)
        {
            liveMonitor.OverrideOpacity = monitor.OverrideOpacity;
            liveMonitor.SyncWithPrimary = syncWithPrimary;
        }

        ApplyNow(persistSettings: true);
    }

    public void SetAutomation(bool enabled)
    {
        if (Settings.AutomationEnabled == enabled)
        {
            return;
        }

        Settings.AutomationEnabled = enabled;
        ApplyNow(persistSettings: true);
    }

    public void SetHoverReveal(bool enabled)
    {
        if (Settings.HoverReveal == enabled)
        {
            return;
        }

        Settings.HoverReveal = enabled;
        SaveAndNotify();
    }

    public void SetHoverDistance(double value)
    {
        SetHoverDistanceCore(value, persistSettings: true);
    }

    public void PreviewHoverDistance(double value)
    {
        SetHoverDistanceCore(value, persistSettings: false);
    }

    private void SetHoverDistanceCore(double value, bool persistSettings)
    {
        var distance = Math.Clamp((int)Math.Round(value), 0, 48);
        if (Settings.HoverDistance == distance)
        {
            if (persistSettings && _hoverDistancePreviewPending)
            {
                _hoverDistancePreviewPending = false;
                SaveAndNotify();
            }

            return;
        }

        Settings.HoverDistance = distance;
        _hoverDistancePreviewPending = !persistSettings;
        if (persistSettings)
        {
            SaveAndNotify();
            return;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetFullscreenOverlap(bool enabled)
    {
        if (Settings.FullscreenOverlap == enabled)
        {
            return;
        }

        Settings.FullscreenOverlap = enabled;
        SaveAndNotify();
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
        if (_opacityBeforeToggle is null)
        {
            _opacityBeforeToggle = Settings.ActiveProfile.Opacity;
            Settings.ActiveProfile = Settings.ActiveProfile with { Opacity = 0 };
        }
        else
        {
            Settings.ActiveProfile = Settings.ActiveProfile with { Opacity = _opacityBeforeToggle.Value };
            _opacityBeforeToggle = null;
        }

        ApplyNow(persistSettings: true);
    }

    public void CompleteFirstRun(TaskbarProfile profile)
    {
        if (Settings.FirstRunCompleted && Settings.ActiveProfile == profile)
        {
            return;
        }

        _opacityBeforeToggle = null;
        _opacityPreviewPending = false;
        Settings.FirstRunCompleted = true;
        Settings.ActiveProfile = profile;
        ApplyNow(persistSettings: true);
    }

    public void ApplyNow() => ApplyNow(AutomationTrigger.Desktop, persistSettings: false);

    public void ReapplyCurrentRuntimeState() => ApplyNow(RuntimeTriggerText.Parse(Runtime.State), persistSettings: false);

    public void ApplyNow(AutomationTrigger trigger) => ApplyNow(trigger, persistSettings: false);

    private void ApplyNow(bool persistSettings) => ApplyNow(AutomationTrigger.Desktop, persistSettings);

    private void ApplyNow(AutomationTrigger trigger, bool persistSettings)
    {
        var opacity = OpacityPolicy.Resolve(Settings.ActiveProfile, trigger, Settings.AutomationEnabled);
        var previousState = Runtime.State;
        var previousOpacity = Runtime.ResolvedOpacity;
        Runtime.TaskbarsUpdated = _taskbar.Apply(Settings.ActiveProfile, opacity, Settings.Monitors);
        Runtime.LastAppliedAt = DateTimeOffset.Now;
        Runtime.State = trigger.ToString();
        Runtime.AppliedProfile = Settings.ActiveProfile.Name;
        Runtime.ResolvedOpacity = opacity;
        Runtime.LastMessage = Runtime.TaskbarsUpdated == 0
            ? "No taskbar windows were found"
            : $"Applied {Settings.ActiveProfile.Mode} at {opacity}%";
        if (Runtime.RecentEvents.Count == 0 || previousState != Runtime.State || previousOpacity != opacity)
        {
            Runtime.RecordEvent(new RuntimeEvent
            {
                Time = Runtime.LastAppliedAt,
                State = Runtime.State,
                Profile = Runtime.AppliedProfile,
                Opacity = Runtime.ResolvedOpacity,
                TaskbarsUpdated = Runtime.TaskbarsUpdated,
                Message = Runtime.LastMessage
            });
        }

        if (persistSettings)
        {
            SaveAndNotify();
            return;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void SaveAndNotify()
    {
        Save();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Save()
    {
        _store.Save(Settings);
    }

    private static string NormalizeHotkey(string hotkey, string fallback)
    {
        return string.IsNullOrWhiteSpace(hotkey) ? fallback : hotkey.Trim();
    }

}

public enum AppView
{
    Dashboard,
    Tuning
}

public sealed class AppViewRequestedEventArgs(AppView view) : EventArgs
{
    public AppView View { get; } = view;
}
