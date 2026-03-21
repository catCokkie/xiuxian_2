# Testing Automation

## Default local loop

Use this command for the fast Phase 1 feedback loop:

```bash
dotnet test tests/Xiuxian2.Core.Tests/Xiuxian2.Core.Tests.csproj
```

This is the default developer entry point. It runs the dedicated core test project without requiring the Godot editor, a booted scene tree, GdUnit, or any other runtime-bound harness.

## Full solution command

Use this command when you want the solution-level entry point that automation should also use:

```bash
dotnet test xiuxian-2/xiuxian2.sln --settings tests/.runsettings
```

The shared `tests/.runsettings` file keeps the CLI path stable across local development and automation runs.

## Phase 1 rules

- Fast service-level tests are the default loop for this milestone.
- New automated coverage should land in `tests/Xiuxian2.Core.Tests/` unless a later phase explicitly introduces another suite.
- Godot runtime tests are not part of the default feedback path in Phase 1.
- Windows hook behavior and scene-driven smoke coverage stay outside the plain `dotnet test` loop for now.

## When to use each command

- Use the project command while iterating on service code and shared deterministic test support.
- Use the solution command before closing plan work so the solution wiring and shared settings path stay green.
