# Features Research

## Scope Of This Research

This is not greenfield feature discovery. It is brownfield maintenance scoping for an existing desktop cultivation prototype. The question is which outcomes are table stakes for a useful maintenance milestone, which are nice-to-have improvements, and which should explicitly be avoided.

## Table Stakes

These are the expected outcomes for a maintenance/stabilization pass.

### Runtime Stability

- Main loop remains playable without obvious regressions
- Save/load still restores current runtime state correctly
- Main UI and submenu behave predictably during normal use
- Existing developer diagnostics remain available without leaking into player-facing flow

Why table stakes:
- The project already works; a maintenance pass that does not improve stability fails its main purpose.

### Regression Safety

- Core pure-logic gameplay rules are covered by automated tests
- Maintenance-sensitive calculations (progression, battle, rewards, settlement, persistence-adjacent rules) gain repeatable test coverage
- Build/test/verify commands remain easy to run from repository root

Why table stakes:
- Brownfield maintenance is only sustainable if changes become safer over time.

### UI Repair

- Main bar layout no longer overlaps internally
- Main bar and submenu dragging work reliably
- Current major display anomalies are addressed

Why table stakes:
- The user explicitly identified these as current pain points, so this is not optional polish.

## Differentiators

These improve the quality of the maintenance milestone but are not mandatory for this pass to count as successful.

- Better codebase maps and planning artifacts under `.planning/`
- More explicit maintainer docs such as `docs/SAVE_SYSTEM.md`
- More refined developer diagnostics for config validation
- Additional regression tests around adjacent progression/state domains beyond the most critical core set

These are useful because they increase maintainability and planning clarity, but they are secondary to fixing the user-visible runtime/UI issues.

## Anti-Features / Things To Avoid

### Full Visual Redesign

- Avoid turning this into a visual redesign or art pass
- Why: It would consume time without addressing the current operational pain points

### Full Asset Integration Milestone

- Avoid broad asset-pipeline completion
- Why: `docs/design/10_todo.md` clearly shows asset work is large, manual, and separate from stability/test/UI repair

### Large New Gameplay Systems

- Avoid adding new content systems just because the architecture is now cleaner
- Why: This expands scope and reduces confidence in stabilization outcomes

### Premature Tooling Complexity

- Avoid introducing heavyweight UI/E2E automation or external orchestration layers unless the current pure-logic and manual validation strategy is clearly insufficient

## Suggested Requirement Categories

For this project-initialization pass, the most natural requirement groups are:

- Stability
- Testing
- Main UI Behavior
- Visual/Display Cleanup
- Documentation / Maintainer Safety

## Dependencies Between Categories

- Stability and UI repair should move early, because they affect whether the project feels usable
- Testing should expand in parallel with stabilization, so bug fixes gain regression protection immediately
- Documentation should follow confirmed structure and persistence behavior, not lead it

## Recommendation

Treat the maintenance milestone as successful when:

1. the project feels safe to run,
2. the most painful UI behavior is fixed,
3. regression coverage is meaningfully better,
4. scope has not exploded into a content or art milestone.
