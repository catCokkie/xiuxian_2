# Architecture Research

**Domain:** automated testing architecture for a brownfield Godot 4 + C# project with autoload services and scene-root orchestration
**Researched:** 2026-03-21
**Confidence:** MEDIUM-HIGH

## Standard Architecture

### System Overview

```text
┌─────────────────────────────────────────────────────────────────────┐
│                    Test Runners and Fixtures                       │
├─────────────────────────────────────────────────────────────────────┤
│  ┌────────────────────┐  ┌──────────────────────────────────────┐  │
│  │ Core unit tests    │  │ Godot integration and smoke tests   │  │
│  │ dotnet test        │  │ headless Godot or GdUnit4Net        │  │
│  └─────────┬──────────┘  └──────────────┬───────────────────────┘  │
│            │                            │                          │
├────────────┴────────────────────────────┴──────────────────────────┤
│                     Seams and Adapters Layer                       │
├─────────────────────────────────────────────────────────────────────┤
│  ┌────────────────┐  ┌────────────────┐  ┌──────────────────────┐  │
│  │ Autoload facade│  │ Scene adapters │  │ External adapters    │  │
│  │ Node + signals │  │ NodePath/UI    │  │ file, time, RNG, OS  │  │
│  └────────┬───────┘  └───────┬────────┘  └──────────┬───────────┘  │
│           │                  │                       │              │
├───────────┴──────────────────┴───────────────────────┴──────────────┤
│                      Extracted Domain/Core                          │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────────────┐  │
│  │ Input rules  │  │ Config rules │  │ Explore/battle policies  │  │
│  │ pure C#      │  │ pure C#      │  │ pure C# state machines   │  │
│  └──────────────┘  └──────────────┘  └───────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

The right architecture for this codebase is not "test the autoloads directly forever." It is a layered design where autoload `Node`s remain the runtime shell, but the behavior that actually needs regression protection moves into pure C# classes with explicit collaborators. That matches both the current pain points in `LevelConfigLoader`, `ExploreProgressController`, and `PrototypeRootController` and Godot's own guidance to avoid using nodes for everything and to reserve autoloads for broad-scoped systems that manage their own information.

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| Autoload facade | Preserve existing `/root/...` access, Godot lifecycle, signals, and save hooks | Thin `Node` classes in `scripts/services/` that delegate to extracted services |
| Scene orchestration adapter | Bind scene nodes, subscribe to signals, translate UI events into domain calls | `Node` or `Control` scripts in `scripts/game/` and `scripts/ui/` |
| Domain/core services | Own config parsing, progression rules, battle progression, reward resolution, snapshot mapping | Plain C# classes or `RefCounted`-like lightweight objects with constructor injection |
| External adapters | Wrap file IO, RNG, clock/time, Godot logging, platform hooks, cloud/OS behavior | Small interfaces plus Godot-backed implementations |
| Test fixtures/builders | Create stable runtime state, fake services, sample config docs, snapshot objects | Builders and fixture files under `tests/Fixtures/` |
| Runtime integration tests | Verify autoload boot, signal wiring, serialization round-trip, and minimal scene-root behavior | Headless Godot scene tests or GdUnit4Net tests marked as runtime-dependent |

## Recommended Project Structure

```text
xiuxian-2/
├── scripts/
│   ├── services/                # Existing autoload Nodes kept as facades
│   ├── game/                    # Scene-root and gameplay adapters
│   ├── ui/                      # UI adapters only, minimal game logic
│   ├── domain/                  # Extracted pure rules and state machines
│   │   ├── input/               # AP rules, decay, batching policies
│   │   ├── config/              # level parsing, validation, unlocks, drops
│   │   ├── explore/             # progress, battle, reward application rules
│   │   └── persistence/         # DTOs and snapshot mappers
│   ├── contracts/               # IClock, IRng, IConfigSource, IHookBackend
│   └── adapters/
│       ├── godot/               # Godot-backed implementations of contracts
│       └── platform/            # Windows/global-input and cloud wrappers
├── tests/
│   ├── Xiuxian2.Core.Tests/     # Pure dotnet tests; highest-volume suite
│   ├── Xiuxian2.Godot.Tests/    # Runtime-aware tests; lower-volume suite
│   ├── Fixtures/
│   │   ├── config/              # Stable JSON/config samples
│   │   └── snapshots/           # save/load payload samples
│   └── Builders/                # State builders and fakes
├── scenes/tests/                # Minimal boot scenes, not feature-rich harnesses
└── scripts/tests/               # Scene-bound helpers only; not the main suite
```

### Structure Rationale

- **`scripts/domain/`:** this is the seam that the current codebase is missing; it lets `LevelConfigLoader` and `ExploreProgressController` shrink without changing their runtime role.
- **`scripts/contracts/` + `scripts/adapters/`:** all unstable dependencies live here first: `/root` lookups, file reads, RNG, time, OS hooks, cloud sync, and Godot logging.
- **`tests/Xiuxian2.Core.Tests/`:** most refactor safety comes from fast deterministic tests that do not boot Godot.
- **`tests/Xiuxian2.Godot.Tests/`:** keep this suite intentionally thin and focused on wiring, signals, serialization, and smoke coverage.
- **`scenes/tests/`:** only keep scenes that prove boot and interaction seams; do not grow another giant manual harness layer.

## Architectural Patterns

### Pattern 1: Autoload Facade Over Pure Core

**What:** keep the autoload name and public Godot-facing API stable, but move logic into an injected or internally constructed core object.
**When to use:** first on large services that many nodes already reach through `/root`, especially `LevelConfigLoader` and `InputActivityState`.
**Trade-offs:** preserves compatibility and keeps refactors incremental, but temporarily introduces a wrapper layer and duplicated method names.

**Example:**
```csharp
public interface ILevelConfigEngine
{
    bool Load(string json);
    DropResult RollDrop(string levelId, string monsterId);
    LevelRuntimeSnapshot ToSnapshot();
    void Restore(LevelRuntimeSnapshot snapshot);
}

public partial class LevelConfigLoader : Node
{
    private readonly IConfigSource _configSource = new GodotConfigSource();
    private ILevelConfigEngine _engine = new LevelConfigEngine(
        _configSource,
        new SystemRandomAdapter());

    public bool LoadConfig()
    {
        var ok = _engine.Load(_configSource.ReadAllText(ConfigPath));
        if (ok)
        {
            EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
        }
        return ok;
    }
}
```

### Pattern 2: Constructor-Injected Policies, Not SceneTree Lookups

**What:** extracted logic gets collaborators through constructor parameters or explicit method arguments, never by calling `GetNode("/root/..." )`.
**When to use:** any rule set that needs deterministic tests: AP decay, drop/pity logic, unlock progression, battle rounds, save/load mapping.
**Trade-offs:** more types and interfaces up front, but drastically simpler tests and safer refactors.

**Example:**
```csharp
public sealed class DropResolver
{
    private readonly IRng _rng;
    private readonly IClock _clock;

    public DropResolver(IRng rng, IClock clock)
    {
        _rng = rng;
        _clock = clock;
    }

    public DropResult Roll(DropTable table, DropRuntimeState runtime)
    {
        // deterministic in tests because rng and clock are fakeable
        return DropPolicies.Roll(table, runtime, _rng, _clock.UtcNow);
    }
}
```

### Pattern 3: Snapshot DTO Boundary for Save/Load Tests

**What:** replace handwritten `Dictionary<string, Variant>` logic as the true business boundary with typed snapshot DTOs, then adapt those DTOs to Godot dictionaries only at the edge.
**When to use:** anywhere `ToDictionary`, `FromDictionary`, `ToRuntimeDictionary`, or `FromRuntimeDictionary` exist today.
**Trade-offs:** one more translation step, but schema changes become explicit and round-trip tests become straightforward.

**Example:**
```csharp
public sealed record LevelRuntimeSnapshot(
    string ActiveLevelId,
    int ActiveWaveIndex,
    IReadOnlyDictionary<string, int> PityCounters,
    IReadOnlyCollection<string> UnlockedLevelIds);

public static class LevelRuntimeVariantMapper
{
    public static Godot.Collections.Dictionary<string, Variant> ToVariant(LevelRuntimeSnapshot snapshot) { ... }
    public static LevelRuntimeSnapshot FromVariant(Godot.Collections.Dictionary<string, Variant> data) { ... }
}
```

## Data Flow

### Request Flow

```text
[Test case]
    ↓
[Fixture builder] → [Core service/policy] → [Fake adapter or test data]
    ↓                    ↓                        ↓
[Assertion]      [Snapshot/result]         [Deterministic side effect]
```

### State Management

```text
[Autoload Node facade]
    ↓ delegates to
[Core state object] ←→ [Commands / rule methods] → [Typed snapshot DTO]
    ↓ emits
[Godot signals for scene adapters]
```

### Key Data Flows

1. **Pure logic path:** a unit test builds a typed config fixture, injects fake RNG/clock/input, executes a core rule object, and asserts on typed results without booting Godot.
2. **Autoload contract path:** an integration test instantiates the autoload facade, injects fake adapters or fixture-backed core objects, and asserts signal emission plus snapshot round-trip behavior.
3. **Scene-root path:** a smoke test boots a minimal scene with fake autoload registrations, then verifies that `PrototypeRootController` and `ExploreProgressController` can bind and coordinate without missing required services.
4. **Platform path:** tests never invoke Win32 hooks directly; they target an `IHookBackend` contract, while one runtime smoke test only verifies that the Godot adapter can initialize or degrade gracefully.

## Recommended Test Architecture

### Test Layers

| Layer | What it covers | Primary target | Runner |
|------|-----------------|----------------|--------|
| Layer 1: core unit tests | deterministic business logic, edge cases, regression tables | extracted classes under `scripts/domain/` | standard `dotnet test` |
| Layer 2: contract tests | autoload facade delegation, signal emission, DTO mapping | `scripts/services/` thin wrappers | standard `dotnet test` if facade can be isolated; otherwise runtime-aware |
| Layer 3: Godot integration tests | boot order, scene binding, NodePath/exported references, minimal controller flow | `PrototypeRootController`, `ExploreProgressController`, scene boot | headless Godot and/or GdUnit4Net |
| Layer 4: smoke tests | one or two end-to-end paths only | app boot, autoload registration, save/load sanity | headless Godot CLI |

The suite should be pyramid-shaped: many core tests, some contract tests, very few runtime tests. In this project, trying to prove correctness mainly through `PrototypeRoot.tscn` will create slow, brittle tests and still fail to make `LevelConfigLoader` safer to refactor.

### Fixtures and Test Doubles

| Fixture type | Purpose | First examples to add |
|--------------|---------|-----------------------|
| Config fixture files | stable level/drop JSON cases | valid baseline, missing monster reference, pity-trigger scenario, unlock progression chain |
| Snapshot builders | round-trip save/load coverage | `LevelRuntimeSnapshotBuilder`, `ExploreRuntimeSnapshotBuilder`, `PlayerProgressSnapshotBuilder` |
| Deterministic adapters | isolate unstable behavior | `FakeRng`, `FakeClock`, `InMemoryConfigSource`, `FakeHookBackend`, `FakeCloudSaveSync` |
| Service registry fixture | fake the autoload graph in tests | a small builder that exposes `InputActivityState`, `LevelConfigLoader`, wallet, backpack, and action state |
| Scene smoke fixtures | keep boot tests minimal | `AutoloadSmokeRoot.tscn`, `ExploreControllerSmokeRoot.tscn` |

## Where Seams and Adapters Should Be Introduced First

### First seam: `LevelConfigLoader`

This is the highest-value starting point because it already combines JSON IO, parsing, validation, unlock state, pity counters, drop resolution, simulation, and runtime persistence in one autoload. Split it behind a facade in this order:

1. `IConfigSource` / `GodotConfigSource` for file access.
2. `LevelConfigParser` for JSON-to-typed-model conversion.
3. `LevelConfigValidator` for validation entries/issues.
4. `DropResolver` + `UnlockProgressionService` for runtime rules.
5. `LevelRuntimeSnapshot` mapper for persistence.

That sequence creates tests before changing behavior and lets the autoload keep its current public surface.

### Second seam: `InputHookService` and `InputActivityState`

- Introduce `IHookBackend` so Win32/global input is behind one boundary.
- Keep `InputActivityState` as the signal-emitting autoload facade, but move AP weighting, rolling-window decay, and soft-cap logic into a pure `InputAggregationEngine`.
- This gives deterministic regression tests for anti-abuse rules without touching platform APIs.

### Third seam: `PrototypeRootController`

Do not try to unit-test the whole controller first. Extract:

- `SaveCoordinator` for dirty-marking and debounce rules.
- `RuntimeServiceRegistry` or explicit exported references for required services.
- `UnifiedSaveComposer` for assembling subsystem snapshots.

That reduces scene-root orchestration to wiring and makes save/load compatibility testable without a full scene boot.

### Fourth seam: `ExploreProgressController`

After `LevelConfigLoader` and input rules are protected, extract:

- `ExploreLoopState` and `BattleLoopState` as pure state machines.
- `RewardApplicationService` if reward logic remains mixed into the controller.
- `MarkerTrackLayoutState` only if battle-track visuals continue to be a hotspot.

This should come after the config and input seams because `ExploreProgressController` depends on both.

## How to Stage Refactor-Safe Extraction Work

### Stage 1: Wrap current behavior without changing call sites

- Keep autoload names in `project.godot` unchanged.
- Add facade-internal delegates/core objects while preserving existing public methods and signals.
- Add characterization tests around current `LevelConfigLoader` behavior using fixture JSON and snapshot round-trips.

### Stage 2: Extract pure rules behind typed DTOs

- Replace internal `Dictionary<string, Variant>` manipulation with typed models and mappers.
- Move RNG, clock, file IO, and platform access behind interfaces.
- Add dense unit coverage around pity, unlock, battle-round, and AP-decay behavior.

### Stage 3: Reduce controller responsibilities

- Move persistence orchestration out of `PrototypeRootController`.
- Move explore/battle rule progression out of `ExploreProgressController`.
- Keep controllers as adapters that translate Godot signals and node events into core calls.

### Stage 4: Add minimal runtime verification

- Add headless boot smoke tests for autoload registration and scene-root binding.
- Add a small number of runtime-aware tests for signals, scene node binding, and graceful degradation on missing services.
- Keep these tests sparse; do not rebuild gameplay assertions here if they already exist at the core layer.

## Build-Order Implications

| Concern | Implication |
|---------|-------------|
| Pure-core extraction | create a plain C# class library first if needed; it should build without Godot runtime dependencies |
| Godot project dependency | the Godot `.csproj` should reference the extracted core library, not the other way around |
| Runtime-aware tests | only add them after the core library exists, otherwise the suite will stay slow and tightly coupled |
| CI sequence | run `dotnet test` for pure core tests first, then run headless/runtime tests second |
| Tooling risk | GdUnit4Net is the official C# path from the GdUnit maintainers, but current public compatibility notes are not fully aligned with Godot 4.5.1, so validate package compatibility before making it the only runtime test runner |

A safe build pipeline for this repo is:

1. build core library and pure tests,
2. build the Godot project,
3. run pure unit tests,
4. run runtime-aware/headless smoke tests,
5. optionally run editor/plugin-backed scene tests locally.

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| 0-50 tests | keep tests focused on `LevelConfigLoader`, `InputActivityState`, and snapshot mappers; one smoke scene is enough |
| 50-300 tests | split fixtures by subsystem, add builders, and enforce that most tests stay Godot-free |
| 300+ tests | parallelize pure tests, keep runtime suite tiny, and treat scene-boot tests as contract checks rather than behavior tests |

### Scaling Priorities

1. **First bottleneck:** runtime-dependent tests; fix by moving logic down into pure services and keeping Godot tests thin.
2. **Second bottleneck:** fixture drift in save/config payloads; fix by centralizing builders and typed snapshot DTOs.

## Anti-Patterns

### Anti-Pattern 1: Testing everything through `PrototypeRoot.tscn`

**What people do:** boot the full game scene for config, drop, persistence, and progression assertions.
**Why it's wrong:** failures become slow, noisy, and hard to localize; controller churn breaks unrelated tests.
**Do this instead:** put rules in pure services, reserve full-scene tests for boot and wiring smoke coverage only.

### Anti-Pattern 2: Replacing one big autoload `Node` with several smaller `Node`s

**What people do:** split `LevelConfigLoader` into more Godot nodes but keep all dependencies as `/root/...` lookups.
**Why it's wrong:** the codebase looks more modular but remains just as hard to test and refactor.
**Do this instead:** extract pure classes and interfaces first; only keep `Node`s where lifecycle, signals, or scene integration are genuinely needed.

### Anti-Pattern 3: Using `Dictionary<string, Variant>` as the domain model

**What people do:** keep business rules and persistence rules operating directly on untyped Godot dictionaries.
**Why it's wrong:** schema drift and invalid state are hard to detect, and tests become string-key fragile.
**Do this instead:** use typed snapshots/models internally and map to `Variant` only at the Godot boundary.

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| Godot runtime | facade Nodes plus exported references and signals | required only for thin integration coverage |
| Windows global input hooks | `IHookBackend` adapter | pure tests should never call Win32 directly |
| Config file loading | `IConfigSource` adapter | use in-memory fixtures in tests |
| RNG and time | `IRng` and `IClock` adapters | critical for pity/drop determinism |
| Cloud save | `ICloudSaveClient` adapter | keep out of most tests; verify only fallback behavior |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| autoload facade ↔ core service | direct API calls | synchronous and explicit; easiest to characterize |
| scene controller ↔ autoload facade | signals plus method calls | runtime-only boundary |
| persistence coordinator ↔ subsystem snapshots | typed DTOs | independent versioning becomes possible |
| platform adapters ↔ domain rules | interfaces | required for deterministic tests |

## Sources

- Codebase evidence: `.planning/PROJECT.md`, `.planning/codebase/ARCHITECTURE.md`, `.planning/codebase/CONCERNS.md`, `.planning/codebase/STRUCTURE.md`
- Godot docs: https://raw.githubusercontent.com/godotengine/godot-docs/master/tutorials/best_practices/autoloads_versus_regular_nodes.rst
- Godot docs: https://raw.githubusercontent.com/godotengine/godot-docs/master/tutorials/best_practices/node_alternatives.rst
- Godot docs: https://raw.githubusercontent.com/godotengine/godot-docs/master/tutorials/best_practices/godot_interfaces.rst
- Godot docs: https://raw.githubusercontent.com/godotengine/godot-docs/master/tutorials/editor/command_line_tutorial.rst
- Official GdUnit4 repo: https://github.com/godot-gdunit-labs/gdUnit4/blob/master/README.md
- Official GdUnit4Net repo: https://github.com/godot-gdunit-labs/gdUnit4Net/blob/master/README.md
- Official GdUnit4Net test adapter docs: https://github.com/godot-gdunit-labs/gdUnit4Net/blob/master/TestAdapter/README.md

---
*Architecture research for: automated testing in a brownfield Godot 4 + C# project*
*Researched: 2026-03-21*
