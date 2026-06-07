using Microsoft.UI.Xaml.Controls;

namespace TaskbarTransparency.Pages;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        Loaded += (_, _) => SettingsPathText.Text = ((App)Microsoft.UI.Xaml.Application.Current).State.SettingsPath;
    }
}
