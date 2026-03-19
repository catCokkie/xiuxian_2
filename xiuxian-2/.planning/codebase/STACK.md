# Stack

## Runtime

- Engine: Godot 4.5 with C# support, declared in `project.godot` and `config/features=PackedStringArray("4.5", "C#", "Forward Plus")`
- Main scene: `scenes/PrototypeRoot.tscn`
- Main assembly/project: `xiuxian2.csproj`
- Main solution: `xiuxian2.sln`

## Languages And SDKs

- Primary language: C#, used across `scripts/game/`, `scripts/services/`, and `scripts/ui/`
- Project SDK: `Godot.NET.Sdk/4.5.1` in `xiuxian2.csproj`
- Main target framework: `net8.0` in `xiuxian2.csproj`
- Conditional Android target: `net9.0` when `GodotTargetPlatform == android` in `xiuxian2.csproj`
- Dynamic loading: enabled with `<EnableDynamicLoading>true</EnableDynamicLoading>` in `xiuxian2.csproj`

## Godot Project Shape

- Autoload services are registered in `project.godot`
- Runtime composition is scene-driven: `scenes/PrototypeRoot.tscn` instantiates `MainBarWindow`, `SubmenuBookWindow`, and `ExploreProgressController`
- The app entry controller is `scripts/game/PrototypeRootController.cs`

## Core Runtime Services

- Input capture: `scripts/services/InputHookService.cs`
- Input aggregation and AP accumulation: `scripts/services/InputActivityState.cs`
- Input pause shortcut: `scripts/services/InputPauseShortcut.cs`
- Wallet/resources: `scripts/services/ResourceWalletState.cs`
- Player progression: `scripts/services/PlayerProgressState.cs`
- Main action mode: `scripts/services/PlayerActionState.cs`
- Activity settlement: `scripts/services/ActivityConversionService.cs`
- Config runtime: `scripts/services/LevelConfigLoader.cs` plus partial files under `scripts/services/LevelConfigLoader*.cs`
- Cloud save bridge: `scripts/services/CloudSaveSyncService.cs`

## Build And Verification Tooling

- Solution build: `dotnet build xiuxian2.sln`
- Project build: `dotnet build xiuxian2.csproj`
- Task runner: `justfile`
- Common recipes in `justfile`:
  - `just build`
  - `just test`
  - `just check-bom`
  - `just verify`
  - `just verify-runtime`
- Scene/resource encoding gate: `scripts/tools/check-bom.ps1`
- Runtime verification helper: `scripts/tools/verify-runtime.ps1`

## Test Stack

- Automated test project: `tests/xiuxian2.Tests/xiuxian2.Tests.csproj`
- Test framework: xUnit via `xunit` and `xunit.runner.visualstudio`
- Test SDK: `Microsoft.NET.Test.Sdk`
- Coverage collector: `coverlet.collector`
- Current test files live in `tests/xiuxian2.Tests/`
- Manual/in-project test scene remains in `scripts/tests/InputSystemTest.cs` and `scenes/tests/InputSystemTest.tscn`

## Data And Configuration

- Unified save file: `user://save_state.cfg`, documented in `docs/SAVE_SYSTEM.md`
- Runtime level/monster/drop config currently points at `docs/design/09_level_monster_drop_sample.json` from `scripts/services/LevelConfigLoader.cs`
- User/system settings are persisted through `BookTabsController` and save orchestration in `scripts/game/PrototypeRootController.cs`

## Encoding And File Rules

- Encoding rules are defined in `.editorconfig`
- `*.cs` uses `utf-8-bom`
- `*.md` uses `utf-8-bom`
- `*.tscn` uses UTF-8 without BOM
- BOM-sensitive Godot scene/resource validation is enforced by `scripts/tools/check-bom.ps1`

## Repository Support Files

- Maintainer/project rules: `AGENTS.md`
- Project overview and workflows: `README.md`
- Save schema reference: `docs/SAVE_SYSTEM.md`
- Backlog and maintenance planning: `docs/design/10_todo.md`

## Notable Structural Patterns

- Large gameplay/runtime services are being split into partial classes, for example:
  - `scripts/game/ExploreProgressController*.cs`
  - `scripts/services/LevelConfigLoader*.cs`
- Pure logic rules for tests and separation live under `scripts/core/`
- UI-visible text is centralized in `scripts/ui/UiText.cs`
