# Technology Stack

**Analysis Date:** 2026-03-20

## Languages

**Primary:**
- C# / .NET 8 - gameplay, UI, state, persistence, and platform services in `xiuxian-2/scripts/**/*.cs`

**Secondary:**
- Godot scene/config formats - project wiring and scene composition in `xiuxian-2/project.godot` and `xiuxian-2/scenes/**/*.tscn`
- JSON - content/config data loaded at runtime from `xiuxian-2/docs/design/09_level_monster_drop_sample.json` via `xiuxian-2/scripts/services/LevelConfigLoader.cs`
- Markdown - design and system documentation in `xiuxian-2/docs/**/*.md`

## Runtime

**Environment:**
- Godot 4.5 with C# support, declared by `config/features=PackedStringArray("4.5", "C#", "Forward Plus")` in `xiuxian-2/project.godot`
- .NET runtime target `net8.0` in `xiuxian-2/xiuxian2.csproj`
- Conditional Android target `net9.0` when `$(GodotTargetPlatform) == 'android'` in `xiuxian-2/xiuxian2.csproj`

**Package Manager:**
- NuGet/MSBuild restore through the Godot .NET SDK, driven by `xiuxian-2/xiuxian2.csproj`
- Lockfile: missing; no `packages.lock.json` or other dependency lockfile detected under `xiuxian-2/`

## Frameworks

**Core:**
- Godot.NET.Sdk `4.5.1` - project SDK and engine integration in `xiuxian-2/xiuxian2.csproj`
- Godot engine APIs - scene tree, UI, persistence, file IO, platform APIs, and autoload services across `xiuxian-2/scripts/**/*.cs`

**Testing:**
- Ad hoc in-engine test scene rather than a standalone test framework, implemented in `xiuxian-2/scripts/tests/InputSystemTest.cs` and `xiuxian-2/scenes/tests/InputSystemTest.tscn`

**Build/Dev:**
- `GodotSharp` `4.5.1` - .NET runtime bindings present in `xiuxian-2/.godot/mono/temp/bin/Debug/xiuxian2.deps.json`
- `GodotSharpEditor` `4.5.1` - editor-side C# integration present in `xiuxian-2/.godot/mono/temp/bin/Debug/xiuxian2.deps.json`
- `Godot.SourceGenerators` `4.5.1` - source generation for Godot C# glue in `xiuxian-2/.godot/mono/temp/bin/Debug/xiuxian2.deps.json`

## Key Dependencies

**Critical:**
- `Godot.NET.Sdk/4.5.1` - the project cannot build or run as a C# Godot game without it; declared in `xiuxian-2/xiuxian2.csproj`
- `GodotSharp/4.5.1` - provides the managed Godot API surface used by every script in `xiuxian-2/scripts/**/*.cs`

**Infrastructure:**
- Win32 `user32.dll` / `kernel32.dll` - native Windows input hooks used by `xiuxian-2/scripts/services/InputHookService.cs`
- Optional `Steamworks.SteamRemoteStorage` assembly - discovered by reflection for cloud save sync in `xiuxian-2/scripts/services/CloudSaveSyncService.cs`
- `Microsoft.NETCore.App 8.0.0` - runtime framework recorded in `xiuxian-2/.godot/mono/temp/bin/Debug/xiuxian2.runtimeconfig.json`

## Configuration

**Environment:**
- No `.env*` files detected under `xiuxian-2/`
- Runtime configuration is file- and scene-driven rather than env-var driven; project bootstrap lives in `xiuxian-2/project.godot`
- Main scene is `res://scenes/PrototypeRoot.tscn`, defined in `xiuxian-2/project.godot`
- Ten global autoload services are registered in `xiuxian-2/project.godot`: `LevelConfigLoader`, `InputActivityState`, `InputHookService`, `InputPauseShortcut`, `BackpackState`, `ResourceWalletState`, `PlayerProgressState`, `PlayerActionState`, `ActivityConversionService`, and `CloudSaveSyncService`

**Build:**
- Core build/project files: `xiuxian-2/xiuxian2.csproj`, `xiuxian-2/xiuxian2.sln`, and `xiuxian-2/project.godot`
- Editor encoding rules live in `xiuxian-2/.editorconfig`
- No CI config, export preset file, Dockerfile, or package manifest beyond the Godot/.NET project files was detected under `xiuxian-2/`

## Platform Requirements

**Development:**
- Godot 4.5 with C#/.NET support is required to open and run `xiuxian-2/project.godot`
- .NET 8 SDK/runtime is required by `xiuxian-2/xiuxian2.csproj` and `xiuxian-2/.godot/mono/temp/bin/Debug/xiuxian2.runtimeconfig.json`
- Windows is required for full global input-hook behavior because `xiuxian-2/scripts/services/InputHookService.cs` calls Win32 APIs; non-Windows platforms fall back to in-app input capture only

**Production:**
- Desktop Godot runtime is the active target; the main playable scene is `xiuxian-2/scenes/PrototypeRoot.tscn`
- Windows is the feature-complete platform because the global input system in `xiuxian-2/scripts/services/InputHookService.cs` is Windows-specific
- Optional Steam cloud behavior is only active when a compatible Steamworks assembly is present at runtime, as implemented in `xiuxian-2/scripts/services/CloudSaveSyncService.cs`

---

*Stack analysis: 2026-03-20*
