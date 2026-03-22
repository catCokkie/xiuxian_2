using Xiuxian.Scripts.Core;
using Xunit;

namespace Xiuxian2.Tests;

public sealed class ExploreProgressionRuleTests
{
    [Fact]
    public void Advance_ReachesMaxProgress_AndSignalsLevelCompletion()
    {
        ExploreProgressAdvanceResult result = ExploreProgressionRule.Advance(96.0f, 5, 1.0f, 100.0f);

        Assert.True(result.Completed);
        Assert.Equal(100.0f, result.RawProgress);
        Assert.Equal(0.0f, result.NextProgress);
    }
}
