# Plan 02 Summary

## Objective

Extend the UI layout persistence contract for Phase 1.

## Completed

- Added submenu X/Y persistence to the unified `ui` save section in `scripts/game/PrototypeRootController.cs`
- Restored submenu position through controller wiring via `ApplySavedLayout()`
- Added `LayoutChanged` emission on submenu drag release in `scripts/ui/SubmenuWindowController.cs`
- Documented submenu position keys in `docs/SAVE_SYSTEM.md`

## Key Files

- `scripts/game/PrototypeRootController.cs`
- `scripts/ui/SubmenuWindowController.cs`
- `docs/SAVE_SYSTEM.md`

## Verification

- `dotnet build xiuxian2.sln` passes
- `dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj` passes

## Notes

- Main bar persistence remains X/width-based because the bar stays bottom-docked by design
