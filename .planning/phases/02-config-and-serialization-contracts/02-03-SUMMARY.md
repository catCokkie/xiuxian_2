---
phase: 02-config-and-serialization-contracts
plan: "03"
subsystem: testing
tags: [save-contract, configfile, schema-5, legacy-compat, runtime-free]
requires:
  - phase: 02-config-and-serialization-contracts
    provides: config fixtures and serialization contract helpers from plans 02-01 and 02-02
provides:
  - authoritative schema-5 unified save contract helper for PrototypeRootController
  - current-schema and curated legacy save fixtures under automated regression coverage
  - focused controller-facing save verification without booting runtime-heavy flows
affects: [phase-02, TEST-04, PrototypeRootController, save-system]
tech-stack:
  added: []
  patterns: [pure save contract helper, ConfigFile fixture comparison, curated legacy migration coverage]
key-files:
  created:
    - tests/Xiuxian2.Core.Tests/Fixtures/save/phase2-save-v5.cfg
    - tests/Xiuxian2.Core.Tests/Fixtures/save/phase2-legacy-save-v1.cfg
    - tests/Xiuxian2.Core.Tests/Game/PrototypeRootSaveContractTests.cs
    - xiuxian-2/scripts/game/PrototypeRootSaveContract.cs
  modified:
    - xiuxian-2/scripts/game/PrototypeRootController.cs
key-decisions:
  - "Keep writes locked to schema version 5 and support only one curated legacy read shape instead of building a general migration system."
  - "Use a runtime-free save helper plus fixture-backed ConfigFile comparisons so save regressions fail before manual runtime QA."
patterns-established:
  - "Controller save/read orchestration delegates through a pure contract helper while the controller remains the runtime-facing facade."
  - "Unified save verification uses golden ConfigFile fixtures and raw contract assertions rather than `user://` paths or live runtime state."
requirements-completed: [TEST-04]
duration: 24min
completed: 2026-03-21
---

# Phase 02 Plan 03: Unified Save Contract Summary

**PrototypeRootController now delegates unified save read/write behavior through a runtime-free contract helper, with schema-5 and curated legacy fixtures locked down under automated tests.**

## Performance

- **Duration:** 24 min
- **Started:** 2026-03-21T13:11:00Z
- **Completed:** 2026-03-21T13:58:00Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- Added frozen current-schema and curated legacy save fixtures for the Phase 2 save contract.
- Added `PrototypeRootSaveContractTests` covering schema-5 writes, legacy UI-tab migration, and runtime-free fixture usage.
- Extracted `PrototypeRootSaveContract` so `PrototypeRootController` delegates unified save read/write behavior through a focused helper instead of keeping all section wiring inline.
- Verified the focused save suite and the phase-level combined contract command after the controller integration fix.

## Task Commits

Each task was committed atomically:

1. **task 1: add frozen unified save fixtures and failing contract tests** - `aed4b2a` (`test`)
2. **task 2: extract the save contract helper and wire `PrototypeRootController` through it** - `8efda4d` (`feat`)

## Files Created/Modified
- `tests/Xiuxian2.Core.Tests/Fixtures/save/phase2-save-v5.cfg` - golden current-schema save snapshot fixture.
- `tests/Xiuxian2.Core.Tests/Fixtures/save/phase2-legacy-save-v1.cfg` - curated legacy save fixture for bounded backward compatibility.
- `tests/Xiuxian2.Core.Tests/Game/PrototypeRootSaveContractTests.cs` - runtime-free regression suite for unified save write/read behavior.
- `xiuxian-2/scripts/game/PrototypeRootSaveContract.cs` - pure helper that freezes unified save read/write policy.
- `xiuxian-2/scripts/game/PrototypeRootController.cs` - delegates save/load orchestration through the new helper.

## Decisions Made
- Preserved `PrototypeRootController` as the runtime-facing owner of save orchestration while extracting the schema logic into a helper.
- Kept legacy compatibility intentionally narrow: one curated older payload shape, not a generalized migration subsystem.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Fixed helper integration compile drift before final combined verification**
- **Found during:** task 2 (extract the save contract helper and wire `PrototypeRootController` through it)
- **Issue:** the initial helper integration passed the focused save tests but failed the combined phase verification because `PrototypeRootController` passed a `double` timestamp into the helper write contract.
- **Fix:** aligned the controller callsite with the helper’s `long` timestamp contract and reran the plan-level verification commands.
- **Files modified:** `xiuxian-2/scripts/game/PrototypeRootController.cs`
- **Verification:** `dotnet test xiuxian-2/xiuxian2.sln --settings tests/.runsettings --filter "FullyQualifiedName~PrototypeRootSaveContractTests|FullyQualifiedName~StateSerializationContractTests|FullyQualifiedName~LevelConfigLoaderConfigContractTests"`
- **Committed in:** `8efda4d`

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** The fix stayed within the save-contract integration scope and restored the required combined verification path.

## Issues Encountered
- The first recovery attempt stopped after the RED step, leaving the helper file staged but no summary/docs output.
- The final combined verification exposed a concrete controller/helper type mismatch that was not visible in the focused save-only test run.

## User Setup Required

None.

## Next Phase Readiness
- Phase 2 now has frozen config contracts, service/runtime serialization contracts, and unified save snapshot contracts.
- The phase is ready for formal goal verification and, if passed, transition to Phase 3.

## Self-Check: PASSED
