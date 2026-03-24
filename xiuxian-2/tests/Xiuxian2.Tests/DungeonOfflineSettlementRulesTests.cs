using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class DungeonOfflineSettlementRulesTests
{
    [Fact]
    public void CalculateEstimatedEncounters_UsesExploreProgressThreshold()
    {
        int encounters = DungeonOfflineSettlementRules.CalculateEstimatedEncounters(
            offlineInputBudget: 600,
            dungeonProgressPerInput: 0.2,
            encounterProgressThreshold: 20.0);

        Assert.Equal(6, encounters);
    }

    [Fact]
    public void CalculateWeightedVictoryFactor_RespectsMonsterWeights()
    {
        CharacterStatBlock player = new(100, 14, 5, 100, 0.05, 1.5);
        var monsters = new[]
        {
            new DungeonOfflineSettlementRules.WeightedMonsterProfile(new MonsterStatProfile("m1", "弱怪", new CharacterStatBlock(30, 4, 1, 80, 0.0, 1.5), 10), 80),
            new DungeonOfflineSettlementRules.WeightedMonsterProfile(new MonsterStatProfile("m2", "强怪", new CharacterStatBlock(100, 14, 4, 80, 0.0, 1.5), 10), 20)
        };

        double factor = DungeonOfflineSettlementRules.CalculateWeightedVictoryFactor(player, monsters);

        Assert.InRange(factor, 0.7, 0.95);
    }

    [Fact]
    public void BuildDungeonOfflineSettlement_CapsEquipmentDropsAndGeneratesRewards()
    {
        CharacterStatBlock player = new(100, 16, 6, 100, 0.1, 1.6);
        var monsters = new[]
        {
            new DungeonOfflineSettlementRules.WeightedMonsterProfile(new MonsterStatProfile("m1", "怪", new CharacterStatBlock(40, 4, 1, 80, 0.0, 1.5), 10), 100)
        };

        ActionSettlementResult result = DungeonOfflineSettlementRules.BuildDungeonOfflineSettlement(
            actionTargetId: "lv_qi_001",
            offlineInputBudget: 600,
            dungeonProgressPerInput: 0.2,
            encounterProgressThreshold: 20.0,
            playerStats: player,
            weightedMonsters: monsters,
            averageLingqiPerVictory: 3.0,
            averageInsightPerVictory: 1.0,
            averageItemDropsPerVictory: new Dictionary<string, double> { ["lingqi_shard"] = 0.5 },
            equipmentDropCap: 2,
            estimatedEquipmentDropsPerVictory: 0.8);

        Assert.Equal(PlayerActionState.ActionDungeon, result.ActionId);
        Assert.True(result.ExploreProgressGain > 0.0);
        Assert.True(result.BattleRoundsAdvanced > 0);
        Assert.True(result.LingqiGain > 0.0);
        Assert.True(result.InsightGain > 0.0);
        Assert.True(result.ItemDrops.ContainsKey("lingqi_shard"));
        Assert.True(result.EquipmentDrops.Count <= 2);
    }
}
