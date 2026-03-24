using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class DungeonOfflineSettlementRules
    {
        public readonly record struct WeightedMonsterProfile(
            MonsterStatProfile Monster,
            int Weight);

        public static int CalculateEstimatedEncounters(
            double offlineInputBudget,
            double dungeonProgressPerInput,
            double encounterProgressThreshold)
        {
            if (offlineInputBudget <= 0.0 || dungeonProgressPerInput <= 0.0 || encounterProgressThreshold <= 0.0)
            {
                return 0;
            }

            double progress = offlineInputBudget * dungeonProgressPerInput;
            return Math.Max(0, (int)Math.Floor(progress / encounterProgressThreshold));
        }

        public static double CalculateWeightedVictoryFactor(
            CharacterStatBlock playerStats,
            IReadOnlyList<WeightedMonsterProfile> weightedMonsters)
        {
            if (weightedMonsters.Count == 0)
            {
                return 0.0;
            }

            int totalWeight = 0;
            double totalScore = 0.0;
            for (int i = 0; i < weightedMonsters.Count; i++)
            {
                int weight = Math.Max(0, weightedMonsters[i].Weight);
                if (weight <= 0)
                {
                    continue;
                }

                totalWeight += weight;
                totalScore += weight * CalculateMonsterVictoryFactor(playerStats, weightedMonsters[i].Monster.ToStatBlock());
            }

            return totalWeight > 0 ? totalScore / totalWeight : 0.0;
        }

        public static ActionSettlementResult BuildDungeonOfflineSettlement(
            string actionTargetId,
            double offlineInputBudget,
            double dungeonProgressPerInput,
            double encounterProgressThreshold,
            CharacterStatBlock playerStats,
            IReadOnlyList<WeightedMonsterProfile> weightedMonsters,
            double averageLingqiPerVictory,
            double averageInsightPerVictory,
            IReadOnlyDictionary<string, double> averageItemDropsPerVictory,
            int equipmentDropCap,
            double estimatedEquipmentDropsPerVictory)
        {
            double exploreProgressGain = offlineInputBudget * dungeonProgressPerInput;
            int estimatedEncounters = CalculateEstimatedEncounters(offlineInputBudget, dungeonProgressPerInput, encounterProgressThreshold);
            double weightedVictoryFactor = CalculateWeightedVictoryFactor(playerStats, weightedMonsters);
            double effectiveVictories = estimatedEncounters * weightedVictoryFactor;

            var itemDrops = new Dictionary<string, int>();
            foreach (KeyValuePair<string, double> entry in averageItemDropsPerVictory)
            {
                int qty = (int)Math.Floor(Math.Max(0.0, entry.Value) * effectiveVictories);
                if (qty > 0)
                {
                    itemDrops[entry.Key] = qty;
                }
            }

            int equipmentCount = Math.Min(Math.Max(0, equipmentDropCap), (int)Math.Floor(Math.Max(0.0, estimatedEquipmentDropsPerVictory) * effectiveVictories));
            var equipmentDrops = new List<EquipmentInstanceData>(equipmentCount);

            return ActionSettlementRules.BuildDungeonRewardSettlement(
                actionTargetId: actionTargetId,
                sourceTag: "offline_dungeon",
                exploreProgressGain: exploreProgressGain,
                battleRoundsAdvanced: estimatedEncounters,
                itemDrops: itemDrops,
                equipmentDrops: equipmentDrops,
                lingqiGain: averageLingqiPerVictory * effectiveVictories,
                insightGain: averageInsightPerVictory * effectiveVictories);
        }

        private static double CalculateMonsterVictoryFactor(CharacterStatBlock playerStats, CharacterStatBlock monsterStats)
        {
            double playerAttackAfterDefense = Math.Max(1.0, playerStats.Attack - monsterStats.Defense);
            double monsterAttackAfterDefense = Math.Max(1.0, monsterStats.Attack - playerStats.Defense);
            double critFactor = 1.0 + playerStats.CritChance * Math.Max(0.0, playerStats.CritDamage - 1.0);

            double playerDpr = playerAttackAfterDefense * critFactor;
            double monsterDpr = monsterAttackAfterDefense;
            double playerTtk = Math.Max(1.0, monsterStats.MaxHp) / Math.Max(1.0, playerDpr);
            double monsterTtk = Math.Max(1.0, playerStats.MaxHp) / Math.Max(1.0, monsterDpr);
            double combatRatio = monsterTtk / Math.Max(0.1, playerTtk);

            if (combatRatio >= 1.8)
            {
                return 0.95;
            }

            if (combatRatio >= 1.1)
            {
                return 0.75;
            }

            if (combatRatio >= 0.7)
            {
                return 0.40;
            }

            return 0.10;
        }
    }
}
