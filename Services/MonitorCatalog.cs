using System.Runtime.InteropServices;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

public sealed class MonitorCatalog
{
    public IReadOnlyList<MonitorProfile> GetCurrent()
    {
        var taskbars = EnumerateTaskbars().ToArray();
        if (taskbars.Length == 0)
        {
            return [];
        }

        var profiles = new List<MonitorProfile>();
        for (var index = 0; index < taskbars.Length; index++)
        {
            var taskbar = taskbars[index];
            var monitor = MonitorFromWindow(taskbar.Handle, 2);
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            var hasInfo = monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info);
            var isPrimary = taskbar.ClassName == "Shell_TrayWnd" || (hasInfo && (info.Flags & 1) == 1);
            var deviceName = hasInfo && !string.IsNullOrWhiteSpace(info.DeviceName)
                ? info.DeviceName
                : taskbar.ClassName;
            profiles.Add(new MonitorProfile
            {
                DeviceName = deviceName,
                FriendlyName = isPrimary ? "Primary display" : $"Display {index + 1}",
                IsPrimary = isPrimary,
                SyncWithPrimary = isPrimary,
                OverrideOpacity = isPrimary ? (byte)32 : (byte)64
            });
        }

        return profiles
            .OrderByDescending(profile => profile.IsPrimary)
            .ThenBy(profile => profile.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<TaskbarWindow> EnumerateTaskbars()
    {
        var primary = FindWindow("Shell_TrayWnd", null);
        if (primary != IntPtr.Zero)
        {
            yield return new TaskbarWindow(primary, "Shell_TrayWnd");
        }

        var current = IntPtr.Zero;
        while (true)
        {
            current = FindWindowEx(IntPtr.Zero, current, "Shell_SecondaryTrayWnd", null);
            if (current == IntPtr.Zero)
            {
                yield break;
            }

            yield return new TaskbarWindow(current, "Shell_SecondaryTrayWnd");
        }
    }

    private readonly record struct TaskbarWindow(IntPtr Handle, string ClassName);

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
