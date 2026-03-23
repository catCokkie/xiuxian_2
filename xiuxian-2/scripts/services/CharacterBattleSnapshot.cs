namespace Xiuxian.Scripts.Services
{
    public readonly record struct CharacterBattleSnapshot(
        int MaxHp,
        int CurrentHp,
        int Attack,
        int Defense,
        int Speed,
        double CritChance,
        double CritDamage);
}
