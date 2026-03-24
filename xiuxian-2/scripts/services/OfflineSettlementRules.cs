using System;

namespace Xiuxian.Scripts.Services
{
    public static class OfflineSettlementRules
    {
        public const double MaxOfflineSeconds = 24.0 * 60.0 * 60.0;

        public static double ClampOfflineSeconds(double offlineSeconds)
        {
            return Math.Clamp(offlineSeconds, 0.0, MaxOfflineSeconds);
        }

        public static double CalculateOfflineInputBudget(double offlineSeconds)
        {
            double remainingMinutes = ClampOfflineSeconds(offlineSeconds) / 60.0;
            double totalInputs = 0.0;

            totalInputs += ConsumeSegment(ref remainingMinutes, 30.0, 12.0);
            totalInputs += ConsumeSegment(ref remainingMinutes, 210.0, 8.0);
            totalInputs += ConsumeSegment(ref remainingMinutes, 240.0, 6.0);
            totalInputs += ConsumeSegment(ref remainingMinutes, 960.0, 3.0);

            return totalInputs;
        }

        public static ActionSettlementResult BuildCultivationOfflineSettlement(
            double offlineSeconds,
            double apPerInput,
            double lingqiFactor,
            double insightFactor,
            double petAffinityFactor,
            double realmExpFromLingqiRate,
            double moodMultiplier,
            double realmMultiplier,
            bool inputExpActive,
            string actionTargetId = "")
        {
            double inputBudget = CalculateOfflineInputBudget(offlineSeconds);
            double offlineAp = inputBudget * apPerInput;

            double lingqiGain = offlineAp * lingqiFactor * moodMultiplier * realmMultiplier;
            double insightGain = offlineAp * insightFactor;
            double petAffinityGain = offlineAp * petAffinityFactor;
            double realmExpGain = inputExpActive ? 0.0 : lingqiGain * realmExpFromLingqiRate;

            return ActionSettlementRules.BuildCultivationSettlement(
                actionTargetId,
                offlineAp,
                lingqiGain,
                insightGain,
                petAffinityGain,
                realmExpGain);
        }

        private static double ConsumeSegment(ref double remainingMinutes, double segmentMinutes, double inputRatePerMinute)
        {
            if (remainingMinutes <= 0.0)
            {
                return 0.0;
            }

            double minutes = Math.Min(remainingMinutes, segmentMinutes);
            remainingMinutes -= minutes;
            return minutes * inputRatePerMinute;
        }
    }
}
