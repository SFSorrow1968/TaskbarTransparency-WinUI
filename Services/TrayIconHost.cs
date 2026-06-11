using System.Runtime.InteropServices;

namespace TaskbarTransparency.Services;

public sealed class TrayIconHost : IDisposable
{
    private const int CallbackMessage = 0x8001;
    private const int LeftButtonDoubleClick = 0x0203;
    private const int RightButtonUp = 0x0205;
    private const int ImageIcon = 1;
    private const int LoadFromFile = 0x0010;
    private const uint NotifyAdd = 0;
    private const uint NotifyModify = 1;
    private const uint NotifyDelete = 2;
    private const uint NotifyVersion = 4;
    private const uint IconMessage = 0x00000001;
    private const uint IconIcon = 0x00000002;
    private const uint IconTip = 0x00000004;
    private const uint IconGuid = 0x00000020;
    private const uint IconVersion4 = 4;
    private const uint TrackReturnCommand = 0x0100;
    private const uint TrackRightButton = 0x0002;
    private const uint MenuString = 0;
    private const uint MenuSeparator = 0x0800;
    internal const int OpenCommand = 1;
    internal const int ApplyCommand = 2;
    internal const int ToggleCommand = 3;
    internal const int ExitCommand = 4;
    internal const string OpenCommandText = "Open Oxygen Taskbar";
    internal const string ApplyCommandText = "Reapply Now";
    internal const string ToggleCommandText = "Pause or Resume Transparency";
    internal const string ExitCommandText = "Exit";
    private readonly Guid _iconId = new("a5b8d7f6-8a4f-44ff-9c16-565457be2e23");
    private readonly WndProc _wndProc;
    private IntPtr _windowHandle;
    private IntPtr _iconHandle;
    private Action? _showWindow;
    private Action? _applyNow;
    private Action? _toggleTransparency;
    private Action? _exit;
    private bool _added;

    public TrayIconHost()
    {
        _wndProc = WindowProc;
    }

    public void Start(Action showWindow, Action applyNow, Action toggleTransparency, Action exit)
    {
        _showWindow = showWindow;
        _applyNow = applyNow;
        _toggleTransparency = toggleTransparency;
        _exit = exit;
        EnsureWindow();
        EnsureIcon();
    }

    internal void ConfigureCommandsForTest(Action showWindow, Action applyNow, Action toggleTransparency, Action exit)
    {
        _showWindow = showWindow;
        _applyNow = applyNow;
        _toggleTransparency = toggleTransparency;
        _exit = exit;
    }

    internal bool ExecuteCommandForTest(int command) => ExecuteCommand(command);

    public void SetVisible(bool visible)
    {
        if (_windowHandle == IntPtr.Zero || _iconHandle == IntPtr.Zero)
        {
            return;
        }

        if (visible && !_added)
        {
            var data = CreateIconData();
            Shell_NotifyIcon(NotifyAdd, ref data);
            data.VersionOrTimeout = IconVersion4;
            Shell_NotifyIcon(NotifyVersion, ref data);
            _added = true;
        }
        else if (!visible && _added)
        {
            var data = CreateIconData();
            Shell_NotifyIcon(NotifyDelete, ref data);
            _added = false;
        }
        else if (visible)
        {
            var data = CreateIconData();
            Shell_NotifyIcon(NotifyModify, ref data);
        }
    }

    public void Dispose()
    {
        if (_added)
        {
            var data = CreateIconData();
            Shell_NotifyIcon(NotifyDelete, ref data);
        }

        if (_iconHandle != IntPtr.Zero)
        {
            DestroyIcon(_iconHandle);
        }

        if (_windowHandle != IntPtr.Zero)
        {
            DestroyWindow(_windowHandle);
        }

        _added = false;
        _iconHandle = IntPtr.Zero;
        _windowHandle = IntPtr.Zero;
    }

    private void EnsureWindow()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            return;
        }

        var className = "OxygenTaskbarTrayWindow";
        var windowClass = new WindowClass
        {
            ClassName = className,
            WindowProc = Marshal.GetFunctionPointerForDelegate(_wndProc)
        };
        RegisterClass(ref windowClass);
        _windowHandle = CreateWindowEx(0, className, string.Empty, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
    }

    private void EnsureIcon()
    {
        if (_iconHandle != IntPtr.Zero)
        {
            return;
        }

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        _iconHandle = LoadImage(IntPtr.Zero, iconPath, ImageIcon, 16, 16, LoadFromFile);
        if (_iconHandle == IntPtr.Zero)
        {
            _iconHandle = LoadIcon(IntPtr.Zero, new IntPtr(32512));
        }
    }

    private NotifyIconData CreateIconData()
    {
        return new NotifyIconData
        {
            Size = Marshal.SizeOf<NotifyIconData>(),
            WindowHandle = _windowHandle,
            Id = 1,
            Flags = IconMessage | IconIcon | IconTip | IconGuid,
            CallbackMessage = CallbackMessage,
            Icon = _iconHandle,
            Tip = "Oxygen Taskbar",
            GuidItem = _iconId
        };
    }

    private IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == CallbackMessage)
        {
            switch (lParam.ToInt32())
            {
                case LeftButtonDoubleClick:
                    _showWindow?.Invoke();
                    return IntPtr.Zero;
                case RightButtonUp:
                    ShowContextMenu();
                    return IntPtr.Zero;
            }
        }

        return DefWindowProc(hwnd, message, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        var menu = CreatePopupMenu();
        AppendMenu(menu, MenuString, OpenCommand, OpenCommandText);
        AppendMenu(menu, MenuString, ToggleCommand, ToggleCommandText);
        AppendMenu(menu, MenuString, ApplyCommand, ApplyCommandText);
        AppendMenu(menu, MenuSeparator, 0, null);
        AppendMenu(menu, MenuString, ExitCommand, ExitCommandText);

        GetCursorPos(out var point);
        SetForegroundWindow(_windowHandle);
        var command = TrackPopupMenu(menu, TrackReturnCommand | TrackRightButton, point.X, point.Y, 0, _windowHandle, IntPtr.Zero);
        DestroyMenu(menu);
        ExecuteCommand(command);
    }

    private bool ExecuteCommand(int command)
    {
        switch (command)
        {
            case OpenCommand:
                _showWindow?.Invoke();
                return true;
            case ApplyCommand:
                _applyNow?.Invoke();
                return true;
            case ToggleCommand:
                _toggleTransparency?.Invoke();
                return true;
            case ExitCommand:
                _exit?.Invoke();
                return true;
            default:
                return false;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        public uint Style;
        public IntPtr WindowProc;
        public int ClassExtra;
        public int WindowExtra;
        public IntPtr Instance;
        public IntPtr Icon;
        public IntPtr Cursor;
        public IntPtr Background;
        public string? MenuName;
        public string ClassName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int Size;
        public IntPtr WindowHandle;
        public uint Id;
        public uint Flags;
        public int CallbackMessage;
        public IntPtr Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Tip;
        public uint State;
        public uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Info;
        public uint VersionOrTimeout;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string InfoTitle;
        public uint InfoFlags;
        public Guid GuidItem;
        public IntPtr BalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClass(ref WindowClass lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName, int style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadImage(IntPtr instance, string name, int type, int cx, int cy, int load);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr instance, IntPtr iconName);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr icon);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr menu, uint flags, int itemId, string? itemText);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr menu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr hwnd, IntPtr rect);

    private delegate IntPtr WndProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
}
