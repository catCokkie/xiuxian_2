# Phase 2: Regression Expansion - Context

**Gathered:** 2026-03-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Strengthen automated regression protection around the logic most likely to be touched during maintenance.

For this phase, the immediate focus is expanding the existing pure-logic test suite rather than introducing a new test framework or scene-level integration harness.

</domain>

<decisions>
## Implementation Decisions

### Coverage priority
- Phase 2 should prioritize `PlayerProgressState` before other remaining areas such as save-schema roundtrip, `LevelConfigLoader` validation/reporting, or additional input/AP edge cases.
- Within `PlayerProgressState`, the highest-priority regression risk is incorrect breakthrough gating.

### Scope boundary for this pass
- This pass should focus on breakthrough-threshold behavior only.
- Do not expand this pass into mood multiplier coverage.
- Do not expand this pass into save/load restoration coverage for player progression state.

### Test style
- Use key-path coverage first, not exhaustive edge enumeration.
- The minimum target scenarios are:
  - not enough realm EXP means breakthrough is not allowed
  - exactly enough realm EXP means breakthrough is allowed
  - after breakthrough, the state updates/reset behavior matches the current design contract

### OpenCode's Discretion
- Exact test file layout inside `tests/xiuxian2.Tests/`
- Whether one or multiple test methods are the cleanest expression of the three breakthrough-path scenarios
- Whether tiny pure-rule extraction is needed before tests can be added safely

</decisions>

<specifics>
## Specific Ideas

- Keep following the current maintenance pattern: pure logic first, scene/runtime integration later.
- Avoid expanding this phase into a broader "test everything around progression" effort.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `tests/xiuxian2.Tests/` already contains xUnit-based pure-logic regression tests and is the correct home for this phase's new tests
- `scripts/core/` already holds extracted pure rules for other maintenance-sensitive domains and establishes the testability direction for new work

### Established Patterns
- Current regression strategy prefers deterministic pure-logic coverage rather than Godot scene-tree tests
- Maintenance-sensitive rules have been isolated and tested one domain at a time (`ExploreProgressionRule`, `BattleRoundRule`, `DropEconomyRule`, `ActivitySettlementRule`)
- The repository already treats `dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj` as the canonical automated regression command

### Integration Points
- `scripts/services/PlayerProgressState.cs` is the primary code target for this phase
- `tests/xiuxian2.Tests/` is the primary test target for this phase
- `README.md`, `justfile`, and `.planning/REQUIREMENTS.md` / `.planning/ROADMAP.md` are the documents that may need updating if the phase meaningfully expands regression coverage surface or commands

</code_context>

<deferred>
## Deferred Ideas

- Mood multiplier regression coverage — keep for a later testing pass
- Save/load restoration tests for player progression state — separate future test slice
- Broader `LevelConfigLoader` validation/reporting tests — still valuable, but not the first focus of Phase 2 discussion

</deferred>

---

*Phase: 02-regression-expansion*
*Context gathered: 2026-03-20*
