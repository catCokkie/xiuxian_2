using System.Collections;
using System.Collections.Generic;
using Xiuxian.Scripts.Game;
using Xiuxian2.Core.Tests.Support.Serialization;
using Xunit;

namespace Xiuxian2.Core.Tests.Game;

public sealed class PrototypeRootSaveContractTests
{
    [Fact]
    public void WriteSnapshotEmitsFrozenSchemaFiveSaveLayout()
    {
        var snapshot = CreateCurrentSnapshot();
        var config = new Godot.ConfigFile();

        PrototypeRootSaveContract.Write(config, snapshot, 1712345678);

        Assert.Equal(5, config.GetValue("meta", "version", 0).AsInt32());
        Assert.True(config.HasSection("ui"));
        Assert.True(config.HasSectionKey("ui", "submenu_active_left_tab"));
        Assert.True(config.HasSectionKey("ui", "submenu_active_right_tab"));
        Assert.True(config.HasSectionKey("input", "stats"));
        Assert.True(config.HasSectionKey("backpack", "items"));
        Assert.True(config.HasSectionKey("resource", "wallet"));
        Assert.True(config.HasSectionKey("progress", "player"));
        Assert.True(config.HasSectionKey("action", "mode"));
        Assert.True(config.HasSectionKey("explore", "runtime"));
        Assert.True(config.HasSectionKey("level", "runtime"));
        Assert.True(config.HasSectionKey("settings", "system"));

        string fixturePath = GetFixturePath("phase2-save-v5.cfg");
        string actualPath = Path.Combine(Path.GetTempPath(), $"prototype-root-save-{Guid.NewGuid():N}.cfg");

        try
        {
            Assert.Equal(Godot.Error.Ok, config.Save(actualPath));
            AssertFixtureMatchesConfig(fixturePath, actualPath);
        }
        finally
        {
            if (File.Exists(actualPath))
            {
                File.Delete(actualPath);
            }
        }
    }

    [Fact]
    public void ReadSnapshotMigratesLegacyUiTabAndDefaultsMissingRuntimeSections()
    {
        var config = new Godot.ConfigFile();
        Assert.Equal(Godot.Error.Ok, config.Load(GetFixturePath("phase2-legacy-save-v1.cfg")));

        PrototypeRootSaveSnapshot snapshot = PrototypeRootSaveContract.Read(config);

        Assert.Equal(1, snapshot.SchemaVersion);
        Assert.Equal("StatsTab", snapshot.Ui.ActiveLeftTab);
        Assert.Equal("OnlineTab", snapshot.Ui.ActiveRightTab);
        Assert.True(snapshot.Ui.SubmenuVisible);
        Assert.Equal(StateSerializationFixtureBuilder.CreateInputActivityLegacyPayload(), snapshot.InputStats);
        Assert.Equal(StateSerializationFixtureBuilder.CreateBackpackLegacyPayload(), snapshot.BackpackItems);
        Assert.Equal(StateSerializationFixtureBuilder.CreateResourceWalletLegacyPayload(), snapshot.ResourceWallet);
        Assert.Equal(StateSerializationFixtureBuilder.CreatePlayerProgressLegacyPayload(), snapshot.PlayerProgress);
        Assert.Equal(StateSerializationFixtureBuilder.CreatePlayerActionLegacyPayload(), snapshot.ActionMode);
        Assert.Equal(CreateExpectedDefaultLevelRuntime(), snapshot.LevelRuntime);
        Assert.Equal(CreateExpectedDefaultExploreRuntime(), snapshot.ExploreRuntime);
        Assert.Equal(CreateExpectedDefaultSystemSettings(), snapshot.SystemSettings);
    }

    [Fact]
    public void ContractSuiteStaysRuntimeFreeAndAvoidsUserSavePaths()
    {
        string currentFixturePath = GetFixturePath("phase2-save-v5.cfg");
        string legacyFixturePath = GetFixturePath("phase2-legacy-save-v1.cfg");

        Assert.Contains(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "save"), currentFixturePath);
        Assert.Contains(Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "save"), legacyFixturePath);
        Assert.DoesNotContain("user://save_state.cfg", File.ReadAllText(currentFixturePath));
        Assert.DoesNotContain("user://save_state.cfg", File.ReadAllText(legacyFixturePath));
    }

    private static PrototypeRootSaveSnapshot CreateCurrentSnapshot()
    {
        return new PrototypeRootSaveSnapshot
        {
            Ui = new PrototypeRootUiSnapshot(240.5f, 640.0f, true, "StatsTab", "SettingsTab"),
            InputStats = StateSerializationFixtureBuilder.CreateInputActivityRoundTripPayload(),
            HookPaused = false,
            BackpackItems = StateSerializationFixtureBuilder.CreateBackpackRoundTripPayload(),
            ResourceWallet = StateSerializationFixtureBuilder.CreateResourceWalletRoundTripPayload(),
            PlayerProgress = StateSerializationFixtureBuilder.CreatePlayerProgressRoundTripPayload(),
            ActionMode = StateSerializationFixtureBuilder.CreatePlayerActionRoundTripPayload(),
            ExploreRuntime = CreateExpectedCurrentExploreRuntime(),
            LevelRuntime = CreateExpectedCurrentLevelRuntime(),
            SystemSettings = CreateExpectedDefaultSystemSettings()
        };
    }

    private static Dictionary<string, object?> CreateExpectedCurrentExploreRuntime()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["zone_id"] = "starter_plain",
            ["zone_name"] = "Starter Plain",
            ["explore_progress"] = 42.5,
            ["battle_state"] = "in_battle",
            ["move_frame_counter"] = 7,
            ["queue_move_input_pending"] = 3,
            ["player_hp"] = 28,
            ["player_max_hp"] = 36,
            ["enemy_hp"] = 11,
            ["enemy_max_hp"] = 24,
            ["enemy_attack_power"] = 5,
            ["inputs_per_battle_round_runtime"] = 18,
            ["player_attack_per_round_runtime"] = 6,
            ["enemy_damage_divider_runtime"] = 4,
            ["enemy_min_damage_runtime"] = 1,
            ["battle_round_counter"] = 2,
            ["pending_battle_input_events"] = 9,
            ["battle_monster_index"] = 1,
            ["battle_monster_id"] = "green_slime",
            ["battle_monster_name"] = "Green Slime",
            ["monster_marker_states"] = new List<object?>
            {
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["x"] = 540.0,
                    ["y"] = 0.0,
                    ["monster_id"] = "green_slime",
                    ["move_pending"] = 1,
                    ["move_threshold"] = 4
                },
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["x"] = 650.0,
                    ["y"] = 0.0,
                    ["monster_id"] = "bog_rat",
                    ["move_pending"] = 0,
                    ["move_threshold"] = 5
                }
            }
        };
    }

    private static Dictionary<string, object?> CreateExpectedCurrentLevelRuntime()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["active_level_id"] = "starter_plain",
            ["active_wave_index"] = 2,
            ["unlocked_level_ids"] = new List<object?> { "misty_bog", "starter_plain" },
            ["boss_cleared_level_ids"] = new List<object?> { "starter_plain" },
            ["level_clear_count_by_id"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["misty_bog"] = 3,
                ["starter_plain"] = 1
            },
            ["pity_counter_by_key"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["bog_pity"] = 4,
                ["slime_pity"] = 2
            },
            ["daily_roll_count_by_table"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["starter_plain_green_slime"] = 5
            },
            ["daily_roll_day_by_table"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["starter_plain_green_slime"] = 12L
            },
            ["hourly_roll_count_by_table"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["starter_plain_green_slime"] = 2
            },
            ["hourly_roll_hour_by_table"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["starter_plain_green_slime"] = 48L
            }
        };
    }

    private static Dictionary<string, object?> CreateExpectedDefaultLevelRuntime()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["active_level_id"] = string.Empty,
            ["active_wave_index"] = 0,
            ["unlocked_level_ids"] = new List<object?>(),
            ["boss_cleared_level_ids"] = new List<object?>(),
            ["level_clear_count_by_id"] = new Dictionary<string, object?>(StringComparer.Ordinal),
            ["pity_counter_by_key"] = new Dictionary<string, object?>(StringComparer.Ordinal),
            ["daily_roll_count_by_table"] = new Dictionary<string, object?>(StringComparer.Ordinal),
            ["daily_roll_day_by_table"] = new Dictionary<string, object?>(StringComparer.Ordinal),
            ["hourly_roll_count_by_table"] = new Dictionary<string, object?>(StringComparer.Ordinal),
            ["hourly_roll_hour_by_table"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        };
    }

    private static Dictionary<string, object?> CreateExpectedDefaultExploreRuntime()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["zone_id"] = string.Empty,
            ["zone_name"] = string.Empty,
            ["explore_progress"] = 0.0,
            ["battle_state"] = "exploring",
            ["move_frame_counter"] = 0,
            ["queue_move_input_pending"] = 0,
            ["player_hp"] = 0,
            ["player_max_hp"] = 1,
            ["enemy_hp"] = 0,
            ["enemy_max_hp"] = 1,
            ["enemy_attack_power"] = 1,
            ["inputs_per_battle_round_runtime"] = 1,
            ["player_attack_per_round_runtime"] = 1,
            ["enemy_damage_divider_runtime"] = 1,
            ["enemy_min_damage_runtime"] = 1,
            ["battle_round_counter"] = 0,
            ["pending_battle_input_events"] = 0,
            ["battle_monster_index"] = -1,
            ["battle_monster_id"] = string.Empty,
            ["battle_monster_name"] = string.Empty,
            ["monster_marker_states"] = new List<object?>()
        };
    }

    private static Dictionary<string, object?> CreateExpectedDefaultSystemSettings()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["language"] = "zh-CN",
            ["keep_on_top"] = false,
            ["taskbar_icon"] = true,
            ["startup_animation"] = true,
            ["admin_mode"] = false,
            ["handwriting_support"] = false,
            ["vsync"] = true,
            ["max_fps"] = 60,
            ["resolution"] = "1600x900",
            ["show_control_markers"] = true,
            ["show_validation_panel"] = true,
            ["game_scale"] = 1.33,
            ["ui_scale"] = 1.0,
            ["auto_save_interval_sec"] = 10,
            ["cloud_sync"] = false,
            ["milestone_tips"] = true,
            ["global_debug_overlay"] = false
        };
    }

    private static string GetFixturePath(string fileName)
    {
        string repositoryRoot = ResolveRepositoryRoot();
        return Path.Combine(repositoryRoot, "tests", "Xiuxian2.Core.Tests", "Fixtures", "save", fileName);
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Directory.Packages.props"))
                && Directory.Exists(Path.Combine(current.FullName, "xiuxian-2")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }

    private static void AssertFixtureMatchesConfig(string expectedPath, string actualPath)
    {
        var expected = new Godot.ConfigFile();
        var actual = new Godot.ConfigFile();

        Assert.Equal(Godot.Error.Ok, expected.Load(expectedPath));
        Assert.Equal(Godot.Error.Ok, actual.Load(actualPath));

        AssertConfigEqual(expected, actual);
    }

    private static void AssertConfigEqual(Godot.ConfigFile expected, Godot.ConfigFile actual)
    {
        Assert.Equal(expected.GetSections(), actual.GetSections());

        foreach (string section in expected.GetSections())
        {
            Assert.Equal(expected.GetSectionKeys(section), actual.GetSectionKeys(section));

            foreach (string key in expected.GetSectionKeys(section))
            {
                AssertVariantEqual(expected.GetValue(section, key), actual.GetValue(section, key));
            }
        }
    }

    private static void AssertVariantEqual(Godot.Variant expected, Godot.Variant actual)
    {
        Assert.Equal(expected.VariantType, actual.VariantType);

        switch (expected.VariantType)
        {
            case Godot.Variant.Type.Dictionary:
                AssertRawValueEqual(
                    Xiuxian.Scripts.Services.RawVariantBridge.ToRawDictionary((Godot.Collections.Dictionary<string, Godot.Variant>)expected),
                    Xiuxian.Scripts.Services.RawVariantBridge.ToRawDictionary((Godot.Collections.Dictionary<string, Godot.Variant>)actual));
                break;
            case Godot.Variant.Type.Array:
                AssertRawValueEqual(
                    Xiuxian.Scripts.Services.RawVariantBridge.ToRawDictionary(new Godot.Collections.Dictionary<string, Godot.Variant> { ["value"] = expected })["value"],
                    Xiuxian.Scripts.Services.RawVariantBridge.ToRawDictionary(new Godot.Collections.Dictionary<string, Godot.Variant> { ["value"] = actual })["value"]);
                break;
            case Godot.Variant.Type.Float:
                Assert.Equal(expected.AsDouble(), actual.AsDouble(), 6);
                break;
            case Godot.Variant.Type.Int:
                Assert.Equal(expected.AsInt64(), actual.AsInt64());
                break;
            case Godot.Variant.Type.Bool:
                Assert.Equal(expected.AsBool(), actual.AsBool());
                break;
            default:
                Assert.Equal(expected.ToString(), actual.ToString());
                break;
        }
    }

    private static void AssertRawValueEqual(object? expected, object? actual)
    {
        switch (expected)
        {
            case IReadOnlyDictionary<string, object?> expectedDictionary:
                var actualDictionary = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(actual);
                Assert.Equal(expectedDictionary.Count, actualDictionary.Count);
                foreach ((string key, object? value) in expectedDictionary)
                {
                    Assert.True(actualDictionary.ContainsKey(key), $"Missing key '{key}'.");
                    AssertRawValueEqual(value, actualDictionary[key]);
                }
                break;
            case IEnumerable expectedEnumerable when expected is not string:
                AssertEnumerableEqual(expectedEnumerable, Assert.IsAssignableFrom<IEnumerable>(actual));
                break;
            case double expectedDouble:
                Assert.Equal(expectedDouble, Assert.IsType<double>(actual), 6);
                break;
            default:
                Assert.Equal(expected, actual);
                break;
        }
    }

    private static void AssertEnumerableEqual(IEnumerable expected, IEnumerable actual)
    {
        IEnumerator expectedEnumerator = expected.GetEnumerator();
        IEnumerator actualEnumerator = actual.GetEnumerator();

        while (true)
        {
            bool hasExpected = expectedEnumerator.MoveNext();
            bool hasActual = actualEnumerator.MoveNext();

            Assert.Equal(hasExpected, hasActual);
            if (!hasExpected)
            {
                return;
            }

            AssertRawValueEqual(expectedEnumerator.Current, actualEnumerator.Current);
        }
    }
}
