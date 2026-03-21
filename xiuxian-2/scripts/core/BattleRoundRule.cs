using System;

namespace Xiuxian.Scripts.Core
{
    public readonly record struct BattleRoundResolution(
        int PendingInputEvents,
        int BattleRoundCounter,
        int PlayerHp,
        int EnemyHp,
        bool Defeated,
        bool Victory);

    public static class BattleRoundRule
    {
        public static BattleRoundResolution Advance(
            int pendingInputEvents,
            int inputEvents,
            int inputsPerBattleRound,
            int battleRoundCounter,
            int playerHp,
            int enemyHp,
            int playerAttackPerRound,
            int enemyAttackPower,
            int enemyDamageDivider,
            int enemyMinDamageRuntime)
        {
            int threshold = Math.Max(1, inputsPerBattleRound);
            int nextPending = pendingInputEvents + inputEvents;
            int rounds = nextPending / threshold;
            if (rounds <= 0)
            {
                return new BattleRoundResolution(nextPending, battleRoundCounter, playerHp, enemyHp, false, false);
            }

            nextPending -= rounds * threshold;
            int nextRoundCounter = battleRoundCounter;
            int nextPlayerHp = playerHp;
            int nextEnemyHp = enemyHp;
            int damageToPlayer = Math.Max(enemyMinDamageRuntime, enemyAttackPower / Math.Max(1, enemyDamageDivider));

            for (int i = 0; i < rounds; i++)
            {
                nextRoundCounter++;
                nextEnemyHp -= playerAttackPerRound;
                nextPlayerHp = Math.Max(0, nextPlayerHp - damageToPlayer);

                if (nextPlayerHp <= 0)
                {
                    return new BattleRoundResolution(nextPending, nextRoundCounter, nextPlayerHp, nextEnemyHp, true, false);
                }

                if (nextEnemyHp <= 0)
                {
                    return new BattleRoundResolution(nextPending, nextRoundCounter, nextPlayerHp, nextEnemyHp, false, true);
                }
            }

            return new BattleRoundResolution(nextPending, nextRoundCounter, nextPlayerHp, nextEnemyHp, false, false);
        }
    }
}
