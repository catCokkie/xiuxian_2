# Roadmap: xiuxian2 Maintenance Pass

**Created:** 2026-03-19
**Project:** `xiuxian2`
**Focus:** brownfield maintenance / stabilization

## Summary

- Phases: 4
- v1 requirements mapped: 14 / 14
- Unmapped requirements: 0
- Planning mode: `yolo`
- Granularity: `standard`
- Phase 1 status: complete

## Phases

| # | Phase | Goal | Requirements | Success Criteria |
|---|-------|------|--------------|------------------|
| 1 | Main UI Stabilization | Fix the most visible main-bar/submenu interaction failures without redesigning the product shell | UI-01, UI-02, UI-03, UI-04 | 4 |
| 2 | Regression Expansion | Increase automated regression confidence for maintenance-sensitive pure logic and recently touched behavior | TEST-01, TEST-02, TEST-03 | 4 |
| 3 | Runtime Safety Cleanup | Ensure save/runtime behavior and developer-only tooling stay aligned with the stabilized UI/runtime state | STAB-02, STAB-03, SAFE-01, SAFE-02 | 4 |
| 4 | Bounded Display Cleanup | Fix obvious display anomalies that hurt readability while explicitly avoiding a full asset milestone | STAB-01, DISP-01, DISP-02 | 4 |

## Phase Details

### Phase 1: Main UI Stabilization

**Goal:** Fix the main UI behavior users feel immediately: overlapping controls and ineffective dragging for the main bar and submenu.

**Status:** Complete

**Requirements:** UI-01, UI-02, UI-03, UI-04

**Success criteria:**
1. Main bar controls no longer overlap in a way that blocks normal use. ✓
2. Main bar can be dragged to a target position during use. ✓
3. Submenu/book window can be dragged to a target position during use. ✓
4. Existing UI shell remains recognizable and is not replaced by a redesign. ✓

### Phase 2: Regression Expansion

**Goal:** Strengthen automated regression protection around the logic most likely to be touched during maintenance.

**Requirements:** TEST-01, TEST-02, TEST-03

**Success criteria:**
1. Maintainers can run the automated suite from repository root using documented commands.
2. Existing pure-logic rule coverage remains green.
3. Additional tests are added or updated for maintenance-sensitive logic touched during this milestone.
4. Build + test evidence is part of the phase completion check.

### Phase 3: Runtime Safety Cleanup

**Goal:** Make sure stabilization work does not leave behind save/runtime inconsistencies or developer-tool leakage.

**Requirements:** STAB-02, STAB-03, SAFE-01, SAFE-02

**Success criteria:**
1. Save/load behavior remains aligned with the current runtime expectations after maintenance changes.
2. Exploration progress still depends on `InputActivityState.InputBatchTick` rather than local input counting.
3. Maintainer-facing docs remain aligned with current save/verification behavior.
4. Developer-only tools such as config validation stay out of normal player-facing flow by default.

### Phase 4: Bounded Display Cleanup

**Goal:** Fix obvious display anomalies that hurt comprehension while keeping this pass scoped as maintenance, not full art integration.

**Requirements:** STAB-01, DISP-01, DISP-02

**Success criteria:**
1. The existing prototype loop remains playable after the cleanup work.
2. Obvious current display anomalies that directly affect readability are fixed.
3. Cleanup does not expand into a broad manual asset integration effort.
4. Manual Godot verification confirms the cleaned-up display is acceptable in the current prototype shell.

## Requirement Mapping

| Requirement | Phase |
|-------------|-------|
| UI-01 | Phase 1 |
| UI-02 | Phase 1 |
| UI-03 | Phase 1 |
| UI-04 | Phase 1 |
| TEST-01 | Phase 2 |
| TEST-02 | Phase 2 |
| TEST-03 | Phase 2 |
| STAB-02 | Phase 3 |
| STAB-03 | Phase 3 |
| SAFE-01 | Phase 3 |
| SAFE-02 | Phase 3 |
| STAB-01 | Phase 4 |
| DISP-01 | Phase 4 |
| DISP-02 | Phase 4 |

## Planning Notes

- Sequence is intentionally maintenance-first: user-visible UI pain first, regression safety second, runtime/documentation safety third, bounded display cleanup last.
- The roadmap deliberately avoids full redesign, full asset integration, or platform/cloud expansion.

---
*Roadmap created: 2026-03-19 after maintenance-pass initialization*
