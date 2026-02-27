using Godot;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.UI
{
    /// <summary>
    /// 暂停/恢复按钮控制器
    /// 显示当前采集状态，点击可切换
    /// </summary>
    public partial class PauseToggleButton : Button
    {
        [Export] public NodePath HookServicePath = "/root/InputHookService";

        private InputHookService _hookService;

        public override void _Ready()
        {
            _hookService = GetNode<InputHookService>(HookServicePath);

            if (_hookService == null)
            {
                GD.PushError("PauseToggleButton: InputHookService not found!");
                Disabled = true;
                return;
            }

            // 订阅状态变更
            _hookService.HookStateChanged += OnHookStateChanged;

            // 初始化显示
            UpdateDisplay();

            // 连接点击事件
            Pressed += OnButtonPressed;
        }

        public override void _ExitTree()
        {
            if (_hookService != null)
            {
                _hookService.HookStateChanged -= OnHookStateChanged;
            }
        }

        private void OnButtonPressed()
        {
            _hookService?.TogglePause();
            UpdateDisplay();
        }

        private void OnHookStateChanged(bool isActive)
        {
            // 如果钩子完全停止，禁用按钮
            Disabled = !isActive;
            if (!isActive)
            {
                Text = "⚠";
                TooltipText = "输入采集已停止";
            }
            else
            {
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (_hookService == null) return;

            if (_hookService.IsPaused)
            {
                Text = "⏸";
                TooltipText = "点击恢复输入采集";
                Modulate = new Color(0.8f, 0.8f, 0.8f);
            }
            else
            {
                Text = "▶";
                TooltipText = "点击暂停输入采集";
                Modulate = Colors.White;
            }
        }
    }
}
