using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class EquipmentInstanceRulesTests
{
    [Fact]
    public void BuildModifier_CombinesMainAndSubStats()
    {
        var instance = new EquipmentInstanceData(
            EquipmentId: "eq_instance_001",
            EquipmentTemplateId: "eq_weapon_qi_outer_moss_blade",
            DisplayName: "苔锋短刃",
            Slot: EquipmentSlotType.Weapon,
            SeriesId: "series_qi_outer_cave",
            RarityTier: EquipmentRarityTier.Artifact,
            SourceStage: EquipmentSourceStage.Elite,
            SourceLevelId: "lv_qi_001",
            MainStatKey: "attack_flat",
            MainStatValue: 5,
            SubStats: new[]
            {
                new EquipmentSubStatData("crit_chance_delta", 0.02),
                new EquipmentSubStatData("speed_flat", 2)
            },
            EnhanceLevel: 0,
            PowerBudget: 14,
            ObtainedUnix: 123456789,
            IsEquipped: true);

        CharacterStatModifier modifier = EquipmentInstanceRules.BuildModifier(instance);

        Assert.Equal(5, modifier.AttackFlat);
        Assert.Equal(2, modifier.SpeedFlat);
        Assert.Equal(0.02, modifier.CritChanceDelta, 6);
    }

    [Fact]
    public void ToStatProfile_MapsInstanceIntoExistingCombatProfile()
    {
        var instance = new EquipmentInstanceData(
            EquipmentId: "eq_instance_002",
            EquipmentTemplateId: "eq_armor_qi_outer_moss_robe",
            DisplayName: "苔纹护袍",
            Slot: EquipmentSlotType.Armor,
            SeriesId: "series_qi_outer_cave",
            RarityTier: EquipmentRarityTier.Spirit,
            SourceStage: EquipmentSourceStage.FirstClear,
            SourceLevelId: "lv_qi_001",
            MainStatKey: "max_hp_flat",
            MainStatValue: 16,
            SubStats: new[]
            {
                new EquipmentSubStatData("defense_flat", 2)
            },
            EnhanceLevel: 1,
            PowerBudget: 18,
            ObtainedUnix: 123456789,
            IsEquipped: false);

        EquipmentStatProfile profile = EquipmentInstanceRules.ToStatProfile(instance);

        Assert.Equal(instance.EquipmentId, profile.EquipmentId);
        Assert.Equal(instance.DisplayName, profile.DisplayName);
        Assert.Equal(instance.Slot, profile.Slot);
        Assert.Equal(16, profile.Modifier.MaxHpFlat);
        Assert.Equal(2, profile.Modifier.DefenseFlat);
        Assert.Equal((int)EquipmentRarityTier.Spirit, profile.Rarity);
        Assert.False(profile.IsEquipped);
        Assert.Equal(instance.SeriesId, profile.SetTag);
    }

    [Fact]
    public void BuildModifier_IgnoresUnknownSubStats()
    {
        var instance = new EquipmentInstanceData(
            EquipmentId: "eq_instance_003",
            EquipmentTemplateId: "eq_accessory_unknown",
            DisplayName: "未知坠饰",
            Slot: EquipmentSlotType.Accessory,
            SeriesId: "series_unknown",
            RarityTier: EquipmentRarityTier.CommonTool,
            SourceStage: EquipmentSourceStage.Normal,
            SourceLevelId: "lv_qi_001",
            MainStatKey: "unknown_stat",
            MainStatValue: 999,
            SubStats: new[]
            {
                new EquipmentSubStatData("unknown_sub_stat", 5)
            },
            EnhanceLevel: 0,
            PowerBudget: 1,
            ObtainedUnix: 123456789,
            IsEquipped: true);

        CharacterStatModifier modifier = EquipmentInstanceRules.BuildModifier(instance);

        Assert.Equal(default, modifier);
    }
}
