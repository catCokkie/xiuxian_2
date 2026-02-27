using Godot;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// 全局快捷键服务
    /// Ctrl + Shift + X: 暂停/恢复输入采集
    /// </summary>
    public partial class InputPauseShortcut : Node
    {
        [Export] public NodePath HookServicePath = "/root/InputHookService";

        private InputHookService _hookService;

        public override void _Ready()
        {
            _hookService = GetNode<InputHookService>(HookServicePath);
            ProcessMode = ProcessModeEnum.Always; // 即使暂停也处理
        }

        public override void _Input(InputEvent @event)
        {
            // 检测 Ctrl + Shift + X
            if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
            {
                if (keyEvent.Keycode == Key.X && keyEvent.CtrlPressed && keyEvent.ShiftPressed)
                {
                    TogglePause();
                }
            }
        }

        private void TogglePause()
        {
            if (_hookService == null)
            {
                GD.PushWarning("InputPauseShortcut: HookService not available");
                return;
            }

            _hookService.TogglePause();

            // 显示通知（如果实现了通知系统）
            string status = _hookService.IsPaused ? "已暂停" : "已恢复";
            GD.Print($"InputPauseShortcut: 输入采集 {status}");

            // TODO: 显示 UI 通知提示用户
            // NotificationManager.Show($"输入采集 {status}");
        }
    }
}
