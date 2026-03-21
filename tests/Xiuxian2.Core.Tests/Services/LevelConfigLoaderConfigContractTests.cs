using System.Text;
using System.Text.Json.Nodes;
using Xiuxian2.Core.Tests.Builders;
using Xunit;

namespace Xiuxian2.Core.Tests.Services;

public sealed class LevelConfigLoaderConfigContractTests
{
    [Fact]
    public void ValidFixtureLoadsWithoutValidationDrift()
    {
        var fixture = new ServiceFixtureBuilder()
            .WithFrozenConfigRelativePath(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config", "phase2-valid-level-config.json"))
            .Build();

        var runtime = CreateRuntime(fixture);

        var loaded = runtime.LoadConfig(fixture.FrozenConfigPath);

        Assert.True(loaded);
        Assert.Equal("starter_plain", runtime.ActiveLevelId);
        Assert.Equal("Starter Plain", runtime.ActiveLevelName);
        Assert.Equal(0, runtime.GetValidationIssues().Count);
        Assert.Equal(0, runtime.GetValidationEntries().Count);
        Assert.Equal("config validation: OK", runtime.BuildValidationSummary());
    }

    [Fact]
    public void InvalidFixtureProducesStableStructuredValidationEntries()
    {
        var fixture = new ServiceFixtureBuilder()
            .WithFrozenConfigRelativePath(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config", "phase2-invalid-level-config.json"))
            .Build();

        var runtime = CreateRuntime(fixture);

        var loaded = runtime.LoadConfig(fixture.FrozenConfigPath);

        Assert.True(loaded);

        var issues = runtime.GetValidationIssues();
        var entries = runtime.GetValidationEntries();

        Assert.Equal(10, issues.Count);
        Assert.Equal(10, entries.Count);

        AssertValidationEntry(entries[0], "level", "misty_bog", "spawn_table[0].monster_id", "spawn monster 'ghost_slug' not found in monsters[]", levelId: "misty_bog", monsterId: "ghost_slug");
        AssertValidationEntry(entries[1], "level", "misty_bog", "spawn_table.total_weight", "total weight = 90 (expected 100)", levelId: "misty_bog");
        AssertValidationEntry(entries[2], "monster", "bog_rat", "drops.drop_table_id", "drop table 'missing_table' not found", monsterId: "bog_rat", dropTableId: "missing_table");
        AssertValidationEntry(entries[3], "drop_table", "bog_table", "bind_level_id", "'missing_level' not found in levels[]", levelId: "missing_level", dropTableId: "bog_table");
        AssertValidationEntry(entries[4], "drop_table", "bog_table", "bind_monster_ids[0]", "is empty", dropTableId: "bog_table");
        AssertValidationEntry(entries[5], "drop_table", "bog_table", "bind_monster_ids[1]", "bound monster 'ghost_slug' not found", monsterId: "ghost_slug", dropTableId: "bog_table");
        AssertValidationEntry(entries[6], "drop_table", "bog_table", "entries[0].item_id", "missing", dropTableId: "bog_table");
        AssertValidationEntry(entries[7], "drop_table", "bog_table", "entries[0].weight", "weight <= 0", dropTableId: "bog_table");
        AssertValidationEntry(entries[8], "drop_table", "bog_table", "entries[0].qty_max", "qty_max < qty_min", dropTableId: "bog_table");
        AssertValidationEntry(entries[9], "drop_table", "bog_table", "entries.total_weight", "total weight <= 0", dropTableId: "bog_table");

        Assert.Equal("level misty_bog: spawn_table[0].monster_id spawn monster 'ghost_slug' not found in monsters[].", issues[0]);
        Assert.Equal("drop_table bog_table: entries.total_weight total weight <= 0.", issues[9]);

        var summary = runtime.BuildValidationSummary(3);
        Assert.Equal(
            "config validation: 10 issue(s)\n- level misty_bog: spawn_table[0].monster_id spawn monster 'ghost_slug' not found in monsters[].\n- level misty_bog: spawn_table.total_weight total weight = 90 (expected 100).\n- monster bog_rat: drops.drop_table_id drop table 'missing_table' not found.\n- ... and 7 more",
            summary);
    }

    [Fact]
    public void ContractFixturesStayOwnedByTheTestProject()
    {
        var validFixture = new ServiceFixtureBuilder()
            .WithFrozenConfigRelativePath(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config", "phase2-valid-level-config.json"))
            .Build();
        var invalidFixture = new ServiceFixtureBuilder()
            .WithFrozenConfigRelativePath(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config", "phase2-invalid-level-config.json"))
            .Build();

        Assert.Contains(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config"), validFixture.FrozenConfigPath);
        Assert.Contains(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config"), invalidFixture.FrozenConfigPath);
        Assert.DoesNotContain("09_level_monster_drop_sample.json", validFixture.LoadFrozenConfigText());
        Assert.DoesNotContain("09_level_monster_drop_sample.json", invalidFixture.LoadFrozenConfigText());
    }

    private static LevelConfigLoaderContractHarness CreateRuntime(ServiceFixture fixture)
    {
        return new LevelConfigLoaderContractHarness();
    }

    private static void AssertValidationEntry(
        ValidationEntry entry,
        string scope,
        string id,
        string field,
        string message,
        string levelId = "",
        string monsterId = "",
        string dropTableId = "")
    {
        Assert.Equal(scope, entry.Scope);
        Assert.Equal(id, entry.Id);
        Assert.Equal(field, entry.Field);
        Assert.Equal("error", entry.Severity);
        Assert.Equal(message, entry.Message);
        Assert.Equal(levelId, entry.LevelId);
        Assert.Equal(monsterId, entry.MonsterId);
        Assert.Equal(dropTableId, entry.DropTableId);
    }

    private sealed class LevelConfigLoaderContractHarness
    {
        private JsonObject _root = new();
        private readonly List<JsonObject> _levels = new();
        private readonly Dictionary<string, JsonObject> _monsterById = new(StringComparer.Ordinal);
        private readonly Dictionary<string, JsonObject> _dropTableById = new(StringComparer.Ordinal);
        private readonly List<string> _validationIssues = new();
        private readonly List<ValidationEntry> _validationEntries = new();
        private int _activeLevelIndex;

        public string ActiveLevelId { get; private set; } = string.Empty;

        public string ActiveLevelName { get; private set; } = "Unknown Zone";

        public bool LoadConfig(string configPath)
        {
            string text = File.ReadAllText(configPath);
            JsonNode? node = JsonNode.Parse(text);
            if (node is not JsonObject root)
            {
                return false;
            }

            _root = root;
            _levels.Clear();
            _monsterById.Clear();
            _dropTableById.Clear();
            _validationIssues.Clear();
            _validationEntries.Clear();
            _activeLevelIndex = 0;

            ParseLevelsSection();
            IndexMonsters();
            IndexDropTables();
            ValidateConfiguration();
            return _levels.Count > 0;
        }

        public IReadOnlyList<string> GetValidationIssues()
        {
            return _validationIssues;
        }

        public IReadOnlyList<ValidationEntry> GetValidationEntries()
        {
            return _validationEntries;
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

        private void ParseLevelsSection()
        {
            if (!TryGetArray(_root, "levels", out JsonArray? levels))
            {
                return;
            }

            foreach (JsonNode? item in levels)
            {
                if (item is JsonObject level)
                {
                    _levels.Add(level);
                }
            }

            ApplyActiveLevelData();
        }

        private void ApplyActiveLevelData()
        {
            if (_levels.Count == 0)
            {
                ActiveLevelId = string.Empty;
                ActiveLevelName = "Unknown Zone";
                return;
            }

            _activeLevelIndex = Math.Clamp(_activeLevelIndex, 0, _levels.Count - 1);
            JsonObject level = _levels[_activeLevelIndex];
            ActiveLevelId = GetString(level, "level_id", string.Empty);
            ActiveLevelName = GetString(level, "level_name", "Unknown Zone");
        }

        private void IndexMonsters()
        {
            if (!TryGetArray(_root, "monsters", out JsonArray? monsters))
            {
                return;
            }

            foreach (JsonNode? item in monsters)
            {
                if (item is not JsonObject monster)
                {
                    continue;
                }

                string monsterId = GetString(monster, "monster_id", string.Empty);
                if (!string.IsNullOrEmpty(monsterId))
                {
                    _monsterById[monsterId] = monster;
                }
            }
        }

        private void IndexDropTables()
        {
            if (!TryGetArray(_root, "drop_tables", out JsonArray? dropTables))
            {
                return;
            }

            foreach (JsonNode? item in dropTables)
            {
                if (item is not JsonObject dropTable)
                {
                    continue;
                }

                string tableId = GetString(dropTable, "drop_table_id", string.Empty);
                if (!string.IsNullOrEmpty(tableId))
                {
                    _dropTableById[tableId] = dropTable;
                }
            }
        }

        private void ValidateConfiguration()
        {
            if (_levels.Count == 0)
            {
                AddValidationIssue(scope: "config", id: "levels", field: "levels[]", message: "is empty");
                return;
            }

            var levelIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonObject level in _levels)
            {
                string levelId = GetString(level, "level_id", string.Empty);
                if (string.IsNullOrEmpty(levelId))
                {
                    AddValidationIssue(scope: "level", id: "(unknown-level)", field: "level_id", message: "missing");
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

        private void ValidateLevelSpawnTable(JsonObject level, string levelId)
        {
            string id = string.IsNullOrEmpty(levelId) ? "(unknown-level)" : levelId;
            if (!TryGetArray(level, "spawn_table", out JsonArray? spawnTable))
            {
                AddValidationIssue(scope: "level", id: id, field: "spawn_table", message: "missing or not array", levelId: id);
                return;
            }

            if (spawnTable.Count == 0)
            {
                AddValidationIssue(scope: "level", id: id, field: "spawn_table", message: "is empty", levelId: id);
                return;
            }

            int totalWeight = 0;
            for (int i = 0; i < spawnTable.Count; i++)
            {
                if (spawnTable[i] is not JsonObject entry)
                {
                    AddValidationIssue(scope: "level", id: id, field: $"spawn_table[{i}]", message: "is not dictionary", levelId: id);
                    continue;
                }

                string monsterId = GetString(entry, "monster_id", string.Empty);
                int weight = Math.Max(0, GetInt(entry, "weight", 0));
                totalWeight += weight;

                if (string.IsNullOrEmpty(monsterId))
                {
                    AddValidationIssue(scope: "level", id: id, field: $"spawn_table[{i}].monster_id", message: "missing", levelId: id);
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
                AddValidationIssue(scope: "level", id: id, field: "spawn_table.total_weight", message: "total weight <= 0", levelId: id);
            }
            else if (totalWeight != 100)
            {
                AddValidationIssue(scope: "level", id: id, field: "spawn_table.total_weight", message: $"total weight = {totalWeight} (expected 100)", levelId: id);
            }
        }

        private void ValidateMonsters()
        {
            foreach ((string monsterId, JsonObject monster) in _monsterById)
            {
                if (!TryGetObject(monster, "drops", out JsonObject? drops))
                {
                    AddValidationIssue(scope: "monster", id: monsterId, field: "drops", message: "section missing", monsterId: monsterId);
                    continue;
                }

                string tableId = GetString(drops, "drop_table_id", string.Empty);
                if (string.IsNullOrEmpty(tableId))
                {
                    AddValidationIssue(scope: "monster", id: monsterId, field: "drops.drop_table_id", message: "missing", monsterId: monsterId);
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
                AddValidationIssue(scope: "config", id: "drop_tables", field: "drop_tables[]", message: "is empty");
                return;
            }

            foreach ((string tableId, JsonObject table) in _dropTableById)
            {
                string bindLevelId = GetString(table, "bind_level_id", string.Empty);
                if (string.IsNullOrEmpty(bindLevelId))
                {
                    AddValidationIssue(scope: "drop_table", id: tableId, field: "bind_level_id", message: "missing", dropTableId: tableId);
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

                if (!TryGetArray(table, "bind_monster_ids", out JsonArray? boundMonsters))
                {
                    AddValidationIssue(scope: "drop_table", id: tableId, field: "bind_monster_ids", message: "missing or not array", dropTableId: tableId);
                }
                else
                {
                    if (boundMonsters.Count == 0)
                    {
                        AddValidationIssue(scope: "drop_table", id: tableId, field: "bind_monster_ids", message: "is empty", dropTableId: tableId);
                    }

                    for (int i = 0; i < boundMonsters.Count; i++)
                    {
                        string monsterId = boundMonsters[i]?.GetValue<string?>() ?? string.Empty;
                        if (string.IsNullOrEmpty(monsterId))
                        {
                            AddValidationIssue(scope: "drop_table", id: tableId, field: $"bind_monster_ids[{i}]", message: "is empty", dropTableId: tableId);
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

                if (!TryGetArray(table, "entries", out JsonArray? entries))
                {
                    AddValidationIssue(scope: "drop_table", id: tableId, field: "entries", message: "missing or not array", dropTableId: tableId);
                    continue;
                }

                if (entries.Count == 0)
                {
                    AddValidationIssue(scope: "drop_table", id: tableId, field: "entries", message: "is empty", dropTableId: tableId);
                    continue;
                }

                int totalWeight = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] is not JsonObject entry)
                    {
                        AddValidationIssue(scope: "drop_table", id: tableId, field: $"entries[{i}]", message: "is not dictionary", dropTableId: tableId);
                        continue;
                    }

                    string itemId = GetString(entry, "item_id", string.Empty);
                    int weight = Math.Max(0, GetInt(entry, "weight", 0));
                    int qtyMin = Math.Max(0, GetInt(entry, "qty_min", 0));
                    int qtyMax = Math.Max(0, GetInt(entry, "qty_max", 0));
                    totalWeight += weight;

                    if (string.IsNullOrEmpty(itemId))
                    {
                        AddValidationIssue(scope: "drop_table", id: tableId, field: $"entries[{i}].item_id", message: "missing", dropTableId: tableId);
                    }

                    if (weight <= 0)
                    {
                        AddValidationIssue(scope: "drop_table", id: tableId, field: $"entries[{i}].weight", message: "weight <= 0", dropTableId: tableId);
                    }

                    if (qtyMax < qtyMin)
                    {
                        AddValidationIssue(scope: "drop_table", id: tableId, field: $"entries[{i}].qty_max", message: "qty_max < qty_min", dropTableId: tableId);
                    }
                }

                if (totalWeight <= 0)
                {
                    AddValidationIssue(scope: "drop_table", id: tableId, field: "entries.total_weight", message: "total weight <= 0", dropTableId: tableId);
                }
                else if (totalWeight != 100)
                {
                    AddValidationIssue(scope: "drop_table", id: tableId, field: "entries.total_weight", message: $"total weight = {totalWeight} (expected 100)", dropTableId: tableId);
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
            var entry = new ValidationEntry(scope, id, field, severity, message, levelId, monsterId, dropTableId);
            _validationEntries.Add(entry);
            _validationIssues.Add(BuildValidationIssueMessage(entry));
        }

        private static string BuildValidationIssueMessage(ValidationEntry entry)
        {
            return $"{entry.Scope} {entry.Id}: {entry.Field} {entry.Message}.";
        }

        private static bool TryGetObject(JsonObject source, string key, out JsonObject? value)
        {
            if (source.TryGetPropertyValue(key, out JsonNode? node) && node is JsonObject obj)
            {
                value = obj;
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryGetArray(JsonObject source, string key, out JsonArray? value)
        {
            if (source.TryGetPropertyValue(key, out JsonNode? node) && node is JsonArray arr)
            {
                value = arr;
                return true;
            }

            value = null;
            return false;
        }

        private static string GetString(JsonObject source, string key, string fallback)
        {
            if (!source.TryGetPropertyValue(key, out JsonNode? node) || node is null)
            {
                return fallback;
            }

            return node.GetValue<string?>() ?? fallback;
        }

        private static int GetInt(JsonObject source, string key, int fallback)
        {
            if (!source.TryGetPropertyValue(key, out JsonNode? node) || node is null)
            {
                return fallback;
            }

            return node.GetValue<int>();
        }
    }

    private sealed record ValidationEntry(
        string Scope,
        string Id,
        string Field,
        string Severity,
        string Message,
        string LevelId,
        string MonsterId,
        string DropTableId);
}
