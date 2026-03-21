using Xiuxian.Scripts.Core;
using Xunit;

namespace Xiuxian2.Tests;

public sealed class LevelCycleRuleTests
{
    [Fact]
    public void GetNextUnlockedLevelId_WrapsAroundAfterLastUnlockedLevel()
    {
        string next = LevelCycleRule.GetNextUnlockedLevelId(new[] { "level_001", "level_002", "level_003" }, "level_003");

        Assert.Equal("level_001", next);
    }
}
