# Phase 1 Research - Test Harness and Deterministic Seams

**Phase:** 1
**Requirements:** TEST-01, TEST-02, TEST-06
**Mode:** standard planning
**Context source:** roadmap, requirements, project state, codebase map, repo inspection, global research
**Researched:** 2026-03-21

## Recommended Direction

Phase 1 should establish one authoritative `dotnet test` path, a reusable test project layout, and deterministic boundary seams before any broad behavior-freezing work starts. The repo currently has no automated C# test framework and only an in-engine manual harness in `xiuxian-2/scripts/tests/InputSystemTest.cs`, so the safest first move is a plain .NET test foundation rather than a Godot-runtime-first harness.

## Standard Stack

- Primary fast suite: `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`
- Test doubles at unstable boundaries only: `NSubstitute`
- Readable assertions: `Shouldly` or xUnit assertions; do not require FluentAssertions licensing decisions in this phase
- Shared package/version pinning: `Directory.Packages.props`
- Common runner config: `tests/.runsettings`
- Runtime-aware Godot tests: defer as a thin later layer; do not make them the Phase 1 default loop

## Boundary Targets In Current Code

- `xiuxian-2/scripts/services/LevelConfigLoader.cs`
  - owns JSON/file loading through `FileAccess.Open`
  - owns randomness through `RandomNumberGenerator`
  - owns time-sensitive counters and drop/pity behavior in one autoload service
- `xiuxian-2/scripts/services/CloudSaveSyncService.cs`
  - owns filesystem writes/reads through `File.WriteAllBytes`, `File.ReadAllBytes`, and `ProjectSettings.GlobalizePath`
  - owns reflective cloud/runtime boundary behavior
- `xiuxian-2/scripts/services/InputHookService.cs`
  - owns platform detection and Win32 hook lifecycle
  - should be covered by fakeable platform and hook-backend seams, not by real global hooks in the default suite

## Planning Implications

- Keep Phase 1 focused on fast service-level tests and seam introduction, not config contract coverage or runtime smoke breadth
- Treat TDD as mandatory for code-producing tasks: write failing tests first, then implementation, then refactor
- Create seam contracts before service adoption so downstream plans can code against stable interfaces
- Keep `Node` autoload names and public Godot-facing APIs stable while introducing internal adapters or injectable collaborators

## Do Not Hand-Roll

- Do not invent a custom scene-driven automated harness as the default suite entry point
- Do not make Windows global hook automation part of the normal local feedback loop
- Do not test most new behavior by booting `PrototypeRoot.tscn` or resolving many `/root/...` autoloads
- Do not blanket-interface every method; only boundary seams are needed in this phase

## Common Pitfalls To Prevent In Plans

- Live-autoload-first testing instead of seam-first testing
- Warning-driven partial initialization being treated as a passing test outcome
- Mixing runtime-heavy Godot tests into the default `dotnet test` feedback loop
- Introducing seams without reusable fakes/builders, which would recreate ad hoc setup in every test file
- Refactoring giant services without first proving the new seams can be driven deterministically under tests

## Validation Architecture

### Default Feedback Loop

- Quick command: `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj`
- Full phase command: `dotnet test xiuxian-2/xiuxian2.sln --settings tests/.runsettings`
- Expected default runtime target: fast core tests only; runtime-aware suites remain opt-in until later phases

### Validation Expectations For Phase 1

- Every plan task must have an automated verification command
- The first plan must create the runnable test entry point used by every later plan
- Seams must be proven with deterministic tests, not only by interface creation
- The suite must avoid real user save paths, live Win32 hooks, and mutable designer-owned config files in default runs

### Plan Breakdown Recommendation

1. Foundation plan: package pinning, solution wiring, test project, documented command
2. Shared TDD support plan: reusable fixtures/builders plus seam contracts and fake adapters
3. Config/filesystem seam adoption plan: `LevelConfigLoader` and `CloudSaveSyncService`
4. Platform seam adoption plan: `InputHookService` fallback and hook-backend abstraction

## Research Conclusion

Phase 1 does not need another open-ended discovery pass. The repo evidence and current testing/tooling guidance are aligned: ship a plain `dotnet test` foundation first, add shared deterministic support next, then push seams into the largest unstable services without making runtime-heavy automation the default path.

---

*Ready for planning: yes*
