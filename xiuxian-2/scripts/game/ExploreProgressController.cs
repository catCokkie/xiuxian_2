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
        [Export] public NodePath ValidationPanelPath = "MainBarWindow/Chrome/ConfigValidationPanel";
        [Export] public NodePath ValidationTitleLabelPath = "MainBarWindow/Chrome/ConfigValidationPanel/TitleLabel";
        [Export] public NodePath ValidationBodyLabelPath = "MainBarWindow/Chrome/ConfigValidationPanel/BodyLabel";
        [Export] public NodePath PlayerSlotTexturePath = "MainBarWindow/Chrome/BattleTrack/PlayerSlotTexture";
        [Export] public NodePath PlayerSlotLabelPath = "MainBarWindow/Chrome/BattleTrack/PlayerSlotLabel";
        [Export] public NodePath EnemySlotTexturePath = "MainBarWindow/Chrome/BattleTrack/EnemySlotTexture";
        [Export] public NodePath EnemySlotLabelPath = "MainBarWindow/Chrome/BattleTrack/EnemySlotLabel";

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
        private Panel? _validationPanel;
        private Label? _validationTitleLabel;
        private Label? _validationBodyLabel;
        private TextureRect? _playerSlotTexture;
        private Label? _playerSlotLabel;
        private TextureRect? _enemySlotTexture;
        private Label? _enemySlotLabel;
        private readonly List<Label> _monsterMarkers = new();
        private readonly List<string> _monsterMarkerIds = new();

        private InputActivityState? _activityState;
        private BackpackState? _backpackState;
        private PlayerProgressState? _playerProgressState;
        private ResourceWalletState? _resourceWalletState;
        private LevelConfigLoader? _levelConfigLoader;

        private string _currentZone = UiText.DefaultZoneName;
        private float _exploreProgress;
        private int _moveFrameCounter;
        private int _battleRoundCounter;
        private int _pendingExploreInputEvents;
        private int _pendingBattleInputEvents;
        private bool _inBattle;
        private int _battleMonsterIndex = -1;
        private string _battleMonsterId = "";
        private string _battleMonsterName = UiText.DefaultMonsterName;
        private int _enemyHp = 24;
        private int _enemyMaxHp = 24;
        private int _playerHp = 36;
        private int _playerMaxHp = 36;
        private int _enemyAttackPower = 4;
        private int _inputsPerBattleRoundRuntime = 18;
        private int _playerAttackPerRoundRuntime = 4;
        private int _enemyDamageDividerRuntime = 4;
        private int _enemyMinDamageRuntime = 1;
        private string _activeEnemyVisualMonsterId = "";
        private string _enemySlotAnimType = "none";
        private float _enemySlotAnimSpeed;
        private float _enemySlotAnimAmplitude;
        private Vector2 _enemySlotBasePosition;
        private Texture2D? _enemySlotDefaultTexture;
        private double _enemyVisualTime;
        private bool _debugPanelVisible;
        private int _validationScopeFilterIndex;
        private bool _validationOnlyActiveLevel;
        private string _lastDropSummary = "none";
        private string _lastSimulationSummary = "no simulation";
        private string _simulationLevelFilterId = "";
        private string _simulationMonsterFilterId = "";
        private static readonly string[] ValidationScopeFilters = { "all", "level", "monster", "drop_table", "config" };

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
            _validationPanel = GetNodeOrNull<Panel>(ValidationPanelPath);
            _validationTitleLabel = GetNodeOrNull<Label>(ValidationTitleLabelPath);
            _validationBodyLabel = GetNodeOrNull<Label>(ValidationBodyLabelPath);
            _playerSlotTexture = GetNodeOrNull<TextureRect>(PlayerSlotTexturePath);
            _playerSlotLabel = GetNodeOrNull<Label>(PlayerSlotLabelPath);
            _enemySlotTexture = GetNodeOrNull<TextureRect>(EnemySlotTexturePath);
            _enemySlotLabel = GetNodeOrNull<Label>(EnemySlotLabelPath);
            if (_enemySlotTexture != null)
            {
                _enemySlotDefaultTexture = _enemySlotTexture.Texture;
                _enemySlotTexture.PivotOffset = _enemySlotTexture.Size * 0.5f;
            }
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
            _simulationLevelFilterId = _levelConfigLoader?.ActiveLevelId ?? "";
            UpdateRealmStageLabel();
            ResetTrackVisual();
            RefreshDebugPanel();
            RefreshValidationPanel();

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
                    if (_validationPanel != null)
                    {
                        _validationPanel.Visible = _debugPanelVisible;
                    }
                    RefreshDebugPanel();
                    RefreshValidationPanel();
                }
                else if (keyEvent.Keycode == Key.F9)
                {
                    if (_levelConfigLoader != null)
                    {
                        _lastSimulationSummary = RunSimulationWithFilters(200);
                    }
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F10)
                {
                    if (_levelConfigLoader != null)
                    {
                        _lastSimulationSummary = RunSimulationWithFilters(1000);
                    }
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F6)
                {
                    CycleSimulationLevelFilter();
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F7)
                {
                    CycleSimulationMonsterFilter();
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F11)
                {
                    CycleValidationScopeFilter();
                    RefreshValidationPanel();
                }
                else if (keyEvent.Keycode == Key.F12)
                {
                    _validationOnlyActiveLevel = !_validationOnlyActiveLevel;
                    RefreshValidationPanel();
                }
            }
        }

        public override void _Process(double delta)
        {
            if (_enemySlotTexture == null || !_enemySlotTexture.Visible)
            {
                return;
            }

            _enemyVisualTime += delta;
            float t = (float)_enemyVisualTime;
            _enemySlotTexture.Position = _enemySlotBasePosition;
            _enemySlotTexture.Scale = Vector2.One;

            switch (_enemySlotAnimType)
            {
                case "hover":
                    _enemySlotTexture.Position += new Vector2(0.0f, Mathf.Sin(t * _enemySlotAnimSpeed) * _enemySlotAnimAmplitude);
                    break;
                case "pulse":
                    float factor = 1.0f + Mathf.Sin(t * _enemySlotAnimSpeed) * _enemySlotAnimAmplitude;
                    _enemySlotTexture.Scale = new Vector2(factor, factor);
                    break;
            }
        }

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
        }

        private void OnLevelConfigLoaded(string levelId, string levelName)
        {
            ApplyLevelConfig();
            _zoneLabel.Text = _currentZone;
            RefreshValidationPanel();
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
            _activityRateLabel.Text = UiText.BatchInputAndAp(inputEvents, apFinal);
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

            _pendingExploreInputEvents += inputEvents;
            int frames = _pendingExploreInputEvents / Mathf.Max(1, InputsPerMoveFrame);
            if (frames > 0)
            {
                _pendingExploreInputEvents -= frames * Mathf.Max(1, InputsPerMoveFrame);
                _moveFrameCounter += frames;
                float shift = frames * MonsterMovePxPerFrame;
                MoveMonsterQueue(shift);
            }

            _battleInfoLabel.Text = UiText.ExploreFrame(_moveFrameCounter);
            _battleInfoLabel.Visible = false;
            _roundInfoLabel.Text = $"{UiText.ExploreProgress(_exploreProgress)} | move {_pendingExploreInputEvents}/{Mathf.Max(1, InputsPerMoveFrame)}";

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
                _battleInfoLabel.Text = UiText.ZoneComplete;
                _battleInfoLabel.Visible = true;
                _pendingExploreInputEvents = 0;
            }

            UpdateHpLabels();
            RefreshActorSlots();
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
                RefreshDebugPanel();
                return;
            }

            _pendingBattleInputEvents -= rounds * threshold;
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

            _battleInfoLabel.Text = UiText.BattleInProgress(_battleMonsterName);
            _battleInfoLabel.Visible = true;
            _roundInfoLabel.Text = $"{UiText.BattleRound(_battleRoundCounter, _battleMonsterName, _enemyHp)} | next {_pendingBattleInputEvents}/{threshold}";
            UpdateHpLabels();
            RefreshActorSlots();
            RefreshDebugPanel();
        }

        private void CompleteBattle()
        {
            _inBattle = false;
            _roundInfoLabel.Text = UiText.BattleRound(_battleRoundCounter, _battleMonsterName, 0);
            _battleInfoLabel.Text = UiText.BattleVictory(_battleMonsterName);
            _battleInfoLabel.Visible = true;

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
            _pendingBattleInputEvents = 0;
            _enemyHpLabel.Visible = false;
            UpdateHpLabels();
            RefreshActorSlots();
            RefreshDebugPanel();
        }

        private void ResetTrackVisual()
        {
            _battleMonsterIndex = -1;
            _inBattle = false;
            _battleMonsterId = "";
            _pendingExploreInputEvents = 0;
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
            _roundInfoLabel.Text = UiText.ExploreProgress(_exploreProgress);
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
            _debugPanelLabel.Size = new Vector2(620.0f, 130.0f);
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
            sb.Append($"\nSimFilter level={(string.IsNullOrEmpty(_simulationLevelFilterId) ? "active" : _simulationLevelFilterId)}");
            sb.Append($" | monster={(string.IsNullOrEmpty(_simulationMonsterFilterId) ? "auto" : _simulationMonsterFilterId)}");

            if (_levelConfigLoader != null)
            {
                sb.Append('\n');
                sb.Append(_levelConfigLoader.BuildDebugSummary());
                sb.Append('\n');
                sb.Append(_levelConfigLoader.BuildValidationSummary(6));
            }

            sb.Append("\n[F6] level  [F7] monster  [F9] sim200  [F10] sim1000  [F11] scope  [F12] active-level");
            sb.Append($"\nSim: {_lastSimulationSummary}");

            _debugPanelLabel.Text = sb.ToString();
        }

        private void RefreshValidationPanel()
        {
            if (_validationPanel == null || _validationTitleLabel == null || _validationBodyLabel == null)
            {
                return;
            }

            _validationPanel.Visible = _debugPanelVisible;
            if (!_debugPanelVisible)
            {
                return;
            }

            if (_levelConfigLoader == null)
            {
                _validationPanel.SelfModulate = new Color(0.82f, 0.82f, 0.82f, 0.95f);
                _validationTitleLabel.Text = "配置校验：不可用";
                _validationBodyLabel.Text = "LevelConfigLoader 未加载。\n[F11] scope  [F12] 当前关卡";
                return;
            }

            var entries = _levelConfigLoader.GetValidationEntries();
            var filtered = FilterValidationEntries(entries);
            int issueCount = filtered.Count;
            int totalCount = entries.Count;
            if (issueCount <= 0)
            {
                _validationPanel.SelfModulate = new Color(0.70f, 0.90f, 0.74f, 0.95f);
                _validationTitleLabel.Text = $"配置校验：通过 ({BuildValidationFilterSummary()})";
                _validationBodyLabel.Text = "当前过滤条件下未发现配置错误。\n[F11] scope  [F12] 当前关卡";
                return;
            }

            _validationPanel.SelfModulate = new Color(0.98f, 0.72f, 0.72f, 0.96f);
            _validationTitleLabel.Text = $"配置校验：{issueCount}/{totalCount} 项 ({BuildValidationFilterSummary()})";

            int maxLines = 2;
            var sb = new StringBuilder();
            int shown = Mathf.Min(maxLines, issueCount);
            for (int i = 0; i < shown; i++)
            {
                var entry = filtered[i];
                string scope = entry.ContainsKey("scope") ? entry["scope"].AsString() : "config";
                string id = entry.ContainsKey("id") ? entry["id"].AsString() : "(unknown)";
                string field = entry.ContainsKey("field") ? entry["field"].AsString() : "(unknown)";
                string message = entry.ContainsKey("message") ? entry["message"].AsString() : "validation failed";
                string levelId = entry.ContainsKey("level_id") ? entry["level_id"].AsString() : "";
                string monsterId = entry.ContainsKey("monster_id") ? entry["monster_id"].AsString() : "";
                string dropTableId = entry.ContainsKey("drop_table_id") ? entry["drop_table_id"].AsString() : "";

                if (i > 0)
                {
                    sb.Append('\n');
                }

                sb.Append($"• {scope}/{id} {field} {message}");

                if (!string.IsNullOrEmpty(levelId) || !string.IsNullOrEmpty(monsterId) || !string.IsNullOrEmpty(dropTableId))
                {
                    sb.Append(" (");
                    bool first = true;
                    if (!string.IsNullOrEmpty(levelId))
                    {
                        sb.Append($"level_id={levelId}");
                        first = false;
                    }
                    if (!string.IsNullOrEmpty(monsterId))
                    {
                        if (!first)
                        {
                            sb.Append(", ");
                        }
                        sb.Append($"monster_id={monsterId}");
                        first = false;
                    }
                    if (!string.IsNullOrEmpty(dropTableId))
                    {
                        if (!first)
                        {
                            sb.Append(", ");
                        }
                        sb.Append($"drop_table_id={dropTableId}");
                    }
                    sb.Append(')');
                }
            }

            if (issueCount > shown)
            {
                sb.Append($"\n… 还有 {issueCount - shown} 项");
            }

            sb.Append("\n[F11] scope  [F12] 当前关卡");
            _validationBodyLabel.Text = sb.ToString();
        }

        private void CycleValidationScopeFilter()
        {
            _validationScopeFilterIndex = (_validationScopeFilterIndex + 1) % ValidationScopeFilters.Length;
        }

        private string BuildValidationFilterSummary()
        {
            string scope = ValidationScopeFilters[Mathf.Clamp(_validationScopeFilterIndex, 0, ValidationScopeFilters.Length - 1)];
            string levelScope = _validationOnlyActiveLevel ? "active-level" : "all-levels";
            return $"{scope}, {levelScope}";
        }

        private Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> FilterValidationEntries(
            Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> entries)
        {
            var result = new Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>>();
            string scopeFilter = ValidationScopeFilters[Mathf.Clamp(_validationScopeFilterIndex, 0, ValidationScopeFilters.Length - 1)];
            string activeLevelId = _levelConfigLoader?.ActiveLevelId ?? "";

            foreach (var entry in entries)
            {
                string scope = entry.ContainsKey("scope") ? entry["scope"].AsString() : "config";
                string levelId = entry.ContainsKey("level_id") ? entry["level_id"].AsString() : "";

                if (scopeFilter != "all" && scope != scopeFilter)
                {
                    continue;
                }

                if (_validationOnlyActiveLevel && !string.IsNullOrEmpty(activeLevelId))
                {
                    if (string.IsNullOrEmpty(levelId) || levelId != activeLevelId)
                    {
                        continue;
                    }
                }

                result.Add(entry);
            }

            return result;
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

