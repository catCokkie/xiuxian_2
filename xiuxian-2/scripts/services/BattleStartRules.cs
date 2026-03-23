using System;

namespace Xiuxian.Scripts.Services
{
    public static class BattleStartRules
    {
        public static BattleEncounterDecision DetermineEncounterStart(
            int candidateIndex,
            float candidateX,
            float battleTriggerX,
            string monsterId)
        {
            return BattleLifecycleRules.DetermineEncounterStart(candidateIndex, candidateX, battleTriggerX, monsterId);
        }

        public static BattleStartSetup BuildStartSetup(string monsterId, MonsterStatProfile? profile, int defaultInputsPerRound)
        {
            if (profile.HasValue)
            {
                MonsterStatProfile value = profile.Value;
                return new BattleStartSetup(
                    string.IsNullOrEmpty(monsterId) ? value.MonsterId : monsterId,
                    value.DisplayName,
                    value.BaseStats.MaxHp,
                    value.BaseStats.Attack,
                    Math.Max(1, value.InputsPerRound),
                    0,
                    0);
            }

            return new BattleStartSetup(
                monsterId,
                UiText.DefaultMonsterName,
                24,
                4,
                Math.Max(1, defaultInputsPerRound),
                0,
                0);
        }
    }
}
