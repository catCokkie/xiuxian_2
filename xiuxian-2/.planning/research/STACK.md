# Stack Research

## Context

This is a brownfield maintenance milestone for an existing Godot 4.5 + C# desktop application. The goal is not to replace the stack, but to identify which parts of the current stack should remain stable, which supporting tools are worth strengthening, and which changes should be avoided during a maintenance pass.

## Recommended Stack Strategy

### Keep As-Is

- **Godot 4.5 + C# / .NET 8**
  - Evidence: `project.godot`, `xiuxian2.csproj`
  - Why: The runtime, scenes, autoloads, and current build flow are already aligned with this stack. Replatforming or changing engine/runtime is incompatible with a maintenance-first milestone.
  - Confidence: High

- **Godot autoload service model**
  - Evidence: `[autoload]` section in `project.godot`
  - Why: Current runtime architecture depends on long-lived state/services like `InputActivityState`, `LevelConfigLoader`, `PlayerProgressState`, and `CloudSaveSyncService`. This is the project's established integration surface.
  - Confidence: High

- **`just` + `dotnet` + PowerShell verification scripts**
  - Evidence: `justfile`, `scripts/tools/check-bom.ps1`, `scripts/tools/verify-runtime.ps1`
  - Why: The repo already has a coherent command-line workflow for build/test/verify. This is enough for a maintenance cycle and should be treated as the stable operator interface.
  - Confidence: High

### Strengthen During This Milestone

- **Pure-logic extraction into `scripts/core/`**
  - Evidence: `scripts/core/ExploreProgressionRule.cs`, `scripts/core/BattleRoundRule.cs`, `scripts/core/DropEconomyRule.cs`, `scripts/core/LevelCycleRule.cs`, `scripts/core/ActivitySettlementRule.cs`
  - Why: This is the cleanest way to expand automated regression safety without dragging the whole Godot scene tree into tests.
  - Confidence: High

- **xUnit test suite under `tests/xiuxian2.Tests/`**
  - Evidence: `tests/xiuxian2.Tests/xiuxian2.Tests.csproj`, `just test`
  - Why: The current automated suite is already the right test layer for maintenance. Expansion should continue here before attempting scene-driven automation.
  - Confidence: High

- **Repository-first documentation**
  - Evidence: `README.md`, `docs/SAVE_SYSTEM.md`, `.planning/codebase/*.md`
  - Why: Brownfield maintenance becomes safer when rules, save schema, and codebase maps stay current. Documentation has already become part of this repo's operating model.
  - Confidence: High

## Useful Supporting Tools To Keep Using

- `dotnet build xiuxian2.sln` for full solution validation
- `dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj` for regression checks
- `just verify` for build + BOM + manual Godot checklist
- `just verify-runtime` for stronger runtime validation
- `scripts/tools/check-bom.ps1` as a non-negotiable scene/resource guard

## What Not To Introduce In This Maintenance Pass

- **No stack migration**
  - Do not switch away from Godot C# or introduce a parallel UI framework.
  - Why: It destroys the scope discipline of a stabilization milestone.

- **No full asset pipeline rewrite**
  - Evidence: `assets/origin/README.md`, `docs/design/10_todo.md`
  - Why: Asset work is still partly manual and should remain bounded to obvious display-impacting issues.

- **No heavy Godot integration test harness yet**
  - Why: The project now benefits more from expanding pure-logic coverage and manual editor/runtime checks than from prematurely introducing a fragile scene-test framework.

- **No release-grade Steamworks productization in this pass**
  - Evidence: `scripts/services/CloudSaveSyncService.cs`, backlog items in `docs/design/10_todo.md`
  - Why: The cloud bridge is intentionally incremental and not the primary maintenance target for this round.

## Recommended Tooling Priorities

1. Expand `tests/xiuxian2.Tests/`
2. Keep extracting small pure rules from runtime-heavy classes
3. Preserve `just` workflows as the shared operator interface
4. Keep `.planning/`, `README.md`, and save docs aligned with code changes

## Decision Summary

- Current stack is already suitable for a maintenance milestone
- The biggest stack-level opportunity is not new tech, but better isolation of logic for testability
- The biggest stack-level risk is introducing too much new infrastructure while core UI/runtime issues are still unresolved
