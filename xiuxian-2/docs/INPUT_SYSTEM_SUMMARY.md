# 全局输入采集系统 - 完成总结

## ✅ 已实现功能

### 1. 核心服务 (Autoload)

| 服务 | 文件 | 功能 |
|------|------|------|
| `InputActivityState` | `scripts/services/InputActivityState.cs` | 输入数据聚合、AP 计算、高频衰减 |
| `InputHookService` | `scripts/services/InputHookService.cs` | Win32 全局钩子管理 |
| `InputPauseShortcut` | `scripts/services/InputPauseShortcut.cs` | 全局快捷键 (Ctrl+Shift+X) |

### 2. 隐私合规设计

- ✅ **不记录键值**：仅统计按键次数，不保存具体按键内容
- ✅ **不记录轨迹**：仅累积鼠标移动距离，不保存坐标轨迹
- ✅ **本地处理**：所有数据仅本地聚合，不上传

### 3. 数值公式实现

```csharp
// AP 计算公式（设计文档 03_progression_and_balance.md）
AP_raw = key_down * 1.0 + mouse_click * 1.2 + scroll_step * 0.4 + move_px / 600

// 高频衰减公式
R = AP_raw / AP_baseline  // baseline = 6.0
decay = clamp(1.0 - max(0, R - 1) * 0.25, 0.45, 1.0)
AP_final = AP_raw * decay
```

### 4. 采集项

| 采集项 | 权重 | 说明 |
|--------|------|------|
| key_down_count | 1.0 | 键盘按下次数 |
| mouse_click_count | 1.2 | 鼠标点击次数 |
| mouse_scroll_step | 0.4 | 滚轮滚动步数 |
| mouse_move_distance_px | /600 | 鼠标移动距离（像素） |

### 5. 存档集成

输入统计已集成到存档系统，保存以下字段：
- `total_key_down` - 总按键数
- `total_mouse_click` - 总点击数
- `total_scroll_steps` - 总滚轮步数
- `total_move_distance` - 总移动距离
- `ap_accumulator` - AP 平滑缓冲区
- `hook_paused` - 钩子暂停状态

### 6. UI 控制

- **底栏显示**：实时显示 AP/s 速率
- **暂停按钮**：可添加 PauseToggleButton 到 UI
- **快捷键**：Ctrl + Shift + X 切换暂停/恢复

## 📁 新增文件清单

```
scripts/
├── services/
│   ├── InputActivityState.cs      # 输入状态管理
│   ├── InputHookService.cs        # Win32 全局钩子
│   └── InputPauseShortcut.cs      # 全局快捷键
├── game/
│   ├── ExploreProgressController.cs   # 已更新：使用全局输入
│   └── PrototypeRootController.cs     # 已更新：集成输入存档
└── tests/
    └── InputSystemTest.cs         # 测试工具

scenes/
└── tests/
    └── InputSystemTest.tscn       # 测试场景

docs/
├── INPUT_SYSTEM.md               # 系统详细文档
└── INPUT_SYSTEM_SUMMARY.md       # 本文件

project.godot                     # 已更新：添加 Autoload
```

## 🚀 使用方法

### 运行项目

1. 打开 Godot 编辑器，加载项目
2. 运行 `PrototypeRoot.tscn` 主场景
3. 全局钩子会自动启动（见控制台输出）
4. 在任意窗口进行键鼠操作，观察底栏 AP/s 数值变化

### 测试全局输入

1. 运行 `scenes/tests/InputSystemTest.tscn`
2. 点击"启动钩子"按钮
3. 在 Godot 编辑器外的任意窗口输入
4. 观察测试面板的统计数据更新

### 快捷键

- **Ctrl + Shift + X**: 暂停/恢复输入采集

## ⚠️ 注意事项

1. **Windows 平台限定**：当前仅支持 Windows（Win32 API），其他平台会自动降级（无全局输入）

2. **管理员权限**：通常不需要，但某些安全软件可能拦截钩子。如果遇到问题，尝试以管理员身份运行

3. **钩子生命周期**：
   - 程序启动时自动安装
   - 程序退出时自动卸载
   - 切勿在钩子回调中执行耗时操作

4. **性能影响**：
   - 钩子回调仅做简单计数，性能开销极小
   - AP 计算每秒执行一次，不影响帧率

## 🔧 后续建议

1. **多平台支持**：
   - macOS: 使用 `CGEventTap`
   - Linux: 使用 `X11` 或 `evdev`

2. **反作弊增强**：
   - 检测输入模式异常（如固定间隔）
   - 添加机器学习检测脚本行为

3. **可视化增强**：
   - 底栏添加采集状态指示灯
   - 添加 AP 曲线图历史记录

4. **用户控制**：
   - 设置面板添加采集灵敏度调节
   - 白名单/黑名单应用过滤

## 📊 测试验证

启动项目后，验证以下功能：

- [ ] 控制台显示 `InputHookService: Global hooks started successfully`
- [ ] 底栏显示 `AP/s 0.0`，输入后数值变化
- [ ] 高频输入时（快速按键），衰减生效（AP/s 增长变缓）
- [ ] Ctrl+Shift+X 可暂停/恢复采集
- [ ] 关闭程序后重新打开，累计统计值保持
