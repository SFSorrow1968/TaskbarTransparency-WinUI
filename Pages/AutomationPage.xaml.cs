using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class AutomationPage : Page
{
    private readonly AppState _state = ((App)Application.Current).State;
    private readonly RefreshCoalescer _refreshCoalescer = new();
    private readonly DispatcherQueueTimer _ruleCommitTimer;
    private readonly DispatcherQueueTimer _hoverDistanceCommitTimer;
    private AutomationTrigger _pendingRuleTrigger;
    private double _pendingRuleOpacity;
    private double _pendingHoverDistance;
    private bool _loading = true;

    public AutomationPage()
    {
        InitializeComponent();
        _ruleCommitTimer = DispatcherQueue.CreateTimer();
        _ruleCommitTimer.Interval = TimeSpan.FromMilliseconds(350);
        _ruleCommitTimer.Tick += CommitPendingRule;
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
        if (_ruleCommitTimer.IsRunning)
        {
            _ruleCommitTimer.Stop();
            _state.SetRuleOpacity(_pendingRuleTrigger, _pendingRuleOpacity);
        }

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

    private void Refresh()
    {
        var settings = _state.Settings;
        var runtime = _state.Runtime;
        _loading = true;
        AutomationSwitch.IsOn = settings.AutomationEnabled;

        HoverSwitch.IsOn = settings.HoverRule.Enabled;
        HoverOpacitySlider.Value = settings.HoverRule.Opacity;
        HoverOpacityText.Text = $"{settings.HoverRule.Opacity}%";
        HoverOpacitySlider.IsEnabled = settings.HoverRule.Enabled;
        HoverDistanceSlider.Value = settings.HoverDistance;
        HoverDistanceText.Text = $"{settings.HoverDistance} px";
        HoverDistanceSlider.IsEnabled = settings.HoverRule.Enabled;

        FullscreenSwitch.IsOn = settings.FullscreenRule.Enabled;
        FullscreenOpacitySlider.Value = settings.FullscreenRule.Opacity;
        FullscreenOpacityText.Text = $"{settings.FullscreenRule.Opacity}%";
        FullscreenOpacitySlider.IsEnabled = settings.FullscreenRule.Enabled;

        MaximizedSwitch.IsOn = settings.MaximizedRule.Enabled;
        MaximizedOpacitySlider.Value = settings.MaximizedRule.Opacity;
        MaximizedOpacityText.Text = $"{settings.MaximizedRule.Opacity}%";
        MaximizedOpacitySlider.IsEnabled = settings.MaximizedRule.Enabled;

        WindowSwitch.IsOn = settings.WindowRule.Enabled;
        WindowOpacitySlider.Value = settings.WindowRule.Opacity;
        WindowOpacityText.Text = $"{settings.WindowRule.Opacity}%";
        WindowOpacitySlider.IsEnabled = settings.WindowRule.Enabled;

        NowStateText.Text = RuntimeTriggerText.Label(runtime.State);
        NowOpacityText.Text = $"{runtime.ResolvedOpacity}%";
        NowDetailText.Text = settings.AutomationEnabled
            ? $"{runtime.OpacitySource} · {RuntimeTriggerText.Detail(runtime.State)}"
            : "Automation is off; the base opacity is applied everywhere.";
        _loading = false;
    }

    private void AutomationSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            _state.SetAutomationEnabled(AutomationSwitch.IsOn);
        }
    }

    private void RuleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading && sender is ToggleSwitch toggle && TryParseTrigger(toggle.Tag, out var trigger))
        {
            _state.SetRuleEnabled(trigger, toggle.IsOn);
        }
    }

    private void RuleSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_loading || sender is not Slider slider || !TryParseTrigger(slider.Tag, out var trigger))
        {
            return;
        }

        if (_ruleCommitTimer.IsRunning && _pendingRuleTrigger != trigger)
        {
            _ruleCommitTimer.Stop();
            _state.SetRuleOpacity(_pendingRuleTrigger, _pendingRuleOpacity);
        }

        _pendingRuleTrigger = trigger;
        _pendingRuleOpacity = e.NewValue;
        UpdateRuleValueText(trigger, e.NewValue);
        _state.PreviewRuleOpacity(trigger, e.NewValue);
        _ruleCommitTimer.Stop();
        _ruleCommitTimer.Start();
    }

    private void UpdateRuleValueText(AutomationTrigger trigger, double value)
    {
        var text = $"{value:0}%";
        switch (trigger)
        {
            case AutomationTrigger.Hover:
                HoverOpacityText.Text = text;
                break;
            case AutomationTrigger.Fullscreen:
                FullscreenOpacityText.Text = text;
                break;
            case AutomationTrigger.WindowMaximized:
                MaximizedOpacityText.Text = text;
                break;
            case AutomationTrigger.WindowVisible:
                WindowOpacityText.Text = text;
                break;
        }
    }

    private void CommitPendingRule(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        _state.SetRuleOpacity(_pendingRuleTrigger, _pendingRuleOpacity);
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

    private static bool TryParseTrigger(object? tag, out AutomationTrigger trigger)
    {
        if (tag is string text && Enum.TryParse(text, out trigger))
        {
            return true;
        }

        trigger = AutomationTrigger.Desktop;
        return false;
    }
}
