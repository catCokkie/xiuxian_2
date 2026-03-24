using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentDropInstanceGenerationRules
    {
        public static EquipmentInstanceData[] GenerateInstances(
            IReadOnlyDictionary<string, EquipmentGenerationRules.EquipmentTemplateGenerationSpec> specsByTemplateId,
            IReadOnlyList<string> templateIds,
            string sourceLevelId,
            EquipmentSourceStage sourceStage,
            Func<int, int> pickIndex,
            Func<double, double, double> rollValue,
            Func<long> nowUnix)
        {
            var result = new List<EquipmentInstanceData>();
            int counter = 0;
            for (int i = 0; i < templateIds.Count; i++)
            {
                string templateId = templateIds[i];
                if (!specsByTemplateId.TryGetValue(templateId, out var spec))
                {
                    continue;
                }

                counter++;
                result.Add(EquipmentGenerationRules.GenerateFromSpec(
                    spec,
                    sourceLevelId,
                    sourceStage,
                    rarityOverride: null,
                    uniqueSuffix: $"drop_{counter}",
                    pickIndex: pickIndex,
                    rollValue: rollValue,
                    nowUnix: nowUnix));
            }

            return result.ToArray();
        }
    }
}
