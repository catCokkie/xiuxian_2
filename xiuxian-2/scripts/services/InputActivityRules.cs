using System;

namespace Xiuxian.Scripts.Services
{
    public static class InputActivityRules
    {
        public static double CalculateDecayMultiplier(
            double apPerSecond,
            double apBaseline,
            double decayThreshold,
            double decayRate,
            double minDecayMultiplier)
        {
            double ratio = apBaseline > 0.0 ? apPerSecond / apBaseline : apPerSecond;
            double decay = 1.0 - Math.Max(0.0, ratio - decayThreshold) * decayRate;
            if (decay < minDecayMultiplier)
            {
                return minDecayMultiplier;
            }

            if (decay > 1.0)
            {
                return 1.0;
            }

            return decay;
        }

        public static double CalculateCapMultiplier(double apFinalThisMinute, double softCapPerMinute, double minCapMultiplier)
        {
            if (softCapPerMinute <= 0.0)
            {
                return 1.0;
            }

            double ratio = apFinalThisMinute / softCapPerMinute;
            if (ratio <= 1.0)
            {
                return 1.0;
            }

            double multiplier = 1.0 / ratio;
            if (multiplier < minCapMultiplier)
            {
                return minCapMultiplier;
            }

            return multiplier;
        }

        public static double CalculateAccumulator(double currentAccumulator, double apFinal, double drainPerSecond, double delta)
        {
            return Math.Max(0.0, currentAccumulator + apFinal - drainPerSecond * delta);
        }
    }
}
