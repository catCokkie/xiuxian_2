# Structure

## Top-Level Layout

- `project.godot` - Godot project config and autoload registration
- `xiuxian2.csproj` - main Godot/C# project
- `xiuxian2.sln` - solution containing main project and tests
- `justfile` - common build/test/verify task runner
- `README.md` - maintainer-facing project overview
- `opencode.json` - project-level OpenCode plugin config
- `.planning/` - planning and mapping outputs

## Main Source Directories

### `scenes/`

- `scenes/PrototypeRoot.tscn` - runtime root scene
- `scenes/ui/` - UI scenes such as `MainBarWindow.tscn` and `SubmenuBookWindow.tscn`
- `scenes/tests/` - manual in-project test scenes such as `InputSystemTest.tscn`

### `scripts/game/`

- Scene-level runtime controllers
- Main anchors:
  - `scripts/game/PrototypeRootController.cs`
  - `scripts/game/ExploreProgressController*.cs`
- This folder is where scene orchestration and gameplay runtime live

### `scripts/services/`

- Long-lived autoload services and state holders
- Examples:
  - `scripts/services/InputActivityState.cs`
  - `scripts/services/InputHookService.cs`
  - `scripts/services/ActivityConversionService.cs`
  - `scripts/services/LevelConfigLoader*.cs`
  - `scripts/services/CloudSaveSyncService.cs`
- This is the main “global runtime services” directory

### `scripts/ui/`

- Scene-attached UI logic and shared text helpers
- Main anchors:
  - `scripts/ui/MainBarLayoutController.cs`
  - `scripts/ui/SubmenuWindowController.cs`
  - `scripts/ui/BookTabsController.cs`
  - `scripts/ui/UiText.cs`

### `scripts/core/`

- Pure rule classes extracted for testability
- Current files include:
  - `scripts/core/ExploreProgressionRule.cs`
  - `scripts/core/BattleRoundRule.cs`
  - `scripts/core/DropEconomyRule.cs`
  - `scripts/core/LevelCycleRule.cs`
  - `scripts/core/ActivitySettlementRule.cs`

### `scripts/tools/`

- Repository support scripts and asset/verification tooling
- Current anchors:
  - `scripts/tools/check-bom.ps1`
  - `scripts/tools/verify-runtime.ps1`
  - `scripts/tools/convert-jpg-alpha.ps1`
  - `scripts/tools/normalize-origin-sprites.ps1`

### `scripts/tests/`

- Manual or in-runtime test helpers rather than the formal automated suite
- Current anchor: `scripts/tests/InputSystemTest.cs`

## Documentation Directories

### `docs/`

- Maintainer/system docs
- Main anchors:
  - `docs/INPUT_SYSTEM.md`
  - `docs/INPUT_SYSTEM_SUMMARY.md`
  - `docs/SAVE_SYSTEM.md`

### `docs/design/`

- Design and planning artifacts
- Main anchors:
  - `docs/design/README.md`
  - `docs/design/01_core_loop.md`
  - `docs/design/02_systems.md`
  - `docs/design/04_milestones.md`
  - `docs/design/09_level_monster_drop_sample.json`
  - `docs/design/10_todo.md`

## Testing Layout

- Automated tests: `tests/xiuxian2.Tests/`
- Test project file: `tests/xiuxian2.Tests/xiuxian2.Tests.csproj`
- Current test files are grouped by pure rule class, one file per rule domain

## Assets Layout

- `assets/origin/` is the source asset library and manual import staging area
- Subfolders include:
  - `assets/origin/ui/`
  - `assets/origin/spirit_pet/`
  - `assets/origin/scene_bg/`
  - `assets/origin/monsters/`
  - `assets/origin/items/`
  - `assets/origin/effects/`
  - `assets/origin/fonts/`
- `assets/origin/README.md` documents intended mapping into runtime asset directories

## Naming Patterns

- Partial files use `ClassName.Feature.cs`
- Services tend to live in `Xiuxian.Scripts.Services`
- Game/runtime scene controllers tend to live in `Xiuxian.Scripts.Game`
- Test files use `*Tests.cs`

## High-Value Navigation Starting Points

- Runtime bootstrap: `project.godot`, `scenes/PrototypeRoot.tscn`, `scripts/game/PrototypeRootController.cs`
- Explore/battle runtime: `scripts/game/ExploreProgressController*.cs`
- Config/rewards/runtime data: `scripts/services/LevelConfigLoader*.cs`
- Save system: `docs/SAVE_SYSTEM.md`
- Ongoing maintenance priorities: `docs/design/10_todo.md`
