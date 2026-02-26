using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Loads level/monster/drop configuration from JSON and exposes indexed lookup.
    /// </summary>
    public partial class LevelConfigLoader : Node
    {
        [Signal]
        public delegate void ConfigLoadedEventHandler(string levelId, string levelName);

        [Export] public string ConfigPath = "res://docs/design/09_level_monster_drop_sample.json";

        public string ActiveLevelId { get; private set; } = "";
        public string ActiveLevelName { get; private set; } = "Unknown Zone";
        public double ProgressPer100Inputs { get; private set; } = 2.0;
        public double EncounterCheckIntervalProgress { get; private set; } = 20.0;
        public double BaseEncounterRate { get; private set; } = 0.18;
        public double BattlePauseFactor { get; private set; } = 0.0;
        public int PlayerBaseHp { get; private set; } = 36;
        public int PlayerAttackPerRound { get; private set; } = 4;
        public int EnemyDamageDivider { get; private set; } = 4;
        public int EnemyMinDamagePerRound { get; private set; } = 1;

        private Godot.Collections.Dictionary<string, Variant> _rootData = new();
        private readonly List<Godot.Collections.Dictionary<string, Variant>> _levels = new();
        private int _activeLevelIndex;
        private readonly Dictionary<string, Godot.Collections.Dictionary<string, Variant>> _monsterById = new();
        private readonly Dictionary<string, Godot.Collections.Dictionary<string, Variant>> _dropTableById = new();
        private readonly Dictionary<string, int> _levelClearCountById = new();
        private readonly Dictionary<string, int> _pityCounterByKey = new();
        private readonly Dictionary<string, int> _dailyRollCountByTable = new();
        private readonly Dictionary<string, long> _dailyRollDayByTable = new();
        private readonly Dictionary<string, int> _hourlyRollCountByTable = new();
        private readonly Dictionary<string, long> _hourlyRollHourByTable = new();
        private readonly RandomNumberGenerator _rng = new();
        private string _lastDropTableResolved = "";
        private bool _lastDailyCapBlocked;
        private bool _lastSoftCapSkipped;
        private bool _lastPityTriggered;
        private string _lastPityCounterKey = "";
        private int _lastPityCounterValue;
        private string _lastSimulationReport = "no simulation yet";

        public override void _Ready()
        {
            _rng.Randomize();
            LoadConfig();
        }

        public bool LoadConfig()
        {
            _monsterById.Clear();
            _dropTableById.Clear();
            _levelClearCountById.Clear();
            _pityCounterByKey.Clear();
            _dailyRollCountByTable.Clear();
            _dailyRollDayByTable.Clear();
            _hourlyRollCountByTable.Clear();
            _hourlyRollHourByTable.Clear();

            using FileAccess? file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PushWarning($"LevelConfigLoader: failed to open config at {ConfigPath}");
                return false;
            }

            string text = file.GetAsText();
            Variant parsed = Json.ParseString(text);
            if (parsed.VariantType != Variant.Type.Dictionary)
            {
                GD.PushWarning("LevelConfigLoader: config is not a valid dictionary JSON.");
                return false;
            }

            _rootData = (Godot.Collections.Dictionary<string, Variant>)parsed;
            ParseLevelsSection();
            IndexMonsters();
            IndexDropTables();

            EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
            GD.Print($"LevelConfigLoader: loaded level '{ActiveLevelId}' ({ActiveLevelName})");
            return true;
        }

        public bool AdvanceToNextLevel()
        {
            if (_levels.Count == 0)
            {
                return false;
            }

            _activeLevelIndex = (_activeLevelIndex + 1) % _levels.Count;
            ApplyActiveLevelData();
            EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
            return true;
        }

        public bool TrySetActiveLevel(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) || _levels.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < _levels.Count; i++)
            {
                string id = GetString(_levels[i], "level_id", "");
                if (id != levelId)
                {
                    continue;
                }

                _activeLevelIndex = i;
                ApplyActiveLevelData();
                EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
                return true;
            }

            return false;
        }

        public bool TryGetMonster(string monsterId, out Godot.Collections.Dictionary<string, Variant> monsterData)
        {
            if (_monsterById.TryGetValue(monsterId, out monsterData))
            {
                return true;
            }

            monsterData = new Godot.Collections.Dictionary<string, Variant>();
            return false;
        }

        public bool TryGetDropTable(string dropTableId, out Godot.Collections.Dictionary<string, Variant> dropTableData)
        {
            if (_dropTableById.TryGetValue(dropTableId, out dropTableData))
            {
                return true;
            }

            dropTableData = new Godot.Collections.Dictionary<string, Variant>();
            return false;
        }

        public string RollSpawnMonsterId()
        {
            if (!TryGetActiveLevel(out var level))
            {
                return "";
            }
            if (!level.ContainsKey("spawn_table"))
            {
                return "";
            }

            Variant spawnTableVariant = level["spawn_table"];
            if (spawnTableVariant.VariantType != Variant.Type.Array)
            {
                return "";
            }

            var spawnTable = (Godot.Collections.Array<Variant>)spawnTableVariant;
            int totalWeight = 0;
            foreach (Variant item in spawnTable)
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
                return "";
            }

            int roll = _rng.RandiRange(1, totalWeight);
            int acc = 0;
            foreach (Variant item in spawnTable)
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
                    return GetString(dict, "monster_id", "");
                }
            }

            return "";
        }

        public bool TryGetMonsterCombatParams(
            string monsterId,
            out string monsterName,
            out int hp,
            out int inputsPerRound,
            out int attack)
        {
            monsterName = "Enemy";
            hp = 24;
            inputsPerRound = 18;
            attack = 4;

            if (!TryGetMonster(monsterId, out var monster))
            {
                return false;
            }

            monsterName = GetString(monster, "monster_name", monsterName);
            if (!TryGetChildDictionary(monster, "combat", out var combat))
            {
                return true;
            }

            hp = Math.Max(1, combat.ContainsKey("hp") ? combat["hp"].AsInt32() : hp);
            inputsPerRound = Math.Max(1, combat.ContainsKey("inputs_per_round") ? combat["inputs_per_round"].AsInt32() : inputsPerRound);
            attack = Math.Max(1, combat.ContainsKey("attack") ? combat["attack"].AsInt32() : attack);
            return true;
        }

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

        public string BuildDebugSummary()
        {
            var sb = new StringBuilder();
            sb.Append($"Level {ActiveLevelId} | dropTable {_lastDropTableResolved}");
            sb.Append($" | dailyCapBlocked={_lastDailyCapBlocked}");
            sb.Append($" | softCapSkip={_lastSoftCapSkipped}");
            sb.Append($" | pityTriggered={_lastPityTriggered}");
            sb.Append($" | clearCount={GetLevelClearCount(ActiveLevelId)}");

            if (!string.IsNullOrEmpty(_lastPityCounterKey))
            {
                sb.Append($"\nPity {_lastPityCounterKey}: {_lastPityCounterValue}");
            }

            sb.Append("\nHourly rolls:");
            foreach (var kv in _hourlyRollCountByTable)
            {
                sb.Append($" {kv.Key}={kv.Value}");
            }

            sb.Append("\nDaily rolls:");
            foreach (var kv in _dailyRollCountByTable)
            {
                sb.Append($" {kv.Key}={kv.Value}");
            }

            sb.Append($"\nSim: {_lastSimulationReport}");

            return sb.ToString();
        }

        public string RunBattleSimulation(int battleCount, string forcedMonsterId = "")
        {
            int count = Math.Max(1, battleCount);

            var pityBackup = new Dictionary<string, int>(_pityCounterByKey);
            var dailyCountBackup = new Dictionary<string, int>(_dailyRollCountByTable);
            var dailyDayBackup = new Dictionary<string, long>(_dailyRollDayByTable);
            var hourlyCountBackup = new Dictionary<string, int>(_hourlyRollCountByTable);
            var hourlyHourBackup = new Dictionary<string, long>(_hourlyRollHourByTable);

            var itemTotals = new Dictionary<string, int>();
            double totalLingqi = 0.0;
            double totalInsight = 0.0;
            int pityTriggeredCount = 0;
            int dailyBlockedCount = 0;
            int softSkipCount = 0;

            for (int i = 0; i < count; i++)
            {
                string monsterId = forcedMonsterId;
                if (string.IsNullOrEmpty(monsterId))
                {
                    monsterId = RollSpawnMonsterId();
                }

                if (string.IsNullOrEmpty(monsterId))
                {
                    continue;
                }

                var drops = RollMonsterDrops(monsterId);
                foreach (var kv in drops)
                {
                    AddDrop(itemTotals, kv.Key, kv.Value);
                }

                if (TryRollMonsterSettlementReward(monsterId, out double lingqi, out double insight))
                {
                    totalLingqi += lingqi;
                    totalInsight += insight;
                }

                if (_lastPityTriggered)
                {
                    pityTriggeredCount++;
                }
                if (_lastDailyCapBlocked)
                {
                    dailyBlockedCount++;
                }
                if (_lastSoftCapSkipped)
                {
                    softSkipCount++;
                }
            }

            _pityCounterByKey.Clear();
            _dailyRollCountByTable.Clear();
            _dailyRollDayByTable.Clear();
            _hourlyRollCountByTable.Clear();
            _hourlyRollHourByTable.Clear();
            MergeDictionary(_pityCounterByKey, pityBackup);
            MergeDictionary(_dailyRollCountByTable, dailyCountBackup);
            MergeDictionary(_dailyRollDayByTable, dailyDayBackup);
            MergeDictionary(_hourlyRollCountByTable, hourlyCountBackup);
            MergeDictionary(_hourlyRollHourByTable, hourlyHourBackup);

            double avgLingqi = totalLingqi / count;
            double avgInsight = totalInsight / count;
            string topDrops = BuildTopDropsSummary(itemTotals, 3);

            _lastSimulationReport =
                $"n={count}, avg_lq={avgLingqi:0.0}, avg_in={avgInsight:0.0}, pity={pityTriggeredCount}, softSkip={softSkipCount}, dailyBlock={dailyBlockedCount}, top={topDrops}";
            return _lastSimulationReport;
        }

        public Godot.Collections.Dictionary<string, Variant> ToRuntimeDictionary()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["active_level_id"] = ActiveLevelId,
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

            bool hasPityItem = result.ContainsKey(pityItemId) && result[pityItemId] > 0;
            if (hasPityItem)
            {
                _pityCounterByKey[pityCounterKey] = 0;
                _lastPityCounterKey = pityCounterKey;
                _lastPityCounterValue = 0;
                return;
            }

            int next = (_pityCounterByKey.TryGetValue(pityCounterKey, out int current) ? current : 0) + 1;
            if (next >= pityThreshold)
            {
                AddDrop(result, pityItemId, Math.Max(1, pityQty));
                _pityCounterByKey[pityCounterKey] = 0;
                _lastPityTriggered = true;
                _lastPityCounterKey = pityCounterKey;
                _lastPityCounterValue = 0;
                return;
            }

            _pityCounterByKey[pityCounterKey] = next;
            _lastPityCounterKey = pityCounterKey;
            _lastPityCounterValue = next;
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
            if (dailyCap > 0 && dailyCount >= dailyCap)
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
            if (decay <= 0.0)
            {
                return true;
            }

            int overflow = hourlyCountAfterConsume - softCap;
            double allowChance = Math.Pow(Math.Min(1.0, decay), overflow);
            bool skipped = _rng.Randf() > allowChance;
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

        private static string BuildTopDropsSummary(Dictionary<string, int> itemTotals, int topN)
        {
            if (itemTotals.Count == 0)
            {
                return "none";
            }

            var list = new List<KeyValuePair<string, int>>(itemTotals);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            int n = Math.Min(Math.Max(1, topN), list.Count);
            var sb = new StringBuilder();
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

        private int GetLevelClearCount(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return 0;
            }

            return _levelClearCountById.TryGetValue(levelId, out int count) ? count : 0;
        }

        private void ParseLevelsSection()
        {
            _levels.Clear();
            _activeLevelIndex = 0;

            if (_rootData.ContainsKey("levels"))
            {
                Variant levelsVariant = _rootData["levels"];
                if (levelsVariant.VariantType == Variant.Type.Array)
                {
                    var levels = (Godot.Collections.Array<Variant>)levelsVariant;
                    foreach (Variant item in levels)
                    {
                        if (item.VariantType != Variant.Type.Dictionary)
                        {
                            continue;
                        }

                        _levels.Add((Godot.Collections.Dictionary<string, Variant>)item);
                    }
                }
            }

            if (_levels.Count == 0 && TryGetChildDictionary(_rootData, "level", out var singleLevel))
            {
                _levels.Add(singleLevel);
            }

            ApplyActiveLevelData();
        }

        private void IndexMonsters()
        {
            if (!_rootData.ContainsKey("monsters"))
            {
                return;
            }

            Variant monstersVariant = _rootData["monsters"];
            if (monstersVariant.VariantType != Variant.Type.Array)
            {
                return;
            }

            var monsters = (Godot.Collections.Array<Variant>)monstersVariant;
            foreach (Variant item in monsters)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                string id = GetString(dict, "monster_id", "");
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                _monsterById[id] = dict;
            }
        }

        private void IndexDropTables()
        {
            if (!_rootData.ContainsKey("drop_tables"))
            {
                return;
            }

            Variant dropTablesVariant = _rootData["drop_tables"];
            if (dropTablesVariant.VariantType != Variant.Type.Array)
            {
                return;
            }

            var tables = (Godot.Collections.Array<Variant>)dropTablesVariant;
            foreach (Variant item in tables)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                string id = GetString(dict, "drop_table_id", "");
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                _dropTableById[id] = dict;
            }
        }

        private static bool TryGetChildDictionary(
            Godot.Collections.Dictionary<string, Variant> parent,
            string key,
            out Godot.Collections.Dictionary<string, Variant> child)
        {
            child = new Godot.Collections.Dictionary<string, Variant>();
            if (!parent.ContainsKey(key))
            {
                return false;
            }

            Variant value = parent[key];
            if (value.VariantType != Variant.Type.Dictionary)
            {
                return false;
            }

            child = (Godot.Collections.Dictionary<string, Variant>)value;
            return true;
        }

        private static string GetString(Godot.Collections.Dictionary<string, Variant> dict, string key, string fallback)
        {
            return dict.ContainsKey(key) ? dict[key].AsString() : fallback;
        }

        private static double GetDouble(Godot.Collections.Dictionary<string, Variant> dict, string key, double fallback)
        {
            return dict.ContainsKey(key) ? dict[key].AsDouble() : fallback;
        }

        private bool TryGetActiveLevel(out Godot.Collections.Dictionary<string, Variant> level)
        {
            level = new Godot.Collections.Dictionary<string, Variant>();
            if (_levels.Count == 0)
            {
                return false;
            }

            _activeLevelIndex = Math.Clamp(_activeLevelIndex, 0, _levels.Count - 1);
            level = _levels[_activeLevelIndex];
            return true;
        }

        private void ApplyActiveLevelData()
        {
            if (!TryGetActiveLevel(out var level))
            {
                ActiveLevelId = "";
                ActiveLevelName = "Unknown Zone";
                ProgressPer100Inputs = 2.0;
                EncounterCheckIntervalProgress = 20.0;
                BaseEncounterRate = 0.18;
                BattlePauseFactor = 0.0;
                PlayerBaseHp = 36;
                PlayerAttackPerRound = 4;
                EnemyDamageDivider = 4;
                EnemyMinDamagePerRound = 1;
                return;
            }

            ActiveLevelId = GetString(level, "level_id", "");
            ActiveLevelName = GetString(level, "level_name", "Unknown Zone");

            if (!TryGetChildDictionary(level, "explore", out var explore))
            {
                ProgressPer100Inputs = 2.0;
                EncounterCheckIntervalProgress = 20.0;
                BaseEncounterRate = 0.18;
                BattlePauseFactor = 0.0;
            }
            else
            {
                ProgressPer100Inputs = GetDouble(explore, "progress_per_100_inputs", 2.0);
                EncounterCheckIntervalProgress = GetDouble(explore, "encounter_check_interval_progress", 20.0);
                BaseEncounterRate = GetDouble(explore, "base_encounter_rate", 0.18);
                BattlePauseFactor = GetDouble(explore, "battle_pause_factor", 0.0);
            }

            if (!TryGetChildDictionary(level, "battle_runtime", out var battleRuntime))
            {
                PlayerBaseHp = 36;
                PlayerAttackPerRound = 4;
                EnemyDamageDivider = 4;
                EnemyMinDamagePerRound = 1;
                return;
            }

            PlayerBaseHp = Math.Max(1, battleRuntime.ContainsKey("player_base_hp") ? battleRuntime["player_base_hp"].AsInt32() : 36);
            PlayerAttackPerRound = Math.Max(1, battleRuntime.ContainsKey("player_attack_per_round") ? battleRuntime["player_attack_per_round"].AsInt32() : 4);
            EnemyDamageDivider = Math.Max(1, battleRuntime.ContainsKey("enemy_damage_divider") ? battleRuntime["enemy_damage_divider"].AsInt32() : 4);
            EnemyMinDamagePerRound = Math.Max(1, battleRuntime.ContainsKey("enemy_min_damage_per_round") ? battleRuntime["enemy_min_damage_per_round"].AsInt32() : 1);
        }
    }
}
