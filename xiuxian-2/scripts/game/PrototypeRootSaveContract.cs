using Godot;
using System;
using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    public sealed class PrototypeRootSaveSnapshot
    {
        public int SchemaVersion { get; set; } = PrototypeRootSaveContract.SaveSchemaVersion;

        public PrototypeRootUiSnapshot Ui { get; set; } = new(0.0f, 0.0f, false, "CultivationTab", "OnlineTab");

        public Dictionary<string, object?> InputStats { get; set; } = StateSerializationContracts.NormalizeInputActivityRaw(new Dictionary<string, object?>());

        public bool HookPaused { get; set; }

        public Dictionary<string, object?> BackpackItems { get; set; } = StateSerializationContracts.NormalizeBackpackRaw(new Dictionary<string, object?>());

        public Dictionary<string, object?> ResourceWallet { get; set; } = StateSerializationContracts.NormalizeResourceWalletRaw(new Dictionary<string, object?>());

        public Dictionary<string, object?> PlayerProgress { get; set; } = StateSerializationContracts.NormalizePlayerProgressRaw(new Dictionary<string, object?>());

        public Dictionary<string, object?> ActionMode { get; set; } = StateSerializationContracts.NormalizePlayerActionRaw(new Dictionary<string, object?>());

        public Dictionary<string, object?> ExploreRuntime { get; set; } = PrototypeRootSaveContract.CreateDefaultExploreRuntime();

        public Dictionary<string, object?> LevelRuntime { get; set; } = PrototypeRootSaveContract.CreateDefaultLevelRuntime();

        public Dictionary<string, object?> SystemSettings { get; set; } = PrototypeRootSaveContract.CreateDefaultSystemSettings();
    }

    public sealed record PrototypeRootUiSnapshot(float MainBarX, float MainBarWidth, bool SubmenuVisible, string ActiveLeftTab, string ActiveRightTab);

    public static class PrototypeRootSaveContract
    {
        public const int SaveSchemaVersion = 5;

        public static void Write(ConfigFile config, PrototypeRootSaveSnapshot snapshot, long lastSavedUnix)
        {
            config.SetValue("meta", "version", SaveSchemaVersion);
            config.SetValue("meta", "last_saved_unix", lastSavedUnix);

            config.SetValue("ui", "main_bar_x", snapshot.Ui.MainBarX);
            config.SetValue("ui", "main_bar_width", snapshot.Ui.MainBarWidth);
            config.SetValue("ui", "submenu_visible", snapshot.Ui.SubmenuVisible);
            config.SetValue("ui", "submenu_active_left_tab", snapshot.Ui.ActiveLeftTab);
            config.SetValue("ui", "submenu_active_right_tab", snapshot.Ui.ActiveRightTab);

            config.SetValue("input", "stats", RawVariantBridge.ToVariantDictionary(snapshot.InputStats));
            config.SetValue("input", "hook_paused", snapshot.HookPaused);
            config.SetValue("backpack", "items", RawVariantBridge.ToVariantDictionary(snapshot.BackpackItems));
            config.SetValue("resource", "wallet", RawVariantBridge.ToVariantDictionary(snapshot.ResourceWallet));
            config.SetValue("progress", "player", RawVariantBridge.ToVariantDictionary(snapshot.PlayerProgress));
            config.SetValue("action", "mode", RawVariantBridge.ToVariantDictionary(snapshot.ActionMode));
            config.SetValue("explore", "runtime", RawVariantBridge.ToVariantDictionary(snapshot.ExploreRuntime));
            config.SetValue("level", "runtime", RawVariantBridge.ToVariantDictionary(snapshot.LevelRuntime));
            config.SetValue("settings", "system", RawVariantBridge.ToVariantDictionary(snapshot.SystemSettings));
        }

        public static PrototypeRootSaveSnapshot Read(ConfigFile config)
        {
            int version = config.GetValue("meta", "version", 1).AsInt32();
            return new PrototypeRootSaveSnapshot
            {
                SchemaVersion = version,
                Ui = ReadUi(config, version),
                InputStats = ReadNormalizedDictionary(config, "input", "stats", StateSerializationContracts.NormalizeInputActivityRaw),
                HookPaused = config.GetValue("input", "hook_paused", false).AsBool(),
                BackpackItems = ReadNormalizedDictionary(config, "backpack", "items", StateSerializationContracts.NormalizeBackpackRaw),
                ResourceWallet = ReadNormalizedDictionary(config, "resource", "wallet", StateSerializationContracts.NormalizeResourceWalletRaw),
                PlayerProgress = ReadNormalizedDictionary(config, "progress", "player", StateSerializationContracts.NormalizePlayerProgressRaw),
                ActionMode = ReadNormalizedDictionary(config, "action", "mode", StateSerializationContracts.NormalizePlayerActionRaw),
                ExploreRuntime = ReadMergedDictionary(config, "explore", "runtime", CreateDefaultExploreRuntime),
                LevelRuntime = ReadMergedDictionary(config, "level", "runtime", CreateDefaultLevelRuntime),
                SystemSettings = ReadMergedDictionary(config, "settings", "system", CreateDefaultSystemSettings)
            };
        }

        public static Dictionary<string, object?> CreateDefaultExploreRuntime()
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["zone_id"] = string.Empty,
                ["zone_name"] = string.Empty,
                ["explore_progress"] = 0.0,
                ["battle_state"] = "exploring",
                ["move_frame_counter"] = 0,
                ["queue_move_input_pending"] = 0,
                ["player_hp"] = 0,
                ["player_max_hp"] = 1,
                ["enemy_hp"] = 0,
                ["enemy_max_hp"] = 1,
                ["enemy_attack_power"] = 1,
                ["inputs_per_battle_round_runtime"] = 1,
                ["player_attack_per_round_runtime"] = 1,
                ["enemy_damage_divider_runtime"] = 1,
                ["enemy_min_damage_runtime"] = 1,
                ["battle_round_counter"] = 0,
                ["pending_battle_input_events"] = 0,
                ["battle_monster_index"] = -1,
                ["battle_monster_id"] = string.Empty,
                ["battle_monster_name"] = string.Empty,
                ["monster_marker_states"] = new List<object?>()
            };
        }

        public static Dictionary<string, object?> CreateDefaultLevelRuntime()
        {
            return LevelRuntimeSerializationContracts.ToRawDictionary(new LevelRuntimeSerializationState());
        }

        public static Dictionary<string, object?> CreateDefaultSystemSettings()
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["language"] = "zh-CN",
                ["keep_on_top"] = false,
                ["taskbar_icon"] = true,
                ["startup_animation"] = true,
                ["admin_mode"] = false,
                ["handwriting_support"] = false,
                ["vsync"] = true,
                ["max_fps"] = 60,
                ["resolution"] = "1600x900",
                ["show_control_markers"] = true,
                ["show_validation_panel"] = true,
                ["game_scale"] = 1.33,
                ["ui_scale"] = 1.0,
                ["auto_save_interval_sec"] = 10,
                ["cloud_sync"] = false,
                ["milestone_tips"] = true,
                ["global_debug_overlay"] = false
            };
        }

        private static PrototypeRootUiSnapshot ReadUi(ConfigFile config, int version)
        {
            float mainBarX = config.GetValue("ui", "main_bar_x", 0.0f).AsSingle();
            float mainBarWidth = config.GetValue("ui", "main_bar_width", 0.0f).AsSingle();
            string activeLeftTab = config.GetValue("ui", "submenu_active_left_tab", "CultivationTab").AsString();
            string activeRightTab = config.GetValue("ui", "submenu_active_right_tab", "OnlineTab").AsString();

            if (version < 2 || !config.HasSectionKey("ui", "submenu_active_left_tab"))
            {
                activeLeftTab = config.GetValue("ui", "submenu_active_tab", "CultivationTab").AsString();
            }

            return new PrototypeRootUiSnapshot(
                mainBarX,
                mainBarWidth,
                config.GetValue("ui", "submenu_visible", false).AsBool(),
                activeLeftTab,
                activeRightTab);
        }

        private static Dictionary<string, object?> ReadNormalizedDictionary(
            ConfigFile config,
            string section,
            string key,
            Func<IReadOnlyDictionary<string, object?>, Dictionary<string, object?>> normalize)
        {
            return normalize(ReadRawDictionary(config, section, key));
        }

        private static Dictionary<string, object?> ReadMergedDictionary(
            ConfigFile config,
            string section,
            string key,
            Func<Dictionary<string, object?>> createDefaults)
        {
            var merged = createDefaults();
            foreach ((string itemKey, object? itemValue) in ReadRawDictionary(config, section, key))
            {
                merged[itemKey] = itemValue;
            }

            return merged;
        }

        private static Dictionary<string, object?> ReadRawDictionary(ConfigFile config, string section, string key)
        {
            Variant data = config.GetValue(section, key, new Godot.Collections.Dictionary<string, Variant>());
            return data.VariantType == Variant.Type.Dictionary
                ? RawVariantBridge.ToRawDictionary((Godot.Collections.Dictionary<string, Variant>)data)
                : new Dictionary<string, object?>(StringComparer.Ordinal);
        }
    }
}
