using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Pages;

public sealed partial class DiagnosticsPage : Page
{
    public DiagnosticsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
        ((App)Application.Current).State.Changed += (_, _) => DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        var runtime = ((App)Application.Current).State.Runtime;
        MessageText.Text = runtime.LastMessage;
        DetailsText.Text = $"State: {runtime.State}\nProfile: {runtime.AppliedProfile}\nTaskbars updated: {runtime.TaskbarsUpdated}\nLast applied: {runtime.LastAppliedAt:O}";
    }

    private void ApplyDesktop_Click(object sender, RoutedEventArgs e) => ((App)Application.Current).State.ApplyNow(AutomationTrigger.Desktop);
    private void ApplyHover_Click(object sender, RoutedEventArgs e) => ((App)Application.Current).State.ApplyNow(AutomationTrigger.Hover);
    private void ApplyFullscreen_Click(object sender, RoutedEventArgs e) => ((App)Application.Current).State.ApplyNow(AutomationTrigger.Fullscreen);
}
