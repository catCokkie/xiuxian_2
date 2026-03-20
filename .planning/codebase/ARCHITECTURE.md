# Architecture

**Analysis Date:** 2026-03-20

## Pattern Overview

**Overall:** Scene-root orchestration over autoloaded service singletons.

**Key Characteristics:**
- The Godot entry scene `xiuxian-2/scenes/PrototypeRoot.tscn` composes the playable UI and attaches `PrototypeRootController` plus `ExploreProgressController` as the main runtime coordinators.
- Cross-scene state lives in autoloaded `Node` services declared in `xiuxian-2/project.godot`, and controllers resolve them with `/root/...` `NodePath` lookups instead of constructor injection.
- Most runtime communication uses Godot signals (`ActivityTick`, `InputBatchTick`, `WalletChanged`, `RealmProgressChanged`, `ConfigLoaded`, `ModeChanged`) rather than direct polling.

## Layers

**Boot and Scene Composition:**
- Purpose: Start the Godot app, register autoloads, and instantiate the main scene tree.
- Location: `xiuxian-2/project.godot`, `xiuxian-2/scenes/PrototypeRoot.tscn`
- Contains: `run/main_scene`, autoload registrations, scene instances for `MainBarWindow`, `SubmenuBookWindow`, and `ExploreProgressController`.
- Depends on: Godot scene loading and C# script bindings.
- Used by: The entire runtime.

**Scene Root Orchestration:**
- Purpose: Wire UI controllers to services, manage persistence, and translate book settings into runtime toggles.
- Location: `xiuxian-2/scripts/game/PrototypeRootController.cs`
- Contains: Service discovery, signal subscriptions, save/load orchestration, legacy migration, and cloud-sync triggers.
- Depends on: `MainBarLayoutController`, `SubmenuWindowController`, `BookTabsController`, `ExploreProgressController`, and most autoload services.
- Used by: `xiuxian-2/scenes/PrototypeRoot.tscn`.

**Gameplay Runtime:**
- Purpose: Turn input batches into exploration progress, combat rounds, level completion, rewards, and validation/debug overlays.
- Location: `xiuxian-2/scripts/game/ExploreProgressController.cs`
- Contains: Explore loop state, battle state, marker visuals, reward application, validation display, and action-mode/level option sync.
- Depends on: `InputActivityState`, `BackpackState`, `PlayerProgressState`, `ResourceWalletState`, `LevelConfigLoader`, `PlayerActionState`, and UI nodes under `MainBarWindow`.
- Used by: `PrototypeRootController` persistence and the main scene tree.

**UI Control Layer:**
- Purpose: Own widget behavior, layout, submenu animation, tab switching, and settings UI generation.
- Location: `xiuxian-2/scripts/ui/MainBarLayoutController.cs`, `xiuxian-2/scripts/ui/SubmenuWindowController.cs`, `xiuxian-2/scripts/ui/BookTabsController.cs`, `xiuxian-2/scripts/ui/UiText.cs`, `xiuxian-2/scripts/ui/PauseToggleButton.cs`
- Contains: Drag/resize behavior, open/close tweening, dynamic tab content, settings controls, and centralized UI strings.
- Depends on: Godot `Control` nodes plus autoload state services for live data.
- Used by: `PrototypeRootController` and `ExploreProgressController`.

**Autoload Service Layer:**
- Purpose: Hold long-lived state and background behaviors that should survive scene boundaries.
- Location: `xiuxian-2/scripts/services/*.cs`, registered in `xiuxian-2/project.godot`
- Contains: Input aggregation, global input hook integration, AP-to-resource conversion, progression state, wallet state, inventory state, action mode, level config loading, pause shortcut handling, and cloud save bridge.
- Depends on: Godot `Node` lifecycle, `/root` autoload registration, JSON/config files, and platform APIs.
- Used by: Both gameplay and UI controllers.

**Content and Design Data:**
- Purpose: Define level, monster, drop, and systems content outside code.
- Location: `xiuxian-2/docs/design/09_level_monster_drop_sample.json`, `xiuxian-2/docs/design/*.md`
- Contains: Configurable exploration, combat, unlock, drop-table, and design-reference data.
- Depends on: `LevelConfigLoader` parsing and validation.
- Used by: `LevelConfigLoader` and, transitively, `ExploreProgressController`.

## Data Flow

**Input to Progression and Rewards:**

1. `InputHookService` in `xiuxian-2/scripts/services/InputHookService.cs` captures global Windows input or in-app fallback input and forwards normalized counters into `InputActivityState`.
2. `InputActivityState` in `xiuxian-2/scripts/services/InputActivityState.cs` batches raw input, applies decay and soft-cap rules, then emits `ActivityTick` and `InputBatchTick`.
3. `ActivityConversionService` in `xiuxian-2/scripts/services/ActivityConversionService.cs` consumes activity signals to add currencies in `ResourceWalletState` and experience in `PlayerProgressState`.
4. `ExploreProgressController` in `xiuxian-2/scripts/game/ExploreProgressController.cs` consumes `InputBatchTick` to advance exploration, trigger battles, roll drops through `LevelConfigLoader`, and write item rewards into `BackpackState`.
5. UI controllers in `xiuxian-2/scripts/ui/BookTabsController.cs` and `xiuxian-2/scripts/game/ExploreProgressController.cs` react to wallet/progress/activity changes and redraw labels, bars, and settings panels.

**Settings to Runtime Behavior:**

1. `BookTabsController` builds and owns the in-book settings dictionary in `xiuxian-2/scripts/ui/BookTabsController.cs`.
2. `PrototypeRootController` listens to `ActiveTabsChanged` in `xiuxian-2/scripts/game/PrototypeRootController.cs` and re-reads the settings dictionary.
3. `PrototypeRootController` pushes setting effects into runtime flags such as cloud sync, autosave cadence, validation panel visibility, and debug overlay visibility.
4. `BookTabsController` applies direct display settings immediately through `DisplayServer` and `Engine` APIs.

**Persistence and Resume:**

1. `PrototypeRootController` marks the session dirty from layout, tab, activity, wallet, and progression signals in `xiuxian-2/scripts/game/PrototypeRootController.cs`.
2. On cooldown expiry or window close, it serializes UI state, service state, level runtime state, and explore runtime state into `user://save_state.cfg`.
3. On startup, it loads the unified save, falls back to legacy save files if missing, restores state dictionaries, and optionally rehydrates from `CloudSaveSyncService`.
4. `LevelConfigLoader` and `ExploreProgressController` restore their runtime dictionaries so unlock state, wave position, battle state, and marker placement continue from the last session.

**State Management:**
- Mutable game state is centralized in autoload services such as `InputActivityState`, `BackpackState`, `ResourceWalletState`, `PlayerProgressState`, `PlayerActionState`, and `LevelConfigLoader`.
- Scene-local UI state stays inside controllers like `MainBarLayoutController` and `SubmenuWindowController` until `PrototypeRootController` persists it.
- Runtime snapshots use Godot `Dictionary<string, Variant>` payloads via `ToDictionary`/`FromDictionary` and `ToRuntimeDictionary`/`FromRuntimeDictionary` methods.

## Key Abstractions

**Autoload State Service:**
- Purpose: Expose global mutable state as a Godot `Node` reachable from `/root`.
- Examples: `xiuxian-2/scripts/services/InputActivityState.cs`, `xiuxian-2/scripts/services/BackpackState.cs`, `xiuxian-2/scripts/services/ResourceWalletState.cs`, `xiuxian-2/scripts/services/PlayerProgressState.cs`, `xiuxian-2/scripts/services/PlayerActionState.cs`
- Pattern: Thin service nodes with properties, mutator methods, signal emission, and dictionary serialization.

**Runtime Coordinator Controller:**
- Purpose: Bridge scene nodes and services for a large interactive feature.
- Examples: `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/scripts/game/ExploreProgressController.cs`
- Pattern: Godot `Control` or `Node` scripts that resolve dependencies in `_Ready()`, subscribe to signals, own transient state, and update many child nodes.

**Config-Driven Content Loader:**
- Purpose: Convert design JSON into indexed runtime lookup and unlock/reward logic.
- Examples: `xiuxian-2/scripts/services/LevelConfigLoader.cs`, `xiuxian-2/docs/design/09_level_monster_drop_sample.json`
- Pattern: Load on `_Ready()`, build dictionaries/lists, expose query methods, and keep runtime progress in a serializable dictionary.

**UI-as-Scene with Scripted Behavior:**
- Purpose: Keep visual hierarchy in `.tscn` files while scripts control behavior and runtime-generated subtrees.
- Examples: `xiuxian-2/scenes/ui/MainBarWindow.tscn`, `xiuxian-2/scenes/ui/SubmenuBookWindow.tscn`, `xiuxian-2/scripts/ui/BookTabsController.cs`
- Pattern: Scene defines base nodes; script uses `GetNode` and `NodePath` exports to bind widgets and may append extra controls dynamically.

## Entry Points

**Application Entry:**
- Location: `xiuxian-2/project.godot`
- Triggers: Godot launch.
- Responsibilities: Set `run/main_scene` to `res://scenes/PrototypeRoot.tscn` and register service autoloads.

**Main Scene Root:**
- Location: `xiuxian-2/scenes/PrototypeRoot.tscn`
- Triggers: Loaded as `run/main_scene`.
- Responsibilities: Instantiate `MainBarWindow`, `SubmenuBookWindow`, and `ExploreProgressController` under the `PrototypeRoot` control.

**Scene Root Controller:**
- Location: `xiuxian-2/scripts/game/PrototypeRootController.cs`
- Triggers: `_Ready()`, `_Process()`, `_Notification(NotificationWMCloseRequest)`.
- Responsibilities: Resolve services and child controllers, subscribe to signals, load state, debounce save operations, and coordinate runtime settings.

**Autoload Services:**
- Location: `xiuxian-2/project.godot`, `xiuxian-2/scripts/services/*.cs`
- Triggers: Godot autoload initialization.
- Responsibilities: Start background processing such as input capture, config loading, conversion loops, and pause shortcut handling.

## Error Handling

**Strategy:** Guard-and-log with graceful degradation.

**Patterns:**
- Missing dependencies are usually handled with `GetNodeOrNull(...)` plus `GD.PushWarning(...)` instead of hard failure, as in `xiuxian-2/scripts/game/PrototypeRootController.cs` and `xiuxian-2/scripts/services/ActivityConversionService.cs`.
- Service/bootstrap failures return early from `_Ready()` or helper methods and keep the rest of the runtime alive, as in `xiuxian-2/scripts/services/InputHookService.cs` and `xiuxian-2/scripts/services/LevelConfigLoader.cs`.
- Persistence and platform integrations emit warnings and continue locally when cloud or hook features are unavailable, as in `xiuxian-2/scripts/services/CloudSaveSyncService.cs` and `xiuxian-2/scripts/services/InputHookService.cs`.

## Cross-Cutting Concerns

**Logging:** `GD.Print`, `GD.PushWarning`, and `GD.PushError` are used directly across controllers and services, especially in `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/scripts/services/InputHookService.cs`, `xiuxian-2/scripts/services/LevelConfigLoader.cs`, and `xiuxian-2/scripts/services/CloudSaveSyncService.cs`.
**Validation:** `LevelConfigLoader` validates design JSON and exposes summarized issues that `ExploreProgressController` renders inside `MainBarWindow`; see `xiuxian-2/scripts/services/LevelConfigLoader.cs` and `xiuxian-2/scripts/game/ExploreProgressController.cs`.
**Authentication:** Not applicable inside the current gameplay runtime; the only external bridge is optional Steam cloud reflection in `xiuxian-2/scripts/services/CloudSaveSyncService.cs`.

---

*Architecture analysis: 2026-03-20*
