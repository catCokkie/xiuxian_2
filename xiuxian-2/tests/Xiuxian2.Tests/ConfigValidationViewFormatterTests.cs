using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class ConfigValidationViewFormatterTests
{
    [Fact]
    public void FilterEntries_RespectsScopeAndActiveLevel()
    {
        ConfigValidationViewFormatter.ConfigValidationItem[] entries =
        {
            CreateEntry("level", "lv_a", "progress_per_100_inputs", "must be > 0", levelId: "lv_a"),
            CreateEntry("monster", "monster_a", "hp", "must be > 0", levelId: "lv_a", monsterId: "monster_a"),
            CreateEntry("monster", "monster_b", "hp", "must be > 0", levelId: "lv_b", monsterId: "monster_b"),
            CreateEntry("config", "root", "levels", "must not be empty")
        };

        var filtered = ConfigValidationViewFormatter.FilterItems(entries, "monster", true, "lv_a");

        Assert.Single(filtered);
        Assert.Equal("monster_a", filtered[0].Id);
    }

    [Fact]
    public void BuildBody_IncludesIssueDetailsAndRemainingCount()
    {
        ConfigValidationViewFormatter.ConfigValidationItem[] filtered =
        {
            CreateEntry("level", "lv_a", "progress_per_100_inputs", "must be > 0", levelId: "lv_a"),
            CreateEntry("drop_table", "drop_alpha", "items", "must not be empty", levelId: "lv_a", dropTableId: "drop_alpha"),
            CreateEntry("monster", "monster_a", "hp", "must be > 0", levelId: "lv_a", monsterId: "monster_a")
        };

        string body = ConfigValidationViewFormatter.BuildBody(filtered, 2);

        Assert.Contains("• level/lv_a progress_per_100_inputs must be > 0 (level_id=lv_a)", body);
        Assert.Contains("• drop_table/drop_alpha items must not be empty (level_id=lv_a, drop_table_id=drop_alpha)", body);
        Assert.Contains("… 还有 1 项", body);
    }

    [Fact]
    public void BuildTitle_UsesPassStateWhenNoIssuesRemain()
    {
        string title = ConfigValidationViewFormatter.BuildTitle(0, 3, "monster, active-level");

        Assert.Equal("配置校验：通过 (monster, active-level)", title);
    }

    [Fact]
    public void BuildSimulationLabels_ReflectCurrentSelection()
    {
        string level = ConfigValidationViewFormatter.BuildSimulationLevelLabel("lv_test_001", "幽泉洞窟", useActiveLevel: false);
        string monster = ConfigValidationViewFormatter.BuildSimulationMonsterLabel("monster_slime_moss", useAutoMonster: false);

        Assert.Equal("模拟关卡：幽泉洞窟 (lv_test_001)", level);
        Assert.Equal("模拟怪物：monster_slime_moss", monster);
    }

    [Fact]
    public void BuildSimulationStatus_FallsBackWhenNotRunYet()
    {
        string status = ConfigValidationViewFormatter.BuildSimulationStatus("");

        Assert.Equal("模拟结果：尚未运行", status);
    }

    private static ConfigValidationViewFormatter.ConfigValidationItem CreateEntry(
        string scope,
        string id,
        string field,
        string message,
        string levelId = "",
        string monsterId = "",
        string dropTableId = "")
    {
        return new ConfigValidationViewFormatter.ConfigValidationItem(scope, id, field, message, levelId, monsterId, dropTableId);
    }
}
