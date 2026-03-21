namespace Xiuxian.Scripts.Contracts
{
    public delegate nint HookCallback(int nCode, nint wParam, nint lParam);

    public interface IHookBackend
    {
        bool IsActive { get; }
        bool TryStart(HookCallback keyboardCallback, HookCallback mouseCallback, out string errorMessage);
        void Stop();
        nint CallNextKeyboardHook(int nCode, nint wParam, nint lParam);
        nint CallNextMouseHook(int nCode, nint wParam, nint lParam);
    }
}
