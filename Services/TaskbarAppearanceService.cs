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
    private readonly ConcurrentDictionary<IntPtr, byte> _layeredHandles = new();
    private readonly object _animationSync = new();
    private CancellationTokenSource? _animationCancellation;

    public TaskbarApplyDiagnostics Diagnostics { get; private set; } = TaskbarApplyDiagnostics.Empty;

    public int Apply(TaskbarProfile profile, byte opacity, IReadOnlyCollection<MonitorProfile>? monitors = null)
    {
        return Apply(profile, opacity, monitors, TaskbarWindowCatalog.GetCurrent());
    }

    public int Apply(TaskbarProfile profile, byte opacity, IReadOnlyCollection<MonitorProfile>? monitors, IReadOnlyList<TaskbarWindowInfo> taskbarTargets)
    {
        var targets = DistinctByHandle(taskbarTargets);
        var monitorLookup = BuildMonitorOverrideLookup(monitors);
        var alphaTargets = new Dictionary<IntPtr, byte>(targets.Count);
        var compositionApplied = 0;
        var compositionSkipped = 0;
        foreach (var target in targets)
        {
            var monitor = monitorLookup?.GetValueOrDefault(target.DeviceName);
            var targetOpacity = ResolveMonitorOpacity(opacity, monitor);
            if (ApplyIfChanged(target.Handle, profile, targetOpacity))
            {
                compositionApplied++;
            }
            else
            {
                compositionSkipped++;
            }

            alphaTargets[target.Handle] = targetOpacity;
        }

        PruneStaleHandles(targets);
        var alphaSummary = AnimateTaskbarAlpha(alphaTargets, profile);
        Diagnostics = TaskbarApplyDiagnostics.Create(
            targets.Count,
            compositionApplied,
            compositionSkipped,
            alphaSummary.LayeredAlphaChanges,
            alphaSummary.LayeredAlphaNoOps,
            monitorLookup is not null,
            alphaSummary.AnimationStarted,
            DateTimeOffset.Now);
        return targets.Count;
    }

    private bool ApplyIfChanged(IntPtr handle, TaskbarProfile profile, byte opacity)
    {
        var request = CreateAppearanceRequest(profile, opacity);
        if (_currentAppearances.TryGetValue(handle, out var current) && current == request)
        {
            return false;
        }

        Apply(handle, request);
        _currentAppearances[handle] = request;
        return true;
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
    public static IReadOnlyList<int> BuildAlphaAnimationDurationsForTest(TaskbarProfile profile, IReadOnlyDictionary<IntPtr, byte> currentAlphas, IReadOnlyDictionary<IntPtr, byte> targetAlphas) => BuildAlphaAnimationTargets(profile, currentAlphas, targetAlphas).ConvertAll(target => target.Duration);
    public static byte ResolveMonitorOpacityForTest(byte opacity, MonitorProfile? monitor) => ResolveMonitorOpacity(opacity, monitor);
    public static bool ShouldApplyLayeredAlphaForTest(byte? currentAlpha, byte targetAlpha) => ShouldApplyLayeredAlpha(currentAlpha, targetAlpha);
    public static bool ShouldReadLayeredStyleForTest(byte? currentAlpha, byte targetAlpha, bool layeredStyleKnown) => ShouldReadLayeredStyle(currentAlpha, targetAlpha, layeredStyleKnown);
    public static bool AppearanceRequestMatchesForTest(TaskbarProfile leftProfile, byte leftOpacity, TaskbarProfile rightProfile, byte rightOpacity) => CreateAppearanceRequest(leftProfile, leftOpacity) == CreateAppearanceRequest(rightProfile, rightOpacity);
    public static TaskbarApplyDiagnostics CreateDiagnosticsForTest(int targetCount, int compositionApplied, int compositionSkipped, int layeredAlphaChanges, int layeredAlphaNoOps, bool monitorLookupBuilt, bool animationStarted) => TaskbarApplyDiagnostics.Create(targetCount, compositionApplied, compositionSkipped, layeredAlphaChanges, layeredAlphaNoOps, monitorLookupBuilt, animationStarted, DateTimeOffset.UnixEpoch);
    public static IReadOnlyCollection<IntPtr> FindStaleHandlesForTest(IEnumerable<IntPtr> cachedHandles, IEnumerable<IntPtr> liveHandles) => FindStaleHandles(cachedHandles, liveHandles);
    public static bool NeedsMonitorOverrideLookupForTest(IReadOnlyCollection<MonitorProfile>? monitors) => NeedsMonitorOverrideLookup(monitors);
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

    private AlphaApplySummary AnimateTaskbarAlpha(IReadOnlyDictionary<IntPtr, byte> targetOpacities, TaskbarProfile profile)
    {
        if (targetOpacities.Count == 0)
        {
            _currentAlphas.Clear();
            _currentAppearances.Clear();
            _layeredHandles.Clear();
            return AlphaApplySummary.Empty;
        }

        var targetAlphas = new Dictionary<IntPtr, byte>(targetOpacities.Count);
        var skipped = 0;
        foreach (var pair in targetOpacities)
        {
            var alpha = ConvertOpacityToAlpha(pair.Value);
            if (ShouldApplyLayeredAlpha(GetCurrentAlpha(pair.Key), alpha))
            {
                targetAlphas[pair.Key] = alpha;
            }
            else
            {
                skipped++;
            }
        }

        if (targetAlphas.Count == 0)
        {
            return new AlphaApplySummary(0, skipped, AnimationStarted: false);
        }

        CancellationTokenSource cancellation;
        lock (_animationSync)
        {
            _animationCancellation?.Cancel();
            _animationCancellation?.Dispose();
            _animationCancellation = new CancellationTokenSource();
            cancellation = _animationCancellation;
        }

        var animationTargets = BuildAlphaAnimationTargets(profile, _currentAlphas, targetAlphas);

        if (!HasAnimatedDuration(animationTargets))
        {
            foreach (var target in animationTargets)
            {
                ApplyLayeredAlpha(target.Handle, target.TargetAlpha);
            }

            return new AlphaApplySummary(targetAlphas.Count, skipped, AnimationStarted: false);
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
                        var eased = EaseProgress(progress, profile.Easing);
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

        return new AlphaApplySummary(targetAlphas.Count, skipped, AnimationStarted: true);
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

    private static bool NeedsMonitorOverrideLookup(IReadOnlyCollection<MonitorProfile>? monitors)
    {
        return monitors is not null && monitors.Any(monitor => !monitor.SyncWithPrimary);
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
        var durations = new Dictionary<IntPtr, int>(targetAlphas.Count);
        foreach (var pair in targetAlphas)
        {
            var start = starts.GetValueOrDefault(pair.Key, pair.Value);
            durations[pair.Key] = start == pair.Value ? 0 : SelectFadeMilliseconds(profile, start, pair.Value);
        }

        return durations;
    }

    private static List<AlphaAnimationTarget> BuildAlphaAnimationTargets(TaskbarProfile profile, IReadOnlyDictionary<IntPtr, byte> currentAlphas, IReadOnlyDictionary<IntPtr, byte> targetAlphas)
    {
        var targets = new List<AlphaAnimationTarget>(targetAlphas.Count);
        foreach (var pair in targetAlphas)
        {
            var start = currentAlphas.GetValueOrDefault(pair.Key, pair.Value);
            var duration = start == pair.Value ? 0 : SelectFadeMilliseconds(profile, start, pair.Value);
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
    private readonly record struct AlphaApplySummary(int LayeredAlphaChanges, int LayeredAlphaNoOps, bool AnimationStarted)
    {
        public static AlphaApplySummary Empty { get; } = new(0, 0, AnimationStarted: false);
    }

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

public sealed record TaskbarApplyDiagnostics(
    int TargetCount,
    int CompositionApplied,
    int CompositionSkipped,
    int LayeredAlphaChanges,
    int LayeredAlphaNoOps,
    bool MonitorLookupBuilt,
    bool AnimationStarted,
    DateTimeOffset UpdatedAt)
{
    public static TaskbarApplyDiagnostics Empty { get; } = Create(0, 0, 0, 0, 0, false, false, DateTimeOffset.UnixEpoch);

    public double CompositionSkipRatio => Ratio(CompositionSkipped, CompositionApplied + CompositionSkipped);

    public double LayeredAlphaSkipRatio => Ratio(LayeredAlphaNoOps, LayeredAlphaChanges + LayeredAlphaNoOps);

    public static TaskbarApplyDiagnostics Create(
        int targetCount,
        int compositionApplied,
        int compositionSkipped,
        int layeredAlphaChanges,
        int layeredAlphaNoOps,
        bool monitorLookupBuilt,
        bool animationStarted,
        DateTimeOffset updatedAt)
    {
        return new TaskbarApplyDiagnostics(
            Math.Max(0, targetCount),
            Math.Max(0, compositionApplied),
            Math.Max(0, compositionSkipped),
            Math.Max(0, layeredAlphaChanges),
            Math.Max(0, layeredAlphaNoOps),
            monitorLookupBuilt,
            animationStarted,
            updatedAt);
    }

    private static double Ratio(int numerator, int denominator)
    {
        return denominator <= 0 ? 0d : numerator / (double)denominator;
    }
}
