using Xiuxian.Scripts.Contracts;

namespace Xiuxian2.Core.Tests.Support.Deterministic;

public sealed class FakeHookBackend : IHookBackend
{
    private readonly Queue<(bool Success, string Error)> _startResults = new();
    private HookCallback? _keyboardCallback;
    private HookCallback? _mouseCallback;
    private nint _keyboardNextResult;
    private nint _mouseNextResult;

    public bool IsActive { get; private set; }

    public int StopCallCount { get; private set; }

    public FakeHookBackend QueueStartSuccess()
    {
        _startResults.Enqueue((true, string.Empty));
        return this;
    }

    public FakeHookBackend QueueStartFailure(string errorMessage)
    {
        _startResults.Enqueue((false, errorMessage));
        return this;
    }

    public FakeHookBackend SetKeyboardNextResult(nint result)
    {
        _keyboardNextResult = result;
        return this;
    }

    public FakeHookBackend SetMouseNextResult(nint result)
    {
        _mouseNextResult = result;
        return this;
    }

    public bool TryStart(HookCallback keyboardCallback, HookCallback mouseCallback, out string errorMessage)
    {
        _keyboardCallback = keyboardCallback;
        _mouseCallback = mouseCallback;

        (bool Success, string Error) result = _startResults.Count > 0
            ? _startResults.Dequeue()
            : (true, string.Empty);
        IsActive = result.Success;
        errorMessage = result.Error;
        return result.Success;
    }

    public void Stop()
    {
        IsActive = false;
        StopCallCount++;
    }

    public nint CallNextKeyboardHook(int nCode, nint wParam, nint lParam)
    {
        return _keyboardNextResult;
    }

    public nint CallNextMouseHook(int nCode, nint wParam, nint lParam)
    {
        return _mouseNextResult;
    }
}
