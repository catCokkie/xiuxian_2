# Research Summary

## Context

This is a brownfield maintenance milestone for an existing Godot 4.5 + C# desktop cultivation prototype. The user wants to improve the existing project rather than define a new product, with emphasis on stability, stronger automated regression coverage, and fixing current UI/layout/dragging problems.

## Key Findings

### Stack

- The current stack is already appropriate for a maintenance milestone: Godot 4.5 + C# / .NET 8, autoload services, `just` workflows, PowerShell verification scripts, and xUnit for pure logic
- The highest-value stack improvement is continued extraction of deterministic logic into `scripts/core/` for testing, not introducing a new framework or heavy integration-test infrastructure

### Table Stakes

- Stable runtime behavior
- Reliable main UI and dragging behavior
- Better regression coverage around critical rules
- Limited, obvious display cleanup without expanding into a full art/asset milestone

### Architecture Guidance

- Sequence work as: UI stabilization → test expansion → runtime/persistence safety → bounded display cleanup
- Prefer local fixes, isolated rule extraction, and documentation alignment over broad redesign
- Preserve current gameplay and save contracts unless a phase explicitly changes them

### Pitfalls To Avoid

- Turning maintenance into a rewrite
- Shipping UI fixes without Godot runtime verification
- Adding tests that miss real maintenance risk
- Accidentally breaking persistence symmetry
- Letting developer tools leak into player-facing UX
- Expanding into full asset-integration work by accident

## Recommendation For Requirements / Roadmap

Use a maintenance-first roadmap with a small number of practical phases focused on:

1. Main UI stabilization
2. Regression expansion
3. Runtime/persistence safety cleanup
4. Bounded display anomaly cleanup

Do not turn this milestone into a redesign, a Steamworks release effort, or a full asset/content expansion pass.
