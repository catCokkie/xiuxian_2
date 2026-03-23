namespace Xiuxian.Scripts.Services
{
    public static class BattleLifecycleRules
    {
        public static BattleEncounterDecision DetermineEncounterStart(
            int candidateIndex,
            float candidateX,
            float battleTriggerX,
            string monsterId)
        {
            if (candidateIndex < 0)
            {
                return new BattleEncounterDecision(false, -1, string.Empty);
            }

            if (candidateX > battleTriggerX)
            {
                return new BattleEncounterDecision(false, -1, string.Empty);
            }

            return new BattleEncounterDecision(true, candidateIndex, monsterId);
        }

        public static BattleDefeatDecision DetermineDefeatReset(string activeLevelId)
        {
            return new BattleDefeatDecision(
                ShouldEndBattle: true,
                ShouldResetExploreProgress: true,
                ShouldResetLevel: !string.IsNullOrEmpty(activeLevelId),
                ActiveLevelId: activeLevelId);
        }

        public static BattleVictoryDecision DetermineVictorySettlement(string activeLevelId, string monsterId)
        {
            return new BattleVictoryDecision(
                ShouldEndBattle: true,
                ShouldApplyBattleRewards: true,
                ShouldTryBossUnlock: !string.IsNullOrEmpty(activeLevelId) && !string.IsNullOrEmpty(monsterId),
                ActiveLevelId: activeLevelId,
                MonsterId: monsterId);
        }
    }
}
