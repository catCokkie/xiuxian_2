# Testing Patterns

**Analysis Date:** 2026-03-20

## Test Framework

**Runner:**
- No automated C# test framework is detected. `xiuxian-2/xiuxian2.csproj` contains only the Godot SDK and target framework; it has no `PackageReference` entries for NUnit, xUnit, MSTest, or GDUnit.
- Config: Not detected. No `*.runsettings`, `nunit`, `xunit`, `mstest`, `gdunit`, or CI test config files are present under `xiuxian-2/`.

**Assertion Library:**
- Not detected. The current test surface uses live UI inspection, Godot output, and runtime signal wiring instead of assertion APIs.

**Run Commands:**
```bash
# Not detected: no scripted test runner is configured in `xiuxian-2/`

# Manual verification path documented in `xiuxian-2/docs/INPUT_SYSTEM_SUMMARY.md`
# Open `xiuxian-2/scenes/tests/InputSystemTest.tscn` in Godot 4 and run the scene.

# Manual regression path documented in `xiuxian-2/docs/INPUT_SYSTEM_SUMMARY.md`
# Open `xiuxian-2/scenes/PrototypeRoot.tscn` in Godot 4 and exercise gameplay flows.
```

## Test File Organization

**Location:**
- Current test code is separate rather than co-located. The only dedicated test script is `xiuxian-2/scripts/tests/InputSystemTest.cs` and its scene wrapper is `xiuxian-2/scenes/tests/InputSystemTest.tscn`.
- Manual verification guidance lives in docs instead of code comments alone, primarily in `xiuxian-2/docs/INPUT_SYSTEM_SUMMARY.md`.

**Naming:**
- Use `*Test` suffix for manual test harnesses, as shown by `xiuxian-2/scripts/tests/InputSystemTest.cs` and `xiuxian-2/scenes/tests/InputSystemTest.tscn`.
- Keep the scene and attached script names aligned one-to-one for test harnesses.

**Structure:**
```
xiuxian-2/scripts/tests/*Test.cs
xiuxian-2/scenes/tests/*Test.tscn
xiuxian-2/docs/*.md            # manual test steps and acceptance checklists
```

## Test Structure

**Suite Organization:**
```csharp
public partial class InputSystemTest : Control
{
    [Export] public NodePath ActivityStatePath = "/root/InputActivityState";
    [Export] public NodePath HookServicePath = "/root/InputHookService";

    public override void _Ready()
    {
        _activityState = GetNode<InputActivityState>(ActivityStatePath);
        _hookService = GetNode<InputHookService>(HookServicePath);

        SetupTestUI();

        if (_activityState != null)
        {
            _activityState.ActivityTick += OnActivityTick;
        }

        if (_hookService != null)
        {
            _hookService.HookStateChanged += OnHookStateChanged;
            _hookService.InputError += OnInputError;
        }

        UpdateDisplay();
    }
}
```

Pattern source: `xiuxian-2/scripts/tests/InputSystemTest.cs`

**Patterns:**
- Build a lightweight in-engine harness scene and attach a controller script, as in `xiuxian-2/scenes/tests/InputSystemTest.tscn` + `xiuxian-2/scripts/tests/InputSystemTest.cs`.
- Fetch real autoloads via exported node paths instead of building isolated fakes, as in `xiuxian-2/scripts/tests/InputSystemTest.cs` and the autoload declarations in `xiuxian-2/project.godot`.
- Subscribe to live signals in `_Ready()` and unsubscribe in `_ExitTree()` to avoid leaked handlers, following the same cleanup pattern used in production code like `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/scripts/services/ActivityConversionService.cs`, and `xiuxian-2/scripts/ui/BookTabsController.cs`.
- Refresh UI by calling `CallDeferred(nameof(UpdateDisplay))` from signal handlers so the test panel reflects post-frame state, as in `xiuxian-2/scripts/tests/InputSystemTest.cs`.

## Mocking

**Framework:**
- None detected. No mocking library or fake framework is referenced in `xiuxian-2/xiuxian2.csproj` or any `xiuxian-2/scripts/*.cs` file.

**Patterns:**
```csharp
_hookService.HookStateChanged += OnHookStateChanged;
_hookService.InputError += OnInputError;

private void OnHookStateChanged(bool isActive)
{
    GD.Print($"InputSystemTest: Hook state changed to {isActive}");
    CallDeferred(nameof(UpdateDisplay));
}
```

Pattern source: `xiuxian-2/scripts/tests/InputSystemTest.cs`

**What to Mock:**
- Not applicable in current repo state. Testing currently exercises real autoload nodes from `xiuxian-2/project.godot`, real Godot scene trees from `xiuxian-2/scenes/tests/InputSystemTest.tscn`, and real runtime controllers.

**What NOT to Mock:**
- Do not bypass the autoload wiring when validating runtime integration. The visible testing pattern is to hit `InputActivityState`, `InputHookService`, and the UI together, as documented in `xiuxian-2/docs/INPUT_SYSTEM_SUMMARY.md` and implemented in `xiuxian-2/scripts/tests/InputSystemTest.cs`.

## Fixtures and Factories

**Test Data:**
```csharp
[Export] public NodePath ActivityStatePath = "/root/InputActivityState";
[Export] public NodePath HookServicePath = "/root/InputHookService";
```

Fixture source: `xiuxian-2/scripts/tests/InputSystemTest.cs`

```ini
[autoload]
LevelConfigLoader="*res://scripts/services/LevelConfigLoader.cs"
InputActivityState="*res://scripts/services/InputActivityState.cs"
InputHookService="*res://scripts/services/InputHookService.cs"
InputPauseShortcut="*res://scripts/services/InputPauseShortcut.cs"
BackpackState="*res://scripts/services/BackpackState.cs"
ResourceWalletState="*res://scripts/services/ResourceWalletState.cs"
PlayerProgressState="*res://scripts/services/PlayerProgressState.cs"
PlayerActionState="*res://scripts/services/PlayerActionState.cs"
ActivityConversionService="*res://scripts/services/ActivityConversionService.cs"
CloudSaveSyncService="*res://scripts/services/CloudSaveSyncService.cs"
```

Fixture source: `xiuxian-2/project.godot`

**Location:**
- Runtime fixtures are implicit and scene-based. The test harness depends on project autoloads from `xiuxian-2/project.godot` and scene setup from `xiuxian-2/scenes/tests/InputSystemTest.tscn`.
- Config-heavy systems reuse design assets as runtime inputs, such as `xiuxian-2/docs/design/09_level_monster_drop_sample.json` loaded by `xiuxian-2/scripts/services/LevelConfigLoader.cs`.

## Coverage

**Requirements:**
- None enforced. No coverage target, gate, report folder, or CI enforcement is detected in `xiuxian-2/`.
- Visible coverage is manual and feature-specific. `xiuxian-2/docs/INPUT_SYSTEM_SUMMARY.md` includes a checklist for hook startup, AP display, decay behavior, pause/resume, and save persistence.

**View Coverage:**
```bash
# Not detected: no coverage command or report generator is configured in `xiuxian-2/`
```

## Test Types

**Unit Tests:**
- Not used. No isolated method-level tests or assertion-based unit test files are present in `xiuxian-2/scripts/`.

**Integration Tests:**
- The existing pattern is manual in-engine integration testing. `xiuxian-2/scripts/tests/InputSystemTest.cs` verifies interaction between autoload services, scene UI, and platform hook behavior through a live Godot `Control`.
- `xiuxian-2/docs/INPUT_SYSTEM_SUMMARY.md` describes the integration procedure: run `xiuxian-2/scenes/tests/InputSystemTest.tscn`, start the hook, input outside the editor window, and observe the stats panel.

**E2E Tests:**
- No automated E2E framework is detected.
- Manual gameplay regression uses the main scene `xiuxian-2/scenes/PrototypeRoot.tscn` plus design acceptance checklists in `xiuxian-2/docs/design/04_milestones.md`, `xiuxian-2/docs/design/06_bottom_exploration_battle.md`, and `xiuxian-2/docs/design/10_todo.md`.

## Common Patterns

**Async Testing:**
```csharp
private void OnActivityTick(double apThisSecond, double apFinal)
{
    CallDeferred(nameof(UpdateDisplay));
}
```

Pattern source: `xiuxian-2/scripts/tests/InputSystemTest.cs`

- Use signal-driven UI refresh instead of sleeps or polling loops. This matches engine timing behavior and keeps the test harness aligned with production signal flow in `xiuxian-2/scripts/services/InputActivityState.cs`.

**Error Testing:**
```csharp
private void OnInputError(string errorMessage)
{
    GD.PushError($"InputSystemTest: Input error - {errorMessage}");
}
```

Pattern source: `xiuxian-2/scripts/tests/InputSystemTest.cs`

- Surface runtime failure paths through the Godot output panel rather than assertions. Production services also emit warnings and errors via `GD.PushWarning` and `GD.PushError`, especially in `xiuxian-2/scripts/services/InputHookService.cs`, `xiuxian-2/scripts/services/LevelConfigLoader.cs`, and `xiuxian-2/scripts/game/PrototypeRootController.cs`.

## Manual Regression Checklist To Reuse

- Run `xiuxian-2/scenes/tests/InputSystemTest.tscn` when touching `xiuxian-2/scripts/services/InputActivityState.cs`, `xiuxian-2/scripts/services/InputHookService.cs`, or `xiuxian-2/scripts/services/InputPauseShortcut.cs`.
- Run `xiuxian-2/scenes/PrototypeRoot.tscn` when touching persistence or runtime orchestration in `xiuxian-2/scripts/game/PrototypeRootController.cs`, `xiuxian-2/scripts/game/ExploreProgressController.cs`, `xiuxian-2/scripts/services/LevelConfigLoader.cs`, or `xiuxian-2/scripts/services/CloudSaveSyncService.cs`.
- Verify scene file encoding stays UTF-8 without BOM for `*.tscn`, per `xiuxian-2/.editorconfig` and the warning in `xiuxian-2/docs/design/README.md`.
- Use design docs as acceptance criteria before closing work. `xiuxian-2/docs/design/README.md` requires design-first changes, and `xiuxian-2/docs/design/10_todo.md` stores checklist-style acceptance expectations.

## Current Gaps

- There is no repeatable headless test entry point in `xiuxian-2/`.
- There are no assertion-based regression tests for persistence helpers such as `ToDictionary` and `FromDictionary` in `xiuxian-2/scripts/services/*.cs` and `xiuxian-2/scripts/game/ExploreProgressController.cs`.
- There is no test isolation around platform-sensitive code in `xiuxian-2/scripts/services/InputHookService.cs` or reflection-based cloud integration in `xiuxian-2/scripts/services/CloudSaveSyncService.cs`.

---

*Testing analysis: 2026-03-20*
