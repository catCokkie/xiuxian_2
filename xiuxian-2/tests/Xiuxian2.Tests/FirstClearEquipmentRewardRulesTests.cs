using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class FirstClearEquipmentRewardRulesTests
{
    [Fact]
    public void GenerateInstances_BuildsConfiguredRewardInstances()
    {
        var specs = new Dictionary<string, EquipmentGenerationRules.EquipmentTemplateGenerationSpec>
        {
            ["eq_armor_qi_outer_moss_robe"] = BuildSpec()
        };
        var rewards = new[]
        {
            new FirstClearEquipmentRewardRules.FirstClearEquipmentRewardSpec(
                "eq_armor_qi_outer_moss_robe",
                EquipmentRarityTier.Spirit,
                2)
        };

        EquipmentInstanceData[] instances = FirstClearEquipmentRewardRules.GenerateInstances(
            specs,
            rewards,
            sourceLevelId: "lv_qi_001",
            pickIndex: totalWeight => 1,
            rollValue: (min, max) => max,
            nowUnix: () => 123456789);

        Assert.Equal(2, instances.Length);
        Assert.All(instances, item => Assert.Equal(EquipmentSourceStage.FirstClear, item.SourceStage));
        Assert.All(instances, item => Assert.Equal(EquipmentRarityTier.Spirit, item.RarityTier));
        Assert.Equal("eq_armor_qi_outer_moss_robe__first_clear_1", instances[0].EquipmentId);
        Assert.Equal("eq_armor_qi_outer_moss_robe__first_clear_2", instances[1].EquipmentId);
    }

    [Fact]
    public void GenerateInstances_SkipsUnknownTemplates()
    {
        EquipmentInstanceData[] instances = FirstClearEquipmentRewardRules.GenerateInstances(
            new Dictionary<string, EquipmentGenerationRules.EquipmentTemplateGenerationSpec>(),
            new[]
            {
                new FirstClearEquipmentRewardRules.FirstClearEquipmentRewardSpec("missing", null, 1)
            },
            sourceLevelId: "lv_qi_001",
            pickIndex: totalWeight => 1,
            rollValue: (min, max) => min,
            nowUnix: () => 1);

        Assert.Empty(instances);
    }

    private static EquipmentGenerationRules.EquipmentTemplateGenerationSpec BuildSpec()
    {
        return new EquipmentGenerationRules.EquipmentTemplateGenerationSpec(
            EquipmentTemplateId: "eq_armor_qi_outer_moss_robe",
            DisplayName: "苔纹护袍",
            Slot: EquipmentSlotType.Armor,
            SeriesId: "series_qi_outer_cave",
            RarityTier: EquipmentRarityTier.Artifact,
            MainStatPool: new[]
            {
                new EquipmentGenerationRules.EquipmentStatRollDefinition("max_hp_flat", 100, 12, 16)
            },
            SubStatPool: new[]
            {
                new EquipmentGenerationRules.EquipmentStatRollDefinition("defense_flat", 100, 1, 2)
            },
            PowerBudgetMin: 11,
            PowerBudgetMax: 15);
    }
}
