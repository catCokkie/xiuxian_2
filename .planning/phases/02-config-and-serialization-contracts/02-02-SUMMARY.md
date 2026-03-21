---
phase: 02-config-and-serialization-contracts
plan: "02"
subsystem: testing
tags: [serialization, contracts, runtime-free, xunit, level-runtime, save-state]
requires:
  - phase: 01-test-harness-and-deterministic-seams
    provides: runtime-free xUnit harness and deterministic service test patterns
provides:
  - runtime-free normalization helpers for save-participating state services
  - deterministic regression coverage for state dictionary round-trips and malformed payload normalization
  - level runtime payload contract coverage independent of Godot Node construction
affects: [phase-02, TEST-04, state-services, LevelConfigLoader]
tech-stack:
  added: []
  patterns: [raw contract normalization helpers, variant bridge boundary, runtime-free serialization assertions]
key-files:
  created:
    - tests/Xiuxian2.Core.Tests/Support/Serialization/StateSerializationFixtureBuilder.cs
    - tests/Xiuxian2.Core.Tests/Services/StateSerializationContractTests.cs
    - tests/Xiuxian2.Core.Tests/Services/LevelConfigLoaderRuntimeDictionaryTests.cs
    - xiuxian-2/scripts/services/StateSerializationContracts.cs
  modified:
    - xiuxian-2/scripts/services/BackpackState.cs
    - xiuxian-2/scripts/services/ResourceWalletState.cs
    - xiuxian-2/scripts/services/PlayerProgressState.cs
    - xiuxian-2/scripts/services/InputActivityState.cs
    - xiuxian-2/scripts/services/PlayerActionState.cs
    - xiuxian-2/scripts/services/LevelConfigLoader.cs
key-decisions:
  - "Keep the public state-service APIs intact and move Phase 2 verification onto runtime-free raw normalization helpers instead of Godot Variant-heavy CLI paths that stall the test host."
  - "Freeze service dictionary and level runtime payload shape now so unified save work in 02-03 can depend on one deterministic contract source."
patterns-established:
  - "Service serialization contracts normalize through raw helper dictionaries first, then bridge back to Godot Variant payloads at the runtime boundary."
  - "When CLI tests hang on Variant-heavy paths, keep the test surface in pure .NET collections and treat the Variant bridge as a thin adapter."
requirements-completed: [TEST-04]
duration: 20min
completed: 2026-03-21
---

# Phase 02 Plan 02: State Serialization Contract Summary

**Core save-participating services and the level runtime payload now have runtime-free contract coverage, so dictionary round-trips and malformed payload normalization can be verified without booting Godot runtime behavior in the CLI loop.**

## Performance

- **Duration:** 20 min
- **Started:** 2026-03-21T12:35:00Z
- **Completed:** 2026-03-21T12:55:00Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments
- Added reusable serialization fixture builders for representative and malformed payloads.
- Added runtime-free regression suites for service dictionary normalization and `LevelConfigLoader` runtime payload contracts.
- Introduced `StateSerializationContracts` as the normalization/bridging layer so state services can keep their public APIs while tests stay off the Godot runtime path.
- Verified the focused serialization suites pass repeatably in the Phase 2 CLI loop.

## Task Commits

Each task was committed atomically:

1. **task 1: add failing round-trip and normalization tests for serializable services** - `430d421` (`test`)
2. **task 2: freeze level runtime dictionary behavior alongside the service state contracts** - `09afbe1` (`feat`)

## Files Created/Modified
- `tests/Xiuxian2.Core.Tests/Support/Serialization/StateSerializationFixtureBuilder.cs` - reusable service and malformed payload fixtures.
- `tests/Xiuxian2.Core.Tests/Services/StateSerializationContractTests.cs` - runtime-free service dictionary contract tests.
- `tests/Xiuxian2.Core.Tests/Services/LevelConfigLoaderRuntimeDictionaryTests.cs` - runtime-free level runtime payload contract tests.
- `xiuxian-2/scripts/services/StateSerializationContracts.cs` - raw normalization helpers plus Variant bridge support.
- `xiuxian-2/scripts/services/BackpackState.cs` - normalized dictionary round-trip through shared contract helpers.
- `xiuxian-2/scripts/services/ResourceWalletState.cs` - normalized dictionary round-trip through shared contract helpers.
- `xiuxian-2/scripts/services/PlayerProgressState.cs` - normalized dictionary round-trip through shared contract helpers.
- `xiuxian-2/scripts/services/InputActivityState.cs` - normalized dictionary round-trip through shared contract helpers.
- `xiuxian-2/scripts/services/PlayerActionState.cs` - normalized dictionary round-trip through shared contract helpers.
- `xiuxian-2/scripts/services/LevelConfigLoader.cs` - level runtime dictionary behavior delegates through the shared serialization contracts.

## Decisions Made
- Preserved the existing service-level `ToDictionary` / `FromDictionary` and runtime-facing `ToRuntimeDictionary` / `FromRuntimeDictionary` APIs.
- Treated the raw normalization helpers and focused filtered suite as the authoritative verification path for Phase 2 serialization work.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Replaced the hanging Variant-heavy CLI path with runtime-free raw normalization helpers**
- **Found during:** task 2 (freeze level runtime dictionary behavior alongside the service state contracts)
- **Issue:** even isolated Phase 2 serialization tests hung before execution finished when the verification path stayed Variant-heavy in the CLI host.
- **Fix:** introduced raw contract normalization helpers and a thin Variant bridge so tests verify pure .NET dictionaries while production code preserves the existing Godot-facing payload APIs.
- **Files modified:** `xiuxian-2/scripts/services/StateSerializationContracts.cs`, `tests/Xiuxian2.Core.Tests/Services/StateSerializationContractTests.cs`, `tests/Xiuxian2.Core.Tests/Services/LevelConfigLoaderRuntimeDictionaryTests.cs`
- **Verification:** `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter "FullyQualifiedName~StateSerializationContractTests|FullyQualifiedName~LevelConfigLoaderRuntimeDictionaryTests"`
- **Committed in:** `09afbe1`

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** The recovery stayed inside TEST-04 contract-freezing scope and produced a repeatable runtime-free verification path for the upcoming unified save plan.

## Issues Encountered
- The original Variant-heavy contract path stalled the CLI host until the raw normalization/bridge split isolated the pure serialization logic.

## User Setup Required

None.

## Next Phase Readiness
- Service and level runtime serialization contracts are frozen and can feed unified save snapshot work in `02-03`.
- Phase 2 Wave 2 can focus on the save helper and legacy fixture path without reopening service-level normalization scope.

## Self-Check: PASSED
