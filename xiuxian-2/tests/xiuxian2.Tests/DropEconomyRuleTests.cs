using Xiuxian.Scripts.Core;
using Xunit;

namespace Xiuxian2.Tests;

public sealed class DropEconomyRuleTests
{
    [Fact]
    public void ApplyPity_TriggersGuaranteedDropAtThreshold()
    {
        Dictionary<string, int> drops = new();
        Dictionary<string, int> pityCounters = new()
        {
            ["boss_core"] = 2
        };

        PityApplyResult result = DropEconomyRule.ApplyPity(drops, pityCounters, "boss_core", 3, "rare_core", 1);

        Assert.True(result.Triggered);
        Assert.Equal(0, result.CounterValue);
        Assert.Equal(1, drops["rare_core"]);
        Assert.Equal(0, pityCounters["boss_core"]);
    }

    [Fact]
    public void CanConsumeDailyRoll_BlocksWhenDailyCapReached()
    {
        bool canConsume = DropEconomyRule.CanConsumeDailyRoll(dailyCap: 5, dailyCount: 5);

        Assert.False(canConsume);
    }
}
