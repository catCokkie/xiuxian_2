# Testing

## Current Test Strategy

- The repository now has a minimal automated regression suite for pure logic
- Manual Godot/editor/runtime verification remains required for scene/UI/runtime integration
- Verification commands are centralized in `justfile`

## Automated Tests

### Test Project

- Project: `tests/xiuxian2.Tests/xiuxian2.Tests.csproj`
- Framework: xUnit
- Runner support: `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`
- Coverage collector: `coverlet.collector`

### Current Automated Coverage

- `tests/xiuxian2.Tests/ExploreProgressionRuleTests.cs`
  - input-driven progress completion
- `tests/xiuxian2.Tests/LevelCycleRuleTests.cs`
  - unlocked level cycling
- `tests/xiuxian2.Tests/BattleRoundRuleTests.cs`
  - battle round resolution
- `tests/xiuxian2.Tests/DropEconomyRuleTests.cs`
  - pity trigger and daily-cap behavior
- `tests/xiuxian2.Tests/ActivitySettlementRuleTests.cs`
  - AP settlement and cultivation-mode gating

### Test Command

- `dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj`
- Or via `just test`

## Manual / In-Project Tests

- `scripts/tests/InputSystemTest.cs` is a manual in-project test utility, not a formal automated suite
- `scenes/tests/InputSystemTest.tscn` is the corresponding manual scene
- `README.md` and `AGENTS.md` still describe Godot runtime verification as part of normal maintenance

## Verification Commands

- `just build` - main project build
- `just test` - automated pure-logic regression suite
- `just check-bom` - scene/resource encoding gate
- `just verify` - build + BOM checks + manual Godot checklist
- `just verify-runtime` - build + BOM checks + `scripts/tools/verify-runtime.ps1`

## Runtime Verification Expectations

Typical manual checks still include:

- open `res://scenes/PrototypeRoot.tscn` with no parse errors
- verify recent battle log refreshes after combat
- verify explore runtime and recent battle logs restore after reload

## What Is Covered Well

- Pure rules in `scripts/core/`
- Minimum regression safety around progression, level cycling, battle math, drop economy, and AP settlement
- Build/test commands are now repeatable from repository root

## Current Gaps

- No automated Godot scene-tree integration tests
- No UI interaction/E2E automation for Godot UI
- No automated save/load roundtrip tests for `user://save_state.cfg`
- No automated tests for `PrototypeRootController` orchestration or `BookTabsController` settings persistence
- No automated verification for asset import pipeline or BOM-sensitive scene opening

## Testing Design Direction

- Pure logic should continue to be extracted into `scripts/core/` when practical
- Rule classes should remain deterministic and easy to test without Godot scene dependencies
- Heavier runtime or UI verification should stay manual unless a stable Godot-specific harness is introduced later

## Known Test Warnings

- The test project may emit a Godot source-generator warning about `GodotProjectDir` being empty
- Current evidence shows this warning does not block successful build or test execution
