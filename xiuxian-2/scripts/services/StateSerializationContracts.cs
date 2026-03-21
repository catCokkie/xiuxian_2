using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Xiuxian.Scripts.Services
{
    internal static class StateSerializationContracts
    {
        public static Godot.Collections.Dictionary<string, Variant> NormalizeBackpack(Godot.Collections.Dictionary<string, Variant> data)
        {
            return RawVariantBridge.ToVariantDictionary(NormalizeBackpackRaw(RawVariantBridge.ToRawDictionary(data)));
        }

        public static Godot.Collections.Dictionary<string, Variant> NormalizeResourceWallet(Godot.Collections.Dictionary<string, Variant> data)
        {
            return RawVariantBridge.ToVariantDictionary(NormalizeResourceWalletRaw(RawVariantBridge.ToRawDictionary(data)));
        }

        public static Godot.Collections.Dictionary<string, Variant> NormalizePlayerProgress(Godot.Collections.Dictionary<string, Variant> data)
        {
            return RawVariantBridge.ToVariantDictionary(NormalizePlayerProgressRaw(RawVariantBridge.ToRawDictionary(data)));
        }

        public static Godot.Collections.Dictionary<string, Variant> NormalizeInputActivity(Godot.Collections.Dictionary<string, Variant> data)
        {
            return RawVariantBridge.ToVariantDictionary(NormalizeInputActivityRaw(RawVariantBridge.ToRawDictionary(data)));
        }

        public static Godot.Collections.Dictionary<string, Variant> NormalizePlayerAction(Godot.Collections.Dictionary<string, Variant> data)
        {
            return RawVariantBridge.ToVariantDictionary(NormalizePlayerActionRaw(RawVariantBridge.ToRawDictionary(data)));
        }

        internal static Dictionary<string, object?> NormalizeBackpackRaw(IReadOnlyDictionary<string, object?> data)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach ((string key, object? value) in data)
            {
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                normalized[key] = Math.Max(0, RawRead.Int(value));
            }

            return normalized;
        }

        internal static Dictionary<string, object?> NormalizeResourceWalletRaw(IReadOnlyDictionary<string, object?> data)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["lingqi"] = Math.Max(0.0, RawRead.Double(data, "lingqi")),
                ["insight"] = Math.Max(0.0, RawRead.Double(data, "insight")),
                ["pet_affinity"] = Math.Max(0.0, RawRead.Double(data, "pet_affinity"))
            };
        }

        internal static Dictionary<string, object?> NormalizePlayerProgressRaw(IReadOnlyDictionary<string, object?> data)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["realm_level"] = Math.Max(1, RawRead.Int(data, "realm_level", 1)),
                ["realm_exp"] = Math.Max(0.0, RawRead.Double(data, "realm_exp")),
                ["pet_mood"] = Math.Clamp(RawRead.Int(data, "pet_mood", 60), 0, 100)
            };
        }

        internal static Dictionary<string, object?> NormalizeInputActivityRaw(IReadOnlyDictionary<string, object?> data)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["total_key_down"] = Math.Max(0L, RawRead.Long(data, "total_key_down")),
                ["total_mouse_click"] = Math.Max(0L, RawRead.Long(data, "total_mouse_click")),
                ["total_scroll_steps"] = Math.Max(0L, RawRead.Long(data, "total_scroll_steps")),
                ["total_move_distance"] = Math.Max(0.0, RawRead.Double(data, "total_move_distance")),
                ["total_joypad_button"] = Math.Max(0L, RawRead.Long(data, "total_joypad_button")),
                ["total_joypad_axis"] = Math.Max(0L, RawRead.Long(data, "total_joypad_axis")),
                ["ap_accumulator"] = Math.Max(0.0, RawRead.Double(data, "ap_accumulator"))
            };
        }

        internal static Dictionary<string, object?> NormalizePlayerActionRaw(IReadOnlyDictionary<string, object?> data)
        {
            string modeId = RawRead.String(data, "mode_id", PlayerActionState.ModeDungeon);
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["mode_id"] = modeId == PlayerActionState.ModeCultivation ? PlayerActionState.ModeCultivation : PlayerActionState.ModeDungeon
            };
        }
    }

    internal sealed class LevelRuntimeSerializationState
    {
        public string ActiveLevelId { get; set; } = string.Empty;

        public int ActiveWaveIndex { get; set; }

        public HashSet<string> UnlockedLevelIds { get; } = new(StringComparer.Ordinal);

        public HashSet<string> BossClearedLevelIds { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, int> LevelClearCountById { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, int> PityCounterByKey { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, int> DailyRollCountByTable { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, long> DailyRollDayByTable { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, int> HourlyRollCountByTable { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, long> HourlyRollHourByTable { get; } = new(StringComparer.Ordinal);
    }

    internal static class LevelRuntimeSerializationContracts
    {
        public static Godot.Collections.Dictionary<string, Variant> ToDictionary(LevelRuntimeSerializationState state)
        {
            return RawVariantBridge.ToVariantDictionary(ToRawDictionary(state));
        }

        internal static Dictionary<string, object?> ToRawDictionary(LevelRuntimeSerializationState state)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["active_level_id"] = state.ActiveLevelId,
                ["active_wave_index"] = state.ActiveWaveIndex,
                ["unlocked_level_ids"] = state.UnlockedLevelIds.OrderBy(static value => value, StringComparer.Ordinal).Cast<object?>().ToList(),
                ["boss_cleared_level_ids"] = state.BossClearedLevelIds.OrderBy(static value => value, StringComparer.Ordinal).Cast<object?>().ToList(),
                ["level_clear_count_by_id"] = ToRawDictionary(state.LevelClearCountById),
                ["pity_counter_by_key"] = ToRawDictionary(state.PityCounterByKey),
                ["daily_roll_count_by_table"] = ToRawDictionary(state.DailyRollCountByTable),
                ["daily_roll_day_by_table"] = ToRawDictionary(state.DailyRollDayByTable),
                ["hourly_roll_count_by_table"] = ToRawDictionary(state.HourlyRollCountByTable),
                ["hourly_roll_hour_by_table"] = ToRawDictionary(state.HourlyRollHourByTable)
            };
        }

        public static LevelRuntimeSerializationState Normalize(
            Godot.Collections.Dictionary<string, Variant> data,
            string fallbackActiveLevelId,
            IReadOnlyList<string> knownLevelIds,
            int activeWaveCount)
        {
            return NormalizeRaw(RawVariantBridge.ToRawDictionary(data), fallbackActiveLevelId, knownLevelIds, activeWaveCount);
        }

        internal static LevelRuntimeSerializationState NormalizeRaw(
            IReadOnlyDictionary<string, object?> data,
            string fallbackActiveLevelId,
            IReadOnlyList<string> knownLevelIds,
            int activeWaveCount)
        {
            var knownLevelSet = new HashSet<string>(knownLevelIds, StringComparer.Ordinal);
            string firstKnownLevelId = knownLevelIds.Count > 0 ? knownLevelIds[0] : string.Empty;
            string activeLevelId = ReadKnownLevelId(data, "active_level_id", knownLevelSet, fallbackActiveLevelId, firstKnownLevelId);

            var state = new LevelRuntimeSerializationState
            {
                ActiveLevelId = activeLevelId,
                ActiveWaveIndex = activeWaveCount > 0 ? Math.Clamp(RawRead.Int(data, "active_wave_index"), 0, activeWaveCount - 1) : 0
            };

            CopyKnownLevelIds(data, "unlocked_level_ids", knownLevelSet, state.UnlockedLevelIds);
            CopyKnownLevelIds(data, "boss_cleared_level_ids", knownLevelSet, state.BossClearedLevelIds);

            if (state.UnlockedLevelIds.Count == 0 && !string.IsNullOrEmpty(firstKnownLevelId))
            {
                state.UnlockedLevelIds.Add(firstKnownLevelId);
            }

            CopyNonNegativeIntDictionary(data, "level_clear_count_by_id", state.LevelClearCountById);
            CopyNonNegativeIntDictionary(data, "pity_counter_by_key", state.PityCounterByKey);
            CopyNonNegativeIntDictionary(data, "daily_roll_count_by_table", state.DailyRollCountByTable);
            CopyNonNegativeLongDictionary(data, "daily_roll_day_by_table", state.DailyRollDayByTable);
            CopyNonNegativeIntDictionary(data, "hourly_roll_count_by_table", state.HourlyRollCountByTable);
            CopyNonNegativeLongDictionary(data, "hourly_roll_hour_by_table", state.HourlyRollHourByTable);
            return state;
        }

        private static string ReadKnownLevelId(
            IReadOnlyDictionary<string, object?> data,
            string key,
            HashSet<string> knownLevelIds,
            string fallbackActiveLevelId,
            string firstKnownLevelId)
        {
            string fallback = knownLevelIds.Contains(fallbackActiveLevelId) ? fallbackActiveLevelId : firstKnownLevelId;
            if (!data.TryGetValue(key, out object? value) || value is not string candidate)
            {
                return fallback;
            }

            return knownLevelIds.Contains(candidate) ? candidate : fallback;
        }

        private static void CopyKnownLevelIds(
            IReadOnlyDictionary<string, object?> data,
            string key,
            HashSet<string> knownLevelIds,
            HashSet<string> destination)
        {
            if (!data.TryGetValue(key, out object? value) || value is not IEnumerable<object?> items)
            {
                return;
            }

            foreach (object? item in items)
            {
                string levelId = item as string ?? string.Empty;
                if (knownLevelIds.Contains(levelId))
                {
                    destination.Add(levelId);
                }
            }
        }

        private static void CopyNonNegativeIntDictionary(
            IReadOnlyDictionary<string, object?> data,
            string key,
            Dictionary<string, int> destination)
        {
            if (!data.TryGetValue(key, out object? value) || value is not IReadOnlyDictionary<string, object?> source)
            {
                return;
            }

            foreach ((string itemKey, object? itemValue) in source)
            {
                if (string.IsNullOrEmpty(itemKey))
                {
                    continue;
                }

                destination[itemKey] = Math.Max(0, RawRead.Int(itemValue));
            }
        }

        private static void CopyNonNegativeLongDictionary(
            IReadOnlyDictionary<string, object?> data,
            string key,
            Dictionary<string, long> destination)
        {
            if (!data.TryGetValue(key, out object? value) || value is not IReadOnlyDictionary<string, object?> source)
            {
                return;
            }

            foreach ((string itemKey, object? itemValue) in source)
            {
                if (string.IsNullOrEmpty(itemKey))
                {
                    continue;
                }

                destination[itemKey] = Math.Max(0L, RawRead.Long(itemValue));
            }
        }

        private static Dictionary<string, object?> ToRawDictionary(Dictionary<string, int> source)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach ((string key, int value) in source.OrderBy(static item => item.Key, StringComparer.Ordinal))
            {
                normalized[key] = Math.Max(0, value);
            }

            return normalized;
        }

        private static Dictionary<string, object?> ToRawDictionary(Dictionary<string, long> source)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach ((string key, long value) in source.OrderBy(static item => item.Key, StringComparer.Ordinal))
            {
                normalized[key] = Math.Max(0L, value);
            }

            return normalized;
        }
    }

    internal static class RawRead
    {
        public static int Int(IReadOnlyDictionary<string, object?> data, string key, int fallback = 0)
        {
            return data.TryGetValue(key, out object? value) ? Int(value, fallback) : fallback;
        }

        public static int Int(object? value, int fallback = 0)
        {
            return value switch
            {
                int intValue => intValue,
                long longValue when longValue >= int.MinValue && longValue <= int.MaxValue => (int)longValue,
                float floatValue => (int)floatValue,
                double doubleValue => (int)doubleValue,
                _ => fallback
            };
        }

        public static long Long(IReadOnlyDictionary<string, object?> data, string key, long fallback = 0)
        {
            return data.TryGetValue(key, out object? value) ? Long(value, fallback) : fallback;
        }

        public static long Long(object? value, long fallback = 0)
        {
            return value switch
            {
                int intValue => intValue,
                long longValue => longValue,
                float floatValue => (long)floatValue,
                double doubleValue => (long)doubleValue,
                _ => fallback
            };
        }

        public static double Double(IReadOnlyDictionary<string, object?> data, string key, double fallback = 0.0)
        {
            return data.TryGetValue(key, out object? value) ? Double(value, fallback) : fallback;
        }

        public static double Double(object? value, double fallback = 0.0)
        {
            return value switch
            {
                int intValue => intValue,
                long longValue => longValue,
                float floatValue => floatValue,
                double doubleValue => doubleValue,
                _ => fallback
            };
        }

        public static string String(IReadOnlyDictionary<string, object?> data, string key, string fallback)
        {
            return data.TryGetValue(key, out object? value) ? String(value, fallback) : fallback;
        }

        public static string String(object? value, string fallback)
        {
            return value as string ?? fallback;
        }
    }

    internal static class RawVariantBridge
    {
        public static Dictionary<string, object?> ToRawDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (string key in data.Keys)
            {
                normalized[key] = ToRawValue(data[key]);
            }

            return normalized;
        }

        public static Godot.Collections.Dictionary<string, Variant> ToVariantDictionary(IReadOnlyDictionary<string, object?> data)
        {
            var normalized = new Godot.Collections.Dictionary<string, Variant>();
            foreach ((string key, object? value) in data)
            {
                normalized[key] = ToVariant(value);
            }

            return normalized;
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

        private static Godot.Collections.Array<Variant> ToVariantArray(IEnumerable<object?> values)
        {
            var array = new Godot.Collections.Array<Variant>();
            foreach (object? value in values)
            {
                array.Add(ToVariant(value));
            }

            return array;
        }
    }
}
