using System.Runtime.InteropServices;

namespace TaskbarTransparency.Services;

public static class TaskbarWindowCatalog
{
    public const string PrimaryTaskbarClassName = "Shell_TrayWnd";
    public const string SecondaryTaskbarClassName = "Shell_SecondaryTrayWnd";

    public static IReadOnlyList<TaskbarWindowInfo> GetCurrent()
    {
        return EnumerateCurrent().ToArray();
    }

    private static IEnumerable<TaskbarWindowInfo> EnumerateCurrent()
    {
        var primary = FindWindow(PrimaryTaskbarClassName, null);
        if (primary != IntPtr.Zero)
        {
            yield return BuildInfo(primary, PrimaryTaskbarClassName);
        }

        var current = IntPtr.Zero;
        while (true)
        {
            current = FindWindowEx(IntPtr.Zero, current, SecondaryTaskbarClassName, null);
            if (current == IntPtr.Zero)
            {
                yield break;
            }

            yield return BuildInfo(current, SecondaryTaskbarClassName);
        }
    }

    private static TaskbarWindowInfo BuildInfo(IntPtr handle, string className)
    {
        var monitor = MonitorFromWindow(handle, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        var hasInfo = monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info);
        var isPrimary = IsPrimaryClass(className) || (hasInfo && (info.Flags & 1) == 1);
        var deviceName = hasInfo && !string.IsNullOrWhiteSpace(info.DeviceName)
            ? info.DeviceName
            : className;

        return new TaskbarWindowInfo(handle, className, deviceName, isPrimary);
    }

    public static bool IsPrimaryClassForTest(string className) => IsPrimaryClass(className);

    private static bool IsPrimaryClass(string className)
    {
        return string.Equals(className, PrimaryTaskbarClassName, StringComparison.Ordinal);
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

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string? windowTitle);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);
}

public readonly record struct TaskbarWindowInfo(IntPtr Handle, string ClassName, string DeviceName, bool IsPrimary);
