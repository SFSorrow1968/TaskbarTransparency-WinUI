using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using TaskbarTransparency.Pages;
using TaskbarTransparency.Services;
using TaskbarTransparency.Models;
using Windows.Graphics;
using System.Runtime.InteropServices;

namespace TaskbarTransparency;

public sealed partial class MainWindow : Window
{
    private const int ShowWindowHide = 0;
    private const int ShowWindowShow = 5;
    private readonly AppState _state;
    private readonly IntPtr _hwnd;

    public MainWindow()
    {
        InitializeComponent();

        _state = ((App)Application.Current).State;
        _hwnd = WindowNative.GetWindowHandle(this);
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.Resize(new SizeInt32(1280, 880));
        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Closing += AppWindow_Closing;
        _state.AttachWindow(_hwnd);
        _state.ShowWindowRequested += State_ShowWindowRequested;
        NavFrame.Navigated += (_, _) => AppTitleBar.IsBackButtonVisible = NavFrame.CanGoBack;
        NavigatePage(typeof(HomePage));
    }

    private void NavigatePage(Type pageType)
    {
        if (!AppNavigation.ShouldNavigate(NavFrame.Content?.GetType(), pageType))
        {
            return;
        }

        NavFrame.Navigate(pageType);
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (!_state.Settings.ShowTrayIcon || _state.ExitRequested)
        {
            return;
        }

        args.Cancel = true;
        ShowWindow(_hwnd, ShowWindowHide);
    }

    private void State_ShowWindowRequested(object? sender, EventArgs args)
    {
        ShowWindow(_hwnd, ShowWindowShow);
        Activate();
        SelectNavigationItem("home");
        NavigatePage(typeof(HomePage));
    }

    private void SelectNavigationItem(string tag)
    {
        foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            if (Equals(item.Tag, tag))
            {
                NavView.SelectedItem = item;
                return;
            }
        }
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
        if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "home":
                    NavigatePage(typeof(HomePage));
                    break;
                case "rules":
                    NavigatePage(typeof(AutomationPage));
                    break;
                case "monitors":
                    NavigatePage(typeof(MonitorsPage));
                    break;
                case "settings":
                    NavigatePage(typeof(SettingsPage));
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

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int command);
}
