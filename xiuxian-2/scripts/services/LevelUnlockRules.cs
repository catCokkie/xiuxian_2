using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class LevelUnlockRules
    {
        public static string GetNextUnlockedLevelId(IReadOnlyList<string> unlockedLevelIds, string currentLevelId)
        {
            if (unlockedLevelIds.Count == 0)
            {
                return "";
            }

            int currentIndex = -1;
            for (int i = 0; i < unlockedLevelIds.Count; i++)
            {
                if (unlockedLevelIds[i] == currentLevelId)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                return unlockedLevelIds[0];
            }

            return unlockedLevelIds[(currentIndex + 1) % unlockedLevelIds.Count];
        }
    }
}
