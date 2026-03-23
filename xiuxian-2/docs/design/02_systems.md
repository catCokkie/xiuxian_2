# 02 系统设计

## 系统清单
1. 输入采集系统（Keyboard/Mouse Activity Collector）
2. 活动点与资源转换系统
3. 境界与突破系统
4. 探索 / 遭遇 / 战斗规则系统
5. 装备、背包与最小奖励闭环系统
6. 双窗口桌面 UI 系统（主横向窗口 + 图书子菜单）
7. 灵宠成长预留系统
8. 存档、时间校验与防刷系统

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

## 4) 探索 / 遭遇 / 战斗规则
- 规则层已拆分为：`BattleStartRules`、`BattleRules`、`BattleLifecycleRules`、`RewardRules`。
- 当前主流程口径：
- 怪物在探索轨道上接近主角。
- 进入触发线后进入战斗态。
- 玩家继续通过输入数推进战斗回合。
- 胜负、奖励、Boss 解锁与战斗日志均通过规则层决策后再由控制器应用。

## 5) 装备、背包与最小奖励闭环
- 已具备 `BackpackState`、`EquippedItemsState`、`EquipmentStatProfile` 的最小闭环。
- 当前装备来源：空存档 starter loadout + 首通固定装备奖励。
- 关键产品边界：
- 新装备进入背包，不自动替换已装备物品。
- 仅在玩家手动触发装备动作时才替换同槽位旧装备。
- 当前仍属原型/开发验证：未实现完整装备内容池、随机词条、强化和最终换装 UX。

## 6) 双窗口桌面 UI 系统
- 主横向窗口：
- 常驻桌面底部，承载探索与战斗进度演出。
- 左上按钮用于拖动窗口位置。
- 右上按钮用于调整主窗口横向宽度。
- 左下图书按钮用于打开/关闭子菜单窗口。
- 图书按钮下方展示当前修炼阶段。
- 右下展示当前区域探索进度条。
- 子菜单窗口：
- 独立窗口，采用书页/卷轴形态，顶部为左右两组页签。
- 左侧页签组（游戏内容）：`修炼概况`、`战斗日志`、`装备情况`、`统计概览`。
- 右侧页签组（功能设置）：`Bug反馈`、`设置`。
- 页签内容区采用单页全宽展示（不再左右分栏同时显示）。
- 左上角提供关闭按钮（`X`）。
- 底部状态条固定显示 `灵石数量`。
- 可单独开关，不中断主窗口探索循环。
- `战斗日志` 承载最近战斗记录，不再额外占用主横向窗口常驻区域。
- 战斗日志默认保留最近 10 条，按时间倒序展示，支持显示怪物、结果、掉落与资源结算摘要。
- Demo 阶段隐藏 `联机`、`管理员模式`、`手写支持`、`开机启动动画` 等未形成闭环的入口，避免把预留项暴露为可用功能。
- `Bug反馈` 提供最小闭环：问题描述输入、复制日志路径、导出反馈文件、打开数据目录。
- 设置页对未完全接线的选项使用 `（预留）` / `（实验）` 标识，降低误解成本。
- 交互约束：
- 默认低干扰，面板状态与窗口尺寸需持久化。
- 窗口缩放后核心信息（修炼阶段、探索进度）必须可见。
- UI 文案统一从 `scripts/ui/UiText.cs` 提供，避免分散硬编码与乱码问题。

## 7) 灵宠成长预留
- 当前 V1 主闭环仅保留 `pet_affinity` / 灵宠亲和等长期资源沉淀，不要求玩家维护独立心情条。
- `pet_mood`、`bond_level` 可继续作为后续迭代预留字段存在，但默认不作为主 UI 重点展示项。
- 若后续开启灵宠互动玩法，再补充心情恢复、衰减、羁绊被动等完整闭环。

## 8) 存档与防刷
- 存档结构：单文件 `user://save_state.cfg`（ConfigFile）+ `meta.version`。
- 必要字段：
- `meta.version`
- `meta.last_saved_unix`
- `ui.main_bar_x`
- `ui.main_bar_width`
- `ui.submenu_visible`
- `ui.submenu_active_left_tab`
- `ui.submenu_active_right_tab`
- `combat.recent_logs`
- `equipment.equipped`
- `inventory.__equipment_profiles`
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
