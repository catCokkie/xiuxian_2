# Feature Research

**Domain:** Automated test foundation for a brownfield Godot 4 + C# desktop game
**Researched:** 2026-03-21
**Confidence:** MEDIUM

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = the test foundation does not meaningfully reduce refactor risk.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Repeatable headless test entry point | A real automated suite must run from CLI and CI, not only from an in-editor scene harness | MEDIUM | Establish one supported command path for this repo, ideally `dotnet test` for C# suites plus selective Godot/headless execution where runtime access is required. Current repo has no repeatable automated entry point. |
| Assertion-based service regression tests for deterministic logic | The highest-value brownfield safety net is direct verification of service behavior, not manual observation | MEDIUM | Start with `LevelConfigLoader`, progression/resource services, reward/drop logic, and config validation paths. Prioritize outputs and invariants over scene wiring. |
| Round-trip persistence and config fixture tests | Brownfield refactors break save/load and config parsing first; these must be locked down early | MEDIUM | Add tests for `ToDictionary`/`FromDictionary`, runtime dictionaries, schema defaults, and representative JSON/config samples from `docs/design/`. This directly addresses current fragile areas. |
| Test seams around platform and external boundaries | Existing Windows hooks, file IO, clocks, RNG, and cloud reflection paths are too unstable to test through real side effects every run | HIGH | Introduce wrappers or adapters around input hooks, filesystem, time, randomness, and cloud sync. Without seams, service tests stay slow, flaky, or impossible. |
| Autoload/bootstrap smoke coverage | In a Godot autoload-heavy app, broken singleton wiring is a common regression that pure unit tests miss | MEDIUM | Add a small number of startup tests that validate required autoload presence, boot-time service construction, and fail-loud behavior for missing dependencies. Keep these sparse and stable. |
| Stable fixtures/builders for brownfield state | Brownfield service tests become brittle unless test setup reuses representative game state and config data | MEDIUM | Create reusable test data for save payloads, level configs, wallet/progression states, and runtime dictionaries. Prefer builders and canned fixtures over ad hoc dictionaries in every test. |
| CI-friendly failure output and test filtering | A useful suite must tell you what broke and let you run only affected categories during refactors | LOW | Use categories/traits for `service`, `serialization`, `runtime`, and `windows-only`. GdUnit4Net and VSTest both support filtering; this matters once the suite grows. |

### Differentiators (Competitive Advantage)

Features that are not strictly required for v1, but materially improve long-term refactor safety in this repo.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Characterization tests for monolith extraction | Lets the team freeze current behavior before splitting `LevelConfigLoader` and other large controllers | HIGH | Capture current outputs for unlock rules, pity counters, drop resolution, validation messages, and runtime state transitions before internal structure changes. High leverage for brownfield decomposition. |
| Golden-master regression fixtures for config-driven gameplay | Protects complex config/runtime behavior that is hard to reason about line-by-line after refactors | HIGH | Use representative JSON inputs and expected outputs for battle/setup/drop/progression flows. Best fit for `LevelConfigLoader` and other config-driven systems where exact behavior matters more than internal implementation. |
| Dual-speed suite design: fast logic tests by default, Godot runtime only when required | Keeps tests fast enough to run constantly while still allowing scene/runtime verification where needed | MEDIUM | GdUnit4Net explicitly supports logic-only tests without Godot runtime and opt-in runtime tests via `[RequireGodotRuntime]`. This is a strong fit for service-heavy refactor work. |
| Property/fuzz tests for invariant-heavy systems | Finds edge cases in drop tables, progression thresholds, and config validation that example-based tests miss | HIGH | Use this after baseline deterministic cases exist. Best for invariants like non-negative resources, bounded pity counters, unlock ordering, and stable deserialization defaults. |
| Contract tests around extracted service interfaces and facades | Makes later modularization safer by locking behavior at service boundaries instead of concrete monolith internals | MEDIUM | Especially useful once autoload facades are split into parser/validator/runtime/reward components. Supports ongoing refactors without rewriting every test. |
| Platform-segregated smoke jobs for Windows-only behavior | Gives coverage for global input hooks without making the main suite flaky or cross-platform hostile | MEDIUM | Keep hook tests as a separate Windows lane with explicit environment requirements. Do not make them the default gate for every local run. |
| Analyzer-backed test correctness and metadata discipline | Reduces false confidence from misconfigured attributes, missing runtime markers, or inconsistent categories | LOW | GdUnit4Net ships Roslyn analyzers; useful once the suite expands and multiple test layers exist. |

### Anti-Features (Commonly Requested, Often Problematic)

Features that sound attractive but are the wrong first move for this milestone.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Full UI or end-to-end automation first | Feels closest to "real gameplay" and seems comprehensive | High setup cost, high flake risk, and poor signal for service-layer refactors; the project explicitly marks this out of scope for the first milestone | Start with service, serialization, and bootstrap coverage; add a few narrow runtime smoke tests later |
| Mocking every Godot dependency away | Makes tests easy to write initially | Over-mocked tests stop reflecting actual autoload/runtime behavior and miss wiring issues that matter in this codebase | Mock only external or unstable boundaries; keep core domain/state logic real |
| Coverage percentage as the primary success metric | Easy to track and communicate | Encourages shallow tests and does not guarantee refactor safety for config-heavy logic | Use risk-based target areas first; add coverage reporting as a visibility tool, not the goal |
| Big-bang DI rewrite before adding tests | Promises a "clean" architecture | Delays protection, increases churn, and risks behavior drift in a shipped brownfield codebase | Add seams incrementally around hot spots, then extract services behind tests |
| Running Windows hook and cloud integration tests in every default test pass | Sounds thorough | These paths are platform-sensitive and environment-sensitive; they will become the main source of flake | Tag them as explicit smoke/integration lanes with opt-in or CI-matrix execution |

## Feature Dependencies

```text
[Repeatable headless test entry point]
    └──requires──> [Chosen test framework + runner integration]
                        └──requires──> [Stable test categories and reporting]

[Service regression tests]
    └──requires──> [Test seams around platform/external boundaries]
                        └──requires──> [Reusable fixtures/builders]

[Round-trip persistence/config tests]
    └──requires──> [Representative save + config fixtures]

[Characterization tests for monolith extraction]
    └──requires──> [Baseline service regression tests]
                        └──enhances──> [Contract tests around extracted services]

[Golden-master regression fixtures]
    └──requires──> [Stable deterministic inputs/outputs]

[Platform-segregated Windows smoke jobs]
    └──conflicts──> [Default fast local feedback loop]
```

### Dependency Notes

- **Service regression tests require test seams around platform/external boundaries:** `InputHookService`, cloud sync, file IO, time, and randomness cannot be reliable regression targets until their side effects are controllable.
- **Round-trip persistence/config tests require representative fixtures:** handwritten micro-fixtures will miss the real brownfield data shapes that currently break save/load and config behavior.
- **Characterization tests require baseline service regression tests:** lock down core service expectations first, then freeze more complex current behavior before large extractions.
- **Golden-master fixtures require stable deterministic inputs/outputs:** if RNG, time, or environment leak into the test, the snapshot becomes noise.
- **Platform-segregated Windows smoke jobs conflict with the default fast loop:** keep platform validation real, but isolate it from the main refactor-safety suite.

## MVP Definition

### Launch With (v1)

Minimum viable test foundation for this milestone.

- [ ] Repeatable CLI test run for the repo — without this, tests remain a manual ritual rather than a safety net
- [ ] Assertion-based service tests for config/progression/resource logic — highest-value regression protection for brownfield refactors
- [ ] Round-trip save/config tests using representative fixtures — directly protects current fragile serialization/config paths
- [ ] Seams for filesystem/time/RNG/platform/cloud boundaries — enables reliable tests without a destabilizing rewrite
- [ ] Small autoload/bootstrap smoke suite — catches broken singleton wiring that pure logic tests will miss

### Add After Validation (v1.x)

- [ ] Characterization/golden-master coverage for `LevelConfigLoader` and similar monoliths — add once the basic harness is trusted
- [ ] Runtime-required Godot tests for a few critical service interactions — add when pure logic tests no longer cover the highest-risk refactors
- [ ] Coverage reporting and filtered CI lanes — add when the suite is large enough that visibility and selective execution matter

### Future Consideration (v2+)

- [ ] Property/fuzz testing for config and progression invariants — valuable, but only after deterministic baseline tests exist
- [ ] Windows-only hook smoke lane in CI — useful once the core suite is stable and environment provisioning is solved
- [ ] Narrow UI automation for top-risk regressions — only after service-layer protection is in place and scene seams are clearer

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Repeatable CLI/headless test entry point | HIGH | MEDIUM | P1 |
| Service regression tests for deterministic logic | HIGH | MEDIUM | P1 |
| Round-trip persistence/config tests | HIGH | MEDIUM | P1 |
| Test seams for external/platform boundaries | HIGH | HIGH | P1 |
| Bootstrap/autoload smoke tests | MEDIUM | MEDIUM | P1 |
| Characterization tests for monolith extraction | HIGH | HIGH | P2 |
| Golden-master config regression fixtures | HIGH | HIGH | P2 |
| Coverage reports and filtered CI lanes | MEDIUM | LOW | P2 |
| Property/fuzz tests | MEDIUM | HIGH | P3 |
| UI automation | LOW | HIGH | P3 |

**Priority key:**
- P1: Must have for launch
- P2: Should have, add when possible
- P3: Nice to have, future consideration

## Ecosystem Pattern Analysis

| Capability | Common Ecosystem Pattern | Fit For This Repo | Our Approach |
|------------|--------------------------|-------------------|--------------|
| Fast C# logic tests | `dotnet test` + assertion framework + coverage tooling | Strong | Make pure service/state tests the default feedback loop |
| Godot-aware runtime tests | GdUnit4/GdUnit4Net runtime-required tests and scene runners | Strong | Use selectively for autoload/bootstrap and limited runtime interactions |
| Coverage visibility | Coverlet collector/MSBuild + HTML/JUnit-style reports | Strong | Add after the first useful suite exists; use for visibility, not as the main goal |
| IDE/CI integration | VSTest-compatible discovery, filtering, results, and runsettings | Strong | Favor tooling that supports filtering by layer and environment |
| Full end-to-end gameplay automation | Heavy scene automation | Weak for v1 | Defer until service-layer regression safety is already delivering value |

## Sources

- `.planning/PROJECT.md` — project scope, priorities, and out-of-scope constraints for this milestone (HIGH confidence)
- `.planning/codebase/TESTING.md` — current testing baseline, gaps, and Godot-specific patterns already present in the repo (HIGH confidence)
- `.planning/codebase/CONCERNS.md` — fragile areas, monolith hotspots, and regression-prone boundaries that should shape feature prioritization (HIGH confidence)
- `.planning/codebase/CONVENTIONS.md` — repo conventions relevant to fixture design, serialization patterns, and autoload wiring (HIGH confidence)
- Godot official docs, command line tutorial: https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html — confirms CLI/headless execution is part of normal Godot workflows (MEDIUM confidence; page is authoritative but not deeply test-specific)
- Microsoft Learn, NUnit with `dotnet test`: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-nunit — confirms standard .NET test project and runner workflow for C# suites (HIGH confidence)
- Microsoft Learn, code coverage for .NET: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage — confirms Coverlet/report-generation workflows and positions coverage as a refactoring aid, not the suite itself (HIGH confidence)
- GdUnit4 README: https://github.com/godot-gdunit-labs/gdUnit4/blob/master/README.md — confirms current Godot-focused testing capabilities such as scene runner, mocking/spying, CLI, reports, and C# support (MEDIUM confidence; project-maintainer source, not official Godot docs)
- GdUnit4Net README: https://github.com/godot-gdunit-labs/gdUnit4Net/blob/master/README.md — confirms current C#-specific capabilities like logic-only default execution, `[RequireGodotRuntime]`, filtering, analyzers, and VSTest integration (MEDIUM confidence; project-maintainer source)
- GdUnit4Net Test Adapter README: https://raw.githubusercontent.com/godot-gdunit-labs/gdUnit4Net/master/TestAdapter/README.md — confirms `.runsettings`, IDE support, filtering, and result/logging support for C# Godot tests (MEDIUM confidence)

---
*Feature research for: brownfield Godot 4 + C# automated test foundation focused on refactor safety*
*Researched: 2026-03-21*
