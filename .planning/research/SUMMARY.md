# Project Research Summary

**Project:** Xiuxian 2
**Domain:** Brownfield automated testing foundation for a Godot 4 + C# desktop game
**Researched:** 2026-03-21
**Confidence:** MEDIUM-HIGH

## Executive Summary

Xiuxian 2 is not a greenfield test project; it is a shipped, service-heavy Godot 4 + C# desktop game whose biggest maintainability problem is large autoload and controller classes carrying config parsing, persistence, progression, drop logic, and runtime orchestration together. The research is consistent on the right expert approach: build a layered test foundation around pure C# business logic first, keep Godot `Node` autoloads as thin runtime facades, and reserve engine-backed tests for wiring, signals, scene boot, and a very small number of runtime-only contracts.

The recommended implementation path is a two-layer suite: fast `dotnet test`-driven xUnit tests as the default safety net, plus a thin GdUnit4Net or headless-Godot integration layer for autoload/bootstrap verification. Roadmap work should start by establishing a single repeatable CLI entry point, extracting seams around file IO, RNG, time, platform hooks, and cloud/runtime dependencies, and then freezing serialization/config behavior with fixtures before major refactors. That sequence creates immediate regression value without forcing a risky big-bang architecture rewrite.

The main risks are architectural rather than tool-related. The biggest failure mode is writing lots of scene-heavy tests against the live autoload graph, which would create a slow and brittle suite while leaving core rules unsafe to refactor. The second major risk is changing save/config behavior while splitting monoliths such as `LevelConfigLoader`. Both are mitigated by a pyramid-shaped suite: many pure-domain tests, explicit round-trip snapshot/config fixtures, fail-fast smoke tests for missing autoloads, and isolated platform-specific validation instead of making Windows hooks and frame timing part of the default feedback loop.

## Key Findings

### Recommended Stack

The stack recommendation is clear and pragmatic: use xUnit v3 as the main test framework for extracted pure logic and use GdUnit4Net only where Godot runtime behavior is the thing being tested. The best architecture is not "pick one framework for everything"; it is a split execution model where most of the suite stays in plain .NET and only a small integration layer pays the engine/runtime cost.

**Core technologies:**
- `xunit.v3` `2.0.3` + `xunit.runner.visualstudio` `3.1.1`: primary fast suite for service logic and regression tests — best fit for .NET 8 and `dotnet test`
- `Microsoft.NET.Test.Sdk` `17.14.1+`: test discovery and execution glue — required floor for stable CLI/IDE integration and GdUnit4 adapter compatibility
- `gdUnit4.api` `5.0.0` + `gdUnit4.test.adapter` `3.0.0`: Godot-aware integration coverage — use only for signals, autoload wiring, scene boot, and runtime-only behavior
- `coverlet.collector` `6.0.4`: coverage reporting — add after the suite is useful, as visibility rather than a success metric
- `NSubstitute` `5.3.0`: seams for file IO, clock, RNG, OS hooks, and cloud wrappers — mock boundaries, not domain logic
- `Shouldly` `4.3.0`: optional readable assertions — good expressive failures without FluentAssertions licensing ambiguity
- `Directory.Packages.props` + shared `.runsettings`: version and runner stability — centralize package management and pin Godot runtime settings such as `GODOT_BIN`, `--headless`, and `MaxCpuCount=1`

### Expected Features

The v1 feature set is deliberately narrow: establish a reliable automated test foundation that improves refactor safety for service-heavy systems, especially config, persistence, progression, and reward logic. Research consistently rejects UI-first or end-to-end-first automation and instead prioritizes deterministic service regression coverage plus a small amount of boot/wiring validation.

**Must have (table stakes):**
- Repeatable CLI/headless test entry point — one authoritative run path for local and CI use
- Assertion-based service regression tests — especially for config parsing, progression, resources, rewards, and validation
- Round-trip persistence and config fixture tests — freeze `ToDictionary`/`FromDictionary` and representative config behavior early
- Test seams for filesystem, time, RNG, platform hooks, and cloud/runtime boundaries — required for deterministic tests
- Small autoload/bootstrap smoke suite — catches broken singleton wiring pure logic tests will miss
- Stable fixtures/builders — reusable state, save payload, and config data instead of ad hoc dictionaries

**Should have (competitive):**
- Characterization tests for monolith extraction — freeze behavior before splitting `LevelConfigLoader` and similar services
- Golden-master config regression fixtures — protect complex config-driven flows where exact outputs matter
- Dual-speed suite discipline — fast logic tests by default, runtime only when explicitly needed
- Contract tests around extracted service boundaries — preserve behavior while modularizing autoload facades
- Coverage reports and filtered CI lanes — useful once the suite has enough volume to need filtering and visibility

**Defer (v2+):**
- Property/fuzz testing for invariants — valuable after baseline deterministic coverage exists
- Windows-only hook smoke lane in CI — useful but should not block initial trust in the suite
- UI or gameplay end-to-end automation — low first-milestone ROI and high flake risk for this project

### Architecture Approach

The architecture recommendation is to turn the current autoload-heavy runtime into a layered system rather than trying to test or rewrite it in place. Keep existing autoload `Node`s and controllers as thin facades/adapters, extract durable rules into pure C# services and state machines, introduce explicit contracts for unstable boundaries, and push save/load boundaries through typed snapshot DTOs instead of treating `Dictionary<string, Variant>` as the business model.

**Major components:**
1. Autoload facades — preserve `/root/...` access, signals, lifecycle, and compatibility while delegating logic downward
2. Domain/core services — own config parsing, progression, rewards, AP/input rules, battle/explore policies, and snapshot logic as pure C#
3. External adapters — isolate filesystem, config loading, RNG, clock/time, logging, platform hooks, and cloud behavior behind interfaces
4. Scene orchestration adapters — keep scene controllers focused on node binding, signal hookup, and UI/runtime translation
5. Fixtures/builders and runtime smoke tests — provide representative config/save data, deterministic fakes, and minimal boot verification

### Critical Pitfalls

1. **Building tests on the live autoload graph** — avoid by extracting seams first and keeping scene/autoload boot tests to a thin smoke layer
2. **Allowing warning-driven partial initialization to pass green** — avoid by making smoke tests fail fast on missing services, nodes, or noisy Godot warnings
3. **Refactoring monoliths before freezing serialization/config contracts** — avoid by adding round-trip fixtures and golden inputs/outputs before structural changes
4. **Automating Windows hooks and frame-driven flows too early** — avoid by starting with deterministic domain slices and isolating platform/runtime tests into opt-in lanes
5. **Confusing engine integration coverage with refactor safety** — avoid by writing most assertions against business rules, typed snapshots, and contract behavior rather than scene state

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: Test Harness and Deterministic Seams
**Rationale:** The project needs a single execution model and controllable dependencies before any meaningful regression suite can scale.
**Delivers:** `Directory.Packages.props`, test project scaffolding, shared runsettings, initial `dotnet test` entry point, and first seams for file IO, RNG, clock, platform hooks, and cloud/runtime boundaries.
**Addresses:** Repeatable CLI entry point, test seams around external boundaries, stable test categorization, and fixture/building groundwork.
**Avoids:** Fragmented execution, live-autoload-first testing, and flake from implicit environment dependencies.

### Phase 2: Serialization and Config Contract Coverage
**Rationale:** Save/load and config interpretation are the highest-risk behavior to preserve before refactoring large services.
**Delivers:** Round-trip tests for `ToDictionary`/`FromDictionary` and runtime dictionary methods, representative config/save fixtures, typed snapshot DTO boundaries, and first regression coverage for `LevelConfigLoader` parser/validator/drop/unlock logic.
**Uses:** xUnit, Shouldly, Coverlet-ready fixtures, and deterministic fakes.
**Implements:** Snapshot DTO boundary, config parser/validator services, and fixture/builders under `tests/Fixtures/` and `tests/Builders/`.
**Avoids:** Behavior drift during refactors, string-key fragility, and false confidence from manual save/load checks.

### Phase 3: Core Service Extraction and Contract Tests
**Rationale:** Once contracts are frozen, the roadmap can safely split the highest-risk monoliths into testable pure services.
**Delivers:** Facade-over-core refactors for `LevelConfigLoader`, `InputActivityState`, and early extraction work around persistence and aggregation logic, plus contract tests for stable service behavior.
**Addresses:** Characterization tests, contract tests, and the main maintainability goal of making large service files safe to split.
**Uses:** Autoload facade pattern, constructor-injected policies, and domain/core class extraction.
**Avoids:** Big-bang DI rewrites, controller-only modularization, and over-mocking Godot internals.

### Phase 4: Runtime Wiring and Minimal Smoke Coverage
**Rationale:** Runtime verification matters, but only after most correctness is already protected in fast deterministic tests.
**Delivers:** Sparse GdUnit4Net/headless-Godot tests for autoload registration, signal contracts, `PrototypeRootController` and `ExploreProgressController` boot/binding, and platform fallback smoke behavior.
**Addresses:** Autoload/bootstrap smoke coverage, selected runtime-required tests, and explicit validation of required services.
**Uses:** `gdUnit4.api`, `gdUnit4.test.adapter`, shared `.runsettings`, and headless Godot execution.
**Implements:** Thin Godot integration layer over the already-extracted core.
**Avoids:** Full-scene regression suites, hidden warning-driven partial init, and making Windows/platform concerns part of every local run.

### Phase 5: Suite Maturity and Specialized Lanes
**Rationale:** Only after the foundation proves useful should the roadmap add broader visibility and more advanced regression techniques.
**Delivers:** Coverage publishing, filtered CI lanes, optional Windows-only smoke jobs, golden-master characterization depth, and later invariant/property testing.
**Addresses:** Coverage visibility, filtered execution, platform-segregated validation, and post-MVP hardening.
**Avoids:** Premature optimization, coverage-as-goal behavior, and overloading the first milestone with advanced test strategies.

### Phase Ordering Rationale

- The order follows hard dependencies: execution model and seams must exist before fixtures and contract tests; contracts must exist before service extraction; runtime smoke tests should validate the thin shell after behavior is already protected underneath.
- The grouping matches the recommended architecture: boundaries first, DTO/contracts second, service extraction third, runtime verification fourth.
- This sequencing directly counters the top pitfalls by preventing scene-heavy-first testing, fail-open smoke coverage, and refactors that change persistence/config behavior before it is locked down.

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 4:** Validate exact Godot 4.5.1 plus GdUnit4Net adapter compatibility, headless runner commands, and warning-capture strategy before standardizing runtime tests.
- **Phase 5:** Windows-only hook smoke jobs and later property/fuzz testing need targeted planning because they depend on environment provisioning and specialized tooling choices.

Phases with standard patterns (skip research-phase):
- **Phase 1:** .NET test-project setup, package centralization, and deterministic seam introduction are well-documented and low-ambiguity.
- **Phase 2:** Fixture-backed serialization/config regression coverage follows established patterns and is strongly supported by both repo evidence and official .NET testing guidance.
- **Phase 3:** Facade-over-core extraction and constructor-injected policy seams are opinionated but well-supported by both Godot best practices and brownfield refactor patterns.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Strongest area; official .NET docs, NuGet package data, and GdUnit maintainers all align on the layered toolchain and version floor. |
| Features | MEDIUM | Feature priorities fit repo goals well, but some differentiators rely more on brownfield testing judgment than official prescriptive guidance. |
| Architecture | MEDIUM-HIGH | Architecture guidance is well-supported by repo structure and Godot best-practice themes, though exact extraction boundaries still need local implementation judgment. |
| Pitfalls | MEDIUM | Pitfalls are highly plausible and grounded in repo evidence, but they are synthesized risk analysis rather than directly vendor-documented prescriptions. |

**Overall confidence:** MEDIUM-HIGH

### Gaps to Address

- **Runtime tool compatibility:** Confirm the exact GdUnit4 plugin/package combination and headless execution flow against the local Godot 4.5/4.5.1 setup before making runtime tests a required gate.
- **Core library boundary shape:** Decide during planning whether extracted pure services live inside the existing Godot project or in a separate plain C# library referenced by the Godot `.csproj`.
- **Serialization versioning policy:** Research shows typed snapshots are the right direction, but planning still needs an explicit migration/versioning policy for legacy save payloads.
- **Warning/error capture strategy:** Define how Godot warnings become actionable test failures so smoke tests do not silently pass on partial initialization.
- **Platform smoke scope:** Clarify how much Windows-hook and cloud behavior should be covered by automation versus manual validation in the first roadmap.

## Sources

### Primary (HIGH confidence)
- `.planning/PROJECT.md` — project scope, constraints, active goals, and out-of-scope lines for the milestone
- `.planning/research/STACK.md` — package/version recommendations and execution model
- `.planning/research/FEATURES.md` — MVP scope, differentiators, and anti-features
- `.planning/research/ARCHITECTURE.md` — layered design, seam locations, extraction order, and test pyramid guidance
- `.planning/research/PITFALLS.md` — brownfield failure modes and phase-specific prevention strategies
- https://xunit.net/docs/getting-started/v3/getting-started — xUnit v3 and `.NET 8` workflow validation
- https://www.nuget.org/packages/Microsoft.NET.Test.Sdk — test SDK version floor and compatibility
- https://www.nuget.org/packages/coverlet.collector — coverage collection workflow and version baseline
- https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test — authoritative `dotnet test` execution model

### Secondary (MEDIUM confidence)
- https://github.com/godot-gdunit-labs/gdUnit4/blob/master/README.md — Godot-aware testing capabilities and compatibility notes
- https://github.com/godot-gdunit-labs/gdUnit4/releases/tag/v6.0.2 — Godot 4.5-era compatibility details
- https://www.nuget.org/packages/gdUnit4.api — C# runtime opt-in model and package metadata
- https://www.nuget.org/packages/gdUnit4.test.adapter — `.runsettings`, `GODOT_BIN`, and IDE/CLI adapter behavior
- https://docs.godotengine.org/en/stable/tutorials/best_practices/node_alternatives.html — guidance against putting all logic in nodes
- https://docs.godotengine.org/en/stable/tutorials/best_practices/autoloads_versus_internal_nodes.html — autoload design guidance relevant to this repo
- https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html — CLI/headless execution patterns for Godot

### Tertiary (LOW confidence)
- Repo-specific future choices around exact save migration/versioning, warning-to-failure handling, and Windows CI provisioning — direction is clear, but implementation details need validation during roadmap planning.

---
*Research completed: 2026-03-21*
*Ready for roadmap: yes*
