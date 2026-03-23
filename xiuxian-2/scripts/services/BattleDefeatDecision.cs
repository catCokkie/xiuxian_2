namespace Xiuxian.Scripts.Services
{
    public readonly record struct BattleDefeatDecision(
        bool ShouldEndBattle,
        bool ShouldResetExploreProgress,
        bool ShouldResetLevel,
        string ActiveLevelId);
}
