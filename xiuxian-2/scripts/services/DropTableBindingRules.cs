using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class DropTableBindingRules
    {
        public static bool IsTableBoundToLevel(string boundLevelId, string activeLevelId)
        {
            if (string.IsNullOrEmpty(boundLevelId) || string.IsNullOrEmpty(activeLevelId))
            {
                return true;
            }

            return boundLevelId == activeLevelId;
        }

        public static bool IsTableBoundToMonster(IReadOnlyList<string> boundMonsterIds, string monsterId)
        {
            if (string.IsNullOrEmpty(monsterId))
            {
                return false;
            }

            if (boundMonsterIds.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < boundMonsterIds.Count; i++)
            {
                if (boundMonsterIds[i] == monsterId)
                {
                    return true;
                }
            }

            return false;
        }

        public static string ResolveDropTableForActiveLevel(
            string activeLevelId,
            string monsterId,
            string configuredDropTableId,
            bool configuredTableValidForLevel,
            IReadOnlyList<(string DropTableId, bool LevelMatch, bool MonsterMatch)> candidates)
        {
            if (!string.IsNullOrEmpty(configuredDropTableId) && configuredTableValidForLevel)
            {
                return configuredDropTableId;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate.LevelMatch && candidate.MonsterMatch)
                {
                    return candidate.DropTableId;
                }
            }

            return configuredDropTableId;
        }
    }
}
