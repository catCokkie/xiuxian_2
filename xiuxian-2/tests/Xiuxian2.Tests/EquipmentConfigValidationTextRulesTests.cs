using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class EquipmentConfigValidationTextRulesTests
{
    [Fact]
    public void Validate_FlagsTemplateWithUnknownSeriesAndInvalidRarity()
    {
        var issues = EquipmentConfigValidationTextRules.Validate(
            """
            {
              "levels": [{ "level_id": "lv_qi_001" }],
              "equipment_series": [],
              "equipment_templates": [
                {
                  "equipment_template_id": "eq_weapon_bad",
                  "series_id": "series_missing",
                  "rarity_tier": "legendary",
                  "main_stat_pool": [{ "stat": "attack_flat", "weight": 100, "min": 1, "max": 2 }],
                  "power_budget_min": 10,
                  "power_budget_max": 8
                }
              ],
              "equipment_exchange_recipes": []
            }
            """);

        Assert.Contains(issues, issue => issue.Scope == "equipment_template" && issue.Field == "series_id");
        Assert.Contains(issues, issue => issue.Scope == "equipment_template" && issue.Field == "rarity_tier");
        Assert.Contains(issues, issue => issue.Scope == "equipment_template" && issue.Field == "power_budget_max");
    }

    [Fact]
    public void Validate_FlagsRecipeReferencingMissingLevelAndTemplate()
    {
        var issues = EquipmentConfigValidationTextRules.Validate(
            """
            {
              "levels": [{ "level_id": "lv_qi_001" }],
              "equipment_series": [{ "series_id": "series_qi_outer", "bind_level_ids": ["lv_qi_001"] }],
              "equipment_templates": [
                {
                  "equipment_template_id": "eq_weapon_ok",
                  "series_id": "series_qi_outer",
                  "rarity_tier": "artifact",
                  "main_stat_pool": [{ "stat": "attack_flat", "weight": 100, "min": 1, "max": 2 }],
                  "power_budget_min": 8,
                  "power_budget_max": 10
                }
              ],
              "equipment_exchange_recipes": [
                {
                  "recipe_id": "recipe_bad",
                  "level_id": "lv_missing",
                  "output_template_id": "eq_missing"
                }
              ]
            }
            """);

        Assert.Contains(issues, issue => issue.Scope == "equipment_recipe" && issue.Field == "level_id");
        Assert.Contains(issues, issue => issue.Scope == "equipment_recipe" && issue.Field == "output_template_id");
    }

    [Fact]
    public void Validate_AllowsWellFormedEquipmentConfig()
    {
        var issues = EquipmentConfigValidationTextRules.Validate(
            """
            {
              "levels": [{ "level_id": "lv_qi_001" }],
              "equipment_series": [
                {
                  "series_id": "series_qi_outer",
                  "bind_level_ids": ["lv_qi_001"]
                }
              ],
              "equipment_templates": [
                {
                  "equipment_template_id": "eq_weapon_ok",
                  "series_id": "series_qi_outer",
                  "rarity_tier": "artifact",
                  "main_stat_pool": [{ "stat": "attack_flat", "weight": 100, "min": 1, "max": 2 }],
                  "power_budget_min": 8,
                  "power_budget_max": 10
                }
              ],
              "equipment_exchange_recipes": [
                {
                  "recipe_id": "recipe_ok",
                  "level_id": "lv_qi_001",
                  "output_template_id": "eq_weapon_ok"
                }
              ]
            }
            """);

        Assert.Empty(issues);
    }
}
