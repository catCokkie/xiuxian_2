namespace Xiuxian.Scripts.Services
{
    public readonly record struct BattleRewardDecision(
        bool HasConfiguredRewards,
        bool ShouldUseFallback,
        string BattleLogRewardSummary);
}
