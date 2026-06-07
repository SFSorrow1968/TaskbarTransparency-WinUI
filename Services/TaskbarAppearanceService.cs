using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

public sealed class TaskbarAppearanceService
{
    private const int GwlExStyle = -20;
    private const int WsExLayered = 0x00080000;
    private const uint LwaAlpha = 0x00000002;
    private const int AccentDisabled = 0;
    private const int AccentEnableGradient = 1;
    private const int AccentEnableTransparentGradient = 2;
    private const int AccentEnableBlurBehind = 3;
    private const int AccentEnableAcrylicBlurBehind = 4;
    private const int MinimumFrameMilliseconds = 16;
    private readonly ConcurrentDictionary<IntPtr, byte> _currentAlphas = new();
    private readonly object _animationSync = new();
    private CancellationTokenSource? _animationCancellation;

    public int Apply(TaskbarProfile profile, byte opacity)
    {
        var handles = EnumerateTaskbars().Distinct().Where(handle => handle != IntPtr.Zero).ToArray();
        foreach (var handle in handles)
        {
            Apply(handle, profile, opacity);
            EnsureLayeredWindow(handle);
        }

        AnimateWholeTaskbarAlpha(handles, opacity, profile);
        return handles.Length;
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

    private static void Apply(IntPtr handle, TaskbarProfile profile, byte opacity)
    {
        var accent = new AccentPolicy
        {
            AccentState = profile.Mode switch
            {
                TaskbarVisualMode.Clear => AccentEnableTransparentGradient,
                TaskbarVisualMode.Acrylic => AccentEnableAcrylicBlurBehind,
                TaskbarVisualMode.Mica => AccentEnableBlurBehind,
                TaskbarVisualMode.Solid => AccentEnableGradient,
                _ => AccentDisabled
            },
            GradientColor = ComposeColor(profile.AccentHex, opacity)
        };

        var accentSize = Marshal.SizeOf<AccentPolicy>();
        var accentPtr = Marshal.AllocHGlobal(accentSize);
        try
        {
            Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new WindowCompositionAttributeData
            {
                Attribute = 19,
                Data = accentPtr,
                SizeOfData = accentSize
            };
            SetWindowCompositionAttribute(handle, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(accentPtr);
        }
    }

    public static int ComposeColorForTest(string hex, byte opacity) => ComposeColor(hex, opacity);
    public static byte ConvertOpacityToAlphaForTest(byte opacity) => ConvertOpacityToAlpha(opacity);
    public static double EaseProgressForTest(double progress, string easing) => EaseProgress(progress, easing);
    public static int SelectFadeMillisecondsForTest(TaskbarProfile profile, byte startAlpha, byte targetAlpha) => SelectFadeMilliseconds(profile, startAlpha, targetAlpha);

    private static int ComposeColor(string hex, byte opacity)
    {
        var clean = hex.TrimStart('#');
        var r = Convert.ToByte(clean[..2], 16);
        var g = Convert.ToByte(clean.Substring(2, 2), 16);
        var b = Convert.ToByte(clean.Substring(4, 2), 16);
        var alpha = (byte)Math.Clamp(opacity * 255 / 100, 0, 255);
        return (alpha << 24) | (b << 16) | (g << 8) | r;
    }

    private void AnimateWholeTaskbarAlpha(IReadOnlyCollection<IntPtr> handles, byte opacity, TaskbarProfile profile)
    {
        var targetAlpha = ConvertOpacityToAlpha(opacity);
        if (handles.Count == 0)
        {
            _currentAlphas.Clear();
            return;
        }

        CancellationTokenSource cancellation;
        lock (_animationSync)
        {
            _animationCancellation?.Cancel();
            _animationCancellation?.Dispose();
            _animationCancellation = new CancellationTokenSource();
            cancellation = _animationCancellation;
        }

        var starts = handles.ToDictionary(handle => handle, handle => _currentAlphas.GetValueOrDefault(handle, targetAlpha));
        var representativeStart = starts.Values.FirstOrDefault(targetAlpha);
        var fadeMilliseconds = SelectFadeMilliseconds(profile, representativeStart, targetAlpha);

        if (fadeMilliseconds <= 0)
        {
            foreach (var handle in handles)
            {
                ApplyLayeredAlpha(handle, targetAlpha);
            }

            return;
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
                    var elapsed = Environment.TickCount64 - started;
                    var progress = Math.Clamp(elapsed / (double)fadeMilliseconds, 0d, 1d);
                    var eased = EaseProgress(progress, profile.Easing);

                    foreach (var handle in handles)
                    {
                        var start = starts[handle];
                        var alpha = (byte)Math.Clamp((int)Math.Round(start + ((targetAlpha - start) * eased)), 0, 255);
                        ApplyLayeredAlpha(handle, alpha);
                    }

                    if (progress >= 1d)
                    {
                        break;
                    }

                    await Task.Delay(MinimumFrameMilliseconds, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, cancellation.Token);
    }

    private void ApplyLayeredAlpha(IntPtr handle, byte alpha)
    {
        EnsureLayeredWindow(handle);
        SetLayeredWindowAttributes(handle, 0, alpha, LwaAlpha);
        _currentAlphas[handle] = alpha;
    }

    private static byte ConvertOpacityToAlpha(byte opacity)
    {
        return (byte)Math.Clamp(opacity * 255 / 100, 0, 255);
    }

    private static double EaseProgress(double progress, string easing)
    {
        var clamped = Math.Clamp(progress, 0d, 1d);
        return easing switch
        {
            "Linear" => clamped,
            "QuintOut" => 1d - Math.Pow(1d - clamped, 5d),
            _ => 1d - Math.Pow(1d - clamped, 3d)
        };
    }

    private static int SelectFadeMilliseconds(TaskbarProfile profile, byte startAlpha, byte targetAlpha)
    {
        return targetAlpha >= startAlpha
            ? Math.Max(0, profile.FadeInMilliseconds)
            : Math.Max(0, profile.FadeOutMilliseconds);
    }

    private static void EnsureLayeredWindow(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var style = GetWindowLongPtr(handle, GwlExStyle);
        var styleValue = style.ToInt64();
        if ((styleValue & WsExLayered) != WsExLayered)
        {
            SetWindowLongPtr(handle, GwlExStyle, new IntPtr(styleValue | WsExLayered));
        }
    }

    private static IntPtr GetWindowLongPtr(IntPtr handle, int index)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(handle, index)
            : new IntPtr(GetWindowLong32(handle, index));
    }

    private static IntPtr SetWindowLongPtr(IntPtr handle, int index, IntPtr value)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(handle, index, value)
            : new IntPtr(SetWindowLong32(handle, index, value.ToInt32()));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string? windowTitle);

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongA", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongA", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrA", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrA", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint colorKey, byte alpha, uint flags);
}
