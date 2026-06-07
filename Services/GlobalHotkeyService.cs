using System.Runtime.InteropServices;

namespace TaskbarTransparency.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int OpenHotkeyId = 1001;
    private const int ToggleHotkeyId = 1002;
    private readonly SubclassProc _subclassProc;
    private IntPtr _hwnd;
    private Action? _openDashboard;
    private Action? _toggleTransparency;
    private bool _subclassed;

    public GlobalHotkeyService()
    {
        _subclassProc = WndProc;
    }

    public void Attach(IntPtr hwnd, string openHotkey, string toggleHotkey, Action openDashboard, Action toggleTransparency)
    {
        _hwnd = hwnd;
        _openDashboard = openDashboard;
        _toggleTransparency = toggleTransparency;
        if (!_subclassed)
        {
            SetWindowSubclass(_hwnd, _subclassProc, UIntPtr.Zero, IntPtr.Zero);
            _subclassed = true;
        }

        Reconfigure(openHotkey, toggleHotkey);
    }

    public void Reconfigure(string openHotkey, string toggleHotkey)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        UnregisterHotKey(_hwnd, OpenHotkeyId);
        UnregisterHotKey(_hwnd, ToggleHotkeyId);
        if (TryParse(openHotkey, out var open))
        {
            RegisterHotKey(_hwnd, OpenHotkeyId, open.Modifiers, open.VirtualKey);
        }

        if (TryParse(toggleHotkey, out var toggle))
        {
            RegisterHotKey(_hwnd, ToggleHotkeyId, toggle.Modifiers, toggle.VirtualKey);
        }
    }

    public void Dispose()
    {
        if (_hwnd != IntPtr.Zero)
        {
            UnregisterHotKey(_hwnd, OpenHotkeyId);
            UnregisterHotKey(_hwnd, ToggleHotkeyId);
            if (_subclassed)
            {
                RemoveWindowSubclass(_hwnd, _subclassProc, UIntPtr.Zero);
            }
        }

        _hwnd = IntPtr.Zero;
        _subclassed = false;
    }

    public static bool TryParse(string hotkey, out HotkeyRegistration registration)
    {
        registration = default;
        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        var modifiers = 0u;
        var keyPart = parts[^1];
        foreach (var part in parts[..^1])
        {
            modifiers |= part.ToUpperInvariant() switch
            {
                "ALT" => 0x0001u,
                "CTRL" or "CONTROL" => 0x0002u,
                "SHIFT" => 0x0004u,
                "WIN" or "WINDOWS" => 0x0008u,
                _ => 0u
            };
        }

        if (modifiers == 0 || !TryParseVirtualKey(keyPart, out var virtualKey))
        {
            return false;
        }

        registration = new HotkeyRegistration(modifiers, virtualKey);
        return true;
    }

    private static bool TryParseVirtualKey(string key, out uint virtualKey)
    {
        virtualKey = 0;
        if (key.Length == 1)
        {
            var ch = char.ToUpperInvariant(key[0]);
            if (ch is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                virtualKey = ch;
                return true;
            }
        }

        if (key.StartsWith('F') && int.TryParse(key[1..], out var functionKey) && functionKey is >= 1 and <= 24)
        {
            virtualKey = (uint)(0x70 + functionKey - 1);
            return true;
        }

        return false;
    }

    private IntPtr WndProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, UIntPtr subclassId, IntPtr refData)
    {
        if (message == WmHotkey)
        {
            switch (wParam.ToInt32())
            {
                case OpenHotkeyId:
                    _openDashboard?.Invoke();
                    return IntPtr.Zero;
                case ToggleHotkeyId:
                    _toggleTransparency?.Invoke();
                    return IntPtr.Zero;
            }
        }

        return DefSubclassProc(hwnd, message, wParam, lParam);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass);

    [DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    private delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData);
}

public readonly record struct HotkeyRegistration(uint Modifiers, uint VirtualKey);
