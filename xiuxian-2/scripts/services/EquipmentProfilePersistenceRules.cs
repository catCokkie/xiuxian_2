using System;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentProfilePersistenceRules
    {
        public static EquipmentProfilePersistenceData ToData(EquipmentStatProfile profile)
        {
            return new EquipmentProfilePersistenceData(
                profile.EquipmentId,
                profile.DisplayName,
                profile.Slot.ToString(),
                profile.SetTag,
                profile.Rarity,
                profile.EnhanceLevel,
                profile.IsEquipped,
                profile.Modifier.MaxHpFlat,
                profile.Modifier.AttackFlat,
                profile.Modifier.DefenseFlat,
                profile.Modifier.SpeedFlat,
                profile.Modifier.MaxHpRate,
                profile.Modifier.AttackRate,
                profile.Modifier.DefenseRate,
                profile.Modifier.SpeedRate,
                profile.Modifier.CritChanceDelta,
                profile.Modifier.CritDamageDelta);
        }

        public static EquipmentStatProfile FromData(EquipmentProfilePersistenceData data)
        {
            Enum.TryParse(data.Slot, out EquipmentSlotType slot);
            return new EquipmentStatProfile(
                data.EquipmentId,
                data.DisplayName,
                slot,
                new CharacterStatModifier(
                    MaxHpFlat: data.MaxHpFlat,
                    AttackFlat: data.AttackFlat,
                    DefenseFlat: data.DefenseFlat,
                    SpeedFlat: data.SpeedFlat,
                    MaxHpRate: data.MaxHpRate,
                    AttackRate: data.AttackRate,
                    DefenseRate: data.DefenseRate,
                    SpeedRate: data.SpeedRate,
                    CritChanceDelta: data.CritChanceDelta,
                    CritDamageDelta: data.CritDamageDelta),
                data.SetTag,
                data.Rarity,
                data.EnhanceLevel,
                data.IsEquipped);
        }
    }
}
