# Requirements: xiuxian2 Maintenance Pass

**Defined:** 2026-03-19
**Core Value:** The existing prototype should feel stable to use and safe to keep iterating on: the main loop works, the UI behaves predictably, and regressions are caught earlier.

## v1 Requirements

### Stability

- [ ] **STAB-01**: User can play the existing core loop without obvious runtime breakage in the main prototype scene
- [ ] **STAB-02**: User can reload the project state and recover the expected explore/runtime context without save corruption
- [ ] **STAB-03**: Maintenance fixes do not break the project rule that exploration progress is driven by `InputActivityState.InputBatchTick`

### Testing

- [ ] **TEST-01**: Maintainer can run automated regression tests from repository root with a documented command
- [ ] **TEST-02**: Critical pure-logic gameplay rules for progression, battle, drop economy, and settlement remain covered by automated tests
- [ ] **TEST-03**: This maintenance pass adds or updates regression coverage for newly touched maintenance-sensitive logic

### Main UI Behavior

- [x] **UI-01**: User can use the main bar without internal control overlap blocking core information or interaction
- [x] **UI-02**: User can drag the main bar to a target position during use
- [x] **UI-03**: User can drag the submenu/book window to a target position during use
- [x] **UI-04**: Main UI fixes preserve the current core interaction shell instead of replacing it with a new layout system

### Display Cleanup

- [ ] **DISP-01**: Obvious display anomalies that directly affect readability or comprehension in the current UI are fixed
- [ ] **DISP-02**: Display cleanup remains bounded and does not expand into a full asset-integration milestone

### Maintainer Safety

- [ ] **SAFE-01**: Maintainer-facing docs remain aligned with the save system, verification flow, and current maintenance scope
- [ ] **SAFE-02**: Developer-only tools such as config validation stay hidden from normal player-facing flow unless actively needed

## v2 Requirements

### Asset Integration

- **ASSET-01**: Runtime UI uses fully integrated final production assets instead of partial/manual staging assets
- **ASSET-02**: Character, monster, item, and effect asset pipelines become fully documented and repeatable

### Platform / Cloud

- **PLAT-01**: Steam/cloud sync becomes a release-ready feature with clearer conflict handling
- **PLAT-02**: Cross-platform input behavior moves beyond the current Windows-first constraint

### Advanced Testing

- **ADVTEST-01**: Godot scene/runtime integration tests exist for key user workflows
- **ADVTEST-02**: Save/load roundtrip behavior has automated validation beyond pure-logic tests

## Out of Scope

| Feature | Reason |
|---------|--------|
| Full visual redesign | This is a maintenance-first pass, not a redesign milestone |
| Complete asset pipeline completion | Too large and too manual for the current maintenance scope |
| Major new gameplay systems | Would dilute the stabilization/testing/UI repair goal |
| Release-grade Steamworks productization | Important, but not part of the current maintenance target |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| STAB-01 | Phase 1 | Pending |
| STAB-02 | Phase 3 | Pending |
| STAB-03 | Phase 3 | Pending |
| TEST-01 | Phase 2 | Pending |
| TEST-02 | Phase 2 | Pending |
| TEST-03 | Phase 2 | Pending |
| UI-01 | Phase 1 | Complete |
| UI-02 | Phase 1 | Complete |
| UI-03 | Phase 1 | Complete |
| UI-04 | Phase 1 | Complete |
| DISP-01 | Phase 4 | Pending |
| DISP-02 | Phase 4 | Pending |
| SAFE-01 | Phase 3 | Pending |
| SAFE-02 | Phase 3 | Pending |

**Coverage:**
- v1 requirements: 14 total
- Mapped to phases: 14
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-19*
*Last updated: 2026-03-19 after Phase 1 completion*
