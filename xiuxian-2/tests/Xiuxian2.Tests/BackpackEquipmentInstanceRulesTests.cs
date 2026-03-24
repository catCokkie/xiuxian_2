using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class BackpackEquipmentInstanceRulesTests
{
    [Fact]
    public void StoreInstance_AlsoCreatesProfileProjection()
    {
        var instances = new Dictionary<string, EquipmentInstanceData>();
        var profiles = new Dictionary<string, EquipmentStatProfile>();
        EquipmentInstanceData instance = BuildInstance();

        BackpackEquipmentInstanceRules.StoreInstance(instances, profiles, instance);

        Assert.True(instances.ContainsKey(instance.EquipmentId));
        Assert.True(profiles.ContainsKey(instance.EquipmentId));
        Assert.Equal(5, profiles[instance.EquipmentId].Modifier.AttackFlat);
        Assert.False(profiles[instance.EquipmentId].IsEquipped);
    }

    [Fact]
    public void TryTakeBySlot_RemovesStoredInstanceAndReturnsEquippedProjection()
    {
        var instances = new Dictionary<string, EquipmentInstanceData>();
        var profiles = new Dictionary<string, EquipmentStatProfile>();
        EquipmentInstanceData instance = BuildInstance();
        BackpackEquipmentInstanceRules.StoreInstance(instances, profiles, instance);

        bool taken = BackpackEquipmentInstanceRules.TryTakeBySlot(instances, profiles, EquipmentSlotType.Weapon, out var equippedInstance, out var profile);

        Assert.True(taken);
        Assert.True(equippedInstance.IsEquipped);
        Assert.True(profile.IsEquipped);
        Assert.Empty(instances);
        Assert.Empty(profiles);
    }

    private static EquipmentInstanceData BuildInstance()
    {
        return new EquipmentInstanceData(
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
            SubStats: Array.Empty<EquipmentSubStatData>(),
            EnhanceLevel: 0,
            PowerBudget: 12,
            ObtainedUnix: 123,
            IsEquipped: false);
    }
}
