using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace QuickLook;

public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int VkSpace = 0x20;
    private const int VkEnter = 0x0D;
    private const int VkControl = 0x11;
    private const int VkC = 0x43;

    private readonly HookProcedure _hookProcedure;
    private nint _hookHandle;

    public GlobalKeyboardHook()
    {
        _hookProcedure = HookCallback;
    }

    public Func<bool>? SpacePressed { get; init; }
    public Func<bool>? EnterPressed { get; init; }
    public Func<bool>? CopyPressed { get; init; }

    public void Start()
    {
        _hookHandle = SetWindowsHookEx(WhKeyboardLl, _hookProcedure, 0, 0);
        if (_hookHandle == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public void Dispose()
    {
        if (_hookHandle != 0)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }
    }

    private nint HookCallback(int code, nint message, nint data)
    {
        if (code >= 0 && message == WmKeyDown)
        {
            var key = Marshal.ReadInt32(data);
            if ((key == VkSpace && SpacePressed?.Invoke() == true) ||
                (key == VkEnter && EnterPressed?.Invoke() == true) ||
                (key == VkC && IsKeyDown(VkControl) && CopyPressed?.Invoke() == true))
            {
                return 1;
            }
        }

        return CallNextHookEx(_hookHandle, code, message, data);
    }

    private delegate nint HookProcedure(int code, nint message, nint data);

    private static bool IsKeyDown(int key)
    {
        return (GetAsyncKeyState(key) & 0x8000) != 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int hookType, HookProcedure procedure, nint module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(nint hookHandle);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hookHandle, int code, nint message, nint data);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);
}
