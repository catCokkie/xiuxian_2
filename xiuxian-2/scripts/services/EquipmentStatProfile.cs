namespace Xiuxian.Scripts.Services
{
    public readonly record struct EquipmentStatProfile(
        string EquipmentId,
        string DisplayName,
        EquipmentSlotType Slot,
        CharacterStatModifier Modifier,
        string SetTag = "",
        int Rarity = 1,
        int EnhanceLevel = 0,
        bool IsEquipped = true)
    {
        public CharacterStatModifier ToModifier()
        {
            return IsEquipped ? Modifier : default;
        }
    }
}
