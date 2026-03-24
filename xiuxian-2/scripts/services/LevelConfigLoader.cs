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
        private readonly Dictionary<string, Godot.Collections.Dictionary<string, Variant>> _equipmentSeriesById = new();
        private readonly Dictionary<string, Godot.Collections.Dictionary<string, Variant>> _equipmentTemplateById = new();
        private readonly Dictionary<string, List<Godot.Collections.Dictionary<string, Variant>>> _equipmentExchangeRecipesByLevelId = new();
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
        private readonly RandomNumberGenerator _rng = new();
        private readonly List<string> _validationIssues = new();
        private readonly List<Godot.Collections.Dictionary<string, Variant>> _validationEntries = new();
        private readonly List<string> _lastEquipmentDropTemplateIds = new();
        private readonly List<EquipmentInstanceData> _lastGeneratedEquipmentDrops = new();
        private readonly List<EquipmentInstanceData> _lastGeneratedFirstClearEquipmentRewards = new();
        private string _lastDropTableResolved = "";
        private string _lastLoadedConfigText = "";
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

        public bool LoadConfig()
        {
            _monsterById.Clear();
            _dropTableById.Clear();
            _equipmentSeriesById.Clear();
            _equipmentTemplateById.Clear();
            _equipmentExchangeRecipesByLevelId.Clear();
            _levelClearCountById.Clear();
            _pityCounterByKey.Clear();
            _dailyRollCountByTable.Clear();
            _dailyRollDayByTable.Clear();
            _hourlyRollCountByTable.Clear();
            _hourlyRollHourByTable.Clear();
            _bossClearedLevelIds.Clear();

            using FileAccess? file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PushWarning($"LevelConfigLoader: failed to open config at {ConfigPath}");
                return false;
            }

            return LoadConfigFromText(file.GetAsText());
        }

        public bool LoadConfigFromText(string text)
        {
            _monsterById.Clear();
            _dropTableById.Clear();
            _equipmentSeriesById.Clear();
            _equipmentTemplateById.Clear();
            _equipmentExchangeRecipesByLevelId.Clear();
            _levelClearCountById.Clear();
            _pityCounterByKey.Clear();
            _dailyRollCountByTable.Clear();
            _dailyRollDayByTable.Clear();
            _hourlyRollCountByTable.Clear();
            _hourlyRollHourByTable.Clear();
            _bossClearedLevelIds.Clear();

            Variant parsed = Json.ParseString(text);
            if (parsed.VariantType != Variant.Type.Dictionary)
            {
                GD.PushWarning("LevelConfigLoader: config is not a valid dictionary JSON.");
                return false;
            }

            _rootData = (Godot.Collections.Dictionary<string, Variant>)parsed;
            _lastLoadedConfigText = text;
            ParseLevelsSection();
            EnsureLevelUnlockBootstrap();
            IndexMonsters();
            IndexDropTables();
            IndexEquipmentSeries();
            IndexEquipmentTemplates();
            IndexEquipmentExchangeRecipes();
            ValidateConfiguration();

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
                EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
                return true;
            }

            return false;
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

            if (!TryGetMonsterStatProfile(monsterId, out MonsterStatProfile profile))
            {
                return false;
            }

            monsterName = profile.DisplayName;
            hp = profile.BaseStats.MaxHp;
            inputsPerRound = profile.InputsPerRound;
            attack = profile.BaseStats.Attack;
            return true;
        }

        public bool TryGetMonsterStatProfile(string monsterId, out MonsterStatProfile profile)
        {
            profile = new MonsterStatProfile(
                monsterId,
                "Enemy",
                new CharacterStatBlock(24, 4, 0, 100, 0.0, 1.5),
                InputsPerRound: 18,
                MoveCategory: "normal",
                IsBoss: false);

            if (!TryGetMonster(monsterId, out var monster))
            {
                return false;
            }

            string monsterName = GetString(monster, "monster_name", profile.DisplayName);
            string moveCategory = GetString(monster, "move_category", "");
            if (string.IsNullOrEmpty(moveCategory))
            {
                moveCategory = GetString(monster, "rarity", "normal");
            }

            int hp = profile.BaseStats.MaxHp;
            int attack = profile.BaseStats.Attack;
            int defense = profile.BaseStats.Defense;
            double speedFactor = 1.0;
            int inputsPerRound = profile.InputsPerRound;
            if (TryGetChildDictionary(monster, "combat", out var combat))
            {
                hp = Math.Max(1, combat.ContainsKey("hp") ? combat["hp"].AsInt32() : hp);
                attack = Math.Max(1, combat.ContainsKey("attack") ? combat["attack"].AsInt32() : attack);
                defense = Math.Max(0, combat.ContainsKey("defense") ? combat["defense"].AsInt32() : defense);
                speedFactor = combat.ContainsKey("speed_factor") ? combat["speed_factor"].AsDouble() : speedFactor;
                inputsPerRound = Math.Max(1, combat.ContainsKey("inputs_per_round") ? combat["inputs_per_round"].AsInt32() : inputsPerRound);
            }

            string activeLevelId = ActiveLevelId;
            bool isBoss = !string.IsNullOrEmpty(activeLevelId) && IsBossMonsterForLevel(activeLevelId, monsterId);
            profile = MonsterStatRules.BuildProfile(monsterId, monsterName, hp, attack, defense, speedFactor, inputsPerRound, moveCategory, isBoss);
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

        public Dictionary<string, int> RollMonsterDrops(string monsterId)
        {
            var result = new Dictionary<string, int>();
            ResetLastDropDebug();
            _lastEquipmentDropTemplateIds.Clear();
            _lastGeneratedEquipmentDrops.Clear();
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
            GenerateLastEquipmentDropInstances();
            return result;
        }

        public Godot.Collections.Array<string> GetLastEquipmentDropTemplateIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (string templateId in _lastEquipmentDropTemplateIds)
            {
                result.Add(templateId);
            }

            return result;
        }

        public EquipmentInstanceData[] GetLastGeneratedEquipmentDrops()
        {
            return _lastGeneratedEquipmentDrops.ToArray();
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

            (int rolledLingqi, int rolledInsight) = BattleSettlementRules.RollReward(
                lingqiMin,
                lingqiMax,
                insightMin,
                insightMax,
                (min, max) => _rng.RandiRange(min, max));
            lingqi = rolledLingqi;
            insight = rolledInsight;
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
            _lastGeneratedFirstClearEquipmentRewards.Clear();

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

                GenerateFirstClearEquipmentRewards(first);
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

        public EquipmentInstanceData[] GetLastGeneratedFirstClearEquipmentRewards()
        {
            return _lastGeneratedFirstClearEquipmentRewards.ToArray();
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

            sb.Append($"\nValidation issues={_validationIssues.Count}");
            if (_validationIssues.Count > 0)
            {
                int max = Math.Min(3, _validationIssues.Count);
                for (int i = 0; i < max; i++)
                {
                    sb.Append($"\n! {_validationIssues[i]}");
                }
            }

            sb.Append($"\nSim: {_lastSimulationReport}");

            return sb.ToString();
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

        public string BuildLevelPreviewSummary(int maxLines = 12)
        {
            EnsureLevelUnlockBootstrap();
            if (_levels.Count == 0)
            {
                return "Levels: none";
            }

            int max = Math.Max(1, maxLines);
            int shown = Math.Min(max, _levels.Count);
            var sb = new StringBuilder();
            sb.Append("Levels:");

            for (int i = 0; i < shown; i++)
            {
                var level = _levels[i];
                string levelId = GetString(level, "level_id", $"lv_{i + 1:000}");
                string levelName = GetString(level, "level_name", "Unknown Zone");
                string realm = GetString(level, "realm_recommend", "?");
                int danger = level.ContainsKey("danger_level") ? level["danger_level"].AsInt32() : 0;
                string boss = GetLevelBossMonsterId(level);
                bool unlocked = _unlockedLevelIds.Contains(levelId);
                bool active = levelId == ActiveLevelId;
                string flag = active ? "*" : (unlocked ? "O" : "X");

                sb.Append($"\n{flag} {levelId} {levelName} | rec={realm} | danger={danger}");
                if (!string.IsNullOrEmpty(boss))
                {
                    sb.Append($" | boss={boss}");
                }
            }

            if (_levels.Count > shown)
            {
                sb.Append($"\n... {_levels.Count - shown} more");
            }

            sb.Append("\nLegend: *=active, O=unlocked, X=locked");
            return sb.ToString();
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

        public string RunBattleSimulation(int battleCount, string forcedMonsterId = "")
        {
            return RunBattleSimulationFiltered(battleCount, "", forcedMonsterId);
        }

        public string RunBattleSimulationFiltered(int battleCount, string levelId = "", string forcedMonsterId = "")
        {
            int originalLevelIndex = _activeLevelIndex;
            bool switchedLevel = false;

            if (!string.IsNullOrEmpty(levelId) && TryFindLevelIndex(levelId, out int levelIndex))
            {
                _activeLevelIndex = levelIndex;
                ApplyActiveLevelData();
                switchedLevel = true;
            }

            string report = RunBattleSimulationCore(battleCount, forcedMonsterId);

            if (switchedLevel)
            {
                _activeLevelIndex = originalLevelIndex;
                ApplyActiveLevelData();
            }

            return report;
        }

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

        public Godot.Collections.Array<string> GetEquipmentSeriesIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (string id in _equipmentSeriesById.Keys)
            {
                result.Add(id);
            }
            return result;
        }

        public Godot.Collections.Array<string> GetEquipmentTemplateIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (string id in _equipmentTemplateById.Keys)
            {
                result.Add(id);
            }
            return result;
        }

        public Godot.Collections.Array<string> GetEquipmentExchangeLevelIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (string id in _equipmentExchangeRecipesByLevelId.Keys)
            {
                result.Add(id);
            }
            return result;
        }

        public bool TryGetEquipmentSeries(string seriesId, out Godot.Collections.Dictionary<string, Variant> seriesData)
        {
            if (_equipmentSeriesById.TryGetValue(seriesId, out seriesData))
            {
                seriesData = new Godot.Collections.Dictionary<string, Variant>(seriesData);
                return true;
            }

            seriesData = new Godot.Collections.Dictionary<string, Variant>();
            return false;
        }

        public bool TryGetEquipmentTemplate(string templateId, out Godot.Collections.Dictionary<string, Variant> templateData)
        {
            if (_equipmentTemplateById.TryGetValue(templateId, out templateData))
            {
                templateData = new Godot.Collections.Dictionary<string, Variant>(templateData);
                return true;
            }

            templateData = new Godot.Collections.Dictionary<string, Variant>();
            return false;
        }

        public Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> GetEquipmentExchangeRecipes(string levelId = "")
        {
            var result = new Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>>();
            string resolvedLevelId = string.IsNullOrEmpty(levelId) ? ActiveLevelId : levelId;
            if (string.IsNullOrEmpty(resolvedLevelId) || !_equipmentExchangeRecipesByLevelId.TryGetValue(resolvedLevelId, out List<Godot.Collections.Dictionary<string, Variant>> recipes))
            {
                return result;
            }

            foreach (var recipe in recipes)
            {
                result.Add(new Godot.Collections.Dictionary<string, Variant>(recipe));
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
            string next = BossUnlockRules.ResolveNextUnlockedLevelId(GetConfiguredNextLevelId(levelId), GetNextLevelId(levelId));

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

        public DungeonOfflineSettlementRules.WeightedMonsterProfile[] GetOfflineWeightedMonsters(string levelId = "")
        {
            List<DungeonOfflineProjectionRules.MonsterSettlementSpec> specs = BuildOfflineMonsterSettlementSpecs(levelId);
            return DungeonOfflineProjectionRules.BuildWeightedMonsters(specs);
        }

        public double GetOfflineAverageLingqiPerVictory(string levelId = "")
        {
            return DungeonOfflineProjectionRules.CalculateAverageLingqiPerVictory(BuildOfflineMonsterSettlementSpecs(levelId));
        }

        public double GetOfflineAverageInsightPerVictory(string levelId = "")
        {
            return DungeonOfflineProjectionRules.CalculateAverageInsightPerVictory(BuildOfflineMonsterSettlementSpecs(levelId));
        }

        public Dictionary<string, double> GetOfflineAverageItemDropsPerVictory(string levelId = "")
        {
            return DungeonOfflineProjectionRules.CalculateAverageItemDropsPerVictory(BuildOfflineMonsterSettlementSpecs(levelId));
        }

        public double GetOfflineAverageEquipmentDropsPerVictory(string levelId = "")
        {
            return DungeonOfflineProjectionRules.CalculateAverageEquipmentDropsPerVictory(BuildOfflineMonsterSettlementSpecs(levelId));
        }

        private string RunBattleSimulationCore(int battleCount, string forcedMonsterId = "")
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

        private List<DungeonOfflineProjectionRules.MonsterSettlementSpec> BuildOfflineMonsterSettlementSpecs(string levelId = "")
        {
            var result = new List<DungeonOfflineProjectionRules.MonsterSettlementSpec>();
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
            foreach (Variant item in spawnTable)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                string monsterId = GetString(dict, "monster_id", "");
                int weight = Math.Max(0, dict.ContainsKey("weight") ? dict["weight"].AsInt32() : 0);
                if (string.IsNullOrEmpty(monsterId) || weight <= 0 || !TryGetMonsterStatProfile(monsterId, out MonsterStatProfile monsterProfile))
                {
                    continue;
                }

                double averageLingqi = 0.0;
                double averageInsight = 0.0;
                if (TryGetMonster(monsterId, out var monster) && TryGetChildDictionary(monster, "settlement_reward", out var settlement))
                {
                    int lingqiMin = settlement.ContainsKey("lingqi_min") ? settlement["lingqi_min"].AsInt32() : 0;
                    int lingqiMax = settlement.ContainsKey("lingqi_max") ? settlement["lingqi_max"].AsInt32() : lingqiMin;
                    int insightMin = settlement.ContainsKey("insight_min") ? settlement["insight_min"].AsInt32() : 0;
                    int insightMax = settlement.ContainsKey("insight_max") ? settlement["insight_max"].AsInt32() : insightMin;
                    averageLingqi = (lingqiMin + lingqiMax) / 2.0;
                    averageInsight = (insightMin + insightMax) / 2.0;
                }

                result.Add(new DungeonOfflineProjectionRules.MonsterSettlementSpec(
                    weight,
                    monsterProfile,
                    averageLingqi,
                    averageInsight,
                    BuildOfflineAverageItemDrops(monsterId),
                    BuildOfflineAverageEquipmentDrops(monsterId)));
            }

            return result;
        }

        private Dictionary<string, double> BuildOfflineAverageItemDrops(string monsterId)
        {
            var result = new Dictionary<string, double>();
            if (!TryGetMonster(monsterId, out var monster) || !TryGetChildDictionary(monster, "drops", out var drops))
            {
                return result;
            }

            string configuredDropTableId = GetString(drops, "drop_table_id", "");
            string dropTableId = ResolveDropTableForActiveLevel(monsterId, configuredDropTableId);
            int dropRollCount = Math.Max(0, drops.ContainsKey("drop_roll_count") ? drops["drop_roll_count"].AsInt32() : 1);
            if (!string.IsNullOrEmpty(dropTableId) && TryGetDropTable(dropTableId, out var table) && table.ContainsKey("entries") && table["entries"].VariantType == Variant.Type.Array)
            {
                var entries = (Godot.Collections.Array<Variant>)table["entries"];
                List<EquipmentDropResolutionRules.DropEntrySpec> specs = BuildDropEntrySpecs(entries);
                int totalWeight = 0;
                for (int i = 0; i < specs.Count; i++)
                {
                    totalWeight += Math.Max(0, specs[i].Weight);
                }

                if (totalWeight > 0)
                {
                    for (int i = 0; i < specs.Count; i++)
                    {
                        EquipmentDropResolutionRules.DropEntrySpec spec = specs[i];
                        if (spec.EntryType != "item" || string.IsNullOrEmpty(spec.ItemId) || spec.Weight <= 0)
                        {
                            continue;
                        }

                        double avgQty = (spec.MinQty + spec.MaxQty) / 2.0;
                        double expected = dropRollCount * (spec.Weight / (double)totalWeight) * avgQty;
                        result[spec.ItemId] = result.TryGetValue(spec.ItemId, out double current) ? current + expected : expected;
                    }
                }
            }

            if (drops.ContainsKey("guaranteed_drop") && drops["guaranteed_drop"].VariantType == Variant.Type.Array)
            {
                var guaranteed = (Godot.Collections.Array<Variant>)drops["guaranteed_drop"];
                foreach (Variant item in guaranteed)
                {
                    if (item.VariantType != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                    string itemId = GetString(dict, "item_id", "");
                    int qty = Math.Max(0, dict.ContainsKey("qty") ? dict["qty"].AsInt32() : 0);
                    if (!string.IsNullOrEmpty(itemId) && qty > 0)
                    {
                        result[itemId] = result.TryGetValue(itemId, out double current) ? current + qty : qty;
                    }
                }
            }

            return result;
        }

        private double BuildOfflineAverageEquipmentDrops(string monsterId)
        {
            if (!TryGetMonster(monsterId, out var monster) || !TryGetChildDictionary(monster, "drops", out var drops))
            {
                return 0.0;
            }

            string configuredDropTableId = GetString(drops, "drop_table_id", "");
            string dropTableId = ResolveDropTableForActiveLevel(monsterId, configuredDropTableId);
            int dropRollCount = Math.Max(0, drops.ContainsKey("drop_roll_count") ? drops["drop_roll_count"].AsInt32() : 1);
            if (string.IsNullOrEmpty(dropTableId) || !TryGetDropTable(dropTableId, out var table) || !table.ContainsKey("entries") || table["entries"].VariantType != Variant.Type.Array)
            {
                return 0.0;
            }

            var entries = (Godot.Collections.Array<Variant>)table["entries"];
            List<EquipmentDropResolutionRules.DropEntrySpec> specs = BuildDropEntrySpecs(entries);
            int totalWeight = 0;
            int equipmentWeight = 0;
            for (int i = 0; i < specs.Count; i++)
            {
                totalWeight += Math.Max(0, specs[i].Weight);
                if (specs[i].EntryType == "equipment_template")
                {
                    equipmentWeight += Math.Max(0, specs[i].Weight);
                }
            }

            if (totalWeight <= 0 || equipmentWeight <= 0)
            {
                return 0.0;
            }

            return dropRollCount * (equipmentWeight / (double)totalWeight);
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
            return LevelUnlockRules.GetNextUnlockedLevelId(unlocked, levelId);
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
            List<EquipmentDropResolutionRules.DropEntrySpec> specs = BuildDropEntrySpecs(entries);
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

                EquipmentDropResolutionRules.DropEntrySpec picked = EquipmentDropResolutionRules.PickWeightedEntry(specs, totalWeight => _rng.RandiRange(1, totalWeight));
                if (picked.Weight <= 0)
                {
                    continue;
                }

                EquipmentDropResolutionRules.DropResolveResult resolved = EquipmentDropResolutionRules.ResolveEntry(
                    picked,
                    (minQty, maxQty) => _rng.RandiRange(minQty, Math.Max(minQty, maxQty)));

                if (!string.IsNullOrEmpty(resolved.ItemId))
                {
                    AddDrop(result, resolved.ItemId, resolved.Quantity);
                }
                else if (!string.IsNullOrEmpty(resolved.EquipmentTemplateId))
                {
                    _lastEquipmentDropTemplateIds.Add(resolved.EquipmentTemplateId);
                }
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
            int current = _pityCounterByKey.TryGetValue(pityCounterKey, out int saved) ? saved : 0;
            (int nextCounter, bool triggered, int addedQty) = LevelDropEconomyRules.ApplyPity(current, pityThreshold, hasPityItem, pityQty);
            if (triggered)
            {
                AddDrop(result, pityItemId, addedQty);
                _lastPityTriggered = true;
            }

            _pityCounterByKey[pityCounterKey] = nextCounter;
            _lastPityCounterKey = pityCounterKey;
            _lastPityCounterValue = nextCounter;
        }

        private bool TryConsumeDropRoll(
            Godot.Collections.Dictionary<string, Variant> table,
            string dropTableId,
            out int hourlyCountAfterConsume)
        {
            hourlyCountAfterConsume = 0;
            long unix = (long)Time.GetUnixTimeFromSystem();
            int dailyCap = ReadDailyCap(table);
            bool hasSavedDay = _dailyRollDayByTable.TryGetValue(dropTableId, out long savedDay);
            bool hasSavedHour = _hourlyRollHourByTable.TryGetValue(dropTableId, out long savedHour);
            int dailyCount = _dailyRollCountByTable.TryGetValue(dropTableId, out int d) ? d : 0;
            int hourlyCount = _hourlyRollCountByTable.TryGetValue(dropTableId, out int h) ? h : 0;

            var result = LevelDropEconomyRules.ConsumeDropRoll(
                dailyCount,
                savedDay,
                dailyCap,
                hourlyCount,
                savedHour,
                unix,
                hasSavedDay,
                hasSavedHour);

            _dailyRollCountByTable[dropTableId] = result.DailyCount;
            _dailyRollDayByTable[dropTableId] = result.DayIndex;
            _hourlyRollCountByTable[dropTableId] = result.HourlyCount;
            _hourlyRollHourByTable[dropTableId] = result.HourIndex;
            hourlyCountAfterConsume = result.HourlyCountAfterConsume;
            _lastDailyCapBlocked = result.DailyCapBlocked;
            return result.Allowed;
        }

        private bool ShouldSkipDropBySoftCap(Godot.Collections.Dictionary<string, Variant> table, int hourlyCountAfterConsume)
        {
            int softCap = ReadHourlySoftCap(table);
            if (softCap <= 0 || hourlyCountAfterConsume <= softCap)
            {
                return false;
            }

            double decay = ReadRepeatDecay(table);
            bool skipped = LevelDropEconomyRules.ShouldSkipDropBySoftCap(softCap, decay, hourlyCountAfterConsume, _rng.Randf());
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

        private static List<EquipmentDropResolutionRules.DropEntrySpec> BuildDropEntrySpecs(Godot.Collections.Array<Variant> entries)
        {
            var result = new List<EquipmentDropResolutionRules.DropEntrySpec>();
            foreach (Variant item in entries)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                string entryType = GetString(dict, "entry_type", "item");
                string itemId = GetString(dict, "item_id", "");
                string equipmentTemplateId = GetString(dict, "equipment_template_id", "");
                int weight = Math.Max(0, dict.ContainsKey("weight") ? dict["weight"].AsInt32() : 0);
                int minQty = Math.Max(0, dict.ContainsKey("min_qty") ? dict["min_qty"].AsInt32() : 1);
                int maxQty = Math.Max(minQty, dict.ContainsKey("max_qty") ? dict["max_qty"].AsInt32() : minQty);

                result.Add(new EquipmentDropResolutionRules.DropEntrySpec(
                    entryType,
                    itemId,
                    equipmentTemplateId,
                    weight,
                    minQty,
                    maxQty));
            }

            return result;
        }

        private void GenerateLastEquipmentDropInstances()
        {
            _lastGeneratedEquipmentDrops.Clear();
            if (_lastEquipmentDropTemplateIds.Count == 0)
            {
                return;
            }

            var specsByTemplateId = new Dictionary<string, EquipmentGenerationRules.EquipmentTemplateGenerationSpec>();
            foreach (string templateId in _lastEquipmentDropTemplateIds)
            {
                if (specsByTemplateId.ContainsKey(templateId))
                {
                    continue;
                }

                if (_equipmentTemplateById.TryGetValue(templateId, out var templateDict))
                {
                    specsByTemplateId[templateId] = EquipmentGenerationRules.FromTemplateDictionary(templateDict);
                }
            }

            EquipmentInstanceData[] generated = EquipmentDropInstanceGenerationRules.GenerateInstances(
                specsByTemplateId,
                _lastEquipmentDropTemplateIds,
                ActiveLevelId,
                EquipmentSourceStage.Normal,
                totalWeight => _rng.RandiRange(1, totalWeight),
                (min, max) => min == max ? min : _rng.RandfRange((float)min, (float)max),
                () => (long)Time.GetUnixTimeFromSystem());

            _lastGeneratedEquipmentDrops.AddRange(generated);
        }

        private void GenerateFirstClearEquipmentRewards(Godot.Collections.Dictionary<string, Variant> firstClearRewards)
        {
            _lastGeneratedFirstClearEquipmentRewards.Clear();
            if (!firstClearRewards.ContainsKey("equipment_templates") || firstClearRewards["equipment_templates"].VariantType != Variant.Type.Array)
            {
                return;
            }

            var rewardSpecs = new List<FirstClearEquipmentRewardRules.FirstClearEquipmentRewardSpec>();
            var equipmentTemplates = (Godot.Collections.Array<Variant>)firstClearRewards["equipment_templates"];
            foreach (Variant item in equipmentTemplates)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                string templateId = GetString(dict, "equipment_template_id", "");
                string rarityText = GetString(dict, "rarity_tier", "");
                int qty = Math.Max(0, dict.ContainsKey("qty") ? dict["qty"].AsInt32() : 1);
                EquipmentRarityTier? rarityOverride = ParseOptionalRarity(rarityText);
                rewardSpecs.Add(new FirstClearEquipmentRewardRules.FirstClearEquipmentRewardSpec(templateId, rarityOverride, qty));
            }

            var specsByTemplateId = new Dictionary<string, EquipmentGenerationRules.EquipmentTemplateGenerationSpec>();
            foreach (FirstClearEquipmentRewardRules.FirstClearEquipmentRewardSpec reward in rewardSpecs)
            {
                if (string.IsNullOrEmpty(reward.EquipmentTemplateId) || specsByTemplateId.ContainsKey(reward.EquipmentTemplateId))
                {
                    continue;
                }

                if (_equipmentTemplateById.TryGetValue(reward.EquipmentTemplateId, out var templateDict))
                {
                    specsByTemplateId[reward.EquipmentTemplateId] = EquipmentGenerationRules.FromTemplateDictionary(templateDict);
                }
            }

            EquipmentInstanceData[] generated = FirstClearEquipmentRewardRules.GenerateInstances(
                specsByTemplateId,
                rewardSpecs,
                ActiveLevelId,
                totalWeight => _rng.RandiRange(1, totalWeight),
                (min, max) => min == max ? min : _rng.RandfRange((float)min, (float)max),
                () => (long)Time.GetUnixTimeFromSystem());

            _lastGeneratedFirstClearEquipmentRewards.AddRange(generated);
        }

        private static EquipmentRarityTier? ParseOptionalRarity(string rarity)
        {
            return rarity switch
            {
                "common_tool" => EquipmentRarityTier.CommonTool,
                "artifact" => EquipmentRarityTier.Artifact,
                "spirit" => EquipmentRarityTier.Spirit,
                "treasure" => EquipmentRarityTier.Treasure,
                _ => null,
            };
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
            bool configuredTableValidForLevel = !string.IsNullOrEmpty(configuredDropTableId)
                && TryGetDropTable(configuredDropTableId, out var configuredTable)
                && IsTableBoundToLevel(configuredTable, levelId);

            var candidates = new List<(string DropTableId, bool LevelMatch, bool MonsterMatch)>();
            foreach (var kv in _dropTableById)
            {
                var table = kv.Value;
                candidates.Add((
                    kv.Key,
                    IsTableBoundToLevel(table, levelId),
                    IsTableBoundToMonster(table, monsterId)));
            }

            return DropTableBindingRules.ResolveDropTableForActiveLevel(levelId, monsterId, configuredDropTableId, configuredTableValidForLevel, candidates);
        }

        private static bool IsTableBoundToLevel(Godot.Collections.Dictionary<string, Variant> table, string levelId)
        {
            string boundLevelId = GetString(table, "bind_level_id", "");
            return DropTableBindingRules.IsTableBoundToLevel(boundLevelId, levelId);
        }

        private static bool IsTableBoundToMonster(Godot.Collections.Dictionary<string, Variant> table, string monsterId)
        {
            if (!table.ContainsKey("bind_monster_ids"))
            {
                return DropTableBindingRules.IsTableBoundToMonster(System.Array.Empty<string>(), monsterId);
            }

            Variant bindVariant = table["bind_monster_ids"];
            if (bindVariant.VariantType != Variant.Type.Array)
            {
                return DropTableBindingRules.IsTableBoundToMonster(System.Array.Empty<string>(), monsterId);
            }

            var bindArray = (Godot.Collections.Array<Variant>)bindVariant;
            var boundMonsterIds = new List<string>();
            foreach (Variant item in bindArray)
            {
                string id = item.AsString();
                if (!string.IsNullOrEmpty(id))
                {
                    boundMonsterIds.Add(id);
                }
            }

            return DropTableBindingRules.IsTableBoundToMonster(boundMonsterIds, monsterId);
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
            ValidateEquipmentConfiguration();
        }

        private void ValidateEquipmentConfiguration()
        {
            foreach (var issue in EquipmentConfigValidationTextRules.Validate(_lastLoadedConfigText))
            {
                AddValidationIssue(
                    scope: issue.Scope,
                    id: issue.Id,
                    field: issue.Field,
                    message: issue.Message,
                    levelId: issue.LevelId);
            }
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
                    string entryType = GetString(entry, "entry_type", "item");
                    string itemId = GetString(entry, "item_id", "");
                    string equipmentTemplateId = GetString(entry, "equipment_template_id", "");
                    int weight = Math.Max(0, entry.ContainsKey("weight") ? entry["weight"].AsInt32() : 0);
                    int qtyMin = Math.Max(0, entry.ContainsKey("qty_min") ? entry["qty_min"].AsInt32() : 0);
                    int qtyMax = Math.Max(0, entry.ContainsKey("qty_max") ? entry["qty_max"].AsInt32() : 0);
                    totalWeight += weight;

                    bool isEquipmentTemplate = entryType == "equipment_template";
                    if (isEquipmentTemplate)
                    {
                        if (string.IsNullOrEmpty(equipmentTemplateId))
                        {
                            AddValidationIssue(
                                scope: "drop_table",
                                id: tableId,
                                field: $"entries[{i}].equipment_template_id",
                                message: "missing",
                                dropTableId: tableId);
                        }
                        else if (!_equipmentTemplateById.ContainsKey(equipmentTemplateId))
                        {
                            AddValidationIssue(
                                scope: "drop_table",
                                id: tableId,
                                field: $"entries[{i}].equipment_template_id",
                                message: $"equipment template '{equipmentTemplateId}' not found",
                                dropTableId: tableId);
                        }
                    }
                    else if (string.IsNullOrEmpty(itemId))
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

        private void IndexEquipmentSeries()
        {
            _equipmentSeriesById.Clear();
            var indexes = EquipmentConfigTextParser.ParseIndexes(_lastLoadedConfigText);
            foreach (var kv in indexes.SeriesJsonById)
            {
                Variant parsed = Json.ParseString(kv.Value);
                if (parsed.VariantType == Variant.Type.Dictionary)
                {
                    _equipmentSeriesById[kv.Key] = (Godot.Collections.Dictionary<string, Variant>)parsed;
                }
            }
        }

        private void IndexEquipmentTemplates()
        {
            _equipmentTemplateById.Clear();
            var indexes = EquipmentConfigTextParser.ParseIndexes(_lastLoadedConfigText);
            foreach (var kv in indexes.TemplateJsonById)
            {
                Variant parsed = Json.ParseString(kv.Value);
                if (parsed.VariantType == Variant.Type.Dictionary)
                {
                    _equipmentTemplateById[kv.Key] = (Godot.Collections.Dictionary<string, Variant>)parsed;
                }
            }
        }

        private void IndexEquipmentExchangeRecipes()
        {
            _equipmentExchangeRecipesByLevelId.Clear();
            var indexes = EquipmentConfigTextParser.ParseIndexes(_lastLoadedConfigText);
            foreach (var kv in indexes.ExchangeRecipeJsonByLevelId)
            {
                var list = new List<Godot.Collections.Dictionary<string, Variant>>();
                foreach (string recipeJson in kv.Value)
                {
                    Variant parsed = Json.ParseString(recipeJson);
                    if (parsed.VariantType == Variant.Type.Dictionary)
                    {
                        list.Add((Godot.Collections.Dictionary<string, Variant>)parsed);
                    }
                }

                _equipmentExchangeRecipesByLevelId[kv.Key] = list;
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
