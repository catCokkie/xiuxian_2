namespace Xiuxian.Scripts.Services
{
    public readonly record struct MonsterStatProfile(
        string MonsterId,
        string DisplayName,
        CharacterStatBlock BaseStats,
        int InputsPerRound,
        string MoveCategory = "normal",
        bool IsBoss = false)
    {
        public CharacterStatBlock ToStatBlock()
        {
            return BaseStats;
        }
    }
}
