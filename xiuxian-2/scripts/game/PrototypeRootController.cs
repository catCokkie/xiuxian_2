using Godot;
using System;
using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    /// <summary>
    /// Prototype root controller: coordinates UI/input persistence and global services.
    /// </summary>
    public partial class PrototypeRootController : Control
    {
        private const string UnifiedStatePath = "user://save_state.cfg";
        private const string LegacyUiStatePath = "user://ui_state.cfg";
        private const string LegacyGameStatePath = "user://game_state.cfg";
        private const double SaveIntervalSeconds = 0.5;
        private const double DefaultActivitySaveMarkIntervalSeconds = 10.0;

        private MainBarLayoutController _mainBar = null!;
        private SubmenuWindowController _submenu = null!;
        private BookTabsController _bookTabs = null!;
        private InputActivityState? _activityState;
        private InputHookService? _hookService;
        private BackpackState? _backpackState;
        private ResourceWalletState? _resourceWalletState;
        private PlayerProgressState? _playerProgressState;
        private PlayerActionState? _playerActionState;
        private LevelConfigLoader? _levelConfigLoader;
        private ExploreProgressController? _exploreProgressController;
        private CloudSaveSyncService? _cloudSaveSyncService;
        private bool _cloudSyncEnabled;

        private bool _saveDirty;
        private double _saveCooldown;
        private double _activitySaveMarkTimer;
        private double _activitySaveMarkIntervalSeconds = DefaultActivitySaveMarkIntervalSeconds;

        public override void _Ready()
        {
            _mainBar = GetNode<MainBarLayoutController>("MainBarWindow");
            _submenu = GetNode<SubmenuWindowController>("SubmenuBookWindow");
            _bookTabs = GetNode<BookTabsController>("SubmenuBookWindow/BookFrame");

            _activityState = GetNodeOrNull<InputActivityState>("/root/InputActivityState");
            _hookService = GetNodeOrNull<InputHookService>("/root/InputHookService");
            _backpackState = GetNodeOrNull<BackpackState>("/root/BackpackState");
            _resourceWalletState = GetNodeOrNull<ResourceWalletState>("/root/ResourceWalletState");
            _playerProgressState = GetNodeOrNull<PlayerProgressState>("/root/PlayerProgressState");
            _playerActionState = GetNodeOrNull<PlayerActionState>("/root/PlayerActionState");
            _levelConfigLoader = GetNodeOrNull<LevelConfigLoader>("/root/LevelConfigLoader");
            _exploreProgressController = GetNodeOrNull<ExploreProgressController>("ExploreProgressController");
            _cloudSaveSyncService = GetNodeOrNull<CloudSaveSyncService>("/root/CloudSaveSyncService");

            _mainBar.BookButtonPressed += _submenu.ToggleVisible;
            _mainBar.LayoutChanged += (_, _) => MarkDirty();
            _submenu.VisibilityChanged += _ => MarkDirty();
            _bookTabs.ActiveTabsChanged += (_, _) =>
            {
                RefreshRuntimeSettingsFromBookTabs();
                MarkDirty();
            };

            if (_activityState != null)
            {
                _activityState.ActivityTick += OnActivityTick;
                _activityState.InputBatchTick += OnInputBatchTick;
            }
            if (_resourceWalletState != null)
            {
                _resourceWalletState.WalletChanged += OnEconomyStateChanged;
            }
            if (_playerProgressState != null)
            {
                _playerProgressState.RealmProgressChanged += OnRealmProgressChanged;
            }

            if (_hookService == null)
            {
                GD.PushWarning("PrototypeRootController: InputHookService not found at /root/InputHookService");
            }
            if (_backpackState == null)
            {
                GD.PushWarning("PrototypeRootController: BackpackState not found at /root/BackpackState");
            }
            if (_resourceWalletState == null)
            {
                GD.PushWarning("PrototypeRootController: ResourceWalletState not found at /root/ResourceWalletState");
            }
            if (_playerProgressState == null)
            {
                GD.PushWarning("PrototypeRootController: PlayerProgressState not found at /root/PlayerProgressState");
            }
            if (_cloudSaveSyncService == null)
            {
                GD.PushWarning("PrototypeRootController: CloudSaveSyncService not found at /root/CloudSaveSyncService");
            }
            if (_levelConfigLoader == null)
            {
                GD.PushWarning("PrototypeRootController: LevelConfigLoader not found at /root/LevelConfigLoader");
            }
            if (_exploreProgressController == null)
            {
                GD.PushWarning("PrototypeRootController: ExploreProgressController not found under PrototypeRoot");
            }

            CallDeferred(nameof(LoadAllState));
        }

        public override void _ExitTree()
        {
            if (_activityState != null)
            {
                _activityState.ActivityTick -= OnActivityTick;
                _activityState.InputBatchTick -= OnInputBatchTick;
            }
            if (_resourceWalletState != null)
            {
                _resourceWalletState.WalletChanged -= OnEconomyStateChanged;
            }
            if (_playerProgressState != null)
            {
                _playerProgressState.RealmProgressChanged -= OnRealmProgressChanged;
            }
        }

        public override void _Process(double delta)
        {
            if (!_saveDirty)
            {
                return;
            }

            _saveCooldown -= delta;
            if (_saveCooldown > 0.0)
            {
                return;
            }

            SaveAllState();
            _saveDirty = false;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationWMCloseRequest)
            {
                SaveAllState();
            }
        }

        private void OnActivityTick(double apThisSecond, double apFinal)
        {
            _activitySaveMarkTimer += 1.0;
            if (_activitySaveMarkTimer >= _activitySaveMarkIntervalSeconds)
            {
                _activitySaveMarkTimer = 0.0;
                MarkDirty();
            }
        }

        private void OnInputBatchTick(int inputEventsThisBatch, double apFinal)
        {
            if (inputEventsThisBatch > 0)
            {
                MarkDirty();
            }
        }

        private void OnEconomyStateChanged(double lingqi, double insight, double petAffinity)
        {
            MarkDirty();
        }

        private void OnRealmProgressChanged(int realmLevel, double realmExp, double realmExpRequired)
        {
            MarkDirty();
        }

        private void LoadAllState()
        {
            bool loaded = LoadUnifiedState();
            if (!loaded)
            {
                LoadLegacyState();
                SaveAllState();
            }

            _saveDirty = false;
            _saveCooldown = SaveIntervalSeconds;
            _activitySaveMarkTimer = 0.0;
            RefreshRuntimeSettingsFromBookTabs();
        }

        private void SaveAllState()
        {
            ConfigFile config = new();
            PrototypeRootSaveContract.Write(config, CreateSaveSnapshot(), (long)Time.GetUnixTimeFromSystem());

            Error err = config.Save(UnifiedStatePath);
            if (err != Error.Ok)
            {
                GD.PushWarning($"PrototypeRootController: failed to save unified state ({err})");
                return;
            }

            _cloudSaveSyncService?.TryUploadLocal(_cloudSyncEnabled);
        }

        private bool LoadUnifiedState()
        {
            ConfigFile config = new();
            if (config.Load(UnifiedStatePath) != Error.Ok)
            {
                return false;
            }

            ApplySaveSnapshot(PrototypeRootSaveContract.Read(config));

            if (_cloudSyncEnabled && _cloudSaveSyncService != null && _cloudSaveSyncService.TryDownloadToLocal(true))
            {
                ConfigFile refreshed = new();
                if (refreshed.Load(UnifiedStatePath) == Error.Ok)
                {
                    ApplySaveSnapshot(PrototypeRootSaveContract.Read(refreshed));
                }
            }

            return true;
        }

        private void LoadLegacyState()
        {
            ConfigFile uiConfig = new();
            if (uiConfig.Load(LegacyUiStatePath) == Error.Ok)
            {
                ApplyLegacyUiSnapshot(PrototypeRootSaveContract.Read(uiConfig).Ui);
            }

            ConfigFile gameConfig = new();
            if (gameConfig.Load(LegacyGameStatePath) == Error.Ok)
            {
                PrototypeRootSaveSnapshot snapshot = PrototypeRootSaveContract.Read(gameConfig);
                _activityState?.FromDictionary(RawVariantBridge.ToVariantDictionary(snapshot.InputStats));
                if (snapshot.HookPaused)
                {
                    GD.PushWarning("PrototypeRootController: saved hook_paused=true detected, auto-resuming input capture.");
                }

                _hookService?.SetPaused(false);
            }
        }

        private PrototypeRootSaveSnapshot CreateSaveSnapshot()
        {
            return new PrototypeRootSaveSnapshot
            {
                Ui = new PrototypeRootUiSnapshot(
                    _mainBar.Position.X,
                    _mainBar.Size.X,
                    _submenu.Visible,
                    _bookTabs.ActiveLeftTabName,
                    _bookTabs.ActiveRightTabName),
                InputStats = ReadRawDictionary(_activityState?.ToDictionary()),
                HookPaused = _hookService?.IsPaused ?? false,
                BackpackItems = ReadRawDictionary(_backpackState?.ToDictionary()),
                ResourceWallet = ReadRawDictionary(_resourceWalletState?.ToDictionary()),
                PlayerProgress = ReadRawDictionary(_playerProgressState?.ToDictionary()),
                ActionMode = ReadRawDictionary(_playerActionState?.ToDictionary()),
                ExploreRuntime = ReadRawDictionary(_exploreProgressController?.ToRuntimeDictionary()),
                LevelRuntime = ReadRawDictionary(_levelConfigLoader?.ToRuntimeDictionary()),
                SystemSettings = ReadRawDictionary(_bookTabs.ToSystemSettingsDictionary())
            };
        }

        private void ApplySaveSnapshot(PrototypeRootSaveSnapshot snapshot)
        {
            _mainBar.ApplyLayout(snapshot.Ui.MainBarX, snapshot.Ui.MainBarWidth);
            _bookTabs.RestoreActiveTabs(snapshot.Ui.ActiveLeftTab, snapshot.Ui.ActiveRightTab);
            _submenu.SetVisibleImmediate(snapshot.Ui.SubmenuVisible);

            _activityState?.FromDictionary(RawVariantBridge.ToVariantDictionary(snapshot.InputStats));
            if (snapshot.HookPaused)
            {
                GD.PushWarning("PrototypeRootController: saved hook_paused=true detected, auto-resuming input capture.");
            }

            _hookService?.SetPaused(false);
            _backpackState?.FromDictionary(RawVariantBridge.ToVariantDictionary(snapshot.BackpackItems));
            _resourceWalletState?.FromDictionary(RawVariantBridge.ToVariantDictionary(snapshot.ResourceWallet));
            _playerProgressState?.FromDictionary(RawVariantBridge.ToVariantDictionary(snapshot.PlayerProgress));
            _playerActionState?.FromDictionary(RawVariantBridge.ToVariantDictionary(snapshot.ActionMode));

            if (snapshot.LevelRuntime.Count > 0)
            {
                _levelConfigLoader?.FromRuntimeDictionary(RawVariantBridge.ToVariantDictionary(snapshot.LevelRuntime));
            }

            if (snapshot.ExploreRuntime.Count > 0)
            {
                _exploreProgressController?.FromRuntimeDictionary(RawVariantBridge.ToVariantDictionary(snapshot.ExploreRuntime));
            }

            if (snapshot.SystemSettings.Count > 0)
            {
                _bookTabs.FromSystemSettingsDictionary(RawVariantBridge.ToVariantDictionary(snapshot.SystemSettings));
            }

            RefreshRuntimeSettingsFromBookTabs();
        }

        private void ApplyLegacyUiSnapshot(PrototypeRootUiSnapshot ui)
        {
            _mainBar.ApplyLayout(ui.MainBarX, ui.MainBarWidth);
            _bookTabs.RestoreActiveTabs(ui.ActiveLeftTab, _bookTabs.ActiveRightTabName);
            _submenu.SetVisibleImmediate(ui.SubmenuVisible);
        }

        private static Dictionary<string, object?> ReadRawDictionary(Godot.Collections.Dictionary<string, Variant>? data)
        {
            return data == null
                ? new Dictionary<string, object?>(StringComparer.Ordinal)
                : RawVariantBridge.ToRawDictionary(data);
        }


        private void RefreshRuntimeSettingsFromBookTabs()
        {
            var dict = _bookTabs.ToSystemSettingsDictionary();
            _cloudSyncEnabled = dict.ContainsKey("cloud_sync") && dict["cloud_sync"].AsBool();
            _activitySaveMarkIntervalSeconds = ReadActivitySaveInterval(dict);
            bool showValidationPanel = !dict.ContainsKey("show_validation_panel") || dict["show_validation_panel"].AsBool();
            _exploreProgressController?.SetValidationPanelEnabled(showValidationPanel);
            bool globalDebugOverlay = dict.ContainsKey("global_debug_overlay") && dict["global_debug_overlay"].AsBool();
            _exploreProgressController?.SetGlobalDebugOverlayEnabled(globalDebugOverlay);
        }

        private static double ReadActivitySaveInterval(Godot.Collections.Dictionary<string, Variant> dict)
        {
            if (!dict.ContainsKey("auto_save_interval_sec"))
            {
                return DefaultActivitySaveMarkIntervalSeconds;
            }

            int value = dict["auto_save_interval_sec"].AsInt32();
            return Math.Max(1, value);
        }

        private void MarkDirty()
        {
            _saveDirty = true;
            _saveCooldown = SaveIntervalSeconds;
        }
    }
}
