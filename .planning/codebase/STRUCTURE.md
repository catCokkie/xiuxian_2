# Codebase Structure

**Analysis Date:** 2026-03-20

## Directory Layout

```text
[project-root]/
├── `.planning/codebase/`        # Generated architecture and structure docs for orchestration
└── `xiuxian-2/`                 # Actual Godot 4 + C# project root
    ├── `project.godot`          # Godot app config, main scene, and autoload registrations
    ├── `xiuxian2.csproj`        # .NET project definition for Godot C#
    ├── `scenes/`                # Main runtime scene and reusable UI/test scenes
    ├── `scripts/`               # C# gameplay, UI, service, and test scripts
    ├── `docs/design/`           # Design specs and level/drop JSON content
    ├── `assets/ui/`             # Imported UI art assets such as actor placeholders
    └── `.godot/`                # Godot editor/import metadata
```

## Directory Purposes

**`xiuxian-2/scenes/`:**
- Purpose: Store the scene tree definitions that Godot instantiates at runtime.
- Contains: `PrototypeRoot.tscn`, `ui/*.tscn`, and `tests/*.tscn`.
- Key files: `xiuxian-2/scenes/PrototypeRoot.tscn`, `xiuxian-2/scenes/ui/MainBarWindow.tscn`, `xiuxian-2/scenes/ui/SubmenuBookWindow.tscn`, `xiuxian-2/scenes/tests/InputSystemTest.tscn`

**`xiuxian-2/scripts/game/`:**
- Purpose: Hold scene-level gameplay coordinators.
- Contains: Root orchestration and exploration/battle runtime controllers.
- Key files: `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/scripts/game/ExploreProgressController.cs`

**`xiuxian-2/scripts/services/`:**
- Purpose: Hold autoload singletons and long-lived service/state nodes.
- Contains: Input, wallet, inventory, progression, config loading, cloud save, and shortcut services.
- Key files: `xiuxian-2/scripts/services/InputActivityState.cs`, `xiuxian-2/scripts/services/InputHookService.cs`, `xiuxian-2/scripts/services/ActivityConversionService.cs`, `xiuxian-2/scripts/services/LevelConfigLoader.cs`, `xiuxian-2/scripts/services/CloudSaveSyncService.cs`

**`xiuxian-2/scripts/ui/`:**
- Purpose: Hold UI behavior scripts for scene widgets and shared UI text.
- Contains: Window layout logic, tab/page behavior, animated submenu behavior, and string constants.
- Key files: `xiuxian-2/scripts/ui/MainBarLayoutController.cs`, `xiuxian-2/scripts/ui/SubmenuWindowController.cs`, `xiuxian-2/scripts/ui/BookTabsController.cs`, `xiuxian-2/scripts/ui/UiText.cs`

**`xiuxian-2/scripts/tests/`:**
- Purpose: Hold in-engine manual test scenes/scripts rather than standalone unit-test projects.
- Contains: Diagnostic controls for the input system.
- Key files: `xiuxian-2/scripts/tests/InputSystemTest.cs`

**`xiuxian-2/docs/design/`:**
- Purpose: Keep the design source of truth and sample content/config data.
- Contains: Numbered markdown design documents and the JSON level/drop sample consumed by runtime code.
- Key files: `xiuxian-2/docs/design/README.md`, `xiuxian-2/docs/design/09_level_monster_drop_sample.json`

**`xiuxian-2/assets/ui/`:**
- Purpose: Hold imported visual assets referenced by scene files.
- Contains: `actor_placeholder.svg` plus Godot import metadata.
- Key files: `xiuxian-2/assets/ui/actor_placeholder.svg`

## Key File Locations

**Entry Points:**
- `xiuxian-2/project.godot`: Declares the main scene and all autoload singletons.
- `xiuxian-2/scenes/PrototypeRoot.tscn`: Root scene that instantiates the playable UI tree.
- `xiuxian-2/scripts/game/PrototypeRootController.cs`: Root runtime controller attached to `PrototypeRoot`.

**Configuration:**
- `xiuxian-2/project.godot`: App bootstrapping and autoload registration.
- `xiuxian-2/xiuxian2.csproj`: Godot .NET SDK and target frameworks.
- `xiuxian-2/.editorconfig`: Encoding rules, including `utf-8-bom` for `.cs` and `utf-8` for `.tscn`.
- `xiuxian-2/docs/design/09_level_monster_drop_sample.json`: Runtime level/monster/drop configuration loaded by `LevelConfigLoader`.

**Core Logic:**
- `xiuxian-2/scripts/game/ExploreProgressController.cs`: Explore/battle loop, reward application, debug overlay, and validation panel.
- `xiuxian-2/scripts/services/InputActivityState.cs`: Input aggregation and AP calculation.
- `xiuxian-2/scripts/services/ActivityConversionService.cs`: AP-to-resource and AP-to-exp conversion.
- `xiuxian-2/scripts/services/LevelConfigLoader.cs`: Content indexing, validation, unlock flow, and drop simulation.

**Testing:**
- `xiuxian-2/scripts/tests/InputSystemTest.cs`: Manual runtime test harness for input hooks and AP counters.
- `xiuxian-2/scenes/tests/InputSystemTest.tscn`: Scene wrapper for the test harness.

## Naming Conventions

**Files:**
- Scene scripts use PascalCase controller/service names that mirror the main class name: `PrototypeRootController.cs`, `ExploreProgressController.cs`, `BookTabsController.cs`, `LevelConfigLoader.cs`.
- Godot scenes use PascalCase `.tscn` names that match the scene root role: `PrototypeRoot.tscn`, `MainBarWindow.tscn`, `SubmenuBookWindow.tscn`.
- Design docs use numeric prefixes to express reading order and topic grouping: `xiuxian-2/docs/design/00_vision.md`, `xiuxian-2/docs/design/09_level_monster_drop_sample.json`, `xiuxian-2/docs/design/10_todo.md`.

**Directories:**
- Runtime code is grouped by responsibility under `xiuxian-2/scripts/game/`, `xiuxian-2/scripts/ui/`, `xiuxian-2/scripts/services/`, and `xiuxian-2/scripts/tests/`.
- Scenes follow the same split with `xiuxian-2/scenes/ui/` and `xiuxian-2/scenes/tests/`; the root scene stays directly under `xiuxian-2/scenes/`.

## Where to Add New Code

**New Feature:**
- Primary code: Put scene-level orchestration in `xiuxian-2/scripts/game/` when the feature coordinates services and multiple UI nodes; put reusable state or background logic in `xiuxian-2/scripts/services/`.
- Tests: Add manual verification scenes under `xiuxian-2/scenes/tests/` and matching scripts under `xiuxian-2/scripts/tests/`.

**New Component/Module:**
- Implementation: Add visual hierarchy in `xiuxian-2/scenes/ui/` and attach behavior from `xiuxian-2/scripts/ui/` when the feature is primarily a widget or window.

**Utilities:**
- Shared helpers: Keep UI text/constants in `xiuxian-2/scripts/ui/UiText.cs`; keep gameplay/data helpers close to the owning service, following the current pattern in `xiuxian-2/scripts/services/LevelConfigLoader.cs`.

## Special Directories

**`xiuxian-2/.godot/`:**
- Purpose: Store Godot editor state and import/cache metadata.
- Generated: Yes.
- Committed: Yes in the current repository layout.

**`xiuxian-2/docs/design/`:**
- Purpose: Provide the design baseline that code follows; `README.md` explicitly says design changes should happen before code changes.
- Generated: No.
- Committed: Yes.

**`xiuxian-2/assets/ui/`:**
- Purpose: Hold source UI assets plus `.import` metadata consumed by scenes.
- Generated: Mixed; source art is authored, `.import` files are generated by Godot.
- Committed: Yes.

**`xiuxian-2/scripts/services/`:**
- Purpose: Central autoload/singleton service area; use this directory for any new global `Node` that should be registered in `xiuxian-2/project.godot`.
- Generated: No.
- Committed: Yes.

---

*Structure analysis: 2026-03-20*
