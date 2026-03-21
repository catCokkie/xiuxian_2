using Xiuxian.Scripts.Core;
using Xunit;

namespace Xiuxian2.Tests;

public sealed class BattleRoundRuleTests
{
    [Fact]
    public void Advance_ConsumesInputs_AndEndsBattleInVictory()
    {
        BattleRoundResolution result = BattleRoundRule.Advance(
            pendingInputEvents: 0,
            inputEvents: 6,
            inputsPerBattleRound: 3,
            battleRoundCounter: 0,
            playerHp: 36,
            enemyHp: 8,
            playerAttackPerRound: 4,
            enemyAttackPower: 4,
            enemyDamageDivider: 4,
            enemyMinDamageRuntime: 1);

        Assert.True(result.Victory);
        Assert.False(result.Defeated);
        Assert.Equal(2, result.BattleRoundCounter);
        Assert.Equal(0, result.PendingInputEvents);
        Assert.Equal(0, result.EnemyHp);
        Assert.Equal(34, result.PlayerHp);
    }
}
