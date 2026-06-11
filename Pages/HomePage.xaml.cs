using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class HomePage : Page
{
    private readonly AppState _state = ((App)Application.Current).State;
    private readonly RefreshCoalescer _refreshCoalescer = new();
    private readonly DispatcherQueueTimer _opacityCommitTimer;
    private double _pendingOpacity;
    private bool _loading = true;

    public HomePage()
    {
        InitializeComponent();
        _opacityCommitTimer = DispatcherQueue.CreateTimer();
        _opacityCommitTimer.Interval = TimeSpan.FromMilliseconds(350);
        _opacityCommitTimer.Tick += CommitPendingOpacity;
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
        if (_opacityCommitTimer.IsRunning)
        {
            _opacityCommitTimer.Stop();
            _state.SetBaseOpacity(_pendingOpacity);
        }
    }

    private void State_Changed(object? sender, EventArgs e)
    {
        _refreshCoalescer.Request(action => DispatcherQueue.TryEnqueue(() => action()), Refresh);
    }

    private void Refresh()
    {
        _loading = true;
        var runtime = _state.Runtime;
        var settings = _state.Settings;

        AppliedOpacityText.Text = $"{runtime.ResolvedOpacity}%";
        AppliedSourceText.Text = runtime.OpacitySource;
        AppliedStateText.Text = $"Current state: {RuntimeTriggerText.Label(runtime.State)}";
        AppliedDetailText.Text = runtime.TaskbarsUpdated == 0
            ? "No taskbar windows were found"
            : $"Applied to {runtime.TaskbarsUpdated} taskbar{(runtime.TaskbarsUpdated == 1 ? string.Empty : "s")}";
        PauseButton.Content = _state.TransparencyPaused ? "Resume transparency" : "Pause transparency";

        OpacitySlider.Value = settings.BaseOpacity;
        OpacityValueBoxText.Text = $"{settings.BaseOpacity}%";
        OverrideInfo.IsOpen = !_state.TransparencyPaused && runtime.OpacitySource != OpacityPolicy.BaseSource;

        AutomationSwitch.IsOn = settings.AutomationEnabled;
        AutomationDetailText.Text = settings.AutomationEnabled
            ? "Rules react to hover, fullscreen, and windows."
            : "Base opacity is used everywhere.";
        TaskbarsText.Text = runtime.TaskbarsUpdated.ToString();
        TaskbarsDetailText.Text = runtime.TaskbarsUpdated == 0 ? "Waiting for taskbar" : "Detected and controlled";
        LastAppliedText.Text = runtime.LastAppliedAt.ToString("h:mm tt");
        LastMessageText.Text = runtime.LastMessage;
        _loading = false;
    }

    private void OpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_loading)
        {
            _pendingOpacity = e.NewValue;
            OpacityValueBoxText.Text = $"{e.NewValue:0}%";
            _state.PreviewBaseOpacity(e.NewValue);
            _opacityCommitTimer.Stop();
            _opacityCommitTimer.Start();
        }
    }

    private void CommitPendingOpacity(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        _state.SetBaseOpacity(_pendingOpacity);
    }

    private void AutomationSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetAutomationEnabled(AutomationSwitch.IsOn);
        }
    }

    private void Pause_Click(object sender, RoutedEventArgs e) => _state.ToggleTransparency();
    private void Reapply_Click(object sender, RoutedEventArgs e) => _state.ReapplyNow();
}
