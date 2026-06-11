using System.Runtime.InteropServices;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

/// <summary>
/// Overlays a live image of each taskbar above fullscreen windows. Windows refuses to raise
/// the real taskbar above a fullscreen ("rude") window — SetWindowPos reports success but the
/// topmost flag is silently discarded — so instead this service creates its own click-through
/// topmost window per taskbar and renders the taskbar into it with a DWM thumbnail. The
/// thumbnail fades in to the fullscreen rule opacity and holds there until fullscreen ends.
/// </summary>
public sealed class FullscreenOverlayService : IDisposable
{
    private const uint EnterMessage = 0x8001;
    private const uint ExitMessage = 0x8002;
    private const uint QuitMessage = 0x0012;
    private const int GwlExStyle = -20;
    private const int WsExTopMost = 0x00000008;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExLayered = 0x00080000;
    private const int WsExNoActivate = 0x08000000;
    private const uint WsPopup = 0x80000000;
    private const uint LwaColorKey = 0x00000001;
    private const uint MagentaColorKey = 0x00FF00FF;
    private const int SwShowNoActivate = 4;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;
    private const uint ThumbnailDestination = 0x00000001;
    private const uint ThumbnailOpacity = 0x00000004;
    private const uint ThumbnailVisible = 0x00000008;
    private const int FrameMilliseconds = 16;
    private static readonly IntPtr TopMost = new(-1);

    private readonly object _sync = new();
    private readonly ManualResetEventSlim _threadReady = new(false);
    private readonly WndProc _wndProc;
    private readonly List<Overlay> _overlays = [];
    private Thread? _thread;
    private uint _threadId;
    private List<TaskbarWindowInfo>? _pendingTaskbars;
    private byte _targetAlpha;
    private int _fadeInMilliseconds;
    private CancellationTokenSource? _fadeCancellation;
    private bool _classRegistered;

    public bool IsActive { get; private set; }

    public FullscreenOverlayService()
    {
        _wndProc = (hwnd, message, wParam, lParam) => DefWindowProc(hwnd, message, wParam, lParam);
    }

    public static bool ShouldOverlay(bool transparencyPaused, bool automationEnabled, bool fullscreenRuleEnabled, AutomationTrigger trigger)
    {
        return !transparencyPaused
            && automationEnabled
            && fullscreenRuleEnabled
            && trigger == AutomationTrigger.Fullscreen;
    }

    /// <summary>Returns true when the overlay was newly engaged by this call.</summary>
    public bool Update(bool shouldOverlay, IReadOnlyList<TaskbarWindowInfo> taskbars, byte opacityPercent, int fadeInMilliseconds)
    {
        var alpha = (byte)Math.Clamp(opacityPercent * 255 / 100, 0, 255);
        if (!shouldOverlay)
        {
            Exit();
            return false;
        }

        lock (_sync)
        {
            if (IsActive)
            {
                if (_targetAlpha != alpha)
                {
                    _targetAlpha = alpha;
                    CancelFadeLocked();
                    UpdateOverlayOpacityLocked(alpha);
                }

                return false;
            }

            IsActive = true;
            _pendingTaskbars = [.. taskbars];
            _targetAlpha = alpha;
            _fadeInMilliseconds = fadeInMilliseconds;
        }

        EnsureThread();
        PostThreadMessage(_threadId, EnterMessage, IntPtr.Zero, IntPtr.Zero);
        return true;
    }

    public void Exit()
    {
        lock (_sync)
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            CancelFadeLocked();
        }

        if (_threadId != 0)
        {
            PostThreadMessage(_threadId, ExitMessage, IntPtr.Zero, IntPtr.Zero);
        }
    }

    public void Dispose()
    {
        Exit();
        if (_threadId != 0)
        {
            PostThreadMessage(_threadId, QuitMessage, IntPtr.Zero, IntPtr.Zero);
            _thread?.Join(TimeSpan.FromSeconds(2));
        }
    }

    private void EnsureThread()
    {
        lock (_sync)
        {
            if (_thread is not null)
            {
                return;
            }

            _thread = new Thread(RunMessageLoop)
            {
                IsBackground = true,
                Name = "OxygenFullscreenOverlay"
            };
            _thread.Start();
        }

        _threadReady.Wait(TimeSpan.FromSeconds(5));
    }

    private void RunMessageLoop()
    {
        _threadId = GetCurrentThreadId();
        PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        _threadReady.Set();

        while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
        {
            if (message.Hwnd == IntPtr.Zero && message.Message == EnterMessage)
            {
                CreateOverlays();
            }
            else if (message.Hwnd == IntPtr.Zero && message.Message == ExitMessage)
            {
                DestroyOverlays();
            }
            else
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }
        }

        DestroyOverlays();
    }

    private void CreateOverlays()
    {
        List<TaskbarWindowInfo>? taskbars;
        byte targetAlpha;
        int fadeMilliseconds;
        lock (_sync)
        {
            taskbars = _pendingTaskbars;
            _pendingTaskbars = null;
            targetAlpha = _targetAlpha;
            fadeMilliseconds = _fadeInMilliseconds;
            if (!IsActive || taskbars is null)
            {
                return;
            }
        }

        EnsureWindowClass();
        lock (_sync)
        {
            DestroyOverlaysLocked();
            foreach (var taskbar in taskbars)
            {
                if (TryCreateOverlay(taskbar, out var overlay))
                {
                    _overlays.Add(overlay);
                }
            }
        }

        StartFadeIn(targetAlpha, fadeMilliseconds);
    }

    private bool TryCreateOverlay(TaskbarWindowInfo taskbar, out Overlay overlay)
    {
        overlay = default;
        if (!GetWindowRect(taskbar.Handle, out var bounds))
        {
            return false;
        }

        var width = bounds.Right - bounds.Left;
        var height = bounds.Bottom - bounds.Top;
        var hwnd = CreateWindowEx(
            WsExTopMost | WsExTransparent | WsExToolWindow | WsExLayered | WsExNoActivate,
            OverlayClassName,
            string.Empty,
            WsPopup,
            bounds.Left,
            bounds.Top,
            width,
            height,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        SetLayeredWindowAttributes(hwnd, MagentaColorKey, 0, LwaColorKey);
        if (DwmRegisterThumbnail(hwnd, taskbar.Handle, out var thumbnail) != 0)
        {
            DestroyWindow(hwnd);
            return false;
        }

        UpdateThumbnail(thumbnail, width, height, alpha: 0);
        ShowWindow(hwnd, SwShowNoActivate);
        SetWindowPos(hwnd, TopMost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
        overlay = new Overlay(hwnd, thumbnail, width, height);
        return true;
    }

    private void StartFadeIn(byte targetAlpha, int fadeMilliseconds)
    {
        CancellationTokenSource cancellation;
        lock (_sync)
        {
            CancelFadeLocked();
            if (fadeMilliseconds <= 0)
            {
                UpdateOverlayOpacityLocked(targetAlpha);
                return;
            }

            _fadeCancellation = new CancellationTokenSource();
            cancellation = _fadeCancellation;
        }

        _ = Task.Run(async () =>
        {
            var token = cancellation.Token;
            var started = Environment.TickCount64;
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    var progress = Math.Clamp((Environment.TickCount64 - started) / (double)fadeMilliseconds, 0d, 1d);
                    var eased = 1d - Math.Pow(1d - progress, 3d);
                    var alpha = (byte)Math.Clamp((int)Math.Round(targetAlpha * eased), 0, 255);
                    lock (_sync)
                    {
                        UpdateOverlayOpacityLocked(alpha);
                    }

                    if (progress >= 1d)
                    {
                        break;
                    }

                    await Task.Delay(FrameMilliseconds, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, cancellation.Token);
    }

    private void DestroyOverlays()
    {
        lock (_sync)
        {
            CancelFadeLocked();
            DestroyOverlaysLocked();
        }
    }

    private void DestroyOverlaysLocked()
    {
        foreach (var overlay in _overlays)
        {
            DwmUnregisterThumbnail(overlay.Thumbnail);
            DestroyWindow(overlay.Handle);
        }

        _overlays.Clear();
    }

    private void UpdateOverlayOpacityLocked(byte alpha)
    {
        foreach (var overlay in _overlays)
        {
            UpdateThumbnail(overlay.Thumbnail, overlay.Width, overlay.Height, alpha);
        }
    }

    private void CancelFadeLocked()
    {
        _fadeCancellation?.Cancel();
        _fadeCancellation?.Dispose();
        _fadeCancellation = null;
    }

    private static void UpdateThumbnail(IntPtr thumbnail, int width, int height, byte alpha)
    {
        var properties = new ThumbnailProperties
        {
            Flags = ThumbnailDestination | ThumbnailOpacity | ThumbnailVisible,
            Destination = new Rect { Left = 0, Top = 0, Right = width, Bottom = height },
            Opacity = alpha,
            Visible = true
        };
        DwmUpdateThumbnailProperties(thumbnail, ref properties);
    }

    private const string OverlayClassName = "OxygenTaskbarFullscreenOverlay";

    private void EnsureWindowClass()
    {
        if (_classRegistered)
        {
            return;
        }

        var windowClass = new WindowClass
        {
            ClassName = OverlayClassName,
            WindowProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            Background = CreateSolidBrush(MagentaColorKey)
        };
        RegisterClass(ref windowClass);
        _classRegistered = true;
    }

    private readonly record struct Overlay(IntPtr Handle, IntPtr Thumbnail, int Width, int Height);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ThumbnailProperties
    {
        public uint Flags;
        public Rect Destination;
        public Rect Source;
        public byte Opacity;
        [MarshalAs(UnmanagedType.Bool)]
        public bool Visible;
        [MarshalAs(UnmanagedType.Bool)]
        public bool SourceClientAreaOnly;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int PointX;
        public int PointY;
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

    private delegate IntPtr WndProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClass(ref WindowClass windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int command);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint colorKey, byte alpha, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out NativeMessage message, IntPtr hwnd, uint filterMin, uint filterMax);

    [DllImport("user32.dll")]
    private static extern bool PeekMessage(out NativeMessage message, IntPtr hwnd, uint filterMin, uint filterMax, uint remove);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref NativeMessage message);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref NativeMessage message);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint threadId, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(uint color);

    [DllImport("dwmapi.dll")]
    private static extern int DwmRegisterThumbnail(IntPtr destination, IntPtr source, out IntPtr thumbnail);

    [DllImport("dwmapi.dll")]
    private static extern int DwmUnregisterThumbnail(IntPtr thumbnail);

    [DllImport("dwmapi.dll")]
    private static extern int DwmUpdateThumbnailProperties(IntPtr thumbnail, ref ThumbnailProperties properties);
}
