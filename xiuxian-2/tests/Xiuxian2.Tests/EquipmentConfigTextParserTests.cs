using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class EquipmentConfigTextParserTests
{
    [Fact]
    public void ParseIndexes_IndexesSeriesAndTemplatesById()
    {
        var parsed = EquipmentConfigTextParser.ParseIndexes(BuildConfigJson());

        Assert.True(parsed.SeriesJsonById.ContainsKey("series_qi_outer_cave"));
        Assert.True(parsed.TemplateJsonById.ContainsKey("eq_weapon_qi_outer_moss_blade"));
        Assert.Contains("幽泉洞窟试炼系", parsed.SeriesJsonById["series_qi_outer_cave"]);
        Assert.Contains("artifact", parsed.TemplateJsonById["eq_weapon_qi_outer_moss_blade"]);
    }

    [Fact]
    public void ParseIndexes_GroupsExchangeRecipesByLevelId()
    {
        var parsed = EquipmentConfigTextParser.ParseIndexes(BuildConfigJson());

        Assert.True(parsed.ExchangeRecipeJsonByLevelId.ContainsKey("lv_qi_001"));
        Assert.Single(parsed.ExchangeRecipeJsonByLevelId["lv_qi_001"]);
        Assert.Contains("recipe_qi_outer_weapon_01", parsed.ExchangeRecipeJsonByLevelId["lv_qi_001"][0]);
    }

    [Fact]
    public void ParseIndexes_IgnoresEntriesMissingIds()
    {
        var parsed = EquipmentConfigTextParser.ParseIndexes(
            """
            {
              "equipment_series": [{ "series_name": "missing id" }],
              "equipment_templates": [{ "display_name": "missing id" }],
              "equipment_exchange_recipes": [{ "recipe_id": "r1" }]
            }
            """);

        Assert.Empty(parsed.SeriesJsonById);
        Assert.Empty(parsed.TemplateJsonById);
        Assert.Empty(parsed.ExchangeRecipeJsonByLevelId);
    }

    private static string BuildConfigJson()
    {
        return
            """
            {
              "equipment_series": [
                {
                  "series_id": "series_qi_outer_cave",
                  "series_name": "幽泉洞窟试炼系"
                }
              ],
              "equipment_templates": [
                {
                  "equipment_template_id": "eq_weapon_qi_outer_moss_blade",
                  "display_name": "苔锋短刃",
                  "rarity_tier": "artifact"
                }
              ],
              "equipment_exchange_recipes": [
                {
                  "recipe_id": "recipe_qi_outer_weapon_01",
                  "level_id": "lv_qi_001",
                  "output_template_id": "eq_weapon_qi_outer_moss_blade"
                }
              ]
            }
            """;
    }
}
