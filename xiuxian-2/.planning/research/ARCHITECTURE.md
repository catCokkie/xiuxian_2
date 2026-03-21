# Architecture Research

## Goal

Identify the safest way to structure a brownfield maintenance roadmap for this existing Godot 4 + C# project.

## Recommended Workstream Order

### 1. Stabilize The Most User-Visible Runtime/UI Failures First

- Focus first on the currently broken or unreliable interaction loop:
  - main bar overlap
  - dragging behavior for main bar and submenu
  - obvious display anomalies

Why first:
- These are the most visible failures and also the easiest to verify manually in Godot.

### 2. Expand Regression Coverage Around Confirmed Critical Logic

- Continue the existing `scripts/core/` extraction approach
- Add tests where maintenance work touches logic that can reasonably be isolated from Godot scene dependencies

Why second:
- This locks in bug fixes and reduces the chance of re-breaking repaired behaviors.

### 3. Harden Runtime/Persistence Boundaries Where Maintenance Work Touched Them

- If UI fixes or state fixes affect persisted values, confirm save/read symmetry
- Keep `PrototypeRootController` and `docs/SAVE_SYSTEM.md` aligned

Why third:
- Persistence regressions are high-cost and often discovered late.

### 4. Only Then Take On Broader Structural Cleanup

- Use partial-file or helper extraction where it reduces maintenance burden without changing behavior
- Avoid ambitious architecture churn unless a concrete pain point requires it

Why fourth:
- Brownfield maintenance succeeds through controlled change, not wholesale redesign.

## Safe Sequencing Principles

- Fix user-facing runtime pain before aesthetic or content expansion
- Add tests close to the logic you just touched
- Prefer “extract then test” over “rewrite then hope”
- Keep save/runtime interfaces stable unless the milestone explicitly includes schema work

## What To Isolate First

- Pure calculations and deterministic rules
- UI layout/dragging logic with bounded scene/controller scope
- Runtime state transitions that are already mostly localized

## What To Leave Largely Alone In This Pass

- Steam/cloud productization beyond stability-safe cleanup
- Full asset integration pipeline
- Broad scene or architecture redesign not directly tied to current user pain

## Suggested Phase Structure For This Project

### Phase 1: Main UI Stabilization

- Repair overlapping controls and drag behavior
- Confirm manual Godot behavior is acceptable at runtime

### Phase 2: Regression Expansion For Critical Logic

- Expand pure-logic automated coverage around the maintenance-sensitive rules most likely to regress

### Phase 3: Runtime Safety / Persistence Cleanup

- Close any save/runtime/documentation gaps discovered while doing Phases 1 and 2

### Phase 4: Bounded Display Cleanup

- Fix obvious asset/display anomalies that directly affect readability or comprehension, without expanding into a full asset milestone

## Brownfield Safety Rules

- Preserve existing gameplay contract: exploration remains `InputBatchTick`-driven
- Preserve current save format unless explicitly changing schema
- Prefer local fixes with verification over wide refactors
- Treat `.planning/codebase/*.md`, `README.md`, and `docs/SAVE_SYSTEM.md` as active maintenance surfaces
