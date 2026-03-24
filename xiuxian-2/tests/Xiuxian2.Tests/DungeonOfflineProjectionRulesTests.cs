using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class DungeonOfflineProjectionRulesTests
{
    [Fact]
    public void ProjectionRules_BuildWeightedAveragesFromMonsterSpecs()
    {
        var specs = new[]
        {
            new DungeonOfflineProjectionRules.MonsterSettlementSpec(
                80,
                new MonsterStatProfile("m1", "A", new CharacterStatBlock(30, 4, 1, 80, 0, 1.5), 10),
                AverageLingqi: 4,
                AverageInsight: 1,
                AverageItemDrops: new Dictionary<string, double> { ["herb"] = 0.5 },
                AverageEquipmentDrops: 0.1),
            new DungeonOfflineProjectionRules.MonsterSettlementSpec(
                20,
                new MonsterStatProfile("m2", "B", new CharacterStatBlock(60, 8, 2, 80, 0, 1.5), 10),
                AverageLingqi: 10,
                AverageInsight: 3,
                AverageItemDrops: new Dictionary<string, double> { ["herb"] = 1.5, ["shard"] = 0.5 },
                AverageEquipmentDrops: 0.4)
        };

        double avgLingqi = DungeonOfflineProjectionRules.CalculateAverageLingqiPerVictory(specs);
        double avgInsight = DungeonOfflineProjectionRules.CalculateAverageInsightPerVictory(specs);
        Dictionary<string, double> itemDrops = DungeonOfflineProjectionRules.CalculateAverageItemDropsPerVictory(specs);
        double avgEquipment = DungeonOfflineProjectionRules.CalculateAverageEquipmentDropsPerVictory(specs);

        Assert.Equal(5.2, avgLingqi, 6);
        Assert.Equal(1.4, avgInsight, 6);
        Assert.Equal(0.7, itemDrops["herb"], 6);
        Assert.Equal(0.1, itemDrops["shard"], 6);
        Assert.Equal(0.16, avgEquipment, 6);
    }
}
