# Stack Research

**Domain:** automated testing stack for a brownfield Godot 4.5 + C# / .NET 8 desktop game with service-heavy runtime logic
**Researched:** 2026-03-21
**Confidence:** HIGH

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| xUnit.net | `xunit.v3` `2.0.3` + `xunit.runner.visualstudio` `3.1.1` | Primary fast test framework for extracted service logic, parsers, reward rules, serializers, and regression tests | Best fit for .NET 8-first C# code. It runs cheaply from `dotnet test`, has strong IDE support, and does not require Godot runtime for most service tests. |
| Microsoft test platform | `Microsoft.NET.Test.Sdk` `17.14.1+` | Discovery and execution for CLI/IDE tests | Required glue for `dotnet test`, VS Code, Rider, and adapters. `17.14.1` is the safe floor because the GdUnit4 adapter depends on it; newer 18.x can be adopted after local verification. |
| GdUnit4 + GdUnit4Net | Godot plugin `v6.x` for Godot `4.5/4.5.1`; `gdUnit4.api` `5.0.0`; `gdUnit4.test.adapter` `3.0.0` | Godot-aware integration tests for autoloads, signals, scene boot, and runtime-only behavior | This is the current Godot-native choice with real C# support. Crucially, `gdUnit4.api` v5 no longer requires Godot runtime for every test, so only `[RequireGodotRuntime]` tests pay the engine cost. |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `coverlet.collector` | `6.0.4` | Cross-platform coverage collection from `dotnet test` | Use for all CI and local coverage reporting. `6.0.4` matches the dependency floor used by `gdUnit4.test.adapter` in 2025. |
| `NSubstitute` | `5.3.0` | Test doubles for file IO, clock/random seams, platform hooks, Steam/cloud wrappers, and other interfaces | Use only at external boundaries. Prefer real objects for pure domain logic and use substitutes for OS/process/network/native seams. |
| `Shouldly` | `4.3.0` | Readable assertion layer for xUnit tests | Use if the team wants more expressive failures than raw `Assert.*` without introducing FluentAssertions commercial-license questions. |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| `Directory.Packages.props` | Centralize test package versions | Strongly recommended in this brownfield repo because you will likely have both pure .NET and Godot-aware test projects. |
| `.runsettings` | Stable adapter config for GdUnit4Net | Set `MaxCpuCount` to `1`, define `GODOT_BIN`, set `ResultsDirectory`, and pass `--headless` for CI-friendly Godot runs. |
| `dotnet test` | Single entry point for fast local and CI execution | Keep one default command path; let GdUnit4Net plug into the same test platform instead of inventing a parallel bespoke runner. |

## Brownfield Testing Shape

The best-practice stack for this codebase is a two-layer test system, not a single framework:

1. **Default layer: pure .NET tests with xUnit**
   - Target the logic you want to refactor out of autoload `Node` classes: config parsing, validation, pity/drop math, progression rules, save/load DTO mapping, AP decay calculations, and settlement logic.
   - This should become the bulk of the suite because it is fast, deterministic, and independent of Godot scene lifecycle.

2. **Thin Godot layer: GdUnit4Net tests**
   - Cover only behavior that actually needs the engine: autoload wiring, signal emission, `Variant`/Godot `Dictionary` interaction, scene boot smoke tests, and runtime controller integration.
   - Mark only those tests with `[RequireGodotRuntime]` so logic-only tests stay fast.

For this repo specifically, treat the current autoload services as **adapters/facades** and move durable business logic behind plain C# interfaces/classes over time. Godot's own best-practice docs explicitly warn against using nodes for everything, which aligns with the repo's current pain points around `LevelConfigLoader`, `ExploreProgressController`, and hard-wired autoload lookups.

## How To Test Service-Layer Logic In Godot + C#

### Preferred Pattern

| Service shape | Test approach | Why |
|--------------|---------------|-----|
| Pure calculation/state transition logic | xUnit only | Fastest and most stable; no engine dependency needed. |
| Logic currently buried in an autoload `Node` | Extract to a plain C# collaborator, then test with xUnit | Gives immediate refactor safety without needing a full engine harness. |
| Signal contracts, autoload registration, scene-tree lifecycle, `ResourceLoader`/`Node` behavior | GdUnit4Net with `[RequireGodotRuntime]` | These are real Godot integration concerns and should be tested with the engine. |
| Windows-only global input hook and reflective Steam/cloud code | Interface seam + xUnit substitutes, plus a tiny manual or Windows-only smoke test | Native hooks are poor first automation targets; test your wrapper behavior, not Win32 itself. |

### Concrete Guidance For This Codebase

- `LevelConfigLoader`: split and test parser, validator, reward resolver, unlock progression, and runtime-state serialization as pure xUnit tests fed by JSON fixtures.
- `InputActivityState` and `ActivityConversionService`: extract time and randomness seams, then test decay, batching, caps, and settlement with xUnit.
- `PrototypeRootController` and `ExploreProgressController`: keep only smoke/integration tests in GdUnit4Net for signal wiring, load/save orchestration, and required node presence.
- `ToDictionary`/`FromDictionary` and runtime dictionary methods: treat these as first-class regression tests in xUnit; they are a current fragility hotspot and do not need full scene boot.

## Installation

```bash
# 1) Centralize versions at repo root
cat > Directory.Packages.props <<'EOF'
<Project>
  <ItemGroup>
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageVersion Include="xunit.v3" Version="2.0.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.1" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="Shouldly" Version="4.3.0" />
    <PackageVersion Include="gdUnit4.api" Version="5.0.0" />
    <PackageVersion Include="gdUnit4.test.adapter" Version="3.0.0" />
  </ItemGroup>
</Project>
EOF

# 2) Create the fast pure-.NET test project
dotnet new xunit3 -n Xiuxian2.Core.Tests -o tests/Xiuxian2.Core.Tests
dotnet sln "xiuxian-2/xiuxian2.sln" add tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj
dotnet add tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj package NSubstitute
dotnet add tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj package Shouldly
dotnet add tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj package coverlet.collector

# 3) Add a Godot-aware integration test project only after fast tests exist
dotnet new classlib -n Xiuxian2.Godot.Tests -o tests/Xiuxian2.Godot.Tests
dotnet sln "xiuxian-2/xiuxian2.sln" add tests/Xiuxian2.Godot.Tests/Xiuxian2.Godot.Tests.csproj
dotnet add tests/Xiuxian2.Godot.Tests/Xiuxian2.Godot.Tests.csproj package Microsoft.NET.Test.Sdk
dotnet add tests/Xiuxian2.Godot.Tests/Xiuxian2.Godot.Tests.csproj package gdUnit4.api
dotnet add tests/Xiuxian2.Godot.Tests/Xiuxian2.Godot.Tests.csproj package gdUnit4.test.adapter

# 4) Run the fast suite first
dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj
```

## Practical Setup Sequence For This Brownfield Repo

1. **Create package/version management first**
   - Add `Directory.Packages.props` before adding multiple test projects.

2. **Stand up a pure .NET test project before touching Godot runtime tests**
   - Start with serialization, parser, validation, drop math, pity logic, and progression rules.

3. **Extract seams from the worst services instead of trying to test current monoliths as-is**
   - Prioritize `LevelConfigLoader`, `InputActivityState`, and save/load DTO logic.

4. **Introduce interfaces only at unstable boundaries**
   - File system, OS hooks, clock/random, cloud sync, and Godot resource access.
   - Do not blanket-interface every method just to satisfy mocking.

5. **Add GdUnit4Net once you have a reason to boot Godot**
   - Use it for autoload startup smoke tests, signal flow, and runtime-only interactions.

6. **Add a single `.runsettings` file for all Godot-aware tests**
   - Pin `GODOT_BIN`, `ResultsDirectory`, `TreatNoTestsAsError`, and `MaxCpuCount=1`.

7. **Only after the above, add coverage reporting in CI**
   - Use `dotnet test --collect:"XPlat Code Coverage"` and publish Cobertura/HTML reports.

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| xUnit v3 | NUnit 4 | Use NUnit only if the team already has strong NUnit conventions or existing reusable NUnit tooling. For a new brownfield foundation in .NET 8, xUnit v3 is the cleaner default. |
| GdUnit4Net for engine-aware tests | Custom in-engine harness scenes only | Keep custom harness scenes for exploratory/manual debugging, not for the first automated foundation. They are harder to scale, assert, and run in CI. |
| NSubstitute | Moq | Use Moq only if the team is already standardized on it. For new tests, NSubstitute's AAA style is simpler and enough for this project's boundary seams. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| UI automation or full gameplay E2E as the first milestone | High flake risk, high maintenance, and low refactor-safety ROI for this repo's current problems | xUnit for service logic first; GdUnit4Net only for thin integration smoke tests |
| GdUnit4 runtime tests for all service logic | Forces slow engine-backed execution for code that should be deterministic and cheap | Extract plain C# collaborators and test them with xUnit |
| GUT/WAT or other GDScript-first frameworks as the main foundation | Good tools for GDScript, but this codebase is C#/.NET-heavy and needs first-class .NET tooling | xUnit v3 + GdUnit4Net |
| FluentAssertions 8+ without an explicit license decision | Version 8+ is free only for open-source/non-commercial use; commercial use requires a paid license | Shouldly or xUnit asserts, or stay on a licensed/approved FluentAssertions policy |
| Blanket mocking of Godot nodes and autoloads | Produces brittle tests that assert implementation details instead of game rules | Fake only external boundaries; move rules into POCO services and test those directly |

## Stack Patterns by Variant

**If the code can run without `GodotObject` or `Node`:**
- Use `xunit.v3` + `Shouldly` + `NSubstitute`
- Because the test should stay fast, deterministic, and runnable from plain `dotnet test`

**If the code needs signals, scene tree, resources, or autoload runtime:**
- Use `gdUnit4.api` + `gdUnit4.test.adapter` and mark tests with `[RequireGodotRuntime]`
- Because only the engine can validate those contracts honestly

**If the code touches Win32 hooks or Steam/cloud reflection:**
- Use xUnit against an interface seam and keep one tiny runtime smoke test
- Because native/system integrations are bad unit-test targets and should be isolated behind wrappers

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|-----------------|-------|
| GdUnit4 plugin `v6.0.x` / `v6.1.x` | Godot `4.5` / `4.5.1` | GdUnit4 `v6.x` is the correct line for Godot 4.5-era projects. |
| `gdUnit4.api` `5.0.0` | `gdUnit4.test.adapter` `3.0.0` | 2025 C# stack; adapter adds `dotnet test`/IDE integration and supports runtime opt-in via `[RequireGodotRuntime]`. |
| `gdUnit4.test.adapter` `3.0.0` | `Microsoft.NET.Test.Sdk` `17.14.1+` | Use `17.14.1` as the minimum safe floor; validate 18.x locally before standardizing. |
| `coverlet.collector` `6.0.4` | `Microsoft.NET.Test.Sdk` `17.13.0+` | Stable 2025 coverage baseline and aligned with GdUnit4 adapter dependency floor. |
| `xunit.v3` `2.0.3` | `.NET 8` | The official getting-started docs target .NET 8 and support both direct run and `dotnet test`. |

## Sources

- https://xunit.net/docs/getting-started/v3/getting-started — verified xUnit v3 `.NET 8` guidance, package set, and `dotnet test` workflow (`HIGH`)
- https://www.nuget.org/packages/Microsoft.NET.Test.Sdk — verified current/stable test SDK versions and framework support (`HIGH`)
- https://www.nuget.org/packages/coverlet.collector — verified coverage collector setup, `dotnet test --collect`, and version floor guidance (`HIGH`)
- https://github.com/godot-gdunit-labs/gdUnit4/blob/master/README.md — verified GdUnit4 supports Godot 4.5/4.5.1 and C# via GdUnit4Net (`HIGH`)
- https://github.com/godot-gdunit-labs/gdUnit4/releases/tag/v6.0.2 — verified Godot 4.5 compatibility break and correct plugin generation for 4.5-era projects (`HIGH`)
- https://www.nuget.org/packages/gdUnit4.api — verified C# API package, `[RequireGodotRuntime]`, and 2025 architecture change for fast logic-only tests (`HIGH`)
- https://www.nuget.org/packages/gdUnit4.test.adapter — verified `dotnet test`/IDE adapter, `.runsettings`, `GODOT_BIN`, and `--headless` support (`HIGH`)
- https://www.nuget.org/packages/NSubstitute — verified version and current framework compatibility (`HIGH`)
- https://www.nuget.org/packages/Shouldly — verified version and framework compatibility (`HIGH`)
- https://docs.godotengine.org/en/stable/tutorials/best_practices/node_alternatives.html — verified Godot's guidance to avoid pushing all logic into nodes (`MEDIUM`, page content was noisy but the official page and topic are current)
- https://docs.godotengine.org/en/stable/tutorials/best_practices/autoloads_versus_internal_nodes.html — verified official autoload design guidance relevant to this repo's architecture (`MEDIUM`, same retrieval caveat)
- https://fluentassertions.com/introduction and https://xceed.com/fluent-assertions-faq/ — verified FluentAssertions v8+ licensing/commercial-use constraints (`HIGH`)

---
*Stack research for: brownfield Godot 4 + C# test foundation*
*Researched: 2026-03-21*
