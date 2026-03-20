# Save System

Maintainer notes for the unified save schema used by this repository.

## Overview

- Primary save file: `user://save_state.cfg`
- Unified save owner: `scripts/game/PrototypeRootController.cs`
- Current schema version: `5`
- Cloud sync local path: `user://save_state.cfg`
- Cloud sync remote file name: `save_state.cfg`

The project writes one unified `ConfigFile` save and routes each subsection through the runtime state object that owns it.

## Save Flow

- Load entry: `PrototypeRootController.LoadAllState()`
- Unified load: `PrototypeRootController.LoadUnifiedState()`
- Legacy fallback: `PrototypeRootController.LoadLegacyState()`
- Save entry: `PrototypeRootController.SaveAllState()`
- Save trigger style: dirty-marked debounce save, plus save on window close

If unified load fails, the project falls back to legacy files and immediately rewrites unified state.

## Files

- Unified save: `user://save_state.cfg`
- Legacy UI save: `user://ui_state.cfg`
- Legacy game save: `user://game_state.cfg`

## Top-Level Sections

The unified save currently writes these sections:

- `meta`
- `ui`
- `input`
- `backpack`
- `resource`
- `progress`
- `action`
- `explore`
- `level`
- `settings`

## Section Schema

### `meta`

- `version`: current schema version integer
- `last_saved_unix`: Unix timestamp written at save time

### `ui`

Written by `PrototypeRootController.WriteUiState()`.

- `main_bar_x`: main bar X position
- `main_bar_width`: main bar width
- `submenu_x`: submenu X position
- `submenu_y`: submenu Y position
- `submenu_visible`: whether submenu window is visible
- `submenu_active_left_tab`: active left tab id
- `submenu_active_right_tab`: active right tab id

Legacy compatibility:

- version `< 2` or missing `submenu_active_left_tab` falls back to legacy `submenu_active_tab`

### `input`

Written by `PrototypeRootController.WriteInputState()` using `InputActivityState.ToDictionary()`.

- `stats.total_key_down`
- `stats.total_mouse_click`
- `stats.total_scroll_steps`
- `stats.total_move_distance`
- `stats.total_joypad_button`
- `stats.total_joypad_axis`
- `stats.ap_accumulator`
- `hook_paused`

Load behavior note:

- Saved `hook_paused=true` does not keep capture paused after load; the controller logs a warning and force-resumes input capture

### `backpack`

Written by `PrototypeRootController.WriteBackpackState()` using `BackpackState.ToDictionary()`.

- `items.<item_id>` = item count

### `resource`

Written by `PrototypeRootController.WriteResourceState()` using `ResourceWalletState.ToDictionary()`.

- `wallet.lingqi`
- `wallet.insight`
- `wallet.pet_affinity`

### `progress`

Written by `PrototypeRootController.WritePlayerProgressState()` using `PlayerProgressState.ToDictionary()`.

- `player.realm_level`
- `player.realm_exp`
- `player.pet_mood`

Note:

- `AutoBreakthrough` is runtime/exported state and is not currently persisted here

### `action`

Written by `PrototypeRootController.WriteActionModeState()` using `PlayerActionState.ToDictionary()`.

- `mode.mode_id`

Known values:

- `dungeon`
- `cultivation`

### `explore`

Written by `PrototypeRootController.WriteExploreRuntimeState()` using `ExploreProgressController.ToRuntimeDictionary()`.

- `runtime.zone_id`
- `runtime.zone_name`
- `runtime.explore_progress`
- `runtime.battle_state`
- `runtime.move_frame_counter`
- `runtime.queue_move_input_pending`
- `runtime.player_hp`
- `runtime.player_max_hp`
- `runtime.enemy_hp`
- `runtime.enemy_max_hp`
- `runtime.enemy_attack_power`
- `runtime.inputs_per_battle_round_runtime`
- `runtime.player_attack_per_round_runtime`
- `runtime.enemy_damage_divider_runtime`
- `runtime.enemy_min_damage_runtime`
- `runtime.battle_round_counter`
- `runtime.pending_battle_input_events`
- `runtime.battle_monster_index`
- `runtime.battle_monster_id`
- `runtime.battle_monster_name`
- `runtime.monster_marker_states[]`
- `runtime.recent_battle_logs[]`

`monster_marker_states[]` item shape:

- `x`
- `y`
- `monster_id`
- `move_pending`
- `move_threshold`

`recent_battle_logs[]` item shape:

- `ts`
- `result`
- `monster_id`
- `monster_name`
- `lingqi`
- `insight`
- `items.<item_id>`

### `level`

Written by `PrototypeRootController.WriteLevelRuntimeState()` using `LevelConfigLoader.ToRuntimeDictionary()`.

- `runtime.active_level_id`
- `runtime.active_wave_index`
- `runtime.unlocked_level_ids[]`
- `runtime.boss_cleared_level_ids[]`
- `runtime.level_clear_count_by_id.<level_id>`
- `runtime.pity_counter_by_key.<counter_key>`
- `runtime.daily_roll_count_by_table.<drop_table_id>`
- `runtime.daily_roll_day_by_table.<drop_table_id>`
- `runtime.hourly_roll_count_by_table.<drop_table_id>`
- `runtime.hourly_roll_hour_by_table.<drop_table_id>`

This section is the main persistence point for level unlock progression and drop-economy counters.

### `settings`

Written by `PrototypeRootController.WriteSystemSettings()` using `BookTabsController.ToSystemSettingsDictionary()`.

Current persisted keys:

- `system.keep_on_top`
- `system.taskbar_icon`
- `system.startup_animation`
- `system.admin_mode`
- `system.handwriting_support`
- `system.vsync`
- `system.max_fps`
- `system.resolution`
- `system.show_control_markers`
- `system.show_validation_panel`
- `system.game_scale`
- `system.ui_scale`
- `system.auto_save_interval_sec`
- `system.cloud_sync`
- `system.milestone_tips`
- `system.global_debug_overlay`

## Migration And Compatibility

- Unified schema version constant lives in `PrototypeRootController` as `SaveSchemaVersion`
- Current migration behavior is light-weight and key-based, not a dedicated migration pipeline
- Legacy fallback only reads:
  - UI state from `user://ui_state.cfg`
  - input stats from `user://game_state.cfg`
- After legacy fallback load, the controller immediately writes `user://save_state.cfg`

## Ownership Map

- `PrototypeRootController`: save orchestration and section routing
- `InputActivityState`: input stats payload
- `BackpackState`: inventory payload
- `ResourceWalletState`: resource wallet payload
- `PlayerProgressState`: realm/mood payload
- `PlayerActionState`: main action mode payload
- `ExploreProgressController`: exploration and battle runtime payload
- `LevelConfigLoader`: unlocked levels and drop-economy runtime payload
- `BookTabsController`: persisted system settings payload

## Change Checklist

When changing save-related behavior:

1. Update both write and read paths in `PrototypeRootController`
2. Update the owning `ToDictionary()` / `FromDictionary()` pair if the payload shape changes
3. Bump `SaveSchemaVersion` if compatibility expectations change materially
4. Add or update migration/fallback logic if old saves must continue to load cleanly
5. Update this file and any README references
6. Verify in-game load/save using `user://save_state.cfg`

## High-Risk Changes

- Renaming section names such as `explore`, `level`, or `settings`
- Renaming nested dictionary keys consumed by `FromDictionary()` or `FromRuntimeDictionary()`
- Changing node layout keys under `ui`
- Changing level runtime counters under `level.runtime`
- Changing recent battle log entry shape under `explore.runtime.recent_battle_logs`

If a key changes, update all read/write references in the same change.
