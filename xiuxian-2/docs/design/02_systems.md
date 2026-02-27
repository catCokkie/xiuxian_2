# 02 系统设计

## 系统清单
1. 输入采集系统（Keyboard/Mouse Activity Collector）
2. 活动点与资源转换系统
3. 境界与突破系统
4. 双窗口桌面 UI 系统（主横向窗口 + 图书子菜单）
5. 灵宠心情与羁绊系统
6. 存档、时间校验与防刷系统

## 1) 输入采集系统
- 目标：稳定统计键鼠活跃度，作为核心进度来源。
- 采集项（V1）：
- `key_down_count`
- `mouse_click_count`
- `mouse_scroll_step`
- `mouse_move_distance_px`（仅累积距离）
- 关键规则：
- 不记录键值文本、不记录窗口标题。
- 仅统计计数与强度，按 1 秒时间片聚合。
- 超高频输入触发衰减，降低脚本刷分收益。
- 平台策略：
- Windows 优先，使用系统级 Hook（Godot C# 调用 Win32）。
- 非 Windows 平台在 V1 可降级为“仅应用内输入计数”。

## 2) 活动点与资源转换
- 中间资源：`activity_point`（AP）。
- 产出资源：`lingqi`、`insight`、`pet_affinity`。
- 设计原则：
- 转换公式公开可见。
- 存在分钟产出软上限，防止极端输入破坏节奏。
- 允许短时技能提高转化率，但不超过上限阈值。

## 3) 境界与突破
- 字段：
- `realm_level` (int)
- `realm_exp` (double)
- `breakthrough_pill` (int)
- `insight` (double)
- 规则：
- `realm_exp` 满后，需消耗突破丹与悟性进行突破。
- 突破失败积累保底值，下一次成功率提升（V1 不掉境界）。

## 4) 双窗口桌面 UI 系统
- 主横向窗口：
- 常驻桌面底部，承载探索与战斗进度演出。
- 左上按钮用于拖动窗口位置。
- 右上按钮用于调整主窗口横向宽度。
- 左下图书按钮用于打开/关闭子菜单窗口。
- 图书按钮下方展示当前修炼阶段。
- 右下展示当前区域探索进度条。
- 子菜单窗口：
- 独立窗口，采用书页/卷轴形态，顶部为左右两组页签。
- 左侧页签组（游戏内容）：`修炼概况`、`装备情况`、`统计概览`。
- 右侧页签组（功能设置）：`联机`、`Bug反馈`、`设置`。
- 页签内容区采用单页全宽展示（不再左右分栏同时显示）。
- 左上角提供关闭按钮（`X`）。
- 底部状态条固定显示 `灵石数量`。
- 可单独开关，不中断主窗口探索循环。
- 交互约束：
- 默认低干扰，面板状态与窗口尺寸需持久化。
- 窗口缩放后核心信息（修炼阶段、探索进度）必须可见。
- UI 文案统一从 `scripts/ui/UiText.cs` 提供，避免分散硬编码与乱码问题。

## 5) 灵宠心情与羁绊
- 字段：
- `pet_mood` (0-100)
- `bond_level` (int)
- 效果：
- `pet_mood >= 80`：资源转换 +10%。
- `pet_mood <= 30`：资源转换 -15%。
- 羁绊升级解锁被动效果（如 AP 衰减抗性）。

## 6) 存档与防刷
- 存档结构：单文件 `user://save_state.cfg`（ConfigFile）+ `meta.version`。
- 必要字段：
- `meta.version`
- `meta.last_saved_unix`
- `ui.main_bar_x`
- `ui.main_bar_width`
- `ui.submenu_visible`
- `ui.submenu_active_left_tab`
- `ui.submenu_active_right_tab`
- `input.stats`
- `input.hook_paused`
- 迁移策略：
- 优先读取统一存档；若不存在则自动迁移旧 `ui_state.cfg` + `game_state.cfg`。
- 防刷策略：
- 检测异常高频峰值并打标。
- 本地时间跳变超阈值时进入保守结算。
- 长时间无焦点但高输入速率时降低收益权重。

## 建议 C# 领域类
- `InputActivityState`
- `InputHookService`
- `ActivityConversionService`
- `PlayerProgress`
- `ResourceWallet`
- `PetState`
- `DesktopUiState`
- `MainBarWindowState`
- `SubmenuWindowState`
- `SaveRoot`
