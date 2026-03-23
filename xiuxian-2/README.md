# Xiuxian 2

## Current Playable Loop

The current prototype already supports a minimal but real gameplay loop:

- keyboard/mouse activity drives exploration progress
- monsters advance toward the player and trigger combat on proximity
- battle rounds resolve from accumulated input counts
- combat grants resources and recent battle logs
- first clears can grant fixed equipment rewards
- new equipment goes into backpack first and is only equipped manually
- equipped gear changes player combat stats and persists in save data

## Regression Tests

Run the current automated regression suite from the project root:

```bash
./run-regression-tests.sh
```

GitHub Actions also runs this same regression suite automatically on pushes to `main` and on pull requests.

This currently covers the refactor-stable core rules around:

- input-driven exploration progress
- 100% level completion switching semantics
- battle start, round, lifecycle, and reward formalization
- pity, soft-cap, and daily-cap behavior
- player / monster / equipment stat pipeline
- starter equipment and manual equip loop

The underlying test project lives at `tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj`.

For a fuller coverage summary and current testing gaps, see `TESTING.md`.

## Still Prototype-Only

- equipment acquisition is fixed-rule and debug-oriented, not full content-driven loot yet
- equipment UI is minimal and focused on verification, not final UX
- cloud sync, online features, and richer pet systems remain out of current product scope
