using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class EquipmentDropResolutionRulesTests
{
    [Fact]
    public void ResolveEntry_PicksEquipmentTemplateWithoutAddingItemCount()
    {
        var entry = new EquipmentDropResolutionRules.DropEntrySpec(
            EntryType: "equipment_template",
            ItemId: "",
            EquipmentTemplateId: "eq_weapon_qi_outer_moss_blade",
            Weight: 20,
            MinQty: 1,
            MaxQty: 1);

        EquipmentDropResolutionRules.DropResolveResult result = EquipmentDropResolutionRules.ResolveEntry(entry, (min, max) => max);

        Assert.Null(result.ItemId);
        Assert.Equal("eq_weapon_qi_outer_moss_blade", result.EquipmentTemplateId);
        Assert.Equal(1, result.Quantity);
    }

    [Fact]
    public void ResolveEntry_PicksItemDropWithQuantityRange()
    {
        var entry = new EquipmentDropResolutionRules.DropEntrySpec(
            EntryType: "item",
            ItemId: "lingqi_shard",
            EquipmentTemplateId: "",
            Weight: 80,
            MinQty: 2,
            MaxQty: 4);

        EquipmentDropResolutionRules.DropResolveResult result = EquipmentDropResolutionRules.ResolveEntry(entry, (min, max) => min);

        Assert.Equal("lingqi_shard", result.ItemId);
        Assert.Null(result.EquipmentTemplateId);
        Assert.Equal(2, result.Quantity);
    }

    [Fact]
    public void PickWeightedEntry_SupportsMixedItemAndEquipmentEntries()
    {
        var entries = new[]
        {
            new EquipmentDropResolutionRules.DropEntrySpec("item", "lingqi_shard", "", 70, 1, 2),
            new EquipmentDropResolutionRules.DropEntrySpec("equipment_template", "", "eq_weapon_qi_outer_moss_blade", 30, 1, 1)
        };

        EquipmentDropResolutionRules.DropEntrySpec picked = EquipmentDropResolutionRules.PickWeightedEntry(entries, totalWeight => totalWeight);

        Assert.Equal("equipment_template", picked.EntryType);
        Assert.Equal("eq_weapon_qi_outer_moss_blade", picked.EquipmentTemplateId);
    }
}
