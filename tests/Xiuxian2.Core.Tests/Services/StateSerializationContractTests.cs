using System.Collections;
using System.Collections.Generic;
using Xiuxian.Scripts.Services;
using Xiuxian2.Core.Tests.Support.Serialization;
using Xunit;

namespace Xiuxian2.Core.Tests.Services;

public sealed class StateSerializationContractTests
{
    [Fact]
    public void BackpackRoundTripPreservesInventoryCounts()
    {
        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreateBackpackRoundTripPayload(),
            StateSerializationContracts.NormalizeBackpackRaw(StateSerializationFixtureBuilder.CreateBackpackRoundTripPayload()));
    }

    [Fact]
    public void ResourceWalletRoundTripPreservesTotals()
    {
        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreateResourceWalletRoundTripPayload(),
            StateSerializationContracts.NormalizeResourceWalletRaw(StateSerializationFixtureBuilder.CreateResourceWalletRoundTripPayload()));
    }

    [Fact]
    public void PlayerProgressRoundTripPreservesRealmState()
    {
        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreatePlayerProgressRoundTripPayload(),
            StateSerializationContracts.NormalizePlayerProgressRaw(StateSerializationFixtureBuilder.CreatePlayerProgressRoundTripPayload()));
    }

    [Fact]
    public void InputActivityRoundTripPreservesAccumulatedTotals()
    {
        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreateInputActivityRoundTripPayload(),
            StateSerializationContracts.NormalizeInputActivityRaw(StateSerializationFixtureBuilder.CreateInputActivityRoundTripPayload()));
    }

    [Fact]
    public void PlayerActionRoundTripPreservesMode()
    {
        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreatePlayerActionRoundTripPayload(),
            StateSerializationContracts.NormalizePlayerActionRaw(StateSerializationFixtureBuilder.CreatePlayerActionRoundTripPayload()));
    }

    [Fact]
    public void BackpackMalformedPayloadFallsBackToSafeCounts()
    {
        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreateBackpackMalformedExpectation(),
            StateSerializationContracts.NormalizeBackpackRaw(StateSerializationFixtureBuilder.CreateBackpackMalformedPayload()));
    }

    [Fact]
    public void ResourceWalletMalformedPayloadFallsBackToSafeTotals()
    {
        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreateResourceWalletMalformedExpectation(),
            StateSerializationContracts.NormalizeResourceWalletRaw(StateSerializationFixtureBuilder.CreateResourceWalletMalformedPayload()));
    }

    [Fact]
    public void PlayerProgressMalformedPayloadClampsToCurrentRules()
    {
        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreatePlayerProgressMalformedExpectation(),
            StateSerializationContracts.NormalizePlayerProgressRaw(StateSerializationFixtureBuilder.CreatePlayerProgressMalformedPayload()));
    }

    [Fact]
    public void InputActivityMalformedPayloadClearsNegativeTotals()
    {
        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreateInputActivityMalformedExpectation(),
            StateSerializationContracts.NormalizeInputActivityRaw(StateSerializationFixtureBuilder.CreateInputActivityMalformedPayload()));
    }

    [Fact]
    public void PlayerActionMalformedPayloadNormalizesToDungeonMode()
    {
        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreatePlayerActionMalformedExpectation(),
            StateSerializationContracts.NormalizePlayerActionRaw(StateSerializationFixtureBuilder.CreatePlayerActionMalformedPayload()));
    }

    private static void AssertDictionaryEqual(IReadOnlyDictionary<string, object?> expected, IReadOnlyDictionary<string, object?> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        foreach ((string key, object? expectedValue) in expected)
        {
            Assert.True(actual.ContainsKey(key), $"Missing key '{key}' in restored payload.");
            AssertValueEqual(expectedValue, actual[key]);
        }
    }

    private static void AssertValueEqual(object? expected, object? actual)
    {
        Assert.Equal(expected?.GetType(), actual?.GetType());

        switch (expected)
        {
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
