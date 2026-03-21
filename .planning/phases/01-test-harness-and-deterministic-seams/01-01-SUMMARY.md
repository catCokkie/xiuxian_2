---
phase: 01-test-harness-and-deterministic-seams
plan: "01"
subsystem: testing
tags: [dotnet, xunit-v3, runsettings, deterministic-seams]
requires: []
provides:
  - fast xUnit v3 core test project wired into the solution
  - shared CLI test settings for stable local and automation runs
  - smoke coverage proving the default harness is runtime-free
affects: [phase-01-plan-02, phase-01-plan-03, phase-01-plan-04, phase-01-plan-05]
tech-stack:
  added: [Microsoft.NET.Test.Sdk, xunit.v3, xunit.runner.visualstudio]
  patterns: [central package pinning, solution-level test project discovery, runtime-free smoke tests]
key-files:
  created:
    - Directory.Packages.props
    - tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj
    - tests/.runsettings
    - tests/Xiuxian2.Core.Tests/Usings.cs
    - tests/Xiuxian2.Core.Tests/Smoke/TestHarnessSmokeTests.cs
    - xiuxian-2/docs/TESTING_AUTOMATION.md
  modified:
    - xiuxian-2/xiuxian2.sln
key-decisions:
  - "Use an xUnit v3 project outside the Godot runtime as the Phase 1 default loop."
  - "Keep one fast project command for iteration and one solution command with shared runsettings for automation."
patterns-established:
  - "Fast tests live under tests/Xiuxian2.Core.Tests and run with plain dotnet test."
  - "Harness smoke tests verify path discovery and runtime-free execution before deeper service coverage lands."
requirements-completed: [TEST-01, TEST-02]
duration: 7min
completed: 2026-03-21
---

# Phase 1 Plan 1: Test Harness Summary

**A plain `dotnet test` harness now discovers a dedicated xUnit v3 core suite with shared runsettings and runtime-free smoke coverage.**

## Performance

- **Duration:** 7 min
- **Started:** 2026-03-21T00:47:07Z
- **Completed:** 2026-03-21T00:53:43Z
- **Tasks:** 3
- **Files modified:** 7

## Accomplishments

- Added centralized package pinning plus a dedicated `tests/Xiuxian2.Core.Tests/` project wired into `xiuxian-2/xiuxian2.sln`.
- Added smoke tests that prove the standard runner works, repository-relative paths are discoverable, and the default suite does not boot Godot.
- Documented the authoritative local and automation commands in English for Phase 1 test execution.

## task Commits

Each task was committed atomically:

1. **task 1: scaffold the default core test entry point** - `b029bf6` (`chore`)
2. **task 2: add the first failing-then-passing harness smoke tests** - `72c098f` (`test`, RED) and `09b7214` (`test`, GREEN)
3. **task 3: document the single authoritative test command** - `cd04dff` (`docs`)

**Plan metadata:** pending

## Files Created/Modified

- `Directory.Packages.props` - pins shared test package versions centrally.
- `tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj` - defines the fast core test suite and references the game project.
- `tests/.runsettings` - provides the shared CLI settings path for solution runs.
- `tests/Xiuxian2.Core.Tests/Usings.cs` - defines the common imports for the new suite.
- `tests/Xiuxian2.Core.Tests/Smoke/TestHarnessSmokeTests.cs` - proves runner viability, path resolution, and runtime-free execution.
- `xiuxian-2/xiuxian2.sln` - discovers the new test project through standard solution tooling.
- `xiuxian-2/docs/TESTING_AUTOMATION.md` - documents the default local and automation commands.

## Decisions Made

- Used xUnit v3 with central package version management so later Phase 1 plans can add tests without duplicating version pins.
- Kept the quick local command scoped to `tests/Xiuxian2.Core.Tests` while reserving `xiuxian-2/xiuxian2.sln --settings tests/.runsettings` as the solution-level automation path.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- The existing Godot project emits nullable/source-generator warnings during CLI builds, including a `GodotProjectDir` warning when the game project is referenced from tests. These warnings predate this plan and did not block green test runs.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 1 now has a stable, documented default suite entry point for deterministic seam work.
- The next plan can add shared fixtures, fakes, and builders on top of the new core test project.

## Self-Check: PASSED
