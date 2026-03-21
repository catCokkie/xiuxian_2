# Roadmap: Xiuxian 2

## Overview

This milestone builds a brownfield-safe automated test foundation for the existing Godot 4 + C# game by locking down deterministic seams first, freezing config and save contracts next, then protecting large service refactors before adding a thin runtime smoke layer. The sequence favors fast service-level regression value over UI-heavy automation so refactors around files like `LevelConfigLoader.cs` can proceed with confidence.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Test Harness and Deterministic Seams** - Establish the repeatable test entry point, reusable test structure, and controllable service dependencies.
- [ ] **Phase 2: Config and Serialization Contracts** - Freeze the highest-risk config and save behaviors with fixture-backed regression coverage.
- [ ] **Phase 3: Service Characterization for Safe Extraction** - Lock down large service behavior so refactors can split runtime monoliths safely.
- [ ] **Phase 4: Runtime Smoke and Contract Validation** - Add a minimal Godot runtime layer that catches broken startup, autoload, and signal wiring.

## Phase Details

### Phase 1: Test Harness and Deterministic Seams
**Goal**: Developers can run a single repeatable automated test workflow against the brownfield project, and core services can be exercised with deterministic dependencies instead of live runtime boundaries.
**Depends on**: Nothing (first phase)
**Requirements**: TEST-01, TEST-02, TEST-06
**Success Criteria** (what must be TRUE):
  1. A developer can run one documented CLI test command and get the same suite entry point locally and in automation.
  2. New service tests can reuse shared fixtures, builders, and helpers instead of creating ad hoc setup in each file.
  3. Core service tests can replace RNG, clock, filesystem, and platform/runtime boundaries with deterministic seams and assert stable outcomes.
  4. The default feedback loop stays focused on fast service-level tests rather than requiring a live Godot runtime.
**Plans**: 5 plans

Plans:
- [x] `01-01-PLAN.md` - Scaffold the default xUnit-based CLI test harness and document the single test command
- [x] `01-02-PLAN.md` - Define deterministic seam contracts plus reusable fakes, builders, and frozen fixtures
- [ ] `01-03-PLAN.md` - Refactor `LevelConfigLoader` behind config, RNG, and clock seams with TDD coverage
- [x] `01-04-PLAN.md` - Refactor `CloudSaveSyncService` behind a filesystem seam with TDD coverage
- [x] `01-05-PLAN.md` - Refactor `InputHookService` behind platform and hook seams with TDD coverage

### Phase 2: Config and Serialization Contracts
**Goal**: The highest-risk data contracts are frozen so save/config refactors can happen without silent behavior drift.
**Depends on**: Phase 1
**Requirements**: TEST-03, TEST-04
**Success Criteria** (what must be TRUE):
  1. Representative config inputs produce repeatable parsed and validated outputs under automated regression tests.
  2. Save data can complete automated round-trip serialization tests without losing required progression state.
  3. Contract-breaking changes to config parsing or save payload shape fail tests before runtime regressions reach manual QA.
**Plans**: TBD

### Phase 3: Service Characterization for Safe Extraction
**Goal**: Large runtime services have behavior-frozen characterization coverage that makes safe extraction and restructuring possible.
**Depends on**: Phase 2
**Requirements**: TEST-05
**Success Criteria** (what must be TRUE):
  1. A developer can add characterization tests around a large service such as `LevelConfigLoader` before changing its structure.
  2. Existing service behavior can be verified through automated tests before and after extraction work with no expected-result drift.
  3. Refactor candidates can move logic behind thinner facades while regression tests confirm the same externally observable service outcomes.
**Plans**: TBD

### Phase 4: Runtime Smoke and Contract Validation
**Goal**: The project has a thin runtime-facing safety net that catches broken startup and selected engine-facing contracts without turning the milestone into UI automation.
**Depends on**: Phase 3
**Requirements**: TEST-07, TEST-08
**Success Criteria** (what must be TRUE):
  1. A minimal automated runtime smoke path can boot the project and fail fast when required autoload wiring is broken.
  2. Selected signal and autoload contracts have automated validation for the runtime-facing behaviors pure service tests cannot see.
  3. The runtime layer remains intentionally small and does not require full UI automation or Windows-only hook coverage to trust the milestone.
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Test Harness and Deterministic Seams | 4/5 | In Progress | - |
| 2. Config and Serialization Contracts | 0/TBD | Not started | - |
| 3. Service Characterization for Safe Extraction | 0/TBD | Not started | - |
| 4. Runtime Smoke and Contract Validation | 0/TBD | Not started | - |
