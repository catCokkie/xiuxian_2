# Requirements: Xiuxian 2

**Defined:** 2026-03-21
**Core Value:** Create a reliable automated test safety net for the core service layer so major refactors, especially around configuration and progression logic, can happen without breaking the game's main systems.

## v1 Requirements

Requirements for the current brownfield testing milestone. Each requirement should map to roadmap phases.

### Test Foundation

- [ ] **TEST-01**: Developer can run one repeatable CLI test command for the repository
- [ ] **TEST-02**: Repository contains a reusable automated test project structure with shared fixtures, builders, and helpers

### Service Contracts

- [ ] **TEST-03**: Config parsing and validation behavior is covered by automated regression tests
- [ ] **TEST-04**: Save/load round-trip behavior is covered by automated serialization tests
- [ ] **TEST-05**: Large service behavior can be frozen with characterization tests before refactors begin
- [ ] **TEST-06**: Core services can depend on deterministic seams for RNG, clock, filesystem, and platform/runtime boundaries

### Runtime Smoke

- [ ] **TEST-07**: Project has a minimal runtime smoke path that validates startup and autoload wiring
- [ ] **TEST-08**: Selected signal/autoload contracts have thin runtime-facing automated validation

## v2 Requirements

- [ ] **TEST-09**: Windows-specific hook smoke coverage can run in a dedicated environment or lane
- [ ] **TEST-10**: Coverage reporting is published and tracked as a visibility signal, not the primary milestone goal
- [ ] **TEST-11**: UI automation covers high-value interaction paths after the core service safety net is stable

## Out of Scope

- Full UI automation in the first testing milestone — too much complexity and flake risk relative to the immediate refactor-safety goal
- Windows-only hook smoke coverage in the first milestone — environment-specific and better isolated after the baseline suite is trusted
- Coverage percentage as the main success metric — useful for visibility later, but not the best driver for initial brownfield test architecture
- Major gameplay/content expansion during this work — the milestone is about confidence and maintainability, not feature breadth

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| TEST-01 | Phase 1 | Pending |
| TEST-02 | Phase 1 | Pending |
| TEST-03 | Phase 2 | Pending |
| TEST-04 | Phase 2 | Pending |
| TEST-05 | Phase 3 | Pending |
| TEST-06 | Phase 1 | Pending |
| TEST-07 | Phase 4 | Pending |
| TEST-08 | Phase 4 | Pending |

---
*Last updated: 2026-03-21 after roadmap generation*
