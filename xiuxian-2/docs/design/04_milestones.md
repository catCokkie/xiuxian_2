# 04 开发里程碑

## M0 - 技术预研（2-3 天）
- 完成 Windows 全局键鼠计数 PoC（仅次数，不含内容）。
- 确认 Godot 窗口置底驻留方案与多分辨率适配策略。
- 输出输入采集权限提示与隐私文案。

## M1 - 核心管线（4-6 天）
- 接入 `InputHookService` + AP 聚合。
- 完成 AP -> 资源 -> 境界经验的结算链路。
- 建立本地存档与异常输入保护。
- 规则：探索进度的唯一输入源是 `InputActivityState.InputBatchTick`（按输入事件计数推进；禁止在 `ExploreProgressController` 内做本地 `_Input` 统计）。

## 当前进度快照（2026-02-26）
- `M1-1` 已完成：`InputHookService -> InputActivityState -> ExploreProgressController` 已串联。
- `M1-2` 已完成：输入与状态服务 AutoLoad 已接入（`InputActivityState`、`InputHookService`、`InputPauseShortcut`、`LevelConfigLoader`、`ResourceWalletState`、`PlayerProgressState`、`ActivityConversionService`）。
- `M1-3` 已完成：AP -> 资源 -> 境界经验链路已打通，探索进度按输入事件独立推进。
- `M1-4` 已完成：关卡/怪物/掉落改为 JSON 配置驱动，支持多关卡轮转、战斗结算、保底与上限规则。

## 当前实现补充（2026-03-23）
- 子菜单结构已收口：`修炼概况 / 战斗日志 / 装备情况 / 统计概览 / Bug反馈 / 设置`。
- 最近战斗日志已迁移到子菜单，不在主条保留独立日志展示。
- 战斗主线已拆出 `BattleStartRules / BattleRules / BattleLifecycleRules / RewardRules`。
- 玩家/怪物/装备已进入统一属性管线，装备能真实影响战斗。
- 已支持最小装备玩法闭环：首通固定装备奖励 -> 背包 -> 手动装备 -> 存档恢复。
- 自动化回归、统一测试入口、GitHub Actions 已接入。

## M2 - 桌面 UI（4-5 天）
- 实现底部桌宠条（常驻、低干扰）。
- 实现书卷式系统面板（顶部左右页签组 + 单页全宽内容区）。
- 主条演出区保留核心状态，减少无效说明行。
- UI 文案统一收口到 `scripts/ui/UiText.cs`。

## M2 当前实现口径（与代码对齐）
- 子菜单窗口：顶部左右两组页签，内容区单页全宽显示，左上角关闭按钮。
- 主条窗口：`BattleTrack` 精简为一行核心状态 + 角色/怪物/HP 信息。
- 状态机最小集：`Exploring`、`InBattle`、`ExploreComplete`。
- `battle_state` 存档口径：`exploring` / `in_battle`（读档安全回退到 `exploring`）。

## M3 - 养成系统（4-5 天）
- 境界突破、炼丹、灵宠互动/羁绊扩展。
- 加入短时技能（打坐）与灵宠互动玩法（如 `逗灵宠`）的完整闭环。
- 完成首轮数值平衡。

## M4 - 打磨与验收（3-5 天）
- 适配不同 DPI 与多显示器场景。
- 验证异常输入、防刷、时间跳变处理。
- 完成新手引导与关键提示文案。

## Post-MVP 待办（主体功能完成后）
- Steamworks SDK 正式接入（初始化、回调、发布构建校验）。
- Steam Cloud 云存档联调（本地/云端冲突策略、首启拉取策略、失败回退）。
- Steam 后台配置收尾（Cloud 配额、文件规则、测试分支验证）。

## 下一阶段建议（M2.5）
- 增加配置校验与模拟结果可视化（掉落命中率、保底触发率、日/小时上限命中率）。
- 将当前最小装备玩法闭环升级为正式装备内容闭环（更多来源、明确装备管理 UX）。
- 继续推进控制器瘦身，补全战斗开始/重置/显示层与规则层的剩余分离。

## V1 验收标准
- 用户可在后台使用场景中持续获得可感知进度。
- 桌面底栏稳定驻留，不影响主工作区操作。
- 输入采集全程不记录敏感内容。
- 60-90 分钟累计使用可达到第 3 境。
