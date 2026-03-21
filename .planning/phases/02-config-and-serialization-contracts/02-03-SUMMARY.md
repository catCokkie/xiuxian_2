---
phase: 02-config-and-serialization-contracts
plan: "03"
subsystem: testing
tags: [save-contract, schema-5, legacy-compat, runtime-free, serializer]
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
  patterns: [pure save contract helper, runtime-free save text serialization, curated legacy migration coverage]
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
  - "Use a runtime-free save helper plus text fixture comparisons so save regressions fail before manual runtime QA without depending on Godot ConfigFile behavior in xUnit."
patterns-established:
  - "Controller save/read orchestration delegates through a pure contract helper while the controller remains the runtime-facing facade."
  - "Unified save verification uses runtime-free text fixtures and raw contract assertions rather than `user://` paths or live runtime state."
requirements-completed: [TEST-04]
duration: 31min
completed: 2026-03-22
---

# Phase 02 Plan 03: Unified Save Contract Summary

**PrototypeRootController now delegates unified save read/write behavior through a runtime-free contract helper, with schema-5 and curated legacy fixtures locked down under repeatable automated tests.**

## Performance

- **Duration:** 31 min
- **Started:** 2026-03-22T00:00:00Z
- **Completed:** 2026-03-22T00:31:00Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- Added frozen current-schema and curated legacy save fixtures for the Phase 2 save contract.
- Added `PrototypeRootSaveContractTests` covering schema-5 writes, legacy UI-tab migration, and runtime-free fixture usage.
- Extracted `PrototypeRootSaveContract` so `PrototypeRootController` delegates unified save read/write behavior through a focused helper instead of keeping all section wiring inline.
- Stabilized the verification path by moving golden-save comparison onto the helper's runtime-free text serializer/deserializer instead of `Godot.ConfigFile` calls that stalled under the CLI host.
- Verified both the focused save suite and the phase-level combined contract command after the serializer-based stabilization.

## Task Commits

Each task was committed atomically:

1. **task 1: add frozen unified save fixtures and failing contract tests** - `b655026` (`test`)
2. **task 2: extract the save contract helper and wire `PrototypeRootController` through it** - `be5181e` (`feat`)

## Files Created/Modified
- `tests/Xiuxian2.Core.Tests/Fixtures/save/phase2-save-v5.cfg` - golden current-schema save snapshot fixture.
- `tests/Xiuxian2.Core.Tests/Fixtures/save/phase2-legacy-save-v1.cfg` - curated legacy save fixture for bounded backward compatibility.
- `tests/Xiuxian2.Core.Tests/Game/PrototypeRootSaveContractTests.cs` - runtime-free regression suite for unified save write/read behavior.
- `xiuxian-2/scripts/game/PrototypeRootSaveContract.cs` - pure helper that freezes unified save read/write policy and text fixture serialization.
- `xiuxian-2/scripts/game/PrototypeRootController.cs` - delegates save/load orchestration through the new helper.

## Decisions Made
- Preserved `PrototypeRootController` as the runtime-facing owner of save orchestration while extracting the schema logic into a helper.
- Kept legacy compatibility intentionally narrow: one curated older payload shape, not a generalized migration subsystem.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Replaced hanging ConfigFile-based verification with runtime-free serializer assertions**
- **Found during:** task 2 (extract the save contract helper and wire `PrototypeRootController` through it)
- **Issue:** the focused save suite compiled, but any xUnit path that exercised `Godot.ConfigFile` load/save behavior stalled under the CLI host and blocked repeatable verification.
- **Fix:** moved the save helper and fixture assertions onto a runtime-free text serialize/deserialize path while keeping `PrototypeRootController` as the runtime-facing facade that bridges to `ConfigFile` in production.
- **Files modified:** `xiuxian-2/scripts/game/PrototypeRootSaveContract.cs`, `tests/Xiuxian2.Core.Tests/Game/PrototypeRootSaveContractTests.cs`
- **Verification:** `dotnet test xiuxian-2/xiuxian2.sln --settings tests/.runsettings --filter "FullyQualifiedName~PrototypeRootSaveContractTests|FullyQualifiedName~StateSerializationContractTests|FullyQualifiedName~LevelConfigLoaderConfigContractTests"`
- **Committed in:** `b064dc7`

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** The fix stayed within the save-contract integration scope and restored the required combined verification path.

## Issues Encountered
- The recovered workspace already contained partial 02-03 attempts, so the final pass had to reconcile the existing summary/docs state with the latest green verification.
- The focused suite only became repeatable after the runtime-free serializer path replaced `ConfigFile`-driven test assertions in xUnit.

## User Setup Required

None.

## Next Phase Readiness
- Phase 2 now has frozen config contracts, service/runtime serialization contracts, and unified save snapshot contracts.
- The phase is ready for formal goal verification and, if passed, transition to Phase 3.

## Self-Check: PASSED
