using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class ActionSettlementRules
    {
        public static ActionSettlementResult BuildCultivationSettlement(
            string actionTargetId,
            double apConsumed,
            double lingqiGain,
            double insightGain,
            double petAffinityGain,
            double realmExpGain)
        {
            return new ActionSettlementResult(
                ActionId: PlayerActionState.ActionCultivation,
                ActionTargetId: actionTargetId,
                SourceTag: "cultivation_tick",
                ApConsumed: apConsumed,
                LingqiGain: lingqiGain,
                InsightGain: insightGain,
                PetAffinityGain: petAffinityGain,
                RealmExpGain: realmExpGain,
                ExploreProgressGain: 0.0,
                BattleRoundsAdvanced: 0,
                ItemDrops: new Dictionary<string, int>(),
                EquipmentDrops: System.Array.Empty<EquipmentInstanceData>());
        }

        public static ActionSettlementResult BuildDungeonRewardSettlement(
            string actionTargetId,
            string sourceTag,
            double exploreProgressGain,
            int battleRoundsAdvanced,
            IReadOnlyDictionary<string, int> itemDrops,
            IReadOnlyList<EquipmentInstanceData> equipmentDrops,
            double lingqiGain = 0.0,
            double insightGain = 0.0,
            double realmExpGain = 0.0)
        {
            return new ActionSettlementResult(
                ActionId: PlayerActionState.ActionDungeon,
                ActionTargetId: actionTargetId,
                SourceTag: sourceTag,
                ApConsumed: 0.0,
                LingqiGain: lingqiGain,
                InsightGain: insightGain,
                PetAffinityGain: 0.0,
                RealmExpGain: realmExpGain,
                ExploreProgressGain: exploreProgressGain,
                BattleRoundsAdvanced: battleRoundsAdvanced,
                ItemDrops: itemDrops,
                EquipmentDrops: equipmentDrops);
        }
    }
}
