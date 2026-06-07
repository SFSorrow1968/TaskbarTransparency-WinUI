using System.Runtime.InteropServices;
using System.Text;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

public sealed class RuntimeStateSensorService : IDisposable
{
    private readonly Timer _timer;
    private readonly Func<AppSettings> _getSettings;
    private Action<AutomationTrigger>? _stateChanged;
    private AutomationTrigger? _lastTrigger;

    public RuntimeStateSensorService(Func<AppSettings> getSettings)
    {
        _getSettings = getSettings;
        _timer = new Timer(_ => Tick(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Start(Action<AutomationTrigger> stateChanged)
    {
        _stateChanged = stateChanged;
        _timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public void Stop() => _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

    public void Dispose() => _timer.Dispose();

    private void Tick()
    {
        AutomationTrigger trigger;
        try
        {
            trigger = DetectCurrentTrigger();
        }
        catch
        {
            trigger = AutomationTrigger.Desktop;
        }

        if (trigger == _lastTrigger)
        {
            return;
        }

        _lastTrigger = trigger;
        _stateChanged?.Invoke(trigger);
    }

    public static AutomationTrigger ResolveTrigger(
        bool automationEnabled,
        bool hoverRevealEnabled,
        bool fullscreenOverlapEnabled,
        bool isMouseNearTaskbar,
        bool hasForegroundWindow,
        bool isForegroundMaximized,
        bool isForegroundFullscreen)
    {
        if (!automationEnabled)
        {
            return AutomationTrigger.Desktop;
        }

        if (hoverRevealEnabled && isMouseNearTaskbar)
        {
            return AutomationTrigger.Hover;
        }

        if (fullscreenOverlapEnabled && isForegroundFullscreen)
        {
            return AutomationTrigger.Fullscreen;
        }

        if (isForegroundMaximized)
        {
            return AutomationTrigger.WindowMaximized;
        }

        return hasForegroundWindow ? AutomationTrigger.WindowVisible : AutomationTrigger.Desktop;
    }

    private AutomationTrigger DetectCurrentTrigger()
    {
        var foreground = GetForegroundWindow();
        var hasForeground = foreground != IntPtr.Zero && !IsShellWindow(foreground);
        var maximized = hasForeground && IsZoomed(foreground);
        var fullscreen = hasForeground && IsFullscreen(foreground);
        var settings = _getSettings();
        var nearTaskbar = IsMouseNearAnyTaskbar(settings.HoverDistance);
        return ResolveTrigger(
            settings.AutomationEnabled,
            settings.HoverReveal,
            settings.FullscreenOverlap,
            nearTaskbar,
            hasForeground,
            maximized,
            fullscreen);
    }

    private static bool IsShellWindow(IntPtr window)
    {
        var className = new StringBuilder(128);
        var length = GetClassName(window, className, className.Capacity);
        if (length <= 0)
        {
            return false;
        }

        var name = className.ToString();
        return name is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Progman" or "WorkerW";
    }

    private static bool IsFullscreen(IntPtr window)
    {
        var monitor = MonitorFromWindow(window, 2);
        if (monitor == IntPtr.Zero || !GetWindowRect(window, out var windowRect))
        {
            return false;
        }

        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        return windowRect.Left <= info.Monitor.Left
            && windowRect.Top <= info.Monitor.Top
            && windowRect.Right >= info.Monitor.Right
            && windowRect.Bottom >= info.Monitor.Bottom;
    }

    public static bool IsPointNearRectForTest(int x, int y, int left, int top, int right, int bottom, int distance)
    {
        return IsNear(new Point { X = x, Y = y }, new Rect { Left = left, Top = top, Right = right, Bottom = bottom }, distance);
    }

    private static bool IsMouseNearAnyTaskbar(int hoverDistance)
    {
        if (!GetCursorPos(out var point))
        {
            return false;
        }

        foreach (var handle in EnumerateTaskbars())
        {
            if (GetWindowRect(handle, out var rect) && IsNear(point, rect, hoverDistance))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNear(Point point, Rect rect, int distance)
    {
        var clampedDistance = Math.Clamp(distance, 0, 48);
        return point.X >= rect.Left - clampedDistance
            && point.X <= rect.Right + clampedDistance
            && point.Y >= rect.Top - clampedDistance
            && point.Y <= rect.Bottom + clampedDistance;
    }

    private static IEnumerable<IntPtr> EnumerateTaskbars()
    {
        var primary = FindWindow("Shell_TrayWnd", null);
        if (primary != IntPtr.Zero)
        {
            yield return primary;
        }

        var current = IntPtr.Zero;
        while (true)
        {
            current = FindWindowEx(IntPtr.Zero, current, "Shell_SecondaryTrayWnd", null);
            if (current == IntPtr.Zero)
            {
                yield break;
            }

            yield return current;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect WorkArea;
        public int Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string? windowTitle);
}
