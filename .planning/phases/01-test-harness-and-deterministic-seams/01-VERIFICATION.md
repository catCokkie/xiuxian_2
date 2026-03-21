---
phase: 01-test-harness-and-deterministic-seams
verified: 2026-03-21T02:37:15Z
status: passed
score: 4/4 must-haves verified
---

# Phase 1: Test Harness and Deterministic Seams Verification Report

**Phase Goal:** Developers can run a single repeatable automated test workflow against the brownfield project, and core services can be exercised with deterministic dependencies instead of live runtime boundaries.
**Verified:** 2026-03-21T02:37:15Z
**Status:** passed
**Re-verification:** No - initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
| --- | --- | --- | --- |
| 1 | A developer can run one documented CLI test command and get the same suite entry point locally and in automation. | ✓ VERIFIED | `xiuxian-2/docs/TESTING_AUTOMATION.md:5`, `xiuxian-2/docs/TESTING_AUTOMATION.md:15`, `tests/.runsettings:1`, and both documented commands passed with `19` tests green. |
| 2 | New service tests can reuse shared fixtures, builders, and helpers instead of creating ad hoc setup in each file. | ✓ VERIFIED | Shared builder and frozen fixture exist in `tests/Xiuxian2.Core.Tests/Builders/ServiceFixtureBuilder.cs:5`, `tests/Xiuxian2.Core.Tests/Fixtures/config/phase1-sample-config.json:1`, and are exercised by `tests/Xiuxian2.Core.Tests/Support/Deterministic/FixtureSupportTests.cs:5`. |
| 3 | Core service tests can replace RNG, clock, filesystem, and platform/runtime boundaries with deterministic seams and assert stable outcomes. | ✓ VERIFIED | Seam contracts exist under `xiuxian-2/scripts/contracts/`; deterministic fakes implement them in `tests/Xiuxian2.Core.Tests/Support/Deterministic/`; seam-focused tests pass in `tests/Xiuxian2.Core.Tests/Services/LevelConfigLoaderSeamTests.cs:7`, `tests/Xiuxian2.Core.Tests/Services/CloudSaveSyncServiceSeamTests.cs:7`, and `tests/Xiuxian2.Core.Tests/Services/InputHookServicePlatformTests.cs:6`. |
| 4 | The default feedback loop stays focused on fast service-level tests rather than requiring a live Godot runtime. | ✓ VERIFIED | The only default suite is `tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj:1`; docs state runtime tests are excluded in `xiuxian-2/docs/TESTING_AUTOMATION.md:23`; smoke coverage asserts runtime-free execution in `tests/Xiuxian2.Core.Tests/Smoke/TestHarnessSmokeTests.cs:20`. |

**Score:** 4/4 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
| --- | --- | --- | --- |
| `Directory.Packages.props` | Shared pinned test tooling | ✓ VERIFIED | Central package versions present at `Directory.Packages.props:1`. |
| `tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj` | Primary fast test project | ✓ VERIFIED | Test project targets `net8.0`, references xUnit packages, and references `xiuxian-2/xiuxian2.csproj` at `tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj:1`. |
| `tests/.runsettings` | Stable solution-level CLI settings | ✓ VERIFIED | Runsettings file exists and is referenced by docs at `tests/.runsettings:1`. |
| `xiuxian-2/docs/TESTING_AUTOMATION.md` | Documented default and automation commands | ✓ VERIFIED | Both developer and solution commands are documented at `xiuxian-2/docs/TESTING_AUTOMATION.md:3`. |
| `xiuxian-2/xiuxian2.sln` | Solution wiring for test project | ✓ VERIFIED | Solution includes `Xiuxian2.Core.Tests` at `xiuxian-2/xiuxian2.sln:5`. |
| `xiuxian-2/scripts/contracts/IRng.cs` | RNG seam contract | ✓ VERIFIED | Contract exists at `xiuxian-2/scripts/contracts/IRng.cs:1`. |
| `xiuxian-2/scripts/contracts/IClock.cs` | Clock seam contract | ✓ VERIFIED | Contract exists at `xiuxian-2/scripts/contracts/IClock.cs:1`. |
| `xiuxian-2/scripts/contracts/IConfigSource.cs` | Config seam contract | ✓ VERIFIED | Contract exists at `xiuxian-2/scripts/contracts/IConfigSource.cs:1`. |
| `xiuxian-2/scripts/contracts/IFileSystem.cs` | Filesystem seam contract | ✓ VERIFIED | Contract exists at `xiuxian-2/scripts/contracts/IFileSystem.cs:1`. |
| `xiuxian-2/scripts/contracts/IPlatformInfo.cs` | Platform seam contract | ✓ VERIFIED | Contract exists at `xiuxian-2/scripts/contracts/IPlatformInfo.cs:1`. |
| `xiuxian-2/scripts/contracts/IHookBackend.cs` | Hook lifecycle seam contract | ✓ VERIFIED | Contract exists at `xiuxian-2/scripts/contracts/IHookBackend.cs:1`. |
| `tests/Xiuxian2.Core.Tests/Builders/ServiceFixtureBuilder.cs` | Reusable deterministic fixture builder | ✓ VERIFIED | Builder loads a frozen fixture and constructs reusable fakes at `tests/Xiuxian2.Core.Tests/Builders/ServiceFixtureBuilder.cs:5`. |
| `xiuxian-2/scripts/services/LevelConfigLoader.cs` | Config/RNG/clock seam-aware service | ✓ VERIFIED | Runtime defaults and test seam injection are present at `xiuxian-2/scripts/services/LevelConfigLoader.cs:49` and `xiuxian-2/scripts/services/LevelConfigLoader.cs:69`. |
| `xiuxian-2/scripts/services/CloudSaveSyncService.cs` | Filesystem-seamed cloud save runtime | ✓ VERIFIED | Runtime uses `IFileSystem` and exposes seam-testable runtime logic at `xiuxian-2/scripts/services/CloudSaveSyncService.cs:43` and `xiuxian-2/scripts/services/CloudSaveSyncService.cs:71`. |
| `xiuxian-2/scripts/services/InputHookService.cs` | Platform and hook-seamed input service | ✓ VERIFIED | Runtime defaults and seam-aware startup logic are present at `xiuxian-2/scripts/services/InputHookService.cs:42` and `xiuxian-2/scripts/services/InputHookService.cs:63`. |

### Key Link Verification

| From | To | Via | Status | Details |
| --- | --- | --- | --- | --- |
| `xiuxian-2/xiuxian2.sln` | `tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj` | solution project reference | ✓ WIRED | `Xiuxian2.Core.Tests` is included at `xiuxian-2/xiuxian2.sln:5`. |
| `xiuxian-2/docs/TESTING_AUTOMATION.md` | `tests/.runsettings` | documented CLI command | ✓ WIRED | Solution command references `--settings tests/.runsettings` at `xiuxian-2/docs/TESTING_AUTOMATION.md:18`. |
| `tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeRng.cs` | `xiuxian-2/scripts/contracts/IRng.cs` | fake implementation | ✓ WIRED | `FakeRng : IRng` at `tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeRng.cs:5`. |
| `tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeHookBackend.cs` | `xiuxian-2/scripts/contracts/IHookBackend.cs` | fake implementation | ✓ WIRED | `FakeHookBackend : IHookBackend` at `tests/Xiuxian2.Core.Tests/Support/Deterministic/FakeHookBackend.cs:5`. |
| `tests/Xiuxian2.Core.Tests/Builders/ServiceFixtureBuilder.cs` | `tests/Xiuxian2.Core.Tests/Fixtures/config/phase1-sample-config.json` | fixture loading | ✓ WIRED | Builder points at the frozen fixture path at `tests/Xiuxian2.Core.Tests/Builders/ServiceFixtureBuilder.cs:8`. |
| `xiuxian-2/scripts/services/LevelConfigLoader.cs` | `xiuxian-2/scripts/contracts/IConfigSource.cs` | config text loading seam | ✓ WIRED | Default field and runtime constructor use `IConfigSource` at `xiuxian-2/scripts/services/LevelConfigLoader.cs:49` and `xiuxian-2/scripts/services/LevelConfigLoader.cs:214`. |
| `xiuxian-2/scripts/services/LevelConfigLoader.cs` | `xiuxian-2/scripts/contracts/IRng.cs` | RNG seam | ✓ WIRED | Default field and runtime constructor use `IRng` at `xiuxian-2/scripts/services/LevelConfigLoader.cs:50` and `xiuxian-2/scripts/services/LevelConfigLoader.cs:215`. |
| `xiuxian-2/scripts/services/LevelConfigLoader.cs` | `xiuxian-2/scripts/contracts/IClock.cs` | clock seam | ✓ WIRED | Default field and runtime constructor use `IClock` at `xiuxian-2/scripts/services/LevelConfigLoader.cs:51`, `xiuxian-2/scripts/services/LevelConfigLoader.cs:216`, and `_clock.GetUnixTimeSeconds()` at `xiuxian-2/scripts/services/LevelConfigLoader.cs:514`. |
| `xiuxian-2/scripts/services/CloudSaveSyncService.cs` | `xiuxian-2/scripts/contracts/IFileSystem.cs` | save path read/write seam | ✓ WIRED | Runtime globalizes, reads, and writes through `_fileSystem` at `xiuxian-2/scripts/services/CloudSaveSyncService.cs:115` and `xiuxian-2/scripts/services/CloudSaveSyncService.cs:134`. |
| `xiuxian-2/scripts/services/InputHookService.cs` | `xiuxian-2/scripts/contracts/IPlatformInfo.cs` | platform detection seam | ✓ WIRED | Startup logic branches through `platformInfo.IsWindows()` and `platformInfo.PlatformName` at `xiuxian-2/scripts/services/InputHookService.cs:79`. |
| `xiuxian-2/scripts/services/InputHookService.cs` | `xiuxian-2/scripts/contracts/IHookBackend.cs` | hook lifecycle seam | ✓ WIRED | Startup logic calls `hookBackend.TryStart(...)` at `xiuxian-2/scripts/services/InputHookService.cs:90`. |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| --- | --- | --- | --- | --- |
| `TEST-01` | `01-01-PLAN.md` | Developer can run one repeatable CLI test command for the repository | ✓ SATISFIED | Documented commands in `xiuxian-2/docs/TESTING_AUTOMATION.md:5` and `xiuxian-2/docs/TESTING_AUTOMATION.md:15`; both commands passed during verification. |
| `TEST-02` | `01-01-PLAN.md`, `01-02-PLAN.md` | Repository contains a reusable automated test project structure with shared fixtures, builders, and helpers | ✓ SATISFIED | Test project exists in `tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj:1`; shared builder and deterministic helpers exist under `tests/Xiuxian2.Core.Tests/Builders/` and `tests/Xiuxian2.Core.Tests/Support/Deterministic/`. |
| `TEST-06` | `01-02-PLAN.md`, `01-03-PLAN.md`, `01-04-PLAN.md`, `01-05-PLAN.md` | Core services can depend on deterministic seams for RNG, clock, filesystem, and platform/runtime boundaries | ✓ SATISFIED | Contracts exist in `xiuxian-2/scripts/contracts/`; service seam adoption is wired in `xiuxian-2/scripts/services/LevelConfigLoader.cs:49`, `xiuxian-2/scripts/services/CloudSaveSyncService.cs:73`, and `xiuxian-2/scripts/services/InputHookService.cs:29`; deterministic seam tests passed. |

All requirement IDs declared in phase plans are present in `.planning/REQUIREMENTS.md`, and no additional Phase 1 requirements are orphaned there.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
| --- | --- | --- | --- | --- |
| `xiuxian-2/scripts/adapters/platform/Win32HookBackend.cs` | 16 | CS8632 nullable annotation warning during `dotnet test` | ⚠️ Warning | Does not block the goal, but Phase 1 files still emit nullable-context build noise. |
| `xiuxian-2/scripts/services/InputHookService.cs` | 61 | CS8632 nullable annotation warning during `dotnet test` | ⚠️ Warning | Does not block deterministic seam testing, but reduces build cleanliness. |
| `xiuxian-2/scripts/services/CloudSaveSyncService.cs` | 74 | CS8632 nullable annotation warning during `dotnet test` | ⚠️ Warning | Does not block seam behavior, but leaves verification output noisy. |
| `xiuxian-2/scripts/adapters/godot/GodotConfigSource.cs` | 10 | CS8632 nullable annotation warning during `dotnet test` | ⚠️ Warning | Non-blocking warning in a Phase 1 adapter. |

### Gaps Summary

No phase-blocking gaps found. The documented CLI workflow works, the shared deterministic support layer exists and is reused, and the three target services are exercised through deterministic seams instead of live runtime boundaries.

---

_Verified: 2026-03-21T02:37:15Z_
_Verifier: OpenCode (gsd-verifier)_
