using System.Collections;
using System.Collections.Generic;
using Xiuxian.Scripts.Services;
using Xunit;

namespace Xiuxian2.Core.Tests.Services;

public sealed class LevelConfigLoaderRuntimeDictionaryTests
{
    [Fact]
    public void RuntimePayloadRoundTripsIntoDeterministicSaveShape()
    {
        var payload = new Dictionary<string, object?>
        {
            ["active_level_id"] = "starter_plain",
            ["active_wave_index"] = 2,
            ["unlocked_level_ids"] = new List<object?> { "misty_bog", "starter_plain" },
            ["boss_cleared_level_ids"] = new List<object?> { "starter_plain" },
            ["level_clear_count_by_id"] = new Dictionary<string, object?>
            {
                ["misty_bog"] = 3,
                ["starter_plain"] = 1
            },
            ["pity_counter_by_key"] = new Dictionary<string, object?>
            {
                ["slime_pity"] = 2,
                ["bog_pity"] = 4
            },
            ["daily_roll_count_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = 5
            },
            ["daily_roll_day_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = 12L
            },
            ["hourly_roll_count_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = 2
            },
            ["hourly_roll_hour_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = 48L
            }
        };

        var normalized = LevelRuntimeSerializationContracts.NormalizeRaw(payload, "starter_plain", ["starter_plain", "misty_bog"], 3);
        var restored = LevelRuntimeSerializationContracts.ToRawDictionary(normalized);

        var expected = new Dictionary<string, object?>
        {
            ["active_level_id"] = "starter_plain",
            ["active_wave_index"] = 2,
            ["unlocked_level_ids"] = new List<object?> { "misty_bog", "starter_plain" },
            ["boss_cleared_level_ids"] = new List<object?> { "starter_plain" },
            ["level_clear_count_by_id"] = new Dictionary<string, object?>
            {
                ["misty_bog"] = 3,
                ["starter_plain"] = 1
            },
            ["pity_counter_by_key"] = new Dictionary<string, object?>
            {
                ["slime_pity"] = 2,
                ["bog_pity"] = 4
            },
            ["daily_roll_count_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = 5
            },
            ["daily_roll_day_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = 12L
            },
            ["hourly_roll_count_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = 2
            },
            ["hourly_roll_hour_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = 48L
            }
        };

        AssertDictionaryEqual(expected, restored);
    }

    [Fact]
    public void RuntimePayloadFallsBackToKnownDefaultsForMalformedValues()
    {
        var payload = new Dictionary<string, object?>
        {
            ["active_level_id"] = "unknown_level",
            ["active_wave_index"] = 99,
            ["unlocked_level_ids"] = new List<object?> { string.Empty, "unknown_level" },
            ["boss_cleared_level_ids"] = new List<object?> { "misty_bog", "unknown_level" },
            ["level_clear_count_by_id"] = new Dictionary<string, object?>
            {
                ["starter_plain"] = -2,
                [string.Empty] = 8
            },
            ["pity_counter_by_key"] = new Dictionary<string, object?>
            {
                ["slime_pity"] = -1
            },
            ["daily_roll_count_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = -4
            },
            ["daily_roll_day_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = -3L
            },
            ["hourly_roll_count_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = -9
            },
            ["hourly_roll_hour_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = -11L
            }
        };

        var normalized = LevelRuntimeSerializationContracts.NormalizeRaw(payload, "starter_plain", ["starter_plain", "misty_bog"], 0);
        var restored = LevelRuntimeSerializationContracts.ToRawDictionary(normalized);

        var expected = new Dictionary<string, object?>
        {
            ["active_level_id"] = "starter_plain",
            ["active_wave_index"] = 0,
            ["unlocked_level_ids"] = new List<object?> { "starter_plain" },
            ["boss_cleared_level_ids"] = new List<object?> { "misty_bog" },
            ["level_clear_count_by_id"] = new Dictionary<string, object?>
            {
                ["starter_plain"] = 0
            },
            ["pity_counter_by_key"] = new Dictionary<string, object?>
            {
                ["slime_pity"] = 0
            },
            ["daily_roll_count_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = 0
            },
            ["daily_roll_day_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = 0L
            },
            ["hourly_roll_count_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = 0
            },
            ["hourly_roll_hour_by_table"] = new Dictionary<string, object?>
            {
                ["starter_plain_green_slime"] = 0L
            }
        };

        AssertDictionaryEqual(expected, restored);
    }

    private static void AssertDictionaryEqual(IReadOnlyDictionary<string, object?> expected, IReadOnlyDictionary<string, object?> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        foreach ((string key, object? expectedValue) in expected)
        {
            Assert.True(actual.ContainsKey(key), $"Missing key '{key}'.");
            AssertValueEqual(expectedValue, actual[key]);
        }
    }

    private static void AssertValueEqual(object? expected, object? actual)
    {
        Assert.Equal(expected?.GetType(), actual?.GetType());

        switch (expected)
        {
            case IReadOnlyDictionary<string, object?> expectedDictionary:
                AssertDictionaryEqual(expectedDictionary, Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(actual));
                break;
            case double expectedDouble:
                Assert.Equal(expectedDouble, Assert.IsType<double>(actual), 6);
                break;
            case float expectedFloat:
                Assert.Equal(expectedFloat, Assert.IsType<float>(actual), 6);
                break;
            case IEnumerable expectedEnumerable when expected is not string:
                AssertEnumerableEqual(expectedEnumerable, Assert.IsAssignableFrom<IEnumerable>(actual));
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

            AssertValueEqual(expectedEnumerator.Current, actualEnumerator.Current);
        }
    }
}
