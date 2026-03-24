using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentGenerationRules
    {
        public readonly record struct EquipmentStatRollDefinition(
            string Stat,
            int Weight,
            double Min,
            double Max);

        public sealed record EquipmentTemplateGenerationSpec(
            string EquipmentTemplateId,
            string DisplayName,
            EquipmentSlotType Slot,
            string SeriesId,
            EquipmentRarityTier RarityTier,
            IReadOnlyList<EquipmentStatRollDefinition> MainStatPool,
            IReadOnlyList<EquipmentStatRollDefinition> SubStatPool,
            int PowerBudgetMin,
            int PowerBudgetMax);

        public static EquipmentInstanceData GenerateFromSpec(
            EquipmentTemplateGenerationSpec spec,
            string sourceLevelId,
            EquipmentSourceStage sourceStage,
            EquipmentRarityTier? rarityOverride = null,
            string uniqueSuffix = "",
            Func<int, int>? pickIndex = null,
            Func<double, double, double>? rollValue = null,
            Func<long>? nowUnix = null)
        {
            EquipmentRarityTier rarity = rarityOverride ?? spec.RarityTier;
            pickIndex ??= totalWeight => 1;
            rollValue ??= (min, _) => min;
            nowUnix ??= () => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string equipmentId = string.IsNullOrEmpty(uniqueSuffix) ? spec.EquipmentTemplateId : $"{spec.EquipmentTemplateId}__{uniqueSuffix}";
            int powerBudget = (int)Math.Round(rollValue(spec.PowerBudgetMin, Math.Max(spec.PowerBudgetMin, spec.PowerBudgetMax)));

            EquipmentStatRollDefinition main = PickWeightedEntry(spec.MainStatPool, pickIndex);
            List<EquipmentSubStatData> subStats = GenerateSubStats(spec.SubStatPool, rarity, pickIndex, rollValue);

            return new EquipmentInstanceData(
                EquipmentId: equipmentId,
                EquipmentTemplateId: spec.EquipmentTemplateId,
                DisplayName: spec.DisplayName,
                Slot: spec.Slot,
                SeriesId: spec.SeriesId,
                RarityTier: rarity,
                SourceStage: sourceStage,
                SourceLevelId: sourceLevelId,
                MainStatKey: main.Stat,
                MainStatValue: rollValue(main.Min, Math.Max(main.Min, main.Max)),
                SubStats: subStats,
                EnhanceLevel: 0,
                PowerBudget: powerBudget,
                ObtainedUnix: nowUnix(),
                IsEquipped: false);
        }

        public static EquipmentTemplateGenerationSpec FromTemplateDictionary(Godot.Collections.Dictionary<string, Variant> template)
        {
            return new EquipmentTemplateGenerationSpec(
                GetString(template, "equipment_template_id", "unknown_template"),
                GetString(template, "display_name", "unknown_template"),
                ParseSlot(GetString(template, "slot", EquipmentSlotType.Weapon.ToString())),
                GetString(template, "series_id", ""),
                ParseRarity(GetString(template, "rarity_tier", "artifact")),
                ReadPool(template, "main_stat_pool"),
                ReadPool(template, "sub_stat_pool"),
                GetInt(template, "power_budget_min", 0),
                GetInt(template, "power_budget_max", 0));
        }

        private static List<EquipmentSubStatData> GenerateSubStats(
            IReadOnlyList<EquipmentStatRollDefinition> pool,
            EquipmentRarityTier rarity,
            Func<int, int> pickIndex,
            Func<double, double, double> rollValue)
        {
            int targetCount = rarity switch
            {
                EquipmentRarityTier.CommonTool => 0,
                EquipmentRarityTier.Artifact => 1,
                EquipmentRarityTier.Spirit => 1,
                EquipmentRarityTier.Treasure => 2,
                _ => 0,
            };

            var result = new List<EquipmentSubStatData>();
            var usedStats = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < targetCount; i++)
            {
                EquipmentStatRollDefinition entry = PickWeightedEntry(pool, pickIndex, usedStats);
                if (string.IsNullOrEmpty(entry.Stat))
                {
                    break;
                }

                usedStats.Add(entry.Stat);
                result.Add(new EquipmentSubStatData(entry.Stat, rollValue(entry.Min, Math.Max(entry.Min, entry.Max))));
            }

            return result;
        }

        private static EquipmentStatRollDefinition PickWeightedEntry(
            IReadOnlyList<EquipmentStatRollDefinition> pool,
            Func<int, int> pickIndex,
            HashSet<string>? excludedStats = null)
        {
            int totalWeight = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                if (excludedStats != null && excludedStats.Contains(pool[i].Stat))
                {
                    continue;
                }

                totalWeight += Math.Max(0, pool[i].Weight);
            }

            if (totalWeight <= 0)
            {
                return default;
            }

            int roll = Math.Clamp(pickIndex(totalWeight), 1, totalWeight);
            int acc = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                EquipmentStatRollDefinition entry = pool[i];
                if (excludedStats != null && excludedStats.Contains(entry.Stat))
                {
                    continue;
                }

                if (entry.Weight <= 0)
                {
                    continue;
                }

                acc += entry.Weight;
                if (roll <= acc)
                {
                    return entry;
                }
            }

            return default;
        }

        private static List<EquipmentStatRollDefinition> ReadPool(Godot.Collections.Dictionary<string, Variant> template, string key)
        {
            var result = new List<EquipmentStatRollDefinition>();
            if (!template.ContainsKey(key) || template[key].VariantType != Variant.Type.Array)
            {
                return result;
            }

            var array = (Godot.Collections.Array<Variant>)template[key];
            foreach (Variant item in array)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                result.Add(new EquipmentStatRollDefinition(
                    GetString(dict, "stat", ""),
                    GetInt(dict, "weight", 0),
                    GetDouble(dict, "min", 0.0),
                    GetDouble(dict, "max", 0.0)));
            }

            return result;
        }

        private static string GetString(Godot.Collections.Dictionary<string, Variant> dict, string key, string fallback)
        {
            return dict.ContainsKey(key) ? dict[key].AsString() : fallback;
        }

        private static int GetInt(Godot.Collections.Dictionary<string, Variant> dict, string key, int fallback)
        {
            return dict.ContainsKey(key) ? dict[key].AsInt32() : fallback;
        }

        private static double GetDouble(Godot.Collections.Dictionary<string, Variant> dict, string key, double fallback)
        {
            return dict.ContainsKey(key) ? dict[key].AsDouble() : fallback;
        }

        private static EquipmentSlotType ParseSlot(string slot)
        {
            return Enum.TryParse(slot, true, out EquipmentSlotType parsed) ? parsed : EquipmentSlotType.Weapon;
        }

        private static EquipmentRarityTier ParseRarity(string rarity)
        {
            return rarity switch
            {
                "common_tool" => EquipmentRarityTier.CommonTool,
                "artifact" => EquipmentRarityTier.Artifact,
                "spirit" => EquipmentRarityTier.Spirit,
                "treasure" => EquipmentRarityTier.Treasure,
                _ => EquipmentRarityTier.Artifact,
            };
        }
    }
}
