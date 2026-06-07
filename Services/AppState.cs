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

    public AppSettings Settings { get; private set; } = new();
    public RuntimeSnapshot Runtime { get; } = new();
    public ObservableCollection<MonitorProfile> Monitors { get; } = [];
    public string SettingsPath => _store.SettingsPath;
    public bool StartupRegistrationFailed { get; private set; }
    public string StartupStatusMessage { get; private set; } = "Startup registration is ready.";
    public bool ExitRequested { get; private set; }

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
            ApplyNow,
            ToggleTransparency,
            RequestExit);
        _tray.SetVisible(Settings.ShowTrayIcon);
        ApplyNow();
        _sensors.Start(trigger => ApplyNow(trigger));
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
        var current = _monitors.GetCurrent();
        Monitors.Clear();
        foreach (var monitor in current)
        {
            var saved = Settings.Monitors.FirstOrDefault(item => item.DeviceName == monitor.DeviceName);
            Monitors.Add(saved ?? monitor);
        }

        Settings.Monitors = Monitors.ToList();
        Save();
    }

    public void SetProfile(TaskbarProfile profile)
    {
        Settings.ActiveProfile = profile;
        ApplyNow();
    }

    public void SetOpacity(double value)
    {
        Settings.ActiveProfile = Settings.ActiveProfile with { Opacity = (byte)Math.Round(value) };
        ApplyNow();
    }

    public void SetAutomation(bool enabled)
    {
        Settings.AutomationEnabled = enabled;
        ApplyNow();
    }

    public void SetHoverReveal(bool enabled)
    {
        Settings.HoverReveal = enabled;
        SaveAndNotify();
    }

    public void SetHoverDistance(double value)
    {
        Settings.HoverDistance = Math.Clamp((int)Math.Round(value), 0, 48);
        SaveAndNotify();
    }

    public void SetFullscreenOverlap(bool enabled)
    {
        Settings.FullscreenOverlap = enabled;
        SaveAndNotify();
    }

    public void SetTrayVisible(bool enabled)
    {
        Settings.ShowTrayIcon = enabled;
        _tray.SetVisible(enabled);
        SaveAndNotify();
    }

    public void SetStartWithWindows(bool enabled)
    {
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
        Settings.OpenHotkey = "Ctrl+Alt+G";
        Settings.ToggleHotkey = "Ctrl+Alt+T";
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

        ApplyNow();
    }

    public void CompleteFirstRun(TaskbarProfile profile)
    {
        Settings.FirstRunCompleted = true;
        Settings.ActiveProfile = profile;
        ApplyNow();
    }

    public void ApplyNow() => ApplyNow(AutomationTrigger.Desktop);

    public void ApplyNow(AutomationTrigger trigger)
    {
        var opacity = OpacityPolicy.Resolve(Settings.ActiveProfile, trigger, Settings.AutomationEnabled);
        var previousState = Runtime.State;
        var previousOpacity = Runtime.ResolvedOpacity;
        Runtime.TaskbarsUpdated = _taskbar.Apply(Settings.ActiveProfile, opacity);
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

        SaveAndNotify();
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
