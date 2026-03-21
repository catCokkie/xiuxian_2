using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xiuxian.Scripts.Adapters.Platform;
using Xiuxian.Scripts.Contracts;

namespace Xiuxian.Scripts.Services
{
    public partial class InputHookService : Node
    {
        [Signal]
        public delegate void HookStateChangedEventHandler(bool isActive);

        [Signal]
        public delegate void InputErrorEventHandler(string errorMessage);

        [Export] public bool AutoStart { get; set; } = true;
        [Export] public bool IsPaused { get; set; } = false;
        [Export] public bool EnableInAppFallback { get; set; } = true;
        [Export] public bool ForceGlobalCapture { get; set; } = true;
        [Export] public double GlobalHookRetryIntervalSeconds { get; set; } = 2.0;
        [Export] public float JoyAxisDeadzone { get; set; } = 0.35f;
        [Export] public float JoyAxisStep { get; set; } = 0.25f;
        [Export] public bool EnableJoyAxisCounting { get; set; } = false;

        [Export] public NodePath ActivityStatePath { get; set; } = "/root/InputActivityState";

        private readonly IPlatformInfo _platformInfo;
        private readonly IHookBackend _hookBackend;
        private readonly HookCallback _keyboardProc;
        private readonly HookCallback _mouseProc;
        private readonly Dictionary<string, float> _joyAxisSample = new();

        private InputActivityState? _activityState;
        private bool _isHookActive;
        private double _retryCooldown;
        private Vector2I _lastMousePosition;
        private bool _hasLastMousePosition;
        private bool _warnedUnsupportedPlatform;

        public InputHookService()
            : this(new GodotPlatformInfo(), new Win32HookBackend())
        {
        }

        public InputHookService(IPlatformInfo platformInfo, IHookBackend hookBackend)
        {
            _platformInfo = platformInfo ?? throw new ArgumentNullException(nameof(platformInfo));
            _hookBackend = hookBackend ?? throw new ArgumentNullException(nameof(hookBackend));
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
        }

        public bool IsHookActive => _isHookActive;

        public bool IsUsingInAppFallback => EnableInAppFallback && !_isHookActive;

        public string ActivePlatformName => _platformInfo.PlatformName;

        public string? LastHookErrorMessage { get; private set; }

        public static HookStartupOutcome EvaluateHookStartup(
            IPlatformInfo platformInfo,
            IHookBackend hookBackend,
            HookCallback keyboardCallback,
            HookCallback mouseCallback)
        {
            if (platformInfo == null)
            {
                throw new ArgumentNullException(nameof(platformInfo));
            }

            if (hookBackend == null)
            {
                throw new ArgumentNullException(nameof(hookBackend));
            }

            if (!platformInfo.IsWindows())
            {
                return new HookStartupOutcome(
                    platformInfo.PlatformName,
                    AttemptedBackendStart: false,
                    IsHookActive: false,
                    ErrorMessage: $"InputHookService: Global hooks are disabled on platform '{platformInfo.PlatformName}'. Fallback to in-app input only.");
            }

            try
            {
                if (!hookBackend.TryStart(keyboardCallback, mouseCallback, out var errorMessage))
                {
                    return new HookStartupOutcome(
                        platformInfo.PlatformName,
                        AttemptedBackendStart: true,
                        IsHookActive: false,
                        ErrorMessage: string.IsNullOrWhiteSpace(errorMessage)
                            ? "InputHookService: Hook backend failed to start."
                            : errorMessage);
                }

                if (!hookBackend.IsActive)
                {
                    return new HookStartupOutcome(
                        platformInfo.PlatformName,
                        AttemptedBackendStart: true,
                        IsHookActive: false,
                        ErrorMessage: "InputHookService: Hook backend reported success but remained inactive.");
                }

                return new HookStartupOutcome(
                    platformInfo.PlatformName,
                    AttemptedBackendStart: true,
                    IsHookActive: true,
                    ErrorMessage: null);
            }
            catch (Exception ex)
            {
                return new HookStartupOutcome(
                    platformInfo.PlatformName,
                    AttemptedBackendStart: true,
                    IsHookActive: false,
                    ErrorMessage: ex.Message);
            }
        }

        public override void _Ready()
        {
            _activityState = GetNodeOrNull<InputActivityState>(ActivityStatePath);
            if (_activityState == null)
            {
                ReportInputError("InputHookService: InputActivityState not found!", pushError: true);
                return;
            }

            ProcessMode = ProcessModeEnum.Always;
            SetProcessInput(true);

            if (AutoStart)
            {
                StartHook();
            }
        }

        public override void _Process(double delta)
        {
            if (!ForceGlobalCapture || IsPaused || _isHookActive || !_platformInfo.IsWindows())
            {
                return;
            }

            _retryCooldown -= delta;
            if (_retryCooldown > 0.0)
            {
                return;
            }

            _retryCooldown = Math.Max(0.2, GlobalHookRetryIntervalSeconds);
            StartHook();
        }

        public override void _ExitTree()
        {
            StopHook();
        }

        public override void _Input(InputEvent @event)
        {
            if (!EnableInAppFallback || IsPaused || _activityState == null)
            {
                return;
            }

            bool skipKeyboardMouseInApp = _isHookActive && _platformInfo.IsWindows();

            switch (@event)
            {
                case InputEventKey keyEvent when keyEvent.Pressed && !keyEvent.Echo:
                    if (!skipKeyboardMouseInApp)
                    {
                        _activityState.RegisterKeyDown();
                    }
                    break;

                case InputEventMouseButton mouseButton when mouseButton.Pressed:
                    if (skipKeyboardMouseInApp)
                    {
                        break;
                    }

                    if (mouseButton.ButtonIndex == MouseButton.WheelUp || mouseButton.ButtonIndex == MouseButton.WheelDown)
                    {
                        _activityState.RegisterMouseScroll(1);
                    }
                    else
                    {
                        _activityState.RegisterMouseClick();
                    }
                    break;

                case InputEventMouseMotion motionEvent:
                    if (!skipKeyboardMouseInApp)
                    {
                        _activityState.RegisterMouseMove(motionEvent.Relative.Length());
                    }
                    break;

                case InputEventJoypadButton joyButton when joyButton.Pressed:
                    _activityState.RegisterJoypadButton();
                    break;

                case InputEventJoypadMotion joyMotion:
                    if (EnableJoyAxisCounting)
                    {
                        HandleJoypadMotion(joyMotion);
                    }
                    break;
            }
        }

        public void StartHook()
        {
            if (_isHookActive)
            {
                return;
            }

            LastHookErrorMessage = null;

            var startup = EvaluateHookStartup(_platformInfo, _hookBackend, _keyboardProc, _mouseProc);
            LastHookErrorMessage = startup.ErrorMessage;

            if (!_platformInfo.IsWindows())
            {
                if (!_warnedUnsupportedPlatform && LastHookErrorMessage is not null)
                {
                    _warnedUnsupportedPlatform = true;
                    ReportInputError(LastHookErrorMessage, pushWarning: true);
                }
                return;
            }

            if (!startup.IsHookActive)
            {
                if (LastHookErrorMessage is not null)
                {
                    ReportInputError(LastHookErrorMessage, pushError: true);
                }

                if (startup.AttemptedBackendStart)
                {
                    _retryCooldown = Math.Max(0.2, GlobalHookRetryIntervalSeconds);
                    if (ForceGlobalCapture)
                    {
                        ReportWarning("InputHookService: Global-only mode active, waiting for next hook retry.");
                    }
                    else
                    {
                        ReportWarning("InputHookService: Falling back to in-app input capture.");
                    }
                }
                return;
            }

            _isHookActive = true;
            _warnedUnsupportedPlatform = false;
            _retryCooldown = 0.0;
            EmitHookStateChanged(true);
            ReportInfo("InputHookService: Global hooks started successfully");
        }

        public void StopHook()
        {
            if (!_isHookActive)
            {
                return;
            }

            _hookBackend.Stop();
            _isHookActive = false;
            _retryCooldown = Math.Max(0.2, GlobalHookRetryIntervalSeconds);
            EmitHookStateChanged(false);
            ReportInfo("InputHookService: Global hooks stopped");
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            if (paused)
            {
                _activityState?.ResetCurrentTick();
            }
            ReportInfo($"InputHookService: {(paused ? "Paused" : "Resumed")}");
        }

        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        private void HandleJoypadMotion(InputEventJoypadMotion joyMotion)
        {
            if (_activityState == null)
            {
                return;
            }

            float value = joyMotion.AxisValue;
            float absValue = Mathf.Abs(value);
            string key = $"{joyMotion.Device}:{(int)joyMotion.Axis}";
            float previous = _joyAxisSample.TryGetValue(key, out float prev) ? prev : 0.0f;

            if (absValue < JoyAxisDeadzone)
            {
                _joyAxisSample[key] = 0.0f;
                return;
            }

            float delta = Mathf.Abs(absValue - previous);
            if (previous <= 0.0f || delta >= JoyAxisStep)
            {
                _activityState.RegisterJoypadAxisInput();
                _joyAxisSample[key] = absValue;
                return;
            }

            _joyAxisSample[key] = absValue;
        }

        private nint KeyboardHookCallback(int nCode, nint wParam, nint lParam)
        {
            if (nCode >= 0 && !IsPaused && _activityState != null)
            {
                if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
                {
                    _activityState.RegisterKeyDown();
                }
            }

            return _hookBackend.CallNextKeyboardHook(nCode, wParam, lParam);
        }

        private nint MouseHookCallback(int nCode, nint wParam, nint lParam)
        {
            if (nCode >= 0 && !IsPaused && _activityState != null)
            {
                var mouseInfo = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var currentPos = new Vector2I(mouseInfo.pt.x, mouseInfo.pt.y);

                switch ((int)wParam)
                {
                    case WM_LBUTTONDOWN:
                    case WM_RBUTTONDOWN:
                    case WM_MBUTTONDOWN:
                    case WM_XBUTTONDOWN:
                        _activityState.RegisterMouseClick();
                        break;

                    case WM_MOUSEWHEEL:
                        int delta = (short)((mouseInfo.mouseData >> 16) & 0xFFFF);
                        int steps = Math.Abs(delta) / WHEEL_DELTA;
                        if (steps > 0)
                        {
                            _activityState.RegisterMouseScroll(steps);
                        }
                        break;

                    case WM_MOUSEMOVE:
                        if (_hasLastMousePosition)
                        {
                            double distance = _lastMousePosition.DistanceTo(currentPos);
                            _activityState.RegisterMouseMove(distance);
                        }
                        _lastMousePosition = currentPos;
                        _hasLastMousePosition = true;
                        break;
                }
            }

            return _hookBackend.CallNextMouseHook(nCode, wParam, lParam);
        }

        private void ReportInputError(string message, bool pushError = false, bool pushWarning = false)
        {
            if (!IsInsideTree())
            {
                return;
            }

            if (pushError)
            {
                GD.PushError(message);
            }

            if (pushWarning)
            {
                GD.PushWarning(message);
            }

            EmitSignal(SignalName.InputError, message);
        }

        private void ReportWarning(string message)
        {
            if (IsInsideTree())
            {
                GD.PushWarning(message);
            }
        }

        private void ReportInfo(string message)
        {
            if (IsInsideTree())
            {
                GD.Print(message);
            }
        }

        private void EmitHookStateChanged(bool isActive)
        {
            if (IsInsideTree())
            {
                EmitSignal(SignalName.HookStateChanged, isActive);
            }
        }

        public sealed record HookStartupOutcome(
            string PlatformName,
            bool AttemptedBackendStart,
            bool IsHookActive,
            string? ErrorMessage)
        {
            public bool IsUsingInAppFallback => !IsHookActive;
        }

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WHEEL_DELTA = 120;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}
