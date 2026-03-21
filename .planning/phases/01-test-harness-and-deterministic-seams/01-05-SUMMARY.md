---
phase: 01-test-harness-and-deterministic-seams
plan: "05"
subsystem: testing
tags: [godot, xunit, input-hooks, platform-seams, win32]
requires:
  - phase: 01-test-harness-and-deterministic-seams
    provides: deterministic seam contracts and test fixture builders from plans 01-01 and 01-02
provides:
  - deterministic platform startup evaluation for InputHookService
  - runtime adapters for Godot platform detection and Win32 hook lifecycle
  - seam-driven tests that avoid real global hook registration in the default CLI suite
affects: [phase-1-default-loop, InputHookService, runtime-platform-adapters]
tech-stack:
  added: []
  patterns: [explicit platform adapter, explicit hook backend adapter, static startup outcome evaluation]
key-files:
  created:
    - xiuxian-2/scripts/adapters/platform/GodotPlatformInfo.cs
    - xiuxian-2/scripts/adapters/platform/Win32HookBackend.cs
  modified:
    - xiuxian-2/scripts/services/InputHookService.cs
    - tests/Xiuxian2.Core.Tests/Services/InputHookServicePlatformTests.cs
    - tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeHookBackend.cs
key-decisions:
  - "Use InputHookService.EvaluateHookStartup as the deterministic seam boundary so platform and backend outcomes can be asserted without constructing a live Godot tree."
  - "Keep runtime behavior on the real adapters by default while routing tests through fake platform and hook collaborators."
patterns-established:
  - "Platform/runtime boundaries stay behind small contracts so service tests do not invoke OS-specific APIs."
  - "Targeted seam suites are the repeatable verification path when unrelated in-progress work exists elsewhere in the shared test project."
requirements-completed: [TEST-06]
duration: 4min
completed: 2026-03-21
---

# Phase 01 Plan 05: Input Hook Platform Seams Summary

**InputHookService now evaluates platform fallback and Win32 hook activation through injectable seams, with repeatable xUnit coverage that never starts real global hooks in the default CLI path.**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-21T01:44:00Z
- **Completed:** 2026-03-21T01:47:57Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- Added runtime adapters for Godot platform detection and Win32 hook lifecycle.
- Refactored `InputHookService` to evaluate startup decisions through `IPlatformInfo` and `IHookBackend` seams.
- Re-verified `InputHookServicePlatformTests` with `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~InputHookServicePlatformTests` and kept the path deterministic.

## Task Commits

Each task was committed atomically:

1. **task 1: write failing tests for platform and hook fallback behavior** - `7e5603b` (`test`)
2. **task 2: refactor InputHookService behind platform and hook backend seams** - `723a2f5` (`feat`)

## Files Created/Modified
- `xiuxian-2/scripts/services/InputHookService.cs` - moves startup and retry decisions behind explicit platform and hook seams.
- `xiuxian-2/scripts/adapters/platform/GodotPlatformInfo.cs` - provides the runtime Godot-backed platform adapter.
- `xiuxian-2/scripts/adapters/platform/Win32HookBackend.cs` - encapsulates live Win32 hook registration and forwarding.
- `tests/Xiuxian2.Core.Tests/Services/InputHookServicePlatformTests.cs` - asserts unsupported-platform, backend-failure, and successful-start outcomes deterministically.
- `tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeHookBackend.cs` - tracks backend start attempts for seam assertions.

## Decisions Made
- Used a pure `EvaluateHookStartup` seam as the main test surface so platform and backend behavior can be verified without requiring `Node._Ready()` or a live Godot tree.
- Preserved the existing Godot-facing service API while moving OS-specific behavior into adapter classes.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- The repeatable targeted test run emitted existing project warnings outside 01-05 scope, but `InputHookServicePlatformTests` still passed cleanly and remained the authoritative verification path for this recovery.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Input hook fallback and platform decisions are now covered through deterministic seams and are safe for the default CLI loop.
- Phase 1 still has separate recovery work remaining for `01-03-PLAN.md`.

## Self-Check: PASSED

- Verified `.planning/phases/01-test-harness-and-deterministic-seams/01-05-SUMMARY.md` exists.
- Verified task commits `7e5603b` and `723a2f5` exist in git history.
