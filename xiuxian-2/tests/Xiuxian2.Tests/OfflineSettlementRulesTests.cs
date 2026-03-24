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

        Assert.True(inputsB < inputsA);
    }

    [Fact]
    public void EvaluateOfflineSeconds_MarksNegativeDeltaAsInvalid()
    {
        OfflineSettlementRules.OfflineTimeEvaluation evaluation = OfflineSettlementRules.EvaluateOfflineSeconds(-10);

        Assert.Equal(OfflineSettlementRules.OfflineTimeGuardMode.Invalid, evaluation.GuardMode);
        Assert.Equal(0.0, evaluation.EffectiveOfflineSeconds);
    }

    [Fact]
    public void EvaluateOfflineSeconds_ReducesSuspiciouslyLargeDelta()
    {
        OfflineSettlementRules.OfflineTimeEvaluation normal = OfflineSettlementRules.EvaluateOfflineSeconds(24 * 60 * 60);
        OfflineSettlementRules.OfflineTimeEvaluation guarded = OfflineSettlementRules.EvaluateOfflineSeconds(48 * 60 * 60);

        Assert.Equal(OfflineSettlementRules.OfflineTimeGuardMode.Normal, normal.GuardMode);
        Assert.Equal(OfflineSettlementRules.OfflineTimeGuardMode.Guarded, guarded.GuardMode);
        Assert.True(guarded.EffectiveOfflineSeconds < normal.EffectiveOfflineSeconds);
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
