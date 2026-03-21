using Godot;
using Xiuxian.Scripts.Services;
using Xiuxian2.Core.Tests.Support.Serialization;
using Xunit;

namespace Xiuxian2.Core.Tests.Services;

public sealed class StateSerializationContractTests
{
    [Fact]
    public void SerializableServicesRoundTripRequiredProgressionValues()
    {
        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreateBackpackRoundTripPayload(),
            StateSerializationContracts.NormalizeBackpack(StateSerializationFixtureBuilder.CreateBackpackRoundTripPayload()));

        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreateResourceWalletRoundTripPayload(),
            StateSerializationContracts.NormalizeResourceWallet(StateSerializationFixtureBuilder.CreateResourceWalletRoundTripPayload()));

        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreatePlayerProgressRoundTripPayload(),
            StateSerializationContracts.NormalizePlayerProgress(StateSerializationFixtureBuilder.CreatePlayerProgressRoundTripPayload()));

        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreateInputActivityRoundTripPayload(),
            StateSerializationContracts.NormalizeInputActivity(StateSerializationFixtureBuilder.CreateInputActivityRoundTripPayload()));

        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreatePlayerActionRoundTripPayload(),
            StateSerializationContracts.NormalizePlayerAction(StateSerializationFixtureBuilder.CreatePlayerActionRoundTripPayload()));
    }

    [Fact]
    public void SerializableServicesNormalizeMalformedPayloadsToSafeDefaults()
    {
        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreateBackpackMalformedExpectation(),
            StateSerializationContracts.NormalizeBackpack(StateSerializationFixtureBuilder.CreateBackpackMalformedPayload()));

        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreateResourceWalletMalformedExpectation(),
            StateSerializationContracts.NormalizeResourceWallet(StateSerializationFixtureBuilder.CreateResourceWalletMalformedPayload()));

        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreatePlayerProgressMalformedExpectation(),
            StateSerializationContracts.NormalizePlayerProgress(StateSerializationFixtureBuilder.CreatePlayerProgressMalformedPayload()));

        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreateInputActivityMalformedExpectation(),
            StateSerializationContracts.NormalizeInputActivity(StateSerializationFixtureBuilder.CreateInputActivityMalformedPayload()));

        AssertDictionaryEqual(
            StateSerializationFixtureBuilder.CreatePlayerActionMalformedExpectation(),
            StateSerializationContracts.NormalizePlayerAction(StateSerializationFixtureBuilder.CreatePlayerActionMalformedPayload()));
    }

    private static void AssertDictionaryEqual(Godot.Collections.Dictionary<string, Variant> expected, Godot.Collections.Dictionary<string, Variant> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        foreach (string key in expected.Keys)
        {
            Assert.True(actual.ContainsKey(key), $"Missing key '{key}' in restored payload.");
            Assert.Equal(expected[key].ToString(), actual[key].ToString());
        }
    }
}
