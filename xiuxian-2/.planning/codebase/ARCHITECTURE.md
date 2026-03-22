# Architecture

## Entry Point

- Godot entry scene is `scenes/PrototypeRoot.tscn`, referenced by `project.godot`
- Root scene script is `scripts/game/PrototypeRootController.cs`
- `PrototypeRoot.tscn` instantiates `MainBarWindow`, `SubmenuBookWindow`, and `ExploreProgressController`

## Top-Level Runtime Flow

1. Godot starts `scenes/PrototypeRoot.tscn`
2. `scripts/game/PrototypeRootController.cs` resolves autoload services from `/root/*`
3. `PrototypeRootController` subscribes to key signals such as:
   - `InputActivityState.ActivityTick`
   - `InputActivityState.InputBatchTick`
   - `ResourceWalletState.WalletChanged`
   - `PlayerProgressState.RealmProgressChanged`
4. `PrototypeRootController` defers `LoadAllState()` and begins save/upload coordination
5. `scripts/game/ExploreProgressController*.cs` handles explore/battle runtime tied to input batches
6. `scripts/services/ActivityConversionService.cs` converts AP to resources on a fixed interval

## Architectural Layers

### Scene Controllers

- `scripts/game/PrototypeRootController.cs` is the main scene-level coordinator
- `scripts/game/ExploreProgressController*.cs` owns exploration, battle runtime, track visuals, debug overlays, and runtime persistence payload
- `scripts/ui/MainBarLayoutController.cs` manages bottom bar positioning and compact layout behavior
- `scripts/ui/SubmenuWindowController.cs` manages submenu visibility
- `scripts/ui/BookTabsController.cs` owns book/settings tab state, settings persistence, and some runtime bridge actions

### Autoload Services

Autoloads are declared in `project.godot` and act like singleton services:

- `InputHookService` - low-level input capture
- `InputActivityState` - input aggregation and AP computation
- `InputPauseShortcut` - pause/resume hook control
- `BackpackState` - inventory state
- `ResourceWalletState` - currencies/resources
- `PlayerProgressState` - realm/mood progression
- `PlayerActionState` - dungeon vs cultivation mode
- `ActivityConversionService` - AP settlement
- `LevelConfigLoader` - config data, unlock progression, rewards, validation, simulation
- `CloudSaveSyncService` - Steam/cloud bridge

### Pure Rule Layer

Pure logic lives in `scripts/core/` and is used to keep gameplay calculations testable:

- `ExploreProgressionRule.cs`
- `BattleRoundRule.cs`
- `DropEconomyRule.cs`
- `LevelCycleRule.cs`
- `ActivitySettlementRule.cs`

These files have no Godot scene-tree dependency and are covered by xUnit tests in `tests/xiuxian2.Tests/`.

## Explore / Battle Flow

- `InputHookService` captures activity, mostly Windows-first
- `InputActivityState` aggregates raw input and emits `InputBatchTick`
- `ExploreProgressController.OnInputBatchTick` is the gameplay pivot point
- Dungeon mode path:
  - `AdvanceExploreByInput()`
  - `MoveMonsterQueueByInputs()`
  - `TryStartBattle()`
  - if in battle, `AdvanceBattleByInput()`
- Cultivation mode path:
  - exploration pauses
  - AP still feeds `ActivityConversionService`
  - input EXP can feed realm progression based on action mode and settings

## Persistence Architecture

- Unified save path is `user://save_state.cfg`
- Save orchestration is centralized in `scripts/game/PrototypeRootController.cs`
- Payload ownership is delegated to each state/service via methods like:
  - `ToDictionary()` / `FromDictionary()`
  - `ToRuntimeDictionary()` / `FromRuntimeDictionary()`
- Schema sections and key ownership are documented in `docs/SAVE_SYSTEM.md`
- Legacy fallback still exists for `user://ui_state.cfg` and `user://game_state.cfg`

## Partial-Class Decomposition

### `ExploreProgressController`

- `scripts/game/ExploreProgressController.cs` - lifecycle, node references, input routing, `_Process`
- `scripts/game/ExploreProgressController.Battle.cs` - battle state transitions and reward application
- `scripts/game/ExploreProgressController.DebugAndValidation.cs` - debug panel and validation overlay
- `scripts/game/ExploreProgressController.ProgressAndRuntime.cs` - progression, runtime save/load, recent battle logs, cultivation UI
- `scripts/game/ExploreProgressController.TrackVisuals.cs` - track visuals, markers, actor slots, HP labels

### `LevelConfigLoader`

- `scripts/services/LevelConfigLoader.cs` - config loading, parsing, indexing, active-level basics
- `scripts/services/LevelConfigLoader.DropAndRewards.cs` - drop rolling and rewards
- `scripts/services/LevelConfigLoader.RuntimeState.cs` - runtime state persistence and unlock state
- `scripts/services/LevelConfigLoader.ValidationAndSimulation.cs` - validation summaries and battle simulation

## Key Constraints In Architecture

- Exploration progress must remain driven by `InputActivityState.InputBatchTick`, per `AGENTS.md`
- AP is a resource-settlement signal, not a direct exploration-progress driver
- User-facing text should be centralized in `scripts/ui/UiText.cs`
- Save-key and node-path changes must be updated at both read/write or lookup call sites

## Main Architectural Pressure Points

- `scripts/game/PrototypeRootController.cs` still couples save orchestration, UI state, and cloud upload concerns
- `scripts/ui/BookTabsController.cs` remains a very large UI/state controller
- Runtime config still points at `docs/design/09_level_monster_drop_sample.json`, which mixes design artifact and runtime data concerns
