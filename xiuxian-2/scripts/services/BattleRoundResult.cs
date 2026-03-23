namespace Xiuxian.Scripts.Services
{
    public readonly record struct BattleRoundResult(
        CharacterBattleSnapshot Player,
        CharacterBattleSnapshot Monster,
        int DamageToMonster,
        int DamageToPlayer,
        BattleOutcome Outcome);
}
