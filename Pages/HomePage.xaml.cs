using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TaskbarTransparency.Pages;

public sealed partial class HomePage : Page
{
    private readonly AppState _state = ((App)Microsoft.UI.Xaml.Application.Current).State;
    private bool _loading;

    public HomePage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
        _state.Changed += (_, _) => DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        _loading = true;
        ProfileText.Text = _state.Settings.ActiveProfile.Name;
        OpacityText.Text = $"{_state.Settings.ActiveProfile.Opacity}%";
        OpacityValueBoxText.Text = $"{_state.Settings.ActiveProfile.Opacity}%";
        TaskbarsText.Text = _state.Runtime.TaskbarsUpdated.ToString();
        ServiceStatusText.Text = _state.Runtime.TaskbarsUpdated > 0 ? "Running" : "Waiting for taskbar";
        CurrentMaterialText.Text = _state.Settings.ActiveProfile.Mode.ToString();
        RuntimeMessageText.Text = _state.Runtime.LastMessage;
        RuntimeTimeText.Text = _state.Runtime.LastAppliedAt.ToString("MMM d, h:mm tt");
        SyncStateText.Text = _state.Monitors.Count <= 1 ? "Primary taskbar in sync" : "All taskbars in sync";
        OpacitySlider.Value = _state.Settings.ActiveProfile.Opacity;
        _loading = false;
    }

    private void OpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetOpacity(e.NewValue);
        }
    }

    private void ApplyClear_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => _state.SetProfile(TaskbarProfile.OxygenClear);
    private void ApplyGlass_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => _state.SetProfile(TaskbarProfile.FocusGlass);
    private void ApplySolid_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => _state.SetProfile(TaskbarProfile.NightSolid);
    private void ApplyNow_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => _state.ApplyNow();
}
