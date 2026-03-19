# Concerns

## Large / Cohesion-Heavy Files

- `scripts/ui/BookTabsController.cs` is still a very large UI/state controller with dynamic settings UI creation and persistence hooks
- `scripts/game/PrototypeRootController.cs` still combines save orchestration, cloud-sync coordination, and UI/service wiring
- Although `ExploreProgressController` and `LevelConfigLoader` were split into partial files, both domains remain conceptually dense and require careful cross-file reasoning

## Save / Config Coupling Risks

- Save orchestration is centralized in `scripts/game/PrototypeRootController.cs`, so key changes can silently break restore logic if write/read paths drift
- Runtime config still points at `docs/design/09_level_monster_drop_sample.json` from `scripts/services/LevelConfigLoader.cs`, which keeps design docs and runtime data coupled
- `docs/SAVE_SYSTEM.md` reduces this risk, but only if maintainers keep it updated when schema changes

## Manual Asset Pipeline Dependency

- `assets/origin/README.md` describes a source asset library and intended destination layout, but actual integration remains partly manual
- `docs/design/10_todo.md` still contains multiple `[MANUAL-ASSET:*]` tasks for fonts, UI, character, monster, item, and effect imports
- This means the repo is not yet self-contained for a fully automated visual build pipeline

## Verification Still Depends On Manual Godot Checks

- `just verify` explicitly relies on manual Godot/editor checks after build and BOM validation
- `scripts/tools/verify-runtime.ps1` helps, but it does not replace interactive scene verification
- UI, scene layout, and asset issues can still pass automated checks while failing in-editor or at runtime

## Warning-Heavy Codebase

- Recent `dotnet build` output still carries a large set of existing warnings, especially nullable-context warnings and one generated signal-hiding warning
- These warnings are not currently blocking, but they raise background noise and can obscure new regressions

## Platform Constraints

- Global input hook behavior is Windows-first per `README.md`
- `scripts/services/InputHookService.cs` includes Win32-specific integration points, so cross-platform behavior is incomplete by design

## Developer-Only Tooling Ambiguity

- Config validation is intentionally treated as a developer tool, not a player feature
- The implementation spans `scripts/game/ExploreProgressController.DebugAndValidation.cs`, `scripts/services/LevelConfigLoader.ValidationAndSimulation.cs`, and settings/UI persistence
- This is useful for maintenance, but it adds UI/runtime complexity that has to stay hidden from normal player experience

## Partial-Class Complexity

- Partial-class decomposition improved maintainability for:
  - `scripts/game/ExploreProgressController*.cs`
  - `scripts/services/LevelConfigLoader*.cs`
- But this also means behavior is distributed across multiple files, and future refactors must track cross-file state ownership carefully

## Cloud / Steam Incompleteness

- `scripts/services/CloudSaveSyncService.cs` is a reflection-based bridge rather than a finalized Steamworks integration layer
- `README.md` and backlog items both indicate Steam/cloud work remains incremental rather than fully productized
- Conflict-resolution UX and release-ready failure handling are still backlog items

## Documentation Drift Risk

- The repository now has better top-level docs (`README.md`, `docs/SAVE_SYSTEM.md`), but `docs/design/10_todo.md` shows an active, fast-moving backlog
- This creates ongoing risk that maintainer docs drift unless updated alongside code changes

## Best Next Risk-Reduction Moves

- Continue expanding pure-logic automated tests around progression and persistence-adjacent rules
- Gradually reduce warning noise so new build problems stand out
- Separate runtime data from `docs/design/` when practical
- Keep manual asset tasks explicitly marked and documented until a real import pipeline exists
