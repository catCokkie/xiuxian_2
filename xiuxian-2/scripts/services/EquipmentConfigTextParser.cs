using System.Collections.Generic;
using System.Text.Json;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentConfigTextParser
    {
        public sealed record ParsedEquipmentIndexes(
            Dictionary<string, string> SeriesJsonById,
            Dictionary<string, string> TemplateJsonById,
            Dictionary<string, List<string>> ExchangeRecipeJsonByLevelId);

        public static ParsedEquipmentIndexes ParseIndexes(string jsonText)
        {
            var series = new Dictionary<string, string>();
            var templates = new Dictionary<string, string>();
            var recipes = new Dictionary<string, List<string>>();

            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return new ParsedEquipmentIndexes(series, templates, recipes);
            }

            using JsonDocument document = JsonDocument.Parse(jsonText);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new ParsedEquipmentIndexes(series, templates, recipes);
            }

            IndexById(root, "equipment_series", "series_id", series);
            IndexById(root, "equipment_templates", "equipment_template_id", templates);

            if (root.TryGetProperty("equipment_exchange_recipes", out JsonElement recipesArray)
                && recipesArray.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in recipesArray.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("level_id", out JsonElement levelIdElement))
                    {
                        continue;
                    }

                    string? levelId = levelIdElement.GetString();
                    if (string.IsNullOrEmpty(levelId))
                    {
                        continue;
                    }

                    if (!recipes.TryGetValue(levelId, out List<string>? list))
                    {
                        list = new List<string>();
                        recipes[levelId] = list;
                    }

                    list.Add(item.GetRawText());
                }
            }

            return new ParsedEquipmentIndexes(series, templates, recipes);
        }

        private static void IndexById(JsonElement root, string sectionKey, string idKey, Dictionary<string, string> destination)
        {
            if (!root.TryGetProperty(sectionKey, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (JsonElement item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty(idKey, out JsonElement idElement))
                {
                    continue;
                }

                string? id = idElement.GetString();
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                destination[id] = item.GetRawText();
            }
        }
    }
}
