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
    private readonly ConcurrentDictionary<IntPtr, AppearanceRequest> _currentAppearances = new();
    private readonly object _animationSync = new();
    private CancellationTokenSource? _animationCancellation;

    public int Apply(TaskbarProfile profile, byte opacity, IReadOnlyCollection<MonitorProfile>? monitors = null)
    {
        var targets = TaskbarWindowCatalog.GetCurrent()
            .GroupBy(target => target.Handle)
            .Select(group => group.First())
            .ToArray();
        var monitorLookup = (monitors ?? [])
            .GroupBy(monitor => monitor.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var alphaTargets = new Dictionary<IntPtr, byte>();
        foreach (var target in targets)
        {
            var monitor = monitorLookup.GetValueOrDefault(target.DeviceName);
            var targetOpacity = ResolveMonitorOpacity(opacity, monitor);
            ApplyIfChanged(target.Handle, profile, targetOpacity);
            alphaTargets[target.Handle] = targetOpacity;
        }

        PruneStaleHandles(targets.Select(target => target.Handle));
        AnimateTaskbarAlpha(alphaTargets, profile);
        return targets.Length;
    }

    private void ApplyIfChanged(IntPtr handle, TaskbarProfile profile, byte opacity)
    {
        var request = CreateAppearanceRequest(profile, opacity);
        if (_currentAppearances.TryGetValue(handle, out var current) && current == request)
        {
            return;
        }

        Apply(handle, request);
        _currentAppearances[handle] = request;
    }

    private static void Apply(IntPtr handle, AppearanceRequest request)
    {
        var accent = new AccentPolicy
        {
            AccentState = request.AccentState,
            GradientColor = request.GradientColor
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
    public static IReadOnlyDictionary<IntPtr, int> SelectFadeDurationsForTest(TaskbarProfile profile, IReadOnlyDictionary<IntPtr, byte> starts, IReadOnlyDictionary<IntPtr, byte> targetAlphas) => SelectFadeDurations(profile, starts, targetAlphas);
    public static byte ResolveMonitorOpacityForTest(byte opacity, MonitorProfile? monitor) => ResolveMonitorOpacity(opacity, monitor);
    public static bool ShouldApplyLayeredAlphaForTest(byte? currentAlpha, byte targetAlpha) => ShouldApplyLayeredAlpha(currentAlpha, targetAlpha);
    public static bool AppearanceRequestMatchesForTest(TaskbarProfile leftProfile, byte leftOpacity, TaskbarProfile rightProfile, byte rightOpacity) => CreateAppearanceRequest(leftProfile, leftOpacity) == CreateAppearanceRequest(rightProfile, rightOpacity);
    public static IReadOnlyCollection<IntPtr> FindStaleHandlesForTest(IEnumerable<IntPtr> cachedHandles, IEnumerable<IntPtr> liveHandles) => FindStaleHandles(cachedHandles, liveHandles);

    private static int ComposeColor(string hex, byte opacity)
    {
        var clean = hex.TrimStart('#');
        var r = Convert.ToByte(clean[..2], 16);
        var g = Convert.ToByte(clean.Substring(2, 2), 16);
        var b = Convert.ToByte(clean.Substring(4, 2), 16);
        var alpha = (byte)Math.Clamp(opacity * 255 / 100, 0, 255);
        return (alpha << 24) | (b << 16) | (g << 8) | r;
    }

    private static AppearanceRequest CreateAppearanceRequest(TaskbarProfile profile, byte opacity)
    {
        return new AppearanceRequest(
            profile.Mode switch
            {
                TaskbarVisualMode.Clear => AccentEnableTransparentGradient,
                TaskbarVisualMode.Acrylic => AccentEnableAcrylicBlurBehind,
                TaskbarVisualMode.Mica => AccentEnableBlurBehind,
                TaskbarVisualMode.Solid => AccentEnableGradient,
                _ => AccentDisabled
            },
            ComposeColor(profile.AccentHex, opacity));
    }

    private void AnimateTaskbarAlpha(IReadOnlyDictionary<IntPtr, byte> targetOpacities, TaskbarProfile profile)
    {
        if (targetOpacities.Count == 0)
        {
            _currentAlphas.Clear();
            _currentAppearances.Clear();
            return;
        }

        var targetAlphas = targetOpacities
            .Select(pair => new KeyValuePair<IntPtr, byte>(pair.Key, ConvertOpacityToAlpha(pair.Value)))
            .Where(pair => ShouldApplyLayeredAlpha(GetCurrentAlpha(pair.Key), pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        if (targetAlphas.Count == 0)
        {
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

        var starts = targetAlphas.ToDictionary(pair => pair.Key, pair => _currentAlphas.GetValueOrDefault(pair.Key, pair.Value));
        var fadeDurations = SelectFadeDurations(profile, starts, targetAlphas);

        if (fadeDurations.Values.All(duration => duration <= 0))
        {
            foreach (var pair in targetAlphas)
            {
                ApplyLayeredAlpha(pair.Key, pair.Value);
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
                    var allComplete = true;

                    foreach (var pair in targetAlphas)
                    {
                        var handle = pair.Key;
                        var start = starts[handle];
                        var duration = fadeDurations[handle];
                        var progress = duration <= 0
                            ? 1d
                            : Math.Clamp(elapsed / (double)duration, 0d, 1d);
                        var eased = EaseProgress(progress, profile.Easing);
                        var alpha = (byte)Math.Clamp((int)Math.Round(start + ((pair.Value - start) * eased)), 0, 255);
                        ApplyLayeredAlpha(handle, alpha);
                        allComplete &= progress >= 1d;
                    }

                    if (allComplete)
                    {
                        break;
                    }

                    await Task.Delay(MinimumFrameMilliseconds, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                ClearAnimationCancellation(cancellation);
            }
        }, cancellation.Token);
    }

    private void PruneStaleHandles(IEnumerable<IntPtr> liveHandles)
    {
        var cachedHandles = _currentAlphas.Keys.Concat(_currentAppearances.Keys);
        foreach (var handle in FindStaleHandles(cachedHandles, liveHandles))
        {
            _currentAlphas.TryRemove(handle, out _);
            _currentAppearances.TryRemove(handle, out _);
        }
    }

    private static IReadOnlyCollection<IntPtr> FindStaleHandles(IEnumerable<IntPtr> cachedHandles, IEnumerable<IntPtr> liveHandles)
    {
        var live = liveHandles.ToHashSet();
        return cachedHandles
            .Distinct()
            .Where(handle => !live.Contains(handle))
            .ToArray();
    }

    private void ClearAnimationCancellation(CancellationTokenSource cancellation)
    {
        lock (_animationSync)
        {
            if (!ReferenceEquals(_animationCancellation, cancellation))
            {
                return;
            }

            _animationCancellation = null;
        }

        cancellation.Dispose();
    }

    private static Dictionary<IntPtr, int> SelectFadeDurations(TaskbarProfile profile, IReadOnlyDictionary<IntPtr, byte> starts, IReadOnlyDictionary<IntPtr, byte> targetAlphas)
    {
        return targetAlphas.ToDictionary(
            pair => pair.Key,
            pair =>
            {
                var start = starts.GetValueOrDefault(pair.Key, pair.Value);
                return start == pair.Value ? 0 : SelectFadeMilliseconds(profile, start, pair.Value);
            });
    }

    private void ApplyLayeredAlpha(IntPtr handle, byte alpha)
    {
        if (!ShouldApplyLayeredAlpha(GetCurrentAlpha(handle), alpha))
        {
            return;
        }

        EnsureLayeredWindow(handle);
        SetLayeredWindowAttributes(handle, 0, alpha, LwaAlpha);
        _currentAlphas[handle] = alpha;
    }

    private static bool ShouldApplyLayeredAlpha(byte? currentAlpha, byte targetAlpha)
    {
        return currentAlpha != targetAlpha;
    }

    private byte? GetCurrentAlpha(IntPtr handle)
    {
        return _currentAlphas.TryGetValue(handle, out var alpha)
            ? alpha
            : null;
    }

    private static byte ConvertOpacityToAlpha(byte opacity)
    {
        return (byte)Math.Clamp(opacity * 255 / 100, 0, 255);
    }

    private static byte ResolveMonitorOpacity(byte opacity, MonitorProfile? monitor)
    {
        return monitor is { SyncWithPrimary: false }
            ? monitor.OverrideOpacity
            : opacity;
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

    private readonly record struct AppearanceRequest(int AccentState, int GradientColor);

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
