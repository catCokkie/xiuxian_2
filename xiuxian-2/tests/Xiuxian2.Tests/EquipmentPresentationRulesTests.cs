using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class EquipmentPresentationRulesTests
{
    [Fact]
    public void BuildEquipmentPageText_ShowsInstanceRaritySourceAndSubStats()
    {
        string text = EquipmentPresentationRules.BuildEquipmentPageText(
            new CharacterStatBlock(80, 10, 3, 100, 0.05, 1.5),
            new CharacterStatBlock(92, 14, 5, 100, 0.07, 1.5),
            new[]
            {
                new EquipmentStatProfile("starter_sword", "练气短剑", EquipmentSlotType.Weapon, new CharacterStatModifier(AttackFlat: 3))
            },
            new[]
            {
                new EquipmentInstanceData(
                    "eq_inst_001",
                    "eq_weapon_qi_outer_moss_blade",
                    "苔锋短刃",
                    EquipmentSlotType.Weapon,
                    "series_qi_outer_cave",
                    EquipmentRarityTier.Artifact,
                    EquipmentSourceStage.Elite,
                    "lv_qi_001",
                    "attack_flat",
                    5,
                    new[] { new EquipmentSubStatData("crit_chance_delta", 0.02) },
                    0,
                    12,
                    123,
                    false)
            },
            Array.Empty<EquipmentStatProfile>());

        Assert.Contains("法器", text);
        Assert.Contains("精英掉落", text);
        Assert.Contains("主属性：攻击+5", text);
        Assert.Contains("副属性：暴击+2 %", text.Replace("%", " %"));
        Assert.Contains("对比当前武器：更强", text);
        Assert.Contains("攻击+2", text);
        Assert.Contains("暴击+2 %", text.Replace("%", " %"));
    }

    [Fact]
    public void BuildEquipmentPageText_FallsBackForLegacyProfiles()
    {
        string text = EquipmentPresentationRules.BuildEquipmentPageText(
            new CharacterStatBlock(80, 10, 3, 100, 0.05, 1.5),
            new CharacterStatBlock(80, 10, 3, 100, 0.05, 1.5),
            Array.Empty<EquipmentStatProfile>(),
            Array.Empty<EquipmentInstanceData>(),
            new[]
            {
                new EquipmentStatProfile("legacy_001", "旧护符", EquipmentSlotType.Accessory, new CharacterStatModifier(CritChanceDelta: 0.03), IsEquipped: false)
            });

        Assert.Contains("旧版装备", text);
        Assert.Contains("暴击+3 %", text.Replace("%", " %"));
    }

    [Fact]
    public void BuildEquipmentPageText_ShowsWeakerHintForInferiorSameSlotItem()
    {
        string text = EquipmentPresentationRules.BuildEquipmentPageText(
            new CharacterStatBlock(80, 10, 3, 100, 0.05, 1.5),
            new CharacterStatBlock(92, 14, 5, 100, 0.07, 1.5),
            new[]
            {
                new EquipmentStatProfile("starter_sword", "练气短剑", EquipmentSlotType.Weapon, new CharacterStatModifier(AttackFlat: 6), IsEquipped: true)
            },
            new[]
            {
                new EquipmentInstanceData(
                    "eq_inst_002",
                    "eq_weapon_qi_outer_bone_dagger",
                    "骨柄短匕",
                    EquipmentSlotType.Weapon,
                    "series_qi_outer_cave",
                    EquipmentRarityTier.CommonTool,
                    EquipmentSourceStage.Normal,
                    "lv_qi_001",
                    "attack_flat",
                    2,
                    Array.Empty<EquipmentSubStatData>(),
                    0,
                    6,
                    123,
                    false)
            },
            Array.Empty<EquipmentStatProfile>());

        Assert.Contains("对比当前武器：更弱", text);
        Assert.Contains("攻击-4", text);
    }
}
