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
        [Export] public NodePath ProgressBarPath = "../MainBarWindow/Chrome/ExploreProgressBar";
        [Export] public NodePath CultivationProgressBarPath = "../MainBarWindow/Chrome/CultivationProgressBar";
        [Export] public NodePath BreakthroughButtonPath = "../MainBarWindow/Chrome/BreakthroughButton";
        [Export] public NodePath CultivationLabelPath = "../MainBarWindow/Chrome/CultivationLabel";
        [Export] public NodePath ZoneLabelPath = "../MainBarWindow/Chrome/ZoneLabel";
        [Export] public NodePath ActivityRateLabelPath = "../MainBarWindow/Chrome/ActivityRateLabel";
        [Export] public NodePath MoveDebugLabelPath = "../MainBarWindow/Chrome/MoveDebugLabel";
        [Export] public NodePath RealmStageLabelPath = "../MainBarWindow/Chrome/RealmStageLabel";

        [Export] public NodePath BattleInfoLabelPath = "../MainBarWindow/Chrome/BattleTrack/BattleInfoLabel";
        [Export] public NodePath RoundInfoLabelPath = "../MainBarWindow/Chrome/BattleTrack/RoundInfoLabel";
        [Export] public NodePath PlayerMarkerPath = "../MainBarWindow/Chrome/BattleTrack/PlayerMarker";
        [Export] public NodePath PlayerHpLabelPath = "../MainBarWindow/Chrome/BattleTrack/PlayerHpLabel";
        [Export] public NodePath EnemyHpLabelPath = "../MainBarWindow/Chrome/BattleTrack/EnemyHpLabel";
        [Export] public NodePath ValidationPanelPath = "../MainBarWindow/Chrome/ConfigValidationPanel";
        [Export] public NodePath ValidationTitleLabelPath = "../MainBarWindow/Chrome/ConfigValidationPanel/TitleLabel";
        [Export] public NodePath ValidationBodyLabelPath = "../MainBarWindow/Chrome/ConfigValidationPanel/BodyLabel";
        [Export] public NodePath ActionModeOptionButtonPath = "../MainBarWindow/Chrome/ActionModeOptionButton";
        [Export] public NodePath LevelOptionButtonPath = "../MainBarWindow/Chrome/LevelOptionButton";
        [Export] public NodePath PlayerSlotTexturePath = "../MainBarWindow/Chrome/BattleTrack/PlayerSlotTexture";
        [Export] public NodePath PlayerSlotLabelPath = "../MainBarWindow/Chrome/BattleTrack/PlayerSlotLabel";
        [Export] public NodePath EnemySlotTexturePath = "../MainBarWindow/Chrome/BattleTrack/EnemySlotTexture";
        [Export] public NodePath EnemySlotLabelPath = "../MainBarWindow/Chrome/BattleTrack/EnemySlotLabel";

        [Export] public NodePath ActivityStatePath = "/root/InputActivityState";
        [Export] public NodePath BackpackStatePath = "/root/BackpackState";
        [Export] public NodePath PlayerProgressPath = "/root/PlayerProgressState";
        [Export] public NodePath ResourceWalletPath = "/root/ResourceWalletState";
        [Export] public NodePath LevelConfigLoaderPath = "/root/LevelConfigLoader";
        [Export] public NodePath ActionStatePath = "/root/PlayerActionState";

        // Explore progress is input-event driven (percent per input event), not AP-driven.
        [Export] public float ProgressPerInput = 0.02f;
        [Export] public int InputsPerMoveFrame = 4;
        [Export] public int InputsPerBattleRound = 18;
        [Export] public float MaxProgress = 100.0f;

        [Export] public float MonsterMovePxPerFrame = 3.8f;
        [Export] public float MonsterRespawnSpacing = 110.0f;
        [Export] public float BattleTriggerX = 220.0f;

        private ProgressBar _progressBar = null!;
        private ProgressBar? _cultivationProgressBar;
        private Button? _breakthroughButton;
        private Label? _cultivationLabel;
        private Label _zoneLabel = null!;
        private Label _activityRateLabel = null!;
        private Label? _moveDebugLabel;
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
        private OptionButton? _actionModeOptionButton;
        private OptionButton? _levelOptionButton;
        private TextureRect? _playerSlotTexture;
        private Label? _playerSlotLabel;
        private TextureRect? _enemySlotTexture;
        private Label? _enemySlotLabel;
        private readonly List<Label> _monsterMarkers = new();
        private readonly List<string> _monsterMarkerIds = new();
        private readonly List<TextureRect> _monsterSlots = new();
        private readonly List<int> _monsterMoveInputPending = new();
        private readonly List<int> _monsterMoveInputThreshold = new();

        private InputActivityState? _activityState;
        private BackpackState? _backpackState;
        private PlayerProgressState? _playerProgressState;
        private ResourceWalletState? _resourceWalletState;
        private LevelConfigLoader? _levelConfigLoader;
        private PlayerActionState? _actionState;

        private string _currentZone = UiText.DefaultZoneName;
        private float _exploreProgress;
        private int _moveFrameCounter;
        private int _queueMoveInputPending;
        private int _battleRoundCounter;
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
        private bool _globalDebugOverlayEnabled;
        private bool _validationPanelEnabled = true;
        private int _validationScopeFilterIndex;
        private bool _validationOnlyActiveLevel;
        private bool _syncingActionModeOption;
        private bool _syncingLevelOption;
        private string _lastDropSummary = "none";
        private string _lastSimulationSummary = "no simulation";
        private string _simulationLevelFilterId = "";
        private string _simulationMonsterFilterId = "";
        private static readonly string[] ValidationScopeFilters = { "all", "level", "monster", "drop_table", "config" };

        public override void _Ready()
        {
            _progressBar = GetNode<ProgressBar>(ProgressBarPath);
            _cultivationProgressBar = GetNodeOrNull<ProgressBar>(CultivationProgressBarPath);
            _breakthroughButton = GetNodeOrNull<Button>(BreakthroughButtonPath);
            _cultivationLabel = GetNodeOrNull<Label>(CultivationLabelPath);
            _zoneLabel = GetNode<Label>(ZoneLabelPath);
            _activityRateLabel = GetNode<Label>(ActivityRateLabelPath);
            _moveDebugLabel = GetNodeOrNull<Label>(MoveDebugLabelPath);
            _realmStageLabel = GetNode<Label>(RealmStageLabelPath);
            _battleInfoLabel = GetNode<Label>(BattleInfoLabelPath);
            _roundInfoLabel = GetNode<Label>(RoundInfoLabelPath);
            _playerMarker = GetNode<Label>(PlayerMarkerPath);
            _playerHpLabel = GetNode<Label>(PlayerHpLabelPath);
            _enemyHpLabel = GetNode<Label>(EnemyHpLabelPath);
            _validationPanel = GetNodeOrNull<Panel>(ValidationPanelPath);
            _validationTitleLabel = GetNodeOrNull<Label>(ValidationTitleLabelPath);
            _validationBodyLabel = GetNodeOrNull<Label>(ValidationBodyLabelPath);
            _actionModeOptionButton = GetNodeOrNull<OptionButton>(ActionModeOptionButtonPath);
            _levelOptionButton = GetNodeOrNull<OptionButton>(LevelOptionButtonPath);
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
            CacheMonsterSlots();

            _activityState = GetNodeOrNull<InputActivityState>(ActivityStatePath);
            _backpackState = GetNodeOrNull<BackpackState>(BackpackStatePath);
            _playerProgressState = GetNodeOrNull<PlayerProgressState>(PlayerProgressPath);
            _resourceWalletState = GetNodeOrNull<ResourceWalletState>(ResourceWalletPath);
            _levelConfigLoader = GetNodeOrNull<LevelConfigLoader>(LevelConfigLoaderPath);
            _actionState = GetNodeOrNull<PlayerActionState>(ActionStatePath);

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
            if (_cultivationProgressBar != null)
            {
                _cultivationProgressBar.MaxValue = 100.0;
            }
            if (_breakthroughButton != null)
            {
                _breakthroughButton.Pressed += OnBreakthroughPressed;
            }
            ConfigureActionModeOptionButton();
            ConfigureLevelOptionButton();
            _simulationLevelFilterId = _levelConfigLoader?.ActiveLevelId ?? "";
            UpdateRealmStageLabel();
            RefreshCultivationPanel();
            ResetTrackVisual();
            RefreshDebugPanel();
            RefreshValidationPanel();
            RefreshMoveDebugLabel();
            ApplyGlobalDebugOverlayVisibility();
            RefreshActionModeOptionButton();
            RefreshLevelOptionButton();

            if (_playerProgressState != null)
            {
                _playerProgressState.RealmProgressChanged += OnRealmProgressChanged;
            }
            if (_actionState != null)
            {
                _actionState.ModeChanged += OnActionModeChanged;
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
            if (_actionState != null)
            {
                _actionState.ModeChanged -= OnActionModeChanged;
            }
            if (_breakthroughButton != null)
            {
                _breakthroughButton.Pressed -= OnBreakthroughPressed;
            }
            if (_actionModeOptionButton != null)
            {
                _actionModeOptionButton.ItemSelected -= OnActionModeOptionSelected;
            }
            if (_levelOptionButton != null)
            {
                _levelOptionButton.ItemSelected -= OnLevelOptionSelected;
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
                else if (keyEvent.Keycode == Key.F5)
                {
                    if (_levelConfigLoader != null && _levelConfigLoader.TrySetNextUnlockedLevelAsActive())
                    {
                        ApplyLevelConfig();
                        _zoneLabel.Text = _currentZone;
                        _exploreProgress = 0.0f;
                        _progressBar.Value = 0.0f;
                        ResetTrackVisual();
                        RefreshLevelOptionButton();
                    }
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F4)
                {
                    _actionState?.ToggleMode();
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
            _actionModeOptionButton.ItemSelected -= OnActionModeOptionSelected;
            _actionModeOptionButton.ItemSelected += OnActionModeOptionSelected;
        }

        private void ConfigureLevelOptionButton()
        {
            if (_levelOptionButton == null)
            {
                return;
            }

            _levelOptionButton.ItemSelected -= OnLevelOptionSelected;
            _levelOptionButton.ItemSelected += OnLevelOptionSelected;
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
            _syncingLevelOption = false;
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

        private void OnInputBatchTick(int inputEvents, double apFinal)
        {
            // AP is displayed for resource settlement transparency; progress uses inputEvents only.
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
            // Core rule: explore progress is computed directly from input event count.
            _exploreProgress = Mathf.Min(_exploreProgress + inputEvents * ProgressPerInput, MaxProgress);
            _progressBar.Value = _exploreProgress;

            int frames = MoveMonsterQueueByInputs(inputEvents);
            _moveFrameCounter += frames;

            _battleInfoLabel.Text = UiText.ExploreFrame(_moveFrameCounter);
            _battleInfoLabel.Visible = false;
            _roundInfoLabel.Text = $"{UiText.ExploreProgress(_exploreProgress)} | {BuildFrontMoveStatus()}";
            RefreshMoveDebugLabel();

            if (_exploreProgress >= MaxProgress)
            {
                _exploreProgress = 0.0f;
                _progressBar.Value = 0.0f;
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

            // Base conveyor movement: all monsters advance together.
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

            // Front bonus movement: front monster still follows per-monster threshold.
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

            // Respawn any marker that drifted beyond the left bound back to queue tail.
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
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
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
                ["monster_marker_states"] = markerStates
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
            string actionMode = IsDungeonMode() ? "dungeon" : "cultivation";
            sb.Append($"[F8] debug | zone={_currentZone}");
            sb.Append($" | mode={actionMode}");
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
                sb.Append('\n');
                sb.Append(_levelConfigLoader.BuildLevelPreviewSummary(8));
            }

            sb.Append("\n[F4] toggle main action  [F5] switch unlocked level  [F6] sim-level  [F7] sim-monster  [F9] sim200  [F10] sim1000  [F11] scope  [F12] active-level");
            sb.Append($"\nSim: {_lastSimulationSummary}");

            _debugPanelLabel.Text = sb.ToString();
        }

        private bool IsDungeonMode()
        {
            return _actionState == null || _actionState.IsDungeonMode;
        }

        private void RefreshValidationPanel()
        {
            if (_validationPanel == null || _validationTitleLabel == null || _validationBodyLabel == null)
            {
                return;
            }

            _validationPanel.Visible = _validationPanelEnabled;
            if (!_validationPanelEnabled)
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

        public void SetValidationPanelEnabled(bool enabled)
        {
            _validationPanelEnabled = enabled;
            RefreshValidationPanel();
        }

        public void SetGlobalDebugOverlayEnabled(bool enabled)
        {
            _globalDebugOverlayEnabled = enabled;
            ApplyGlobalDebugOverlayVisibility();
            RefreshMoveDebugLabel();
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

