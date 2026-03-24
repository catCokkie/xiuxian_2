using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class ActionSettlementRulesTests
{
    [Fact]
    public void BuildCultivationSettlement_ProducesCultivationTaggedResult()
    {
        ActionSettlementResult result = ActionSettlementRules.BuildCultivationSettlement(
            actionTargetId: string.Empty,
            apConsumed: 42,
            lingqiGain: 30,
            insightGain: 2,
            petAffinityGain: 1,
            realmExpGain: 0);

        Assert.Equal(PlayerActionState.ActionCultivation, result.ActionId);
        Assert.Equal("cultivation_tick", result.SourceTag);
        Assert.Equal(42, result.ApConsumed);
        Assert.True(result.HasAnyReward);
    }

    [Fact]
    public void BuildDungeonRewardSettlement_StoresDropsAndProgress()
    {
        var itemDrops = new Dictionary<string, int> { ["lingqi_shard"] = 2 };
        var equipmentDrops = new[]
        {
            new EquipmentInstanceData(
                "eq_drop_1",
                "eq_weapon_qi_outer_moss_blade",
                "苔锋短刃",
                EquipmentSlotType.Weapon,
                "series_qi_outer_cave",
                EquipmentRarityTier.Artifact,
                EquipmentSourceStage.Elite,
                "lv_qi_001",
                "attack_flat",
                5,
                System.Array.Empty<EquipmentSubStatData>(),
                0,
                12,
                123,
                false)
        };

        ActionSettlementResult result = ActionSettlementRules.BuildDungeonRewardSettlement(
            actionTargetId: "lv_qi_001",
            sourceTag: "battle_drop",
            exploreProgressGain: 3.5,
            battleRoundsAdvanced: 2,
            itemDrops: itemDrops,
            equipmentDrops: equipmentDrops,
            lingqiGain: 8,
            insightGain: 1);

        Assert.Equal(PlayerActionState.ActionDungeon, result.ActionId);
        Assert.Equal("lv_qi_001", result.ActionTargetId);
        Assert.Equal(3.5, result.ExploreProgressGain);
        Assert.Equal(2, result.BattleRoundsAdvanced);
        Assert.Equal(2, result.ItemDrops["lingqi_shard"]);
        Assert.Single(result.EquipmentDrops);
    }
}
