namespace Xiuxian.Scripts.Services
{
    public readonly record struct BattleStartSetup(
        string MonsterId,
        string MonsterName,
        int EnemyMaxHp,
        int EnemyAttack,
        int InputsPerRound,
        int BattleRoundCounter,
        int PendingBattleInputEvents);
}
