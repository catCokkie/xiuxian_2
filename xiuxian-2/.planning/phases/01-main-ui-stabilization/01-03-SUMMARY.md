# Plan 03 Summary

## Objective

Finalize submenu drag behavior and complete the human verification pass for Phase 1 main UI stabilization.

## Completed

- Confirmed submenu drag behavior remains whole-window based with interactive controls still blocking drag capture
- Confirmed main bar no longer locks near the top of the game window and can be dragged vertically as expected
- Confirmed Phase 1 shell remains recognizable and was stabilized rather than redesigned
- Completed the blocking Godot human verification pass for the current Phase 1 scope

## Key Files

- `scripts/ui/SubmenuWindowController.cs`
- `scripts/ui/MainBarLayoutController.cs`
- `scripts/game/PrototypeRootController.cs`
- `scenes/ui/SubmenuBookWindow.tscn`
- `scenes/ui/MainBarWindow.tscn`

## Human Verification Result

- Opened `res://scenes/PrototypeRoot.tscn`
- Verified the main bar no longer displays stuck near the top
- Verified the main bar can be dragged downward and repositioned normally
- Verified the updated position behavior works as intended
- User reported: `已通过`

## Verification

- `dotnet build xiuxian2.sln` passes
- `dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj` passes
- Human verification checkpoint passed

## Notes

- The key follow-up fix during verification was removing the old default bottom-lock behavior from `scripts/ui/MainBarLayoutController.cs` and persisting `main_bar_y` through the unified UI save state.
