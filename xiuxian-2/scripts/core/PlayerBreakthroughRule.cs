namespace Xiuxian.Scripts.Core
{
    public readonly record struct PlayerBreakthroughResult(
        bool Succeeded,
        int NextRealmLevel,
        double RemainingRealmExp,
        double NextRealmExpRequired);

    public static class PlayerBreakthroughRule
    {
        public static PlayerBreakthroughResult TryBreakthrough(int realmLevel, double realmExp)
        {
            int currentLevel = realmLevel < 1 ? 1 : realmLevel;
            double currentRequired = GetExpRequired(currentLevel);
            if (realmExp < currentRequired)
            {
                return new PlayerBreakthroughResult(false, currentLevel, realmExp, currentRequired);
            }

            int nextLevel = currentLevel + 1;
            double remainingExp = realmExp - currentRequired;
            return new PlayerBreakthroughResult(true, nextLevel, remainingExp, GetExpRequired(nextLevel));
        }

        public static double GetExpRequired(int realmLevel)
        {
            int normalizedLevel = realmLevel < 1 ? 1 : realmLevel;
            return 120.0 * System.Math.Pow(normalizedLevel, 1.32) + 180.0;
        }
    }
}
