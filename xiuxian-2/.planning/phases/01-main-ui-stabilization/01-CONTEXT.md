# Phase 1: Main UI Stabilization - Context

**Gathered:** 2026-03-19
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix the main UI behavior users feel immediately: overlapping controls and ineffective dragging for the main bar and submenu.

This phase clarifies how to stabilize the existing shell. It does not add new gameplay capabilities, replace the current UI system, or expand into a full visual redesign.

</domain>

<decisions>
## Implementation Decisions

### Drag behavior
- Main bar should support both the dedicated drag handle and dragging from non-interactive blank areas.
- Submenu/book window should keep the current whole-window drag behavior, with interactive controls continuing to block drag capture.
- Both the main bar and submenu must remain fully visible inside the viewport while dragging.
- Dragged positions should be remembered immediately when dragging ends, and restored on later use/restart.

### Claude's Discretion
- Exact hit-testing rules for what counts as a "non-interactive blank area" on the main bar.
- Whether drag persistence uses the existing save path directly or routes through the current dirty/save orchestration in the least invasive way.
- Exact guard logic for viewport resizing after saved positions are restored.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase scope and requirements
- `.planning/ROADMAP.md` — Phase 1 goal, requirements, and success criteria
- `.planning/REQUIREMENTS.md` — `UI-01`, `UI-02`, `UI-03`, `UI-04` scope anchors for this phase
- `.planning/PROJECT.md` — project-wide maintenance boundaries and non-goals

### Codebase references
- `.planning/codebase/ARCHITECTURE.md` — runtime/controller/service boundaries relevant to UI stabilization work
- `.planning/codebase/STRUCTURE.md` — where main bar, submenu, and related controllers live
- `.planning/codebase/CONCERNS.md` — known maintenance risks around UI controllers and runtime coupling

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `scripts/ui/MainBarLayoutController.cs`: existing drag, resize, bottom-lock, compact-layout, and `LayoutChanged` signal logic for the main bar
- `scripts/ui/SubmenuWindowController.cs`: existing submenu drag implementation with interaction blocking via `IsNodeInteractionTarget()`
- `scripts/game/PrototypeRootController.cs`: existing save/load wiring for `ui.main_bar_x`, `ui.main_bar_width`, and submenu visibility

### Established Patterns
- Main bar currently uses a dedicated drag handle (`Chrome/DragHandleButton`) and emits `LayoutChanged` during drag/resize in `scripts/ui/MainBarLayoutController.cs`
- Main bar currently locks to the bottom via `LockToBottom` and `GetBottomLockedY()` in `scripts/ui/MainBarLayoutController.cs`
- Submenu currently allows whole-window dragging except over interactive controls in `scripts/ui/SubmenuWindowController.cs`
- Main bar position persistence already exists for X/width in `scripts/game/PrototypeRootController.cs`; this phase should extend/fix behavior rather than invent a new persistence path

### Integration Points
- `scenes/ui/MainBarWindow.tscn` — main bar control hierarchy, drag handle, resize handle, compact-layout participants
- `scenes/ui/SubmenuBookWindow.tscn` — submenu/book window root and draggable area
- `scripts/ui/MainBarLayoutController.cs` — primary integration point for main bar drag and overlap fixes
- `scripts/ui/SubmenuWindowController.cs` — primary integration point for submenu drag fixes
- `scripts/game/PrototypeRootController.cs` — persistence and runtime restore integration point for remembered layout state

</code_context>

<specifics>
## Specific Ideas

- Preserve the current shell instead of redesigning it.
- Main bar drag should feel easier to use than the current handle-only behavior.
- Submenu should keep its current whole-window drag feel rather than being converted into a title-bar-only window.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 01-main-ui-stabilization*
*Context gathered: 2026-03-19*
