namespace Xiuxian.Scripts.Core
{
    public readonly record struct ActivitySettlementResult(
        double LingqiGain,
        double InsightGain,
        double PetAffinityGain,
        double RealmExpGain,
        bool InputExpActive);

    public static class ActivitySettlementRule
    {
        public static double CalculateInputRealmExpGain(bool cultivationInputExpEnabled, bool isCultivationMode, int inputEvents, double cultivationExpPerInput)
        {
            if (!cultivationInputExpEnabled || !isCultivationMode || inputEvents <= 0 || cultivationExpPerInput <= 0.0)
            {
                return 0.0;
            }

            return inputEvents * cultivationExpPerInput;
        }

        public static ActivitySettlementResult CalculateSettlement(
            double apFinalBucket,
            double lingqiFactor,
            double insightFactor,
            double petAffinityFactor,
            double realmExpFromLingqiRate,
            double moodMultiplier,
            double realmMultiplier,
            bool cultivationInputExpEnabled,
            bool isCultivationMode)
        {
            if (apFinalBucket <= 0.0)
            {
                return new ActivitySettlementResult(0.0, 0.0, 0.0, 0.0, cultivationInputExpEnabled && isCultivationMode);
            }

            double lingqiGain = apFinalBucket * lingqiFactor * moodMultiplier * realmMultiplier;
            double insightGain = apFinalBucket * insightFactor;
            double petAffinityGain = apFinalBucket * petAffinityFactor;
            bool inputExpActive = cultivationInputExpEnabled && isCultivationMode;
            double realmExpGain = inputExpActive ? 0.0 : lingqiGain * realmExpFromLingqiRate;

            return new ActivitySettlementResult(lingqiGain, insightGain, petAffinityGain, realmExpGain, inputExpActive);
        }
    }
}
