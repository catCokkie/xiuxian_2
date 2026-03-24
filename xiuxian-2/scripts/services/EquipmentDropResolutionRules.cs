using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentDropResolutionRules
    {
        public readonly record struct DropEntrySpec(
            string EntryType,
            string ItemId,
            string EquipmentTemplateId,
            int Weight,
            int MinQty,
            int MaxQty);

        public readonly record struct DropResolveResult(
            string? ItemId,
            string? EquipmentTemplateId,
            int Quantity);

        public static DropEntrySpec PickWeightedEntry(IReadOnlyList<DropEntrySpec> entries, Func<int, int> pickIndex)
        {
            int totalWeight = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                totalWeight += Math.Max(0, entries[i].Weight);
            }

            if (totalWeight <= 0)
            {
                return default;
            }

            int roll = Math.Clamp(pickIndex(totalWeight), 1, totalWeight);
            int acc = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                DropEntrySpec entry = entries[i];
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

        public static DropResolveResult ResolveEntry(DropEntrySpec entry, Func<int, int, int> rollQuantity)
        {
            int qty = rollQuantity(Math.Max(0, entry.MinQty), Math.Max(entry.MinQty, entry.MaxQty));
            return entry.EntryType switch
            {
                "equipment_template" => new DropResolveResult(null, entry.EquipmentTemplateId, Math.Max(1, qty)),
                _ => new DropResolveResult(entry.ItemId, null, Math.Max(0, qty)),
            };
        }
    }
}
