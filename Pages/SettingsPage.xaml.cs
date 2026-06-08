// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace TaskbarTransparency.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly Services.AppState _state = ((App)Microsoft.UI.Xaml.Application.Current).State;
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
        DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        var settings = _state.Settings;
        _loading = true;
        TraySwitch.IsOn = settings.ShowTrayIcon;
        StartupSwitch.IsOn = settings.StartWithWindows;
        OpenHotkeyText.Text = settings.OpenHotkey;
        ToggleHotkeyText.Text = settings.ToggleHotkey;
        StartupPermissionInfo.Severity = _state.StartupRegistrationFailed ? InfoBarSeverity.Warning : InfoBarSeverity.Success;
        StartupPermissionInfo.Message = _state.StartupStatusMessage;
        StartupStatusText.Text = _state.StartupRegistrationFailed
            ? "Startup could not be changed. Keep using tray launch for now, or retry after checking Windows account permissions."
            : _state.StartupStatusMessage;
        UpdateHotkeyRegistrationInfo();
        _loading = false;
    }

    private void TraySwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetTrayVisible(TraySwitch.IsOn);
        }
    }

    private void StartupSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetStartWithWindows(StartupSwitch.IsOn);
        }
    }

    private void Save_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _state.SetStartWithWindows(StartupSwitch.IsOn);
        _state.SetTrayVisible(TraySwitch.IsOn);
        _state.SetHotkeys(OpenHotkeyText.Text, ToggleHotkeyText.Text);
        HotkeySaveInfo.Severity = _state.HotkeyStatus.IsReady ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        HotkeySaveInfo.Message = _state.HotkeyStatus.IsReady
            ? "Hotkeys were saved and registered with Windows."
            : "Hotkeys were saved, but one or more shortcuts need attention in Diagnostics.";
        UpdateHotkeyRegistrationInfo();
    }

    private void RetryStartup_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _state.SetStartWithWindows(StartupSwitch.IsOn);
    }

    private void UpdateHotkeyRegistrationInfo()
    {
        var status = _state.HotkeyStatus;
        HotkeyRegistrationInfo.Severity = status.IsReady ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        HotkeyRegistrationInfo.Message = status.IsReady
            ? "Both shortcuts are registered with Windows."
            : $"Open: {status.Open.Summary} Toggle: {status.Toggle.Summary}";
    }
}
