using System;

namespace Xiuxian.Scripts.Services
{
    public static class MonsterStatRules
    {
        public static MonsterStatProfile BuildProfile(
            string monsterId,
            string displayName,
            int maxHp,
            int attack,
            int defense,
            double speedFactor,
            int inputsPerRound,
            string moveCategory,
            bool isBoss)
        {
            return new MonsterStatProfile(
                monsterId,
                string.IsNullOrEmpty(displayName) ? "Enemy" : displayName,
                new CharacterStatBlock(
                    Math.Max(1, maxHp),
                    Math.Max(1, attack),
                    Math.Max(0, defense),
                    Math.Max(1, (int)Math.Round(Math.Max(0.1, speedFactor) * 100.0)),
                    0.0,
                    1.5),
                Math.Max(1, inputsPerRound),
                string.IsNullOrEmpty(moveCategory) ? "normal" : moveCategory,
                isBoss);
        }
    }
}
