using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using Xiuxian.Scripts.Core;

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
        private readonly HashSet<string> _unlockedLevelIds = new();
        private readonly HashSet<string> _bossClearedLevelIds = new();
        private readonly List<string> _activeLevelMonsterWave = new();
        private readonly Dictionary<string, int> _activeMoveInputsByCategory = new();
        private int _activeLevelWaveIndex;
        private IConfigSource _configSource = new GodotConfigSource();
        private IRng _rng = new GodotRandomAdapter();
        private IClock _clock = new SystemClock();
        private readonly List<string> _validationIssues = new();
        private readonly List<Godot.Collections.Dictionary<string, Variant>> _validationEntries = new();
        private string _lastDropTableResolved = "";
        private bool _lastDailyCapBlocked;
        private bool _lastSoftCapSkipped;
        private bool _lastPityTriggered;
        private string _lastPityCounterKey = "";
        private int _lastPityCounterValue;
        private string _lastSimulationReport = "no simulation yet";
        public int ValidationIssueCount => _validationIssues.Count;

        public override void _Ready()
        {
            _rng.Randomize();
            LoadConfig();
        }

        public void UseTestSeams(IConfigSource configSource, IRng rng, IClock clock)
        {
            _configSource = configSource ?? throw new ArgumentNullException(nameof(configSource));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
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
            _bossClearedLevelIds.Clear();

            if (!_configSource.TryReadAllText(ConfigPath, out string text))
            {
                PushWarningIfRuntimeReady($"LevelConfigLoader: failed to open config at {ConfigPath}");
                return false;
            }

            Variant parsed = Json.ParseString(text);
            if (parsed.VariantType != Variant.Type.Dictionary)
            {
                PushWarningIfRuntimeReady("LevelConfigLoader: config is not a valid dictionary JSON.");
                return false;
            }

            _rootData = (Godot.Collections.Dictionary<string, Variant>)parsed;
            ParseLevelsSection();
            EnsureLevelUnlockBootstrap();
            IndexMonsters();
            IndexDropTables();
            ValidateConfiguration();

            EmitConfigLoadedIfRuntimeReady();
            PrintIfRuntimeReady($"LevelConfigLoader: loaded level '{ActiveLevelId}' ({ActiveLevelName})");
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
            EmitConfigLoadedIfRuntimeReady();
            return true;
        }

        public bool TryAdvanceToNextUnlockedLevel()
        {
            string next = GetNextUnlockedLevelId(ActiveLevelId);
            if (string.IsNullOrEmpty(next) || next == ActiveLevelId)
            {
                return false;
            }

            return TrySetActiveLevel(next);
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
                EmitConfigLoadedIfRuntimeReady();
                return true;
            }

            return false;
        }

        private void EmitConfigLoadedIfRuntimeReady()
        {
            if (!IsInsideTree())
            {
                return;
            }

            EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
        }

        private void PushWarningIfRuntimeReady(string message)
        {
            if (!IsInsideTree())
            {
                return;
            }

            GD.PushWarning(message);
        }

        private void PrintIfRuntimeReady(string message)
        {
            if (!IsInsideTree())
            {
                return;
            }

            GD.Print(message);
        }

        public bool TrySetActiveLevelIfUnlocked(string levelId)
        {
            if (!IsLevelUnlocked(levelId))
            {
                return false;
            }

            return TrySetActiveLevel(levelId);
        }

        public bool TrySetNextUnlockedLevelAsActive()
        {
            string next = GetNextUnlockedLevelId(ActiveLevelId);
            if (string.IsNullOrEmpty(next))
            {
                return false;
            }

            return TrySetActiveLevel(next);
        }

        internal sealed class SeamRuntime
        {
            private readonly IConfigSource _configSource;
            private readonly IRng _rng;
            private readonly IClock _clock;
            private Godot.Collections.Dictionary<string, Variant> _rootData = new();
            private readonly List<Godot.Collections.Dictionary<string, Variant>> _levels = new();
            private readonly Dictionary<string, Godot.Collections.Dictionary<string, Variant>> _monsterById = new();
            private readonly Dictionary<string, Godot.Collections.Dictionary<string, Variant>> _dropTableById = new();
            private readonly Dictionary<string, int> _pityCounterByKey = new();
            private readonly Dictionary<string, int> _dailyRollCountByTable = new();
            private readonly Dictionary<string, long> _dailyRollDayByTable = new();
            private readonly Dictionary<string, int> _hourlyRollCountByTable = new();
            private readonly Dictionary<string, long> _hourlyRollHourByTable = new();
            private readonly List<string> _validationIssues = new();
            private readonly List<Godot.Collections.Dictionary<string, Variant>> _validationEntries = new();
            private int _activeLevelIndex;

            public SeamRuntime(IConfigSource configSource, IRng rng, IClock clock)
            {
                _configSource = configSource ?? throw new ArgumentNullException(nameof(configSource));
                _rng = rng ?? throw new ArgumentNullException(nameof(rng));
                _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            }

            public string ActiveLevelId { get; private set; } = "";

            public string ActiveLevelName { get; private set; } = "Unknown Zone";

            public bool LoadConfig(string configPath)
            {
                _monsterById.Clear();
                _dropTableById.Clear();
                _pityCounterByKey.Clear();
                _dailyRollCountByTable.Clear();
                _dailyRollDayByTable.Clear();
                _hourlyRollCountByTable.Clear();
                _hourlyRollHourByTable.Clear();
                _validationIssues.Clear();
                _validationEntries.Clear();

                if (!_configSource.TryReadAllText(configPath, out string text))
                {
                    return false;
                }

                Variant parsed = Json.ParseString(text);
                if (parsed.VariantType != Variant.Type.Dictionary)
                {
                    return false;
                }

                _rootData = (Godot.Collections.Dictionary<string, Variant>)parsed;
                ParseLevelsSection();
                IndexMonsters();
                IndexDropTables();
                ValidateConfiguration();
                return _levels.Count > 0;
            }

            public Godot.Collections.Array<string> GetValidationIssues()
            {
                var result = new Godot.Collections.Array<string>();
                foreach (string issue in _validationIssues)
                {
                    result.Add(issue);
                }

                return result;
            }

            public Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> GetValidationEntries()
            {
                var result = new Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>>();
                foreach (var entry in _validationEntries)
                {
                    result.Add(new Godot.Collections.Dictionary<string, Variant>(entry));
                }

                return result;
            }

            public string BuildValidationSummary(int maxLines = 12)
            {
                if (_validationIssues.Count == 0)
                {
                    return "config validation: OK";
                }

                int lines = Math.Max(1, maxLines);
                var sb = new StringBuilder();
                sb.Append($"config validation: {_validationIssues.Count} issue(s)");

                int count = Math.Min(lines, _validationIssues.Count);
                for (int i = 0; i < count; i++)
                {
                    sb.Append($"\n- {_validationIssues[i]}");
                }

                if (_validationIssues.Count > count)
                {
                    sb.Append($"\n- ... and {_validationIssues.Count - count} more");
                }

                return sb.ToString();
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

                lingqi = _rng.NextInt(lingqiMin, lingqiMax);
                insight = _rng.NextInt(insightMin, insightMax);
                return true;
            }

            public Dictionary<string, int> RollMonsterDrops(string monsterId)
            {
                var result = new Dictionary<string, int>();
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
                int dropRollCount = Math.Max(0, drops.ContainsKey("drop_roll_count") ? drops["drop_roll_count"].AsInt32() : 1);
                string pityCounterKey = "";
                string pityItemId = "";
                int pityThreshold = 0;
                int pityQty = 0;

                if (!string.IsNullOrEmpty(dropTableId) && dropRollCount > 0 && TryGetDropTable(dropTableId, out var table))
                {
                    ReadPityConfig(table, out pityCounterKey, out pityThreshold, out pityItemId, out pityQty);
                    AddDropRollResults(table, dropTableId, dropRollCount, result);
                }

                ApplyPity(dropTableId, pityCounterKey, pityThreshold, pityItemId, pityQty, result);
                return result;
            }

            private void ParseLevelsSection()
            {
                _levels.Clear();
                _activeLevelIndex = 0;

                if (!_rootData.ContainsKey("levels") || _rootData["levels"].VariantType != Variant.Type.Array)
                {
                    return;
                }

                var array = (Godot.Collections.Array<Variant>)_rootData["levels"];
                foreach (Variant item in array)
                {
                    if (item.VariantType == Variant.Type.Dictionary)
                    {
                        _levels.Add((Godot.Collections.Dictionary<string, Variant>)item);
                    }
                }

                ApplyActiveLevelData();
            }

            private void ApplyActiveLevelData()
            {
                if (_levels.Count == 0)
                {
                    ActiveLevelId = "";
                    ActiveLevelName = "Unknown Zone";
                    return;
                }

                _activeLevelIndex = Math.Clamp(_activeLevelIndex, 0, _levels.Count - 1);
                var level = _levels[_activeLevelIndex];
                ActiveLevelId = GetString(level, "level_id", "");
                ActiveLevelName = GetString(level, "level_name", "Unknown Zone");
            }

            private void IndexMonsters()
            {
                _monsterById.Clear();
                if (!_rootData.ContainsKey("monsters") || _rootData["monsters"].VariantType != Variant.Type.Array)
                {
                    return;
                }

                var array = (Godot.Collections.Array<Variant>)_rootData["monsters"];
                foreach (Variant item in array)
                {
                    if (item.VariantType != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                    string monsterId = GetString(dict, "monster_id", "");
                    if (!string.IsNullOrEmpty(monsterId))
                    {
                        _monsterById[monsterId] = dict;
                    }
                }
            }

            private void IndexDropTables()
            {
                _dropTableById.Clear();
                if (!_rootData.ContainsKey("drop_tables") || _rootData["drop_tables"].VariantType != Variant.Type.Array)
                {
                    return;
                }

                var array = (Godot.Collections.Array<Variant>)_rootData["drop_tables"];
                foreach (Variant item in array)
                {
                    if (item.VariantType != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                    string tableId = GetString(dict, "drop_table_id", "");
                    if (!string.IsNullOrEmpty(tableId))
                    {
                        _dropTableById[tableId] = dict;
                    }
                }
            }

            private bool TryGetMonster(string monsterId, out Godot.Collections.Dictionary<string, Variant> monsterData)
            {
                if (_monsterById.TryGetValue(monsterId, out monsterData))
                {
                    return true;
                }

                monsterData = new Godot.Collections.Dictionary<string, Variant>();
                return false;
            }

            private bool TryGetDropTable(string dropTableId, out Godot.Collections.Dictionary<string, Variant> dropTableData)
            {
                if (_dropTableById.TryGetValue(dropTableId, out dropTableData))
                {
                    return true;
                }

                dropTableData = new Godot.Collections.Dictionary<string, Variant>();
                return false;
            }

            private void AddDropRollResults(
                Godot.Collections.Dictionary<string, Variant> table,
                string dropTableId,
                int rollCount,
                Dictionary<string, int> result)
            {
                if (!table.ContainsKey("entries") || table["entries"].VariantType != Variant.Type.Array)
                {
                    return;
                }

                var entries = (Godot.Collections.Array<Variant>)table["entries"];
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
                    int qty = _rng.NextInt(minQty, maxQty);
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
                    return;
                }

                int next = (_pityCounterByKey.TryGetValue(pityCounterKey, out int current) ? current : 0) + 1;
                if (next >= pityThreshold)
                {
                    AddDrop(result, pityItemId, Math.Max(1, pityQty));
                    _pityCounterByKey[pityCounterKey] = 0;
                    return;
                }

                _pityCounterByKey[pityCounterKey] = next;
            }

            private bool TryConsumeDropRoll(
                Godot.Collections.Dictionary<string, Variant> table,
                string dropTableId,
                out int hourlyCountAfterConsume)
            {
                hourlyCountAfterConsume = 0;
                long unix = _clock.GetUnixTimeSeconds();
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
                return _rng.NextSingle() > allowChance;
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

                int roll = _rng.NextInt(1, totalWeight);
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

            private void ValidateConfiguration()
            {
                _validationIssues.Clear();
                _validationEntries.Clear();

                if (_levels.Count == 0)
                {
                    AddValidationIssue(
                        scope: "config",
                        id: "levels",
                        field: "levels[]",
                        message: "is empty");
                    return;
                }

                var levelIds = new HashSet<string>();
                foreach (var level in _levels)
                {
                    string levelId = GetString(level, "level_id", "");
                    if (string.IsNullOrEmpty(levelId))
                    {
                        AddValidationIssue(
                            scope: "level",
                            id: "(unknown-level)",
                            field: "level_id",
                            message: "missing");
                    }
                    else
                    {
                        levelIds.Add(levelId);
                    }

                    ValidateLevelSpawnTable(level, levelId);
                }

                ValidateMonsters();
                ValidateDropTables(levelIds);
            }

            private void ValidateLevelSpawnTable(Godot.Collections.Dictionary<string, Variant> level, string levelId)
            {
                string id = string.IsNullOrEmpty(levelId) ? "(unknown-level)" : levelId;
                if (!level.ContainsKey("spawn_table") || level["spawn_table"].VariantType != Variant.Type.Array)
                {
                    AddValidationIssue(
                        scope: "level",
                        id: id,
                        field: "spawn_table",
                        message: "missing or not array",
                        levelId: id);
                    return;
                }

                var spawnTable = (Godot.Collections.Array<Variant>)level["spawn_table"];
                if (spawnTable.Count == 0)
                {
                    AddValidationIssue(
                        scope: "level",
                        id: id,
                        field: "spawn_table",
                        message: "is empty",
                        levelId: id);
                    return;
                }

                int totalWeight = 0;
                for (int i = 0; i < spawnTable.Count; i++)
                {
                    Variant item = spawnTable[i];
                    if (item.VariantType != Variant.Type.Dictionary)
                    {
                        AddValidationIssue(
                            scope: "level",
                            id: id,
                            field: $"spawn_table[{i}]",
                            message: "is not dictionary",
                            levelId: id);
                        continue;
                    }

                    var entry = (Godot.Collections.Dictionary<string, Variant>)item;
                    string monsterId = GetString(entry, "monster_id", "");
                    int weight = Math.Max(0, entry.ContainsKey("weight") ? entry["weight"].AsInt32() : 0);
                    totalWeight += weight;

                    if (string.IsNullOrEmpty(monsterId))
                    {
                        AddValidationIssue(
                            scope: "level",
                            id: id,
                            field: $"spawn_table[{i}].monster_id",
                            message: "missing",
                            levelId: id);
                    }
                    else if (!_monsterById.ContainsKey(monsterId))
                    {
                        AddValidationIssue(
                            scope: "level",
                            id: id,
                            field: $"spawn_table[{i}].monster_id",
                            message: $"spawn monster '{monsterId}' not found in monsters[]",
                            levelId: id,
                            monsterId: monsterId);
                    }

                    if (weight <= 0)
                    {
                        AddValidationIssue(
                            scope: "level",
                            id: id,
                            field: $"spawn_table[{i}].weight",
                            message: "weight <= 0",
                            levelId: id,
                            monsterId: monsterId);
                    }
                }

                if (totalWeight <= 0)
                {
                    AddValidationIssue(
                        scope: "level",
                        id: id,
                        field: "spawn_table.total_weight",
                        message: "total weight <= 0",
                        levelId: id);
                }
                else if (totalWeight != 100)
                {
                    AddValidationIssue(
                        scope: "level",
                        id: id,
                        field: "spawn_table.total_weight",
                        message: $"total weight = {totalWeight} (expected 100)",
                        levelId: id);
                }
            }

            private void ValidateMonsters()
            {
                foreach (var kv in _monsterById)
                {
                    string monsterId = kv.Key;
                    var monster = kv.Value;
                    if (!TryGetChildDictionary(monster, "drops", out var drops))
                    {
                        AddValidationIssue(
                            scope: "monster",
                            id: monsterId,
                            field: "drops",
                            message: "section missing",
                            monsterId: monsterId);
                        continue;
                    }

                    string tableId = GetString(drops, "drop_table_id", "");
                    if (string.IsNullOrEmpty(tableId))
                    {
                        AddValidationIssue(
                            scope: "monster",
                            id: monsterId,
                            field: "drops.drop_table_id",
                            message: "missing",
                            monsterId: monsterId);
                        continue;
                    }

                    if (!_dropTableById.ContainsKey(tableId))
                    {
                        AddValidationIssue(
                            scope: "monster",
                            id: monsterId,
                            field: "drops.drop_table_id",
                            message: $"drop table '{tableId}' not found",
                            monsterId: monsterId,
                            dropTableId: tableId);
                    }
                }
            }

            private void ValidateDropTables(HashSet<string> levelIds)
            {
                if (_dropTableById.Count == 0)
                {
                    AddValidationIssue(
                        scope: "config",
                        id: "drop_tables",
                        field: "drop_tables[]",
                        message: "is empty");
                    return;
                }

                foreach (var kv in _dropTableById)
                {
                    string tableId = kv.Key;
                    var table = kv.Value;

                    string bindLevelId = GetString(table, "bind_level_id", "");
                    if (string.IsNullOrEmpty(bindLevelId))
                    {
                        AddValidationIssue(
                            scope: "drop_table",
                            id: tableId,
                            field: "bind_level_id",
                            message: "missing",
                            dropTableId: tableId);
                    }
                    else if (!levelIds.Contains(bindLevelId))
                    {
                        AddValidationIssue(
                            scope: "drop_table",
                            id: tableId,
                            field: "bind_level_id",
                            message: $"'{bindLevelId}' not found in levels[]",
                            levelId: bindLevelId,
                            dropTableId: tableId);
                    }

                    if (!table.ContainsKey("bind_monster_ids") || table["bind_monster_ids"].VariantType != Variant.Type.Array)
                    {
                        AddValidationIssue(
                            scope: "drop_table",
                            id: tableId,
                            field: "bind_monster_ids",
                            message: "missing or not array",
                            dropTableId: tableId);
                    }
                    else
                    {
                        var boundMonsters = (Godot.Collections.Array<Variant>)table["bind_monster_ids"];
                        if (boundMonsters.Count == 0)
                        {
                            AddValidationIssue(
                                scope: "drop_table",
                                id: tableId,
                                field: "bind_monster_ids",
                                message: "is empty",
                                dropTableId: tableId);
                        }

                        for (int i = 0; i < boundMonsters.Count; i++)
                        {
                            string monsterId = boundMonsters[i].AsString();
                            if (string.IsNullOrEmpty(monsterId))
                            {
                                AddValidationIssue(
                                    scope: "drop_table",
                                    id: tableId,
                                    field: $"bind_monster_ids[{i}]",
                                    message: "is empty",
                                    dropTableId: tableId);
                                continue;
                            }

                            if (!_monsterById.ContainsKey(monsterId))
                            {
                                AddValidationIssue(
                                    scope: "drop_table",
                                    id: tableId,
                                    field: $"bind_monster_ids[{i}]",
                                    message: $"bound monster '{monsterId}' not found",
                                    monsterId: monsterId,
                                    dropTableId: tableId);
                            }
                        }
                    }

                    if (!table.ContainsKey("entries") || table["entries"].VariantType != Variant.Type.Array)
                    {
                        AddValidationIssue(
                            scope: "drop_table",
                            id: tableId,
                            field: "entries",
                            message: "missing or not array",
                            dropTableId: tableId);
                        continue;
                    }

                    var entries = (Godot.Collections.Array<Variant>)table["entries"];
                    if (entries.Count == 0)
                    {
                        AddValidationIssue(
                            scope: "drop_table",
                            id: tableId,
                            field: "entries",
                            message: "is empty",
                            dropTableId: tableId);
                        continue;
                    }

                    int totalWeight = 0;
                    for (int i = 0; i < entries.Count; i++)
                    {
                        Variant item = entries[i];
                        if (item.VariantType != Variant.Type.Dictionary)
                        {
                            AddValidationIssue(
                                scope: "drop_table",
                                id: tableId,
                                field: $"entries[{i}]",
                                message: "is not dictionary",
                                dropTableId: tableId);
                            continue;
                        }

                        var entry = (Godot.Collections.Dictionary<string, Variant>)item;
                        string itemId = GetString(entry, "item_id", "");
                        int weight = Math.Max(0, entry.ContainsKey("weight") ? entry["weight"].AsInt32() : 0);
                        int qtyMin = Math.Max(0, entry.ContainsKey("qty_min") ? entry["qty_min"].AsInt32() : 0);
                        int qtyMax = Math.Max(0, entry.ContainsKey("qty_max") ? entry["qty_max"].AsInt32() : 0);
                        totalWeight += weight;

                        if (string.IsNullOrEmpty(itemId))
                        {
                            AddValidationIssue(
                                scope: "drop_table",
                                id: tableId,
                                field: $"entries[{i}].item_id",
                                message: "missing",
                                dropTableId: tableId);
                        }
                        if (weight <= 0)
                        {
                            AddValidationIssue(
                                scope: "drop_table",
                                id: tableId,
                                field: $"entries[{i}].weight",
                                message: "weight <= 0",
                                dropTableId: tableId);
                        }
                        if (qtyMax < qtyMin)
                        {
                            AddValidationIssue(
                                scope: "drop_table",
                                id: tableId,
                                field: $"entries[{i}].qty_max",
                                message: "qty_max < qty_min",
                                dropTableId: tableId);
                        }
                    }

                    if (totalWeight <= 0)
                    {
                        AddValidationIssue(
                            scope: "drop_table",
                            id: tableId,
                            field: "entries.total_weight",
                            message: "total weight <= 0",
                            dropTableId: tableId);
                    }
                    else if (totalWeight != 100)
                    {
                        AddValidationIssue(
                            scope: "drop_table",
                            id: tableId,
                            field: "entries.total_weight",
                            message: $"total weight = {totalWeight} (expected 100)",
                            dropTableId: tableId);
                    }
                }
            }

            private void AddValidationIssue(
                string scope,
                string id,
                string field,
                string message,
                string severity = "error",
                string levelId = "",
                string monsterId = "",
                string dropTableId = "")
            {
                string normalizedScope = string.IsNullOrEmpty(scope) ? "config" : scope;
                string normalizedId = string.IsNullOrEmpty(id) ? "(unknown)" : id;
                string normalizedField = string.IsNullOrEmpty(field) ? "(unknown)" : field;
                string normalizedSeverity = string.IsNullOrEmpty(severity) ? "error" : severity;
                string normalizedMessage = string.IsNullOrEmpty(message) ? "validation failed" : message;

                var entry = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["scope"] = normalizedScope,
                    ["id"] = normalizedId,
                    ["field"] = normalizedField,
                    ["severity"] = normalizedSeverity,
                    ["message"] = normalizedMessage,
                    ["level_id"] = levelId,
                    ["monster_id"] = monsterId,
                    ["drop_table_id"] = dropTableId
                };

                _validationEntries.Add(entry);
                _validationIssues.Add(BuildValidationIssueMessage(entry));
            }

            private static string BuildValidationIssueMessage(Godot.Collections.Dictionary<string, Variant> entry)
            {
                string scope = entry.ContainsKey("scope") ? entry["scope"].AsString() : "config";
                string id = entry.ContainsKey("id") ? entry["id"].AsString() : "(unknown)";
                string field = entry.ContainsKey("field") ? entry["field"].AsString() : "(unknown)";
                string message = entry.ContainsKey("message") ? entry["message"].AsString() : "validation failed";
                return $"{scope} {id}: {field} {message}.";
            }
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
            if (_activeLevelMonsterWave.Count > 0)
            {
                _activeLevelWaveIndex = Math.Clamp(_activeLevelWaveIndex, 0, _activeLevelMonsterWave.Count - 1);
                string waveMonsterId = _activeLevelMonsterWave[_activeLevelWaveIndex];
                _activeLevelWaveIndex = (_activeLevelWaveIndex + 1) % _activeLevelMonsterWave.Count;
                return waveMonsterId;
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

            int roll = _rng.NextInt(1, totalWeight);
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

        public bool TryGetMonsterVisualConfig(
            string monsterId,
            out string portraitPath,
            out string animationType,
            out double animationSpeed,
            out double animationAmplitude,
            out Color tint)
        {
            portraitPath = "";
            animationType = "none";
            animationSpeed = 0.0;
            animationAmplitude = 0.0;
            tint = Colors.White;

            if (!TryGetMonster(monsterId, out var monster))
            {
                return false;
            }

            if (!TryGetChildDictionary(monster, "visual", out var visual))
            {
                return true;
            }

            portraitPath = GetString(visual, "portrait", "");
            animationType = GetString(visual, "animation", "none");
            animationSpeed = GetDouble(visual, "anim_speed", 0.0);
            animationAmplitude = GetDouble(visual, "anim_amplitude", 0.0);

            if (visual.ContainsKey("tint"))
            {
                tint = ParseColorVariant(visual["tint"], tint);
            }

            return true;
        }

        public bool TryGetMonsterMoveRule(
            string monsterId,
            out string moveCategory,
            out int inputsPerMove)
        {
            moveCategory = "normal";
            inputsPerMove = 4;

            if (!TryGetMonster(monsterId, out var monster))
            {
                return false;
            }

            moveCategory = GetString(monster, "move_category", "");
            if (string.IsNullOrEmpty(moveCategory))
            {
                moveCategory = GetString(monster, "rarity", "normal");
            }

            if (_activeMoveInputsByCategory.TryGetValue(moveCategory, out int configured))
            {
                inputsPerMove = Math.Max(1, configured);
            }
            else if (_activeMoveInputsByCategory.TryGetValue("default", out int fallback))
            {
                inputsPerMove = Math.Max(1, fallback);
            }

            return true;
        }

        public bool TryGetActiveWaveProgress(
            out int nextSpawnIndex,
            out int waveCount,
            out string nextMonsterId)
        {
            nextSpawnIndex = 0;
            waveCount = _activeLevelMonsterWave.Count;
            nextMonsterId = "";
            if (waveCount <= 0)
            {
                return false;
            }

            int index = _activeLevelWaveIndex;
            if (index < 0 || index >= waveCount)
            {
                index = 0;
            }

            nextSpawnIndex = index + 1;
            nextMonsterId = _activeLevelMonsterWave[index];
            return true;
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

        private static Color ParseColorVariant(Variant value, Color fallback)
        {
            if (value.VariantType == Variant.Type.Array)
            {
                var arr = (Godot.Collections.Array<Variant>)value;
                if (arr.Count >= 3)
                {
                    float r = (float)arr[0].AsDouble();
                    float g = (float)arr[1].AsDouble();
                    float b = (float)arr[2].AsDouble();
                    float a = arr.Count >= 4 ? (float)arr[3].AsDouble() : 1.0f;
                    return new Color(r, g, b, a);
                }
            }

            if (value.VariantType == Variant.Type.String)
            {
                string html = value.AsString();
                if (!string.IsNullOrEmpty(html))
                {
                    try
                    {
                        return Color.FromHtml(html);
                    }
                    catch (Exception)
                    {
                        return fallback;
                    }
                }
            }

            return fallback;
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
                _activeLevelMonsterWave.Clear();
                _activeMoveInputsByCategory.Clear();
                _activeLevelWaveIndex = 0;
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
                _activeMoveInputsByCategory.Clear();
                _activeMoveInputsByCategory["default"] = 4;
            }
            else
            {
                ProgressPer100Inputs = GetDouble(explore, "progress_per_100_inputs", 2.0);
                EncounterCheckIntervalProgress = GetDouble(explore, "encounter_check_interval_progress", 20.0);
                BaseEncounterRate = GetDouble(explore, "base_encounter_rate", 0.18);
                BattlePauseFactor = GetDouble(explore, "battle_pause_factor", 0.0);
                _activeMoveInputsByCategory.Clear();
                _activeMoveInputsByCategory["default"] = 4;
                if (explore.ContainsKey("move_inputs_by_category") &&
                    explore["move_inputs_by_category"].VariantType == Variant.Type.Dictionary)
                {
                    var moveMap = (Godot.Collections.Dictionary<string, Variant>)explore["move_inputs_by_category"];
                    foreach (string key in moveMap.Keys)
                    {
                        int value = Math.Max(1, moveMap[key].AsInt32());
                        _activeMoveInputsByCategory[key] = value;
                    }
                }
            }

            _activeLevelMonsterWave.Clear();
            _activeLevelWaveIndex = 0;
            if (level.ContainsKey("monster_wave") && level["monster_wave"].VariantType == Variant.Type.Array)
            {
                var wave = (Godot.Collections.Array<Variant>)level["monster_wave"];
                foreach (Variant item in wave)
                {
                    string id = item.AsString();
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }
                    _activeLevelMonsterWave.Add(id);
                }
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
