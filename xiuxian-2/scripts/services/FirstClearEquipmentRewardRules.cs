using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class FirstClearEquipmentRewardRules
    {
        public readonly record struct FirstClearEquipmentRewardSpec(
            string EquipmentTemplateId,
            EquipmentRarityTier? RarityOverride,
            int Quantity);

        public static EquipmentInstanceData[] GenerateInstances(
            IReadOnlyDictionary<string, EquipmentGenerationRules.EquipmentTemplateGenerationSpec> specsByTemplateId,
            IReadOnlyList<FirstClearEquipmentRewardSpec> rewardSpecs,
            string sourceLevelId,
            Func<int, int> pickIndex,
            Func<double, double, double> rollValue,
            Func<long> nowUnix)
        {
            var result = new List<EquipmentInstanceData>();
            int counter = 0;
            for (int i = 0; i < rewardSpecs.Count; i++)
            {
                FirstClearEquipmentRewardSpec reward = rewardSpecs[i];
                if (string.IsNullOrEmpty(reward.EquipmentTemplateId) || reward.Quantity <= 0)
                {
                    continue;
                }

                if (!specsByTemplateId.TryGetValue(reward.EquipmentTemplateId, out var spec))
                {
                    continue;
                }

                for (int qty = 0; qty < reward.Quantity; qty++)
                {
                    counter++;
                    result.Add(EquipmentGenerationRules.GenerateFromSpec(
                        spec,
                        sourceLevelId,
                        EquipmentSourceStage.FirstClear,
                        reward.RarityOverride,
                        uniqueSuffix: $"first_clear_{counter}",
                        pickIndex: pickIndex,
                        rollValue: rollValue,
                        nowUnix: nowUnix));
                }
            }

            return result.ToArray();
        }
    }
}
