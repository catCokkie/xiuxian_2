using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using Xiuxian.Scripts.Core;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    public partial class ExploreProgressController
    {
        private string RunSimulationWithFilters(int battleCount)
        {
            if (_levelConfigLoader == null)
            {
                return "loader unavailable";
            }

            string levelId = string.IsNullOrEmpty(_simulationLevelFilterId)
                ? _levelConfigLoader.ActiveLevelId
                : _simulationLevelFilterId;

            return _levelConfigLoader.RunBattleSimulationFiltered(
                battleCount,
                levelId,
                _simulationMonsterFilterId);
        }

        private void CycleSimulationLevelFilter()
        {
            if (_levelConfigLoader == null)
            {
                return;
            }

            var levels = _levelConfigLoader.GetLevelIds();
            if (levels.Count == 0)
            {
                return;
            }

            int currentIndex = -1;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] == _simulationLevelFilterId)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                _simulationLevelFilterId = levels[0];
            }
            else
            {
                int next = (currentIndex + 1) % (levels.Count + 1);
                _simulationLevelFilterId = next >= levels.Count ? "" : levels[next];
            }

            _simulationMonsterFilterId = "";
        }

        private void CycleSimulationMonsterFilter()
        {
            if (_levelConfigLoader == null)
            {
                return;
            }

            string levelId = string.IsNullOrEmpty(_simulationLevelFilterId)
                ? _levelConfigLoader.ActiveLevelId
                : _simulationLevelFilterId;

            var monsters = _levelConfigLoader.GetSpawnMonsterIds(levelId);
            if (monsters.Count == 0)
            {
                _simulationMonsterFilterId = "";
                return;
            }

            int currentIndex = -1;
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i] == _simulationMonsterFilterId)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                _simulationMonsterFilterId = monsters[0];
            }
            else
            {
                int next = (currentIndex + 1) % (monsters.Count + 1);
                _simulationMonsterFilterId = next >= monsters.Count ? "" : monsters[next];
            }
        }

        private void OnRealmProgressChanged(int realmLevel, double realmExp, double realmExpRequired)
        {
            UpdateRealmStageLabel();
            RefreshCultivationPanel();
        }

        private void OnActionModeChanged(string modeId)
        {
            if (IsDungeonMode())
            {
                _battleInfoLabel.Visible = false;
                _roundInfoLabel.Text = $"{UiText.ExploreProgress(_exploreProgress)} | {BuildFrontMoveStatus()}";
            }
            else
            {
                _battleInfoLabel.Text = "主行为：修炼";
                _battleInfoLabel.Visible = true;
                _roundInfoLabel.Text = "副本暂停（修炼模式）";
            }

            RefreshActionModeOptionButton();
            RefreshDebugPanel();
        }

        private void ConfigureActionModeOptionButton()
        {
            if (_actionModeOptionButton == null)
            {
                return;
            }

            _actionModeOptionButton.Clear();
            _actionModeOptionButton.AddItem(UiText.ActionModeDungeon, 0);
            _actionModeOptionButton.AddItem(UiText.ActionModeCultivation, 1);
            _actionModeOptionButton.TooltipText = "切换主行为（等同 F4）";
            if (!_actionModeOptionBound)
            {
                _actionModeOptionButton.ItemSelected += OnActionModeOptionSelected;
                _actionModeOptionBound = true;
            }
        }

        private void ConfigureLevelOptionButton()
        {
            if (_levelOptionButton == null)
            {
                return;
            }

            if (!_levelOptionBound)
            {
                _levelOptionButton.ItemSelected += OnLevelOptionSelected;
                _levelOptionBound = true;
            }
            _levelOptionButton.TooltipText = "切换已解锁副本";
        }

        private void RefreshActionModeOptionButton()
        {
            if (_actionModeOptionButton == null)
            {
                return;
            }

            _syncingActionModeOption = true;
            int selected = IsDungeonMode() ? 0 : 1;
            _actionModeOptionButton.Select(selected);
            _actionModeOptionButton.Text = selected == 0 ? UiText.ActionModeDungeon : UiText.ActionModeCultivation;
            _actionModeOptionButton.Visible = false;
            _syncingActionModeOption = false;
        }

        private void RefreshLevelOptionButton()
        {
            if (_levelOptionButton == null || _levelConfigLoader == null)
            {
                return;
            }

            _syncingLevelOption = true;
            _levelOptionButton.Clear();
            var unlocked = _levelConfigLoader.GetUnlockedLevelIds();
            int selectedIndex = -1;
            for (int i = 0; i < unlocked.Count; i++)
            {
                string levelId = unlocked[i];
                string levelName = _levelConfigLoader.GetLevelName(levelId);
                string text = string.IsNullOrEmpty(levelName) ? levelId : levelName;
                _levelOptionButton.AddItem(text, i);
                _levelOptionButton.SetItemMetadata(i, levelId);
                if (levelId == _levelConfigLoader.ActiveLevelId)
                {
                    selectedIndex = i;
                }
            }

            if (_levelOptionButton.ItemCount > 0)
            {
                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                }
                _levelOptionButton.Select(selectedIndex);
                string selectedLevelId = _levelOptionButton.GetItemMetadata(selectedIndex).AsString();
                _levelOptionButton.TooltipText = string.IsNullOrEmpty(selectedLevelId)
                    ? "切换已解锁副本"
                    : $"当前副本: {selectedLevelId}";
            }
            _levelOptionButton.Visible = false;
            _syncingLevelOption = false;
        }

        public void ToggleMainActionMode()
        {
            _actionState?.ToggleMode();
        }

        public bool TrySelectNextUnlockedLevel()
        {
            if (_levelConfigLoader == null || !_levelConfigLoader.TrySetNextUnlockedLevelAsActive())
            {
                return false;
            }

            ApplyLevelConfig();
            _zoneLabel.Text = _currentZone;
            _exploreProgress = 0.0f;
            _progressBar.Value = 0.0f;
            ResetTrackVisual();
            RefreshLevelOptionButton();
            return true;
        }

        private void OnActionModeOptionSelected(long index)
        {
            if (_syncingActionModeOption || _actionState == null)
            {
                return;
            }

            string modeId = index == 1
                ? PlayerActionState.ModeCultivation
                : PlayerActionState.ModeDungeon;
            _actionState.SetMode(modeId);
        }

        private void OnLevelOptionSelected(long index)
        {
            if (_syncingLevelOption || _levelOptionButton == null || _levelConfigLoader == null)
            {
                return;
            }

            int selectedIndex = (int)index;
            if (selectedIndex < 0 || selectedIndex >= _levelOptionButton.ItemCount)
            {
                return;
            }

            string levelId = _levelOptionButton.GetItemMetadata(selectedIndex).AsString();
            if (string.IsNullOrEmpty(levelId))
            {
                return;
            }

            if (_levelConfigLoader.TrySetActiveLevelIfUnlocked(levelId))
            {
                ApplyLevelConfig();
                _zoneLabel.Text = _currentZone;
                _exploreProgress = 0.0f;
                _progressBar.Value = 0.0f;
                ResetTrackVisual();
                RefreshDebugPanel();
            }
        }

        private void OnLevelConfigLoaded(string levelId, string levelName)
        {
            ApplyLevelConfig();
            _zoneLabel.Text = _currentZone;
            RefreshLevelOptionButton();
            RefreshValidationPanel();
        }

        private void OnInputBatchTick(int inputEvents, double apFinal)
        {
            _activityRateLabel.Text = UiText.BatchInputAndAp(inputEvents, apFinal);
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
            if (inputEvents <= 0)
            {
                return;
            }

            if (!IsDungeonMode())
            {
                _battleInfoLabel.Text = "主行为：修炼";
                _battleInfoLabel.Visible = true;
                _roundInfoLabel.Text = "副本暂停（修炼模式）";
                return;
            }

            if (_inBattle)
            {
                AdvanceBattleByInput(inputEvents);
                return;
            }

            AdvanceExploreByInput(inputEvents);
            TryStartBattle();
        }

        private void AdvanceExploreByInput(int inputEvents)
        {
            var progressResult = ExploreProgressionRule.Advance(_exploreProgress, inputEvents, ProgressPerInput, MaxProgress);
            _exploreProgress = progressResult.RawProgress;
            _progressBar.Value = progressResult.RawProgress;

            int frames = MoveMonsterQueueByInputs(inputEvents);
            _moveFrameCounter += frames;

            _battleInfoLabel.Text = UiText.ExploreFrame(_moveFrameCounter);
            _battleInfoLabel.Visible = false;
            _roundInfoLabel.Text = $"{UiText.ExploreProgress(_exploreProgress)} | {BuildFrontMoveStatus()}";
            RefreshMoveDebugLabel();

            if (progressResult.Completed)
            {
                _exploreProgress = progressResult.NextProgress;
                _progressBar.Value = progressResult.NextProgress;
                ApplyLevelCompletionRewards();
                if (_levelConfigLoader != null && _levelConfigLoader.TryAdvanceToNextUnlockedLevel())
                {
                    ApplyLevelConfig();
                    _zoneLabel.Text = _currentZone;
                }
                _battleInfoLabel.Text = UiText.ZoneComplete;
                _battleInfoLabel.Visible = true;
                ResetMonsterMoveState();
            }

            UpdateHpLabels();
            RefreshActorSlots();
            RefreshDebugPanel();
        }

        private int MoveMonsterQueueByInputs(int inputEvents)
        {
            if (inputEvents <= 0)
            {
                return 0;
            }

            int movedFrames = 0;

            _queueMoveInputPending += inputEvents;
            int baseThreshold = Mathf.Max(1, InputsPerMoveFrame);
            int queueFrames = _queueMoveInputPending / baseThreshold;
            if (queueFrames > 0)
            {
                _queueMoveInputPending -= queueFrames * baseThreshold;
                float queueShift = queueFrames * MonsterMovePxPerFrame;
                for (int i = 0; i < _monsterMarkers.Count; i++)
                {
                    Label m = _monsterMarkers[i];
                    m.Position = new Vector2(m.Position.X - queueShift, m.Position.Y);
                }
                movedFrames += queueFrames;
            }

            int frontIndex = FindFrontMonsterIndex();
            if (frontIndex >= 0 && frontIndex < _monsterMarkers.Count)
            {
                int threshold = frontIndex < _monsterMoveInputThreshold.Count
                    ? Mathf.Max(1, _monsterMoveInputThreshold[frontIndex])
                    : Mathf.Max(1, InputsPerMoveFrame);
                if (frontIndex < _monsterMoveInputPending.Count)
                {
                    _monsterMoveInputPending[frontIndex] += inputEvents;
                }

                int bonusFrames = frontIndex < _monsterMoveInputPending.Count
                    ? _monsterMoveInputPending[frontIndex] / threshold
                    : inputEvents / threshold;
                if (bonusFrames > 0)
                {
                    if (frontIndex < _monsterMoveInputPending.Count)
                    {
                        _monsterMoveInputPending[frontIndex] -= bonusFrames * threshold;
                    }

                    Label front = _monsterMarkers[frontIndex];
                    float bonusShift = bonusFrames * MonsterMovePxPerFrame;
                    front.Position = new Vector2(front.Position.X - bonusShift, front.Position.Y);
                    movedFrames += bonusFrames;
                }
            }

            for (int i = 0; i < _monsterMarkers.Count; i++)
            {
                Label monster = _monsterMarkers[i];
                if (monster.Position.X >= 120.0f)
                {
                    continue;
                }

                float rightMostX = Mathf.Max(GetRightMostMonsterX(), monster.Position.X);
                monster.Position = new Vector2(rightMostX + MonsterRespawnSpacing, monster.Position.Y);
                AssignMonsterToMarker(i);
            }

            return movedFrames;
        }

        private void ResetMonsterMoveState()
        {
            _queueMoveInputPending = 0;
            for (int i = 0; i < _monsterMoveInputPending.Count; i++)
            {
                _monsterMoveInputPending[i] = 0;
            }
        }

        private string BuildFrontMoveStatus()
        {
            int idx = FindFrontMonsterIndex();
            if (idx < 0 || idx >= _monsterMoveInputThreshold.Count || idx >= _monsterMoveInputPending.Count)
            {
                return "move idle";
            }

            int threshold = Mathf.Max(1, _monsterMoveInputThreshold[idx]);
            int pending = Mathf.Clamp(_monsterMoveInputPending[idx], 0, threshold);
            int remaining = Mathf.Max(0, threshold - pending);
            return $"move remain {remaining} ({pending}/{threshold})";
        }

        private void ApplyLevelConfig()
        {
            if (_levelConfigLoader == null)
            {
                return;
            }

            _currentZone = _levelConfigLoader.ActiveLevelName;
            ProgressPerInput = (float)(_levelConfigLoader.ProgressPer100Inputs / 100.0);
            _playerMaxHp = Mathf.Max(1, _levelConfigLoader.PlayerBaseHp);
            _playerAttackPerRoundRuntime = Mathf.Max(1, _levelConfigLoader.PlayerAttackPerRound);
            _enemyDamageDividerRuntime = Mathf.Max(1, _levelConfigLoader.EnemyDamageDivider);
            _enemyMinDamageRuntime = Mathf.Max(1, _levelConfigLoader.EnemyMinDamagePerRound);
            _playerHp = Mathf.Clamp(_playerHp, 0, _playerMaxHp);
        }

        public Godot.Collections.Dictionary<string, Variant> ToRuntimeDictionary()
        {
            string zoneId = _levelConfigLoader?.ActiveLevelId ?? "";
            string battleState = _inBattle ? "in_battle" : "exploring";
            var markerStates = new Godot.Collections.Array<Variant>();
            for (int i = 0; i < _monsterMarkers.Count; i++)
            {
                Label marker = _monsterMarkers[i];
                var item = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["x"] = marker.Position.X,
                    ["y"] = marker.Position.Y,
                    ["monster_id"] = i < _monsterMarkerIds.Count ? _monsterMarkerIds[i] : "",
                    ["move_pending"] = i < _monsterMoveInputPending.Count ? _monsterMoveInputPending[i] : 0,
                    ["move_threshold"] = i < _monsterMoveInputThreshold.Count ? _monsterMoveInputThreshold[i] : Mathf.Max(1, InputsPerMoveFrame)
                };
                markerStates.Add(item);
            }

            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["zone_id"] = zoneId,
                ["zone_name"] = _currentZone,
                ["explore_progress"] = _exploreProgress,
                ["battle_state"] = battleState,
                ["move_frame_counter"] = _moveFrameCounter,
                ["queue_move_input_pending"] = _queueMoveInputPending,
                ["player_hp"] = _playerHp,
                ["player_max_hp"] = _playerMaxHp,
                ["enemy_hp"] = _enemyHp,
                ["enemy_max_hp"] = _enemyMaxHp,
                ["enemy_attack_power"] = _enemyAttackPower,
                ["inputs_per_battle_round_runtime"] = _inputsPerBattleRoundRuntime,
                ["player_attack_per_round_runtime"] = _playerAttackPerRoundRuntime,
                ["enemy_damage_divider_runtime"] = _enemyDamageDividerRuntime,
                ["enemy_min_damage_runtime"] = _enemyMinDamageRuntime,
                ["battle_round_counter"] = _battleRoundCounter,
                ["pending_battle_input_events"] = _pendingBattleInputEvents,
                ["battle_monster_index"] = _battleMonsterIndex,
                ["battle_monster_id"] = _battleMonsterId,
                ["battle_monster_name"] = _battleMonsterName,
                ["monster_marker_states"] = markerStates,
                ["recent_battle_logs"] = BuildRecentBattleLogsArray()
            };
        }

        public void FromRuntimeDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            if (data.ContainsKey("zone_name"))
            {
                _currentZone = data["zone_name"].AsString();
            }

            if (data.ContainsKey("explore_progress"))
            {
                _exploreProgress = Mathf.Clamp((float)data["explore_progress"].AsDouble(), 0.0f, MaxProgress);
            }

            _moveFrameCounter = data.ContainsKey("move_frame_counter") ? Mathf.Max(0, data["move_frame_counter"].AsInt32()) : _moveFrameCounter;
            _queueMoveInputPending = data.ContainsKey("queue_move_input_pending") ? Mathf.Max(0, data["queue_move_input_pending"].AsInt32()) : 0;
            _playerHp = data.ContainsKey("player_hp") ? Mathf.Max(0, data["player_hp"].AsInt32()) : _playerHp;
            _playerMaxHp = data.ContainsKey("player_max_hp") ? Mathf.Max(1, data["player_max_hp"].AsInt32()) : _playerMaxHp;
            _enemyHp = data.ContainsKey("enemy_hp") ? Mathf.Max(0, data["enemy_hp"].AsInt32()) : _enemyHp;
            _enemyMaxHp = data.ContainsKey("enemy_max_hp") ? Mathf.Max(1, data["enemy_max_hp"].AsInt32()) : _enemyMaxHp;
            _enemyAttackPower = data.ContainsKey("enemy_attack_power") ? Mathf.Max(1, data["enemy_attack_power"].AsInt32()) : _enemyAttackPower;
            _inputsPerBattleRoundRuntime = data.ContainsKey("inputs_per_battle_round_runtime") ? Mathf.Max(1, data["inputs_per_battle_round_runtime"].AsInt32()) : _inputsPerBattleRoundRuntime;
            _playerAttackPerRoundRuntime = data.ContainsKey("player_attack_per_round_runtime") ? Mathf.Max(1, data["player_attack_per_round_runtime"].AsInt32()) : _playerAttackPerRoundRuntime;
            _enemyDamageDividerRuntime = data.ContainsKey("enemy_damage_divider_runtime") ? Mathf.Max(1, data["enemy_damage_divider_runtime"].AsInt32()) : _enemyDamageDividerRuntime;
            _enemyMinDamageRuntime = data.ContainsKey("enemy_min_damage_runtime") ? Mathf.Max(1, data["enemy_min_damage_runtime"].AsInt32()) : _enemyMinDamageRuntime;
            _battleRoundCounter = data.ContainsKey("battle_round_counter") ? Mathf.Max(0, data["battle_round_counter"].AsInt32()) : _battleRoundCounter;
            _pendingBattleInputEvents = data.ContainsKey("pending_battle_input_events") ? Mathf.Max(0, data["pending_battle_input_events"].AsInt32()) : _pendingBattleInputEvents;
            _battleMonsterIndex = data.ContainsKey("battle_monster_index") ? data["battle_monster_index"].AsInt32() : _battleMonsterIndex;
            _battleMonsterId = data.ContainsKey("battle_monster_id") ? data["battle_monster_id"].AsString() : _battleMonsterId;
            _battleMonsterName = data.ContainsKey("battle_monster_name") ? data["battle_monster_name"].AsString() : _battleMonsterName;
            _recentBattleLogs.Clear();
            if (data.ContainsKey("recent_battle_logs") && data["recent_battle_logs"].VariantType == Variant.Type.Array)
            {
                var battleLogs = (Godot.Collections.Array<Variant>)data["recent_battle_logs"];
                foreach (Variant itemVariant in battleLogs)
                {
                    if (itemVariant.VariantType != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    var item = (Godot.Collections.Dictionary<string, Variant>)itemVariant;
                    var entry = new BattleLogEntry
                    {
                        TimestampUnix = item.ContainsKey("ts") ? item["ts"].AsInt64() : 0L,
                        Result = item.ContainsKey("result") ? item["result"].AsString() : "victory",
                        MonsterId = item.ContainsKey("monster_id") ? item["monster_id"].AsString() : "",
                        MonsterName = item.ContainsKey("monster_name") ? item["monster_name"].AsString() : UiText.DefaultMonsterName,
                        Lingqi = item.ContainsKey("lingqi") ? item["lingqi"].AsDouble() : 0.0,
                        Insight = item.ContainsKey("insight") ? item["insight"].AsDouble() : 0.0
                    };

                    if (item.ContainsKey("items") && item["items"].VariantType == Variant.Type.Dictionary)
                    {
                        var itemDict = (Godot.Collections.Dictionary<string, Variant>)item["items"];
                        foreach (string key in itemDict.Keys)
                        {
                            int qty = Math.Max(0, itemDict[key].AsInt32());
                            if (qty > 0)
                            {
                                entry.Items[key] = qty;
                            }
                        }
                    }

                    _recentBattleLogs.Add(entry);
                }
            }

            if (data.ContainsKey("monster_marker_states") && data["monster_marker_states"].VariantType == Variant.Type.Array)
            {
                var markerStates = (Godot.Collections.Array<Variant>)data["monster_marker_states"];
                int count = Mathf.Min(markerStates.Count, _monsterMarkers.Count);
                for (int i = 0; i < count; i++)
                {
                    if (markerStates[i].VariantType != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    var item = (Godot.Collections.Dictionary<string, Variant>)markerStates[i];
                    Label marker = _monsterMarkers[i];
                    float x = item.ContainsKey("x") ? (float)item["x"].AsDouble() : marker.Position.X;
                    float y = item.ContainsKey("y") ? (float)item["y"].AsDouble() : marker.Position.Y;
                    marker.Position = new Vector2(x, y);

                    string monsterId = item.ContainsKey("monster_id") ? item["monster_id"].AsString() : "";
                    if (i < _monsterMarkerIds.Count)
                    {
                        _monsterMarkerIds[i] = monsterId;
                    }

                    if (i < _monsterMoveInputPending.Count)
                    {
                        _monsterMoveInputPending[i] = item.ContainsKey("move_pending") ? Mathf.Max(0, item["move_pending"].AsInt32()) : 0;
                    }
                    if (i < _monsterMoveInputThreshold.Count)
                    {
                        int threshold = item.ContainsKey("move_threshold") ? item["move_threshold"].AsInt32() : Mathf.Max(1, InputsPerMoveFrame);
                        _monsterMoveInputThreshold[i] = Mathf.Max(1, threshold);
                    }

                    ApplyMarkerVisual(marker, monsterId);
                }
            }

            string battleState = data.ContainsKey("battle_state") ? data["battle_state"].AsString() : "exploring";
            _inBattle = battleState == "in_battle";
            if (_battleMonsterIndex < 0 || _battleMonsterIndex >= _monsterMarkers.Count)
            {
                _battleMonsterIndex = FindFrontMonsterIndex();
            }

            if (string.IsNullOrEmpty(_battleMonsterId) &&
                _battleMonsterIndex >= 0 &&
                _battleMonsterIndex < _monsterMarkerIds.Count)
            {
                _battleMonsterId = _monsterMarkerIds[_battleMonsterIndex];
            }

            _zoneLabel.Text = _currentZone;
            _progressBar.Value = _exploreProgress;
            _playerHp = Mathf.Clamp(_playerHp, 0, _playerMaxHp);
            _enemyHp = Mathf.Clamp(_enemyHp, 0, _enemyMaxHp);

            if (_inBattle)
            {
                if (_levelConfigLoader != null && !string.IsNullOrEmpty(_battleMonsterId))
                {
                    int savedEnemyHp = _enemyHp;
                    ConfigureBattleMonster();
                    _enemyHp = Mathf.Clamp(savedEnemyHp, 0, _enemyMaxHp);
                }

                _battleInfoLabel.Text = UiText.BattleInProgress(_battleMonsterName);
                _battleInfoLabel.Visible = true;
                int threshold = Mathf.Max(1, _inputsPerBattleRoundRuntime);
                _roundInfoLabel.Text = $"{UiText.BattleRound(_battleRoundCounter, _battleMonsterName, _enemyHp)} | next {_pendingBattleInputEvents}/{threshold}";
            }
            else
            {
                _battleInfoLabel.Text = "";
                _battleInfoLabel.Visible = false;
                _roundInfoLabel.Text = $"{UiText.ExploreProgress(_exploreProgress)} | {BuildFrontMoveStatus()}";
            }

            UpdateHpLabels();
            RefreshActorSlots();
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
            EmitSignal(SignalName.RecentBattleLogsChanged);
        }

        public string BuildRecentBattleLogsText(int maxEntries = 10)
        {
            if (_recentBattleLogs.Count == 0)
            {
                return "最近战斗\n- 暂无记录";
            }

            int limit = Math.Max(1, maxEntries);
            var sb = new StringBuilder();
            sb.Append("最近战斗");

            int shown = 0;
            for (int i = _recentBattleLogs.Count - 1; i >= 0 && shown < limit; i--)
            {
                BattleLogEntry entry = _recentBattleLogs[i];
                string timeText = entry.TimestampUnix > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(entry.TimestampUnix).ToLocalTime().ToString("HH:mm:ss")
                    : "--:--:--";
                string resultText = entry.Result == "defeat" ? "战败" : "胜利";
                string itemText = entry.Items.Count > 0 ? BuildDropSummary(entry.Items) : "none";
                sb.Append($"\n- [{timeText}] {resultText} {entry.MonsterName} | lq={entry.Lingqi:0} in={entry.Insight:0} | items={itemText}");
                shown++;
            }

            return sb.ToString();
        }

        public Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> GetRecentBattleLogsSnapshot(int maxEntries = 10)
        {
            int limit = Math.Max(1, maxEntries);
            var result = new Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>>();
            for (int i = _recentBattleLogs.Count - 1; i >= 0 && result.Count < limit; i--)
            {
                BattleLogEntry entry = _recentBattleLogs[i];
                var itemDict = new Godot.Collections.Dictionary<string, Variant>();
                foreach (var kv in entry.Items)
                {
                    itemDict[kv.Key] = kv.Value;
                }

                result.Add(new Godot.Collections.Dictionary<string, Variant>
                {
                    ["ts"] = entry.TimestampUnix,
                    ["result"] = entry.Result,
                    ["monster_id"] = entry.MonsterId,
                    ["monster_name"] = entry.MonsterName,
                    ["lingqi"] = entry.Lingqi,
                    ["insight"] = entry.Insight,
                    ["items"] = itemDict
                });
            }

            return result;
        }

        private Godot.Collections.Array<Variant> BuildRecentBattleLogsArray()
        {
            var result = new Godot.Collections.Array<Variant>();
            foreach (BattleLogEntry entry in _recentBattleLogs)
            {
                var itemDict = new Godot.Collections.Dictionary<string, Variant>();
                foreach (var kv in entry.Items)
                {
                    itemDict[kv.Key] = kv.Value;
                }

                result.Add(new Godot.Collections.Dictionary<string, Variant>
                {
                    ["ts"] = entry.TimestampUnix,
                    ["result"] = entry.Result,
                    ["monster_id"] = entry.MonsterId,
                    ["monster_name"] = entry.MonsterName,
                    ["lingqi"] = entry.Lingqi,
                    ["insight"] = entry.Insight,
                    ["items"] = itemDict
                });
            }
            return result;
        }

        private void AddRecentBattleLog(string result, string monsterId, string monsterName, double lingqi, double insight, Dictionary<string, int> items)
        {
            var copiedItems = new Dictionary<string, int>();
            foreach (var kv in items)
            {
                if (kv.Value > 0)
                {
                    copiedItems[kv.Key] = kv.Value;
                }
            }

            _recentBattleLogs.Add(new BattleLogEntry
            {
                TimestampUnix = (long)Time.GetUnixTimeFromSystem(),
                Result = result,
                MonsterId = monsterId,
                MonsterName = string.IsNullOrEmpty(monsterName) ? UiText.DefaultMonsterName : monsterName,
                Lingqi = Math.Max(0.0, lingqi),
                Insight = Math.Max(0.0, insight),
                Items = copiedItems
            });

            if (_recentBattleLogs.Count > MaxRecentBattleLogs)
            {
                _recentBattleLogs.RemoveAt(0);
            }

            EmitSignal(SignalName.RecentBattleLogsChanged);
        }

        private void RefreshCultivationPanel()
        {
            if (_playerProgressState == null || _cultivationProgressBar == null || _breakthroughButton == null)
            {
                return;
            }

            double required = Mathf.Max(1.0f, (float)_playerProgressState.RealmExpRequired);
            double percentRaw = _playerProgressState.RealmExp / required * 100.0;
            double percent = Mathf.Clamp((float)percentRaw, 0.0f, 100.0f);
            _cultivationProgressBar.Value = percent;
            _cultivationProgressBar.TooltipText = $"修炼进度 {_playerProgressState.RealmExp:0.0}/{required:0.0}";
            _breakthroughButton.Disabled = !_playerProgressState.CanBreakthrough;
            _breakthroughButton.Text = _playerProgressState.CanBreakthrough ? "突破!" : "突破";
            _breakthroughButton.TooltipText = _playerProgressState.CanBreakthrough
                ? "可突破，点击提升境界"
                : $"进度未满，还需 {Mathf.Max(0.0f, (float)(required - _playerProgressState.RealmExp)):0.0}";
            if (_cultivationLabel != null)
            {
                _cultivationLabel.Text = $"修炼 {_playerProgressState.RealmExp:0}/{required:0}";
            }
        }

        private void OnBreakthroughPressed()
        {
            if (_playerProgressState == null)
            {
                return;
            }

            _playerProgressState.TryBreakthrough();
            RefreshCultivationPanel();
        }
    }
}
