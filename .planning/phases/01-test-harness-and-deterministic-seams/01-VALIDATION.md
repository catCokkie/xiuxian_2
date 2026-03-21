---
phase: 1
slug: test-harness-and-deterministic-seams
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-03-21
---

# Phase 1 - Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit v3 for the default fast suite |
| **Config file** | `tests/.runsettings` |
| **Quick run command** | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj` |
| **Full suite command** | `dotnet test xiuxian-2/xiuxian2.sln --settings tests/.runsettings` |
| **Estimated runtime** | ~20 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj`
- **After every plan wave:** Run `dotnet test xiuxian-2/xiuxian2.sln --settings tests/.runsettings`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 20 seconds

---

## Per-task Verification Map

| task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 01-01-01 | 01 | 1 | TEST-01 | scaffold | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj` | ❌ W1 | ⬜ pending |
| 01-01-02 | 01 | 1 | TEST-02 | unit | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~TestHarnessSmokeTests` | ❌ W1 | ⬜ pending |
| 01-01-03 | 01 | 1 | TEST-01 | docs | `dotnet test xiuxian-2/xiuxian2.sln --settings tests/.runsettings` | ❌ W1 | ⬜ pending |
| 01-02-01 | 02 | 2 | TEST-06 | unit | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~DeterministicSupportContractsTests` | ❌ W2 | ⬜ pending |
| 01-02-02 | 02 | 2 | TEST-02 | unit | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~FixtureSupportTests` | ❌ W2 | ⬜ pending |
| 01-03-01 | 03 | 3 | TEST-06 | unit | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~LevelConfigLoaderSeamTests` | ❌ W3 | ⬜ pending |
| 01-03-02 | 03 | 3 | TEST-06 | unit | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~CloudSaveSyncServiceSeamTests` | ❌ W3 | ⬜ pending |
| 01-04-01 | 04 | 3 | TEST-06 | unit | `dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj --filter FullyQualifiedName~InputHookServicePlatformTests` | ❌ W3 | ⬜ pending |
| 01-04-02 | 04 | 3 | TEST-06 | unit | `dotnet test xiuxian-2/xiuxian2.sln --settings tests/.runsettings` | ❌ W3 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj` - default fast suite scaffold for TEST-01 and TEST-02
- [ ] `tests/.runsettings` - shared runner settings for one stable CLI entry point
- [ ] `Directory.Packages.props` - shared package version pinning

---

## Manual-Only Verifications

All phase behaviors have automated verification.

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 20s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
