using System.Diagnostics;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Pages;

public sealed partial class AboutPage : Page
{
    private readonly AboutMetadata _metadata = AboutMetadata.FromAssembly(Assembly.GetExecutingAssembly());

    public AboutPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh()
    {
        VersionText.Text = _metadata.Version;
        SettingsPathText.Text = ((App)Microsoft.UI.Xaml.Application.Current).State.SettingsPath;
    }

    private void ViewLogs_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var folder = FindLauncherLogFolder();
        Directory.CreateDirectory(folder);
        OpenPath(folder);
    }

    private void OpenSettingsFile_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var settingsPath = ((App)Microsoft.UI.Xaml.Application.Current).State.SettingsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        if (!File.Exists(settingsPath))
        {
            File.WriteAllText(settingsPath, "{}");
        }

        OpenPath(settingsPath);
    }

    private static void OpenPath(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
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
}
