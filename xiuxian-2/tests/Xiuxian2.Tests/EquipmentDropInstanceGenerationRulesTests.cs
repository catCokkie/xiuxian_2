using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class EquipmentDropInstanceGenerationRulesTests
{
    [Fact]
    public void GenerateInstances_CreatesConcreteInstancesFromTemplateHits()
    {
        var specs = new Dictionary<string, EquipmentGenerationRules.EquipmentTemplateGenerationSpec>
        {
            ["eq_weapon_qi_outer_moss_blade"] = BuildSpec()
        };

        EquipmentInstanceData[] instances = EquipmentDropInstanceGenerationRules.GenerateInstances(
            specs,
            new[] { "eq_weapon_qi_outer_moss_blade" },
            sourceLevelId: "lv_qi_001",
            sourceStage: EquipmentSourceStage.Elite,
            pickIndex: totalWeight => 1,
            rollValue: (min, max) => max,
            nowUnix: () => 123456789);

        Assert.Single(instances);
        Assert.Equal("eq_weapon_qi_outer_moss_blade__drop_1", instances[0].EquipmentId);
        Assert.Equal("attack_flat", instances[0].MainStatKey);
        Assert.Equal(6, instances[0].MainStatValue);
    }

    [Fact]
    public void GenerateInstances_SkipsUnknownTemplateIds()
    {
        EquipmentInstanceData[] instances = EquipmentDropInstanceGenerationRules.GenerateInstances(
            new Dictionary<string, EquipmentGenerationRules.EquipmentTemplateGenerationSpec>(),
            new[] { "eq_missing" },
            sourceLevelId: "lv_qi_001",
            sourceStage: EquipmentSourceStage.Elite,
            pickIndex: totalWeight => 1,
            rollValue: (min, max) => min,
            nowUnix: () => 1);

        Assert.Empty(instances);
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
                new EquipmentGenerationRules.EquipmentStatRollDefinition("attack_flat", 70, 4, 6)
            },
            SubStatPool: new[]
            {
                new EquipmentGenerationRules.EquipmentStatRollDefinition("crit_chance_delta", 60, 0.01, 0.02)
            },
            PowerBudgetMin: 12,
            PowerBudgetMax: 16);
    }
}
