using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Core
{
    public readonly record struct PityApplyResult(bool Triggered, int CounterValue);

    public static class DropEconomyRule
    {
        public static bool CanConsumeDailyRoll(int dailyCap, int dailyCount)
        {
            return dailyCap <= 0 || dailyCount < dailyCap;
        }

        public static bool ShouldSkipBySoftCap(int softCap, int hourlyCountAfterConsume, double decay, double roll)
        {
            if (softCap <= 0 || hourlyCountAfterConsume <= softCap)
            {
                return false;
            }

            if (decay <= 0.0)
            {
                return true;
            }

            int overflow = hourlyCountAfterConsume - softCap;
            double allowChance = Math.Pow(Math.Min(1.0, decay), overflow);
            return roll > allowChance;
        }

        public static PityApplyResult ApplyPity(
            IDictionary<string, int> result,
            IDictionary<string, int> pityCounterByKey,
            string pityCounterKey,
            int pityThreshold,
            string pityItemId,
            int pityQty)
        {
            if (string.IsNullOrEmpty(pityCounterKey) || pityThreshold <= 0 || string.IsNullOrEmpty(pityItemId))
            {
                return new PityApplyResult(false, 0);
            }

            bool hasPityItem = result.TryGetValue(pityItemId, out int existingQty) && existingQty > 0;
            if (hasPityItem)
            {
                pityCounterByKey[pityCounterKey] = 0;
                return new PityApplyResult(false, 0);
            }

            int next = (pityCounterByKey.TryGetValue(pityCounterKey, out int current) ? current : 0) + 1;
            if (next >= pityThreshold)
            {
                int qty = Math.Max(1, pityQty);
                result[pityItemId] = (result.TryGetValue(pityItemId, out int saved) ? saved : 0) + qty;
                pityCounterByKey[pityCounterKey] = 0;
                return new PityApplyResult(true, 0);
            }

            pityCounterByKey[pityCounterKey] = next;
            return new PityApplyResult(false, next);
        }
    }
}
