using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class OfflineSummaryPresentationRulesTests
{
    [Fact]
    public void BuildCultivationSummary_FormatsResourceGains()
    {
        ActionSettlementResult result = ActionSettlementRules.BuildCultivationSettlement(
            actionTargetId: string.Empty,
            apConsumed: 240,
            lingqiGain: 120,
            insightGain: 8,
            petAffinityGain: 3,
            realmExpGain: 30);

        Assert.Equal("离线修炼完成", OfflineSummaryPresentationRules.BuildTitle(result));
        string body = OfflineSummaryPresentationRules.BuildBody(result);
        Assert.Contains("灵气+120", body);
        Assert.Contains("悟性+8", body);
        Assert.Contains("境界经验+30", body);
    }

    [Fact]
    public void BuildDungeonSummary_FormatsProgressAndDrops()
    {
        ActionSettlementResult result = ActionSettlementRules.BuildDungeonRewardSettlement(
            actionTargetId: "lv_qi_001",
            sourceTag: "offline_dungeon",
            exploreProgressGain: 120.0,
            battleRoundsAdvanced: 6,
            itemDrops: new Dictionary<string, int> { ["lingqi_shard"] = 3 },
            equipmentDrops: new[]
            {
                new EquipmentInstanceData("eq1", "tpl", "苔锋短刃", EquipmentSlotType.Weapon, "s", EquipmentRarityTier.Artifact, EquipmentSourceStage.Normal, "lv_qi_001", "attack_flat", 5, System.Array.Empty<EquipmentSubStatData>(), 0, 10, 1, false)
            },
            lingqiGain: 20,
            insightGain: 2);

        Assert.Equal("离线副本完成", OfflineSummaryPresentationRules.BuildTitle(result));
        string body = OfflineSummaryPresentationRules.BuildBody(result);
        Assert.Contains("探索推进+120.0", body);
        Assert.Contains("遭遇6次", body);
        Assert.Contains("lingqi_shard+3", body);
        Assert.Contains("装备+1", body);
    }
}
