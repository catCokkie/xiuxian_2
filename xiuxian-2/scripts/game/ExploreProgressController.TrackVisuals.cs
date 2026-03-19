using Godot;
using System;
using System.Text;

namespace Xiuxian.Scripts.Game
{
    public partial class ExploreProgressController
    {
        private void CacheMonsterMarkers()
        {
            _monsterMarkers.Clear();
            _monsterMarkerIds.Clear();
            _monsterMoveInputPending.Clear();
            _monsterMoveInputThreshold.Clear();
            for (int i = 1; i <= 8; i++)
            {
                NodePath markerPath = $"{PlayerMarkerPath.GetConcatenatedNames().Replace("PlayerMarker", $"MonsterMarker{i:00}")}";
                Label marker = GetNodeOrNull<Label>(markerPath);
                if (marker != null)
                {
                    _monsterMarkers.Add(marker);
                    _monsterMarkerIds.Add(string.Empty);
                    _monsterMoveInputPending.Add(0);
                    _monsterMoveInputThreshold.Add(Mathf.Max(1, InputsPerMoveFrame));
                }
            }
        }

        private void CacheMonsterSlots()
        {
            _monsterSlots.Clear();
            for (int i = 1; i <= 8; i++)
            {
                NodePath slotPath = $"{PlayerMarkerPath.GetConcatenatedNames().Replace("PlayerMarker", $"MonsterSlot{i:00}")}";
                TextureRect slot = GetNodeOrNull<TextureRect>(slotPath);
                if (slot != null)
                {
                    _monsterSlots.Add(slot);
                }
            }
        }

        private void RefreshMoveDebugLabel()
        {
            if (_moveDebugLabel == null)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.Append(BuildMoveDebugStatus());
            sb.Append('\n');
            sb.Append(BuildBattleDebugStatus());
            sb.Append('\n');
            sb.Append(BuildInputSourceDebugStatus());
            sb.Append('\n');
            sb.Append(BuildWaveDebugStatus());
            _moveDebugLabel.Text = sb.ToString();
        }

        private void ApplyGlobalDebugOverlayVisibility()
        {
            if (_moveDebugLabel != null)
            {
                _moveDebugLabel.Visible = _globalDebugOverlayEnabled;
            }
        }

        private float GetRightMostMonsterX()
        {
            float maxX = 0.0f;
            foreach (Label monster in _monsterMarkers)
            {
                maxX = Mathf.Max(maxX, monster.Position.X);
            }
            return maxX;
        }

        private string BuildMoveDebugStatus()
        {
            int idx = FindFrontMonsterIndex();
            if (idx < 0 || idx >= _monsterMoveInputThreshold.Count || idx >= _monsterMoveInputPending.Count)
            {
                return "调试-移动：前排怪 无目标";
            }

            int threshold = Mathf.Max(1, _monsterMoveInputThreshold[idx]);
            int pending = Mathf.Clamp(_monsterMoveInputPending[idx], 0, threshold);
            int remaining = Mathf.Max(0, threshold - pending);
            string monsterId = idx < _monsterMarkerIds.Count ? _monsterMarkerIds[idx] : "unknown";
            return $"调试-移动：前排#{idx + 1} [{monsterId}] 还需 {remaining} 步 ({pending}/{threshold})";
        }

        private string BuildBattleDebugStatus()
        {
            if (!_inBattle)
            {
                return "调试-战斗：未接敌";
            }

            int threshold = Mathf.Max(1, _inputsPerBattleRoundRuntime);
            int pending = Mathf.Clamp(_pendingBattleInputEvents, 0, threshold);
            int remaining = Mathf.Max(0, threshold - pending);
            int roundsToKill = Mathf.CeilToInt(Mathf.Max(0, _enemyHp) / (float)Mathf.Max(1, _playerAttackPerRoundRuntime));
            return $"调试-战斗：回合 {_battleRoundCounter}，下回合剩 {remaining} 输入 ({pending}/{threshold})，预计 {roundsToKill} 回合结束";
        }

        private string BuildInputSourceDebugStatus()
        {
            if (_activityState == null)
            {
                return "调试-输入：InputActivityState 不可用";
            }

            return $"调试-输入：batch={_activityState.InputEventsThisSecond} ap={_activityState.ApFinal:0.00} | 键={_activityState.KeyDownCount} 鼠键={_activityState.MouseClickCount} 滚轮={_activityState.MouseScrollSteps} 移动px={_activityState.MouseMoveDistancePx:0} 手柄键={_activityState.JoypadButtonCount} 轴={_activityState.JoypadAxisInputCount}";
        }

        private string BuildWaveDebugStatus()
        {
            int frontIndex = _inBattle ? _battleMonsterIndex : FindFrontMonsterIndex();
            string frontMonsterId = (frontIndex >= 0 && frontIndex < _monsterMarkerIds.Count)
                ? _monsterMarkerIds[frontIndex]
                : "none";

            if (_levelConfigLoader == null)
            {
                return $"调试-副本：loader 不可用 | 当前前排#{frontIndex + 1} [{frontMonsterId}]";
            }

            if (_levelConfigLoader.TryGetActiveWaveProgress(out int nextIndex, out int waveCount, out string nextMonsterId))
            {
                return $"调试-副本：波次 {nextIndex}/{waveCount}，next=[{nextMonsterId}] | 当前前排#{frontIndex + 1} [{frontMonsterId}]";
            }

            return $"调试-副本：未配置 monster_wave（使用 spawn_table） | 当前前排#{frontIndex + 1} [{frontMonsterId}]";
        }

        private int FindFrontMonsterIndex()
        {
            int index = -1;
            float bestX = float.MaxValue;

            for (int i = 0; i < _monsterMarkers.Count; i++)
            {
                float x = _monsterMarkers[i].Position.X;
                if (x >= _playerMarker.Position.X + 50.0f && x < bestX)
                {
                    bestX = x;
                    index = i;
                }
            }

            return index;
        }

        private void ResetTrackVisual()
        {
            _battleMonsterIndex = -1;
            _inBattle = false;
            _battleMonsterId = "";
            ResetMonsterMoveState();
            _pendingBattleInputEvents = 0;
            _battleMonsterName = UiText.DefaultMonsterName;
            _enemyMaxHp = 24;
            _enemyAttackPower = 4;
            _inputsPerBattleRoundRuntime = InputsPerBattleRound;
            _enemyHp = _enemyMaxHp;
            _playerHp = _playerMaxHp;
            _battleInfoLabel.Text = "";
            _battleInfoLabel.Visible = false;
            _roundInfoLabel.Text = UiText.WaitingInput;

            float startX = 540.0f;
            for (int i = 0; i < _monsterMarkers.Count; i++)
            {
                _monsterMarkers[i].Visible = true;
                _monsterMarkers[i].Modulate = Colors.White;
                _monsterMarkers[i].Position = new Vector2(startX + i * MonsterRespawnSpacing, _monsterMarkers[i].Position.Y);
                AssignMonsterToMarker(i);
            }

            UpdateHpLabels();
            RefreshActorSlots();
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
        }

        private void UpdateHpLabels()
        {
            _playerHpLabel.Text = $"HP {_playerHp}/{_playerMaxHp}";
            _playerHpLabel.Position = new Vector2(_playerMarker.Position.X - 20.0f, _playerMarker.Position.Y + 22.0f);

            if (_inBattle && _battleMonsterIndex >= 0 && _battleMonsterIndex < _monsterMarkers.Count)
            {
                Label target = _monsterMarkers[_battleMonsterIndex];
                _enemyHpLabel.Visible = true;
                _enemyHpLabel.Text = $"HP {_enemyHp}/{_enemyMaxHp}";
                _enemyHpLabel.Position = new Vector2(target.Position.X - 24.0f, target.Position.Y + 22.0f);
            }
            else
            {
                _enemyHpLabel.Visible = false;
            }
        }

        private void RefreshActorSlots()
        {
            if (_playerSlotTexture != null)
            {
                _playerSlotTexture.Position = new Vector2(_playerMarker.Position.X - 16.0f, _playerMarker.Position.Y - 26.0f);
            }
            if (_playerSlotLabel != null)
            {
                _playerSlotLabel.Text = "主角";
                _playerSlotLabel.Position = new Vector2(_playerMarker.Position.X - 12.0f, _playerMarker.Position.Y - 24.0f);
            }

            if (_enemySlotTexture == null || _enemySlotLabel == null)
            {
                return;
            }

            int focusIndex = _inBattle ? _battleMonsterIndex : FindFrontMonsterIndex();
            RefreshMonsterSlots(focusIndex);
            if (focusIndex < 0 || focusIndex >= _monsterMarkers.Count)
            {
                _enemySlotTexture.Visible = false;
                _enemySlotLabel.Visible = false;
                _activeEnemyVisualMonsterId = "";
                return;
            }

            Label focus = _monsterMarkers[focusIndex];
            _enemySlotTexture.Visible = true;
            _enemySlotLabel.Visible = true;
            _enemySlotBasePosition = new Vector2(focus.Position.X - 16.0f, focus.Position.Y - 26.0f);
            _enemySlotTexture.Position = _enemySlotBasePosition;
            _enemySlotLabel.Position = new Vector2(focus.Position.X - 12.0f, focus.Position.Y - 24.0f);
            _enemySlotLabel.Text = _inBattle ? _battleMonsterName : "敌人";

            string focusMonsterId = _inBattle ? _battleMonsterId : _monsterMarkerIds[focusIndex];
            ApplyEnemyVisualConfig(focusMonsterId);
        }

        private void RefreshMonsterSlots(int focusIndex)
        {
            if (_monsterSlots.Count == 0)
            {
                return;
            }

            int count = Mathf.Min(_monsterSlots.Count, _monsterMarkers.Count);
            for (int i = 0; i < count; i++)
            {
                TextureRect slot = _monsterSlots[i];
                Label marker = _monsterMarkers[i];
                slot.Position = new Vector2(marker.Position.X - 16.0f, marker.Position.Y - 26.0f);
                slot.Visible = i != focusIndex;
                slot.Modulate = GetMarkerTint(_monsterMarkerIds[i]);
            }
        }

        private void ApplyEnemyVisualConfig(string monsterId)
        {
            if (_enemySlotTexture == null || _levelConfigLoader == null)
            {
                return;
            }

            if (_activeEnemyVisualMonsterId == monsterId)
            {
                return;
            }

            _activeEnemyVisualMonsterId = monsterId;
            _enemyVisualTime = 0.0;
            _enemySlotAnimType = "none";
            _enemySlotAnimSpeed = 0.0f;
            _enemySlotAnimAmplitude = 0.0f;
            _enemySlotTexture.Scale = Vector2.One;
            _enemySlotTexture.Modulate = Colors.White;
            _enemySlotTexture.Texture = _enemySlotDefaultTexture;

            if (string.IsNullOrEmpty(monsterId))
            {
                return;
            }

            if (!_levelConfigLoader.TryGetMonsterVisualConfig(
                monsterId,
                out string portraitPath,
                out string animationType,
                out double animSpeed,
                out double animAmplitude,
                out Color tint))
            {
                return;
            }

            if (!string.IsNullOrEmpty(portraitPath))
            {
                Texture2D? loaded = GD.Load<Texture2D>(portraitPath);
                if (loaded != null)
                {
                    _enemySlotTexture.Texture = loaded;
                }
            }

            _enemySlotTexture.Modulate = tint;
            _enemySlotAnimType = animationType.ToLowerInvariant();
            _enemySlotAnimSpeed = Mathf.Max(0.0f, (float)animSpeed);
            _enemySlotAnimAmplitude = Mathf.Max(0.0f, (float)animAmplitude);
        }

        private void UpdateRealmStageLabel()
        {
            if (_playerProgressState == null)
            {
                _realmStageLabel.Text = UiText.RealmFallback;
                return;
            }

            double required = Mathf.Max(1.0f, (float)_playerProgressState.RealmExpRequired);
            double percent = _playerProgressState.RealmExp / required * 100.0;
            _realmStageLabel.Text = UiText.RealmStage(_playerProgressState.RealmLevel, percent);
        }

        private void AssignMonsterToMarker(int markerIndex)
        {
            if (markerIndex < 0 || markerIndex >= _monsterMarkers.Count || markerIndex >= _monsterMarkerIds.Count)
            {
                return;
            }

            string monsterId = _levelConfigLoader?.RollSpawnMonsterId() ?? string.Empty;
            _monsterMarkerIds[markerIndex] = monsterId;
            int threshold = Mathf.Max(1, InputsPerMoveFrame);
            if (_levelConfigLoader != null &&
                !string.IsNullOrEmpty(monsterId) &&
                _levelConfigLoader.TryGetMonsterMoveRule(monsterId, out _, out int configured))
            {
                threshold = Mathf.Max(1, configured);
            }
            if (markerIndex < _monsterMoveInputThreshold.Count)
            {
                _monsterMoveInputThreshold[markerIndex] = threshold;
            }
            if (markerIndex < _monsterMoveInputPending.Count)
            {
                _monsterMoveInputPending[markerIndex] = 0;
            }
            ApplyMarkerVisual(_monsterMarkers[markerIndex], monsterId);
        }

        private static void ApplyMarkerVisual(Label marker, string monsterId)
        {
            marker.Modulate = GetMarkerTint(monsterId);
            switch (monsterId)
            {
                case "monster_slime_moss":
                    marker.Text = "SL";
                    break;
                case "monster_bat_shadow":
                    marker.Text = "BT";
                    break;
                case "monster_spider_cave":
                    marker.Text = "SP";
                    break;
                default:
                    marker.Text = "MO";
                    break;
            }
        }

        private static Color GetMarkerTint(string monsterId)
        {
            switch (monsterId)
            {
                case "monster_slime_moss":
                    return new Color(0.66f, 0.92f, 0.52f, 1.0f);
                case "monster_bat_shadow":
                    return new Color(0.75f, 0.75f, 0.92f, 1.0f);
                case "monster_spider_cave":
                    return new Color(0.95f, 0.56f, 0.56f, 1.0f);
                default:
                    return new Color(0.9f, 0.9f, 0.9f, 0.92f);
            }
        }
    }
}
