# Xiuxian Pet Desktop - Design Hub

本目录用于沉淀 Godot 4 + C# 修仙桌宠项目的可执行策划文档。

## 文档索引
- `00_vision.md`：产品愿景、边界、目标用户体验
- `01_core_loop.md`：以键鼠行为为核心的循环设计
- `02_systems.md`：系统拆分、数据字段、技术约束
- `03_progression_and_balance.md`：进度公式与数值护栏
- `04_milestones.md`：开发里程碑与验收标准
- `05_ui_style.md`：桌面底栏与展开面板的界面风格规范
- `06_bottom_exploration_battle.md`：底部探索/战斗进度界面详细设计（你来细化）
- `07_content_template.md`：境界/地点/怪物/掉落物通用设计模板
- `08_content_sample_qi_refining.md`：炼气初期内容示例（可直接改）
- `09_level_monster_drop_sample.md` / `.json`：关卡、怪物、掉落配置与样例数据
- `10_todo.md`：全局任务池（按优先级、状态、验收标准维护）

## 工作规则
- 设计变更先改文档，再改代码。
- 涉及数值必须写清公式、上下限、调参口。
- 每个系统都要定义：输入、输出、存档字段、UI 入口。
- 输入采集只记录“次数/强度”，不记录键值文本和鼠标轨迹明文。
- UI 文案优先维护在 `scripts/ui/UiText.cs`，避免脚本分散硬编码。
- Godot 场景文件 `*.tscn` 必须使用 `UTF-8`（无 BOM）。写入 BOM 会导致 `Parse Error: Expected '['` 并引发场景依赖加载失败。
