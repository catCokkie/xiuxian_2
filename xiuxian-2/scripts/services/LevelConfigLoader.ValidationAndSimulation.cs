using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xiuxian.Scripts.Services
{
    public partial class LevelConfigLoader
    {
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
}
