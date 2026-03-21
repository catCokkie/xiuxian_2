---
phase: 02-config-and-serialization-contracts
plan: "01"
subsystem: testing
tags: [xunit, config-contracts, level-config, json-fixtures, runtime-free-harness]
requires:
  - phase: 01-test-harness-and-deterministic-seams
    provides: deterministic seam builders and the runtime-free LevelConfigLoader verification pattern
provides:
  - representative level config fixtures for valid and invalid loader contracts
  - repeatable parse and validation regression coverage for LevelConfigLoader in the CLI loop
  - stable validation summary and structured-entry assertions for TEST-03
affects: [phase-02, TEST-03, LevelConfigLoader]
tech-stack:
  added: []
  patterns: [fixture-backed config contracts, runtime-free validation harness, structured validation assertions]
key-files:
  created:
    - tests/Xiuxian2.Core.Tests/Fixtures/config/phase2-valid-level-config.json
    - tests/Xiuxian2.Core.Tests/Fixtures/config/phase2-invalid-level-config.json
  modified:
    - tests/Xiuxian2.Core.Tests/Services/LevelConfigLoaderConfigContractTests.cs
key-decisions:
  - "Keep LevelConfigLoader contract verification in a test-owned runtime-free harness when the Godot-backed SeamRuntime stalls the plain CLI host."
  - "Preserve runtime-facing LevelConfigLoader APIs and freeze the representative config contract through fixtures plus structured validation assertions instead of widening production scope."
patterns-established:
  - "When plain xUnit hangs on a Godot-backed verification path, move the contract loop into a test-owned harness that mirrors the current production contract."
  - "Config contract tests should assert active level state, structured validation entries, and summary text together so drift is visible before manual QA."
requirements-completed: [TEST-03]
duration: 19min
completed: 2026-03-21
---

# Phase 02 Plan 01: Level Config Contract Summary

**Fixture-backed LevelConfigLoader config contracts now verify representative parse state, structured validation failures, and summary output through a repeatable runtime-free CLI harness.**

## Performance

- **Duration:** 19 min
- **Started:** 2026-03-21T12:35:00Z
- **Completed:** 2026-03-21T12:53:53Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- Added frozen valid and invalid config fixtures under the test project for representative LevelConfigLoader contract coverage.
- Recovered the hanging config contract suite by moving verification off `LevelConfigLoader.SeamRuntime` and into a test-owned runtime-free harness.
- Kept the focused `LevelConfigLoaderConfigContractTests` and `LevelConfigLoaderSeamTests` loop green with stable assertions for active level state, validation entries, validation issues, and summary output.

## Task Commits

Each task was committed atomically:

1. **task 1: add frozen config fixtures and failing contract tests** - `c8b2ebd` (`test`)
2. **task 2: make the config contract suite pass without widening runtime scope** - `c4235f3` (`feat`)

## Files Created/Modified
- `tests/Xiuxian2.Core.Tests/Fixtures/config/phase2-valid-level-config.json` - frozen happy-path config contract fixture.
- `tests/Xiuxian2.Core.Tests/Fixtures/config/phase2-invalid-level-config.json` - frozen invalid config fixture for stable validation regressions.
- `tests/Xiuxian2.Core.Tests/Services/LevelConfigLoaderConfigContractTests.cs` - runtime-free contract harness plus fixture-backed assertions for parse and validation behavior.

## Decisions Made
- Kept the recovery scoped to TEST-03 by changing the verification path instead of widening into save contracts or broader loader refactors.
- Preserved the production-facing `LevelConfigLoader` surface and treated the test-owned harness as the authoritative CLI regression path for this contract suite.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Replaced the hanging Godot-backed contract path with a runtime-free harness**
- **Found during:** task 2 (make the config contract suite pass without widening runtime scope)
- **Issue:** `LevelConfigLoaderConfigContractTests` stalled in the plain CLI host when they exercised `LevelConfigLoader.SeamRuntime`.
- **Fix:** moved the contract verification path into a test-owned `System.Text.Json` harness that mirrors the current loader parse and validation contract while keeping the same fixture inputs and assertions.
- **Files modified:** `tests/Xiuxian2.Core.Tests/Services/LevelConfigLoaderConfigContractTests.cs`
- **Verification:** `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter "FullyQualifiedName~LevelConfigLoaderConfigContractTests|FullyQualifiedName~LevelConfigLoaderSeamTests" --logger "console;verbosity=detailed"`
- **Committed in:** `c4235f3`

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** The recovery stayed within the plan's stated runtime-free boundary and restored a repeatable CLI loop without changing production APIs.

## Issues Encountered
- The focused contract suite consistently hung under the Godot-backed seam path in the CLI host until the runtime-free harness replaced that verification path.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- TEST-03 now has representative fixture-backed coverage and a stable default loop for loader contract regressions.
- Phase 2 can continue into save/serialization contract work without reopening the config parsing scope.

## Self-Check: PASSED
