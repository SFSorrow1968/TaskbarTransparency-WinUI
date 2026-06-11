using System.Runtime.InteropServices;
using System.Text;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

public sealed record SensorSnapshot(AutomationTrigger BaseTrigger, IReadOnlyList<IntPtr> HoveredTaskbars)
{
    public static SensorSnapshot Idle { get; } = new(AutomationTrigger.Desktop, []);

    public bool Matches(SensorSnapshot? other)
    {
        if (other is null || BaseTrigger != other.BaseTrigger || HoveredTaskbars.Count != other.HoveredTaskbars.Count)
        {
            return false;
        }

        for (var index = 0; index < HoveredTaskbars.Count; index++)
        {
            if (HoveredTaskbars[index] != other.HoveredTaskbars[index])
            {
                return false;
            }
        }

        return true;
    }
}

public sealed class RuntimeStateSensorService : IDisposable
{
    private readonly Timer _timer;
    private readonly Func<AppSettings> _getSettings;
    private readonly StringBuilder _classNameBuffer = new(128);
    private Action<SensorSnapshot>? _stateChanged;
    private SensorSnapshot? _lastSnapshot;
    private int _isTicking;

    public RuntimeStateSensorService(Func<AppSettings> getSettings)
    {
        _getSettings = getSettings;
        _timer = new Timer(_ => Tick(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Start(Action<SensorSnapshot> stateChanged)
    {
        _stateChanged = stateChanged;
        _timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public void Stop() => _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

    public void Dispose() => _timer.Dispose();

    private void Tick()
    {
        if (Interlocked.Exchange(ref _isTicking, 1) == 1)
        {
            return;
        }

        SensorSnapshot snapshot;
        try
        {
            snapshot = DetectSnapshot();
        }
        catch
        {
            snapshot = SensorSnapshot.Idle;
        }

        try
        {
            if (snapshot.Matches(_lastSnapshot))
            {
                return;
            }

            _lastSnapshot = snapshot;
            _stateChanged?.Invoke(snapshot);
        }
        finally
        {
            Volatile.Write(ref _isTicking, 0);
        }
    }

    public static AutomationTrigger ResolveTrigger(
        bool automationEnabled,
        bool fullscreenOverlapEnabled,
        bool hasForegroundWindow,
        bool isForegroundMaximized,
        bool isForegroundFullscreen)
    {
        if (!automationEnabled)
        {
            return AutomationTrigger.Desktop;
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

    private SensorSnapshot DetectSnapshot()
    {
        var settings = _getSettings();
        if (!settings.AutomationEnabled)
        {
            return SensorSnapshot.Idle;
        }

        var hovered = settings.HoverRule.Enabled
            ? FindHoveredTaskbars(settings.HoverDistance)
            : [];

        var foreground = GetForegroundWindow();
        var hasForeground = foreground != IntPtr.Zero && !IsShellWindow(foreground);
        var maximized = hasForeground && IsZoomed(foreground);
        var fullscreen = settings.FullscreenRule.Enabled && hasForeground && IsFullscreen(foreground);
        var baseTrigger = ResolveTrigger(
            settings.AutomationEnabled,
            settings.FullscreenRule.Enabled,
            hasForeground,
            maximized,
            fullscreen);
        return new SensorSnapshot(baseTrigger, hovered);
    }

    private bool IsShellWindow(IntPtr window)
    {
        _classNameBuffer.Clear();
        var length = GetClassName(window, _classNameBuffer, _classNameBuffer.Capacity);
        if (length <= 0)
        {
            return false;
        }

        return IsShellClassName(_classNameBuffer.ToString());
    }

    public static bool IsShellClassNameForTest(string className) => IsShellClassName(className);

    private static bool IsShellClassName(string className)
    {
        return className is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Progman" or "WorkerW";
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

    private static IReadOnlyList<IntPtr> FindHoveredTaskbars(int hoverDistance)
    {
        if (!GetCursorPos(out var point))
        {
            return [];
        }

        List<IntPtr>? hovered = null;
        foreach (var taskbar in TaskbarWindowCatalog.GetCurrent())
        {
            if (GetWindowRect(taskbar.Handle, out var rect) && IsNear(point, rect, hoverDistance))
            {
                hovered ??= [];
                hovered.Add(taskbar.Handle);
            }
        }

        return hovered ?? (IReadOnlyList<IntPtr>)[];
    }

    private static bool IsNear(Point point, Rect rect, int distance)
    {
        var clampedDistance = Math.Clamp(distance, 0, 48);
        return point.X >= rect.Left - clampedDistance
            && point.X <= rect.Right + clampedDistance
            && point.Y >= rect.Top - clampedDistance
            && point.Y <= rect.Bottom + clampedDistance;
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

}
