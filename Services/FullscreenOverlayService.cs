using System.Runtime.InteropServices;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

/// <summary>
/// Keeps the taskbar visible above fullscreen windows. Fullscreen apps normally cover the
/// taskbar; the only supported way to keep it on top is Windows auto-hide. While the
/// fullscreen rule is active this service forces auto-hide on, then holds the taskbar in a
/// fade-in that never ends: it is re-shown at its original position on every tick, so the
/// auto-hide never takes effect and the bar stays overlaid at the rule opacity.
/// </summary>
public sealed class FullscreenOverlayService : IDisposable
{
    private const uint AbmGetState = 0x00000004;
    private const uint AbmSetState = 0x0000000A;
    private const int AbsAutoHide = 0x00000001;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private static readonly IntPtr TopMost = new(-1);

    private readonly Timer _maintenanceTimer;
    private readonly object _sync = new();
    private IReadOnlyList<TaskbarPlacement>? _placements;
    private bool _autoHideForced;

    public bool IsActive { get; private set; }

    public FullscreenOverlayService()
    {
        _maintenanceTimer = new Timer(_ => Maintain(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public static bool ShouldOverlay(bool transparencyPaused, bool automationEnabled, bool fullscreenRuleEnabled, AutomationTrigger trigger)
    {
        return !transparencyPaused
            && automationEnabled
            && fullscreenRuleEnabled
            && trigger == AutomationTrigger.Fullscreen;
    }

    /// <summary>Returns true when the overlay was newly engaged by this call.</summary>
    public bool Update(bool shouldOverlay, IReadOnlyList<TaskbarWindowInfo> taskbars)
    {
        if (shouldOverlay)
        {
            return Enter(taskbars);
        }

        Exit();
        return false;
    }

    private bool Enter(IReadOnlyList<TaskbarWindowInfo> taskbars)
    {
        lock (_sync)
        {
            if (IsActive)
            {
                return false;
            }

            _placements = CapturePlacements(taskbars);
            if ((GetAppBarState() & AbsAutoHide) == 0)
            {
                SetAppBarAutoHide(true);
                _autoHideForced = true;
            }

            IsActive = true;
            _maintenanceTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
            return true;
        }
    }

    public void Exit()
    {
        lock (_sync)
        {
            if (!IsActive)
            {
                return;
            }

            _maintenanceTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            if (_autoHideForced)
            {
                SetAppBarAutoHide(false);
                _autoHideForced = false;
            }

            _placements = null;
            IsActive = false;
        }
    }

    public void Dispose()
    {
        Exit();
        _maintenanceTimer.Dispose();
    }

    private void Maintain()
    {
        var placements = _placements;
        if (placements is null)
        {
            return;
        }

        foreach (var placement in placements)
        {
            SetWindowPos(
                placement.Handle,
                TopMost,
                placement.Bounds.Left,
                placement.Bounds.Top,
                placement.Bounds.Right - placement.Bounds.Left,
                placement.Bounds.Bottom - placement.Bounds.Top,
                SwpNoActivate | SwpShowWindow);
        }
    }

    private static IReadOnlyList<TaskbarPlacement> CapturePlacements(IReadOnlyList<TaskbarWindowInfo> taskbars)
    {
        var placements = new List<TaskbarPlacement>(taskbars.Count);
        foreach (var taskbar in taskbars)
        {
            if (GetWindowRect(taskbar.Handle, out var bounds))
            {
                placements.Add(new TaskbarPlacement(taskbar.Handle, bounds));
            }
        }

        return placements;
    }

    private static int GetAppBarState()
    {
        var data = CreateAppBarData();
        return (int)SHAppBarMessage(AbmGetState, ref data);
    }

    private static void SetAppBarAutoHide(bool enabled)
    {
        var data = CreateAppBarData();
        data.Window = FindWindow(TaskbarWindowCatalog.PrimaryTaskbarClassName, null);
        var state = GetAppBarState();
        data.LParam = new IntPtr(enabled ? state | AbsAutoHide : state & ~AbsAutoHide);
        SHAppBarMessage(AbmSetState, ref data);
    }

    private static AppBarData CreateAppBarData()
    {
        return new AppBarData { Size = Marshal.SizeOf<AppBarData>() };
    }

    private readonly record struct TaskbarPlacement(IntPtr Handle, Rect Bounds);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AppBarData
    {
        public int Size;
        public IntPtr Window;
        public uint CallbackMessage;
        public uint Edge;
        public Rect Bounds;
        public IntPtr LParam;
    }

    [DllImport("shell32.dll")]
    private static extern UIntPtr SHAppBarMessage(uint message, ref AppBarData data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string className, string? windowName);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
