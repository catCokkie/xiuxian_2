# Conventions

## Encoding And File Format Rules

- `.editorconfig` is authoritative for file encoding
- `*.cs` uses `utf-8-bom`
- `*.md` uses `utf-8-bom`
- `*.tscn` uses UTF-8 without BOM
- `AGENTS.md` extends the rule to `*.tres` and `*.gdshader` as UTF-8 without BOM
- Scene/resource BOM regressions are guarded by `scripts/tools/check-bom.ps1`

## C# Naming Patterns

- Public types/methods/properties: PascalCase
- Private fields: `_camelCase`
- Constants: PascalCase, for example `SaveSchemaVersion`
- Event handlers: `OnXxx...`, for example `OnInputBatchTick`
- Partial files use `ClassName.Feature.cs`, for example `ExploreProgressController.Battle.cs`

## Namespace And Imports

- Typical import order is described in `AGENTS.md`
- Common pattern:
  1. `using Godot;`
  2. `using System...`
  3. project namespaces such as `Xiuxian.Scripts.Services`
- Gameplay and service files usually live in `Xiuxian.Scripts.*`

## Godot Usage Patterns

- Signals use `[Signal]` plus delegate declarations
- Subscription happens in `_Ready`, cleanup in `_ExitTree`
- Node access prefers `GetNodeOrNull<T>()` and null checks over unsafe assumptions
- `null!` is used for `_Ready`-initialized fields where the node is guaranteed after ready-time wiring
- Variant/JSON boundaries use `Godot.Collections.Dictionary` and `Godot.Collections.Array`

## Code Style

- 4-space indentation
- K&R braces
- Prefer early-return guard clauses
- Prefer small helper extraction when logic becomes dense
- Large controllers/services are often decomposed by `partial class` rather than deep inheritance

## Error Handling And Logging

- Empty `catch` blocks are forbidden by repository convention
- Recoverable issues use `GD.PushWarning`
- Serious runtime problems use `GD.PushError`
- Debug output uses `GD.Print`, but `AGENTS.md` warns against noisy per-frame spam

## Gameplay-Specific Rules

- Exploration progress must be driven by `InputActivityState.InputBatchTick`
- AP is for resource settlement, not direct exploration progression
- User-facing text should be centralized in `scripts/ui/UiText.cs`

## Save And Scene Safety Rules

- Save read/write structure is orchestrated in `scripts/game/PrototypeRootController.cs`
- Save-key changes must update both serialization and deserialization paths
- Node name/path changes in `.tscn` files must update all `GetNode*` references
- Runtime config/schema changes should be revalidated through `LevelConfigLoader` and manual smoke tests

## UI And Asset Rules

- Favor stable behavior before visual polish
- Keep scene anchor/offset semantics intact when editing `.tscn` files
- Asset integration should be non-destructive and tolerate placeholder fallback paths
- New UI/player-facing text should route through `UiText.cs`

## Testing And Verification Conventions

- Use `just build`, `just verify`, and `just verify-runtime` as canonical workflow entry points
- Automated tests currently target pure logic under `scripts/core/`
- Manual Godot runtime checks remain part of the standard verification flow

## Documentation Maintenance Rules

- `README.md` is the maintainer entry point
- `docs/SAVE_SYSTEM.md` is the canonical save-schema reference
- `docs/design/10_todo.md` is the actionable backlog
- Backlog notes are kept in sync when significant maintenance items are completed
