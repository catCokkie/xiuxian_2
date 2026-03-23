using System;

namespace Xiuxian.Scripts.Services
{
    public static class BattleSettlementRules
    {
        public static (int Lingqi, int Insight) RollReward(
            int lingqiMin,
            int lingqiMax,
            int insightMin,
            int insightMax,
            Func<int, int, int> rollInclusive)
        {
            if (rollInclusive == null)
            {
                throw new ArgumentNullException(nameof(rollInclusive));
            }

            int normalizedLingqiMax = Math.Max(lingqiMin, lingqiMax);
            int normalizedInsightMax = Math.Max(insightMin, insightMax);
            return (
                rollInclusive(lingqiMin, normalizedLingqiMax),
                rollInclusive(insightMin, normalizedInsightMax));
        }
    }
}
