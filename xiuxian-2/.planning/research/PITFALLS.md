# Pitfalls Research

## Pitfall 1: Turning Maintenance Into A Rewrite

### Warning Signs

- UI bugfixes start pulling in unrelated redesign work
- Testing expansion turns into a full harness migration
- Structural cleanup begins changing gameplay behavior “while we are here”

### Prevention Strategy

- Keep each phase tied to a specific maintenance outcome
- Prefer extraction and isolation over behavioral rewrite
- Treat “new system” ideas as out-of-scope unless explicitly promoted

### Phase Fit

- Phase 1-4 guardrail across the entire roadmap

## Pitfall 2: Fixing UI Without Verifying In Godot

### Warning Signs

- Layout changes rely only on code inspection or build success
- Dragging logic is changed without scene/runtime validation
- Control overlap is “probably fixed” but not exercised at different window sizes

### Prevention Strategy

- Require manual Godot verification for any main bar / submenu interaction changes
- Keep verify steps explicit in phase acceptance

### Phase Fit

- Phase 1: Main UI stabilization

## Pitfall 3: Adding Tests Far From The Real Risk

### Warning Signs

- New tests only cover easy helper functions while unstable runtime logic remains unprotected
- Tests assert implementation trivia rather than user-relevant outcomes or critical rule boundaries

### Prevention Strategy

- Add tests around deterministic, maintenance-sensitive rules
- Expand coverage where recent bugfixes or runtime couplings were touched

### Phase Fit

- Phase 2: Regression expansion

## Pitfall 4: Breaking Save Compatibility Indirectly

### Warning Signs

- UI state or runtime state names change without checking read/write symmetry
- Runtime fixes incidentally rename keys, section ownership, or node-path assumptions

### Prevention Strategy

- Treat `PrototypeRootController` and `docs/SAVE_SYSTEM.md` as a pair
- If persistence shape changes, update both code and docs in the same phase

### Phase Fit

- Phase 3: Runtime safety / persistence cleanup

## Pitfall 5: Letting Developer Tools Leak Into Player UX

### Warning Signs

- Validation/debug overlays become visible during normal play
- Maintenance helpers compete for screen space with core gameplay controls

### Prevention Strategy

- Keep config validation explicitly developer-only
- Default hidden unless actively diagnosing a problem

### Phase Fit

- Phase 1 and Phase 3

## Pitfall 6: Accidentally Expanding Into Full Asset Integration

### Warning Signs

- “Obvious display anomaly” fixes turn into broad asset replacement work
- Manual asset tasks from `docs/design/10_todo.md` get pulled into the maintenance scope without deliberate approval

### Prevention Strategy

- Limit visual work to current readability or correctness issues
- Keep `[MANUAL-ASSET:*]` tasks separated from maintenance phases unless intentionally promoted

### Phase Fit

- Phase 4: Bounded display cleanup

## Pitfall 7: Trusting Build Green As If It Means Runtime Green

### Warning Signs

- `dotnet build` passes, but scene layout, drag behavior, or save restore still fail in Godot
- Team confidence grows from command-line success alone

### Prevention Strategy

- Treat build/test/manual-Godot verification as separate evidence streams
- Keep `just verify` and manual runtime checks part of the definition of done

### Phase Fit

- All phases
