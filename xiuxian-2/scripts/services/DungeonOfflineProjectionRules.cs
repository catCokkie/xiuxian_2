using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class DungeonOfflineProjectionRules
    {
        public readonly record struct MonsterSettlementSpec(
            int Weight,
            MonsterStatProfile Monster,
            double AverageLingqi,
            double AverageInsight,
            IReadOnlyDictionary<string, double> AverageItemDrops,
            double AverageEquipmentDrops);

        public static DungeonOfflineSettlementRules.WeightedMonsterProfile[] BuildWeightedMonsters(IReadOnlyList<MonsterSettlementSpec> specs)
        {
            var result = new DungeonOfflineSettlementRules.WeightedMonsterProfile[specs.Count];
            for (int i = 0; i < specs.Count; i++)
            {
                result[i] = new DungeonOfflineSettlementRules.WeightedMonsterProfile(specs[i].Monster, specs[i].Weight);
            }

            return result;
        }

        public static double CalculateAverageLingqiPerVictory(IReadOnlyList<MonsterSettlementSpec> specs)
        {
            return WeightedAverage(specs, spec => spec.AverageLingqi);
        }

        public static double CalculateAverageInsightPerVictory(IReadOnlyList<MonsterSettlementSpec> specs)
        {
            return WeightedAverage(specs, spec => spec.AverageInsight);
        }

        public static Dictionary<string, double> CalculateAverageItemDropsPerVictory(IReadOnlyList<MonsterSettlementSpec> specs)
        {
            var totals = new Dictionary<string, double>();
            double totalWeight = TotalWeight(specs);
            if (totalWeight <= 0.0)
            {
                return totals;
            }

            for (int i = 0; i < specs.Count; i++)
            {
                double ratio = Math.Max(0, specs[i].Weight) / totalWeight;
                foreach (KeyValuePair<string, double> kv in specs[i].AverageItemDrops)
                {
                    totals[kv.Key] = totals.TryGetValue(kv.Key, out double current) ? current + kv.Value * ratio : kv.Value * ratio;
                }
            }

            return totals;
        }

        public static double CalculateAverageEquipmentDropsPerVictory(IReadOnlyList<MonsterSettlementSpec> specs)
        {
            return WeightedAverage(specs, spec => spec.AverageEquipmentDrops);
        }

        private static double WeightedAverage(IReadOnlyList<MonsterSettlementSpec> specs, Func<MonsterSettlementSpec, double> selector)
        {
            double totalWeight = TotalWeight(specs);
            if (totalWeight <= 0.0)
            {
                return 0.0;
            }

            double total = 0.0;
            for (int i = 0; i < specs.Count; i++)
            {
                total += Math.Max(0, specs[i].Weight) * selector(specs[i]);
            }

            return total / totalWeight;
        }

        private static double TotalWeight(IReadOnlyList<MonsterSettlementSpec> specs)
        {
            double total = 0.0;
            for (int i = 0; i < specs.Count; i++)
            {
                total += Math.Max(0, specs[i].Weight);
            }

            return total;
        }
    }
}
