using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentInstanceCodec
    {
        public static Godot.Collections.Dictionary<string, Variant> ToDictionary(EquipmentInstanceData instance)
        {
            var subStats = new Godot.Collections.Array<Variant>();
            IReadOnlyList<EquipmentSubStatData> items = instance.SubStats ?? System.Array.Empty<EquipmentSubStatData>();
            for (int i = 0; i < items.Count; i++)
            {
                subStats.Add(new Godot.Collections.Dictionary<string, Variant>
                {
                    ["stat"] = items[i].Stat,
                    ["value"] = items[i].Value,
                });
            }

            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["equipment_id"] = instance.EquipmentId,
                ["equipment_template_id"] = instance.EquipmentTemplateId,
                ["display_name"] = instance.DisplayName,
                ["slot"] = instance.Slot.ToString(),
                ["series_id"] = instance.SeriesId,
                ["rarity_tier"] = instance.RarityTier.ToString(),
                ["source_stage"] = instance.SourceStage.ToString(),
                ["source_level_id"] = instance.SourceLevelId,
                ["main_stat_key"] = instance.MainStatKey,
                ["main_stat_value"] = instance.MainStatValue,
                ["sub_stats"] = subStats,
                ["enhance_level"] = instance.EnhanceLevel,
                ["power_budget"] = instance.PowerBudget,
                ["obtained_unix"] = instance.ObtainedUnix,
                ["is_equipped"] = instance.IsEquipped,
            };
        }

        public static EquipmentInstanceData FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            var subStats = new List<EquipmentSubStatData>();
            if (data.ContainsKey("sub_stats") && data["sub_stats"].VariantType == Variant.Type.Array)
            {
                var array = (Godot.Collections.Array<Variant>)data["sub_stats"];
                foreach (Variant item in array)
                {
                    if (item.VariantType != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                    subStats.Add(new EquipmentSubStatData(
                        dict.ContainsKey("stat") ? dict["stat"].AsString() : string.Empty,
                        dict.ContainsKey("value") ? dict["value"].AsDouble() : 0.0));
                }
            }

            return new EquipmentInstanceData(
                EquipmentId: data.ContainsKey("equipment_id") ? data["equipment_id"].AsString() : string.Empty,
                EquipmentTemplateId: data.ContainsKey("equipment_template_id") ? data["equipment_template_id"].AsString() : string.Empty,
                DisplayName: data.ContainsKey("display_name") ? data["display_name"].AsString() : string.Empty,
                Slot: ParseSlot(data.ContainsKey("slot") ? data["slot"].AsString() : string.Empty),
                SeriesId: data.ContainsKey("series_id") ? data["series_id"].AsString() : string.Empty,
                RarityTier: ParseRarity(data.ContainsKey("rarity_tier") ? data["rarity_tier"].AsString() : string.Empty),
                SourceStage: ParseSourceStage(data.ContainsKey("source_stage") ? data["source_stage"].AsString() : string.Empty),
                SourceLevelId: data.ContainsKey("source_level_id") ? data["source_level_id"].AsString() : string.Empty,
                MainStatKey: data.ContainsKey("main_stat_key") ? data["main_stat_key"].AsString() : string.Empty,
                MainStatValue: data.ContainsKey("main_stat_value") ? data["main_stat_value"].AsDouble() : 0.0,
                SubStats: subStats,
                EnhanceLevel: data.ContainsKey("enhance_level") ? data["enhance_level"].AsInt32() : 0,
                PowerBudget: data.ContainsKey("power_budget") ? data["power_budget"].AsInt32() : 0,
                ObtainedUnix: data.ContainsKey("obtained_unix") ? data["obtained_unix"].AsInt64() : 0,
                IsEquipped: data.ContainsKey("is_equipped") && data["is_equipped"].AsBool());
        }

        private static EquipmentSlotType ParseSlot(string slot)
        {
            return System.Enum.TryParse(slot, true, out EquipmentSlotType parsed) ? parsed : EquipmentSlotType.Weapon;
        }

        private static EquipmentRarityTier ParseRarity(string rarity)
        {
            return System.Enum.TryParse(rarity, true, out EquipmentRarityTier parsed) ? parsed : EquipmentRarityTier.Artifact;
        }

        private static EquipmentSourceStage ParseSourceStage(string sourceStage)
        {
            return System.Enum.TryParse(sourceStage, true, out EquipmentSourceStage parsed) ? parsed : EquipmentSourceStage.Normal;
        }
    }
}
