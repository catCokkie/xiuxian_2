using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xiuxian.Scripts.Services
{
    public static class ConfigValidationViewFormatter
    {
        public readonly record struct ConfigValidationItem(
            string Scope,
            string Id,
            string Field,
            string Message,
            string LevelId,
            string MonsterId,
            string DropTableId);

        public static string BuildSimulationLevelLabel(string levelId, string levelName, bool useActiveLevel)
        {
            if (useActiveLevel || string.IsNullOrEmpty(levelId))
            {
                return "模拟关卡：当前关卡";
            }

            if (string.IsNullOrEmpty(levelName))
            {
                return $"模拟关卡：{levelId}";
            }

            return $"模拟关卡：{levelName} ({levelId})";
        }

        public static string BuildSimulationMonsterLabel(string monsterId, bool useAutoMonster)
        {
            if (useAutoMonster || string.IsNullOrEmpty(monsterId))
            {
                return "模拟怪物：自动";
            }

            return $"模拟怪物：{monsterId}";
        }

        public static string BuildSimulationStatus(string lastSimulationSummary)
        {
            return string.IsNullOrEmpty(lastSimulationSummary)
                ? "模拟结果：尚未运行"
                : $"模拟结果：{lastSimulationSummary}";
        }

        public static ConfigValidationItem FromDictionary(Godot.Collections.Dictionary<string, Variant> entry)
        {
            return new ConfigValidationItem(
                entry.ContainsKey("scope") ? entry["scope"].AsString() : "config",
                entry.ContainsKey("id") ? entry["id"].AsString() : "(unknown)",
                entry.ContainsKey("field") ? entry["field"].AsString() : "(unknown)",
                entry.ContainsKey("message") ? entry["message"].AsString() : "validation failed",
                entry.ContainsKey("level_id") ? entry["level_id"].AsString() : "",
                entry.ContainsKey("monster_id") ? entry["monster_id"].AsString() : "",
                entry.ContainsKey("drop_table_id") ? entry["drop_table_id"].AsString() : "");
        }

        public static List<ConfigValidationItem> FilterItems(
            IEnumerable<ConfigValidationItem> entries,
            string scopeFilter,
            bool onlyActiveLevel,
            string activeLevelId)
        {
            var result = new List<ConfigValidationItem>();
            string normalizedScope = string.IsNullOrEmpty(scopeFilter) ? "all" : scopeFilter;

            foreach (var entry in entries)
            {
                if (normalizedScope != "all" && entry.Scope != normalizedScope)
                {
                    continue;
                }

                if (onlyActiveLevel && !string.IsNullOrEmpty(activeLevelId))
                {
                    if (string.IsNullOrEmpty(entry.LevelId) || entry.LevelId != activeLevelId)
                    {
                        continue;
                    }
                }

                result.Add(entry);
            }

            return result;
        }

        public static string BuildTitle(int issueCount, int totalCount, string filterSummary)
        {
            if (issueCount <= 0)
            {
                return $"配置校验：通过 ({filterSummary})";
            }

            return $"配置校验：{issueCount}/{totalCount} 项 ({filterSummary})";
        }

        public static string BuildBody(
            IEnumerable<ConfigValidationItem> filteredEntries,
            int maxItems,
            string emptyMessage = "当前过滤条件下未发现配置错误。")
        {
            List<ConfigValidationItem> items = filteredEntries.ToList();
            if (items.Count <= 0)
            {
                return emptyMessage;
            }

            int shown = Mathf.Min(Mathf.Max(1, maxItems), items.Count);
            var sb = new StringBuilder();
            for (int i = 0; i < shown; i++)
            {
                if (i > 0)
                {
                    sb.Append('\n');
                }

                sb.Append(BuildEntryLine(items[i]));
            }

            if (items.Count > shown)
            {
                sb.Append($"\n… 还有 {items.Count - shown} 项");
            }

            return sb.ToString();
        }

        private static string BuildEntryLine(ConfigValidationItem entry)
        {
            List<string> refs = new();
            if (!string.IsNullOrEmpty(entry.LevelId))
            {
                refs.Add($"level_id={entry.LevelId}");
            }
            if (!string.IsNullOrEmpty(entry.MonsterId))
            {
                refs.Add($"monster_id={entry.MonsterId}");
            }
            if (!string.IsNullOrEmpty(entry.DropTableId))
            {
                refs.Add($"drop_table_id={entry.DropTableId}");
            }

            if (refs.Count <= 0)
            {
                return $"• {entry.Scope}/{entry.Id} {entry.Field} {entry.Message}";
            }

            return $"• {entry.Scope}/{entry.Id} {entry.Field} {entry.Message} ({string.Join(", ", refs)})";
        }
    }
}
