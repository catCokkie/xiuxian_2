using System;

namespace Xiuxian.Scripts.Services
{
    public static class BattleRules
    {
        public static BattleInputProgress ConsumeBattleInputs(int pendingInputs, int inputEvents, int threshold)
        {
            int normalizedThreshold = Math.Max(1, threshold);
            int totalPending = Math.Max(0, pendingInputs) + Math.Max(0, inputEvents);
            int roundsToResolve = totalPending / normalizedThreshold;
            int remainingInputs = totalPending - roundsToResolve * normalizedThreshold;
            return new BattleInputProgress(normalizedThreshold, totalPending, roundsToResolve, remainingInputs);
        }

        public static int CalculateAttackDamage(int attack, int defense, int minimumDamage = 1)
        {
            return Math.Max(minimumDamage, attack - defense);
        }

        public static int CalculateScaledDamage(int attack, int divisor, int minimumDamage = 1)
        {
            return Math.Max(minimumDamage, attack / Math.Max(1, divisor));
        }

        public static BattleRoundResult ResolvePlayerVsMonsterRound(
            CharacterBattleSnapshot player,
            CharacterBattleSnapshot monster,
            int enemyDamageDivider,
            int enemyMinimumDamage)
        {
            int damageToMonster = CalculateAttackDamage(player.Attack, monster.Defense, 1);
            int nextMonsterHp = Math.Max(0, monster.CurrentHp - damageToMonster);

            int damageToPlayer = CalculateScaledDamage(monster.Attack, enemyDamageDivider, enemyMinimumDamage);
            int nextPlayerHp = Math.Max(0, player.CurrentHp - damageToPlayer);

            CharacterBattleSnapshot nextPlayer = player with { CurrentHp = nextPlayerHp };
            CharacterBattleSnapshot nextMonster = monster with { CurrentHp = nextMonsterHp };

            BattleOutcome outcome = BattleOutcome.Ongoing;
            if (nextPlayerHp <= 0 && nextMonsterHp <= 0)
            {
                outcome = BattleOutcome.DoubleKnockout;
            }
            else if (nextMonsterHp <= 0)
            {
                outcome = BattleOutcome.PlayerWon;
            }
            else if (nextPlayerHp <= 0)
            {
                outcome = BattleOutcome.MonsterWon;
            }

            return new BattleRoundResult(nextPlayer, nextMonster, damageToMonster, damageToPlayer, outcome);
        }

        public static BattleFlowDecision DetermineBattleFlow(BattleOutcome outcome)
        {
            return outcome switch
            {
                BattleOutcome.PlayerWon => new BattleFlowDecision(BattleFlowAction.Victory, true),
                BattleOutcome.MonsterWon => new BattleFlowDecision(BattleFlowAction.Defeat, true),
                BattleOutcome.DoubleKnockout => new BattleFlowDecision(BattleFlowAction.DoubleKnockout, true),
                _ => new BattleFlowDecision(BattleFlowAction.Continue, false)
            };
        }
    }
}
