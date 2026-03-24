using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class BackpackEquipmentInstanceRules
    {
        public static void StoreInstance(
            Dictionary<string, EquipmentInstanceData> instances,
            Dictionary<string, EquipmentStatProfile> profiles,
            EquipmentInstanceData instance)
        {
            EquipmentInstanceData normalized = instance with { IsEquipped = false };
            instances[normalized.EquipmentId] = normalized;
            profiles[normalized.EquipmentId] = EquipmentInstanceRules.ToStatProfile(normalized) with { IsEquipped = false };
        }

        public static bool TryTakeBySlot(
            Dictionary<string, EquipmentInstanceData> instances,
            Dictionary<string, EquipmentStatProfile> profiles,
            EquipmentSlotType slot,
            out EquipmentInstanceData instance,
            out EquipmentStatProfile profile)
        {
            foreach (EquipmentInstanceData item in instances.Values)
            {
                if (item.Slot != slot)
                {
                    continue;
                }

                instance = item with { IsEquipped = true };
                profile = EquipmentInstanceRules.ToStatProfile(instance);
                instances.Remove(item.EquipmentId);
                profiles.Remove(item.EquipmentId);
                return true;
            }

            instance = default;
            profile = default;
            return false;
        }
    }
}
