---
phase: 2
slug: config-and-serialization-contracts
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-03-21
---

# Phase 2 - Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit v3 for the default fast suite |
| **Config file** | `tests/.runsettings` |
| **Quick run command** | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj` |
| **Full suite command** | `dotnet test xiuxian-2/xiuxian2.sln --settings tests/.runsettings` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj`
- **After every plan wave:** Run `dotnet test xiuxian-2/xiuxian2.sln --settings tests/.runsettings`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-task Verification Map

| task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 02-01-01 | 01 | 1 | TEST-03 | unit | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~LevelConfigLoaderConfigContractTests --logger "console;verbosity=detailed"` | ❌ W1 | ⬜ pending |
| 02-01-02 | 01 | 1 | TEST-03 | unit | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~LevelConfigLoaderConfigContractTests|FullyQualifiedName~LevelConfigLoaderSeamTests --logger "console;verbosity=detailed"` | ❌ W1 | ⬜ pending |
| 02-02-01 | 02 | 1 | TEST-04 | unit | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~StateSerializationContractTests` | ❌ W1 | ⬜ pending |
| 02-02-02 | 02 | 1 | TEST-04 | unit | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~StateSerializationContractTests|FullyQualifiedName~LevelConfigLoaderRuntimeDictionaryTests` | ❌ W1 | ⬜ pending |
| 02-03-01 | 03 | 2 | TEST-04 | unit | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~PrototypeRootSaveContractTests` | ❌ W2 | ⬜ pending |
| 02-03-02 | 03 | 2 | TEST-04 | integration | `dotnet test xiuxian-2/xiuxian2.sln --settings tests/.runsettings --filter FullyQualifiedName~PrototypeRootSaveContractTests|FullyQualifiedName~StateSerializationContractTests|FullyQualifiedName~LevelConfigLoaderConfigContractTests` | ❌ W2 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Xiuxian2.Core.Tests/Fixtures/config/phase2-valid-level-config.json` - frozen valid config contract fixture
- [ ] `tests/Xiuxian2.Core.Tests/Fixtures/config/phase2-invalid-level-config.json` - frozen invalid config fixture for validation regressions
- [ ] `tests/Xiuxian2.Core.Tests/Fixtures/save/phase2-save-v5.cfg` - current-schema unified save snapshot fixture
- [ ] `tests/Xiuxian2.Core.Tests/Fixtures/save/phase2-legacy-save-v1.cfg` - curated legacy snapshot fixture

---

## Manual-Only Verifications

All phase behaviors have automated verification.

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
