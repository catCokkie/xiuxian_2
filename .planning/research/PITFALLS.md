# Pitfalls Research

**Domain:** Brownfield automated testing and refactor-safety for a Godot 4 + C# game with autoload singletons and runtime-heavy services
**Researched:** 2026-03-21
**Confidence:** MEDIUM

## Critical Pitfalls

### Pitfall 1: Building tests on the live autoload graph instead of carving seams first

**What goes wrong:**
Teams copy the current manual harness pattern and write more tests that boot the whole scene tree, resolve `/root/...` singletons, and depend on production startup order. The suite becomes slow, hard to debug, and too fragile to support aggressive refactors.

**Why it happens:**
In brownfield Godot projects, autoloads are already the easiest thing to reach, and Godot's scene tree model makes it tempting to treat integration wiring as the smallest testable unit. This repo already follows that pattern in `xiuxian-2/scripts/tests/InputSystemTest.cs`, `xiuxian-2/scripts/game/PrototypeRootController.cs`, and `xiuxian-2/scripts/game/ExploreProgressController.cs`.

**How to avoid:**
Create a narrow seam layer before adding lots of tests: extract pure C# classes for config parsing, reward resolution, pity counters, and save DTO mapping; keep autoload `Node`s as thin adapters; reserve scene/autoload boot tests for a small smoke layer. Treat `LevelConfigLoader` and `ExploreProgressController` as facades to shrink, not as primary unit-test subjects.

**Warning signs:**
- New tests instantiate `PrototypeRoot.tscn` or depend on multiple autoloads just to assert one rule.
- Refactors require editing many tests because node paths or startup order changed.
- Test failures only reproduce inside a full Godot run and provide poor fault localization.

**Phase to address:**
Phase 1 - Test harness and dependency seams.

---

### Pitfall 2: Treating warning-driven partial initialization as "good enough" test behavior

**What goes wrong:**
Tests pass while the runtime is actually missing required services or nodes, because production code logs warnings and keeps running. That creates false confidence and hides broken composition until manual playtesting.

**Why it happens:**
This codebase favors `GetNodeOrNull(...)` plus `GD.PushWarning(...)` across boot and service code, especially in `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/scripts/game/ExploreProgressController.cs`, and `xiuxian-2/scripts/services/ActivityConversionService.cs`. That is reasonable for player resilience, but dangerous for regression tests.

**How to avoid:**
Add explicit test-only startup validation that fails fast when required autoloads, scene nodes, or exported `NodePath`s are missing. Convert critical boot assumptions into assertions in smoke tests, and capture Godot errors/warnings as failures for the test suite's integration layer.

**Warning signs:**
- Test logs contain `GD.PushWarning` or `Node not found` messages but the suite still reports green.
- A service returns early from `_Ready()` and tests continue anyway.
- Smoke tests verify "no crash" instead of verifying required services are actually wired.

**Phase to address:**
Phase 1 - Test harness and dependency seams.

---

### Pitfall 3: Refactoring giant services before freezing serialization and config contracts

**What goes wrong:**
Teams split god classes first, then discover save payloads, runtime dictionaries, or config interpretation changed in subtle ways. Progression, pity counters, unlock state, and battle resume behavior drift even though the code looks cleaner.

**Why it happens:**
Brownfield Godot projects often persist state through ad hoc `Dictionary<string, Variant>` payloads instead of typed contracts. This repo does that in `InputActivityState`, `BackpackState`, `ResourceWalletState`, `PlayerProgressState`, `PlayerActionState`, `LevelConfigLoader`, and `ExploreProgressController`, with writes coordinated by `xiuxian-2/scripts/game/PrototypeRootController.cs`.

**How to avoid:**
Before structural refactors, lock down round-trip tests and golden fixtures for `ToDictionary`/`FromDictionary` and `ToRuntimeDictionary`/`FromRuntimeDictionary`. Add fixture-based tests for representative config JSON and save files, including missing keys, old versions, and impossible values. Refactor only after those invariants are executable.

**Warning signs:**
- Developers say "the format is internal so we can clean it up later."
- Refactor PRs touch save/load code without new round-trip tests.
- Manual regression checklists mention save/load, but no fixture-backed assertions exist.

**Phase to address:**
Phase 2 - Serialization and config regression coverage.

---

### Pitfall 4: Trying to automate platform hooks and runtime-heavy flows before isolating deterministic logic

**What goes wrong:**
The first automated tests target Windows hooks, input capture timing, deferred UI refreshes, and frame-driven battle loops. The result is flaky, platform-specific tests that burn trust in automation before the suite proves value.

**Why it happens:**
The most visible gameplay behavior is the runtime loop, and the repo's only test artifact is already an in-engine input harness. But `InputHookService` depends on Win32 hooks and fallback input behavior, while controllers rely on signals, `_Process`, `_Input`, `CallDeferred`, and scene timing.

**How to avoid:**
Start with deterministic domain slices: AP conversion rules, level unlocks, reward rolls, pity counters, config validation, and dictionary mapping. For `InputHookService`, add a narrow adapter seam around the OS hook layer and test hook availability/fallback behavior separately with a tiny integration set. Keep global-input and scene-timing coverage out of the first milestone.

**Warning signs:**
- First test milestones require Windows-only runners.
- Developers add sleeps, frame waits, or retry loops to make tests pass.
- Flaky failures cluster around `_Process`, `_Input`, signals, or hook activation timing.

**Phase to address:**
Phase 1 - Test harness and dependency seams, then Phase 4 - Platform and scene smoke coverage.

---

### Pitfall 5: Confusing engine integration tests with refactor-safety tests

**What goes wrong:**
The project gains some passing Godot scene tests but still cannot safely split `LevelConfigLoader` or `ExploreProgressController`, because the tests only prove the current wiring works, not that the underlying rules are preserved.

**Why it happens:**
Godot encourages testing through scenes and signals, and tools like GdUnit4 make scene tests convenient. But refactor safety in a brownfield service layer comes from stable behavioral contracts around pure logic, not from re-running the same scene orchestration path.

**How to avoid:**
Define refactor targets and write tests against those contracts first: config indexing, unlock progression, pity reset behavior, reward distribution rules, resume invariants, and validation output. Keep a pyramid: many pure/domain tests, fewer service tests, very few scene tests.

**Warning signs:**
- Test count grows, but large services still feel unsafe to split.
- Most assertions check labels, node state, or signal wiring instead of business rules.
- A code move with no logic change still breaks many tests.

**Phase to address:**
Phase 2 - Serialization and config regression coverage, then Phase 3 - Service extraction.

---

### Pitfall 6: Leaving time, randomness, and content loading implicit

**What goes wrong:**
Tests become non-deterministic because they rely on wall-clock time, random drop outcomes, live `GD.Load` resource state, or mutable content files. Failures are intermittent and hard to reproduce.

**Why it happens:**
Game runtime code often reaches directly into engine APIs for time, RNG, and resource loading. In this repo, save timestamps, reward simulation, pity/daily/hourly counters, and portrait/config loading all depend on runtime state inside large services.

**How to avoid:**
Introduce injectable seams for time, RNG, and resource existence checks. Freeze test fixtures for config JSON and representative save payloads. For reward logic, test deterministic inputs and seeded randomness, plus a separate statistical/simulation suite that is allowed to run slower and less often.

**Warning signs:**
- Tests assert probabilistic outcomes from a single run.
- Failures disappear when rerun without code changes.
- Test data reads live `docs/design/*.json` files that designers are actively editing.

**Phase to address:**
Phase 2 - Serialization and config regression coverage.

---

### Pitfall 7: Adding a test framework without deciding how it fits Godot and .NET execution

**What goes wrong:**
The team installs a framework, but test execution remains fragmented: some tests require the editor, some run through `dotnet test`, some only work in CI, and nobody knows which suite is authoritative.

**Why it happens:**
Godot and .NET have overlapping test ecosystems. Official .NET testing assumes `dotnet test`, while Godot-oriented frameworks such as GdUnit4 can run inside Godot and also provide .NET/VSTest integration. Brownfield teams often add tooling before defining a layered strategy.

**How to avoid:**
Choose one explicit split early: pure C# domain tests should run fast from the .NET CLI, while Godot `Node`/scene tests should run through a Godot-aware runner such as GdUnit4 or a dedicated headless Godot command. Document which layer owns which class of regression, and make CI run both layers intentionally instead of ad hoc.

**Warning signs:**
- Engineers ask "Should this run in Godot or in dotnet?" for every new test.
- CI only builds the solution and calls that test coverage.
- Test setup docs are longer than the actual first regression suite.

**Phase to address:**
Phase 1 - Test harness and dependency seams.

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Keep `Node` autoloads as the only place business logic lives | No design work up front | Pure logic never becomes cheap to test or refactor | Only as a temporary wrapper while extracting pure services |
| Use live `/root/...` lookups in every test | Fast to match current runtime | Tests lock in scene tree and autoload names instead of behavior | Acceptable only for a tiny smoke-test layer |
| Assert logs or visible UI instead of rule outputs | Easy to observe in Godot | Poor fault localization and fragile tests | Acceptable only for diagnostic/UX coverage |
| Reuse production JSON and save files directly as fixtures | No fixture maintenance | Tests churn whenever content authors edit files | Acceptable only after copying frozen snapshots into test assets |
| Add retries/sleeps around timing issues | Makes flakes appear to stop | Suite stays nondeterministic and slow | Never acceptable for core regression tests |

## Integration Gotchas

Common mistakes when connecting to external services.

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Godot autoloads | Assuming test setup can omit a singleton because production only warns | Fail fast in smoke tests when required autoloads are absent |
| Godot scene tree | Binding tests to fragile node paths and child order | Centralize required node references and keep scene tests shallow |
| Windows global hooks | Running hook-dependent tests as normal unit tests | Put OS hook behavior behind an adapter and cover it with a minimal Windows-only integration suite |
| `dotnet test` and Godot tests | Mixing CLI and editor-driven suites without ownership rules | Separate pure .NET tests from Godot-aware integration tests and document the boundary |
| GdUnit4 adoption | Assuming the framework alone solves architecture debt | Use the framework after extracting deterministic seams; tooling does not replace design work |

## Performance Traps

Patterns that work at small scale but fail as usage grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Full-scene boot for most tests | Slow local runs, skipped test execution, frequent fixture churn | Keep most tests in pure C# and reserve scene boot for smoke coverage | Breaks as soon as the suite reaches dozens of tests |
| Statistical reward tests in the default suite | Long runtimes and intermittent failures | Split deterministic rule tests from slower simulation checks | Breaks once CI starts running every branch |
| Re-importing or live-loading content/resources in each test | Headless/editor runs become IO-bound | Freeze test fixtures and cache reusable assets per suite | Breaks when config and asset coverage expands |
| Global save/config setup per test case | Heavy setup/teardown and hard-to-read failures | Build focused fixtures per subsystem and avoid whole-app persistence setup | Breaks once save/load tests cover multiple systems |

## Security Mistakes

Domain-specific security issues beyond general web security.

| Mistake | Risk | Prevention |
|---------|------|------------|
| Running tests against the real `user://` save path | Tests corrupt or normalize developer save data, hiding real migration bugs | Redirect save paths or isolate user data per test run |
| Exercising global input capture in unattended environments without clear gating | CI or shared machines may capture system-wide input unexpectedly | Disable real hooks by default in automation and require explicit opt-in for hook tests |
| Treating tamper-prone save data as a low-priority test area | Refactors can widen exploit or corruption paths without detection | Add save schema, bounds, and impossible-state regression tests early |

## UX Pitfalls

Common user experience mistakes in this domain.

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Developers cannot tell which tests are fast, slow, or platform-specific | The suite gets avoided because feedback is unpredictable | Label suites by layer and platform, and make fast tests the default path |
| Test failures only show Godot console noise | Engineers waste time reproducing issues manually | Turn boot assumptions and domain invariants into explicit assertions with fixture names |
| Coverage focuses on debug panels and labels first | Users still hit progression/save regressions after refactors | Prioritize invisible but high-risk service rules before UI presentation |

## "Looks Done But Isn't" Checklist

- [ ] **Test foundation:** It is not done if tests still depend on production autoload names for most assertions - verify pure-domain seams exist.
- [ ] **Serialization safety:** It is not done if save/config refactors lack round-trip fixtures - verify golden inputs and outputs are versioned.
- [ ] **Platform coverage:** It is not done if hook behavior only passed on one developer machine - verify fallback and unsupported-platform behavior too.
- [ ] **Refactor safety:** It is not done if `LevelConfigLoader` and `ExploreProgressController` remain scary to split - verify contracts exist for the rules those files currently hide.

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Live-autoload-first test suite | HIGH | Freeze new scene-heavy tests, extract pure services around one hotspot, port existing assertions downward, keep one smoke test for wiring |
| Missing serialization contracts | HIGH | Snapshot current save/config behavior, add golden fixtures, diff old/new outputs, then resume refactor work |
| Flaky runtime/platform tests | MEDIUM | Classify by source of nondeterminism, replace sleeps with explicit seams or move tests to a quarantined integration lane |
| Partial-init warnings hidden in green tests | MEDIUM | Promote warnings/errors to failures in integration runs and add startup validation for required nodes/autoloads |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Building tests on the live autoload graph | Phase 1 | Most new tests run without booting `PrototypeRoot.tscn` or resolving many `/root/...` services |
| Warning-driven partial initialization | Phase 1 | Smoke tests fail on missing autoloads/nodes instead of logging and continuing |
| Refactoring before freezing serialization/config contracts | Phase 2 | Round-trip fixture tests exist for every persisted subsystem and core config inputs |
| Automating platform hooks too early | Phase 1, then Phase 4 | First milestones pass on non-Windows or hook-disabled environments; Windows-only tests are isolated |
| Confusing integration tests with refactor-safety tests | Phase 2 and Phase 3 | Core progression/reward rules are covered by pure-domain assertions, not only scene tests |
| Leaving time/randomness/content loading implicit | Phase 2 | Tests use injected time/RNG/resource seams or frozen fixtures |
| Adding a framework without an execution model | Phase 1 | CI and local docs define exactly which suites run via .NET and which run via Godot |

## Sources

- Repository evidence: `.planning/PROJECT.md`, `.planning/codebase/CONCERNS.md`, `.planning/codebase/TESTING.md`, `.planning/codebase/ARCHITECTURE.md`
- Repository evidence: `xiuxian-2/scripts/tests/InputSystemTest.cs`, `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/scripts/game/ExploreProgressController.cs`, `xiuxian-2/scripts/services/InputHookService.cs`, `xiuxian-2/scripts/services/LevelConfigLoader.cs`
- Godot docs: https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html (autoload behavior and tradeoffs) - HIGH
- Godot docs: https://docs.godotengine.org/en/stable/tutorials/best_practices/autoloads_versus_internal_nodes.html (when not to overuse autoloads) - MEDIUM
- Godot docs: https://docs.godotengine.org/en/stable/tutorials/best_practices/godot_notifications.html (lifecycle and notification timing constraints) - MEDIUM
- Godot docs: https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_signals.html (signal-driven C# patterns) - MEDIUM
- Godot docs: https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html (headless/CLI execution options) - MEDIUM
- Microsoft Learn: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test (`dotnet test` execution model) - HIGH
- GdUnit4 project README: https://github.com/godot-gdunit-labs/gdUnit4 and https://raw.githubusercontent.com/MikeSchulze/gdunit4/master/README.md (Godot-aware C# and scene-test capabilities, version compatibility) - MEDIUM

---
*Pitfalls research for: brownfield Godot 4 + C# automated testing and refactor-safety*
*Researched: 2026-03-21*
