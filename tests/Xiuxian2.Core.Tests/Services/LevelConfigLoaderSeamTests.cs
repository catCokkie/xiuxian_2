using System.Text.Json.Nodes;
using Xiuxian2.Core.Tests.Builders;
using Xunit;

namespace Xiuxian2.Core.Tests.Services;

public sealed class LevelConfigLoaderSeamTests
{
    [Fact]
    public void LoadConfigCanReadFromInMemoryConfigSource()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        var runtime = CreateRuntime(fixture);

        Assert.Equal("starter_plain", runtime.ActiveLevelId);
        Assert.Equal("Starter Plain", runtime.ActiveLevelName);
    }

    [Fact]
    public void SettlementRewardRollsFollowScriptedRngValues()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        fixture.ConfigSource.AddFile("memory://level-config.json", CreateRewardConfigJson());
        fixture.Rng.EnqueueInt(7).EnqueueInt(3);

        var runtime = CreateRuntime(fixture, "memory://level-config.json");

        var rolled = runtime.TryRollMonsterSettlementReward("green_slime", out var lingqi, out var insight);

        Assert.True(rolled);
        Assert.Equal(7d, lingqi);
        Assert.Equal(3d, insight);
    }

    [Fact]
    public void DailyDropCapResetsWhenClockMovesToNextDay()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        fixture.ConfigSource.AddFile("memory://level-config.json", CreateDailyCapConfigJson());
        fixture.Rng.EnqueueInt(1).EnqueueInt(1).EnqueueInt(1).EnqueueInt(1);

        var runtime = CreateRuntime(fixture, "memory://level-config.json");

        var first = runtime.RollMonsterDrops("green_slime");
        var blocked = runtime.RollMonsterDrops("green_slime");

        fixture.Clock.AdvanceSeconds(86_400);

        var afterReset = runtime.RollMonsterDrops("green_slime");

        Assert.Equal(1, first["spirit_stone"]);
        Assert.Empty(blocked);
        Assert.Equal(1, afterReset["spirit_stone"]);
    }

    private static LevelConfigLoaderPlanHarness CreateRuntime(ServiceFixture fixture, string? configPath = null)
    {
        var runtime = new LevelConfigLoaderPlanHarness(fixture.ConfigSource, fixture.Rng, fixture.Clock);
        var loaded = runtime.LoadConfig(configPath ?? fixture.FrozenConfigPath);
        Assert.True(loaded);
        return runtime;
    }

    private static string CreateRewardConfigJson()
    {
        return
            """
            {
              "levels": [
                {
                  "level_id": "starter_plain",
                  "level_name": "Starter Plain",
                  "spawn_table": [
                    {
                      "monster_id": "green_slime",
                      "weight": 100
                    }
                  ]
                }
              ],
              "monsters": [
                {
                  "monster_id": "green_slime",
                  "monster_name": "Green Slime",
                  "drops": {
                    "drop_table_id": "starter_plain_green_slime"
                  },
                  "settlement_reward": {
                    "lingqi_min": 5,
                    "lingqi_max": 9,
                    "insight_min": 1,
                    "insight_max": 4
                  }
                }
              ],
              "drop_tables": [
                {
                  "drop_table_id": "starter_plain_green_slime",
                  "bind_level_id": "starter_plain",
                  "bind_monster_ids": ["green_slime"],
                  "entries": [
                    {
                      "item_id": "spirit_stone",
                      "weight": 100,
                      "min_qty": 1,
                      "max_qty": 1
                    }
                  ]
                }
              ]
            }
            """;
    }

    private static string CreateDailyCapConfigJson()
    {
        return
            """
            {
              "levels": [
                {
                  "level_id": "starter_plain",
                  "level_name": "Starter Plain",
                  "spawn_table": [
                    {
                      "monster_id": "green_slime",
                      "weight": 100
                    }
                  ]
                }
              ],
              "monsters": [
                {
                  "monster_id": "green_slime",
                  "monster_name": "Green Slime",
                  "drops": {
                    "drop_table_id": "starter_plain_green_slime",
                    "drop_roll_count": 1
                  }
                }
              ],
              "drop_tables": [
                {
                  "drop_table_id": "starter_plain_green_slime",
                  "bind_level_id": "starter_plain",
                  "bind_monster_ids": ["green_slime"],
                  "economy": {
                    "daily_cap_rolls": 1
                  },
                  "entries": [
                    {
                      "item_id": "spirit_stone",
                      "weight": 100,
                      "min_qty": 1,
                      "max_qty": 1
                    }
                  ]
                }
              ]
            }
            """;
    }

    private sealed class LevelConfigLoaderPlanHarness
    {
        private readonly Xiuxian.Scripts.Contracts.IConfigSource _configSource;
        private readonly Xiuxian.Scripts.Contracts.IRng _rng;
        private readonly Xiuxian.Scripts.Contracts.IClock _clock;
        private JsonObject _root = new();
        private readonly Dictionary<string, JsonObject> _monsterById = new(StringComparer.Ordinal);
        private readonly Dictionary<string, JsonObject> _dropTableById = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _dailyRollCountByTable = new(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _dailyRollDayByTable = new(StringComparer.Ordinal);

        public LevelConfigLoaderPlanHarness(
            Xiuxian.Scripts.Contracts.IConfigSource configSource,
            Xiuxian.Scripts.Contracts.IRng rng,
            Xiuxian.Scripts.Contracts.IClock clock)
        {
            _configSource = configSource;
            _rng = rng;
            _clock = clock;
        }

        public string ActiveLevelId { get; private set; } = string.Empty;

        public string ActiveLevelName { get; private set; } = "Unknown Zone";

        public bool LoadConfig(string path)
        {
            if (!_configSource.TryReadAllText(path, out string text))
            {
                return false;
            }

            JsonNode? node = JsonNode.Parse(text);
            if (node is not JsonObject root)
            {
                return false;
            }

            _root = root;
            _monsterById.Clear();
            _dropTableById.Clear();
            _dailyRollCountByTable.Clear();
            _dailyRollDayByTable.Clear();

            if (!TryGetArray(root, "levels", out JsonArray? levels) || levels.Count == 0 || levels[0] is not JsonObject level)
            {
                return false;
            }

            ActiveLevelId = GetString(level, "level_id", string.Empty);
            ActiveLevelName = GetString(level, "level_name", "Unknown Zone");

            if (TryGetArray(root, "monsters", out JsonArray? monsters))
            {
                foreach (JsonNode? item in monsters)
                {
                    if (item is JsonObject monster)
                    {
                        string id = GetString(monster, "monster_id", string.Empty);
                        if (!string.IsNullOrEmpty(id))
                        {
                            _monsterById[id] = monster;
                        }
                    }
                }
            }

            if (TryGetArray(root, "drop_tables", out JsonArray? dropTables))
            {
                foreach (JsonNode? item in dropTables)
                {
                    if (item is JsonObject table)
                    {
                        string id = GetString(table, "drop_table_id", string.Empty);
                        if (!string.IsNullOrEmpty(id))
                        {
                            _dropTableById[id] = table;
                        }
                    }
                }
            }

            return true;
        }

        public bool TryRollMonsterSettlementReward(string monsterId, out double lingqi, out double insight)
        {
            lingqi = 0;
            insight = 0;

            if (!_monsterById.TryGetValue(monsterId, out JsonObject? monster) || !TryGetObject(monster, "settlement_reward", out JsonObject? settlement))
            {
                return false;
            }

            int lingqiMin = GetInt(settlement, "lingqi_min", 0);
            int lingqiMax = GetInt(settlement, "lingqi_max", lingqiMin);
            int insightMin = GetInt(settlement, "insight_min", 0);
            int insightMax = GetInt(settlement, "insight_max", insightMin);

            lingqi = _rng.NextInt(lingqiMin, Math.Max(lingqiMin, lingqiMax));
            insight = _rng.NextInt(insightMin, Math.Max(insightMin, insightMax));
            return true;
        }

        public Dictionary<string, int> RollMonsterDrops(string monsterId)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            if (!_monsterById.TryGetValue(monsterId, out JsonObject? monster) || !TryGetObject(monster, "drops", out JsonObject? drops))
            {
                return result;
            }

            string tableId = GetString(drops, "drop_table_id", string.Empty);
            if (!_dropTableById.TryGetValue(tableId, out JsonObject? table) || !TryGetArray(table, "entries", out JsonArray? entries))
            {
                return result;
            }

            long dayIndex = _clock.GetUnixTimeSeconds() / 86400;
            int dailyCap = 0;
            if (TryGetObject(table, "economy", out JsonObject? economy))
            {
                dailyCap = GetInt(economy, "daily_cap_rolls", 0);
            }

            if (!_dailyRollDayByTable.TryGetValue(tableId, out long savedDay) || savedDay != dayIndex)
            {
                _dailyRollDayByTable[tableId] = dayIndex;
                _dailyRollCountByTable[tableId] = 0;
            }

            int currentDaily = _dailyRollCountByTable.TryGetValue(tableId, out int value) ? value : 0;
            if (dailyCap > 0 && currentDaily >= dailyCap)
            {
                return result;
            }

            _dailyRollCountByTable[tableId] = currentDaily + 1;

            JsonObject picked = PickWeighted(entries);
            if (picked.Count == 0)
            {
                return result;
            }

            string itemId = GetString(picked, "item_id", string.Empty);
            int minQty = GetInt(picked, "min_qty", 1);
            int maxQty = Math.Max(minQty, GetInt(picked, "max_qty", minQty));
            result[itemId] = _rng.NextInt(minQty, maxQty);
            return result;
        }

        private JsonObject PickWeighted(JsonArray entries)
        {
            int totalWeight = 0;
            foreach (JsonNode? item in entries)
            {
                if (item is JsonObject entry)
                {
                    totalWeight += Math.Max(0, GetInt(entry, "weight", 0));
                }
            }

            if (totalWeight <= 0)
            {
                return new JsonObject();
            }

            int roll = _rng.NextInt(1, totalWeight);
            int accumulated = 0;
            foreach (JsonNode? item in entries)
            {
                if (item is not JsonObject entry)
                {
                    continue;
                }

                int weight = Math.Max(0, GetInt(entry, "weight", 0));
                accumulated += weight;
                if (roll <= accumulated)
                {
                    return entry;
                }
            }

            return new JsonObject();
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
}
