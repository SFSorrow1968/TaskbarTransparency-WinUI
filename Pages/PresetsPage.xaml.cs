using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class PresetsPage : Page
{
    private readonly Services.AppState _state = ((App)Application.Current).State;
    private readonly RefreshCoalescer _refreshCoalescer = new();
    private readonly DispatcherQueueTimer _hoverDistanceCommitTimer;
    private double _pendingHoverDistance;
    private bool _loading = true;
    private bool _profileNameDirty;
    private bool _profileTuningDirty;

    public PresetsPage()
    {
        InitializeComponent();
        _hoverDistanceCommitTimer = DispatcherQueue.CreateTimer();
        _hoverDistanceCommitTimer.Interval = TimeSpan.FromMilliseconds(350);
        _hoverDistanceCommitTimer.Tick += CommitPendingHoverDistance;
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
        if (_hoverDistanceCommitTimer.IsRunning)
        {
            _hoverDistanceCommitTimer.Stop();
            _state.SetHoverDistance(_pendingHoverDistance);
        }
    }

    private void State_Changed(object? sender, EventArgs e)
    {
        _refreshCoalescer.Request(action => DispatcherQueue.TryEnqueue(() => action()), Refresh);
    }

    private void Apply(TaskbarProfile profile)
    {
        _profileNameDirty = false;
        _profileTuningDirty = false;
        _state.SetProfile(profile);
    }

    private void Refresh()
    {
        var settings = _state.Settings;
        var profile = settings.ActiveProfile;
        _loading = true;
        if (!_profileNameDirty)
        {
            PresetNameText.Text = profile.Name;
        }

        if (!_profileTuningDirty)
        {
            PresetOpacitySlider.Value = profile.Opacity;
            PresetOpacityText.Text = $"{PresetOpacitySlider.Value:0}%";
            FadeInDurationSlider.Value = profile.FadeInMilliseconds;
            FadeInDurationText.Text = $"{profile.FadeInMilliseconds} ms";
            FadeOutDurationSlider.Value = profile.FadeOutMilliseconds;
            FadeOutDurationText.Text = $"{profile.FadeOutMilliseconds} ms";
            EasingComboBox.SelectedIndex = profile.Easing switch
            {
                "QuintOut" => 1,
                "Linear" => 2,
                _ => 0
            };
        }

        HoverDistanceSlider.Value = settings.HoverDistance;
        HoverDistanceText.Text = $"{settings.HoverDistance} px";
        AutomationSwitch.IsOn = settings.AutomationEnabled;
        HoverSwitch.IsOn = settings.HoverReveal;
        FullscreenSwitch.IsOn = settings.FullscreenOverlap;
        TraySwitch.IsOn = settings.ShowTrayIcon;
        StartupSwitch.IsOn = settings.StartWithWindows;
        OpenHotkeyText.Text = settings.OpenHotkey;
        ToggleHotkeyText.Text = settings.ToggleHotkey;
        StartupPermissionInfo.Severity = _state.StartupRegistrationFailed ? InfoBarSeverity.Warning : InfoBarSeverity.Success;
        StartupPermissionInfo.Message = _state.StartupStatusMessage;
        _loading = false;
    }

    private void PresetNameText_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading)
        {
            _profileNameDirty = true;
        }
    }

    private void PresetOpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            _profileTuningDirty = true;
            PresetOpacityText.Text = $"{e.NewValue:0}%";
        }
    }

    private void FadeInDurationSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            _profileTuningDirty = true;
            FadeInDurationText.Text = $"{e.NewValue:0} ms";
        }
    }

    private void FadeOutDurationSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            _profileTuningDirty = true;
            FadeOutDurationText.Text = $"{e.NewValue:0} ms";
        }
    }

    private void EasingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading)
        {
            _profileTuningDirty = true;
        }
    }

    private void HoverDistanceSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            _pendingHoverDistance = e.NewValue;
            HoverDistanceText.Text = $"{e.NewValue:0} px";
            _state.PreviewHoverDistance(e.NewValue);
            _hoverDistanceCommitTimer.Stop();
            _hoverDistanceCommitTimer.Start();
        }
    }

    private void CommitPendingHoverDistance(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        _state.SetHoverDistance(_pendingHoverDistance);
    }

    private void AutomationSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetAutomation(AutomationSwitch.IsOn);
        }
    }

    private void HoverSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetHoverReveal(HoverSwitch.IsOn);
        }
    }

    private void FullscreenSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetFullscreenOverlap(FullscreenSwitch.IsOn);
        }
    }

    private void TraySwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetTrayVisible(TraySwitch.IsOn);
        }
    }

    private void StartupSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetStartWithWindows(StartupSwitch.IsOn);
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => Apply(CurrentTuningProfile(TaskbarVisualMode.Clear));
    private void Acrylic_Click(object sender, RoutedEventArgs e) => Apply(CurrentTuningProfile(TaskbarVisualMode.Acrylic));
    private void Mica_Click(object sender, RoutedEventArgs e) => Apply(CurrentTuningProfile(TaskbarVisualMode.Mica));
    private void Solid_Click(object sender, RoutedEventArgs e) => Apply(CurrentTuningProfile(TaskbarVisualMode.Solid));
    private void Reset_Click(object sender, RoutedEventArgs e) => Apply(TaskbarProfile.OxygenClear);

    private void SaveChanges_Click(object sender, RoutedEventArgs e)
    {
        var openHotkey = OpenHotkeyText.Text;
        var toggleHotkey = ToggleHotkeyText.Text;

        if (_profileNameDirty || _profileTuningDirty)
        {
            Apply(CurrentTuningProfile(_state.Settings.ActiveProfile.Mode));
        }

        _state.SetTrayVisible(TraySwitch.IsOn);
        _state.SetStartWithWindows(StartupSwitch.IsOn);
        _state.SetHotkeys(openHotkey, toggleHotkey);
    }

    private TaskbarProfile CurrentTuningProfile(TaskbarVisualMode mode)
    {
        return _state.Settings.ActiveProfile.WithTuningValues(
            PresetNameText.Text,
            (byte)Math.Round(PresetOpacitySlider.Value),
            (int)Math.Round(FadeInDurationSlider.Value),
            (int)Math.Round(FadeOutDurationSlider.Value),
            SelectedEasing()).WithVisualMode(mode);
    }

    private string SelectedEasing()
    {
        if (EasingComboBox.SelectedItem is ComboBoxItem item && item.Tag is string easing)
        {
            return easing;
        }

        return "CubicOut";
    }
}
