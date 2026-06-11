using System.Diagnostics;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TaskbarTransparency.Services;

namespace TaskbarTransparency.Pages;

public sealed partial class DiagnosticsPage : Page
{
    private readonly AppState _state = ((App)Application.Current).State;
    private readonly RefreshCoalescer _refreshCoalescer = new();
    private int _timelineVersion = -1;

    public DiagnosticsPage()
    {
        InitializeComponent();
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
    }

    private void State_Changed(object? sender, EventArgs e)
    {
        _refreshCoalescer.Request(action => DispatcherQueue.TryEnqueue(() => action()), Refresh);
    }

    private void Refresh()
    {
        var runtime = _state.Runtime;
        var taskbarsFound = runtime.TaskbarsUpdated > 0;
        StatusTitleText.Text = taskbarsFound
            ? $"{runtime.TaskbarsUpdated} taskbar{(runtime.TaskbarsUpdated == 1 ? string.Empty : "s")} detected"
            : "No taskbar window detected";
        StatusDetailText.Text = taskbarsFound
            ? $"{runtime.LastMessage} at {runtime.LastAppliedAt:h:mm:ss tt}."
            : "Oxygen Taskbar could not find a taskbar window to control.";
        StatusHintText.Text = taskbarsFound
            ? string.Empty
            : "Windows Explorer may be restarting, or an unsupported shell replacement is in use. Retry once the taskbar is visible.";
        StatusHintText.Visibility = taskbarsFound ? Visibility.Collapsed : Visibility.Visible;
        StatusIcon.Glyph = taskbarsFound ? "" : "";
        StatusIcon.Foreground = (Brush)Application.Current.Resources[taskbarsFound ? "OxygenMintBrush" : "OxygenDangerBrush"];
        StatusIconBorder.Background = new SolidColorBrush(taskbarsFound
            ? Color.FromArgb(0x33, 0x23, 0xC5, 0x8E)
            : Color.FromArgb(0x33, 0xFF, 0x6B, 0x78));

        if (_timelineVersion != runtime.RecentEventsVersion)
        {
            _timelineVersion = runtime.RecentEventsVersion;
            var rows = new List<ActivityRow>(runtime.RecentEvents.Count);
            foreach (var item in runtime.RecentEvents)
            {
                rows.Add(new ActivityRow(
                    item.Time.ToString("h:mm:ss tt"),
                    RuntimeTriggerText.Label(item.State),
                    $"{item.Source} · {item.TaskbarsUpdated} taskbar{(item.TaskbarsUpdated == 1 ? string.Empty : "s")}",
                    $"{item.Opacity}%"));
            }

            ActivityList.ItemsSource = rows;
        }

        var hotkeyStatus = _state.HotkeyStatus;
        HotkeyInfo.Severity = hotkeyStatus.IsReady ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        HotkeyInfo.Message = hotkeyStatus.IsReady
            ? "Both shortcuts are registered and ready."
            : $"Open: {hotkeyStatus.Open.Summary}\nToggle: {hotkeyStatus.Toggle.Summary}";
    }

    private void Reapply_Click(object sender, RoutedEventArgs e) => _state.RefreshMonitors();
    private void ResetHotkeys_Click(object sender, RoutedEventArgs e) => _state.ResetHotkeys();

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = FindLauncherLogFolder();
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }

    private static string FindLauncherLogFolder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "launcher-logs");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "launcher-logs");
    }

    private sealed record ActivityRow(string TimeText, string State, string Detail, string OpacityText);
}
