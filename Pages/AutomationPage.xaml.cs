using Microsoft.UI.Xaml.Controls;

namespace TaskbarTransparency.Pages;

public sealed partial class AutomationPage : Page
{
    private bool _loading;

    public AutomationPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh()
    {
        var settings = ((App)Microsoft.UI.Xaml.Application.Current).State.Settings;
        _loading = true;
        AutomationSwitch.IsOn = settings.AutomationEnabled;
        HoverSwitch.IsOn = settings.HoverReveal;
        FullscreenSwitch.IsOn = settings.FullscreenOverlap;
        _loading = false;
    }

    private void AutomationSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!_loading)
        {
            ((App)Microsoft.UI.Xaml.Application.Current).State.SetAutomation(AutomationSwitch.IsOn);
        }
    }

    private void HoverSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!_loading)
        {
            ((App)Microsoft.UI.Xaml.Application.Current).State.SetHoverReveal(HoverSwitch.IsOn);
        }
    }

    private void FullscreenSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!_loading)
        {
            ((App)Microsoft.UI.Xaml.Application.Current).State.SetFullscreenOverlap(FullscreenSwitch.IsOn);
        }
    }

    private void Preview_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ((App)Microsoft.UI.Xaml.Application.Current).State.ApplyNow(TaskbarTransparency.Models.AutomationTrigger.WindowVisible);
    }

    private void Save_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ((App)Microsoft.UI.Xaml.Application.Current).State.ApplyNow(TaskbarTransparency.Models.AutomationTrigger.WindowVisible);
    }
}
