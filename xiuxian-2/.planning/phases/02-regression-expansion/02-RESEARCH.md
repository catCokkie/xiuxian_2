# Phase 2 Research: Regression Expansion

## Objective

Research what matters for Phase 2 planning when the immediate goal is to expand automated regression coverage around `PlayerProgressState` breakthrough-threshold behavior.

## Existing Implementation Findings

### Current Growth Logic

- Primary file: `scripts/services/PlayerProgressState.cs`
- Current breakthrough contract:
  - `RealmExpRequired => GetExpRequired(RealmLevel)`
  - `CanBreakthrough => RealmExp >= RealmExpRequired`
  - `TryBreakthrough()` returns `false` when `CanBreakthrough` is false
  - successful breakthrough subtracts `RealmExpRequired`, increments `RealmLevel`, then emits both `RealmLevelUp` and `RealmProgressChanged`
- `AddRealmExp()` has two modes:
  - normal accumulation when `AutoBreakthrough == false`
  - repeated `TryBreakthrough()` loop when `AutoBreakthrough == true`

### Existing Test Strategy Patterns

- Existing automated tests live in `tests/xiuxian2.Tests/`
- Current style is direct xUnit coverage of deterministic pure-logic behavior, for example:
  - `ActivitySettlementRuleTests.cs`
  - `DropEconomyRuleTests.cs`
- Tests currently avoid scene-tree setup when possible and prefer small, stable assertions around business rules

## Planning Implications

### What This Phase Should Probably Do

1. Add focused regression tests for breakthrough gating using the current `PlayerProgressState` contract
2. Cover the three locked scenarios from Phase 2 context:
   - not enough EXP → cannot break through
   - exactly enough EXP → can break through
   - after breakthrough → level/EXP state updates match contract

### What This Phase Should Probably Avoid

- Avoid broadening into mood multiplier coverage
- Avoid broadening into save/load restoration tests
- Avoid introducing scene-level Godot test harnesses just for this slice

## Risk Notes

- `PlayerProgressState` is a Godot `Node`, so tests need to stay lightweight and not assume full runtime scene composition
- `GD.Print` inside `TryBreakthrough()` is harmless for logic tests, but assertions should not depend on output logging
- `AutoBreakthrough` can make test scenarios ambiguous if reused carelessly; tests should set it explicitly per scenario

## Recommended Plan Shape

- One implementation/test plan focused on `PlayerProgressState` breakthrough-path coverage
- Optional second plan only if a tiny pure helper extraction becomes necessary to keep tests clean
- Verification should stay simple: `dotnet build` + `dotnet test`

## Canonical File Set For Planning

- `scripts/services/PlayerProgressState.cs`
- `tests/xiuxian2.Tests/ActivitySettlementRuleTests.cs`
- `tests/xiuxian2.Tests/DropEconomyRuleTests.cs`
- `.planning/phases/02-regression-expansion/02-CONTEXT.md`
- `.planning/REQUIREMENTS.md`
