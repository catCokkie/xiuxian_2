using Xiuxian.Scripts.Core;
using Xiuxian.Scripts.Services;
using Xunit;

namespace Xiuxian2.Tests;

public sealed class PlayerBreakthroughRuleTests
{
    [Fact]
    public void TryBreakthrough_ReturnsFalse_WhenRealmExpIsBelowRequiredThreshold()
    {
        double required = PlayerBreakthroughRule.GetExpRequired(1);

        PlayerBreakthroughResult result = PlayerBreakthroughRule.TryBreakthrough(1, required - 1.0);

        Assert.False(result.Succeeded);
        Assert.Equal(1, result.NextRealmLevel);
        Assert.Equal(required - 1.0, result.RemainingRealmExp, 6);
        Assert.Equal(required, result.NextRealmExpRequired, 6);
    }

    [Fact]
    public void TryBreakthrough_ReturnsTrue_WhenRealmExpExactlyMatchesRequiredThreshold()
    {
        double required = PlayerBreakthroughRule.GetExpRequired(1);

        PlayerBreakthroughResult result = PlayerBreakthroughRule.TryBreakthrough(1, required);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.NextRealmLevel);
        Assert.Equal(0.0, result.RemainingRealmExp, 6);
    }

    [Fact]
    public void TryBreakthrough_ConsumesRequiredExp_AndLeavesOverflowAfterLevelUp()
    {
        double required = PlayerBreakthroughRule.GetExpRequired(1);
        double overflow = 42.5;

        PlayerBreakthroughResult result = PlayerBreakthroughRule.TryBreakthrough(1, required + overflow);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.NextRealmLevel);
        Assert.Equal(overflow, result.RemainingRealmExp, 6);
        Assert.Equal(PlayerProgressState.GetExpRequired(2), result.NextRealmExpRequired, 6);
    }
}
