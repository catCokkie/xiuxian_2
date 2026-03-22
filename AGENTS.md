# AGENTS.md

## 适用范围
- 本文件适用于 `E:/vsp/xiuxian_2` 与 `E:/vsp/xiuxian_2/xiuxian-2`。
- 项目技术栈：Godot 4.5 + C#（`Godot.NET.Sdk/4.5.1`，`net8.0`）。

## 项目概览
- 主场景：`res://scenes/PrototypeRoot.tscn`。
- 核心运行协调器：`scripts/game/PrototypeRootController.cs`。
- 探索运行时：`scripts/game/ExploreProgressController.cs`（含 partial 文件）。
- 配置运行时：`scripts/services/LevelConfigLoader.cs`。
- 统一存档文件：`user://save_state.cfg`。

## 外部规则文件
- `.cursorrules`：未找到。
- `.cursor/rules/*`：未找到。
- `.github/copilot-instructions.md`：未找到。
- 默认以本 `AGENTS.md` 作为主要代理规范。

## 构建、验证与测试命令

### 推荐命令（来自仓库文件）
- 构建解决方案：`dotnet build xiuxian2.sln`
- 构建项目：`dotnet build xiuxian2.csproj`
- 命令封装：`just build`（若已安装 `just`）
- 场景编码检查：`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/check-bom.ps1`
- 综合验证：`just verify`（执行 build + BOM 检查 + 手工检查清单）

### 当前测试状态
- 目前没有接入 `dotnet test` 的独立自动化测试项目。
- 现有 `scripts/tests/InputSystemTest.cs` 属于游戏内/手工测试工具流程。
- 手工测试场景：`res://scenes/tests/InputSystemTest.tscn`（参见文档）。

### 单测命令说明（重点）
- 当前仓库不存在可直接运行的“单条单元测试”命令。
- 如果未来新增 xUnit/NUnit 测试项目，可使用：
  - `dotnet test <test-project.csproj> --filter "FullyQualifiedName~<TestNameFragment>"`
  - `dotnet test <test-project.csproj> --filter "ClassName~<ClassName>"`
- 在真实测试工程落地前，不要对外宣称支持单测命令。

### 建议验证顺序（代理执行）
1. `dotnet build xiuxian2.sln`
2. `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/check-bom.ps1`
3. 打开 Godot 并按以下顺序做场景检查：
   - 主场景可打开且无 Parse Error。
   - 战斗后“最近战斗日志”会刷新。
   - 重载后探索运行态与最近战斗日志可恢复。

## 编码与文件格式规则
- 遵循 `.editorconfig`：
  - `*.cs` = `utf-8-bom`
  - `*.md` = `utf-8-bom`
  - `*.tscn` = `utf-8`（无 BOM）
- `*.tres` 与 `*.gdshader` 也应为 UTF-8 无 BOM（项目规则）。
- 场景文件若带 BOM，Godot 可能出现解析/加载错误。

## 核心玩法约束（不可破坏）
- 探索进度必须由 `InputActivityState.InputBatchTick` 输入驱动。
- 不要新增本地 `_Input` 计数去推进探索进度。
- AP 仅用于资源结算，不可直接推动探索进度。
- 用户可见文案尽量集中到 `scripts/ui/UiText.cs`。

## 代码库观察到的 C# 风格约定

### Import 与命名空间
- 常见 import 顺序：
  1) `using Godot;`
  2) `using System...`
  3) 项目命名空间（`Xiuxian.Scripts...`）
- 多数 gameplay/service 文件使用 `Xiuxian.Scripts.*` 命名空间。
- 部分较早的 UI 脚本使用全局命名空间；改动时遵循原文件风格。

### 命名规则
- 公有类型/方法/属性：PascalCase。
- 私有字段：`_camelCase`。
- 常量：PascalCase（如 `SaveSchemaVersion`、`CloudUploadMinIntervalSeconds`）。
- 事件处理函数：`OnXxx...` 风格。

### 格式规则
- 4 空格缩进，K&R 大括号，一行一语句。
- 优先使用短 guard clause 与早返回。
- 方法职责尽量单一，复杂逻辑优先抽取 helper。

### 类型与可空
- 代码中已大量使用可空引用类型（`Type?`）。
- 项目级 nullable 上下文尚未完全统一；避免大范围可空重构。
- 读取节点/服务优先 `GetNodeOrNull<T>` + null 检查。
- `null!` 仅用于 `_Ready` 中保证初始化的字段。

### Godot 约定
- 信号通过 `[Signal]` + delegate 声明。
- 订阅放 `_Ready`，解除订阅放 `_ExitTree`。
- 涉及节点就绪顺序问题时使用 `CallDeferred`。
- 跨 Variant/JSON 边界时使用 `Godot.Collections.Dictionary/Array`。

### 错误处理与日志
- 禁止空 `catch`。
- 先做输入/配置校验，再早返回。
- 可恢复问题使用 `GD.PushWarning`。
- 严重运行时问题使用 `GD.PushError`。
- 调试输出使用 `GD.Print`，避免逐帧噪声日志。

### 配置与持久化
- 存档读写结构归口：`PrototypeRootController`。
- 关卡/怪物/掉落运行时与校验归口：`LevelConfigLoader`。
- 修改存档 key 或节点名时，必须一次性更新全部引用。

## UI 与素材接入规则
- 优先保证行为稳定，再做视觉打磨。
- 素材接入优先非破坏式方案（必要时回退占位资源）。
- 接入图标/立绘前先确认格式与透明通道兼容性。
- 不要破坏 `.tscn` 既有锚点与 offset 语义。

## 代理协作约定
- 以小步、局部改动为主，避免大范围重写。
- 每次改动后做构建与相关检查。
- 仓库证据不足时，明确写出假设。
- 不要虚构仓库中不存在的命令或能力。

## 素材流水线备注
- `assets/origin` 可能包含迭代期混合源格式。
- 新纹理接入场景前，请先校验真实文件签名。
- `scripts/tools/` 下常用脚本：
  - `check-bom.ps1`：场景/资源编码门禁。
  - `convert-jpg-alpha.ps1`：将指定 JPG 精灵转为带 alpha 的 PNG。
  - `normalize-origin-sprites.ps1`：对指定 PNG 做裁切与缩放标准化。
- 大背景可用 JPG；图标/立绘优先使用真透明 PNG。

## 未来测试扩展建议
- 若新增测试项目，请加入 solution 并写明准确路径。
- 测试命名建议：`<Feature>Tests`、`<Feature>IntegrationTests`。
- 面向 CI 的用例优先确定性输入（固定 seed/固定时间）。
- 若测试依赖 Godot 运行时，需记录必要环境变量（例如 `GODOT_BIN`）。
- 仅在测试工程稳定后再新增 `just test` 配方。

## 变更安全检查清单
- 改了存档 key？读写两端必须同步更新。
- 改了 `.tscn` 节点名/路径？同步更新所有 `GetNode*` 引用。
- 新增用户可见文案？优先归口到 `scripts/ui/UiText.cs`。
- 修改配置 schema？重新执行 loader 校验与游戏内冒烟验证。
- 修改场景文件？重跑 BOM 检查并在 Godot 中打开主场景验证。

## 常用路径速查
- `project.godot`
- `xiuxian2.sln`
- `xiuxian2.csproj`
- `justfile`
- `scripts/game/PrototypeRootController.cs`
- `scripts/game/ExploreProgressController.cs`
- `scripts/services/LevelConfigLoader.cs`
- `scripts/ui/UiText.cs`
- `scripts/tools/check-bom.ps1`
