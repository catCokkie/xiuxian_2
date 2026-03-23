using Xiuxian.Scripts.Game;

namespace Xiuxian.Tests;

public sealed class ExploreProgressRulesTests
{
    [Fact]
    public void AdvanceProgress_UsesInputEventsForProgression()
    {
        (float nextProgress, bool completedLevel) = ExploreProgressRules.AdvanceProgress(12.0f, 5, 0.5f, 100.0f);

        Assert.Equal(14.5f, nextProgress);
        Assert.False(completedLevel);
    }

    [Fact]
    public void AdvanceProgress_ResetsWhenReachingMaxProgress()
    {
        (float nextProgress, bool completedLevel) = ExploreProgressRules.AdvanceProgress(98.0f, 1, 2.0f, 100.0f);

        Assert.Equal(0.0f, nextProgress);
        Assert.True(completedLevel);
    }
}
