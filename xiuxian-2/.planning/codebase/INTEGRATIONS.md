# Integrations

## Godot Autoload Integration

- Core singleton-style services are registered under `[autoload]` in `project.godot`
- Registered integrations include:
  - `LevelConfigLoader`
  - `InputActivityState`
  - `InputHookService`
  - `InputPauseShortcut`
  - `BackpackState`
  - `ResourceWalletState`
  - `PlayerProgressState`
  - `PlayerActionState`
  - `ActivityConversionService`
  - `CloudSaveSyncService`
- This is the primary runtime integration surface between scenes and long-lived services

## Save System Integration

- Unified save path: `user://save_state.cfg`
- Save orchestration lives in `scripts/game/PrototypeRootController.cs`
- Schema and ownership are documented in `docs/SAVE_SYSTEM.md`
- `PrototypeRootController` writes top-level sections for UI, input, backpack, resources, progress, action mode, explore runtime, level runtime, and settings
- Legacy compatibility still exists for `user://ui_state.cfg` and `user://game_state.cfg`

## Cloud Save / Steam Bridge

- Cloud save service: `scripts/services/CloudSaveSyncService.cs`
- Design intent: Steam-first cloud save bridge with reflection-based optional integration
- Steam integration is not compiled as a direct dependency; instead, the service looks up `Steamworks.SteamRemoteStorage` via reflection
- Fallback path: `NoopSteamCloudBridge` inside `scripts/services/CloudSaveSyncService.cs`
- Upload path uses local `user://save_state.cfg` and remote `save_state.cfg`
- Runtime wiring and throttling are controlled from `scripts/game/PrototypeRootController.cs`
- User-facing setting exists in `scripts/ui/BookTabsController.cs` and `scripts/ui/UiText.cs`, but README still treats this as incremental, not final release-ready infrastructure

## Input Capture Integration

- Low-level hook service: `scripts/services/InputHookService.cs`
- Aggregation/state service: `scripts/services/InputActivityState.cs`
- Pause shortcut service: `scripts/services/InputPauseShortcut.cs`
- Main gameplay contract: `ExploreProgressController` consumes `InputActivityState.InputBatchTick`
- Project rule in `AGENTS.md`: exploration progress must remain input-driven and must not regress to local `_Input` counting
- Platform note: README marks global input hook behavior as Windows-first

## Resource And Progression Integration

- AP-to-resource conversion service: `scripts/services/ActivityConversionService.cs`
- Wallet target: `scripts/services/ResourceWalletState.cs`
- Progression target: `scripts/services/PlayerProgressState.cs`
- Action mode gate: `scripts/services/PlayerActionState.cs`
- Settlement rule extraction for tests exists in `scripts/core/ActivitySettlementRule.cs`

## Level / Monster / Drop Configuration Integration

- Config loader: `scripts/services/LevelConfigLoader.cs`
- Supporting partials:
  - `scripts/services/LevelConfigLoader.DropAndRewards.cs`
  - `scripts/services/LevelConfigLoader.RuntimeState.cs`
  - `scripts/services/LevelConfigLoader.ValidationAndSimulation.cs`
- Config source path currently points to `docs/design/09_level_monster_drop_sample.json` from `scripts/services/LevelConfigLoader.cs`
- Validation output is surfaced to developers via `scripts/game/ExploreProgressController.DebugAndValidation.cs`
- README explicitly treats the config validation panel as a developer tool rather than a player-facing feature

## Asset Pipeline Integration

- Raw/generated source assets live under `assets/origin/`
- Asset library metadata and mapping guidance are in `assets/origin/README.md`
- Supporting PowerShell scripts in `scripts/tools/` include:
  - `convert-jpg-alpha.ps1`
  - `normalize-origin-sprites.ps1`
  - `check-bom.ps1`
- Current asset pipeline is partly manual: `docs/design/10_todo.md` still tracks `[MANUAL-ASSET:*]` tasks for fonts, UI, characters, monsters, items, and effects

## Verification Tooling Integration

- `just verify` combines build and BOM checks, then prompts manual Godot verification in `justfile`
- `just verify-runtime` adds `scripts/tools/verify-runtime.ps1`
- `scripts/tools/verify-runtime.ps1` expects optional environment variable `GODOT_BIN` for headless launch checks
- Verification still depends on manual Godot editor/runtime checks, especially for scene parse safety and runtime restoration

## Documentation / Maintainer Integration

- `README.md` is the main maintainer-facing entry point
- `AGENTS.md` contains repository-specific engineering and gameplay constraints
- `docs/SAVE_SYSTEM.md` is the canonical save-schema reference
- `docs/design/10_todo.md` serves as the actionable backlog and records manual integration dependencies

## Current Integration Gaps

- Steam/cloud functionality is partially wired but not fully productized; README and UI text still mark it as in-progress
- Config runtime still depends on a design-doc JSON path rather than a dedicated runtime data directory
- Asset import remains partially manual and is not yet fully codified as an automated pipeline
- Automated tests cover pure logic but do not exercise Godot scene/runtime integrations end-to-end
