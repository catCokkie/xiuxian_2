namespace Xiuxian.Scripts.Services
{
    public static class RewardRules
    {
        public static BattleRewardDecision DetermineBattleRewardDecision(int dropCount, double lingqi, double insight, string itemPart)
        {
            bool hasConfiguredRewards = dropCount > 0 || lingqi > 0.0 || insight > 0.0;
            return new BattleRewardDecision(
                hasConfiguredRewards,
                !hasConfiguredRewards,
                BuildBattleRewardSummary(lingqi, insight, itemPart));
        }

        public static string BuildBattleRewardSummary(double lingqi, double insight, string itemPart)
        {
            return $"灵气+{lingqi:0} 悟性+{insight:0} 掉落:{itemPart}";
        }

        public static string BuildLevelCompletionSourceTag(string levelId, bool firstClear)
        {
            return firstClear ? $"level_first_clear:{levelId}" : $"level_repeat_clear:{levelId}";
        }
    }
}
