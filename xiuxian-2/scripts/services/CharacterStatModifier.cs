namespace Xiuxian.Scripts.Services
{
    public readonly record struct CharacterStatModifier(
        int MaxHpFlat = 0,
        int AttackFlat = 0,
        int DefenseFlat = 0,
        int SpeedFlat = 0,
        double MaxHpRate = 0.0,
        double AttackRate = 0.0,
        double DefenseRate = 0.0,
        double SpeedRate = 0.0,
        double CritChanceDelta = 0.0,
        double CritDamageDelta = 0.0);
}
