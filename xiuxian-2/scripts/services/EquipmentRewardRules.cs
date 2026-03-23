using System.Text;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentRewardRules
    {
        public static bool TryBuildFirstClearReward(string levelId, out EquipmentStatProfile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(levelId))
            {
                return false;
            }

            int checksum = 0;
            for (int i = 0; i < levelId.Length; i++)
            {
                checksum += levelId[i];
            }

            string safeId = BuildSafeId(levelId);
            if (checksum % 2 == 0)
            {
                profile = new EquipmentStatProfile(
                    $"reward_weapon_{safeId}",
                    $"试炼兵刃·{levelId}",
                    EquipmentSlotType.Weapon,
                    new CharacterStatModifier(AttackFlat: 4, CritChanceDelta: 0.01),
                    SetTag: "first_clear",
                    Rarity: 1,
                    EnhanceLevel: 0,
                    IsEquipped: false);
            }
            else
            {
                profile = new EquipmentStatProfile(
                    $"reward_armor_{safeId}",
                    $"试炼护具·{levelId}",
                    EquipmentSlotType.Armor,
                    new CharacterStatModifier(MaxHpFlat: 14, DefenseFlat: 2),
                    SetTag: "first_clear",
                    Rarity: 1,
                    EnhanceLevel: 0,
                    IsEquipped: false);
            }

            return true;
        }

        private static string BuildSafeId(string levelId)
        {
            StringBuilder sb = new();
            for (int i = 0; i < levelId.Length; i++)
            {
                char ch = levelId[i];
                sb.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_');
            }

            return sb.ToString();
        }
    }
}
