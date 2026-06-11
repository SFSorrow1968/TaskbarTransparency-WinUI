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
    private const int AccentEnableTransparentGradient = 2;
    private const int MinimumFrameMilliseconds = 16;
    private const string AccentHex = "#FFFFFF";
    private readonly ConcurrentDictionary<IntPtr, byte> _currentAlphas = new();
    private readonly ConcurrentDictionary<IntPtr, AppearanceRequest> _currentAppearances = new();
    private readonly ConcurrentDictionary<IntPtr, byte> _layeredHandles = new();
    private readonly object _animationSync = new();
    private CancellationTokenSource? _animationCancellation;

    public int Apply(byte opacity, int fadeMilliseconds, bool transparencyActive, IReadOnlyCollection<MonitorProfile>? monitors = null)
    {
        return Apply(opacity, fadeMilliseconds, transparencyActive, monitors, TaskbarWindowCatalog.GetCurrent());
    }

    public int Apply(byte opacity, int fadeMilliseconds, bool transparencyActive, IReadOnlyCollection<MonitorProfile>? monitors, IReadOnlyList<TaskbarWindowInfo> taskbarTargets)
    {
        var targets = DistinctByHandle(taskbarTargets);
        var monitorLookup = transparencyActive ? BuildMonitorOverrideLookup(monitors) : null;
        var alphaTargets = new Dictionary<IntPtr, byte>(targets.Count);
        foreach (var target in targets)
        {
            var monitor = monitorLookup?.GetValueOrDefault(target.DeviceName);
            var targetOpacity = transparencyActive ? ResolveMonitorOpacity(opacity, monitor) : (byte)100;
            ApplyIfChanged(target.Handle, targetOpacity, transparencyActive);
            alphaTargets[target.Handle] = targetOpacity;
        }

        PruneStaleHandles(targets);
        AnimateTaskbarAlpha(alphaTargets, fadeMilliseconds);
        return targets.Count;
    }

    private void ApplyIfChanged(IntPtr handle, byte opacity, bool transparencyActive)
    {
        var request = CreateAppearanceRequest(opacity, transparencyActive);
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
    public static double EaseProgressForTest(double progress) => EaseProgress(progress);
    public static IReadOnlyList<int> BuildAlphaAnimationDurationsForTest(int fadeMilliseconds, IReadOnlyDictionary<IntPtr, byte> currentAlphas, IReadOnlyDictionary<IntPtr, byte> targetAlphas) => BuildAlphaAnimationTargets(fadeMilliseconds, currentAlphas, targetAlphas).ConvertAll(target => target.Duration);
    public static byte ResolveMonitorOpacityForTest(byte opacity, MonitorProfile? monitor) => ResolveMonitorOpacity(opacity, monitor);
    public static bool ShouldApplyLayeredAlphaForTest(byte? currentAlpha, byte targetAlpha) => ShouldApplyLayeredAlpha(currentAlpha, targetAlpha);
    public static bool ShouldReadLayeredStyleForTest(byte? currentAlpha, byte targetAlpha, bool layeredStyleKnown) => ShouldReadLayeredStyle(currentAlpha, targetAlpha, layeredStyleKnown);
    public static bool AppearanceRequestMatchesForTest(byte leftOpacity, bool leftActive, byte rightOpacity, bool rightActive) => CreateAppearanceRequest(leftOpacity, leftActive) == CreateAppearanceRequest(rightOpacity, rightActive);
    public static IReadOnlyCollection<IntPtr> FindStaleHandlesForTest(IEnumerable<IntPtr> cachedHandles, IEnumerable<IntPtr> liveHandles) => FindStaleHandles(cachedHandles, liveHandles);
    public static IReadOnlyDictionary<string, MonitorProfile>? BuildMonitorOverrideLookupForTest(IReadOnlyCollection<MonitorProfile>? monitors) => BuildMonitorOverrideLookup(monitors);
    public static IReadOnlyList<TaskbarWindowInfo> DistinctByHandleForTest(IReadOnlyList<TaskbarWindowInfo> targets) => DistinctByHandle(targets);

    private static int ComposeColor(string hex, byte opacity)
    {
        var clean = hex.TrimStart('#');
        var r = Convert.ToByte(clean[..2], 16);
        var g = Convert.ToByte(clean.Substring(2, 2), 16);
        var b = Convert.ToByte(clean.Substring(4, 2), 16);
        var alpha = (byte)Math.Clamp(opacity * 255 / 100, 0, 255);
        return (alpha << 24) | (b << 16) | (g << 8) | r;
    }

    private static AppearanceRequest CreateAppearanceRequest(byte opacity, bool transparencyActive)
    {
        return transparencyActive
            ? new AppearanceRequest(AccentEnableTransparentGradient, ComposeColor(AccentHex, opacity))
            : new AppearanceRequest(AccentDisabled, 0);
    }

    private static IReadOnlyList<TaskbarWindowInfo> DistinctByHandle(IReadOnlyList<TaskbarWindowInfo> targets)
    {
        if (targets.Count <= 1)
        {
            return targets;
        }

        var seen = new HashSet<IntPtr>(targets.Count);
        var distinct = new List<TaskbarWindowInfo>(targets.Count);
        foreach (var target in targets)
        {
            if (seen.Add(target.Handle))
            {
                distinct.Add(target);
            }
        }

        return distinct;
    }

    private void AnimateTaskbarAlpha(IReadOnlyDictionary<IntPtr, byte> targetOpacities, int fadeMilliseconds)
    {
        if (targetOpacities.Count == 0)
        {
            _currentAlphas.Clear();
            _currentAppearances.Clear();
            _layeredHandles.Clear();
            return;
        }

        var targetAlphas = new Dictionary<IntPtr, byte>(targetOpacities.Count);
        foreach (var pair in targetOpacities)
        {
            var alpha = ConvertOpacityToAlpha(pair.Value);
            if (ShouldApplyLayeredAlpha(GetCurrentAlpha(pair.Key), alpha))
            {
                targetAlphas[pair.Key] = alpha;
            }
        }

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

        var animationTargets = BuildAlphaAnimationTargets(fadeMilliseconds, _currentAlphas, targetAlphas);

        if (!HasAnimatedDuration(animationTargets))
        {
            foreach (var target in animationTargets)
            {
                ApplyLayeredAlpha(target.Handle, target.TargetAlpha);
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

                    foreach (var target in animationTargets)
                    {
                        var progress = target.Duration <= 0
                            ? 1d
                            : Math.Clamp(elapsed / (double)target.Duration, 0d, 1d);
                        var eased = EaseProgress(progress);
                        var alpha = (byte)Math.Clamp((int)Math.Round(target.StartAlpha + ((target.TargetAlpha - target.StartAlpha) * eased)), 0, 255);
                        ApplyLayeredAlpha(target.Handle, alpha);
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

    private void PruneStaleHandles(IReadOnlyList<TaskbarWindowInfo> liveTargets)
    {
        var liveHandles = BuildLiveHandleSet(liveTargets);
        foreach (var pair in _currentAlphas)
        {
            var handle = pair.Key;
            if (!liveHandles.Contains(handle))
            {
                _currentAlphas.TryRemove(handle, out _);
            }
        }

        foreach (var pair in _currentAppearances)
        {
            var handle = pair.Key;
            if (!liveHandles.Contains(handle))
            {
                _currentAppearances.TryRemove(handle, out _);
            }
        }

        foreach (var pair in _layeredHandles)
        {
            var handle = pair.Key;
            if (!liveHandles.Contains(handle))
            {
                _layeredHandles.TryRemove(handle, out _);
            }
        }
    }

    private static HashSet<IntPtr> BuildLiveHandleSet(IReadOnlyList<TaskbarWindowInfo> liveTargets)
    {
        var liveHandles = new HashSet<IntPtr>(liveTargets.Count);
        foreach (var target in liveTargets)
        {
            liveHandles.Add(target.Handle);
        }

        return liveHandles;
    }

    private static IReadOnlyCollection<IntPtr> FindStaleHandles(IEnumerable<IntPtr> cachedHandles, IEnumerable<IntPtr> liveHandles)
    {
        var live = liveHandles.ToHashSet();
        return cachedHandles
            .Distinct()
            .Where(handle => !live.Contains(handle))
            .ToArray();
    }

    private static Dictionary<string, MonitorProfile>? BuildMonitorOverrideLookup(IReadOnlyCollection<MonitorProfile>? monitors)
    {
        if (monitors is null || monitors.Count == 0)
        {
            return null;
        }

        Dictionary<string, MonitorProfile>? lookup = null;
        foreach (var monitor in monitors)
        {
            if (monitor.SyncWithPrimary)
            {
                continue;
            }

            lookup ??= new Dictionary<string, MonitorProfile>(StringComparer.OrdinalIgnoreCase);
            lookup.TryAdd(monitor.DeviceName, monitor);
        }

        return lookup;
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

    private static List<AlphaAnimationTarget> BuildAlphaAnimationTargets(int fadeMilliseconds, IReadOnlyDictionary<IntPtr, byte> currentAlphas, IReadOnlyDictionary<IntPtr, byte> targetAlphas)
    {
        var targets = new List<AlphaAnimationTarget>(targetAlphas.Count);
        foreach (var pair in targetAlphas)
        {
            var start = currentAlphas.GetValueOrDefault(pair.Key, pair.Value);
            var duration = start == pair.Value ? 0 : Math.Max(0, fadeMilliseconds);
            targets.Add(new AlphaAnimationTarget(pair.Key, start, pair.Value, duration));
        }

        return targets;
    }

    private static bool HasAnimatedDuration(IReadOnlyList<AlphaAnimationTarget> animationTargets)
    {
        foreach (var target in animationTargets)
        {
            if (target.Duration > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyLayeredAlpha(IntPtr handle, byte alpha)
    {
        var currentAlpha = GetCurrentAlpha(handle);
        var layeredStyleKnown = _layeredHandles.ContainsKey(handle);
        if (!ShouldApplyLayeredAlpha(currentAlpha, alpha))
        {
            return;
        }

        if (ShouldReadLayeredStyle(currentAlpha, alpha, layeredStyleKnown))
        {
            EnsureLayeredWindow(handle);
        }

        SetLayeredWindowAttributes(handle, 0, alpha, LwaAlpha);
        _currentAlphas[handle] = alpha;
    }

    private static bool ShouldApplyLayeredAlpha(byte? currentAlpha, byte targetAlpha)
    {
        return currentAlpha != targetAlpha;
    }

    private static bool ShouldReadLayeredStyle(byte? currentAlpha, byte targetAlpha, bool layeredStyleKnown)
    {
        return ShouldApplyLayeredAlpha(currentAlpha, targetAlpha) && !layeredStyleKnown;
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

    private static double EaseProgress(double progress)
    {
        var clamped = Math.Clamp(progress, 0d, 1d);
        return 1d - Math.Pow(1d - clamped, 3d);
    }

    private void EnsureLayeredWindow(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        if (_layeredHandles.ContainsKey(handle))
        {
            return;
        }

        var style = GetWindowLongPtr(handle, GwlExStyle);
        var styleValue = style.ToInt64();
        if ((styleValue & WsExLayered) != WsExLayered)
        {
            SetWindowLongPtr(handle, GwlExStyle, new IntPtr(styleValue | WsExLayered));
        }

        _layeredHandles.TryAdd(handle, 0);
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
    private readonly record struct AlphaAnimationTarget(IntPtr Handle, byte StartAlpha, byte TargetAlpha, int Duration);

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
