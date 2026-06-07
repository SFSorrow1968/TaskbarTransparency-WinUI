using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Pages;

public sealed partial class OnboardingPage : Page
{
    public OnboardingPage()
    {
        InitializeComponent();
        Loaded += (_, _) => SettingsPathText.Text = $"Settings are stored locally at {((App)Application.Current).State.SettingsPath}";
    }

    private void StartClear_Click(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).State.CompleteFirstRun(TaskbarProfile.OxygenClear);
        Frame.Navigate(typeof(HomePage));
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        ImportInfo.IsOpen = true;
    }
}
