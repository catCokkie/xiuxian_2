using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class CharacterStatRules
    {
        public static CharacterBattleSnapshot CreatePlayerBattleSnapshot(
            CharacterStatBlock baseStats,
            IReadOnlyList<EquipmentStatProfile> equipmentProfiles,
            int? currentHp = null)
        {
            CharacterStatBlock finalStats = BuildFinalStats(baseStats, equipmentProfiles);
            return CreateBattleSnapshot(finalStats, currentHp);
        }

        public static CharacterStatBlock BuildFinalStats(CharacterStatBlock baseStats, IReadOnlyList<EquipmentStatProfile> equipmentProfiles)
        {
            return BuildFinalStats(baseStats, CollectEquipmentModifiers(equipmentProfiles));
        }

        public static CharacterStatBlock BuildFinalStats(CharacterStatBlock baseStats, IReadOnlyList<CharacterStatModifier> modifiers)
        {
            double hp = baseStats.MaxHp;
            double attack = baseStats.Attack;
            double defense = baseStats.Defense;
            double speed = baseStats.Speed;
            double critChance = baseStats.CritChance;
            double critDamage = baseStats.CritDamage;

            for (int i = 0; i < modifiers.Count; i++)
            {
                CharacterStatModifier modifier = modifiers[i];
                hp = ApplyModifier(hp, modifier.MaxHpFlat, modifier.MaxHpRate);
                attack = ApplyModifier(attack, modifier.AttackFlat, modifier.AttackRate);
                defense = ApplyModifier(defense, modifier.DefenseFlat, modifier.DefenseRate);
                speed = ApplyModifier(speed, modifier.SpeedFlat, modifier.SpeedRate);
                critChance += modifier.CritChanceDelta;
                critDamage += modifier.CritDamageDelta;
            }

            return new CharacterStatBlock(
                Math.Max(1, (int)Math.Round(hp)),
                Math.Max(1, (int)Math.Round(attack)),
                Math.Max(0, (int)Math.Round(defense)),
                Math.Max(1, (int)Math.Round(speed)),
                Math.Clamp(critChance, 0.0, 1.0),
                Math.Max(1.0, critDamage));
        }

        public static CharacterBattleSnapshot CreateBattleSnapshot(CharacterStatBlock finalStats, int? currentHp = null)
        {
            int hp = currentHp.HasValue ? Math.Clamp(currentHp.Value, 0, Math.Max(1, finalStats.MaxHp)) : Math.Max(1, finalStats.MaxHp);
            return new CharacterBattleSnapshot(
                finalStats.MaxHp,
                hp,
                finalStats.Attack,
                finalStats.Defense,
                finalStats.Speed,
                finalStats.CritChance,
                finalStats.CritDamage);
        }

        public static CharacterBattleSnapshot CreateBattleSnapshot(MonsterStatProfile monsterProfile, int? currentHp = null)
        {
            return CreateBattleSnapshot(monsterProfile.ToStatBlock(), currentHp);
        }

        public static int CalculateMitigatedDamage(int attack, int defense, int minimumDamage = 1)
        {
            return Math.Max(minimumDamage, attack - defense);
        }

        public static CharacterStatModifier[] CollectEquipmentModifiers(IReadOnlyList<EquipmentStatProfile> equipmentProfiles)
        {
            CharacterStatModifier[] modifiers = new CharacterStatModifier[equipmentProfiles.Count];
            int count = 0;
            for (int i = 0; i < equipmentProfiles.Count; i++)
            {
                if (!equipmentProfiles[i].IsEquipped)
                {
                    continue;
                }

                modifiers[count] = equipmentProfiles[i].ToModifier();
                count++;
            }

            if (count == modifiers.Length)
            {
                return modifiers;
            }

            CharacterStatModifier[] trimmed = new CharacterStatModifier[count];
            Array.Copy(modifiers, trimmed, count);
            return trimmed;
        }

        private static double ApplyModifier(double baseValue, int flat, double rate)
        {
            return (baseValue + flat) * (1.0 + rate);
        }
    }
}
