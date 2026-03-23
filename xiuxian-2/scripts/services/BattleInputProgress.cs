namespace Xiuxian.Scripts.Services
{
    public readonly record struct BattleInputProgress(
        int Threshold,
        int PendingInputs,
        int RoundsToResolve,
        int RemainingInputs);
}
