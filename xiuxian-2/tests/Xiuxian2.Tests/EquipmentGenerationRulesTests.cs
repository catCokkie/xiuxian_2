using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class EquipmentGenerationRulesTests
{
    [Fact]
    public void GenerateFromSpec_UsesWeightedMainStatAndRollRanges()
    {
        var spec = BuildSpec();

        EquipmentInstanceData instance = EquipmentGenerationRules.GenerateFromSpec(
            spec,
            sourceLevelId: "lv_qi_001",
            sourceStage: EquipmentSourceStage.Elite,
            rarityOverride: null,
            uniqueSuffix: "case001",
            pickIndex: totalWeight => 1,
            rollValue: (min, max) => max,
            nowUnix: () => 123456789);

        Assert.Equal("eq_weapon_qi_outer_moss_blade__case001", instance.EquipmentId);
        Assert.Equal("attack_flat", instance.MainStatKey);
        Assert.Equal(6, instance.MainStatValue);
        Assert.Equal(EquipmentRarityTier.Artifact, instance.RarityTier);
        Assert.Equal(EquipmentSourceStage.Elite, instance.SourceStage);
        Assert.Equal(123456789, instance.ObtainedUnix);
    }

    [Fact]
    public void GenerateFromSpec_AddsExpectedSubStatsForArtifactTier()
    {
        var spec = BuildSpec();

        EquipmentInstanceData instance = EquipmentGenerationRules.GenerateFromSpec(
            spec,
            sourceLevelId: "lv_qi_001",
            sourceStage: EquipmentSourceStage.Elite,
            rarityOverride: null,
            uniqueSuffix: "case002",
            pickIndex: totalWeight => totalWeight,
            rollValue: (min, _) => min,
            nowUnix: () => 1);

        Assert.Single(instance.SubStats);
        Assert.Equal("speed_flat", instance.SubStats[0].Stat);
        Assert.Equal(1, instance.SubStats[0].Value);
    }

    [Fact]
    public void GenerateFromSpec_CanOverrideRarityAndYieldNoSubStatsForCommonTool()
    {
        var spec = BuildSpec();

        EquipmentInstanceData instance = EquipmentGenerationRules.GenerateFromSpec(
            spec,
            sourceLevelId: "lv_qi_001",
            sourceStage: EquipmentSourceStage.Normal,
            rarityOverride: EquipmentRarityTier.CommonTool,
            uniqueSuffix: "case003",
            pickIndex: totalWeight => 1,
            rollValue: (min, _) => min,
            nowUnix: () => 1);

        Assert.Equal(EquipmentRarityTier.CommonTool, instance.RarityTier);
        Assert.Empty(instance.SubStats);
    }

    private static EquipmentGenerationRules.EquipmentTemplateGenerationSpec BuildSpec()
    {
        return new EquipmentGenerationRules.EquipmentTemplateGenerationSpec(
            EquipmentTemplateId: "eq_weapon_qi_outer_moss_blade",
            DisplayName: "苔锋短刃",
            Slot: EquipmentSlotType.Weapon,
            SeriesId: "series_qi_outer_cave",
            RarityTier: EquipmentRarityTier.Artifact,
            MainStatPool: new[]
            {
                new EquipmentGenerationRules.EquipmentStatRollDefinition("attack_flat", 70, 4, 6),
                new EquipmentGenerationRules.EquipmentStatRollDefinition("speed_flat", 30, 2, 3)
            },
            SubStatPool: new[]
            {
                new EquipmentGenerationRules.EquipmentStatRollDefinition("crit_chance_delta", 60, 0.01, 0.02),
                new EquipmentGenerationRules.EquipmentStatRollDefinition("speed_flat", 40, 1, 2)
            },
            PowerBudgetMin: 12,
            PowerBudgetMax: 16);
    }
}
