using Xiuxian.Scripts.Core;
using Xunit;

namespace Xiuxian2.Tests;

public sealed class ActivitySettlementRuleTests
{
    [Fact]
    public void CalculateInputRealmExpGain_OnlyAppliesInCultivationMode()
    {
        double cultivationGain = ActivitySettlementRule.CalculateInputRealmExpGain(
            cultivationInputExpEnabled: true,
            isCultivationMode: true,
            inputEvents: 12,
            cultivationExpPerInput: 0.35);

        double dungeonGain = ActivitySettlementRule.CalculateInputRealmExpGain(
            cultivationInputExpEnabled: true,
            isCultivationMode: false,
            inputEvents: 12,
            cultivationExpPerInput: 0.35);

        Assert.Equal(4.2, cultivationGain, 6);
        Assert.Equal(0.0, dungeonGain, 6);
    }

    [Fact]
    public void CalculateSettlement_GrantsRealmExpFromLingqiOutsideCultivationMode()
    {
        ActivitySettlementResult result = ActivitySettlementRule.CalculateSettlement(
            apFinalBucket: 20.0,
            lingqiFactor: 0.9,
            insightFactor: 0.08,
            petAffinityFactor: 0.03,
            realmExpFromLingqiRate: 0.25,
            moodMultiplier: 1.1,
            realmMultiplier: 1.12,
            cultivationInputExpEnabled: true,
            isCultivationMode: false);

        Assert.False(result.InputExpActive);
        Assert.Equal(22.176, result.LingqiGain, 6);
        Assert.Equal(1.6, result.InsightGain, 6);
        Assert.Equal(0.6, result.PetAffinityGain, 6);
        Assert.Equal(5.544, result.RealmExpGain, 6);
    }

    [Fact]
    public void CalculateSettlement_SuppressesRealmExpFromLingqiDuringCultivationInputMode()
    {
        ActivitySettlementResult result = ActivitySettlementRule.CalculateSettlement(
            apFinalBucket: 20.0,
            lingqiFactor: 0.9,
            insightFactor: 0.08,
            petAffinityFactor: 0.03,
            realmExpFromLingqiRate: 0.25,
            moodMultiplier: 1.0,
            realmMultiplier: 1.0,
            cultivationInputExpEnabled: true,
            isCultivationMode: true);

        Assert.True(result.InputExpActive);
        Assert.Equal(18.0, result.LingqiGain, 6);
        Assert.Equal(0.0, result.RealmExpGain, 6);
    }
}
