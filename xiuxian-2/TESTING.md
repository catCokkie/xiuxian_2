# Testing

## Automated Regression Entry Point

From the project root, run:

```bash
./run-regression-tests.sh
```

This executes:

```bash
dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj
```

The same command is also run automatically in GitHub Actions on pushes to `main` and on pull requests.

## What Is Covered

The current regression suite focuses on refactor-stable core rules rather than scene-bound Godot UI behavior.

Covered areas:

- input-driven exploration progress advancement
- 100% progress completion reset semantics
- unlocked-level rotation behavior
- boss-clear next-level resolution
- battle start setup and proximity-trigger decisions
- battle input-threshold consumption and remaining-input accounting
- battle round outcome and lifecycle mapping
- battle settlement reward normalization
- drop pity trigger and counter reset
- daily-cap and soft-cap drop economy behavior
- drop-table binding resolution by level and monster
- input activity decay multiplier behavior
- input activity per-minute soft-cap multiplier behavior
- AP accumulator drain and floor-at-zero behavior
- player base stat scaling by realm level
- player / monster / equipment unified stat pipeline
- starter equipment loadout and manual equip-loop rules
- reward-side fixed first-clear equipment generation rules

## What Is Not Yet Covered

Important gaps still outside automated regression:

- Godot scene wiring and `.tscn` node-path integrity
- submenu/main-bar UI interactions and visibility toggles
- real battle round presentation and actor-slot visuals
- end-to-end save/load flows through `PrototypeRootController` using live Node integration
- `LevelConfigLoader` full JSON parsing as a black-box Node integration path
- global input hook integration and desktop environment behavior
- cloud sync and other external-service paths
- full inventory UX, item browsing, and non-debug equipment management flows

## Testing Strategy

Current approach is intentional:

- keep volatile Godot scene behavior out of the baseline suite
- move stable gameplay and anti-abuse math into pure rule classes
- test those rule classes directly so future refactors can move UI and Node wiring without weakening regression protection

## When Adding New Tests

Prefer adding tests when the behavior is:

- deterministic or easy to control
- core to progression, rewards, or anti-abuse rules
- likely to break during refactors
- expressible without depending on live scene trees

If a feature is mostly scene/UI driven, first consider extracting a pure rule or decision layer and then testing that layer.
