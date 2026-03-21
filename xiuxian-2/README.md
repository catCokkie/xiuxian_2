# xiuxian2

Godot 4.5 + C# (`net8.0`) desktop cultivation pet prototype. The current prototype focuses on a bottom-docked runtime where keyboard and mouse activity drives exploration, battle progress, and cultivation resource gain.

## Current Status

- Main scene: `res://scenes/PrototypeRoot.tscn`
- Core runtime coordinator: `scripts/game/PrototypeRootController.cs`
- Core exploration runtime: `scripts/game/ExploreProgressController.cs`
- Config-driven level/monster/drop runtime: `scripts/services/LevelConfigLoader.cs`
- Unified save file: `user://save_state.cfg`

The project already has a working prototype loop, design documentation, verification scripts, several Godot autoload services, and a minimal automated regression test project.

## Quick Start

### Requirements

- Godot 4.5 with C# support
- .NET 8 SDK
- Windows for full global input hook behavior
- Optional: `just` command runner

### Build

```bash
dotnet build xiuxian2.sln
```

Or:

```bash
just build
```

### Run

Open the project in Godot and run:

- `res://scenes/PrototypeRoot.tscn`

## Verification Workflow

Recommended verification flow:

```bash
just verify
```

This runs:

- project build
- scene/resource BOM checks
- a short manual runtime checklist

Extended verification:

```bash
just verify-runtime
```

This adds JSON parsing checks and an optional headless Godot smoke check via `scripts/tools/verify-runtime.ps1`.

### Optional Environment Variable

- `GODOT_BIN`: path to a Godot executable for the headless smoke step in `just verify-runtime`

If `GODOT_BIN` is not set, the script skips the headless launch check.

## Testing Status

- Automated regression command:

```bash
dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj
```

Or:

```bash
just test
```

- The current automated suite covers the minimum regression set: input-driven progress completion, unlocked level cycling, battle round resolution, and pity/daily-cap rules.
- Existing validation still includes build checks, config checks, and manual scene/runtime verification.
- `scripts/tests/InputSystemTest.cs` and `scenes/tests/InputSystemTest.tscn` are manual in-project test tools, not a separate automated test suite.

## Repository Map

- `project.godot` - Godot project config and autoload registration
- `xiuxian2.sln` / `xiuxian2.csproj` - main .NET solution and Godot project files
- `tests/xiuxian2.Tests/` - minimal automated regression suite
- `scenes/` - main scene and UI/test scenes
- `scripts/game/` - runtime orchestration and gameplay controllers
- `scripts/services/` - autoload state/services, input capture, progression, config loading, cloud save bridge
- `scripts/ui/` - UI controllers and centralized user-facing text
- `docs/` - system notes and design documentation
- `scripts/tools/` - verification and asset-processing scripts
- `assets/origin/` - source asset notes and import guidance

## Runtime Architecture

High-level runtime flow:

1. `project.godot` registers core autoload services such as `InputActivityState`, `InputHookService`, `LevelConfigLoader`, and player/resource state.
2. `scripts/game/PrototypeRootController.cs` coordinates UI state, persistence, and runtime service wiring.
3. `scripts/services/InputHookService.cs` captures input activity.
4. `scripts/services/InputActivityState.cs` aggregates input and emits `InputBatchTick` / `ActivityTick`.
5. `scripts/game/ExploreProgressController.cs` advances exploration and battle based on input-driven events.
6. `scripts/services/ActivityConversionService.cs` converts AP into cultivation resources.

Important gameplay constraint: exploration progress must be driven by `InputActivityState.InputBatchTick`. Do not introduce separate local `_Input` counting to push exploration progress.

## Key Documents

- `AGENTS.md` - project-specific engineering rules, coding conventions, and verification guidance
- `docs/design/README.md` - design documentation hub
- `docs/design/01_core_loop.md` - core gameplay loop
- `docs/design/02_systems.md` - systems and save/runtime constraints
- `docs/design/04_milestones.md` - milestones and current implementation scope
- `docs/design/10_todo.md` - prioritized maintenance/task list and issue retrospectives
- `docs/SAVE_SYSTEM.md` - unified save schema, section/key ownership, and migration notes
- `docs/INPUT_SYSTEM.md` - input collection system details
- `docs/INPUT_SYSTEM_SUMMARY.md` - implementation summary for the input system

## Encoding And Godot File Safety

This repository has strict encoding requirements:

- `*.cs` = `utf-8-bom`
- `*.md` = `utf-8-bom`
- `*.tscn` = `utf-8` without BOM

Pay special attention to scene files:

- Adding a BOM to `.tscn` files can cause Godot parse failures.
- Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/check-bom.ps1` after scene/resource edits.
- If a scene suddenly fails with `Parse Error: Expected '['`, check the file encoding first.

## Maintenance Notes

- User-facing text should be centralized in `scripts/ui/UiText.cs`.
- Save data changes must update both read and write paths together.
- Renaming scene nodes requires updating all related `GetNode*` references.
- `docs/design/09_level_monster_drop_sample.json` is currently part of the runtime config path; keep docs and config changes in sync.
- Cloud save support is present as a bridge layer, but Steamworks integration is still an incremental work area rather than a fully documented final pipeline.

### Config Validation Tool

The config validation panel is a developer tool, not a player-facing feature.

- Default behavior: hidden during normal play, and it should not occupy main-page space unless there is an actual config issue.
- Purpose: quickly surface broken or inconsistent config entries from `LevelConfigLoader`, especially around `level_id`, `monster_id`, and `drop_table_id` references.
- Runtime hints: `F11` cycles validation scope filters, and `F12` toggles current-level-only filtering.
- Expected usage: after changing level, monster, drop, or reward config, launch the main scene, trigger the validation view only when needed, and use the panel to locate bad references before manual gameplay verification.
- Follow-up check: after fixing config data, rerun `just verify` and confirm the panel is hidden again when no issues remain.

## Known Scope Limits

- Global input hook behavior is currently Windows-first.
- Cross-platform support is not complete.
- Automated regression coverage exists, but it currently covers only the minimum pure-logic safety net.

## Suggested First Steps For Maintainers

1. Read `AGENTS.md`.
2. Read `docs/design/README.md` and `docs/design/10_todo.md`.
3. Run `just build`.
4. Run `just verify`.
5. Open `res://scenes/PrototypeRoot.tscn` in Godot and confirm the main scene loads without parse errors.
