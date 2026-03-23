namespace Xiuxian.Scripts.Services
{
    public static class BossUnlockRules
    {
        public static string ResolveNextUnlockedLevelId(string configuredNextLevelId, string sequentialNextLevelId)
        {
            if (!string.IsNullOrEmpty(configuredNextLevelId))
            {
                return configuredNextLevelId;
            }

            return sequentialNextLevelId;
        }
    }
}
