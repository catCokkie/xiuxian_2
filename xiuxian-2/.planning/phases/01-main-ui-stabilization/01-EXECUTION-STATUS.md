# Phase 1 Execution Status

## Phase

- Phase: `01-main-ui-stabilization`
- Goal: Fix the main UI behavior users feel immediately: overlapping controls and ineffective dragging for the main bar and submenu.

## Plan Status

| Plan | Status | Notes |
|------|--------|-------|
| `01-01` | Complete | Main bar drag capture expanded beyond handle and compact behavior tightened |
| `01-02` | Complete | Unified UI persistence extended for submenu position and main bar Y state |
| `01-03` | Complete | Human verification passed after top-lock fix and drag behavior validation |

## Delivered Outcomes

- Main bar supports drag from both the drag handle and non-interactive blank areas
- Main bar can now move vertically and no longer remains stuck near the top of the preview/game window
- Main bar and submenu positions are kept inside the viewport
- Unified save state now persists:
  - `main_bar_x`
  - `main_bar_y`
  - `main_bar_width`
  - `submenu_x`
  - `submenu_y`
- Save documentation was updated to reflect the new UI layout fields

## Verification Evidence

- Automated verification:
  - `dotnet build xiuxian2.sln` — pass
  - `dotnet test tests/xiuxian2.Tests/xiuxian2.Tests.csproj` — pass
- Human verification:
  - User confirmed the top-lock issue is fixed and dragging now works: `已通过`

## Remaining Notes

- Existing repository warnings remain, but no new blocking build/test failures were introduced by Phase 1 work
- Phase 1 is functionally complete from the current execution perspective
