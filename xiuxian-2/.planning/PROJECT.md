# xiuxian2 Maintenance Pass

## What This Is

This is an existing Godot 4.5 + C# desktop cultivation pet project that already has a playable prototype loop, persistence, UI, and config-driven progression. This maintenance pass focuses on tightening the current experience rather than inventing a new product: stabilize the runtime, expand automated regression coverage, and fix visible UI/layout problems in the current main interface.

## Core Value

The existing prototype should feel stable to use and safe to keep iterating on: the main loop works, the UI behaves predictably, and regressions are caught earlier.

## Requirements

### Validated

- ✓ Bottom-docked runtime prototype exists with input-driven exploration and battle flow — existing
- ✓ Unified save system exists around `user://save_state.cfg` — existing
- ✓ Main bar + submenu UI shell exists — existing
- ✓ Minimal automated regression project exists under `tests/xiuxian2.Tests/` — existing

### Active

- [ ] Existing gameplay/runtime flow is stable enough for regular play and iteration
- [ ] Core automated regression coverage expands around the most important pure logic and maintenance-sensitive behavior
- [ ] Main UI layout issues are fixed, especially overlapping controls inside the main bar
- [ ] Main bar and submenu dragging both work as expected during use
- [ ] Obvious asset/display anomalies that directly affect current UI readability are cleaned up without expanding into a full asset-integration milestone

### Out of Scope

- Full visual overhaul or complete art pass — this round is maintenance-first, not a redesign milestone
- Full asset pipeline completion for all origin assets — only obvious display-impacting issues are in scope
- Large new gameplay systems unrelated to current stability/testing/UI issues — would dilute the maintenance pass

## Context

The repository is a brownfield Godot project with a growing documentation and planning layer under `.planning/` and `docs/`. Recent work already improved repository hygiene by adding `README.md`, `docs/SAVE_SYSTEM.md`, automated xUnit regression tests for pure logic, and partial-file decompositions for `ExploreProgressController` and `LevelConfigLoader`. The most immediate user-reported pain is now around UI behavior: overlapping controls in the main bar and ineffective drag behavior for both the main bar and submenu. Testing expansion and runtime stability improvements are part of the same maintenance push so future iteration becomes safer.

## Constraints

- **Tech stack**: Stay within Godot 4.5 + C# / `net8.0` — preserve current project runtime and tooling
- **Gameplay rule**: Exploration progress must continue to be driven by `InputActivityState.InputBatchTick` — this is a hard project rule
- **Persistence safety**: Save/load compatibility around `user://save_state.cfg` must not regress — save read/write paths must stay aligned
- **Scope control**: Only fix obvious UI-facing asset display issues; do not silently expand into full asset integration
- **Verification**: Build/test verification plus manual Godot UI checks remain required because layout and dragging behavior are scene/runtime dependent

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Treat this as a maintenance milestone, not a new product | The repository already has a working prototype and the user asked to improve it rather than replace it | ✓ Good |
| Prioritize runtime stability, test coverage, and current UI behavior together | These three areas directly support safer iteration on the existing project | ✓ Good |
| Limit asset work to obvious display-impacting anomalies only | Avoid turning a maintenance pass into a large content/material pipeline milestone | ✓ Good |
| Require draggable behavior for both the main bar and submenu | The user explicitly called out drag adjustments as ineffective | ✓ Good |

---
*Last updated: 2026-03-19 after project initialization questioning*
