using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Pages;

public sealed partial class PresetsPage : Page
{
    public PresetsPage()
    {
        InitializeComponent();
    }

    private static void Apply(TaskbarProfile profile) => ((App)Application.Current).State.SetProfile(profile);
    private void Clear_Click(object sender, RoutedEventArgs e) => Apply(TaskbarProfile.OxygenClear);
    private void Glass_Click(object sender, RoutedEventArgs e) => Apply(TaskbarProfile.FocusGlass);
    private void Solid_Click(object sender, RoutedEventArgs e) => Apply(TaskbarProfile.NightSolid);
}
