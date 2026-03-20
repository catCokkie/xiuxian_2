# Codebase Concerns

**Analysis Date:** 2026-03-20

## Tech Debt

**Monolithic config/runtime service:**
- Issue: `xiuxian-2/scripts/services/LevelConfigLoader.cs` is a 2255-line god object that handles JSON IO, config indexing, validation, progression unlocks, pity counters, reward rolls, simulation output, and runtime persistence in one autoload.
- Files: `xiuxian-2/scripts/services/LevelConfigLoader.cs`, `xiuxian-2/docs/design/09_level_monster_drop_sample.json`, `xiuxian-2/project.godot`
- Impact: config bugs, reward bugs, save/load bugs, and validation UI bugs all converge in one class, so changes are hard to isolate and regressions are easy to introduce.
- Fix approach: split `LevelConfigLoader` into focused services for config parsing, validation, runtime progression state, and reward/drop resolution; keep the autoload as a thin facade only.

**Scene controller owns too many responsibilities:**
- Issue: `xiuxian-2/scripts/game/ExploreProgressController.cs` is a 1796-line controller that mixes scene lookup, debug UI, validation UI, battle simulation, reward application, persistence, and runtime resource loading.
- Files: `xiuxian-2/scripts/game/ExploreProgressController.cs`, `xiuxian-2/scenes/PrototypeRoot.tscn`, `xiuxian-2/scenes/ui/MainBarWindow.tscn`
- Impact: UI work, combat tuning, and persistence changes all touch the same file, making scene behavior fragile and hard to verify.
- Fix approach: extract battle state, marker/visual state, debug/validation presentation, and save/load DTO mapping into separate classes or child nodes.

**Settings/content/UI are coupled in one control:**
- Issue: `xiuxian-2/scripts/ui/BookTabsController.cs` is an 855-line class that builds settings UI in code, stores settings state, applies runtime window changes, and renders gameplay tab content.
- Files: `xiuxian-2/scripts/ui/BookTabsController.cs`, `xiuxian-2/scenes/ui/SubmenuBookWindow.tscn`, `xiuxian-2/scripts/ui/UiText.cs`
- Impact: small settings changes can break tab rendering or persistence, and most UI behavior is only discoverable by reading a single large file.
- Fix approach: separate settings data/model, settings view construction, and gameplay tab rendering into independent modules.

**Hard-coded scene and autoload wiring:**
- Issue: controllers depend on many literal node paths and autoload names such as `/root/InputActivityState`, `/root/LevelConfigLoader`, and `../MainBarWindow/Chrome/...`.
- Files: `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/scripts/game/ExploreProgressController.cs`, `xiuxian-2/scripts/services/InputPauseShortcut.cs`, `xiuxian-2/project.godot`
- Impact: scene renames or hierarchy edits produce `Node not found` failures at runtime instead of compile-time errors.
- Fix approach: centralize path constants, prefer exported node references for scene-local dependencies, and add startup validation that fails loudly when required nodes are missing.

## Known Bugs

**Main bar scene ships with mojibake/garbled labels:**
- Symptoms: multiple `text = ...` values in `xiuxian-2/scenes/ui/MainBarWindow.tscn` are unreadable gibberish instead of user-facing labels.
- Files: `xiuxian-2/scenes/ui/MainBarWindow.tscn`, `xiuxian-2/scripts/ui/MainBarLayoutController.cs`, `xiuxian-2/scripts/ui/UiText.cs`
- Trigger: opening the main scene or loading `res://scenes/ui/MainBarWindow.tscn` shows corrupted button/label defaults before runtime overrides apply.
- Workaround: rely on runtime assignments from `UiText` where available; scene-authored fallback text remains broken until the scene text is normalized.

**Pause shortcut is not actually global:**
- Symptoms: `InputPauseShortcut` documents `Ctrl + Shift + X` as a global shortcut, but it only listens through `_Input`, so it depends on Godot focus instead of the Windows hook layer.
- Files: `xiuxian-2/scripts/services/InputPauseShortcut.cs`, `xiuxian-2/project.godot`
- Trigger: pressing the shortcut while the app is unfocused does not guarantee pause/resume behavior.
- Workaround: toggle pause from focused in-app input or call `InputHookService.TogglePause()` through another UI entry point.

**Resource loading failures degrade silently to placeholders:**
- Symptoms: enemy portrait loads fail without an explicit warning, leaving the default slot texture or a stale visual while gameplay continues.
- Files: `xiuxian-2/scripts/game/ExploreProgressController.cs:1143`, `xiuxian-2/scripts/services/LevelConfigLoader.cs`, `xiuxian-2/docs/design/09_level_monster_drop_sample.json`
- Trigger: invalid `portrait` paths, missing `.import` metadata, or broken asset references in config data.
- Workaround: keep portrait paths aligned with imported assets such as `xiuxian-2/assets/ui/actor_placeholder.svg`; missing resources currently fail soft instead of surfacing actionable diagnostics.

**Scene text/encoding regressions have already caused load failures:**
- Symptoms: the project backlog documents `Parse Error: Expected '['`, `Failed loading resource`, and `Node not found` cascades when `.tscn` files are saved with BOM or malformed string lines.
- Files: `xiuxian-2/docs/design/10_todo.md`, `xiuxian-2/.editorconfig`, `xiuxian-2/scenes/ui/MainBarWindow.tscn`, `xiuxian-2/scenes/ui/SubmenuBookWindow.tscn`
- Trigger: editing scene files outside the Godot-safe encoding rules in `xiuxian-2/.editorconfig`.
- Workaround: keep `*.tscn` as UTF-8 without BOM and perform a scene smoke test after manual text edits.

## Security Considerations

**Plain-text save data is easy to tamper with:**
- Risk: gameplay state is stored in `user://save_state.cfg` via `ConfigFile` with no integrity check, signature, or replay protection.
- Files: `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/docs/design/02_systems.md`
- Current mitigation: schema versioning and bounded value reads reduce some crash risk, but they do not prevent manual editing.
- Recommendations: add save integrity metadata, suspicious-state detection, and explicit fallback behavior for impossible values.

**Anti-abuse design is only partially implemented:**
- Risk: design docs require time-jump detection, no-focus high-input downgrades, and conservative settlement, but current code only applies rolling decay and per-minute soft caps.
- Files: `xiuxian-2/scripts/services/InputActivityState.cs`, `xiuxian-2/scripts/services/ActivityConversionService.cs`, `xiuxian-2/docs/design/02_systems.md`, `xiuxian-2/docs/design/10_todo.md`
- Current mitigation: `InputActivityState` applies decay and soft-cap multipliers before AP settlement.
- Recommendations: add wall-clock jump detection, foreground/background awareness, and suspicious-session tagging before release.

**System-wide input capture needs stronger product safeguards:**
- Risk: `InputHookService` installs Windows low-level keyboard and mouse hooks for all desktop input; the code avoids storing key values, but the app still captures global activity with limited user-facing consent/telemetry controls.
- Files: `xiuxian-2/scripts/services/InputHookService.cs`, `xiuxian-2/scripts/services/InputPauseShortcut.cs`, `xiuxian-2/docs/INPUT_SYSTEM.md`
- Current mitigation: comments and implementation only count events, scroll steps, and pointer distance; non-Windows platforms fall back to in-app input.
- Recommendations: add explicit opt-in UX, visible capture state, and a documented privacy boundary before external release.

## Performance Bottlenecks

**Frequent save churn on active sessions:**
- Problem: input batches, wallet changes, realm progress changes, and layout changes all mark the save dirty; `PrototypeRootController` then writes the unified save file every 0.5 seconds when activity continues.
- Files: `xiuxian-2/scripts/game/PrototypeRootController.cs`
- Cause: persistence is event-driven at a very coarse granularity and uses a single hot save path for UI, progression, explore runtime, and config runtime.
- Improvement path: debounce saves by subsystem, batch only changed sections, and avoid writing on every high-frequency input tick.

**Cloud sync can amplify save IO:**
- Problem: every successful local save can immediately call `TryUploadLocal`, which reads the full save file and attempts a cloud write when cloud sync is enabled.
- Files: `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/scripts/services/CloudSaveSyncService.cs`
- Cause: local persistence and remote sync share the same hot path with no backoff, diffing, or conflict strategy.
- Improvement path: separate cloud sync scheduling from local save writes and add retry/backoff plus conflict handling.

**Portrait loading is uncached runtime work:**
- Problem: `ExploreProgressController` calls `GD.Load<Texture2D>(portraitPath)` during enemy visual changes.
- Files: `xiuxian-2/scripts/game/ExploreProgressController.cs:1182`, `xiuxian-2/docs/design/09_level_monster_drop_sample.json`
- Cause: visual config lookup and asset loading happen on demand instead of through a cached portrait registry.
- Improvement path: preload or cache textures by portrait path and log missing-resource failures once.

## Fragile Areas

**Autoload startup order and missing-node tolerance:**
- Files: `xiuxian-2/project.godot`, `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/scripts/services/ActivityConversionService.cs`, `xiuxian-2/scripts/services/InputHookService.cs`
- Why fragile: many systems continue after `GetNodeOrNull` failures and only emit warnings, so the app can enter partially initialized states that are harder to debug than a fast fail.
- Safe modification: keep autoload additions/removals synchronized with `project.godot` and add an integration smoke test that validates required singletons on boot.
- Test coverage: no automated startup smoke test is present.

**Save/load DTO compatibility across controllers:**
- Files: `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/scripts/services/LevelConfigLoader.cs:958`, `xiuxian-2/scripts/game/ExploreProgressController.cs:1297`
- Why fragile: runtime dictionaries are handwritten on both write and read paths, so schema drift can silently drop fields or restore inconsistent battle/config state.
- Safe modification: version each subsystem payload independently and add round-trip tests for `ToDictionary`/`FromDictionary` and runtime dictionary methods.
- Test coverage: no automated serialization tests are present.

**Scene hierarchy-dependent UI behavior:**
- Files: `xiuxian-2/scenes/ui/MainBarWindow.tscn`, `xiuxian-2/scenes/ui/SubmenuBookWindow.tscn`, `xiuxian-2/scripts/ui/MainBarLayoutController.cs`, `xiuxian-2/scripts/ui/SubmenuWindowController.cs`
- Why fragile: drag/resize logic and dynamic node discovery depend on exact node names and fixed control positions.
- Safe modification: change scene trees and node names only together with controller path updates, then open the main scene and interactively verify drag, resize, submenu open/close, and battle track rendering.
- Test coverage: no automated UI regression coverage is present.

## Scaling Limits

**Battle track is capped by hard-coded marker slots:**
- Current capacity: `ExploreProgressController` searches for `MonsterMarker01`..`MonsterMarker08` and `MonsterSlot01`..`MonsterSlot08`, while `MainBarWindow.tscn` currently defines only four visible marker/slot pairs.
- Limit: larger enemy waves require both scene edits and controller assumptions to change together.
- Scaling path: move battle track layout to a generated container so marker count comes from data instead of scene naming conventions.

**Monster visuals are hard-coded beyond config data:**
- Current capacity: marker text and tint fall back to switch statements for only `monster_slime_moss`, `monster_bat_shadow`, and `monster_spider_cave`.
- Limit: adding monsters without updating `ApplyMarkerVisual` and `GetMarkerTint` gives generic `MO` markers and shared fallback visuals.
- Scaling path: move marker glyph/tint metadata into config and resolve visuals through a data-driven registry.

## Dependencies at Risk

**Steam cloud integration is reflective and optional-only:**
- Risk: `CloudSaveSyncService` searches loaded assemblies for `Steamworks.SteamRemoteStorage` by reflection instead of compiling against a stable SDK contract.
- Impact: Steam integration can silently disappear or partially fail when assembly names or method signatures change.
- Migration plan: wrap a concrete Steamworks package behind an internal interface and add an explicit unavailable/error state in settings and startup logs.

**Godot scene text pipeline is tooling-sensitive:**
- Risk: the project depends on raw `.tscn` text files staying UTF-8 without BOM, and backlog notes show this has already broken scene loading.
- Impact: external editors or incorrect encoding defaults can break the main scene before gameplay starts.
- Migration plan: add a repository check that rejects BOM in `*.tscn` and run a scene-open smoke test in CI once CI exists.

## Missing Critical Features

**Cloud save conflict resolution and release-readiness:**
- Problem: design backlog still lists Steamworks preparation, cloud conflict policy, and failure fallback as open items, while the current settings UI advertises cloud sync with a dev-only hint.
- Blocks: reliable multi-device save sync and safe release of the cloud toggle.

**Runtime-facing config validation UX:**
- Problem: `LevelConfigLoader` already computes validation entries, but the backlog still calls out a dedicated validation panel as unfinished.
- Blocks: fast diagnosis of bad level/monster/drop data by non-programmer content authors.

**Automated regression suite for the main loop:**
- Problem: backlog explicitly requests a minimum regression set, but the repo only includes a manual `InputSystemTest` scene.
- Blocks: safe refactors of input collection, encounter flow, battle settlement, pity logic, and save/load behavior.

## Test Coverage Gaps

**Core gameplay loop is effectively untested:**
- What's not tested: input batching, AP decay, battle advancement, level unlock flow, drop tables, pity counters, and persistence restore paths.
- Files: `xiuxian-2/scripts/services/InputActivityState.cs`, `xiuxian-2/scripts/services/LevelConfigLoader.cs`, `xiuxian-2/scripts/game/ExploreProgressController.cs`, `xiuxian-2/scripts/game/PrototypeRootController.cs`
- Risk: regressions surface only in manual playtesting and are hard to localize once stateful bugs appear.
- Priority: High

**Input coverage is manual-only:**
- What's not tested: the only test artifact is a manual scene harness, not an automated test runner.
- Files: `xiuxian-2/scripts/tests/InputSystemTest.cs`, `xiuxian-2/scenes/tests/InputSystemTest.tscn`
- Risk: platform-specific hook regressions and pause/resume behavior can ship unnoticed.
- Priority: High

**Settings and UI state restoration are unverified:**
- What's not tested: settings toggles, resolution/UI scale application, submenu visibility restore, and layout persistence.
- Files: `xiuxian-2/scripts/ui/BookTabsController.cs`, `xiuxian-2/scripts/ui/MainBarLayoutController.cs`, `xiuxian-2/scripts/ui/SubmenuWindowController.cs`, `xiuxian-2/scripts/game/PrototypeRootController.cs`
- Risk: scene/UI regressions are likely during layout or UX cleanup work.
- Priority: Medium

**Observed baseline:**
- `dotnet build xiuxian-2/xiuxian2.sln` currently succeeds with 0 warnings and 0 errors from the CLI environment, so the dominant concerns are runtime behavior, architecture, and release hardening rather than compiler diagnostics.

---

*Concerns audit: 2026-03-20*
