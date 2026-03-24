using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentInstanceRules
    {
        public static EquipmentStatProfile ToStatProfile(EquipmentInstanceData instance)
        {
            return new EquipmentStatProfile(
                instance.EquipmentId,
                instance.DisplayName,
                instance.Slot,
                BuildModifier(instance),
                SetTag: instance.SeriesId,
                Rarity: (int)instance.RarityTier,
                EnhanceLevel: instance.EnhanceLevel,
                IsEquipped: instance.IsEquipped);
        }

        public static CharacterStatModifier BuildModifier(EquipmentInstanceData instance)
        {
            CharacterStatModifier modifier = default;
            modifier = ApplyStat(modifier, instance.MainStatKey, instance.MainStatValue);

            IReadOnlyList<EquipmentSubStatData> subStats = instance.SubStats ?? System.Array.Empty<EquipmentSubStatData>();
            for (int i = 0; i < subStats.Count; i++)
            {
                modifier = ApplyStat(modifier, subStats[i].Stat, subStats[i].Value);
            }

            return modifier;
        }

        private static CharacterStatModifier ApplyStat(CharacterStatModifier modifier, string statKey, double value)
        {
            return statKey switch
            {
                "max_hp_flat" => modifier with { MaxHpFlat = modifier.MaxHpFlat + (int)System.Math.Round(value) },
                "attack_flat" => modifier with { AttackFlat = modifier.AttackFlat + (int)System.Math.Round(value) },
                "defense_flat" => modifier with { DefenseFlat = modifier.DefenseFlat + (int)System.Math.Round(value) },
                "speed_flat" => modifier with { SpeedFlat = modifier.SpeedFlat + (int)System.Math.Round(value) },
                "max_hp_rate" => modifier with { MaxHpRate = modifier.MaxHpRate + value },
                "attack_rate" => modifier with { AttackRate = modifier.AttackRate + value },
                "defense_rate" => modifier with { DefenseRate = modifier.DefenseRate + value },
                "speed_rate" => modifier with { SpeedRate = modifier.SpeedRate + value },
                "crit_chance_delta" => modifier with { CritChanceDelta = modifier.CritChanceDelta + value },
                "crit_damage_delta" => modifier with { CritDamageDelta = modifier.CritDamageDelta + value },
                _ => modifier,
            };
        }
    }
}
