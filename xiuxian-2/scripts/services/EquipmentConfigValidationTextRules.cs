using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentConfigValidationTextRules
    {
        public readonly record struct EquipmentValidationIssue(
            string Scope,
            string Id,
            string Field,
            string Message,
            string LevelId,
            string SeriesId,
            string TemplateId,
            string RecipeId);

        public static List<EquipmentValidationIssue> Validate(string jsonText)
        {
            var issues = new List<EquipmentValidationIssue>();
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return issues;
            }

            using JsonDocument document = JsonDocument.Parse(jsonText);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return issues;
            }

            HashSet<string> levelIds = ReadIds(root, "levels", "level_id");
            HashSet<string> seriesIds = ReadIds(root, "equipment_series", "series_id");
            HashSet<string> templateIds = ReadIds(root, "equipment_templates", "equipment_template_id");

            ValidateSeries(root, levelIds, issues);
            ValidateTemplates(root, seriesIds, issues);
            ValidateRecipes(root, levelIds, templateIds, issues);
            return issues;
        }

        private static void ValidateSeries(JsonElement root, HashSet<string> levelIds, List<EquipmentValidationIssue> issues)
        {
            if (!root.TryGetProperty("equipment_series", out JsonElement array) || array.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (JsonElement item in array.EnumerateArray())
            {
                string seriesId = GetString(item, "series_id");
                if (string.IsNullOrEmpty(seriesId))
                {
                    continue;
                }

                if (!item.TryGetProperty("bind_level_ids", out JsonElement bindLevels) || bindLevels.ValueKind != JsonValueKind.Array || bindLevels.GetArrayLength() == 0)
                {
                    issues.Add(new EquipmentValidationIssue("equipment_series", seriesId, "bind_level_ids", "missing or empty", "", seriesId, "", ""));
                    continue;
                }

                int index = 0;
                foreach (JsonElement levelIdElement in bindLevels.EnumerateArray())
                {
                    string? levelId = levelIdElement.GetString();
                    if (string.IsNullOrEmpty(levelId) || !levelIds.Contains(levelId))
                    {
                        issues.Add(new EquipmentValidationIssue("equipment_series", seriesId, $"bind_level_ids[{index}]", $"'{levelId}' not found in levels[]", levelId ?? "", seriesId, "", ""));
                    }
                    index++;
                }
            }
        }

        private static void ValidateTemplates(JsonElement root, HashSet<string> seriesIds, List<EquipmentValidationIssue> issues)
        {
            if (!root.TryGetProperty("equipment_templates", out JsonElement array) || array.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (JsonElement item in array.EnumerateArray())
            {
                string templateId = GetString(item, "equipment_template_id");
                if (string.IsNullOrEmpty(templateId))
                {
                    continue;
                }

                string seriesId = GetString(item, "series_id");
                if (string.IsNullOrEmpty(seriesId) || !seriesIds.Contains(seriesId))
                {
                    issues.Add(new EquipmentValidationIssue("equipment_template", templateId, "series_id", $"'{seriesId}' not found in equipment_series[]", "", seriesId, templateId, ""));
                }

                string rarity = GetString(item, "rarity_tier");
                if (!IsValidRarity(rarity))
                {
                    issues.Add(new EquipmentValidationIssue("equipment_template", templateId, "rarity_tier", $"'{rarity}' is invalid", "", seriesId, templateId, ""));
                }

                if (!item.TryGetProperty("main_stat_pool", out JsonElement mainStatPool) || mainStatPool.ValueKind != JsonValueKind.Array || mainStatPool.GetArrayLength() == 0)
                {
                    issues.Add(new EquipmentValidationIssue("equipment_template", templateId, "main_stat_pool", "missing or empty", "", seriesId, templateId, ""));
                }

                int min = GetInt(item, "power_budget_min");
                int max = GetInt(item, "power_budget_max");
                if (max < min)
                {
                    issues.Add(new EquipmentValidationIssue("equipment_template", templateId, "power_budget_max", "power_budget_max < power_budget_min", "", seriesId, templateId, ""));
                }
            }
        }

        private static void ValidateRecipes(JsonElement root, HashSet<string> levelIds, HashSet<string> templateIds, List<EquipmentValidationIssue> issues)
        {
            if (!root.TryGetProperty("equipment_exchange_recipes", out JsonElement array) || array.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (JsonElement item in array.EnumerateArray())
            {
                string recipeId = GetString(item, "recipe_id");
                if (string.IsNullOrEmpty(recipeId))
                {
                    continue;
                }

                string levelId = GetString(item, "level_id");
                if (string.IsNullOrEmpty(levelId) || !levelIds.Contains(levelId))
                {
                    issues.Add(new EquipmentValidationIssue("equipment_recipe", recipeId, "level_id", $"'{levelId}' not found in levels[]", levelId, "", "", recipeId));
                }

                string templateId = GetString(item, "output_template_id");
                if (string.IsNullOrEmpty(templateId) || !templateIds.Contains(templateId))
                {
                    issues.Add(new EquipmentValidationIssue("equipment_recipe", recipeId, "output_template_id", $"'{templateId}' not found in equipment_templates[]", levelId, "", templateId, recipeId));
                }
            }
        }

        private static HashSet<string> ReadIds(JsonElement root, string sectionKey, string idKey)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (!root.TryGetProperty(sectionKey, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (JsonElement item in array.EnumerateArray())
            {
                string id = GetString(item, idKey);
                if (!string.IsNullOrEmpty(id))
                {
                    result.Add(id);
                }
            }

            return result;
        }

        private static bool IsValidRarity(string rarity)
        {
            return rarity == "common_tool" || rarity == "artifact" || rarity == "spirit" || rarity == "treasure";
        }

        private static string GetString(JsonElement item, string propertyName)
        {
            return item.TryGetProperty(propertyName, out JsonElement element) && element.ValueKind == JsonValueKind.String
                ? element.GetString() ?? string.Empty
                : string.Empty;
        }

        private static int GetInt(JsonElement item, string propertyName)
        {
            return item.TryGetProperty(propertyName, out JsonElement element) && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int value)
                ? value
                : 0;
        }
    }
}
