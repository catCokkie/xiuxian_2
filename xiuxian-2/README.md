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

The current UI surface also includes:

- a bottom main bar for lightweight always-on progress visibility
- a book-style submenu with `Cultivation`, `Battle Log`, `Equipment`, `Stats`, `Bug Feedback`, and `Settings`
- manual breakthrough when realm progress is full
- persisted recent battle logs and basic local feedback export tools

Current product boundaries:

- progression is real and persistent, but still prototype-oriented
- equipment flow is intentionally minimal and uses fixed-rule rewards
- some settings are intentionally marked as reserved/experimental rather than fully wired product features

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
- config validation currently exists through debug tooling, not a polished standalone panel yet
- some anti-abuse goals are only partially implemented beyond current decay and cap rules
- cloud sync, online features, and richer pet systems remain out of current product scope

## Sync To Server

If the server has unstable `git fetch/pull`, use local-to-server rsync sync instead.

1. Copy the example env file and fill in your server info:

```bash
cp .sync-to-server.env.example .sync-to-server.env
```

2. Run the sync script from the project root:

```bash
./sync-to-server.sh
```

Useful options:

- set `SYNC_DRY_RUN=true` to preview changes first
- set `SYNC_DELETE=true` only when you want remote stale files removed

This workflow treats GitHub/local as the source of truth and the server as a runtime/build target.
