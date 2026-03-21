# Phase 2 Research - Config and Serialization Contracts

**Phase:** 2
**Requirements:** TEST-03, TEST-04
**Mode:** standard planning
**Context source:** roadmap, requirements, project state, codebase map, Phase 1 summaries, repo inspection
**Researched:** 2026-03-21

## Recommended Direction

Phase 2 should freeze two contract surfaces without expanding into a broad runtime refactor: fixture-backed `LevelConfigLoader` parsing and validation behavior, then unified save snapshot behavior around `PrototypeRootController` and the existing `ToDictionary` / `FromDictionary` runtime state pattern. The safest path is still the Phase 1 default loop: keep tests plain `dotnet test`, prefer runtime-free helpers when Godot `Node` construction would hang, and add frozen fixtures under `tests/Xiuxian2.Core.Tests/Fixtures/` instead of reading mutable runtime assets.

## Standard Stack

- Primary fast suite: `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`
- Existing seam support: `IConfigSource`, `IRng`, `IClock`, `IFileSystem`, `ServiceFixtureBuilder`, deterministic fakes under `tests/Xiuxian2.Core.Tests/Support/Deterministic/`
- Shared runner config: `tests/.runsettings`
- Fixture strategy: add phase-owned config and save snapshots under `tests/Xiuxian2.Core.Tests/Fixtures/config/` and `tests/Xiuxian2.Core.Tests/Fixtures/save/`

## Boundary Targets In Current Code

- `xiuxian-2/scripts/services/LevelConfigLoader.cs`
  - already supports deterministic seams through `UseTestSeams(...)`
  - exposes contract-facing outputs through `BuildValidationSummary`, `GetValidationIssues`, `GetValidationEntries`, `ToRuntimeDictionary`, and `FromRuntimeDictionary`
  - is the highest-risk config boundary named in the roadmap and state
- `xiuxian-2/scripts/game/PrototypeRootController.cs`
  - owns unified save persistence at `user://save_state.cfg`
  - freezes the current write contract with `SaveSchemaVersion = 5`
  - delegates save/load by section through `Read*` / `Write*` methods for UI, services, settings, level runtime, and explore runtime state
- Serializable service/state nodes
  - `BackpackState`, `ResourceWalletState`, `PlayerProgressState`, `InputActivityState`, `PlayerActionState`
  - each already exposes `ToDictionary` / `FromDictionary` and can be locked down with regression tests before broader save work

## Snapshot And Versioning Policy

- Treat schema version `5` as the authoritative write contract for Phase 2.
- Add golden fixtures for:
  - one current-schema snapshot that must round-trip without losing required progression/runtime state
  - one curated legacy snapshot that proves current read logic still migrates known older payload shape safely
- Keep reads backward-compatible for curated legacy fixtures only; do not build a generalized migration framework in this phase.
- Keep writes single-version only: every new save emitted by the app must still write version `5` and the existing section names.

Why this is the lowest-risk choice:
- it matches the current controller design instead of forcing a system rewrite during a testing milestone
- it creates hard regression fixtures for both present and legacy behavior
- it prevents Phase 2 from turning into open-ended migration architecture work

## Planning Implications

- Use TDD for code-producing tasks: add failing contract tests first, then minimum implementation or extraction to pass.
- Keep `LevelConfigLoader` and `PrototypeRootController` as the runtime-facing facades; extract pure helpers only when needed to keep the CLI loop runtime-free.
- Freeze current key names and section names before refactoring internals.
- Use fixtures owned by the test project rather than `res://docs/design/...` or live `user://` save files.

## Do Not Hand-Roll

- Do not introduce a broad serializer replacement or a new save format for this phase.
- Do not convert Phase 2 into full runtime smoke or scene boot coverage; that belongs to Phase 4.
- Do not make legacy migration support open-ended across arbitrary unknown versions; cover curated fixtures only.
- Do not test save contracts by writing to real `user://save_state.cfg` in the default suite.

## Common Pitfalls To Prevent In Plans

- Verifying config behavior only through one happy-path fixture and missing validation-entry regressions
- Treating `BuildValidationSummary()` text as the only contract instead of also pinning structured validation entries
- Freezing service dictionary payloads without checking missing-key defaults and normalization/clamping behavior
- Keeping save contract logic trapped inside `PrototypeRootController` where plain xUnit cannot exercise it cleanly
- Changing save key names or section layout while adding tests, which would defeat the purpose of contract freezing

## Validation Architecture

### Default Feedback Loop

- Quick command: `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj`
- Full phase command: `dotnet test xiuxian-2/xiuxian2.sln --settings tests/.runsettings`
- Expected default runtime target: fast core tests only; Phase 2 should stay runtime-free in the default loop

### Validation Expectations For Phase 2

- Every plan task must have an automated verification command.
- Config contract tests must use frozen fixtures and assert structured validation outputs, not only boolean load success.
- Save contract tests must prove both current-schema round-trip behavior and curated legacy read compatibility.
- If Godot `Node` instantiation hangs in xUnit, move the contract behavior into a runtime-free helper while preserving the current public runtime facade.

### Plan Breakdown Recommendation

1. Config contract plan: freeze representative valid and invalid `LevelConfigLoader` inputs with fixture-backed regression tests.
2. State serialization plan: freeze `ToDictionary` / `FromDictionary` behavior for the core save-participating services.
3. Unified save contract plan: extract a runtime-free save snapshot helper from `PrototypeRootController`, pin version `5` output, and cover curated legacy snapshot migration.

## Research Conclusion

Phase 2 does not need external-library discovery. The repo already has the necessary xUnit harness and deterministic seams. The main planning decision is scope discipline: freeze current config/save contracts with fixtures and helper extractions, but do not broaden into runtime automation or serializer redesign.

---

*Ready for planning: yes*
