using Xiuxian.Scripts.Services;
using Xiuxian2.Core.Tests.Builders;
using Xiuxian2.Core.Tests.Support.Deterministic;

namespace Xiuxian2.Core.Tests.Services;

public sealed class LevelConfigLoaderSeamTests
{
    [Fact]
    public void LoadConfigCanReadFromInMemoryConfigSource()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        var loader = CreateLoader(fixture);

        var loaded = loader.LoadConfig();

        Assert.True(loaded);
        Assert.Equal("starter_plain", loader.ActiveLevelId);
        Assert.Equal("Starter Plain", loader.ActiveLevelName);
    }

    [Fact]
    public void SettlementRewardRollsFollowScriptedRngValues()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        fixture.ConfigSource.AddFile("memory://level-config.json", CreateRewardConfigJson());
        fixture.Rng.EnqueueInt(7).EnqueueInt(3);

        var loader = CreateLoader(fixture, "memory://level-config.json");
        Assert.True(loader.LoadConfig());

        var rolled = loader.TryRollMonsterSettlementReward("green_slime", out var lingqi, out var insight);

        Assert.True(rolled);
        Assert.Equal(7d, lingqi);
        Assert.Equal(3d, insight);
    }

    [Fact]
    public void DailyDropCapResetsWhenClockMovesToNextDay()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        fixture.ConfigSource.AddFile("memory://level-config.json", CreateDailyCapConfigJson());
        fixture.Rng.EnqueueInt(1).EnqueueInt(1);

        var loader = CreateLoader(fixture, "memory://level-config.json");
        Assert.True(loader.LoadConfig());

        var first = loader.RollMonsterDrops("green_slime");
        var blocked = loader.RollMonsterDrops("green_slime");

        fixture.Clock.AdvanceSeconds(86_400);

        var afterReset = loader.RollMonsterDrops("green_slime");

        Assert.Equal(1, first["spirit_stone"]);
        Assert.Empty(blocked);
        Assert.Equal(1, afterReset["spirit_stone"]);
    }

    private static LevelConfigLoader CreateLoader(ServiceFixture fixture, string? configPath = null)
    {
        var loader = new LevelConfigLoader
        {
            ConfigPath = configPath ?? fixture.FrozenConfigPath
        };

        loader.UseTestSeams(fixture.ConfigSource, fixture.Rng, fixture.Clock);
        return loader;
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
}
