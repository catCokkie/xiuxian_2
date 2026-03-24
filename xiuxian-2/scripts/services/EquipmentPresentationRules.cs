using System.Collections.Generic;
using System.Text;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentPresentationRules
    {
        public static string BuildEquipmentPageText(
            CharacterStatBlock baseStats,
            CharacterStatBlock finalStats,
            IReadOnlyList<EquipmentStatProfile> equippedProfiles,
            IReadOnlyList<EquipmentInstanceData> backpackInstances,
            IReadOnlyList<EquipmentStatProfile> legacyBackpackProfiles)
        {
            var sb = new StringBuilder();
            sb.AppendLine(UiText.LeftTabEquipment);
            sb.AppendLine($"当前已装备 {equippedProfiles.Count} 件");
            sb.AppendLine($"基础属性：HP {baseStats.MaxHp} / 攻 {baseStats.Attack} / 防 {baseStats.Defense}");
            sb.AppendLine($"装备后：HP {finalStats.MaxHp} / 攻 {finalStats.Attack} / 防 {finalStats.Defense}");

            for (int i = 0; i < equippedProfiles.Count; i++)
            {
                EquipmentStatProfile profile = equippedProfiles[i];
                sb.AppendLine();
                sb.AppendLine($"[{BuildSlotLabel(profile.Slot)}] {profile.DisplayName}");
                sb.AppendLine(BuildModifierSummary(profile.Modifier));
            }

            sb.AppendLine();
            sb.AppendLine($"背包装备 {backpackInstances.Count + legacyBackpackProfiles.Count} 件");

            for (int i = 0; i < backpackInstances.Count; i++)
            {
                EquipmentInstanceData instance = backpackInstances[i];
                sb.AppendLine($"- [{BuildSlotLabel(instance.Slot)}] {instance.DisplayName} | {BuildRarityLabel(instance.RarityTier)} | {BuildSourceLabel(instance.SourceStage)}");
                sb.AppendLine($"  主属性：{BuildSingleStatLine(instance.MainStatKey, instance.MainStatValue)}");
                sb.AppendLine($"  副属性：{BuildSubStatSummary(instance.SubStats)}");
            }

            for (int i = 0; i < legacyBackpackProfiles.Count; i++)
            {
                EquipmentStatProfile profile = legacyBackpackProfiles[i];
                sb.AppendLine($"- [{BuildSlotLabel(profile.Slot)}] {profile.DisplayName} | 旧版装备");
                sb.AppendLine($"  属性：{BuildModifierSummary(profile.Modifier)}");
            }

            return sb.ToString().TrimEnd();
        }

        public static string BuildRarityLabel(EquipmentRarityTier rarity)
        {
            return rarity switch
            {
                EquipmentRarityTier.CommonTool => "俗器",
                EquipmentRarityTier.Artifact => "法器",
                EquipmentRarityTier.Spirit => "灵器",
                EquipmentRarityTier.Treasure => "宝器",
                _ => "装备",
            };
        }

        public static string BuildSourceLabel(EquipmentSourceStage sourceStage)
        {
            return sourceStage switch
            {
                EquipmentSourceStage.Starter => "开局",
                EquipmentSourceStage.Normal => "普通掉落",
                EquipmentSourceStage.Elite => "精英掉落",
                EquipmentSourceStage.Boss => "Boss掉落",
                EquipmentSourceStage.Exchange => "兑换",
                EquipmentSourceStage.FirstClear => "首通奖励",
                _ => "来源未知",
            };
        }

        public static string BuildSubStatSummary(IReadOnlyList<EquipmentSubStatData> subStats)
        {
            if (subStats == null || subStats.Count == 0)
            {
                return "无";
            }

            List<string> parts = new();
            for (int i = 0; i < subStats.Count; i++)
            {
                parts.Add(BuildSingleStatLine(subStats[i].Stat, subStats[i].Value));
            }

            return string.Join(" | ", parts);
        }

        public static string BuildSingleStatLine(string statKey, double value)
        {
            return statKey switch
            {
                "max_hp_flat" => $"HP+{(int)System.Math.Round(value)}",
                "attack_flat" => $"攻击+{(int)System.Math.Round(value)}",
                "defense_flat" => $"防御+{(int)System.Math.Round(value)}",
                "speed_flat" => $"速度+{(int)System.Math.Round(value)}",
                "crit_chance_delta" => $"暴击+{value:P0}",
                "crit_damage_delta" => $"暴伤+{value:0.##}",
                _ => $"{statKey}+{value:0.##}",
            };
        }

        public static string BuildModifierSummary(CharacterStatModifier modifier)
        {
            List<string> parts = new();
            if (modifier.MaxHpFlat != 0) parts.Add($"HP+{modifier.MaxHpFlat}");
            if (modifier.AttackFlat != 0) parts.Add($"攻击+{modifier.AttackFlat}");
            if (modifier.DefenseFlat != 0) parts.Add($"防御+{modifier.DefenseFlat}");
            if (modifier.SpeedFlat != 0) parts.Add($"速度+{modifier.SpeedFlat}");
            if (modifier.CritChanceDelta != 0.0) parts.Add($"暴击+{modifier.CritChanceDelta:P0}");
            if (modifier.CritDamageDelta != 0.0) parts.Add($"暴伤+{modifier.CritDamageDelta:0.##}");
            return parts.Count > 0 ? string.Join(" | ", parts) : "当前无额外词条";
        }

        public static string BuildSlotLabel(EquipmentSlotType slot)
        {
            return slot switch
            {
                EquipmentSlotType.Weapon => "武器",
                EquipmentSlotType.Armor => "护具",
                EquipmentSlotType.Accessory => "饰品",
                _ => "装备"
            };
        }
    }
}
