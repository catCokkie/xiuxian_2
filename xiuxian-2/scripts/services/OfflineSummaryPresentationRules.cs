using System.Collections.Generic;
using System.Text;

namespace Xiuxian.Scripts.Services
{
    public static class OfflineSummaryPresentationRules
    {
        public static string BuildTitle(ActionSettlementResult result)
        {
            return result.ActionId == PlayerActionState.ActionCultivation
                ? "离线修炼完成"
                : "离线副本完成";
        }

        public static string BuildBody(ActionSettlementResult result)
        {
            List<string> parts = new();
            if (result.LingqiGain > 0.0)
            {
                parts.Add($"灵气+{result.LingqiGain:0}");
            }
            if (result.InsightGain > 0.0)
            {
                parts.Add($"悟性+{result.InsightGain:0}");
            }
            if (result.PetAffinityGain > 0.0)
            {
                parts.Add($"灵宠亲和+{result.PetAffinityGain:0}");
            }
            if (result.RealmExpGain > 0.0)
            {
                parts.Add($"境界经验+{result.RealmExpGain:0}");
            }
            if (result.ExploreProgressGain > 0.0)
            {
                parts.Add($"探索推进+{result.ExploreProgressGain:0.0}");
            }
            if (result.BattleRoundsAdvanced > 0)
            {
                parts.Add($"遭遇{result.BattleRoundsAdvanced}次");
            }
            foreach (KeyValuePair<string, int> drop in result.ItemDrops)
            {
                parts.Add($"{drop.Key}+{drop.Value}");
            }
            if (result.EquipmentDrops.Count > 0)
            {
                parts.Add($"装备+{result.EquipmentDrops.Count}");
            }

            if (parts.Count == 0)
            {
                return "本次离线未获得可结算收益。";
            }

            var sb = new StringBuilder();
            for (int i = 0; i < parts.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" | ");
                }
                sb.Append(parts[i]);
            }
            return sb.ToString();
        }
    }
}
