namespace Xiuxian.Scripts.Services
{
    public readonly record struct EquipmentProfilePersistenceData(
        string EquipmentId,
        string DisplayName,
        string Slot,
        string SetTag,
        int Rarity,
        int EnhanceLevel,
        bool IsEquipped,
        int MaxHpFlat,
        int AttackFlat,
        int DefenseFlat,
        int SpeedFlat,
        double MaxHpRate,
        double AttackRate,
        double DefenseRate,
        double SpeedRate,
        double CritChanceDelta,
        double CritDamageDelta);
}
