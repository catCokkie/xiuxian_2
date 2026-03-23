using System;

namespace Xiuxian.Scripts.Services
{
    public static class PlayerBaseStatRules
    {
        public static CharacterStatBlock BuildBaseStats(int realmLevel, int configuredBaseHp, int configuredBaseAttack)
        {
            int normalizedRealm = Math.Max(1, realmLevel);
            int maxHp = Math.Max(1, configuredBaseHp + (normalizedRealm - 1) * 8);
            int attack = Math.Max(1, configuredBaseAttack + (normalizedRealm - 1) * 1);
            int defense = Math.Max(0, normalizedRealm - 1);
            int speed = 100 + (normalizedRealm - 1) * 2;
            double critChance = Math.Min(0.35, 0.03 + (normalizedRealm - 1) * 0.005);
            double critDamage = 1.50;

            return new CharacterStatBlock(maxHp, attack, defense, speed, critChance, critDamage);
        }
    }
}
