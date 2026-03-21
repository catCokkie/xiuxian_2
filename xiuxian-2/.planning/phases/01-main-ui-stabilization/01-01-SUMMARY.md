# Plan 01 Summary

## Objective

Stabilize main bar interaction and compact layout behavior.

## Completed

- Added blank-area drag initiation to `scripts/ui/MainBarLayoutController.cs`
- Preserved dedicated drag handle support in `scripts/ui/MainBarLayoutController.cs`
- Kept interaction guards so buttons, option buttons, progress bars, and rich text controls do not accidentally start drag
- Tightened viewport clamping during `_Process()` so the main bar stays fully visible after viewport/size changes

## Key Files

- `scripts/ui/MainBarLayoutController.cs`

## Verification

- `dotnet build xiuxian2.sln` passes
- `dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj` passes

## Notes

- No `.tscn` edit was required for this first pass because the drag/compact changes were contained in controller logic
