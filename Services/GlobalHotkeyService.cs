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

    public HotkeyConfigurationStatus Status { get; private set; } = HotkeyConfigurationStatus.NotConfigured;

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

        Status = Reconfigure(openHotkey, toggleHotkey);
    }

    public HotkeyConfigurationStatus Reconfigure(string openHotkey, string toggleHotkey)
    {
        if (_hwnd == IntPtr.Zero)
        {
            Status = HotkeyConfigurationStatus.NotConfigured;
            return Status;
        }

        UnregisterHotKey(_hwnd, OpenHotkeyId);
        UnregisterHotKey(_hwnd, ToggleHotkeyId);
        var openResult = HotkeyRegistrationResult.Invalid;
        var toggleResult = HotkeyRegistrationResult.Invalid;
        if (TryParse(openHotkey, out var open))
        {
            openResult = TryRegister(OpenHotkeyId, open);
        }

        if (TryParse(toggleHotkey, out var toggle))
        {
            toggleResult = TryRegister(ToggleHotkeyId, toggle);
        }

        Status = new HotkeyConfigurationStatus(
            new HotkeyRegistrationStatus(openHotkey, openResult.FormatValid, openResult.Registered, openResult.ErrorCode),
            new HotkeyRegistrationStatus(toggleHotkey, toggleResult.FormatValid, toggleResult.Registered, toggleResult.ErrorCode));
        return Status;
    }

    public static bool CanSkipReconfigure(string currentOpen, string currentToggle, string nextOpen, string nextToggle, bool hotkeysReady)
    {
        return hotkeysReady
            && string.Equals(currentOpen, nextOpen, StringComparison.Ordinal)
            && string.Equals(currentToggle, nextToggle, StringComparison.Ordinal);
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
                _ => uint.MaxValue
            };

            if (modifiers == uint.MaxValue)
            {
                return false;
            }
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

    private HotkeyRegistrationResult TryRegister(int id, HotkeyRegistration registration)
    {
        if (RegisterHotKey(_hwnd, id, registration.Modifiers, registration.VirtualKey))
        {
            return HotkeyRegistrationResult.Success;
        }

        return HotkeyRegistrationResult.Failed(Marshal.GetLastWin32Error());
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

public sealed record HotkeyConfigurationStatus(HotkeyRegistrationStatus Open, HotkeyRegistrationStatus Toggle)
{
    public static HotkeyConfigurationStatus NotConfigured { get; } = new(
        HotkeyRegistrationStatus.NotConfigured(""),
        HotkeyRegistrationStatus.NotConfigured(""));

    public bool IsReady => Open.Registered && Toggle.Registered;
}

public sealed record HotkeyRegistrationStatus(string Hotkey, bool FormatValid, bool Registered, int ErrorCode)
{
    public static HotkeyRegistrationStatus NotConfigured(string hotkey) => new(hotkey, false, false, 0);

    public string Summary
    {
        get
        {
            if (!FormatValid)
            {
                return $"{Hotkey}: invalid format";
            }

            if (!Registered)
            {
                return $"{Hotkey}: registration failed ({ErrorCode})";
            }

            return $"{Hotkey}: registered";
        }
    }
}

internal readonly record struct HotkeyRegistrationResult(bool FormatValid, bool Registered, int ErrorCode)
{
    public static HotkeyRegistrationResult Invalid { get; } = new(false, false, 0);
    public static HotkeyRegistrationResult Success { get; } = new(true, true, 0);
    public static HotkeyRegistrationResult Failed(int errorCode) => new(true, false, errorCode);
}
