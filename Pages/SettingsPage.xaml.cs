// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace TaskbarTransparency.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _loading;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh()
    {
        var settings = ((App)Microsoft.UI.Xaml.Application.Current).State.Settings;
        _loading = true;
        TraySwitch.IsOn = settings.ShowTrayIcon;
        StartupSwitch.IsOn = settings.StartWithWindows;
        OpenHotkeyText.Text = settings.OpenHotkey;
        ToggleHotkeyText.Text = settings.ToggleHotkey;
        _loading = false;
    }

    private void TraySwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!_loading)
        {
            ((App)Microsoft.UI.Xaml.Application.Current).State.SetTrayVisible(TraySwitch.IsOn);
        }
    }

    private void StartupSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!_loading)
        {
            ((App)Microsoft.UI.Xaml.Application.Current).State.SetStartWithWindows(StartupSwitch.IsOn);
        }
    }

    private void Save_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ((App)Microsoft.UI.Xaml.Application.Current).State.SetStartWithWindows(StartupSwitch.IsOn);
        ((App)Microsoft.UI.Xaml.Application.Current).State.SetTrayVisible(TraySwitch.IsOn);
    }
}
