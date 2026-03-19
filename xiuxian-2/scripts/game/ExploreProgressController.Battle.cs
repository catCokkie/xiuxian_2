using Godot;
using System.Collections.Generic;
using System.Text;

namespace Xiuxian.Scripts.Game
{
    public partial class ExploreProgressController
    {
        private void TryStartBattle()
        {
            int candidate = FindFrontMonsterIndex();
            if (candidate < 0)
            {
                return;
            }

            if (_monsterMarkers[candidate].Position.X > BattleTriggerX)
            {
                return;
            }

            _inBattle = true;
            _battleMonsterIndex = candidate;
            _battleRoundCounter = 0;
            _pendingBattleInputEvents = 0;
            if (candidate >= 0 && candidate < _monsterMarkerIds.Count)
            {
                _battleMonsterId = _monsterMarkerIds[candidate];
            }
            ConfigureBattleMonster();
            _battleInfoLabel.Text = UiText.Encounter(_battleMonsterName);
            _battleInfoLabel.Visible = true;
            _roundInfoLabel.Text = UiText.BattleRound(0, _battleMonsterName, _enemyMaxHp);
            UpdateHpLabels();
            RefreshActorSlots();
            RefreshMoveDebugLabel();
        }

        private void AdvanceBattleByInput(int inputEvents)
        {
            int threshold = Mathf.Max(1, _inputsPerBattleRoundRuntime);
            _pendingBattleInputEvents += inputEvents;
            int rounds = _pendingBattleInputEvents / threshold;
            if (rounds <= 0)
            {
                _battleInfoLabel.Text = UiText.BattleInProgress(_battleMonsterName);
                _battleInfoLabel.Visible = true;
                _roundInfoLabel.Text = $"蓄力 {_pendingBattleInputEvents}/{threshold} | {UiText.BattleRound(_battleRoundCounter, _battleMonsterName, _enemyHp)}";
                UpdateHpLabels();
                RefreshActorSlots();
                RefreshMoveDebugLabel();
                RefreshDebugPanel();
                return;
            }

            _pendingBattleInputEvents -= rounds * threshold;
            for (int i = 0; i < rounds; i++)
            {
                _battleRoundCounter++;
                _enemyHp -= _playerAttackPerRoundRuntime;
                int damageToPlayer = Mathf.Max(_enemyMinDamageRuntime, _enemyAttackPower / Mathf.Max(1, _enemyDamageDividerRuntime));
                _playerHp = Mathf.Max(0, _playerHp - damageToPlayer);

                if (_playerHp <= 0)
                {
                    HandleBattleDefeat();
                    return;
                }

                if (_enemyHp <= 0)
                {
                    CompleteBattle();
                    return;
                }
            }

            _battleInfoLabel.Text = UiText.BattleInProgress(_battleMonsterName);
            _battleInfoLabel.Visible = true;
            _roundInfoLabel.Text = $"{UiText.BattleRound(_battleRoundCounter, _battleMonsterName, _enemyHp)} | next {_pendingBattleInputEvents}/{threshold}";
            UpdateHpLabels();
            RefreshActorSlots();
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
        }

        private void HandleBattleDefeat()
        {
            string defeatedMonsterId = _battleMonsterId;
            string defeatedMonsterName = _battleMonsterName;
            _inBattle = false;
            _battleMonsterIndex = -1;
            _battleMonsterId = "";
            _battleRoundCounter = 0;
            _pendingBattleInputEvents = 0;
            _exploreProgress = 0.0f;
            _progressBar.Value = 0.0f;

            string levelId = _levelConfigLoader?.ActiveLevelId ?? "";
            if (_levelConfigLoader != null && !string.IsNullOrEmpty(levelId))
            {
                _levelConfigLoader.TrySetActiveLevel(levelId);
            }

            ApplyLevelConfig();
            _zoneLabel.Text = _currentZone;
            ResetTrackVisual();
            _battleInfoLabel.Text = "战败，当前副本已重置";
            _battleInfoLabel.Visible = true;
            _roundInfoLabel.Text = UiText.WaitingInput;
            RefreshLevelOptionButton();
            UpdateHpLabels();
            RefreshActorSlots();
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
            AddRecentBattleLog("defeat", defeatedMonsterId, defeatedMonsterName, 0.0, 0.0, new Dictionary<string, int>());
        }

        private void CompleteBattle()
        {
            _inBattle = false;
            _roundInfoLabel.Text = UiText.BattleRound(_battleRoundCounter, _battleMonsterName, 0);
            _battleInfoLabel.Text = UiText.BattleVictory(_battleMonsterName);
            _battleInfoLabel.Visible = true;

            string activeLevelId = _levelConfigLoader?.ActiveLevelId ?? "";
            if (_levelConfigLoader != null &&
                _levelConfigLoader.TryMarkBossDefeatedAndUnlockNext(activeLevelId, _battleMonsterId, out string unlockedLevelId) &&
                !string.IsNullOrEmpty(unlockedLevelId))
            {
                _battleInfoLabel.Text = $"{UiText.BattleVictory(_battleMonsterName)} | 已解锁 {unlockedLevelId}";
            }

            ApplyBattleRewards(out double rewardLingqi, out double rewardInsight, out Dictionary<string, int> rewardItems);
            AddRecentBattleLog("victory", _battleMonsterId, _battleMonsterName, rewardLingqi, rewardInsight, rewardItems);

            if (_battleMonsterIndex >= 0 && _battleMonsterIndex < _monsterMarkers.Count)
            {
                Label defeated = _monsterMarkers[_battleMonsterIndex];
                defeated.Modulate = new Color(1, 1, 1, 0.45f);
                defeated.Position = new Vector2(GetRightMostMonsterX() + MonsterRespawnSpacing, defeated.Position.Y);
                defeated.Modulate = Colors.White;
                AssignMonsterToMarker(_battleMonsterIndex);
            }

            _battleMonsterIndex = -1;
            _battleMonsterId = "";
            _pendingBattleInputEvents = 0;
            _enemyHpLabel.Visible = false;
            UpdateHpLabels();
            RefreshActorSlots();
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
        }

        private void ConfigureBattleMonster()
        {
            _battleMonsterName = UiText.DefaultMonsterName;
            _enemyMaxHp = 24;
            _enemyAttackPower = 4;
            _inputsPerBattleRoundRuntime = InputsPerBattleRound;

            if (_levelConfigLoader != null && !string.IsNullOrEmpty(_battleMonsterId))
            {
                if (_levelConfigLoader.TryGetMonsterCombatParams(
                    _battleMonsterId,
                    out string monsterName,
                    out int hp,
                    out int inputsPerRound,
                    out int attack))
                {
                    _battleMonsterName = monsterName;
                    _enemyMaxHp = hp;
                    _inputsPerBattleRoundRuntime = inputsPerRound;
                    _enemyAttackPower = attack;
                }
            }

            _enemyHp = _enemyMaxHp;
        }

        private void ApplyBattleRewards(out double totalLingqi, out double totalInsight, out Dictionary<string, int> totalItems)
        {
            totalLingqi = 0.0;
            totalInsight = 0.0;
            totalItems = new Dictionary<string, int>();
            bool appliedFromConfig = false;
            double lingqi = 0.0;
            double insight = 0.0;

            if (_levelConfigLoader != null && !string.IsNullOrEmpty(_battleMonsterId))
            {
                var drops = _levelConfigLoader.RollMonsterDrops(_battleMonsterId);
                MergeItemRewards(totalItems, drops);
                ApplyResourceAndItemRewards(0.0, 0.0, drops, "battle_drop");

                if (_levelConfigLoader.TryRollMonsterSettlementReward(_battleMonsterId, out lingqi, out insight))
                {
                    totalLingqi += lingqi;
                    totalInsight += insight;
                    ApplyResourceAndItemRewards(lingqi, insight, new Dictionary<string, int>(), "battle_settle");
                }

                appliedFromConfig = drops.Count > 0 || lingqi > 0 || insight > 0;
            }

            if (!appliedFromConfig)
            {
                var fallbackItems = new Dictionary<string, int>
                {
                    ["spirit_herb"] = 1,
                    ["lingqi_shard"] = 3
                };
                MergeItemRewards(totalItems, fallbackItems);
                ApplyResourceAndItemRewards(0.0, 0.0, fallbackItems, "battle_fallback");
            }
        }

        private static void MergeItemRewards(Dictionary<string, int> target, Dictionary<string, int> source)
        {
            foreach (var kv in source)
            {
                if (kv.Value <= 0)
                {
                    continue;
                }

                int current = target.TryGetValue(kv.Key, out int saved) ? saved : 0;
                target[kv.Key] = current + kv.Value;
            }
        }

        private void ApplyLevelCompletionRewards()
        {
            if (_levelConfigLoader == null)
            {
                return;
            }

            if (_levelConfigLoader.TryBuildLevelCompletionReward(
                out string levelId,
                out bool firstClear,
                out double lingqi,
                out double insight,
                out Dictionary<string, int> items))
            {
                ApplyResourceAndItemRewards(lingqi, insight, items, firstClear ? $"level_first_clear:{levelId}" : $"level_repeat_clear:{levelId}");
            }
        }

        private static string BuildDropSummary(Dictionary<string, int> drops)
        {
            if (drops.Count == 0)
            {
                return "none";
            }

            var sb = new StringBuilder();
            bool first = true;
            foreach (var kv in drops)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                first = false;
                sb.Append($"{kv.Key} x{kv.Value}");
            }
            return sb.ToString();
        }

        private void ApplyResourceAndItemRewards(double lingqi, double insight, Dictionary<string, int> items, string source)
        {
            if (lingqi > 0.0)
            {
                _resourceWalletState?.AddLingqi(lingqi);
            }
            if (insight > 0.0)
            {
                _resourceWalletState?.AddInsight(insight);
            }

            foreach (var kv in items)
            {
                _backpackState?.AddItem(kv.Key, kv.Value);
            }

            string itemPart = items.Count > 0 ? BuildDropSummary(items) : "none";
            _lastDropSummary = $"{source} | lq={lingqi:0} in={insight:0} | items={itemPart}";
            RefreshDebugPanel();
        }
    }
}
