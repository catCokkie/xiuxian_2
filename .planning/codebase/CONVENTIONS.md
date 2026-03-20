# Coding Conventions

**Analysis Date:** 2026-03-20

## Naming Patterns

**Files:**
- Use PascalCase class-per-file names for C# scripts under `xiuxian-2/scripts/`, such as `xiuxian-2/scripts/services/InputActivityState.cs`, `xiuxian-2/scripts/game/PrototypeRootController.cs`, and `xiuxian-2/scripts/ui/MainBarLayoutController.cs`.
- Keep Godot scene names aligned with the attached controller or purpose, such as `xiuxian-2/scenes/PrototypeRoot.tscn`, `xiuxian-2/scenes/ui/MainBarWindow.tscn`, and `xiuxian-2/scenes/tests/InputSystemTest.tscn`.
- Use uppercase snake case for planning doc filenames under `xiuxian-2/docs/design/`, such as `xiuxian-2/docs/design/03_progression_and_balance.md` and `xiuxian-2/docs/design/10_todo.md`.

**Functions:**
- Use PascalCase for public and private methods, including Godot callbacks and helpers: `_Ready`, `_Process`, `LoadConfig`, `TrySetActiveLevel`, `RefreshValidationPanel`, `ApplySettlement`, and `UpdateDisplay` in `xiuxian-2/scripts/services/LevelConfigLoader.cs`, `xiuxian-2/scripts/game/ExploreProgressController.cs`, and `xiuxian-2/scripts/tests/InputSystemTest.cs`.
- Prefix boolean-returning guard methods with `Is`, `Can`, or `Try`, such as `IsWindowsPlatform` in `xiuxian-2/scripts/services/InputHookService.cs`, `CanBreakthrough` in `xiuxian-2/scripts/services/PlayerProgressState.cs`, and `TryDownloadToLocal` in `xiuxian-2/scripts/services/CloudSaveSyncService.cs`.
- Use `On...` for signal and UI event handlers, such as `OnActivityTick`, `OnInputBatchTick`, `OnBreakthroughPressed`, `OnHookStateChanged`, and `OnButtonPressed` in `xiuxian-2/scripts/game/ExploreProgressController.cs`, `xiuxian-2/scripts/tests/InputSystemTest.cs`, and `xiuxian-2/scripts/ui/PauseToggleButton.cs`.

**Variables:**
- Use `_camelCase` for private fields, such as `_activityState`, `_retryCooldown`, `_bookTabs`, `_validationPanelEnabled`, and `_leftTween` in `xiuxian-2/scripts/services/InputHookService.cs`, `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/scripts/game/ExploreProgressController.cs`, and `xiuxian-2/scripts/ui/BookTabsController.cs`.
- Use PascalCase for exported inspector fields and public state, such as `ActivityStatePath`, `SettlementIntervalSeconds`, `ProgressPerInput`, `Lingqi`, and `RealmLevel` in `xiuxian-2/scripts/services/ActivityConversionService.cs`, `xiuxian-2/scripts/game/ExploreProgressController.cs`, `xiuxian-2/scripts/services/ResourceWalletState.cs`, and `xiuxian-2/scripts/services/PlayerProgressState.cs`.
- Use descriptive constant names for runtime keys and modes, such as `UnifiedStatePath`, `SaveSchemaVersion`, `ModeDungeon`, and `ModeCultivation` in `xiuxian-2/scripts/game/PrototypeRootController.cs` and `xiuxian-2/scripts/services/PlayerActionState.cs`.

**Types:**
- Use PascalCase for classes and static text containers, such as `InputHookService`, `CloudSaveSyncService`, `SubmenuWindowController`, and `UiText` in `xiuxian-2/scripts/services/InputHookService.cs`, `xiuxian-2/scripts/services/CloudSaveSyncService.cs`, `xiuxian-2/scripts/ui/SubmenuWindowController.cs`, and `xiuxian-2/scripts/ui/UiText.cs`.
- Keep namespaces folder-aligned when a namespace is present, such as `Xiuxian.Scripts.Services`, `Xiuxian.Scripts.Game`, `Xiuxian.Scripts.UI`, and `Xiuxian.Scripts.Tests` in `xiuxian-2/scripts/services/*.cs`, `xiuxian-2/scripts/game/*.cs`, `xiuxian-2/scripts/ui/PauseToggleButton.cs`, and `xiuxian-2/scripts/tests/InputSystemTest.cs`.
- Some UI root scripts intentionally omit a namespace, such as `xiuxian-2/scripts/ui/MainBarLayoutController.cs`, `xiuxian-2/scripts/ui/BookTabsController.cs`, `xiuxian-2/scripts/ui/SubmenuWindowController.cs`, and `xiuxian-2/scripts/ui/UiText.cs`. Match the existing file’s choice instead of mixing styles within a file.

## Code Style

**Formatting:**
- Tool used: `.editorconfig` only is detected in `xiuxian-2/.editorconfig`; no repo-level formatter config is detected.
- `*.cs` and `*.md` are stored as `utf-8-bom`, while `*.tscn` are stored as plain `utf-8` per `xiuxian-2/.editorconfig`.
- Keep braces on their own lines and indent with 4 spaces, as shown throughout `xiuxian-2/scripts/services/InputActivityState.cs` and `xiuxian-2/scripts/services/PlayerProgressState.cs`.
- Prefer one guard clause per invalid branch before main logic, such as `if (amount <= 0.0) return;` style checks in `xiuxian-2/scripts/services/ResourceWalletState.cs` and `xiuxian-2/scripts/services/BackpackState.cs`.

**Linting:**
- Tool used: Not detected. No `.ruleset`, Roslyn analyzer config, Rider settings, or CI lint workflow is present in `xiuxian-2/`.
- Nullability warnings are handled by convention rather than enforced annotations. Required scene nodes are commonly stored as `null!` fields populated in `_Ready`, such as `_dragHandle`, `_leftContentLabel`, and `_progressBar` in `xiuxian-2/scripts/ui/MainBarLayoutController.cs`, `xiuxian-2/scripts/ui/BookTabsController.cs`, and `xiuxian-2/scripts/game/ExploreProgressController.cs`.
- Optional dependencies use nullable references plus `GetNodeOrNull<T>()`, such as `_actionModeOptionButton`, `_hookService`, and `_levelConfigLoader` in `xiuxian-2/scripts/ui/MainBarLayoutController.cs`, `xiuxian-2/scripts/game/PrototypeRootController.cs`, and `xiuxian-2/scripts/game/ExploreProgressController.cs`.

## Import Organization

**Order:**
1. `using Godot;`
2. `using System...` namespaces
3. project namespaces such as `using Xiuxian.Scripts.Services;`

**Path Aliases:**
- No C# path alias system is detected.
- Godot node and resource paths are hard-coded as exported `NodePath` or `res://` strings, such as `"/root/InputActivityState"` in `xiuxian-2/scripts/services/InputHookService.cs` and `"res://docs/design/09_level_monster_drop_sample.json"` in `xiuxian-2/scripts/services/LevelConfigLoader.cs`.

## Error Handling

**Patterns:**
- Use early returns for invalid or missing data before mutating state, such as `AddLingqi`, `RegisterMouseScroll`, and `TrySetActiveLevel` in `xiuxian-2/scripts/services/ResourceWalletState.cs`, `xiuxian-2/scripts/services/InputActivityState.cs`, and `xiuxian-2/scripts/services/LevelConfigLoader.cs`.
- Prefer non-throwing lookups with defaults when reading persistence dictionaries: `ContainsKey(...) ? ... : fallback` and `Variant.AsInt32/AsDouble/AsString` appear throughout `xiuxian-2/scripts/services/InputActivityState.cs`, `xiuxian-2/scripts/services/PlayerProgressState.cs`, `xiuxian-2/scripts/services/PlayerActionState.cs`, `xiuxian-2/scripts/game/ExploreProgressController.cs`, and `xiuxian-2/scripts/game/PrototypeRootController.cs`.
- Use `GetNodeOrNull<T>()` when a dependency is optional and degrade gracefully with warnings, as in `xiuxian-2/scripts/game/PrototypeRootController.cs` and `xiuxian-2/scripts/services/ActivityConversionService.cs`.
- Use `try/catch` only around external boundaries such as Win32 hooks or reflection-backed integrations, as in `xiuxian-2/scripts/services/InputHookService.cs` and `xiuxian-2/scripts/services/CloudSaveSyncService.cs`.

## Logging

**Framework:** Godot `GD.Print`, `GD.PushWarning`, and `GD.PushError`

**Patterns:**
- Use prefixed log messages with the class name, such as `"InputHookService: ..."`, `"PrototypeRootController: ..."`, and `"CloudSaveSyncService: ..."` in `xiuxian-2/scripts/services/InputHookService.cs`, `xiuxian-2/scripts/game/PrototypeRootController.cs`, and `xiuxian-2/scripts/services/CloudSaveSyncService.cs`.
- Use warnings for recoverable missing dependencies or degraded modes, such as missing autoload nodes in `xiuxian-2/scripts/services/ActivityConversionService.cs` and unsupported platform fallback in `xiuxian-2/scripts/services/InputHookService.cs`.
- Use errors for broken required wiring, such as missing `InputActivityState` in `xiuxian-2/scripts/services/InputHookService.cs` and missing `InputHookService` in `xiuxian-2/scripts/ui/PauseToggleButton.cs`.
- Use informational prints for milestone state changes, such as hook start/stop in `xiuxian-2/scripts/services/InputHookService.cs`, realm level-up in `xiuxian-2/scripts/services/PlayerProgressState.cs`, and cloud sync status in `xiuxian-2/scripts/services/CloudSaveSyncService.cs`.

## Comments

**When to Comment:**
- Add comments for engine-specific or platform-specific behavior, such as Win32 hook notes in `xiuxian-2/scripts/services/InputHookService.cs`, input-driven progression notes in `xiuxian-2/scripts/game/ExploreProgressController.cs`, and save-format guidance in `xiuxian-2/docs/design/README.md`.
- Keep comments sparse around obvious code. Most service/state methods in `xiuxian-2/scripts/services/BackpackState.cs`, `xiuxian-2/scripts/services/ResourceWalletState.cs`, and `xiuxian-2/scripts/services/PlayerActionState.cs` rely on names instead of inline commentary.
- Preserve design rules in docs first. `xiuxian-2/docs/design/README.md` explicitly requires design changes to land in docs before code and centralizes text in `xiuxian-2/scripts/ui/UiText.cs`.

**JSDoc/TSDoc:**
- XML doc comments are used on many classes and a few methods, such as `InputActivityState`, `InputHookService`, `CloudSaveSyncService`, `LevelConfigLoader`, and `PrototypeRootController` in `xiuxian-2/scripts/services/*.cs` and `xiuxian-2/scripts/game/PrototypeRootController.cs`.
- XML docs usually explain purpose and constraints, not every member. Follow the existing pattern: document non-obvious controllers and services, skip trivial setters/getters.

## Function Design

**Size:**
- Small state containers keep methods short and focused, such as `AddItem`, `ToDictionary`, and `FromDictionary` in `xiuxian-2/scripts/services/BackpackState.cs`.
- Complex Godot controllers are allowed to become large and own multiple concerns when they coordinate scene state, persistence, and debug UI. `xiuxian-2/scripts/game/ExploreProgressController.cs` and `xiuxian-2/scripts/ui/BookTabsController.cs` are the reference pattern for large orchestration files.

**Parameters:**
- Use concrete Godot/C# primitives in signals and handlers, such as `double`, `int`, `string`, and `bool` in `xiuxian-2/scripts/services/InputActivityState.cs`, `xiuxian-2/scripts/services/ResourceWalletState.cs`, and `xiuxian-2/scripts/ui/SubmenuWindowController.cs`.
- Pass dictionaries only at persistence and config boundaries, such as `Godot.Collections.Dictionary<string, Variant>` in `xiuxian-2/scripts/services/InputActivityState.cs`, `xiuxian-2/scripts/services/LevelConfigLoader.cs`, and `xiuxian-2/scripts/game/ExploreProgressController.cs`.
- Use exported `NodePath` properties for scene wiring instead of constructor injection, as shown in `xiuxian-2/scripts/services/ActivityConversionService.cs`, `xiuxian-2/scripts/services/InputPauseShortcut.cs`, and `xiuxian-2/scripts/game/ExploreProgressController.cs`.

**Return Values:**
- Use `bool` plus `out` parameters for non-throwing lookups, such as `TryGetMonster` and `TryGetDropTable` in `xiuxian-2/scripts/services/LevelConfigLoader.cs`.
- Use `ToDictionary`/`FromDictionary` pairs for serializable runtime state in `xiuxian-2/scripts/services/InputActivityState.cs`, `xiuxian-2/scripts/services/PlayerProgressState.cs`, `xiuxian-2/scripts/services/ResourceWalletState.cs`, `xiuxian-2/scripts/services/PlayerActionState.cs`, and `xiuxian-2/scripts/game/ExploreProgressController.cs`.
- Use computed properties for derived state instead of cached duplicates where possible, such as `RealmExpRequired` and `CanBreakthrough` in `xiuxian-2/scripts/services/PlayerProgressState.cs`.

## Module Design

**Exports:**
- Use one public class per script and let Godot attach that class directly to scenes or autoloads, such as `xiuxian-2/scripts/services/InputHookService.cs` and `xiuxian-2/scripts/ui/SubmenuWindowController.cs`.
- Use Godot partial classes consistently for engine-bound types, including all scripts under `xiuxian-2/scripts/`.
- Separate modules by role: autoload state/services under `xiuxian-2/scripts/services/`, scene orchestration under `xiuxian-2/scripts/game/`, and UI controllers/text under `xiuxian-2/scripts/ui/`.

**Barrel Files:**
- Not used. No barrel or aggregator C# file is present in `xiuxian-2/scripts/`.

## Real Patterns To Reuse

```csharp
// Export scene wiring and guard missing dependencies.
[Export] public NodePath ActivityStatePath = "/root/InputActivityState";

public override void _Ready()
{
    _activityState = GetNodeOrNull<InputActivityState>(ActivityStatePath);
    if (_activityState == null)
    {
        GD.PushWarning("ActivityConversionService: missing required autoload state node(s).");
        return;
    }
}
```

Pattern source: `xiuxian-2/scripts/services/ActivityConversionService.cs`

```csharp
public Godot.Collections.Dictionary<string, Variant> ToDictionary()
{
    return new Godot.Collections.Dictionary<string, Variant>
    {
        ["realm_level"] = RealmLevel,
        ["realm_exp"] = RealmExp,
        ["pet_mood"] = PetMood
    };
}

public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
{
    RealmLevel = data.ContainsKey("realm_level") ? Math.Max(1, data["realm_level"].AsInt32()) : 1;
    RealmExp = data.ContainsKey("realm_exp") ? Math.Max(0.0, data["realm_exp"].AsDouble()) : 0.0;
    PetMood = data.ContainsKey("pet_mood") ? Math.Clamp(data["pet_mood"].AsInt32(), 0, 100) : 60;
    EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
}
```

Pattern source: `xiuxian-2/scripts/services/PlayerProgressState.cs`

```csharp
_mainBar.LayoutChanged += (_, _) => MarkDirty();
_submenu.VisibilityChanged += _ => MarkDirty();
_bookTabs.ActiveTabsChanged += (_, _) =>
{
    RefreshRuntimeSettingsFromBookTabs();
    MarkDirty();
};
```

Pattern source: `xiuxian-2/scripts/game/PrototypeRootController.cs`

---

*Convention analysis: 2026-03-20*
