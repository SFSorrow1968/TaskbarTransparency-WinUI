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

    public int Apply(TaskbarProfile profile, byte opacity)
    {
        var handles = EnumerateTaskbars().Distinct().Where(handle => handle != IntPtr.Zero).ToArray();
        foreach (var handle in handles)
        {
            Apply(handle, profile, opacity);
            ApplyWholeTaskbarAlpha(handle, opacity);
        }

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

    private static int ComposeColor(string hex, byte opacity)
    {
        var clean = hex.TrimStart('#');
        var r = Convert.ToByte(clean[..2], 16);
        var g = Convert.ToByte(clean.Substring(2, 2), 16);
        var b = Convert.ToByte(clean.Substring(4, 2), 16);
        var alpha = (byte)Math.Clamp(opacity * 255 / 100, 0, 255);
        return (alpha << 24) | (b << 16) | (g << 8) | r;
    }

    private static void ApplyWholeTaskbarAlpha(IntPtr handle, byte opacity)
    {
        EnsureLayeredWindow(handle);
        SetLayeredWindowAttributes(handle, 0, ConvertOpacityToAlpha(opacity), LwaAlpha);
    }

    private static byte ConvertOpacityToAlpha(byte opacity)
    {
        return (byte)Math.Clamp(opacity * 255 / 100, 0, 255);
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
