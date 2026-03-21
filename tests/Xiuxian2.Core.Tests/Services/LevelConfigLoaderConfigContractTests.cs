using Godot;
using Xiuxian2.Core.Tests.Builders;
using Xunit;

namespace Xiuxian2.Core.Tests.Services;

public sealed class LevelConfigLoaderConfigContractTests
{
    [Fact]
    public void ValidFixtureLoadsWithoutValidationDrift()
    {
        var fixture = new ServiceFixtureBuilder()
            .WithFrozenConfigRelativePath(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config", "phase2-valid-level-config.json"))
            .Build();

        var runtime = CreateRuntime(fixture);

        var loaded = runtime.LoadConfig(fixture.FrozenConfigPath);

        Assert.True(loaded);
        Assert.Equal("starter_plain", runtime.ActiveLevelId);
        Assert.Equal("Starter Plain", runtime.ActiveLevelName);
        Assert.Empty(runtime.GetValidationIssues());
        Assert.Empty(runtime.GetValidationEntries());
        Assert.Equal("config validation: OK", runtime.BuildValidationSummary());
    }

    [Fact]
    public void InvalidFixtureProducesStableStructuredValidationEntries()
    {
        var fixture = new ServiceFixtureBuilder()
            .WithFrozenConfigRelativePath(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config", "phase2-invalid-level-config.json"))
            .Build();

        var runtime = CreateRuntime(fixture);

        var loaded = runtime.LoadConfig(fixture.FrozenConfigPath);

        Assert.True(loaded);

        var issues = runtime.GetValidationIssues();
        var entries = runtime.GetValidationEntries();

        Assert.Equal(10, issues.Count);
        Assert.Equal(10, entries.Count);

        AssertValidationEntry(entries[0], "level", "misty_bog", "spawn_table[0].monster_id", "spawn monster 'ghost_slug' not found in monsters[]", levelId: "misty_bog", monsterId: "ghost_slug");
        AssertValidationEntry(entries[1], "level", "misty_bog", "spawn_table.total_weight", "total weight = 90 (expected 100)", levelId: "misty_bog");
        AssertValidationEntry(entries[2], "monster", "bog_rat", "drops.drop_table_id", "drop table 'missing_table' not found", monsterId: "bog_rat", dropTableId: "missing_table");
        AssertValidationEntry(entries[3], "drop_table", "bog_table", "bind_level_id", "'missing_level' not found in levels[]", levelId: "missing_level", dropTableId: "bog_table");
        AssertValidationEntry(entries[4], "drop_table", "bog_table", "bind_monster_ids[0]", "is empty", dropTableId: "bog_table");
        AssertValidationEntry(entries[5], "drop_table", "bog_table", "bind_monster_ids[1]", "bound monster 'ghost_slug' not found", monsterId: "ghost_slug", dropTableId: "bog_table");
        AssertValidationEntry(entries[6], "drop_table", "bog_table", "entries[0].item_id", "missing", dropTableId: "bog_table");
        AssertValidationEntry(entries[7], "drop_table", "bog_table", "entries[0].weight", "weight <= 0", dropTableId: "bog_table");
        AssertValidationEntry(entries[8], "drop_table", "bog_table", "entries[0].qty_max", "qty_max < qty_min", dropTableId: "bog_table");
        AssertValidationEntry(entries[9], "drop_table", "bog_table", "entries.total_weight", "total weight <= 0", dropTableId: "bog_table");

        Assert.Equal("level misty_bog: spawn_table[0].monster_id spawn monster 'ghost_slug' not found in monsters[].", issues[0]);
        Assert.Equal("drop_table bog_table: entries.total_weight total weight <= 0.", issues[9]);

        var summary = runtime.BuildValidationSummary(3);
        Assert.Equal(
            "config validation: 10 issue(s)\n- level misty_bog: spawn_table[0].monster_id spawn monster 'ghost_slug' not found in monsters[].\n- level misty_bog: spawn_table.total_weight total weight = 90 (expected 100).\n- monster bog_rat: drops.drop_table_id drop table 'missing_table' not found.\n- ... and 7 more",
            summary);
    }

    [Fact]
    public void ContractFixturesStayOwnedByTheTestProject()
    {
        var validFixture = new ServiceFixtureBuilder()
            .WithFrozenConfigRelativePath(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config", "phase2-valid-level-config.json"))
            .Build();
        var invalidFixture = new ServiceFixtureBuilder()
            .WithFrozenConfigRelativePath(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config", "phase2-invalid-level-config.json"))
            .Build();

        Assert.Contains(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config"), validFixture.FrozenConfigPath);
        Assert.Contains(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config"), invalidFixture.FrozenConfigPath);
        Assert.DoesNotContain("09_level_monster_drop_sample.json", validFixture.LoadFrozenConfigText());
        Assert.DoesNotContain("09_level_monster_drop_sample.json", invalidFixture.LoadFrozenConfigText());
    }

    private static Xiuxian.Scripts.Services.LevelConfigLoader.SeamRuntime CreateRuntime(ServiceFixture fixture)
    {
        return new Xiuxian.Scripts.Services.LevelConfigLoader.SeamRuntime(fixture.ConfigSource, fixture.Rng, fixture.Clock);
    }

    private static void AssertValidationEntry(
        Godot.Collections.Dictionary<string, Variant> entry,
        string scope,
        string id,
        string field,
        string message,
        string levelId = "",
        string monsterId = "",
        string dropTableId = "")
    {
        Assert.Equal(scope, entry["scope"].AsString());
        Assert.Equal(id, entry["id"].AsString());
        Assert.Equal(field, entry["field"].AsString());
        Assert.Equal("error", entry["severity"].AsString());
        Assert.Equal(message, entry["message"].AsString());
        Assert.Equal(levelId, entry["level_id"].AsString());
        Assert.Equal(monsterId, entry["monster_id"].AsString());
        Assert.Equal(dropTableId, entry["drop_table_id"].AsString());
    }
}
