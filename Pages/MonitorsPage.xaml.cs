using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TaskbarTransparency.Pages;

public sealed partial class MonitorsPage : Page
{
    public MonitorsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => MonitorList.ItemsSource = ((App)Application.Current).State.Monitors;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        var state = ((App)Application.Current).State;
        state.RefreshMonitors();
        MonitorList.ItemsSource = state.Monitors;
    }
}
