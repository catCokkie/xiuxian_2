# Xiuxian 2

## What This Is

Xiuxian 2 is a Godot 4 + C# desktop cultivation pet game that lives at the bottom of the desktop and turns real keyboard and mouse activity into progression. It already includes a playable runtime with autoloaded state/services, configuration-driven combat and drops, persistence, and UI panels. The current project focus is improving the codebase's long-term maintainability by building an extensible automated test foundation around the core service layer.

## Core Value

Create a reliable automated test safety net for the core service layer so major refactors, especially around configuration and progression logic, can happen without breaking the game's main systems.

## Requirements

### Validated

- ✓ The game converts real keyboard and mouse activity into cultivation progress and activity/resource settlement — existing implementation
- ✓ The game supports multi-level, config-driven exploration, combat, drops, pity rules, and progression state — existing implementation
- ✓ The game provides desktop-bottom UI, submenu panels, local persistence, and runtime settings/state restoration — existing implementation

### Active

- [ ] Establish an extensible automated test framework for the existing Godot/C# project, starting with the core `scripts/services` layer
- [ ] Add regression-oriented automated coverage for critical service behaviors so future refactors can be performed safely
- [ ] Use the new test foundation to reduce risk when splitting or restructuring large runtime services such as `xiuxian-2/scripts/services/LevelConfigLoader.cs`

### Out of Scope

- New gameplay features or major content expansion during this milestone — the priority is testability and refactor safety
- Full UI automation as the first milestone — service-level confidence yields faster value and lower flake risk
- Shipping cloud sync, Steamworks, or release hardening work in this effort — those remain separate roadmap items

## Context

- The active game project lives in `xiuxian-2/` and uses Godot autoload services plus scene-root orchestration.
- Existing codebase mapping lives in `.planning/codebase/` and identifies major hotspots such as `xiuxian-2/scripts/services/LevelConfigLoader.cs`, `xiuxian-2/scripts/game/ExploreProgressController.cs`, and `xiuxian-2/scripts/ui/BookTabsController.cs`.
- Current testing is mostly manual; the only visible test artifact is the in-engine harness `xiuxian-2/scripts/tests/InputSystemTest.cs`.
- The existing design docs in `xiuxian-2/docs/design/` already call out "最小自动化回归用例" as an open P0 item.
- This test initiative should help both solo development refactors and future team-based regression protection.

## Constraints

- **Tech stack**: Must work within the existing Godot 4 + C# / .NET 8 architecture — avoid assuming a web/backend-style test setup
- **Brownfield**: Preserve current gameplay behavior while improving testability — the repo already contains shipped runtime logic
- **Priority**: Focus first on core services and regression safety — UI-heavy coverage can come later
- **Platform**: Some behavior is Windows-specific, especially global input hooks — tests must account for platform-dependent behavior and seams

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Prioritize core services for automated tests | Core services carry the highest logic density and are the safest place to build durable coverage first | — Pending |
| Optimize the first testing milestone for refactor safety | The main user goal is to make large files, especially config/runtime logic, safer to split and improve | — Pending |
| Treat this as a brownfield maintenance/testing initiative, not a feature milestone | Existing gameplay is already present and the highest leverage work is improving confidence and maintainability | — Pending |

---
*Last updated: 2026-03-21 after GSD project initialization*
