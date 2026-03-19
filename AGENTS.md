# AGENTS.md

## 适用范围
- 本文件适用于工作区根目录 `e:/vsp/xiuxian_2` 及项目目录 `e:/vsp/xiuxian_2/xiuxian-2`。

## 项目概况
- 引擎：Godot 4.5 + C#（`net8.0`）。
- 主场景：`res://scenes/PrototypeRoot.tscn`。
- 核心运行服务在 `project.godot` 中通过 AutoLoad 挂载。

## 开发约束
- 保持 `ExploreProgressController` 的探索进度为“输入事件驱动”。
  - 不要在探索逻辑中新增本地 `_Input` 计数。
  - 探索进度唯一来源应保持为 `InputActivityState.InputBatchTick`。
- AP（活动点）仅用于资源结算，不直接推动探索进度。
- UI 文案尽量统一维护在 `scripts/ui/UiText.cs`。
- 场景文本文件（`.tscn`、`.tres`、`.gdshader`）必须为 UTF-8 **无 BOM**。

## 存档与运行态约定
- 统一存档文件：`user://save_state.cfg`。
- 存档结构负责人：`scripts/game/PrototypeRootController.cs`。
- 探索运行态负责人：`scripts/game/ExploreProgressController.cs`。
- 关卡/掉落运行态负责人：`scripts/services/LevelConfigLoader.cs`。

## 当前功能状态（随开发维护）
- 已完成：
  - 输入 -> AP -> 资源结算主链路。
  - JSON 配置驱动的关卡/怪物/掉落。
  - 配置校验/模拟调试快捷能力。
  - 统计页“最近战斗日志”记录与列表展示。
- 进行中：
  - 配置校验可视化面板打磨。
  - 最小自动化回归用例补齐。

## 快速验证
- 编译：在 `xiuxian-2` 目录执行 `dotnet build xiuxian2.sln`。
- Godot 手动检查：
  1. 主场景可打开且无 Parse Error。
  2. 统计页最近战斗列表会随战斗结果刷新。
  3. 读档后探索运行态与最近战斗日志可恢复。

## 编辑注意事项
- 优先小步、局部改动。
- 若变更存档 key 或场景节点名，必须同步更新所有引用。
- 新增用户可见文案时，优先更新 `UiText.cs`。
