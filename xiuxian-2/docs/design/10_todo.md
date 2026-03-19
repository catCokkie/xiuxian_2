# 10 TODO（全局执行清单）

最后更新：2026-03-10（UI backlog 已同步到当前实现）

本文件作为仓库级 backlog 使用，目标不是记录零散想法，而是给后续 agent / 开发者逐条执行。

## 使用方式

### 状态规则
- `TODO`：未开始。
- `DOING`：正在实现，同一时间只允许一个子任务处于进行中。
- `BLOCKED`：受阻，必须写清阻塞原因。
- `MANUAL`：必须由人工在 Godot / 素材目录 / 外部平台中处理。
- `DONE`：已完成，并满足验收标准。

### 任务格式
每条任务尽量保持以下字段，方便切给不同 agent：

```md
- [状态] 任务名
  - Owner: 建议使用的 agent / 人工
  - Scope: 涉及文件或模块
  - Goal: 目标
  - Acceptance: 验收标准
  - Depends on: 前置条件；没有则写 `none`
  - Notes: 实施提示、风险或手动步骤
```

### Agent 约定
- `explore`：先摸清代码路径、依赖和现状。
- `deep` / `unspecified-high`：做跨模块功能实现。
- `visual-engineering`：UI、布局、素材接入、动画演出。
- `quick`：单文件小修、文档更新、文案修订。
- `MANUAL`：需要人在 Godot 编辑器、素材目录、Steam 后台或外部工具中完成。

### 手动素材占位标识
以下标识用于提醒“代码 agent 不应直接宣称已完成”：

- `[MANUAL-ASSET:UI]`：UI 框架、按钮、背景、图标导入
- `[MANUAL-ASSET:CHAR]`：角色/灵宠素材导入
- `[MANUAL-ASSET:MONSTER]`：怪物素材导入
- `[MANUAL-ASSET:ITEM]`：物品图标导入
- `[MANUAL-ASSET:EFFECT]`：特效素材导入
- `[MANUAL-ASSET:FONT]`：字体下载、授权说明、Godot 字体资源接入
- `[MANUAL-VERIFY:GODOT]`：必须在 Godot 编辑器里手动打开场景/检查效果
- `[MANUAL-PLATFORM:STEAM]`：需要 Steamworks/Steam 后台人工配置

## 当前执行原则
- 文档变更优先于代码变更。
- 涉及数值、存档 key、节点路径时，必须同步更新所有引用。
- 探索进度必须继续由 `InputActivityState.InputBatchTick` 驱动，不允许回退到本地 `_Input` 计数。
- 改动 `.tscn/.tres/.gdshader` 后，必须重新做 BOM 检查，并手动打开主场景确认无 Parse Error。
- 需要素材导入的任务，必须拆分为“代码可完成部分”与“人工导入部分”。

## P0（优先实现）

- [DONE] 最近战斗日志面板落地
  - Owner: `deep`
  - Scope: `scripts/game/ExploreProgressController.cs`, `scripts/ui/BookTabsController.cs`, `scenes/ui/*`, save/load 相关逻辑
  - Goal: 把现有最近战斗日志能力从“有运行时数据”提升为“用户可见、可恢复、可浏览”的正式面板
  - Acceptance: 主界面或子菜单中可查看最近 10 次战斗；显示怪物、掉落、灵气/悟性结算；切场景/读档后可恢复
  - Depends on: none
  - Notes: 当前实现落在统计页的最近战斗面板；`ExploreProgressController` 已负责日志记录与持久化；本次补充将展示文案继续收口到 `scripts/ui/UiText.cs`

- [DONE] 最小自动化回归用例
  - Owner: `deep`
  - Scope: 纯逻辑抽取 + 新测试工程（如引入 xUnit/NUnit）+ 构建脚本文档
  - Goal: 给主循环建立最低限度的自动化回归保护
  - Acceptance: 至少覆盖 4 条：输入推进、100% 切关、战斗结算、保底/上限；仓库内形成可重复执行的测试命令文档
  - Depends on: none
  - Notes: 已新增 `tests/xiuxian2.Tests/`，当前覆盖 5 组纯逻辑回归：探索进度完成、已解锁关卡轮转、战斗回合结算、保底/日上限规则、`ActivityConversionService` 资源结算与修炼模式 gating；命令入口已补到 `justfile` 与 `README.md`

- [TODO] 配置校验开发者工具收口
  - Owner: `quick` + `deep`
  - Scope: `scripts/game/ExploreProgressController.DebugAndValidation.cs`, `scenes/ui/MainBarWindow.tscn`, `scripts/services/LevelConfigLoader.cs`, 开发文档
  - Goal: 将现有配置校验能力明确定位为开发者工具，而不是玩家正式功能
  - Acceptance: 配置问题可定位到 `level_id/monster_id/drop_table_id`；默认不打扰玩家；入口、用途和使用方式在开发文档中有明确说明
  - Depends on: none
  - Notes: 当前已完成第一轮收口：主页面默认隐藏配置校验面板，且仅在存在问题时显示；`README.md` 已补开发者使用说明；后续以开发者诊断能力维护，不追求玩家向正式面板

- [TODO] 根 README 与设计文档口径持续对齐
  - Owner: `quick`
  - Scope: `README.md`, `docs/design/README.md`, `docs/INPUT_SYSTEM.md`, 其他新增文档
  - Goal: 防止仓库入口文档与真实实现继续漂移
  - Acceptance: 新增或变更的运行入口、命令、快捷键、测试方式，在 README 与相关专题文档中都有一致描述
  - Depends on: none
  - Notes: 这是持续任务，每次功能落地后顺手更新

## P1（可维护性与体验）

- [DONE] 拆分 `ExploreProgressController`
  - Owner: `deep`
  - Scope: `scripts/game/ExploreProgressController.cs` 及 partial 文件
  - Goal: 降低单类复杂度，把探索、战斗、调试校验、日志展示分层
  - Acceptance: 主控制器职责收敛；新增独立模块后不改变当前玩法约束；关键信号与存档行为不回归
  - Depends on: 最小自动化回归用例
  - Notes: 已完成第一轮职责拆分：主文件保留生命周期与状态字段，新增 `ExploreProgressController.Battle.cs`、`ExploreProgressController.DebugAndValidation.cs`、`ExploreProgressController.ProgressAndRuntime.cs`、`ExploreProgressController.TrackVisuals.cs`；`dotnet build` 与 `dotnet test` 已通过

- [DONE] 拆分 `LevelConfigLoader`
  - Owner: `deep`
  - Scope: `scripts/services/LevelConfigLoader.cs`
  - Goal: 分离配置加载、索引、校验、模拟、掉落结算逻辑
  - Acceptance: 至少拆出“配置读取/索引”“校验/摘要”“掉落/模拟”三个职责层，现有 JSON 口径不变
  - Depends on: 最小自动化回归用例
  - Notes: 已按 partial 文件完成第一轮职责拆分：主文件保留配置读取/索引与基础查询，新增 `LevelConfigLoader.DropAndRewards.cs`、`LevelConfigLoader.RuntimeState.cs`、`LevelConfigLoader.ValidationAndSimulation.cs`；`dotnet build` 与 `dotnet test` 已通过

- [DONE] 存档 schema 文档化
  - Owner: `quick`
  - Scope: 新建 `docs/SAVE_SYSTEM.md` 或等价文档，必要时补充 `README.md`
  - Goal: 明确 `user://save_state.cfg` 的 section/key、版本、迁移约束
  - Acceptance: 新维护者能根据文档安全改 key，并知道哪些地方要同步更新
  - Depends on: none
  - Notes: 已新增 `docs/SAVE_SYSTEM.md`，覆盖统一存档 section/key、状态归属、legacy fallback 与修改清单；`README.md` 已补入口

- [TODO] `UiText` 外置化评估
  - Owner: `explore` -> `deep`
  - Scope: `scripts/ui/UiText.cs`, UI 相关脚本, 文案资源方案
  - Goal: 判断是否值得把文案从 C# 常量迁移到 JSON/CSV/Resource
  - Acceptance: 产出一份明确结论；若实施，则保持现有 key 语义稳定，并支持运行时切换资源
  - Depends on: none
  - Notes: 先做方案评估，不要直接开大重构

- [TODO] 子菜单设置页结构优化
  - Owner: `visual-engineering`
  - Scope: `scripts/ui/BookTabsController.cs`, `scenes/ui/SubmenuBookWindow.tscn`, `docs/design/05_ui_style.md`
  - Goal: 提升设置页信息架构和可读性，减少滚动负担
  - Acceptance: 系统/画面/进度三组首屏可读，常用项 2 次点击内可达
  - Depends on: none
  - Notes: 优先遵守既有书卷式 UI 语言，不要改成通用后台页风格

- [DONE] 主条布局自适应校验
  - Owner: `visual-engineering`
  - Scope: `scenes/ui/MainBarWindow.tscn`, `scripts/ui/MainBarLayoutController.cs`, `scripts/game/ExploreProgressController.cs`
  - Goal: 保证不同宽度下核心信息不遮挡
  - Acceptance: 最小宽与最大宽下无重叠；区域名、境界、进度条始终可见
  - Depends on: none
  - Notes: 已完成代码侧第二轮收口：处理了 `ConfigValidationPanel / ActionModeOptionButton / LevelOptionButton` 紧凑布局冲突，修复主条错误锁顶，并补齐 `ZoneLabel` 可见性与 `RealmStageLabel / ZoneLabel` 自适应裁剪；本项已完成 `[MANUAL-VERIFY:GODOT]` 收口

- [DONE] 主页面信息减法第二轮
  - Owner: `visual-engineering`
  - Scope: `scenes/ui/MainBarWindow.tscn`, `scripts/ui/MainBarLayoutController.cs`, `scripts/game/ExploreProgressController*.cs`
  - Goal: 继续降低主页面信息密度，只保留首屏必需信息
  - Acceptance: 主页面默认只保留书页入口、核心战斗/探索态、修炼/突破与探索进度；次要配置/调试/统计信息进入子页面或按需显示
  - Depends on: 主条布局自适应校验
  - Notes: 已完成第二轮减法：主页面默认隐藏 `ActionModeOptionButton` 与 `LevelOptionButton`，并在书页设置区补充“运行时快捷操作”入口；键盘 `F4/F5` 仍保留

## P2（平台与发布准备）

- [TODO] Steamworks 接入准备
  - Owner: `deep` + `[MANUAL-PLATFORM:STEAM]`
  - Scope: `scripts/services/CloudSaveSyncService.cs`, 发布配置, 外部 SDK 接入文档
  - Goal: 建立 Steamworks 初始化、自检日志和接入准备清单
  - Acceptance: SDK 初始化链路明确；有失败回退日志；文档中写明人工后台配置步骤
  - Depends on: 存档 schema 文档化
  - Notes: 代码与后台配置分开追踪

- [TODO] Steam Cloud 冲突策略
  - Owner: `deep` + `[MANUAL-PLATFORM:STEAM]`
  - Scope: `scripts/services/CloudSaveSyncService.cs`, 保存/加载流程, 文档
  - Goal: 支持本地优先 / 云端优先 / 手动选择三种策略
  - Acceptance: 首启下载、冲突检测、失败回退路径可说明、可验证
  - Depends on: Steamworks 接入准备
  - Notes: 需要补交互文案与异常分支测试策略

- [TODO] 发布前安全与防刷复核
  - Owner: `explore` -> `deep`
  - Scope: 输入采集、时间校验、资源结算、后台输入场景
  - Goal: 复核异常输入与时间跳变下的收益衰减策略
  - Acceptance: 异常输入、时间跳变、后台高频输入三类场景都有明确处理结论
  - Depends on: 最小自动化回归用例
  - Notes: 必须基于仓库现状验证，不要空谈“AI 反作弊”

## 素材接入专项（代码 + 人工分拆）

- [MANUAL] [MANUAL-ASSET:FONT] 标题/正文字体接入
  - Owner: `MANUAL`
  - Scope: 外部字体下载、许可证整理、Godot 字体资源创建
  - Goal: 把 `assets/origin/fonts/FONTS_README.md` 提到的字体真正接入项目
  - Acceptance: Godot 内可正常显示标题/正文字体；README 或相关文档补充来源与许可说明
  - Depends on: none
  - Notes: 该任务不能由纯代码 agent 宣称完成，必须人工下载和导入文件

- [TODO] [MANUAL-ASSET:UI] 主条/书页 UI 素材接入
  - Owner: `visual-engineering` + `MANUAL`
  - Scope: `assets/origin/README.md`, `scenes/ui/*.tscn`, 相关贴图资源目录
  - Goal: 将 UI 框架素材映射到主条、页签、按钮和状态条
  - Acceptance: 占位资源被正式素材替换；按钮状态齐全；布局未因贴图尺寸变化而破坏
  - Depends on: 主条布局自适应校验
  - Notes: 代码 agent 负责场景引用、样式适配；人工负责真实图片导入与导入参数检查；附带 `[MANUAL-VERIFY:GODOT]`

- [TODO] [MANUAL-ASSET:CHAR] 灵宠素材接入
  - Owner: `visual-engineering` + `MANUAL`
  - Scope: 角色显示节点、动画资源、情绪状态表现
  - Goal: 接入待机/眨眼/互动/心情状态灵宠素材
  - Acceptance: 至少有 idle / blink / interact 三组动画可切换；低心情/高心情状态可显示
  - Depends on: none
  - Notes: 需要人工确认 spritesheet 切帧、透明通道和导入设置

- [TODO] [MANUAL-ASSET:MONSTER] 怪物与掉落标记素材接入
  - Owner: `visual-engineering` + `MANUAL`
  - Scope: 战斗轨道怪物槽位、怪物贴图、受击/死亡、掉落标记
  - Goal: 用正式怪物素材替换当前战斗轨道占位表现
  - Acceptance: `mob_001`、`mob_002`、`drop_marker` 至少完成一轮接入与播放
  - Depends on: 最近战斗日志面板落地
  - Notes: 若 `mob_002` 命名仍未定，可先保留配置 id、不阻塞代码接线

- [TODO] [MANUAL-ASSET:ITEM] 物品图标接入
  - Owner: `visual-engineering` + `MANUAL`
  - Scope: 背包/掉落展示、物品图标资源引用
  - Goal: 给灵草、灵气碎片、突破丹接入正式 icon
  - Acceptance: 至少 3 个现有掉落物品在 UI 中可看到对应图标
  - Depends on: 最近战斗日志面板落地
  - Notes: 如果当前 UI 尚无图标位，可先补预留节点和注释文档

- [TODO] [MANUAL-ASSET:EFFECT] 突破/顿悟/区域完成特效接入
  - Owner: `visual-engineering` + `MANUAL`
  - Scope: 动效节点、触发逻辑、素材导入
  - Goal: 在不打扰主体验的前提下，为关键节点加入特效演出
  - Acceptance: 至少 1 个突破特效和 1 个区域完成特效可以正常触发；性能与布局可接受
  - Depends on: 灵宠素材接入 或 UI 素材接入（二选一完成后即可）
  - Notes: 需要人工检查序列帧导入、透明通道、尺寸锚点

## 文档与流程专项

- [TODO] 素材接入流程文档
  - Owner: `quick`
  - Scope: 新建 `docs/ASSET_PIPELINE.md` 或等价文档
  - Goal: 说明哪些素材任务需要人工、哪些可以交给 agent 改代码
  - Acceptance: 至少覆盖目录映射、导入检查、BOM/编码无关说明、透明通道与 Godot 导入注意事项
  - Depends on: none
  - Notes: 与本文件中的 `[MANUAL-ASSET:*]` 标识保持一致

- [TODO] 手动验证清单收口
  - Owner: `quick`
  - Scope: `README.md`, `justfile`, `scripts/tools/verify-runtime.ps1`, 设计/维护文档
  - Goal: 把“需要人工打开 Godot 验证”的步骤整理成一致口径
  - Acceptance: README、脚本输出、待办文档中的手工验证条目一致
  - Depends on: none
  - Notes: 让后续 agent 知道何时必须停下来交给人工验证

## 已完成（摘录）

- [DONE] 输入驱动探索进度（与 AP 解耦）
- [DONE] 多关卡配置驱动（`levels[]`）与 100% 自动切关
- [DONE] 怪物战斗与掉落配置驱动
- [DONE] 保底与日/小时上限规则
- [DONE] 子菜单改为单页全宽内容区，左上角关闭按钮
- [DONE] UI 文案收口到 `scripts/ui/UiText.cs`
- [DONE] 根目录 `README.md` 草案建立
- [DONE] `docs/INPUT_SYSTEM.md` 中快捷键状态与实现对齐
- [DONE] 最小自动化回归测试工程与 `just test` 命令接入
- [DONE] 第二批自动化回归：`ActivityConversionService` 资源结算与修炼模式测试
- [DONE] 统一存档 schema 文档化与 README 入口补充
- [DONE] `LevelConfigLoader` 按职责拆分为多 partial 文件
- [DONE] `ExploreProgressController` 按职责拆分为多 partial 文件
- [DONE] 最近战斗日志面板展示文案继续收口到 `scripts/ui/UiText.cs`
- [DONE] 主页面第一轮减法：默认隐藏活动率文案与无错误时的配置校验面板
- [DONE] 主页面第二轮减法：主行为/副本切换移出主页面，收口到书页设置区
- [DONE] 主条锁底初始化修复，避免错误锁定到屏幕上方
- [DONE] 主条紧凑布局第一轮修复，处理 `ConfigValidationPanel / ActionModeOptionButton / LevelOptionButton` 冲突

## 问题复盘（避免复发）

- [DONE] Godot 场景文件 BOM 触发解析失败（2026-03-01）
  - 问题现象：`res://scenes/ui/MainBarWindow.tscn:1 Parse Error: Expected '['`，并连锁出现 `Failed loading resource` 与 `Node not found`
  - 根因：`.tscn` 被保存为 `UTF-8 with BOM`，Godot 文本场景解析器在首字符处失败
  - 规避规则：
    - `.tscn/.tres/.gdshader` 统一使用 `UTF-8 (no BOM)`
    - 修改场景文件后，优先检查文件头字节，禁止出现 `EF BB BF`
    - 场景文本属性必须是完整引号行（`text = "..."`），禁止出现缺失结尾引号的半行文本
    - 提交前执行一次场景快速自检（至少打开主场景并确认无 Parse Error）
