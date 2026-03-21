---
phase: 01-test-harness-and-deterministic-seams
plan: "02"
subsystem: testing
tags: [deterministic-seams, xunit-v3, godot, test-fixtures]
requires:
  - phase: 01-test-harness-and-deterministic-seams
    provides: xUnit v3 harness and shared runsettings from 01-01
provides:
  - boundary contracts for RNG, clock, config, filesystem, platform, and hook seams
  - deterministic fake adapters for downstream service seam tests
  - reusable fixture builder seeded with a frozen config sample
affects: [phase-01-plan-03, phase-01-plan-04, phase-01-plan-05]
tech-stack:
  added: []
  patterns: [boundary-first seam contracts, scripted deterministic fakes, frozen test-owned fixtures]
key-files:
  created:
    - xiuxian-2/scripts/contracts/IRng.cs
    - xiuxian-2/scripts/contracts/IClock.cs
    - xiuxian-2/scripts/contracts/IConfigSource.cs
    - xiuxian-2/scripts/contracts/IFileSystem.cs
    - xiuxian-2/scripts/contracts/IPlatformInfo.cs
    - xiuxian-2/scripts/contracts/IHookBackend.cs
    - tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeRng.cs
    - tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeClock.cs
    - tests/Xiuxian2.Core.Tests/Support/Deterministic/InMemoryConfigSource.cs
    - tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeFileSystem.cs
    - tests/Xiuxian2.Core.Tests/Support/Deterministic/FakePlatformInfo.cs
    - tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeHookBackend.cs
    - tests/Xiuxian2.Core.Tests/Builders/ServiceFixtureBuilder.cs
    - tests/Xiuxian2.Core.Tests/Fixtures/config/phase1-sample-config.json
    - tests/Xiuxian2.Core.Tests/Support/Deterministic/DeterministicSupportContractsTests.cs
  modified:
    - tests/Xiuxian2.Core.Tests/Support/Deterministic/FixtureSupportTests.cs
key-decisions:
  - "Keep seam contracts boundary-scoped so later service refactors adopt a shared language without extracting domain logic yet."
  - "Seed the shared fixture builder with a frozen config file under the test project instead of reading mutable runtime config assets."
patterns-established:
  - "Script fake collaborators directly in tests rather than mocking Godot or OS state."
  - "Build service fixtures once, then hydrate deterministic config/file/platform seams from test-owned assets."
requirements-completed: [TEST-02, TEST-06]
duration: 5min
completed: 2026-03-21
---

# Phase 1 Plan 2: Deterministic Seams Summary

**Boundary seam contracts plus scripted fake adapters and a frozen config fixture now give Phase 1 a reusable deterministic support layer for upcoming service refactors.**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-21T00:59:10Z
- **Completed:** 2026-03-21T01:03:56Z
- **Tasks:** 2
- **Files modified:** 16

## Accomplishments

- Added explicit contracts for randomness, time, config loading, filesystem, platform detection, and hook lifecycle under `xiuxian-2/scripts/contracts/`.
- Added deterministic fake implementations and coverage proving scripted values can replace live runtime and OS boundaries.
- Added a reusable `ServiceFixtureBuilder` plus a frozen config sample so later seam-adoption plans can share setup instead of inventing helpers.

## task Commits

Each task was committed atomically:

1. **task 1: write seam contracts before any service refactor** - `43a7c95` (`feat`)
2. **task 2: add deterministic fakes, fixture helpers, and their tests** - `7480d5d` (`test`, RED) and `4d13440` (`feat`, GREEN)

**Plan metadata:** pending

## Files Created/Modified

- `xiuxian-2/scripts/contracts/IRng.cs` - defines the shared randomness seam for later service adoption.
- `xiuxian-2/scripts/contracts/IClock.cs` - defines the Unix-time seam used by deterministic counters.
- `xiuxian-2/scripts/contracts/IConfigSource.cs` - defines config text loading without direct `FileAccess` coupling.
- `xiuxian-2/scripts/contracts/IFileSystem.cs` - defines the filesystem/globalized-path boundary for save sync work.
- `xiuxian-2/scripts/contracts/IPlatformInfo.cs` - defines the minimal platform inspection seam for hook behavior.
- `xiuxian-2/scripts/contracts/IHookBackend.cs` - defines the hook lifecycle and callback chaining seam.
- `tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeRng.cs` - scripts integer and float rolls for deterministic tests.
- `tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeClock.cs` - scripts Unix time progression.
- `tests/Xiuxian2.Core.Tests/Support/Deterministic/InMemoryConfigSource.cs` - serves config text from in-memory paths.
- `tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeFileSystem.cs` - keeps save/file interactions in memory.
- `tests/Xiuxian2.Core.Tests/Support/Deterministic/FakePlatformInfo.cs` - scripts platform name and Windows checks.
- `tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeHookBackend.cs` - scripts hook start outcomes and next-hook chaining.
- `tests/Xiuxian2.Core.Tests/Builders/ServiceFixtureBuilder.cs` - builds reusable deterministic service fixtures around the frozen config sample.
- `tests/Xiuxian2.Core.Tests/Fixtures/config/phase1-sample-config.json` - freezes a test-owned config sample for downstream plans.
- `tests/Xiuxian2.Core.Tests/Support/Deterministic/DeterministicSupportContractsTests.cs` - verifies fake seam behavior stays deterministic and scriptable.
- `tests/Xiuxian2.Core.Tests/Support/Deterministic/FixtureSupportTests.cs` - verifies the fixture builder can load frozen config without live runtime files.

## Decisions Made

- Used small, English-named interfaces that map directly to the current service boundaries instead of introducing broader abstractions early.
- Kept the shared fixture builder test-only and seeded it from a frozen file under `tests/Xiuxian2.Core.Tests/Fixtures/` so later plans can reuse deterministic inputs safely.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed tuple result access in the fake hook backend**
- **Found during:** task 2 (add deterministic fakes, fixture helpers, and their tests)
- **Issue:** the initial `FakeHookBackend` used an unnamed tuple fallback, so the GREEN build failed when accessing `Success` and `Error`.
- **Fix:** typed the fallback tuple explicitly before re-running the deterministic support suite.
- **Files modified:** `tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeHookBackend.cs`
- **Verification:** `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~DeterministicSupportContractsTests|FullyQualifiedName~FixtureSupportTests`
- **Committed in:** `4d13440` (part of task commit)

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** The fix stayed inside the new deterministic support layer and did not expand scope.

## Issues Encountered

- The test project still emits the pre-existing Godot source-generator warning about `GodotProjectDir` during CLI runs, but it did not block the deterministic seam suite or solution-level test pass.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `LevelConfigLoader`, `CloudSaveSyncService`, and `InputHookService` can now adopt shared seam contracts and deterministic helpers instead of adding custom test doubles.
- The frozen config fixture and builder pattern are ready to expand with subsystem-specific seeds in later plans.

## Self-Check: PASSED
