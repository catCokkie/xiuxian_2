namespace Xiuxian.Scripts.Services
{
    public readonly record struct CharacterStatBlock(
        int MaxHp,
        int Attack,
        int Defense,
        int Speed,
        double CritChance,
        double CritDamage)
    {
        public static CharacterStatBlock Empty => new(0, 0, 0, 0, 0.0, 1.5);
    }
}
