namespace Xiuxian.Scripts.Services
{
    public readonly record struct BattleEncounterDecision(
        bool ShouldStart,
        int MonsterIndex,
        string MonsterId);
}
