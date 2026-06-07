using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Pages;

public sealed partial class PresetsPage : Page
{
    private bool _loading;

    public PresetsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private static void Apply(TaskbarProfile profile) => ((App)Application.Current).State.SetProfile(profile);
    private void Refresh()
    {
        _loading = true;
        PresetOpacitySlider.Value = ((App)Application.Current).State.Settings.ActiveProfile.Opacity;
        PresetOpacityText.Text = $"{PresetOpacitySlider.Value:0}%";
        _loading = false;
    }

    private void PresetOpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            PresetOpacityText.Text = $"{e.NewValue:0}%";
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => Apply(TaskbarProfile.OxygenClear);
    private void Glass_Click(object sender, RoutedEventArgs e) => Apply(TaskbarProfile.FocusGlass);
    private void Solid_Click(object sender, RoutedEventArgs e) => Apply(TaskbarProfile.NightSolid);
    private void SaveFocusGlass_Click(object sender, RoutedEventArgs e) => Apply(TaskbarProfile.FocusGlass with { Opacity = (byte)Math.Round(PresetOpacitySlider.Value) });
}
