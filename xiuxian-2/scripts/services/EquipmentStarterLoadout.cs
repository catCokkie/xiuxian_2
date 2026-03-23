namespace Xiuxian.Scripts.Services
{
    public static class EquipmentStarterLoadout
    {
        public static EquipmentStatProfile[] CreateDefaultProfiles()
        {
            return new[]
            {
                new EquipmentStatProfile(
                    "starter_sword",
                    "练气短剑",
                    EquipmentSlotType.Weapon,
                    new CharacterStatModifier(AttackFlat: 3, CritChanceDelta: 0.02),
                    SetTag: "starter",
                    Rarity: 1,
                    EnhanceLevel: 0,
                    IsEquipped: true),
                new EquipmentStatProfile(
                    "starter_robe",
                    "练气布袍",
                    EquipmentSlotType.Armor,
                    new CharacterStatModifier(MaxHpFlat: 12, DefenseFlat: 2),
                    SetTag: "starter",
                    Rarity: 1,
                    EnhanceLevel: 0,
                    IsEquipped: true)
            };
        }

        public static EquipmentStatProfile CreateAlternateProfile(EquipmentSlotType slot)
        {
            return slot switch
            {
                EquipmentSlotType.Weapon => new EquipmentStatProfile(
                    "debug_spear",
                    "试炼短枪",
                    EquipmentSlotType.Weapon,
                    new CharacterStatModifier(AttackFlat: 5, SpeedFlat: 4),
                    SetTag: "debug",
                    Rarity: 1,
                    EnhanceLevel: 0,
                    IsEquipped: true),
                EquipmentSlotType.Armor => new EquipmentStatProfile(
                    "debug_vest",
                    "试炼护甲",
                    EquipmentSlotType.Armor,
                    new CharacterStatModifier(MaxHpFlat: 18, DefenseFlat: 3),
                    SetTag: "debug",
                    Rarity: 1,
                    EnhanceLevel: 0,
                    IsEquipped: true),
                _ => new EquipmentStatProfile(
                    "debug_charm",
                    "试炼护符",
                    EquipmentSlotType.Accessory,
                    new CharacterStatModifier(CritChanceDelta: 0.03),
                    SetTag: "debug",
                    Rarity: 1,
                    EnhanceLevel: 0,
                    IsEquipped: true)
            };
        }

        public static EquipmentStatProfile GetDebugSwapProfile(EquipmentSlotType slot, string currentEquipmentId)
        {
            EquipmentStatProfile starter = slot switch
            {
                EquipmentSlotType.Weapon => CreateDefaultProfiles()[0],
                EquipmentSlotType.Armor => CreateDefaultProfiles()[1],
                _ => CreateAlternateProfile(slot)
            };

            EquipmentStatProfile alternate = CreateAlternateProfile(slot);
            return currentEquipmentId == starter.EquipmentId ? alternate : starter;
        }
    }
}
