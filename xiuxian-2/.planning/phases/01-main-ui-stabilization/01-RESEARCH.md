# Phase 1 Research: Main UI Stabilization

## Objective

Research what matters for planning Phase 1: fixing overlapping controls and ineffective dragging for the main bar and submenu, while preserving the current shell.

## Existing Implementation Findings

### Main Bar

- Primary controller: `scripts/ui/MainBarLayoutController.cs`
- Scene: `scenes/ui/MainBarWindow.tscn`
- Current drag implementation:
  - Dedicated handle only via `Chrome/DragHandleButton`
  - Starts in `OnDragHandleGuiInput()`
  - Moves in `_Input()` while `_isDragging`
  - Emits `LayoutChanged` during drag/resize and on release
- Current positioning constraints:
  - X is clamped to viewport width in `_Input()` and `ApplyLayout()`
  - Y is forced by `LockToBottom` and `GetBottomLockedY()`
  - Main bar is intentionally a bottom-docked control, not a fully free-floating window
- Current overlap handling:
  - `UpdateRightAnchoredLayout()` dynamically resizes and repositions right-side controls
  - Compact mode triggers when `Size.X <= 920`
  - Validation panel already hides if available width becomes too small
  - `ActionModeOptionButton` and `LevelOptionButton` are already hidden by default in the current maintenance state

### Submenu

- Primary controller: `scripts/ui/SubmenuWindowController.cs`
- Scene: `scenes/ui/SubmenuBookWindow.tscn`
- Current drag implementation:
  - Whole-window drag model using `TryBeginDrag()`
  - Dragging is blocked when hovered control is interactive (`BaseButton`, `RichTextLabel`, `LineEdit`, `TextEdit`, `ItemList`, `Tree`)
  - Position is clamped fully inside viewport on both X and Y
- Current restore behavior:
  - Default position only via `DefaultPosition`
  - Visibility is persisted, but position is not currently saved in the unified UI state

### Save / Restore Integration

- `scripts/game/PrototypeRootController.cs` currently persists:
  - `ui.main_bar_x`
  - `ui.main_bar_width`
  - `ui.submenu_visible`
  - active left/right tabs
- It does **not** currently persist submenu X/Y
- Main bar Y is recomputed from viewport height; restore does not use a saved Y value

## Practical Constraints For Planning

### What Should Be Preserved

- Keep the main bar bottom-docked
- Keep the existing shell and scene hierarchy
- Keep submenu whole-window drag semantics
- Keep drag positions fully inside viewport
- Use current dirty/save orchestration instead of inventing a parallel persistence path

### What Needs To Change

- Main bar drag must no longer be limited to the handle only
- Main bar drag must exclude interactive controls while still allowing blank-area drag
- Main bar overlap logic needs explicit priority decisions so core information stays readable
- Submenu position must be remembered if the product expectation is “restore later use/restart”

## Risks And Pitfalls

- `PrototypeRootController` is the save orchestration choke point; schema drift here can create silent restore bugs
- `MainBarLayoutController` is already dense; drag UX and compact layout changes should stay local and behavior-preserving
- Manual Godot verification is still required because build/test success does not prove drag feel or visual non-overlap

## Planning Recommendations

1. Split work into separate plans for:
   - main bar drag + overlap behavior
   - UI layout persistence contract
   - submenu drag + manual verification
2. Keep `MainBarLayoutController.cs` and `SubmenuWindowController.cs` as primary implementation points
3. Treat `PrototypeRootController.cs` as the persistence integration boundary
4. Include at least one manual verification checkpoint or explicit post-plan verification path, because drag behavior is user-perceptual

## Canonical File Set For Planning

- `scripts/ui/MainBarLayoutController.cs`
- `scripts/ui/SubmenuWindowController.cs`
- `scripts/game/PrototypeRootController.cs`
- `scenes/ui/MainBarWindow.tscn`
- `scenes/ui/SubmenuBookWindow.tscn`
- `.planning/phases/01-main-ui-stabilization/01-CONTEXT.md`
