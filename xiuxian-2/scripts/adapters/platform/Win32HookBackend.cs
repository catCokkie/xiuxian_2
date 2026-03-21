using System;
using System.Runtime.InteropServices;
using Xiuxian.Scripts.Contracts;

namespace Xiuxian.Scripts.Adapters.Platform;

public sealed class Win32HookBackend : IHookBackend
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;

    private delegate nint LowLevelProc(int nCode, nint wParam, nint lParam);

    private readonly LowLevelProc _keyboardForwarder;
    private readonly LowLevelProc _mouseForwarder;
    private HookCallback? _keyboardCallback;
    private HookCallback? _mouseCallback;
    private nint _keyboardHookId;
    private nint _mouseHookId;

    public Win32HookBackend()
    {
        _keyboardForwarder = ForwardKeyboard;
        _mouseForwarder = ForwardMouse;
    }

    public bool IsActive { get; private set; }

    public bool TryStart(HookCallback keyboardCallback, HookCallback mouseCallback, out string errorMessage)
    {
        _keyboardCallback = keyboardCallback ?? throw new ArgumentNullException(nameof(keyboardCallback));
        _mouseCallback = mouseCallback ?? throw new ArgumentNullException(nameof(mouseCallback));

        _keyboardHookId = SetWindowsHookEx(WhKeyboardLl, _keyboardForwarder, IntPtr.Zero, 0);
        if (_keyboardHookId == IntPtr.Zero)
        {
            errorMessage = $"Keyboard hook failed: {Marshal.GetLastWin32Error()}";
            Stop();
            return false;
        }

        _mouseHookId = SetWindowsHookEx(WhMouseLl, _mouseForwarder, IntPtr.Zero, 0);
        if (_mouseHookId == IntPtr.Zero)
        {
            errorMessage = $"Mouse hook failed: {Marshal.GetLastWin32Error()}";
            Stop();
            return false;
        }

        IsActive = true;
        errorMessage = string.Empty;
        return true;
    }

    public void Stop()
    {
        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }

        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }

        IsActive = false;
    }

    public nint CallNextKeyboardHook(int nCode, nint wParam, nint lParam)
    {
        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    public nint CallNextMouseHook(int nCode, nint wParam, nint lParam)
    {
        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private nint ForwardKeyboard(int nCode, nint wParam, nint lParam)
    {
        return _keyboardCallback is null
            ? CallNextHookEx(_keyboardHookId, nCode, wParam, lParam)
            : _keyboardCallback(nCode, wParam, lParam);
    }

    private nint ForwardMouse(int nCode, nint wParam, nint lParam)
    {
        return _mouseCallback is null
            ? CallNextHookEx(_mouseHookId, nCode, wParam, lParam)
            : _mouseCallback(nCode, wParam, lParam);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, LowLevelProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);
}
