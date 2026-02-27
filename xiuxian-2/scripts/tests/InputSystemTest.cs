using Godot;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests
{
    /// <summary>
    /// 输入系统测试工具
    /// 用于验证全局输入采集是否正常工作
    /// </summary>
    public partial class InputSystemTest : Control
    {
        [Export] public NodePath ActivityStatePath = "/root/InputActivityState";
        [Export] public NodePath HookServicePath = "/root/InputHookService";

        private InputActivityState _activityState;
        private InputHookService _hookService;
        private RichTextLabel _outputLabel;

        public override void _Ready()
        {
            _activityState = GetNode<InputActivityState>(ActivityStatePath);
            _hookService = GetNode<InputHookService>(HookServicePath);

            // 创建测试 UI
            SetupTestUI();

            // 订阅事件
            if (_activityState != null)
            {
                _activityState.ActivityTick += OnActivityTick;
            }

            if (_hookService != null)
            {
                _hookService.HookStateChanged += OnHookStateChanged;
                _hookService.InputError += OnInputError;
            }

            UpdateDisplay();
        }

        public override void _ExitTree()
        {
            if (_activityState != null)
            {
                _activityState.ActivityTick -= OnActivityTick;
            }
            if (_hookService != null)
            {
                _hookService.HookStateChanged -= OnHookStateChanged;
                _hookService.InputError -= OnInputError;
            }
        }

        private void SetupTestUI()
        {
            // 主容器
            var vbox = new VBoxContainer();
            vbox.SetAnchorsPreset(LayoutPreset.FullRect);
            vbox.AddThemeConstantOverride("separation", 10);
            AddChild(vbox);

            // 标题
            var title = new Label();
            title.Text = "🎮 全局输入系统测试";
            title.AddThemeFontSizeOverride("font_size", 24);
            vbox.AddChild(title);

            // 控制按钮
            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 10);
            vbox.AddChild(hbox);

            var startBtn = new Button();
            startBtn.Text = "启动钩子";
            startBtn.Pressed += () => _hookService?.StartHook();
            hbox.AddChild(startBtn);

            var stopBtn = new Button();
            stopBtn.Text = "停止钩子";
            stopBtn.Pressed += () => _hookService?.StopHook();
            hbox.AddChild(stopBtn);

            var pauseBtn = new Button();
            pauseBtn.Text = "暂停/恢复";
            pauseBtn.Pressed += () => _hookService?.TogglePause();
            hbox.AddChild(pauseBtn);

            // 状态输出
            _outputLabel = new RichTextLabel();
            _outputLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _outputLabel.ScrollFollowing = true;
            vbox.AddChild(_outputLabel);
        }

        private void OnActivityTick(double apThisSecond, double apFinal)
        {
            CallDeferred(nameof(UpdateDisplay));
        }

        private void OnHookStateChanged(bool isActive)
        {
            GD.Print($"InputSystemTest: Hook state changed to {isActive}");
            CallDeferred(nameof(UpdateDisplay));
        }

        private void OnInputError(string errorMessage)
        {
            GD.PushError($"InputSystemTest: Input error - {errorMessage}");
        }

        private void UpdateDisplay()
        {
            if (_activityState == null || _outputLabel == null) return;

            string status = _hookService?.IsPaused ?? false ? "⏸ 暂停" : "▶ 采集中";
            string hookActive = _hookService != null ? "🟢 已启动" : "🔴 未启动";

            string text = $@"=== 系统状态 ===
钩子状态: {hookActive}
采集状态: {status}

=== 当前秒统计 ===
按键次数: {_activityState.KeyDownCount}
鼠标点击: {_activityState.MouseClickCount}
滚轮步数: {_activityState.MouseScrollSteps}
移动距离: {_activityState.MouseMoveDistancePx:F1}px

=== AP 计算 ===
原始 AP: {_activityState.ApThisSecond:F2}
衰减后 AP: {_activityState.ApFinal:F2}
衰减倍率: {(_activityState.ApThisSecond > 0 ? _activityState.ApFinal / _activityState.ApThisSecond : 1):F2}

=== 累计统计 ===
总按键: {_activityState.TotalKeyDownCount:N0}
总点击: {_activityState.TotalMouseClickCount:N0}
总滚轮: {_activityState.TotalMouseScrollSteps:N0}
总移动: {_activityState.TotalMouseMoveDistancePx:N0}px";

            _outputLabel.Text = text;
        }

        public override void _Process(double delta)
        {
            // 实时更新当前秒计数
            if (_activityState != null && _outputLabel != null)
            {
                // 可选：每帧更新显示当前计数
            }
        }
    }
}
