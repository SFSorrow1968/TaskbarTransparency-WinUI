using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskbarTransparency.Pages;
using Windows.Graphics;

namespace TaskbarTransparency;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.Resize(new SizeInt32(1440, 920));
        AppWindow.SetIcon("Assets/AppIcon.ico");
        ((App)Application.Current).State.ShowWindowRequested += (_, _) => Activate();
        NavFrame.Navigated += (_, _) => AppTitleBar.IsBackButtonVisible = NavFrame.CanGoBack;
        NavigateToInitialPage();
    }

    private void NavigateToInitialPage()
    {
        if (((App)Application.Current).State.Settings.FirstRunCompleted)
        {
            NavigatePage(typeof(HomePage));
            return;
        }

        NavigatePage(typeof(OnboardingPage));
    }

    private void NavigatePage(Type pageType)
    {
        NavFrame.Navigate(pageType);
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        NavFrame.GoBack();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
        }
        else if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "home":
                    if (((App)Application.Current).State.Settings.FirstRunCompleted)
                    {
                        NavigatePage(typeof(HomePage));
                    }
                    else
                    {
                        NavigatePage(typeof(OnboardingPage));
                    }
                    break;
                case "presets":
                    NavigatePage(typeof(PresetsPage));
                    break;
                case "monitors":
                    NavigatePage(typeof(MonitorsPage));
                    break;
                case "automation":
                    NavigatePage(typeof(AutomationPage));
                    break;
                case "diagnostics":
                    NavigatePage(typeof(DiagnosticsPage));
                    break;
                case "about":
                    NavigatePage(typeof(AboutPage));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown navigation item tag: {item.Tag}");
            }
        }
    }
}
