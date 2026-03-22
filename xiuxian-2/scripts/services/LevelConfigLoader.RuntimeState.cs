using Godot;
using System;
using System.Collections.Generic;
using Xiuxian.Scripts.Core;

namespace Xiuxian.Scripts.Services
{
    public partial class LevelConfigLoader
    {
        public Godot.Collections.Array<string> GetLevelIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (var level in _levels)
            {
                string levelId = GetString(level, "level_id", "");
                if (!string.IsNullOrEmpty(levelId))
                {
                    result.Add(levelId);
                }
            }
            return result;
        }

        public string GetLevelName(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return "";
            }

            foreach (var level in _levels)
            {
                string id = GetString(level, "level_id", "");
                if (id == levelId)
                {
                    return GetString(level, "level_name", levelId);
                }
            }

            return "";
        }

        public Godot.Collections.Array<string> GetUnlockedLevelIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (var level in _levels)
            {
                string levelId = GetString(level, "level_id", "");
                if (string.IsNullOrEmpty(levelId))
                {
                    continue;
                }

                if (_unlockedLevelIds.Contains(levelId))
                {
                    result.Add(levelId);
                }
            }

            return result;
        }

        public bool IsLevelUnlocked(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return false;
            }

            EnsureLevelUnlockBootstrap();
            return _unlockedLevelIds.Contains(levelId);
        }

        public bool IsBossMonsterForLevel(string levelId, string monsterId)
        {
            if (string.IsNullOrEmpty(levelId) || string.IsNullOrEmpty(monsterId))
            {
                return false;
            }

            if (!TryFindLevelIndex(levelId, out int levelIndex))
            {
                return false;
            }

            string bossId = GetLevelBossMonsterId(_levels[levelIndex]);
            return !string.IsNullOrEmpty(bossId) && bossId == monsterId;
        }

        public bool TryMarkBossDefeatedAndUnlockNext(string levelId, string monsterId, out string unlockedLevelId)
        {
            unlockedLevelId = "";
            if (!IsBossMonsterForLevel(levelId, monsterId))
            {
                return false;
            }

            _bossClearedLevelIds.Add(levelId);
            string next = GetConfiguredNextLevelId(levelId);
            if (string.IsNullOrEmpty(next))
            {
                next = GetNextLevelId(levelId);
            }

            if (string.IsNullOrEmpty(next))
            {
                return false;
            }

            if (_unlockedLevelIds.Add(next))
            {
                unlockedLevelId = next;
                return true;
            }

            return false;
        }

        public Godot.Collections.Array<string> GetSpawnMonsterIds(string levelId = "")
        {
            var result = new Godot.Collections.Array<string>();
            if (_levels.Count == 0)
            {
                return result;
            }

            int levelIndex = _activeLevelIndex;
            if (!string.IsNullOrEmpty(levelId) && TryFindLevelIndex(levelId, out int found))
            {
                levelIndex = found;
            }

            var level = _levels[Math.Clamp(levelIndex, 0, _levels.Count - 1)];
            if (!level.ContainsKey("spawn_table") || level["spawn_table"].VariantType != Variant.Type.Array)
            {
                return result;
            }

            var spawnTable = (Godot.Collections.Array<Variant>)level["spawn_table"];
            var unique = new HashSet<string>();
            foreach (Variant item in spawnTable)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                string monsterId = GetString(dict, "monster_id", "");
                if (string.IsNullOrEmpty(monsterId))
                {
                    continue;
                }

                if (unique.Add(monsterId))
                {
                    result.Add(monsterId);
                }
            }

            return result;
        }

        public Godot.Collections.Dictionary<string, Variant> ToRuntimeDictionary()
        {
            var unlocked = new Godot.Collections.Array<Variant>();
            foreach (string levelId in _unlockedLevelIds)
            {
                unlocked.Add(levelId);
            }

            var bossCleared = new Godot.Collections.Array<Variant>();
            foreach (string levelId in _bossClearedLevelIds)
            {
                bossCleared.Add(levelId);
            }

            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["active_level_id"] = ActiveLevelId,
                ["active_wave_index"] = _activeLevelWaveIndex,
                ["unlocked_level_ids"] = unlocked,
                ["boss_cleared_level_ids"] = bossCleared,
                ["level_clear_count_by_id"] = IntDictionaryToVariantDictionary(_levelClearCountById),
                ["pity_counter_by_key"] = IntDictionaryToVariantDictionary(_pityCounterByKey),
                ["daily_roll_count_by_table"] = IntDictionaryToVariantDictionary(_dailyRollCountByTable),
                ["daily_roll_day_by_table"] = LongDictionaryToVariantDictionary(_dailyRollDayByTable),
                ["hourly_roll_count_by_table"] = IntDictionaryToVariantDictionary(_hourlyRollCountByTable),
                ["hourly_roll_hour_by_table"] = LongDictionaryToVariantDictionary(_hourlyRollHourByTable)
            };
        }

        public void FromRuntimeDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            _pityCounterByKey.Clear();
            _levelClearCountById.Clear();
            _dailyRollCountByTable.Clear();
            _dailyRollDayByTable.Clear();
            _hourlyRollCountByTable.Clear();
            _hourlyRollHourByTable.Clear();
            _unlockedLevelIds.Clear();
            _bossClearedLevelIds.Clear();

            if (data.ContainsKey("level_clear_count_by_id") && data["level_clear_count_by_id"].VariantType == Variant.Type.Dictionary)
            {
                VariantDictionaryToIntDictionary((Godot.Collections.Dictionary<string, Variant>)data["level_clear_count_by_id"], _levelClearCountById);
            }
            if (data.ContainsKey("pity_counter_by_key") && data["pity_counter_by_key"].VariantType == Variant.Type.Dictionary)
            {
                VariantDictionaryToIntDictionary((Godot.Collections.Dictionary<string, Variant>)data["pity_counter_by_key"], _pityCounterByKey);
            }
            if (data.ContainsKey("daily_roll_count_by_table") && data["daily_roll_count_by_table"].VariantType == Variant.Type.Dictionary)
            {
                VariantDictionaryToIntDictionary((Godot.Collections.Dictionary<string, Variant>)data["daily_roll_count_by_table"], _dailyRollCountByTable);
            }
            if (data.ContainsKey("daily_roll_day_by_table") && data["daily_roll_day_by_table"].VariantType == Variant.Type.Dictionary)
            {
                VariantDictionaryToLongDictionary((Godot.Collections.Dictionary<string, Variant>)data["daily_roll_day_by_table"], _dailyRollDayByTable);
            }
            if (data.ContainsKey("hourly_roll_count_by_table") && data["hourly_roll_count_by_table"].VariantType == Variant.Type.Dictionary)
            {
                VariantDictionaryToIntDictionary((Godot.Collections.Dictionary<string, Variant>)data["hourly_roll_count_by_table"], _hourlyRollCountByTable);
            }
            if (data.ContainsKey("hourly_roll_hour_by_table") && data["hourly_roll_hour_by_table"].VariantType == Variant.Type.Dictionary)
            {
                VariantDictionaryToLongDictionary((Godot.Collections.Dictionary<string, Variant>)data["hourly_roll_hour_by_table"], _hourlyRollHourByTable);
            }

            if (data.ContainsKey("active_level_id"))
            {
                string levelId = data["active_level_id"].AsString();
                if (!string.IsNullOrEmpty(levelId))
                {
                    TrySetActiveLevel(levelId);
                }
            }

            if (data.ContainsKey("unlocked_level_ids") && data["unlocked_level_ids"].VariantType == Variant.Type.Array)
            {
                var unlocked = (Godot.Collections.Array<Variant>)data["unlocked_level_ids"];
                foreach (Variant v in unlocked)
                {
                    string levelId = v.AsString();
                    if (!string.IsNullOrEmpty(levelId))
                    {
                        _unlockedLevelIds.Add(levelId);
                    }
                }
            }

            if (data.ContainsKey("boss_cleared_level_ids") && data["boss_cleared_level_ids"].VariantType == Variant.Type.Array)
            {
                var cleared = (Godot.Collections.Array<Variant>)data["boss_cleared_level_ids"];
                foreach (Variant v in cleared)
                {
                    string levelId = v.AsString();
                    if (!string.IsNullOrEmpty(levelId))
                    {
                        _bossClearedLevelIds.Add(levelId);
                    }
                }
            }

            EnsureLevelUnlockBootstrap();

            if (data.ContainsKey("active_wave_index"))
            {
                int savedWaveIndex = data["active_wave_index"].AsInt32();
                if (_activeLevelMonsterWave.Count > 0)
                {
                    _activeLevelWaveIndex = Math.Clamp(savedWaveIndex, 0, _activeLevelMonsterWave.Count - 1);
                }
                else
                {
                    _activeLevelWaveIndex = 0;
                }
            }
        }

        private bool TryFindLevelIndex(string levelId, out int levelIndex)
        {
            levelIndex = -1;
            if (string.IsNullOrEmpty(levelId))
            {
                return false;
            }

            for (int i = 0; i < _levels.Count; i++)
            {
                string id = GetString(_levels[i], "level_id", "");
                if (id == levelId)
                {
                    levelIndex = i;
                    return true;
                }
            }

            return false;
        }

        private void EnsureLevelUnlockBootstrap()
        {
            if (_levels.Count == 0)
            {
                return;
            }

            if (_unlockedLevelIds.Count > 0)
            {
                return;
            }

            string firstLevelId = GetString(_levels[0], "level_id", "");
            if (!string.IsNullOrEmpty(firstLevelId))
            {
                _unlockedLevelIds.Add(firstLevelId);
            }
        }

        private string GetNextLevelId(string levelId)
        {
            if (!TryFindLevelIndex(levelId, out int index))
            {
                return "";
            }

            int next = index + 1;
            if (next < 0 || next >= _levels.Count)
            {
                return "";
            }

            return GetString(_levels[next], "level_id", "");
        }

        private string GetConfiguredNextLevelId(string levelId)
        {
            if (!TryFindLevelIndex(levelId, out int index))
            {
                return "";
            }

            return GetString(_levels[index], "unlock_next_level_id", "");
        }

        private string GetNextUnlockedLevelId(string levelId)
        {
            var unlocked = GetUnlockedLevelIds();
            return LevelCycleRule.GetNextUnlockedLevelId(unlocked, levelId);
        }

        private static string GetLevelBossMonsterId(Godot.Collections.Dictionary<string, Variant> level)
        {
            string configured = GetString(level, "boss_monster_id", "");
            if (!string.IsNullOrEmpty(configured))
            {
                return configured;
            }

            if (level.ContainsKey("monster_wave") && level["monster_wave"].VariantType == Variant.Type.Array)
            {
                var wave = (Godot.Collections.Array<Variant>)level["monster_wave"];
                for (int i = wave.Count - 1; i >= 0; i--)
                {
                    string id = wave[i].AsString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        return id;
                    }
                }
            }

            return "";
        }

        private static Godot.Collections.Dictionary<string, Variant> IntDictionaryToVariantDictionary(Dictionary<string, int> source)
        {
            var result = new Godot.Collections.Dictionary<string, Variant>();
            foreach (var kv in source)
            {
                result[kv.Key] = kv.Value;
            }
            return result;
        }

        private static Godot.Collections.Dictionary<string, Variant> LongDictionaryToVariantDictionary(Dictionary<string, long> source)
        {
            var result = new Godot.Collections.Dictionary<string, Variant>();
            foreach (var kv in source)
            {
                result[kv.Key] = kv.Value;
            }
            return result;
        }

        private static void VariantDictionaryToIntDictionary(
            Godot.Collections.Dictionary<string, Variant> source,
            Dictionary<string, int> destination)
        {
            foreach (string key in source.Keys)
            {
                destination[key] = source[key].AsInt32();
            }
        }

        private static void VariantDictionaryToLongDictionary(
            Godot.Collections.Dictionary<string, Variant> source,
            Dictionary<string, long> destination)
        {
            foreach (string key in source.Keys)
            {
                destination[key] = source[key].AsInt64();
            }
        }

        private static void MergeDictionary<TKey, TValue>(Dictionary<TKey, TValue> destination, Dictionary<TKey, TValue> source)
            where TKey : notnull
        {
            foreach (var kv in source)
            {
                destination[kv.Key] = kv.Value;
            }
        }

        private int GetLevelClearCount(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return 0;
            }

            return _levelClearCountById.TryGetValue(levelId, out int count) ? count : 0;
        }
    }
}
