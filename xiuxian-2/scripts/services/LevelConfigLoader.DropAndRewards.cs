using Godot;
using System;
using System.Collections.Generic;
using Xiuxian.Scripts.Core;

namespace Xiuxian.Scripts.Services
{
    public partial class LevelConfigLoader
    {
        public Dictionary<string, int> RollMonsterDrops(string monsterId)
        {
            var result = new Dictionary<string, int>();
            ResetLastDropDebug();
            if (!TryGetMonster(monsterId, out var monster))
            {
                return result;
            }
            if (!TryGetChildDictionary(monster, "drops", out var drops))
            {
                return result;
            }

            string configuredDropTableId = GetString(drops, "drop_table_id", "");
            string dropTableId = ResolveDropTableForActiveLevel(monsterId, configuredDropTableId);
            _lastDropTableResolved = dropTableId;
            int dropRollCount = Math.Max(0, drops.ContainsKey("drop_roll_count") ? drops["drop_roll_count"].AsInt32() : 1);
            string pityCounterKey = "";
            string pityItemId = "";
            int pityThreshold = 0;
            int pityQty = 0;

            if (!string.IsNullOrEmpty(dropTableId) && dropRollCount > 0)
            {
                if (TryGetDropTable(dropTableId, out var table))
                {
                    ReadPityConfig(table, out pityCounterKey, out pityThreshold, out pityItemId, out pityQty);
                }
                AddDropRollResults(dropTableId, dropRollCount, result);
            }

            if (drops.ContainsKey("guaranteed_drop"))
            {
                Variant guaranteedVariant = drops["guaranteed_drop"];
                if (guaranteedVariant.VariantType == Variant.Type.Array)
                {
                    var guaranteed = (Godot.Collections.Array<Variant>)guaranteedVariant;
                    foreach (Variant item in guaranteed)
                    {
                        if (item.VariantType != Variant.Type.Dictionary)
                        {
                            continue;
                        }

                        var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                        string itemId = GetString(dict, "item_id", "");
                        int qty = Math.Max(0, dict.ContainsKey("qty") ? dict["qty"].AsInt32() : 0);
                        AddDrop(result, itemId, qty);
                    }
                }
            }

            ApplyPity(dropTableId, pityCounterKey, pityThreshold, pityItemId, pityQty, result);
            return result;
        }

        public bool TryRollMonsterSettlementReward(string monsterId, out double lingqi, out double insight)
        {
            lingqi = 0.0;
            insight = 0.0;

            if (!TryGetMonster(monsterId, out var monster))
            {
                return false;
            }
            if (!TryGetChildDictionary(monster, "settlement_reward", out var settlement))
            {
                return false;
            }

            int lingqiMin = settlement.ContainsKey("lingqi_min") ? settlement["lingqi_min"].AsInt32() : 0;
            int lingqiMax = settlement.ContainsKey("lingqi_max") ? settlement["lingqi_max"].AsInt32() : lingqiMin;
            int insightMin = settlement.ContainsKey("insight_min") ? settlement["insight_min"].AsInt32() : 0;
            int insightMax = settlement.ContainsKey("insight_max") ? settlement["insight_max"].AsInt32() : insightMin;

            if (lingqiMax < lingqiMin)
            {
                lingqiMax = lingqiMin;
            }
            if (insightMax < insightMin)
            {
                insightMax = insightMin;
            }

            lingqi = _rng.RandiRange(lingqiMin, lingqiMax);
            insight = _rng.RandiRange(insightMin, insightMax);
            return true;
        }

        public bool TryBuildLevelCompletionReward(
            out string levelId,
            out bool firstClear,
            out double lingqi,
            out double insight,
            out Dictionary<string, int> items)
        {
            levelId = ActiveLevelId;
            firstClear = false;
            lingqi = 0.0;
            insight = 0.0;
            items = new Dictionary<string, int>();

            if (!TryGetActiveLevel(out var level))
            {
                return false;
            }
            if (!TryGetChildDictionary(level, "rewards", out var rewards))
            {
                return false;
            }

            int clearCount = _levelClearCountById.TryGetValue(ActiveLevelId, out int c) ? c : 0;
            firstClear = clearCount <= 0;

            if (firstClear)
            {
                if (!TryGetChildDictionary(rewards, "first_clear", out var first))
                {
                    return false;
                }

                lingqi = first.ContainsKey("lingqi") ? first["lingqi"].AsDouble() : 0.0;
                insight = first.ContainsKey("insight") ? first["insight"].AsDouble() : 0.0;
                if (first.ContainsKey("items") && first["items"].VariantType == Variant.Type.Array)
                {
                    var itemArray = (Godot.Collections.Array<Variant>)first["items"];
                    foreach (Variant v in itemArray)
                    {
                        if (v.VariantType != Variant.Type.Dictionary)
                        {
                            continue;
                        }

                        var dict = (Godot.Collections.Dictionary<string, Variant>)v;
                        string itemId = GetString(dict, "item_id", "");
                        int qty = dict.ContainsKey("qty") ? dict["qty"].AsInt32() : 0;
                        AddDrop(items, itemId, qty);
                    }
                }
            }
            else
            {
                if (!TryGetChildDictionary(rewards, "repeat_clear", out var repeat))
                {
                    return false;
                }

                int lingqiMin = repeat.ContainsKey("lingqi_min") ? repeat["lingqi_min"].AsInt32() : 0;
                int lingqiMax = repeat.ContainsKey("lingqi_max") ? repeat["lingqi_max"].AsInt32() : lingqiMin;
                int insightMin = repeat.ContainsKey("insight_min") ? repeat["insight_min"].AsInt32() : 0;
                int insightMax = repeat.ContainsKey("insight_max") ? repeat["insight_max"].AsInt32() : insightMin;

                if (lingqiMax < lingqiMin)
                {
                    lingqiMax = lingqiMin;
                }
                if (insightMax < insightMin)
                {
                    insightMax = insightMin;
                }

                lingqi = _rng.RandiRange(lingqiMin, lingqiMax);
                insight = _rng.RandiRange(insightMin, insightMax);
            }

            _levelClearCountById[ActiveLevelId] = clearCount + 1;
            return true;
        }

        private void AddDropRollResults(string dropTableId, int rollCount, Dictionary<string, int> result)
        {
            if (!TryGetDropTable(dropTableId, out var table))
            {
                return;
            }
            if (!table.ContainsKey("entries"))
            {
                return;
            }

            Variant entriesVariant = table["entries"];
            if (entriesVariant.VariantType != Variant.Type.Array)
            {
                return;
            }

            var entries = (Godot.Collections.Array<Variant>)entriesVariant;
            for (int i = 0; i < rollCount; i++)
            {
                if (!TryConsumeDropRoll(table, dropTableId, out int hourlyCountAfterConsume))
                {
                    break;
                }

                if (ShouldSkipDropBySoftCap(table, hourlyCountAfterConsume))
                {
                    continue;
                }

                Godot.Collections.Dictionary<string, Variant> picked = PickWeightedDropEntry(entries);
                if (picked.Count == 0)
                {
                    continue;
                }

                string itemId = GetString(picked, "item_id", "");
                int minQty = Math.Max(0, picked.ContainsKey("min_qty") ? picked["min_qty"].AsInt32() : 1);
                int maxQty = Math.Max(minQty, picked.ContainsKey("max_qty") ? picked["max_qty"].AsInt32() : minQty);
                int qty = _rng.RandiRange(minQty, maxQty);
                AddDrop(result, itemId, qty);
            }
        }

        private void ApplyPity(
            string dropTableId,
            string pityCounterKey,
            int pityThreshold,
            string pityItemId,
            int pityQty,
            Dictionary<string, int> result)
        {
            if (string.IsNullOrEmpty(dropTableId) || string.IsNullOrEmpty(pityCounterKey) || pityThreshold <= 0 || string.IsNullOrEmpty(pityItemId))
            {
                return;
            }

            var pityResult = DropEconomyRule.ApplyPity(result, _pityCounterByKey, pityCounterKey, pityThreshold, pityItemId, pityQty);
            _lastPityTriggered = pityResult.Triggered;
            _lastPityCounterKey = pityCounterKey;
            _lastPityCounterValue = pityResult.CounterValue;
        }

        private bool TryConsumeDropRoll(
            Godot.Collections.Dictionary<string, Variant> table,
            string dropTableId,
            out int hourlyCountAfterConsume)
        {
            hourlyCountAfterConsume = 0;
            long unix = (long)Time.GetUnixTimeFromSystem();
            long dayIndex = unix / 86400;
            long hourIndex = unix / 3600;

            int dailyCap = ReadDailyCap(table);
            if (!_dailyRollDayByTable.TryGetValue(dropTableId, out long savedDay) || savedDay != dayIndex)
            {
                _dailyRollDayByTable[dropTableId] = dayIndex;
                _dailyRollCountByTable[dropTableId] = 0;
            }

            int dailyCount = _dailyRollCountByTable.TryGetValue(dropTableId, out int d) ? d : 0;
            if (!DropEconomyRule.CanConsumeDailyRoll(dailyCap, dailyCount))
            {
                _lastDailyCapBlocked = true;
                return false;
            }
            _dailyRollCountByTable[dropTableId] = dailyCount + 1;

            if (!_hourlyRollHourByTable.TryGetValue(dropTableId, out long savedHour) || savedHour != hourIndex)
            {
                _hourlyRollHourByTable[dropTableId] = hourIndex;
                _hourlyRollCountByTable[dropTableId] = 0;
            }

            int hourlyCount = _hourlyRollCountByTable.TryGetValue(dropTableId, out int h) ? h : 0;
            hourlyCountAfterConsume = hourlyCount + 1;
            _hourlyRollCountByTable[dropTableId] = hourlyCountAfterConsume;
            return true;
        }

        private bool ShouldSkipDropBySoftCap(Godot.Collections.Dictionary<string, Variant> table, int hourlyCountAfterConsume)
        {
            int softCap = ReadHourlySoftCap(table);
            if (softCap <= 0 || hourlyCountAfterConsume <= softCap)
            {
                return false;
            }

            double decay = ReadRepeatDecay(table);
            bool skipped = DropEconomyRule.ShouldSkipBySoftCap(softCap, hourlyCountAfterConsume, decay, _rng.Randf());
            if (skipped)
            {
                _lastSoftCapSkipped = true;
            }
            return skipped;
        }

        private static void ReadPityConfig(
            Godot.Collections.Dictionary<string, Variant> table,
            out string pityCounterKey,
            out int pityThreshold,
            out string pityItemId,
            out int pityQty)
        {
            pityCounterKey = "";
            pityThreshold = 0;
            pityItemId = "";
            pityQty = 0;

            if (!TryGetChildDictionary(table, "pity", out var pity))
            {
                return;
            }

            pityCounterKey = GetString(pity, "counter_key", "");
            pityThreshold = pity.ContainsKey("threshold") ? pity["threshold"].AsInt32() : 0;
            pityItemId = GetString(pity, "item_id", "");
            pityQty = pity.ContainsKey("qty") ? pity["qty"].AsInt32() : 1;
        }

        private static int ReadDailyCap(Godot.Collections.Dictionary<string, Variant> table)
        {
            if (!TryGetChildDictionary(table, "economy", out var economy))
            {
                return 0;
            }

            return economy.ContainsKey("daily_cap_rolls") ? Math.Max(0, economy["daily_cap_rolls"].AsInt32()) : 0;
        }

        private static int ReadHourlySoftCap(Godot.Collections.Dictionary<string, Variant> table)
        {
            if (!TryGetChildDictionary(table, "economy", out var economy))
            {
                return 0;
            }

            return economy.ContainsKey("hourly_soft_cap_rolls") ? Math.Max(0, economy["hourly_soft_cap_rolls"].AsInt32()) : 0;
        }

        private static double ReadRepeatDecay(Godot.Collections.Dictionary<string, Variant> table)
        {
            if (!TryGetChildDictionary(table, "economy", out var economy))
            {
                return 1.0;
            }

            if (!economy.ContainsKey("repeat_decay_factor"))
            {
                return 1.0;
            }

            double decay = economy["repeat_decay_factor"].AsDouble();
            if (decay < 0.0)
            {
                return 0.0;
            }

            return decay;
        }

        private Godot.Collections.Dictionary<string, Variant> PickWeightedDropEntry(Godot.Collections.Array<Variant> entries)
        {
            int totalWeight = 0;
            foreach (Variant item in entries)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                totalWeight += Math.Max(0, dict.ContainsKey("weight") ? dict["weight"].AsInt32() : 0);
            }

            if (totalWeight <= 0)
            {
                return new Godot.Collections.Dictionary<string, Variant>();
            }

            int roll = _rng.RandiRange(1, totalWeight);
            int acc = 0;
            foreach (Variant item in entries)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                int weight = Math.Max(0, dict.ContainsKey("weight") ? dict["weight"].AsInt32() : 0);
                if (weight <= 0)
                {
                    continue;
                }

                acc += weight;
                if (roll <= acc)
                {
                    return dict;
                }
            }

            return new Godot.Collections.Dictionary<string, Variant>();
        }

        private static void AddDrop(Dictionary<string, int> result, string itemId, int qty)
        {
            if (string.IsNullOrEmpty(itemId) || qty <= 0)
            {
                return;
            }

            if (!result.ContainsKey(itemId))
            {
                result[itemId] = 0;
            }
            result[itemId] += qty;
        }

        private string ResolveDropTableForActiveLevel(string monsterId, string configuredDropTableId)
        {
            string levelId = ActiveLevelId;
            if (!string.IsNullOrEmpty(configuredDropTableId)
                && TryGetDropTable(configuredDropTableId, out var configuredTable)
                && IsTableBoundToLevel(configuredTable, levelId))
            {
                return configuredDropTableId;
            }

            foreach (var kv in _dropTableById)
            {
                var table = kv.Value;
                if (!IsTableBoundToLevel(table, levelId))
                {
                    continue;
                }
                if (!IsTableBoundToMonster(table, monsterId))
                {
                    continue;
                }

                return kv.Key;
            }

            return configuredDropTableId;
        }

        private static bool IsTableBoundToLevel(Godot.Collections.Dictionary<string, Variant> table, string levelId)
        {
            string boundLevelId = GetString(table, "bind_level_id", "");
            if (string.IsNullOrEmpty(boundLevelId) || string.IsNullOrEmpty(levelId))
            {
                return true;
            }

            return boundLevelId == levelId;
        }

        private static bool IsTableBoundToMonster(Godot.Collections.Dictionary<string, Variant> table, string monsterId)
        {
            if (string.IsNullOrEmpty(monsterId))
            {
                return false;
            }

            if (!table.ContainsKey("bind_monster_ids"))
            {
                return true;
            }

            Variant bindVariant = table["bind_monster_ids"];
            if (bindVariant.VariantType != Variant.Type.Array)
            {
                return true;
            }

            var bindArray = (Godot.Collections.Array<Variant>)bindVariant;
            foreach (Variant item in bindArray)
            {
                if (item.AsString() == monsterId)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildTopDropsSummary(Dictionary<string, int> itemTotals, int topN)
        {
            if (itemTotals.Count == 0)
            {
                return "none";
            }

            var list = new List<KeyValuePair<string, int>>(itemTotals);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            int n = Math.Min(Math.Max(1, topN), list.Count);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < n; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }
                sb.Append($"{list[i].Key}x{list[i].Value}");
            }

            return sb.ToString();
        }

        private void ResetLastDropDebug()
        {
            _lastDropTableResolved = "";
            _lastDailyCapBlocked = false;
            _lastSoftCapSkipped = false;
            _lastPityTriggered = false;
            _lastPityCounterKey = "";
            _lastPityCounterValue = 0;
        }
    }
}
