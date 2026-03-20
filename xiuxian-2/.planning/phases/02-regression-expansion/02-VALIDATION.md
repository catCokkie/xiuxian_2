---
phase: 02
slug: regression-expansion
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-20
---

# Phase 02 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit on `net8.0` |
| **Config file** | `tests/xiuxian2.Tests/xiuxian2.Tests.csproj` |
| **Quick run command** | `dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj --filter "FullyQualifiedName~PlayerBreakthroughRuleTests"` |
| **Full suite command** | `dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj` |
| **Estimated runtime** | ~20 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj --filter "FullyQualifiedName~PlayerBreakthroughRuleTests"`
- **After every plan wave:** Run `dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 20 seconds

---

## Per-task Verification Map

| task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 02-01-01 | 01 | 1 | TEST-02, TEST-03 | unit | `dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj --filter "FullyQualifiedName~PlayerBreakthroughRuleTests"` | ✅ | ⬜ pending |
| 02-01-02 | 01 | 1 | TEST-02, TEST-03 | build + unit | `dotnet build xiuxian2.sln && dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj --filter "FullyQualifiedName~PlayerBreakthroughRuleTests"` | ✅ | ⬜ pending |
| 02-01-03 | 01 | 1 | TEST-01 | build + full suite | `dotnet build xiuxian2.sln && dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

All phase behaviors have automated verification.

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 60s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
