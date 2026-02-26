# 全局输入采集系统

本文档说明修仙桌宠项目的全局键鼠输入采集系统实现。

## 系统架构

```
┌─────────────────────────────────────────────────────────────┐
│                    Windows 系统                              │
│  ┌─────────────┐    ┌─────────────┐                        │
│  │  键盘事件   │    │  鼠标事件   │                        │
│  └──────┬──────┘    └──────┬──────┘                        │
│         │                  │                                │
│         ▼                  ▼                                │
│  ┌─────────────────────────────────┐                       │
│  │  SetWindowsHookEx (WH_KEYBOARD_LL) │ 全局低级钩子       │
│  │  SetWindowsHookEx (WH_MOUSE_LL)    │                   │
│  └────────────────┬────────────────┘                       │
│                   │                                         │
└───────────────────┼─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│                    Godot 应用层                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ InputHookService (Autoload)                         │   │
│  │ • 管理 Win32 钩子生命周期                            │   │
│  │ • 原始事件 → 计数转换                                │   │
│  │ • 支持暂停/恢复采集                                  │   │
│  └────────────────────────┬────────────────────────────┘   │
│                           │                                 │
│                           ▼                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ InputActivityState (Autoload)                       │   │
│  │ • 1秒时间片聚合                                      │   │
│  │ • AP 计算：key*1.0 + click*1.2 + scroll*0.4 + move/600│  │
│  │ • 高频衰减：decay = clamp(1 - max(0,R-1)*0.25, 0.45, 1)│ │
│  │ • 累计统计（用于存档）                               │   │
│  └────────────────────────┬────────────────────────────┘   │
│                           │                                 │
│                           ▼                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ ExploreProgressController                           │   │
│  │ • 接收 AP 结算信号                                   │   │
│  │ • 推进探索进度                                       │   │
│  │ • 更新 UI 显示                                       │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## 核心组件

### 1. InputActivityState (输入活动状态)

**职责**：存储、聚合输入数据，计算 Activity Point (AP)

**关键特性**：
- 按 1 秒时间片聚合原始输入
- 不记录具体键值，仅统计次数
- 实现设计文档的高频衰减公式
- 累计总统计值用于持久化

**信号**：
```csharp
[Signal]
public delegate void ActivityTickEventHandler(double apThisSecond, double apFinal);
```

**AP 计算公式**：
```
AP_raw = key_down * 1.0 + mouse_click * 1.2 + scroll_step * 0.4 + move_px / 600
```

**高频衰减公式**：
```
R = AP_raw / AP_baseline      // AP_baseline = 6.0
decay = clamp(1.0 - max(0, R - 1) * 0.25, 0.45, 1.0)
AP_final = AP_raw * decay
```

### 2. InputHookService (输入钩子服务)

**职责**：使用 Win32 API 设置全局低级钩子

**关键特性**：
- 使用 `WH_KEYBOARD_LL` 和 `WH_MOUSE_LL` 钩子
- 仅采集事件计数，不记录键值内容
- 支持暂停/恢复采集
- 程序退出时自动卸载钩子

**Win32 API 使用**：
- `SetWindowsHookEx` - 安装钩子
- `UnhookWindowsHookEx` - 卸载钩子
- `CallNextHookEx` - 传递事件给下一个钩子

**注意**：此服务仅在 Windows 平台有效，其他平台需要降级实现。

## 隐私与合规

### 数据采集原则

| 采集项 | 说明 | 隐私合规 |
|--------|------|----------|
| key_down_count | 键盘按下次数 | ✅ 不记录具体按键 |
| mouse_click_count | 鼠标点击次数 | ✅ 不记录点击位置 |
| mouse_scroll_step | 滚轮滚动步数 | ✅ 不记录方向内容 |
| mouse_move_distance | 移动距离（像素） | ✅ 仅累积距离，不记录轨迹 |

### 不采集的内容

- ❌ 具体按键值（如 'A', 'Enter', 'Ctrl+C'）
- ❌ 鼠标点击的屏幕坐标
- ❌ 窗口标题或焦点信息
- ❌ 输入时间戳序列

### 用户控制

- 全局快捷键 `Ctrl + Shift + X`：暂停/恢复采集（待实现）
- 底栏状态指示器：显示当前采集状态
- 设置面板：可完全禁用全局钩子

## 使用方法

### 在其他脚本中读取输入状态

```csharp
// 获取输入状态服务
var activityState = GetNode<InputActivityState>("/root/InputActivityState");

// 读取当前 AP
GD.Print($"AP this second: {activityState.ApThisSecond}");
GD.Print($"AP after decay: {activityState.ApFinal}");

// 读取累计统计
GD.Print($"Total key presses: {activityState.TotalKeyDownCount}");
```

### 监听 AP 结算事件

```csharp
public override void _Ready()
{
    var activityState = GetNode<InputActivityState>("/root/InputActivityState");
    activityState.ActivityTick += OnActivityTick;
}

private void OnActivityTick(double apThisSecond, double apFinal)
{
    // 使用 apFinal 计算游戏进度
    // apThisSecond 用于显示原始活跃度
}
```

### 控制采集状态

```csharp
// 获取钩子服务
var hookService = GetNode<InputHookService>("/root/InputHookService");

// 暂停采集
hookService.SetPaused(true);

// 恢复采集
hookService.SetPaused(false);

// 完全停止钩子（如需重新启动需调用 StartHook）
hookService.StopHook();
```

## 存档集成

输入统计已集成到存档系统，保存字段：

```json
{
  "input": {
    "stats": {
      "total_key_down": 123456,
      "total_mouse_click": 78901,
      "total_scroll_steps": 3456,
      "total_move_distance": 1234567.8,
      "ap_accumulator": 12.34
    },
    "hook_paused": false
  }
}
```

## 调参与监控

### 可配置参数

在 `InputActivityState` 中可调整：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| ApBaseline | 6.0 | 衰减基准线，超过此值开始衰减 |
| KeyDownWeight | 1.0 | 按键权重 |
| MouseClickWeight | 1.2 | 鼠标点击权重 |
| ScrollStepWeight | 0.4 | 滚轮步数权重 |
| MovePxDivider | 600.0 | 移动距离除数 |
| DecayRate | 0.25 | 衰减系数 |
| MinDecayMultiplier | 0.45 | 最小衰减倍率 |

### 调试输出

运行时可在 Godot 输出面板查看：
- `InputHookService: Global hooks started successfully` - 钩子启动成功
- `InputHookService: Paused/Resumed` - 暂停状态变更
- AP/s 显示在底栏活动速率标签

## 常见问题

### Q: 为什么需要管理员权限？
A: 全局钩子通常不需要管理员权限，但某些安全软件可能会拦截。如果遇到问题，尝试以管理员身份运行 Godot 编辑器或导出的游戏。

### Q: 会影响系统性能吗？
A: 影响极小。低级钩子直接在内核态处理，回调函数只做简单的计数累加，不涉及复杂计算或 I/O。

### Q: 支持多显示器吗？
A: 支持。鼠标移动距离按像素计算，跨显示器时会正确累加总距离。

### Q: 如何防止脚本刷分？
A: 高频衰减机制会自动降低超高频输入的收益。同时 `ApAccumulator` 缓冲区会平滑突发输入，避免瞬间大量操作直接转化为游戏进度。

## 平台适配

| 平台 | 支持状态 | 说明 |
|------|----------|------|
| Windows | ✅ 完全支持 | 使用 Win32 全局钩子 |
| macOS | ⚠️ 待实现 | 需要 CGEventTap 实现 |
| Linux | ⚠️ 待实现 | 需要 X11/evdev 实现 |

非 Windows 平台当前会静默降级（无全局输入），后续可通过条件编译或接口实现多平台支持。
