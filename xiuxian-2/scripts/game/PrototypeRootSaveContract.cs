using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    internal sealed class PrototypeRootSaveSnapshot
    {
        public int SchemaVersion { get; init; } = PrototypeRootSaveContract.SaveSchemaVersion;

        public PrototypeRootUiSnapshot Ui { get; init; } = PrototypeRootUiSnapshot.Default;

        public Dictionary<string, object?> InputStats { get; init; } = CreateDictionary();

        public bool HookPaused { get; init; }

        public Dictionary<string, object?> BackpackItems { get; init; } = CreateDictionary();

        public Dictionary<string, object?> ResourceWallet { get; init; } = CreateDictionary();

        public Dictionary<string, object?> PlayerProgress { get; init; } = CreateDictionary();

        public Dictionary<string, object?> ActionMode { get; init; } = CreateDictionary();

        public Dictionary<string, object?> ExploreRuntime { get; init; } = CreateDictionary();

        public Dictionary<string, object?> LevelRuntime { get; init; } = CreateDictionary();

        public Dictionary<string, object?> SystemSettings { get; init; } = CreateDictionary();

        private static Dictionary<string, object?> CreateDictionary()
        {
            return new(StringComparer.Ordinal);
        }
    }

    internal readonly record struct PrototypeRootUiSnapshot(
        float MainBarX,
        float MainBarWidth,
        bool SubmenuVisible,
        string ActiveLeftTab,
        string ActiveRightTab)
    {
        public static PrototypeRootUiSnapshot Default => new(0.0f, 0.0f, false, "CultivationTab", "OnlineTab");
    }

    internal static class PrototypeRootSaveContract
    {
        public const int SaveSchemaVersion = 5;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private static readonly string[] SectionOrder =
        {
            "meta",
            "ui",
            "input",
            "backpack",
            "resource",
            "progress",
            "action",
            "explore",
            "level",
            "settings"
        };

        private static readonly Dictionary<string, string[]> KeyOrder = new(StringComparer.Ordinal)
        {
            ["meta"] = ["version", "last_saved_unix"],
            ["ui"] = ["main_bar_x", "main_bar_width", "submenu_visible", "submenu_active_left_tab", "submenu_active_right_tab"],
            ["input"] = ["stats", "hook_paused"],
            ["backpack"] = ["items"],
            ["resource"] = ["wallet"],
            ["progress"] = ["player"],
            ["action"] = ["mode"],
            ["explore"] = ["runtime"],
            ["level"] = ["runtime"],
            ["settings"] = ["system"]
        };

        public static void Write(ConfigFile config, PrototypeRootSaveSnapshot snapshot, double lastSavedUnixTime)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(snapshot);

            foreach ((string section, Dictionary<string, object?> values) in BuildDocument(snapshot, lastSavedUnixTime))
            {
                foreach ((string key, object? value) in values)
                {
                    config.SetValue(section, key, ToVariant(value));
                }
            }
        }

        public static PrototypeRootSaveSnapshot Read(ConfigFile config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return ReadDocument(ReadConfigDocument(config));
        }

        public static string Serialize(PrototypeRootSaveSnapshot snapshot, double lastSavedUnixTime)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            return SerializeDocument(BuildDocument(snapshot, lastSavedUnixTime));
        }

        public static PrototypeRootSaveSnapshot Deserialize(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            return ReadDocument(ParseDocument(text));
        }

        private static Dictionary<string, Dictionary<string, object?>> BuildDocument(PrototypeRootSaveSnapshot snapshot, double lastSavedUnixTime)
        {
            return new Dictionary<string, Dictionary<string, object?>>(StringComparer.Ordinal)
            {
                ["meta"] = new(StringComparer.Ordinal)
                {
                    ["version"] = SaveSchemaVersion,
                    ["last_saved_unix"] = (long)lastSavedUnixTime
                },
                ["ui"] = new(StringComparer.Ordinal)
                {
                    ["main_bar_x"] = snapshot.Ui.MainBarX,
                    ["main_bar_width"] = snapshot.Ui.MainBarWidth,
                    ["submenu_visible"] = snapshot.Ui.SubmenuVisible,
                    ["submenu_active_left_tab"] = snapshot.Ui.ActiveLeftTab,
                    ["submenu_active_right_tab"] = snapshot.Ui.ActiveRightTab
                },
                ["input"] = new(StringComparer.Ordinal)
                {
                    ["stats"] = StateSerializationContracts.NormalizeInputActivityRaw(snapshot.InputStats),
                    ["hook_paused"] = snapshot.HookPaused
                },
                ["backpack"] = new(StringComparer.Ordinal)
                {
                    ["items"] = StateSerializationContracts.NormalizeBackpackRaw(snapshot.BackpackItems)
                },
                ["resource"] = new(StringComparer.Ordinal)
                {
                    ["wallet"] = StateSerializationContracts.NormalizeResourceWalletRaw(snapshot.ResourceWallet)
                },
                ["progress"] = new(StringComparer.Ordinal)
                {
                    ["player"] = StateSerializationContracts.NormalizePlayerProgressRaw(snapshot.PlayerProgress)
                },
                ["action"] = new(StringComparer.Ordinal)
                {
                    ["mode"] = StateSerializationContracts.NormalizePlayerActionRaw(snapshot.ActionMode)
                },
                ["explore"] = new(StringComparer.Ordinal)
                {
                    ["runtime"] = NormalizeExploreRuntimeRaw(snapshot.ExploreRuntime)
                },
                ["level"] = new(StringComparer.Ordinal)
                {
                    ["runtime"] = NormalizeLevelRuntimeRaw(snapshot.LevelRuntime)
                },
                ["settings"] = new(StringComparer.Ordinal)
                {
                    ["system"] = NormalizeSystemSettingsRaw(snapshot.SystemSettings)
                }
            };
        }

        private static PrototypeRootSaveSnapshot ReadDocument(Dictionary<string, Dictionary<string, object?>> document)
        {
            int version = ReadInt(document, "meta", "version", 1);
            return new PrototypeRootSaveSnapshot
            {
                SchemaVersion = version,
                Ui = ReadUi(document, version),
                InputStats = StateSerializationContracts.NormalizeInputActivityRaw(ReadDictionary(document, "input", "stats")),
                HookPaused = ReadBool(document, "input", "hook_paused", false),
                BackpackItems = StateSerializationContracts.NormalizeBackpackRaw(ReadDictionary(document, "backpack", "items")),
                ResourceWallet = StateSerializationContracts.NormalizeResourceWalletRaw(ReadDictionary(document, "resource", "wallet")),
                PlayerProgress = StateSerializationContracts.NormalizePlayerProgressRaw(ReadDictionary(document, "progress", "player")),
                ActionMode = StateSerializationContracts.NormalizePlayerActionRaw(ReadDictionary(document, "action", "mode")),
                ExploreRuntime = ReadOptionalNormalizedDictionary(document, "explore", "runtime", NormalizeExploreRuntimeRaw),
                LevelRuntime = ReadOptionalNormalizedDictionary(document, "level", "runtime", NormalizeLevelRuntimeRaw),
                SystemSettings = ReadOptionalNormalizedDictionary(document, "settings", "system", NormalizeSystemSettingsRaw)
            };
        }

        private static PrototypeRootUiSnapshot ReadUi(Dictionary<string, Dictionary<string, object?>> document, int version)
        {
            Dictionary<string, object?> section = GetSection(document, "ui");
            float mainBarX = (float)RawRead.Double(section, "main_bar_x", 0.0);
            float mainBarWidth = (float)RawRead.Double(section, "main_bar_width", 0.0);
            bool submenuVisible = ReadBool(document, "ui", "submenu_visible", false);
            string activeLeftTab = RawRead.String(section, "submenu_active_left_tab", "CultivationTab");
            string activeRightTab = RawRead.String(section, "submenu_active_right_tab", "OnlineTab");

            if (version < 2 || !section.ContainsKey("submenu_active_left_tab"))
            {
                activeLeftTab = RawRead.String(section, "submenu_active_tab", "CultivationTab");
            }

            return new PrototypeRootUiSnapshot(mainBarX, mainBarWidth, submenuVisible, activeLeftTab, activeRightTab);
        }

        private static Dictionary<string, object?> ReadOptionalNormalizedDictionary(
            Dictionary<string, Dictionary<string, object?>> document,
            string section,
            string key,
            Func<IReadOnlyDictionary<string, object?>, Dictionary<string, object?>> normalize)
        {
            if (!document.TryGetValue(section, out Dictionary<string, object?>? values)
                || !values.TryGetValue(key, out object? rawValue)
                || rawValue is not IReadOnlyDictionary<string, object?> rawDictionary)
            {
                return new Dictionary<string, object?>(StringComparer.Ordinal);
            }

            return normalize(rawDictionary);
        }

        private static Dictionary<string, Dictionary<string, object?>> ReadConfigDocument(ConfigFile config)
        {
            var document = new Dictionary<string, Dictionary<string, object?>>(StringComparer.Ordinal);
            foreach (string section in SectionOrder)
            {
                if (!config.HasSection(section))
                {
                    continue;
                }

                var values = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (string key in config.GetSectionKeys(section))
                {
                    values[key] = ToRawValue(config.GetValue(section, key));
                }

                document[section] = values;
            }

            return document;
        }

        private static string SerializeDocument(Dictionary<string, Dictionary<string, object?>> document)
        {
            var builder = new StringBuilder();
            foreach (string section in SectionOrder)
            {
                if (!document.TryGetValue(section, out Dictionary<string, object?>? values))
                {
                    continue;
                }

                builder.Append('[').Append(section).AppendLine("]");
                foreach (string key in GetOrderedKeys(section, values))
                {
                    builder.Append(key).Append('=').AppendLine(SerializeValue(values[key]));
                }

                builder.AppendLine();
            }

            return builder.ToString().TrimEnd() + System.Environment.NewLine;
        }

        private static Dictionary<string, Dictionary<string, object?>> ParseDocument(string text)
        {
            var document = new Dictionary<string, Dictionary<string, object?>>(StringComparer.Ordinal);
            string? currentSection = null;
            string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index].Trim();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
                {
                    currentSection = line[1..^1];
                    document[currentSection] = new Dictionary<string, object?>(StringComparer.Ordinal);
                    continue;
                }

                if (currentSection == null)
                {
                    continue;
                }

                int equalsIndex = line.IndexOf('=');
                if (equalsIndex <= 0)
                {
                    continue;
                }

                string key = line[..equalsIndex];
                string rawValue = line[(equalsIndex + 1)..];
                if (NeedsMultilineParse(rawValue))
                {
                    var valueBuilder = new StringBuilder(rawValue);
                    int depth = GetBracketDepth(rawValue);
                    while (depth > 0 && index + 1 < lines.Length)
                    {
                        index++;
                        string continuation = lines[index];
                        valueBuilder.Append('\n').Append(continuation);
                        depth += GetBracketDepth(continuation);
                    }

                    rawValue = valueBuilder.ToString();
                }

                document[currentSection][key] = ParseValue(rawValue.Trim());
            }

            return document;
        }

        private static bool NeedsMultilineParse(string rawValue)
        {
            rawValue = rawValue.TrimStart();
            if (rawValue.Length == 0)
            {
                return false;
            }

            return (rawValue[0] == '{' || rawValue[0] == '[') && GetBracketDepth(rawValue) > 0;
        }

        private static int GetBracketDepth(string text)
        {
            int depth = 0;
            bool inString = false;
            bool escaped = false;

            foreach (char ch in text)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                {
                    continue;
                }

                if (ch == '{' || ch == '[')
                {
                    depth++;
                }
                else if (ch == '}' || ch == ']')
                {
                    depth--;
                }
            }

            return depth;
        }

        private static object? ParseValue(string rawValue)
        {
            if (rawValue.StartsWith("{", StringComparison.Ordinal) || rawValue.StartsWith("[", StringComparison.Ordinal) || rawValue.StartsWith("\"", StringComparison.Ordinal))
            {
                JsonNode? node = JsonNode.Parse(rawValue);
                return ConvertJsonNode(node);
            }

            if (bool.TryParse(rawValue, out bool boolValue))
            {
                return boolValue;
            }

            if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
            {
                return longValue >= int.MinValue && longValue <= int.MaxValue ? (object)(int)longValue : longValue;
            }

            if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            {
                return doubleValue;
            }

            return rawValue;
        }

        private static object? ConvertJsonNode(JsonNode? node)
        {
            return node switch
            {
                null => null,
                JsonObject obj => obj.ToDictionary(static pair => pair.Key, static pair => ConvertJsonNode(pair.Value), StringComparer.Ordinal),
                JsonArray array => array.Select(ConvertJsonNode).ToList(),
                JsonValue value => ConvertJsonValue(value),
                _ => null
            };
        }

        private static object? ConvertJsonValue(JsonValue value)
        {
            if (value.TryGetValue(out string? stringValue))
            {
                return stringValue;
            }

            if (value.TryGetValue(out bool boolValue))
            {
                return boolValue;
            }

            if (value.TryGetValue(out int intValue))
            {
                return intValue;
            }

            if (value.TryGetValue(out long longValue))
            {
                return longValue;
            }

            if (value.TryGetValue(out double doubleValue))
            {
                return doubleValue;
            }

            return value.ToJsonString();
        }

        private static object? ToRawValue(Variant value)
        {
            return value.VariantType switch
            {
                Variant.Type.Bool => value.AsBool(),
                Variant.Type.Int => value.AsInt64(),
                Variant.Type.Float => value.AsDouble(),
                Variant.Type.String => value.AsString().ToString(),
                Variant.Type.Dictionary => ToRawDictionary((Godot.Collections.Dictionary<string, Variant>)value),
                Variant.Type.Array => ((Godot.Collections.Array<Variant>)value).Select(ToRawValue).ToList(),
                _ => value.Obj
            };
        }

        private static Dictionary<string, object?> ToRawDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (string key in data.Keys)
            {
                normalized[key] = ToRawValue(data[key]);
            }

            return normalized;
        }

        private static Variant ToVariant(object? value)
        {
            return value switch
            {
                null => Variant.CreateFrom((string?)null),
                bool boolValue => Variant.CreateFrom(boolValue),
                int intValue => Variant.CreateFrom(intValue),
                long longValue => Variant.CreateFrom(longValue),
                double doubleValue => Variant.CreateFrom(doubleValue),
                float floatValue => Variant.CreateFrom(floatValue),
                string stringValue => Variant.CreateFrom(stringValue),
                IReadOnlyDictionary<string, object?> dictionaryValue => Variant.CreateFrom(ToVariantDictionary(dictionaryValue)),
                IEnumerable<object?> listValue => Variant.CreateFrom(ToVariantArray(listValue)),
                _ => Variant.CreateFrom(value.ToString())
            };
        }

        private static Godot.Collections.Dictionary<string, Variant> ToVariantDictionary(IReadOnlyDictionary<string, object?> data)
        {
            var normalized = new Godot.Collections.Dictionary<string, Variant>();
            foreach ((string key, object? value) in data)
            {
                normalized[key] = ToVariant(value);
            }

            return normalized;
        }

        private static Godot.Collections.Array<Variant> ToVariantArray(IEnumerable<object?> values)
        {
            var array = new Godot.Collections.Array<Variant>();
            foreach (object? value in values)
            {
                array.Add(ToVariant(value));
            }

            return array;
        }

        private static IEnumerable<string> GetOrderedKeys(string section, Dictionary<string, object?> values)
        {
            if (KeyOrder.TryGetValue(section, out string[]? ordered))
            {
                foreach (string key in ordered)
                {
                    if (values.ContainsKey(key))
                    {
                        yield return key;
                    }
                }

                foreach (string extraKey in values.Keys.Except(ordered, StringComparer.Ordinal).OrderBy(static value => value, StringComparer.Ordinal))
                {
                    yield return extraKey;
                }

                yield break;
            }

            foreach (string key in values.Keys.OrderBy(static value => value, StringComparer.Ordinal))
            {
                yield return key;
            }
        }

        private static string SerializeValue(object? value)
        {
            return value switch
            {
                null => "null",
                bool boolValue => boolValue ? "true" : "false",
                string stringValue => JsonSerializer.Serialize(stringValue, JsonOptions),
                int intValue => intValue.ToString(CultureInfo.InvariantCulture),
                long longValue => longValue.ToString(CultureInfo.InvariantCulture),
                float floatValue => floatValue.ToString("0.0#######", CultureInfo.InvariantCulture),
                double doubleValue => doubleValue.ToString("0.0###############", CultureInfo.InvariantCulture),
                IReadOnlyDictionary<string, object?> dictionaryValue => NormalizeSerializedJson(JsonSerializer.Serialize(dictionaryValue, JsonOptions)),
                IEnumerable<object?> listValue => NormalizeSerializedJson(JsonSerializer.Serialize(listValue, JsonOptions)),
                _ => JsonSerializer.Serialize(value.ToString(), JsonOptions)
            };
        }

        private static string NormalizeSerializedJson(string json)
        {
            string[] lines = json.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            for (int index = 0; index < lines.Length; index++)
            {
                lines[index] = lines[index].TrimStart();
            }

            return string.Join("\n", lines);
        }

        private static Dictionary<string, object?> ReadDictionary(
            Dictionary<string, Dictionary<string, object?>> document,
            string section,
            string key)
        {
            Dictionary<string, object?> sectionValues = GetSection(document, section);
            return sectionValues.TryGetValue(key, out object? value) && value is IReadOnlyDictionary<string, object?> dictionary
                ? new Dictionary<string, object?>(dictionary, StringComparer.Ordinal)
                : new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        private static Dictionary<string, object?> GetSection(Dictionary<string, Dictionary<string, object?>> document, string section)
        {
            return document.TryGetValue(section, out Dictionary<string, object?>? values)
                ? values
                : new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        private static int ReadInt(Dictionary<string, Dictionary<string, object?>> document, string section, string key, int fallback)
        {
            return RawRead.Int(GetSection(document, section), key, fallback);
        }

        private static bool ReadBool(Dictionary<string, Dictionary<string, object?>> document, string section, string key, bool fallback)
        {
            Dictionary<string, object?> values = GetSection(document, section);
            return values.TryGetValue(key, out object? value) && value is bool boolValue ? boolValue : fallback;
        }

        private static Dictionary<string, object?> NormalizeLevelRuntimeRaw(IReadOnlyDictionary<string, object?> data)
        {
            var state = LevelRuntimeSerializationContracts.NormalizeRaw(data, string.Empty, Array.Empty<string>(), 0);
            return LevelRuntimeSerializationContracts.ToRawDictionary(state);
        }

        private static Dictionary<string, object?> NormalizeExploreRuntimeRaw(IReadOnlyDictionary<string, object?> data)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["zone_id"] = RawRead.String(data, "zone_id", string.Empty),
                ["zone_name"] = RawRead.String(data, "zone_name", string.Empty),
                ["explore_progress"] = Math.Max(0.0, RawRead.Double(data, "explore_progress")),
                ["battle_state"] = RawRead.String(data, "battle_state", "exploring") == "in_battle" ? "in_battle" : "exploring",
                ["move_frame_counter"] = Math.Max(0, RawRead.Int(data, "move_frame_counter")),
                ["queue_move_input_pending"] = Math.Max(0, RawRead.Int(data, "queue_move_input_pending")),
                ["player_hp"] = Math.Max(0, RawRead.Int(data, "player_hp")),
                ["player_max_hp"] = Math.Max(1, RawRead.Int(data, "player_max_hp", 1)),
                ["enemy_hp"] = Math.Max(0, RawRead.Int(data, "enemy_hp")),
                ["enemy_max_hp"] = Math.Max(1, RawRead.Int(data, "enemy_max_hp", 1)),
                ["enemy_attack_power"] = Math.Max(1, RawRead.Int(data, "enemy_attack_power", 1)),
                ["inputs_per_battle_round_runtime"] = Math.Max(1, RawRead.Int(data, "inputs_per_battle_round_runtime", 1)),
                ["player_attack_per_round_runtime"] = Math.Max(1, RawRead.Int(data, "player_attack_per_round_runtime", 1)),
                ["enemy_damage_divider_runtime"] = Math.Max(1, RawRead.Int(data, "enemy_damage_divider_runtime", 1)),
                ["enemy_min_damage_runtime"] = Math.Max(1, RawRead.Int(data, "enemy_min_damage_runtime", 1)),
                ["battle_round_counter"] = Math.Max(0, RawRead.Int(data, "battle_round_counter")),
                ["pending_battle_input_events"] = Math.Max(0, RawRead.Int(data, "pending_battle_input_events")),
                ["battle_monster_index"] = RawRead.Int(data, "battle_monster_index", -1),
                ["battle_monster_id"] = RawRead.String(data, "battle_monster_id", string.Empty),
                ["battle_monster_name"] = RawRead.String(data, "battle_monster_name", string.Empty),
                ["monster_marker_states"] = NormalizeMarkerStates(data)
            };
        }

        private static List<object?> NormalizeMarkerStates(IReadOnlyDictionary<string, object?> data)
        {
            if (!data.TryGetValue("monster_marker_states", out object? value) || value is not IEnumerable<object?> items)
            {
                return new List<object?>();
            }

            var normalized = new List<object?>();
            foreach (object? item in items)
            {
                if (item is not IReadOnlyDictionary<string, object?> marker)
                {
                    continue;
                }

                normalized.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["x"] = RawRead.Double(marker, "x"),
                    ["y"] = RawRead.Double(marker, "y"),
                    ["monster_id"] = RawRead.String(marker, "monster_id", string.Empty),
                    ["move_pending"] = Math.Max(0, RawRead.Int(marker, "move_pending")),
                    ["move_threshold"] = Math.Max(1, RawRead.Int(marker, "move_threshold", 1))
                });
            }

            return normalized;
        }

        private static Dictionary<string, object?> NormalizeSystemSettingsRaw(IReadOnlyDictionary<string, object?> data)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
            CopyIfPresent(normalized, data, "language", static value => RawRead.String(value, "zh-CN"));
            CopyIfPresent(normalized, data, "keep_on_top", static value => value is bool boolValue && boolValue);
            CopyIfPresent(normalized, data, "taskbar_icon", static value => value is bool boolValue && boolValue);
            CopyIfPresent(normalized, data, "startup_animation", static value => value is bool boolValue && boolValue);
            CopyIfPresent(normalized, data, "admin_mode", static value => value is bool boolValue && boolValue);
            CopyIfPresent(normalized, data, "handwriting_support", static value => value is bool boolValue && boolValue);
            CopyIfPresent(normalized, data, "vsync", static value => value is bool boolValue && boolValue);
            CopyIfPresent(normalized, data, "max_fps", static value => RawRead.Int(value, 60));
            CopyIfPresent(normalized, data, "resolution", static value => RawRead.String(value, "1600x900"));
            CopyIfPresent(normalized, data, "show_control_markers", static value => value is bool boolValue && boolValue);
            CopyIfPresent(normalized, data, "show_validation_panel", static value => value is not bool boolValue || boolValue);
            CopyIfPresent(normalized, data, "game_scale", static value => RawRead.Double(value, 1.33));
            CopyIfPresent(normalized, data, "ui_scale", static value => RawRead.Double(value, 1.0));
            CopyIfPresent(normalized, data, "auto_save_interval_sec", static value => Math.Max(1, RawRead.Int(value, 10)));
            CopyIfPresent(normalized, data, "cloud_sync", static value => value is bool boolValue && boolValue);
            CopyIfPresent(normalized, data, "milestone_tips", static value => value is not bool boolValue || boolValue);
            CopyIfPresent(normalized, data, "global_debug_overlay", static value => value is bool boolValue && boolValue);
            return normalized;
        }

        private static void CopyIfPresent(
            Dictionary<string, object?> destination,
            IReadOnlyDictionary<string, object?> source,
            string key,
            Func<object?, object?> normalize)
        {
            if (source.TryGetValue(key, out object? value))
            {
                destination[key] = normalize(value);
            }
        }
    }
}
