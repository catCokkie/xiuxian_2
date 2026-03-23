using System;

namespace Xiuxian.Scripts.Services
{
    public static class LevelDropEconomyRules
    {
        public static (bool Allowed, int DailyCount, long DayIndex, int HourlyCount, long HourIndex, int HourlyCountAfterConsume, bool DailyCapBlocked)
            ConsumeDropRoll(
                int dailyCount,
                long savedDayIndex,
                int dailyCap,
                int hourlyCount,
                long savedHourIndex,
                long currentUnix,
                bool hasSavedDay,
                bool hasSavedHour)
        {
            long dayIndex = currentUnix / 86400;
            long hourIndex = currentUnix / 3600;

            if (!hasSavedDay || savedDayIndex != dayIndex)
            {
                savedDayIndex = dayIndex;
                dailyCount = 0;
            }

            if (dailyCap > 0 && dailyCount >= dailyCap)
            {
                return (false, dailyCount, savedDayIndex, hourlyCount, savedHourIndex, hourlyCount, true);
            }

            dailyCount += 1;

            if (!hasSavedHour || savedHourIndex != hourIndex)
            {
                savedHourIndex = hourIndex;
                hourlyCount = 0;
            }

            int hourlyCountAfterConsume = hourlyCount + 1;
            hourlyCount = hourlyCountAfterConsume;
            return (true, dailyCount, savedDayIndex, hourlyCount, savedHourIndex, hourlyCountAfterConsume, false);
        }

        public static bool ShouldSkipDropBySoftCap(int softCap, double repeatDecayFactor, int hourlyCountAfterConsume, double randomRoll)
        {
            if (softCap <= 0 || hourlyCountAfterConsume <= softCap)
            {
                return false;
            }

            if (repeatDecayFactor <= 0.0)
            {
                return true;
            }

            int overflow = hourlyCountAfterConsume - softCap;
            double allowChance = Math.Pow(Math.Min(1.0, repeatDecayFactor), overflow);
            return randomRoll > allowChance;
        }

        public static (int NextCounter, bool Triggered, int AddedQty) ApplyPity(
            int currentCounter,
            int threshold,
            bool hasPityItem,
            int pityQty)
        {
            if (hasPityItem)
            {
                return (0, false, 0);
            }

            int next = currentCounter + 1;
            if (threshold > 0 && next >= threshold)
            {
                return (0, true, Math.Max(1, pityQty));
            }

            return (next, false, 0);
        }
    }
}
