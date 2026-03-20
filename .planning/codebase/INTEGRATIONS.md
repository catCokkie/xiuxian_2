# External Integrations

**Analysis Date:** 2026-03-20

## APIs & External Services

**Platform SDKs:**
- Steam Cloud - optional cloud save upload/download for `user://save_state.cfg`
  - SDK/Client: reflection-based access to `Steamworks.SteamRemoteStorage` in `xiuxian-2/scripts/services/CloudSaveSyncService.cs`
  - Auth: not handled in repo; availability depends on a Steamworks-capable runtime assembly being loaded before `CloudSaveSyncService` initializes in `xiuxian-2/scripts/services/CloudSaveSyncService.cs`

**Native OS APIs:**
- Windows low-level keyboard and mouse hooks - global input capture outside the game window
  - SDK/Client: P/Invoke calls to `SetWindowsHookEx`, `UnhookWindowsHookEx`, and `CallNextHookEx` in `xiuxian-2/scripts/services/InputHookService.cs`
  - Auth: not applicable

## Data Storage

**Databases:**
- Not detected; the project does not use SQLite, server databases, or ORM packages in `xiuxian-2/xiuxian2.csproj` or `xiuxian-2/.godot/mono/temp/bin/Debug/xiuxian2.deps.json`

**File Storage:**
- Local filesystem via Godot `user://` storage and `ConfigFile`, centered on `user://save_state.cfg` in `xiuxian-2/scripts/game/PrototypeRootController.cs`
- Optional Steam Cloud mirror of the same save file through `xiuxian-2/scripts/services/CloudSaveSyncService.cs`
- Local content JSON loaded from `xiuxian-2/docs/design/09_level_monster_drop_sample.json` by `xiuxian-2/scripts/services/LevelConfigLoader.cs`

**Caching:**
- None; no cache layer or cache service is implemented under `xiuxian-2/scripts/`

## Authentication & Identity

**Auth Provider:**
- None detected
  - Implementation: the project has no login flow, token handling, OAuth client, or identity SDK under `xiuxian-2/scripts/` or `xiuxian-2/xiuxian2.csproj`

## Monitoring & Observability

**Error Tracking:**
- None; no Sentry, Crashlytics, OpenTelemetry, or similar SDK appears in `xiuxian-2/xiuxian2.csproj` or `xiuxian-2/.godot/mono/temp/bin/Debug/xiuxian2.deps.json`

**Logs:**
- Godot console logging through `GD.Print`, `GD.PushWarning`, and `GD.PushError` across services such as `xiuxian-2/scripts/services/InputHookService.cs`, `xiuxian-2/scripts/services/CloudSaveSyncService.cs`, and `xiuxian-2/scripts/services/LevelConfigLoader.cs`
- User-facing access to the local log/save folder is exposed with `OS.ShellOpen(ProjectSettings.GlobalizePath("user://"))` in `xiuxian-2/scripts/ui/BookTabsController.cs`

## CI/CD & Deployment

**Hosting:**
- Local desktop runtime only detected; no hosting platform config or live backend deployment files were found under `xiuxian-2/`

**CI Pipeline:**
- None detected; no `.github/workflows/*`, export preset file, or other pipeline configuration was found under `xiuxian-2/`

## Environment Configuration

**Required env vars:**
- None detected in code or project config under `xiuxian-2/`
- No `steam_appid`, API key, cloud credential, or similar environment variable usage was found in `xiuxian-2/scripts/**/*.cs` or `xiuxian-2/project.godot`

**Secrets location:**
- Not detected; no `.env*` files were found under `xiuxian-2/`
- Steam integration is assembly-driven rather than secret-driven, based on the reflection probe in `xiuxian-2/scripts/services/CloudSaveSyncService.cs`

## Webhooks & Callbacks

**Incoming:**
- None; the project has no HTTP server, webhook endpoint, or callback listener under `xiuxian-2/scripts/`

**Outgoing:**
- None over HTTP; no `HttpClient`, REST SDK, WebSocket client, or URL-based API integration was detected under `xiuxian-2/scripts/**/*.cs`
- Outgoing platform calls are limited to Steam remote storage in `xiuxian-2/scripts/services/CloudSaveSyncService.cs` and Win32 hook registration in `xiuxian-2/scripts/services/InputHookService.cs`

---

*Integration audit: 2026-03-20*
