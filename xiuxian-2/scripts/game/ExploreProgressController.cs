using Godot;
using System.Collections.Generic;
using System.Text;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    /// <summary>
    /// Bottom track controller:
    /// player fixed on the left, monsters advance from the right,
    /// and HP is shown under player + current battle target.
    /// </summary>
    public partial class ExploreProgressController : Node
    {
        [Export] public NodePath ProgressBarPath = "MainBarWindow/Chrome/ExploreProgressBar";
        [Export] public NodePath ZoneLabelPath = "MainBarWindow/Chrome/ZoneLabel";
        [Export] public NodePath ActivityRateLabelPath = "MainBarWindow/Chrome/ActivityRateLabel";
        [Export] public NodePath RealmStageLabelPath = "MainBarWindow/Chrome/RealmStageLabel";

        [Export] public NodePath BattleInfoLabelPath = "MainBarWindow/Chrome/BattleTrack/BattleInfoLabel";
        [Export] public NodePath RoundInfoLabelPath = "MainBarWindow/Chrome/BattleTrack/RoundInfoLabel";
        [Export] public NodePath PlayerMarkerPath = "MainBarWindow/Chrome/BattleTrack/PlayerMarker";
        [Export] public NodePath PlayerHpLabelPath = "MainBarWindow/Chrome/BattleTrack/PlayerHpLabel";
        [Export] public NodePath EnemyHpLabelPath = "MainBarWindow/Chrome/BattleTrack/EnemyHpLabel";

        [Export] public NodePath ActivityStatePath = "/root/InputActivityState";
        [Export] public NodePath BackpackStatePath = "/root/BackpackState";
        [Export] public NodePath PlayerProgressPath = "/root/PlayerProgressState";
        [Export] public NodePath ResourceWalletPath = "/root/ResourceWalletState";
        [Export] public NodePath LevelConfigLoaderPath = "/root/LevelConfigLoader";

        // Explore progress is input-event driven (percent per input event), not AP-driven.
        [Export] public float ProgressPerInput = 0.02f;
        [Export] public int InputsPerMoveFrame = 4;
        [Export] public int InputsPerBattleRound = 18;
        [Export] public float MaxProgress = 100.0f;

        [Export] public float MonsterMovePxPerFrame = 3.8f;
        [Export] public float MonsterRespawnSpacing = 110.0f;
        [Export] public float BattleTriggerX = 220.0f;

        private ProgressBar _progressBar = null!;
        private Label _zoneLabel = null!;
        private Label _activityRateLabel = null!;
        private Label _realmStageLabel = null!;
        private Label _battleInfoLabel = null!;
        private Label _roundInfoLabel = null!;
        private Label _playerMarker = null!;
        private Label _playerHpLabel = null!;
        private Label _enemyHpLabel = null!;
        private Label _debugPanelLabel = null!;
        private readonly List<Label> _monsterMarkers = new();
        private readonly List<string> _monsterMarkerIds = new();

        private InputActivityState? _activityState;
        private BackpackState? _backpackState;
        private PlayerProgressState? _playerProgressState;
        private ResourceWalletState? _resourceWalletState;
        private LevelConfigLoader? _levelConfigLoader;

        private string _currentZone = "幽泉洞窟";
        private float _exploreProgress;
        private int _moveFrameCounter;
        private int _battleRoundCounter;
        private bool _inBattle;
        private int _battleMonsterIndex = -1;
        private string _battleMonsterId = "";
        private string _battleMonsterName = "妖物";
        private int _enemyHp = 24;
        private int _enemyMaxHp = 24;
        private int _playerHp = 36;
        private int _playerMaxHp = 36;
        private int _enemyAttackPower = 4;
        private int _inputsPerBattleRoundRuntime = 18;
        private int _playerAttackPerRoundRuntime = 4;
        private int _enemyDamageDividerRuntime = 4;
        private int _enemyMinDamageRuntime = 1;
        private bool _debugPanelVisible;
        private string _lastDropSummary = "none";
        private string _lastSimulationSummary = "no simulation";

        public override void _Ready()
        {
            _progressBar = GetNode<ProgressBar>(ProgressBarPath);
            _zoneLabel = GetNode<Label>(ZoneLabelPath);
            _activityRateLabel = GetNode<Label>(ActivityRateLabelPath);
            _realmStageLabel = GetNode<Label>(RealmStageLabelPath);
            _battleInfoLabel = GetNode<Label>(BattleInfoLabelPath);
            _roundInfoLabel = GetNode<Label>(RoundInfoLabelPath);
            _playerMarker = GetNode<Label>(PlayerMarkerPath);
            _playerHpLabel = GetNode<Label>(PlayerHpLabelPath);
            _enemyHpLabel = GetNode<Label>(EnemyHpLabelPath);
            EnsureDebugPanel();

            CacheMonsterMarkers();

            _activityState = GetNodeOrNull<InputActivityState>(ActivityStatePath);
            _backpackState = GetNodeOrNull<BackpackState>(BackpackStatePath);
            _playerProgressState = GetNodeOrNull<PlayerProgressState>(PlayerProgressPath);
            _resourceWalletState = GetNodeOrNull<ResourceWalletState>(ResourceWalletPath);
            _levelConfigLoader = GetNodeOrNull<LevelConfigLoader>(LevelConfigLoaderPath);

            if (_activityState == null || _monsterMarkers.Count == 0)
            {
                GD.PushError("ExploreProgressController: missing InputActivityState or monster markers.");
                return;
            }

            _activityState.InputBatchTick += OnInputBatchTick;
            if (_levelConfigLoader != null)
            {
                _levelConfigLoader.ConfigLoaded += OnLevelConfigLoaded;
            }

            ApplyLevelConfig();
            _zoneLabel.Text = _currentZone;
            _progressBar.MaxValue = MaxProgress;
            _progressBar.Value = _exploreProgress;
            UpdateRealmStageLabel();
            ResetTrackVisual();
            RefreshDebugPanel();

            if (_playerProgressState != null)
            {
                _playerProgressState.RealmProgressChanged += OnRealmProgressChanged;
            }
        }

        public override void _ExitTree()
        {
            if (_activityState != null)
            {
                _activityState.InputBatchTick -= OnInputBatchTick;
            }
            if (_levelConfigLoader != null)
            {
                _levelConfigLoader.ConfigLoaded -= OnLevelConfigLoaded;
            }
            if (_playerProgressState != null)
            {
                _playerProgressState.RealmProgressChanged -= OnRealmProgressChanged;
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
            {
                if (keyEvent.Keycode == Key.F8)
                {
                    _debugPanelVisible = !_debugPanelVisible;
                    _debugPanelLabel.Visible = _debugPanelVisible;
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F9)
                {
                    if (_levelConfigLoader != null)
                    {
                        _lastSimulationSummary = _levelConfigLoader.RunBattleSimulation(200);
                    }
                    RefreshDebugPanel();
                }
            }
        }

        private void OnRealmProgressChanged(int realmLevel, double realmExp, double realmExpRequired)
        {
            UpdateRealmStageLabel();
        }

        private void OnLevelConfigLoaded(string levelId, string levelName)
        {
            ApplyLevelConfig();
            _zoneLabel.Text = _currentZone;
        }

        private void CacheMonsterMarkers()
        {
            _monsterMarkers.Clear();
            _monsterMarkerIds.Clear();
            for (int i = 1; i <= 8; i++)
            {
                NodePath markerPath = $"{PlayerMarkerPath.GetConcatenatedNames().Replace("PlayerMarker", $"MonsterMarker{i:00}")}";
                Label marker = GetNodeOrNull<Label>(markerPath);
                if (marker != null)
                {
                    _monsterMarkers.Add(marker);
                    _monsterMarkerIds.Add(string.Empty);
                }
            }
        }

        private void OnInputBatchTick(int inputEvents, double apFinal)
        {
            // AP is displayed for resource settlement transparency; progress uses inputEvents only.
            _activityRateLabel.Text = $"本批输入 {inputEvents} | 本批AP(资源) {apFinal:0.0}";
            RefreshDebugPanel();
            if (inputEvents <= 0)
            {
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
            // Core rule: explore progress is computed directly from input event count.
            _exploreProgress = Mathf.Min(_exploreProgress + inputEvents * ProgressPerInput, MaxProgress);
            _progressBar.Value = _exploreProgress;

            int frames = inputEvents / InputsPerMoveFrame;
            if (frames > 0)
            {
                _moveFrameCounter += frames;
                float shift = frames * MonsterMovePxPerFrame;
                MoveMonsterQueue(shift);
            }

            _battleInfoLabel.Text = $"探索中  帧:{_moveFrameCounter}";
            _roundInfoLabel.Text = $"进度 {_exploreProgress:0.0}%";

            if (_exploreProgress >= MaxProgress)
            {
                _exploreProgress = 0.0f;
                _progressBar.Value = 0.0f;
                ApplyLevelCompletionRewards();
                if (_levelConfigLoader != null && _levelConfigLoader.AdvanceToNextLevel())
                {
                    ApplyLevelConfig();
                    _zoneLabel.Text = _currentZone;
                }
                _battleInfoLabel.Text = "区域探索完成，切换下一区域";
            }

            UpdateHpLabels();
            RefreshDebugPanel();
        }

        private void MoveMonsterQueue(float shift)
        {
            float rightMostX = GetRightMostMonsterX();

            foreach (Label monster in _monsterMarkers)
            {
                monster.Position = new Vector2(monster.Position.X - shift, monster.Position.Y);
                if (monster.Position.X < 120.0f)
                {
                    rightMostX = Mathf.Max(rightMostX, GetRightMostMonsterX());
                    monster.Position = new Vector2(rightMostX + MonsterRespawnSpacing, monster.Position.Y);
                    rightMostX = monster.Position.X;
                    int idx = _monsterMarkers.IndexOf(monster);
                    if (idx >= 0)
                    {
                        AssignMonsterToMarker(idx);
                    }
                }
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
            if (candidate >= 0 && candidate < _monsterMarkerIds.Count)
            {
                _battleMonsterId = _monsterMarkerIds[candidate];
            }
            ConfigureBattleMonster();
            _battleInfoLabel.Text = $"遭遇{_battleMonsterName}，输入推进战斗回合";
            _roundInfoLabel.Text = $"Round 0 | {_battleMonsterName} HP {_enemyMaxHp}";
            UpdateHpLabels();
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

        private void AdvanceBattleByInput(int inputEvents)
        {
            int rounds = Mathf.Max(1, inputEvents / Mathf.Max(1, _inputsPerBattleRoundRuntime));
            for (int i = 0; i < rounds; i++)
            {
                _battleRoundCounter++;
                _enemyHp -= _playerAttackPerRoundRuntime;
                int damageToPlayer = Mathf.Max(_enemyMinDamageRuntime, _enemyAttackPower / Mathf.Max(1, _enemyDamageDividerRuntime));
                _playerHp = Mathf.Max(1, _playerHp - damageToPlayer);

                if (_enemyHp <= 0)
                {
                    CompleteBattle();
                    return;
                }
            }

            _battleInfoLabel.Text = $"战斗中... {_battleMonsterName}";
            _roundInfoLabel.Text = $"Round {_battleRoundCounter} | {_battleMonsterName} HP {_enemyHp}";
            UpdateHpLabels();
            RefreshDebugPanel();
        }

        private void CompleteBattle()
        {
            _inBattle = false;
            _roundInfoLabel.Text = $"Round {_battleRoundCounter} | {_battleMonsterName} HP 0";
            _battleInfoLabel.Text = $"战斗胜利，结算{_battleMonsterName}战利品";

            ApplyBattleRewards();

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
            _enemyHpLabel.Visible = false;
            UpdateHpLabels();
            RefreshDebugPanel();
        }

        private void ResetTrackVisual()
        {
            _battleMonsterIndex = -1;
            _inBattle = false;
            _battleMonsterId = "";
            _battleMonsterName = "妖物";
            _enemyMaxHp = 24;
            _enemyAttackPower = 4;
            _inputsPerBattleRoundRuntime = InputsPerBattleRound;
            _enemyHp = _enemyMaxHp;
            _playerHp = _playerMaxHp;
            _battleInfoLabel.Text = "探索待命";
            _roundInfoLabel.Text = "等待输入...";

            float startX = 540.0f;
            for (int i = 0; i < _monsterMarkers.Count; i++)
            {
                _monsterMarkers[i].Visible = true;
                _monsterMarkers[i].Modulate = Colors.White;
                _monsterMarkers[i].Position = new Vector2(startX + i * MonsterRespawnSpacing, _monsterMarkers[i].Position.Y);
                AssignMonsterToMarker(i);
            }

            UpdateHpLabels();
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

        private void UpdateRealmStageLabel()
        {
            if (_playerProgressState == null)
            {
                _realmStageLabel.Text = "炼气一层";
                return;
            }

            double required = Mathf.Max(1.0f, (float)_playerProgressState.RealmExpRequired);
            double percent = _playerProgressState.RealmExp / required * 100.0;
            _realmStageLabel.Text = $"炼气{_playerProgressState.RealmLevel}层 {percent:0}%";
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
            _playerHp = Mathf.Clamp(_playerHp, 1, _playerMaxHp);
        }

        private void ConfigureBattleMonster()
        {
            _battleMonsterName = "妖物";
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

        private void ApplyBattleRewards()
        {
            bool appliedFromConfig = false;
            double lingqi = 0.0;
            double insight = 0.0;

            if (_levelConfigLoader != null && !string.IsNullOrEmpty(_battleMonsterId))
            {
                var drops = _levelConfigLoader.RollMonsterDrops(_battleMonsterId);
                ApplyResourceAndItemRewards(0.0, 0.0, drops, "battle_drop");

                if (_levelConfigLoader.TryRollMonsterSettlementReward(_battleMonsterId, out lingqi, out insight))
                {
                    ApplyResourceAndItemRewards(lingqi, insight, new Dictionary<string, int>(), "battle_settle");
                }

                appliedFromConfig = drops.Count > 0 || lingqi > 0 || insight > 0;
            }

            if (!appliedFromConfig)
            {
                ApplyResourceAndItemRewards(0.0, 0.0, new Dictionary<string, int>
                {
                    ["spirit_herb"] = 1,
                    ["lingqi_shard"] = 3
                }, "battle_fallback");
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

        public Godot.Collections.Dictionary<string, Variant> ToRuntimeDictionary()
        {
            string zoneId = _levelConfigLoader?.ActiveLevelId ?? "";
            string battleState = _inBattle ? "in_battle" : "exploring";

            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["zone_id"] = zoneId,
                ["zone_name"] = _currentZone,
                ["explore_progress"] = _exploreProgress,
                ["battle_state"] = battleState
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

            string battleState = data.ContainsKey("battle_state") ? data["battle_state"].AsString() : "exploring";
            _inBattle = battleState == "in_battle";
            if (_inBattle)
            {
                // V1 persistence restores battle flag only; combat instance resets to safe state.
                _inBattle = false;
            }

            _zoneLabel.Text = _currentZone;
            _progressBar.Value = _exploreProgress;
            _roundInfoLabel.Text = $"进度 {_exploreProgress:0.0}%";
        }

        private void AssignMonsterToMarker(int markerIndex)
        {
            if (markerIndex < 0 || markerIndex >= _monsterMarkers.Count || markerIndex >= _monsterMarkerIds.Count)
            {
                return;
            }

            string monsterId = _levelConfigLoader?.RollSpawnMonsterId() ?? string.Empty;
            _monsterMarkerIds[markerIndex] = monsterId;
            ApplyMarkerVisual(_monsterMarkers[markerIndex], monsterId);
        }

        private static void ApplyMarkerVisual(Label marker, string monsterId)
        {
            switch (monsterId)
            {
                case "monster_slime_moss":
                    marker.Text = "SL";
                    marker.Modulate = new Color(0.66f, 0.92f, 0.52f, 1.0f);
                    break;
                case "monster_bat_shadow":
                    marker.Text = "BT";
                    marker.Modulate = new Color(0.75f, 0.75f, 0.92f, 1.0f);
                    break;
                case "monster_spider_cave":
                    marker.Text = "SP";
                    marker.Modulate = new Color(0.95f, 0.56f, 0.56f, 1.0f);
                    break;
                default:
                    marker.Text = "MO";
                    marker.Modulate = Colors.White;
                    break;
            }
        }

        private void EnsureDebugPanel()
        {
            _debugPanelLabel = new Label();
            _debugPanelLabel.Name = "DebugPanelLabel";
            _debugPanelLabel.Position = new Vector2(360.0f, 4.0f);
            _debugPanelLabel.Size = new Vector2(560.0f, 84.0f);
            _debugPanelLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _debugPanelLabel.Modulate = new Color(0.95f, 0.95f, 0.75f, 0.95f);
            _debugPanelLabel.Visible = false;
            _debugPanelLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            _battleInfoLabel.GetParent().AddChild(_debugPanelLabel);
        }

        private void RefreshDebugPanel()
        {
            if (_debugPanelLabel == null || !_debugPanelVisible)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.Append($"[F8] debug | zone={_currentZone}");
            sb.Append($" | progress={_exploreProgress:0.0}%");
            sb.Append($" | monster={_battleMonsterName}({_battleMonsterId})");
            sb.Append($" | drop={_lastDropSummary}");

            if (_levelConfigLoader != null)
            {
                sb.Append('\n');
                sb.Append(_levelConfigLoader.BuildDebugSummary());
            }

            sb.Append($"\n[F9] sim200: {_lastSimulationSummary}");

            _debugPanelLabel.Text = sb.ToString();
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
