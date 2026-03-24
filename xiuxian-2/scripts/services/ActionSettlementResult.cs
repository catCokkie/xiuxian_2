using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public sealed record ActionSettlementResult(
        string ActionId,
        string ActionTargetId,
        string SourceTag,
        double ApConsumed,
        double LingqiGain,
        double InsightGain,
        double PetAffinityGain,
        double RealmExpGain,
        double ExploreProgressGain,
        int BattleRoundsAdvanced,
        IReadOnlyDictionary<string, int> ItemDrops,
        IReadOnlyList<EquipmentInstanceData> EquipmentDrops)
    {
        public bool HasAnyReward => LingqiGain > 0.0
            || InsightGain > 0.0
            || PetAffinityGain > 0.0
            || RealmExpGain > 0.0
            || ExploreProgressGain > 0.0
            || BattleRoundsAdvanced > 0
            || ItemDrops.Count > 0
            || EquipmentDrops.Count > 0;
    }
}
