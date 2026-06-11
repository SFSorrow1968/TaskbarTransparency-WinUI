using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly AppState _state = ((App)Application.Current).State;
    private readonly RefreshCoalescer _refreshCoalescer = new();
    private bool _loading = true;

    public SettingsPage()
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
        var settings = _state.Settings;
        _loading = true;
        TraySwitch.IsOn = settings.ShowTrayIcon;
        StartupSwitch.IsOn = settings.StartWithWindows;
        OpenHotkeyText.Text = settings.OpenHotkey;
        ToggleHotkeyText.Text = settings.ToggleHotkey;
        StartupPermissionInfo.IsOpen = _state.StartupRegistrationFailed;
        StartupPermissionInfo.Message = _state.StartupStatusMessage;
        UpdateHotkeyRegistrationInfo();
        _loading = false;
    }

    private void TraySwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetTrayVisible(TraySwitch.IsOn);
        }
    }

    private void StartupSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetStartWithWindows(StartupSwitch.IsOn);
        }
    }

    private void RetryStartup_Click(object sender, RoutedEventArgs e)
    {
        _state.SetStartWithWindows(StartupSwitch.IsOn);
    }

    private void SaveHotkeys_Click(object sender, RoutedEventArgs e)
    {
        _state.SetHotkeys(OpenHotkeyText.Text, ToggleHotkeyText.Text);
        UpdateHotkeyRegistrationInfo();
    }

    private void ResetHotkeys_Click(object sender, RoutedEventArgs e)
    {
        _state.ResetHotkeys();
    }

    private void UpdateHotkeyRegistrationInfo()
    {
        var status = _state.HotkeyStatus;
        HotkeyRegistrationInfo.Severity = status.IsReady ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        HotkeyRegistrationInfo.Message = status.IsReady
            ? "Both shortcuts are registered with Windows."
            : $"Open: {status.Open.Summary} · Toggle: {status.Toggle.Summary}";
    }
}
