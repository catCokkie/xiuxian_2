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

        string serialized = PrototypeRootSaveContract.Serialize(snapshot, 1712345678);

        Assert.Contains("[meta]", serialized);
        Assert.Contains("version=5", serialized);
        Assert.Contains("[ui]", serialized);
        Assert.Contains("submenu_active_left_tab=\"StatsTab\"", serialized);
        Assert.Contains("submenu_active_right_tab=\"SettingsTab\"", serialized);
        Assert.Contains("[input]", serialized);
        Assert.Contains("stats={", serialized);
        Assert.Contains("[backpack]", serialized);
        Assert.Contains("[resource]", serialized);
        Assert.Contains("[progress]", serialized);
        Assert.Contains("[action]", serialized);
        Assert.Contains("[explore]", serialized);
        Assert.Contains("[level]", serialized);
        Assert.Contains("[settings]", serialized);

        AssertFixtureMatchesSerializedOutput(GetFixturePath("phase2-save-v5.cfg"), serialized);
    }

    [Fact]
    public void ReadSnapshotMigratesLegacyUiTabAndDefaultsMissingRuntimeSections()
    {
        PrototypeRootSaveSnapshot snapshot = PrototypeRootSaveContract.Deserialize(File.ReadAllText(GetFixturePath("phase2-legacy-save-v1.cfg")));

        Assert.Equal(1, snapshot.SchemaVersion);
        Assert.Equal("StatsTab", snapshot.Ui.ActiveLeftTab);
        Assert.Equal("OnlineTab", snapshot.Ui.ActiveRightTab);
        Assert.True(snapshot.Ui.SubmenuVisible);
        Assert.True(snapshot.HookPaused);
        Assert.Equal(CreateExpectedLegacyInputStats(), snapshot.InputStats);
        Assert.Equal(CreateExpectedLegacyBackpackItems(), snapshot.BackpackItems);
        Assert.Equal(CreateExpectedLegacyResourceWallet(), snapshot.ResourceWallet);
        Assert.Equal(CreateExpectedLegacyPlayerProgress(), snapshot.PlayerProgress);
        Assert.Equal(CreateExpectedLegacyActionMode(), snapshot.ActionMode);
        Assert.Equal(CreateExpectedMissingOptionalSection(), snapshot.LevelRuntime);
        Assert.Equal(CreateExpectedMissingOptionalSection(), snapshot.ExploreRuntime);
        Assert.Equal(CreateExpectedMissingOptionalSection(), snapshot.SystemSettings);
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

    private static Dictionary<string, object?> CreateExpectedLegacyInputStats()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["total_key_down"] = 4L,
            ["total_mouse_click"] = 2L,
            ["total_scroll_steps"] = 1L,
            ["total_move_distance"] = 144.0,
            ["total_joypad_button"] = 0L,
            ["total_joypad_axis"] = 0L,
            ["ap_accumulator"] = 6.25
        };
    }

    private static Dictionary<string, object?> CreateExpectedLegacyBackpackItems()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["spirit_stone"] = 5
        };
    }

    private static Dictionary<string, object?> CreateExpectedLegacyResourceWallet()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["lingqi"] = 18.0,
            ["insight"] = 3.0,
            ["pet_affinity"] = 1.5
        };
    }

    private static Dictionary<string, object?> CreateExpectedLegacyPlayerProgress()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["realm_level"] = 1,
            ["realm_exp"] = 6.0,
            ["pet_mood"] = 70
        };
    }

    private static Dictionary<string, object?> CreateExpectedLegacyActionMode()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["mode_id"] = "dungeon"
        };
    }

    private static Dictionary<string, object?> CreateExpectedMissingOptionalSection()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal);
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

    private static void AssertFixtureMatchesSerializedOutput(string expectedPath, string actual)
    {
        PrototypeRootSaveSnapshot expectedSnapshot = PrototypeRootSaveContract.Deserialize(File.ReadAllText(expectedPath));
        PrototypeRootSaveSnapshot actualSnapshot = PrototypeRootSaveContract.Deserialize(actual);

        Assert.Equal(expectedSnapshot.SchemaVersion, actualSnapshot.SchemaVersion);
        Assert.Equal(expectedSnapshot.Ui, actualSnapshot.Ui);
        Assert.Equal(expectedSnapshot.HookPaused, actualSnapshot.HookPaused);
        AssertRawValueEqual(expectedSnapshot.InputStats, actualSnapshot.InputStats);
        AssertRawValueEqual(expectedSnapshot.BackpackItems, actualSnapshot.BackpackItems);
        AssertRawValueEqual(expectedSnapshot.ResourceWallet, actualSnapshot.ResourceWallet);
        AssertRawValueEqual(expectedSnapshot.PlayerProgress, actualSnapshot.PlayerProgress);
        AssertRawValueEqual(expectedSnapshot.ActionMode, actualSnapshot.ActionMode);
        AssertRawValueEqual(expectedSnapshot.ExploreRuntime, actualSnapshot.ExploreRuntime);
        AssertRawValueEqual(expectedSnapshot.LevelRuntime, actualSnapshot.LevelRuntime);
        AssertRawValueEqual(expectedSnapshot.SystemSettings, actualSnapshot.SystemSettings);
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
