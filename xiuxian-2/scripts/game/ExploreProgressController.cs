using Godot;
using System.Collections.Generic;
using System;
using System.Text;
using Xiuxian.Scripts.Core;
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
        [Signal]
        public delegate void RecentBattleLogsChangedEventHandler();

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
        private RichTextLabel? _validationBodyLabel;
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
        private bool _actionModeOptionBound;
        private bool _levelOptionBound;
        private string _lastDropSummary = "none";
        private string _lastSimulationSummary = "no simulation";
        private string _simulationLevelFilterId = "";
        private string _simulationMonsterFilterId = "";
        private const int MaxRecentBattleLogs = 10;
        private readonly List<BattleLogEntry> _recentBattleLogs = new();
        private static readonly string[] ValidationScopeFilters = { "all", "level", "monster", "drop_table", "config" };

        private sealed class BattleLogEntry
        {
            public long TimestampUnix;
            public string Result = "victory";
            public string MonsterId = "";
            public string MonsterName = UiText.DefaultMonsterName;
            public double Lingqi;
            public double Insight;
            public Dictionary<string, int> Items = new();
        }

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
            _validationBodyLabel = GetNodeOrNull<RichTextLabel>(ValidationBodyLabelPath);
            if (_validationBodyLabel != null)
            {
                _validationBodyLabel.BbcodeEnabled = true;
            }
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
                if (_actionModeOptionBound)
                {
                    _actionModeOptionButton.ItemSelected -= OnActionModeOptionSelected;
                    _actionModeOptionBound = false;
                }
            }
            if (_levelOptionButton != null)
            {
                if (_levelOptionBound)
                {
                    _levelOptionButton.ItemSelected -= OnLevelOptionSelected;
                    _levelOptionBound = false;
                }
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
                    TrySelectNextUnlockedLevel();
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F4)
                {
                    ToggleMainActionMode();
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

    }
}
