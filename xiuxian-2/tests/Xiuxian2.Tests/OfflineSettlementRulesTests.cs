using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class OfflineSettlementRulesTests
{
    [Fact]
    public void CalculateOfflineInputBudget_UsesSegmentedInputRates()
    {
        double inputs = OfflineSettlementRules.CalculateOfflineInputBudget(2 * 60 * 60);

        Assert.Equal(1080, inputs, 6);
    }

    [Fact]
    public void CalculateOfflineInputBudget_CapsAtTwentyFourHours()
    {
        double inputsA = OfflineSettlementRules.CalculateOfflineInputBudget(24 * 60 * 60);
        double inputsB = OfflineSettlementRules.CalculateOfflineInputBudget(48 * 60 * 60);

        Assert.Equal(inputsA, inputsB, 6);
    }

    [Fact]
    public void BuildCultivationOfflineSettlement_BuildsRewardResult()
    {
        ActionSettlementResult result = OfflineSettlementRules.BuildCultivationOfflineSettlement(
            offlineSeconds: 60 * 60,
            apPerInput: 1.0,
            lingqiFactor: 0.9,
            insightFactor: 0.08,
            petAffinityFactor: 0.03,
            realmExpFromLingqiRate: 0.25,
            moodMultiplier: 1.0,
            realmMultiplier: 1.0,
            inputExpActive: false);

        Assert.Equal(PlayerActionState.ActionCultivation, result.ActionId);
        Assert.True(result.ApConsumed > 0.0);
        Assert.True(result.LingqiGain > 0.0);
        Assert.True(result.InsightGain > 0.0);
        Assert.True(result.PetAffinityGain > 0.0);
        Assert.True(result.RealmExpGain > 0.0);
    }
}
