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
        private readonly RandomNumberGenerator _rng = new();
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
            EnsureLevelUnlockBootstrap();
            IndexMonsters();
            IndexDropTables();
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
