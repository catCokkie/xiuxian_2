using Godot;
using System;
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
        private const int SaveSchemaVersion = 5;
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

            config.SetValue("meta", "version", SaveSchemaVersion);
            config.SetValue("meta", "last_saved_unix", Time.GetUnixTimeFromSystem());

            WriteUiState(config);
            WriteInputState(config);
            WriteBackpackState(config);
            WriteResourceState(config);
            WritePlayerProgressState(config);
            WriteExploreRuntimeState(config);
            WriteLevelRuntimeState(config);
            WriteSystemSettings(config);

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

            int version = config.GetValue("meta", "version", 1).AsInt32();
            ReadUiState(config, version);
            ReadInputState(config);
            ReadBackpackState(config);
            ReadResourceState(config);
            ReadPlayerProgressState(config);
            ReadLevelRuntimeState(config);
            ReadExploreRuntimeState(config);
            ReadSystemSettings(config);

            if (_cloudSyncEnabled && _cloudSaveSyncService != null && _cloudSaveSyncService.TryDownloadToLocal(true))
            {
                ConfigFile refreshed = new();
                if (refreshed.Load(UnifiedStatePath) == Error.Ok)
                {
                    ReadUiState(refreshed, version);
                    ReadInputState(refreshed);
                    ReadBackpackState(refreshed);
                    ReadResourceState(refreshed);
                    ReadPlayerProgressState(refreshed);
                    ReadLevelRuntimeState(refreshed);
                    ReadExploreRuntimeState(refreshed);
                    ReadSystemSettings(refreshed);
                }
            }

            return true;
        }

        private void LoadLegacyState()
        {
            ConfigFile uiConfig = new();
            if (uiConfig.Load(LegacyUiStatePath) == Error.Ok)
            {
                ReadUiState(uiConfig, 1);
            }

            ConfigFile gameConfig = new();
            if (gameConfig.Load(LegacyGameStatePath) == Error.Ok)
            {
                ReadInputState(gameConfig);
            }
        }

        private void ReadUiState(ConfigFile config, int version)
        {
            float mainBarX = config.GetValue("ui", "main_bar_x", _mainBar.Position.X).AsSingle();
            float mainBarWidth = config.GetValue("ui", "main_bar_width", _mainBar.Size.X).AsSingle();
            _mainBar.ApplyLayout(mainBarX, mainBarWidth);

            string activeLeftTab = config.GetValue("ui", "submenu_active_left_tab", "CultivationTab").AsString();
            string activeRightTab = config.GetValue("ui", "submenu_active_right_tab", "OnlineTab").AsString();

            if (version < 2 || !config.HasSectionKey("ui", "submenu_active_left_tab"))
            {
                // Legacy single-tab key: migrate into left-page tab selection.
                activeLeftTab = config.GetValue("ui", "submenu_active_tab", "CultivationTab").AsString();
            }

            _bookTabs.RestoreActiveTabs(activeLeftTab, activeRightTab);

            bool submenuVisible = config.GetValue("ui", "submenu_visible", false).AsBool();
            _submenu.SetVisibleImmediate(submenuVisible);
        }

        private void WriteUiState(ConfigFile config)
        {
            config.SetValue("ui", "main_bar_x", _mainBar.Position.X);
            config.SetValue("ui", "main_bar_width", _mainBar.Size.X);
            config.SetValue("ui", "submenu_visible", _submenu.Visible);
            config.SetValue("ui", "submenu_active_left_tab", _bookTabs.ActiveLeftTabName);
            config.SetValue("ui", "submenu_active_right_tab", _bookTabs.ActiveRightTabName);
        }

        private void ReadInputState(ConfigFile config)
        {
            if (_activityState == null)
            {
                return;
            }

            Variant inputData = config.GetValue("input", "stats", new Godot.Collections.Dictionary<string, Variant>());
            if (inputData.VariantType == Variant.Type.Dictionary)
            {
                _activityState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)inputData);
            }

            bool hookPaused = config.GetValue("input", "hook_paused", false).AsBool();
            if (hookPaused)
            {
                GD.PushWarning("PrototypeRootController: saved hook_paused=true detected, auto-resuming input capture.");
            }
            _hookService?.SetPaused(false);
        }

        private void WriteInputState(ConfigFile config)
        {
            if (_activityState == null)
            {
                return;
            }

            config.SetValue("input", "stats", _activityState.ToDictionary());
            config.SetValue("input", "hook_paused", _hookService?.IsPaused ?? false);
        }

        private void ReadBackpackState(ConfigFile config)
        {
            if (_backpackState == null)
            {
                return;
            }

            Variant backpackData = config.GetValue("backpack", "items", new Godot.Collections.Dictionary<string, Variant>());
            if (backpackData.VariantType == Variant.Type.Dictionary)
            {
                _backpackState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)backpackData);
            }
        }

        private void WriteBackpackState(ConfigFile config)
        {
            if (_backpackState == null)
            {
                return;
            }

            config.SetValue("backpack", "items", _backpackState.ToDictionary());
        }

        private void ReadResourceState(ConfigFile config)
        {
            if (_resourceWalletState == null)
            {
                return;
            }

            Variant walletData = config.GetValue("resource", "wallet", new Godot.Collections.Dictionary<string, Variant>());
            if (walletData.VariantType == Variant.Type.Dictionary)
            {
                _resourceWalletState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)walletData);
            }
        }

        private void WriteResourceState(ConfigFile config)
        {
            if (_resourceWalletState == null)
            {
                return;
            }

            config.SetValue("resource", "wallet", _resourceWalletState.ToDictionary());
        }

        private void ReadPlayerProgressState(ConfigFile config)
        {
            if (_playerProgressState == null)
            {
                return;
            }

            Variant progressData = config.GetValue("progress", "player", new Godot.Collections.Dictionary<string, Variant>());
            if (progressData.VariantType == Variant.Type.Dictionary)
            {
                _playerProgressState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)progressData);
            }
        }

        private void WritePlayerProgressState(ConfigFile config)
        {
            if (_playerProgressState == null)
            {
                return;
            }

            config.SetValue("progress", "player", _playerProgressState.ToDictionary());
        }

        private void ReadExploreRuntimeState(ConfigFile config)
        {
            if (_exploreProgressController == null)
            {
                return;
            }

            Variant data = config.GetValue("explore", "runtime", new Godot.Collections.Dictionary<string, Variant>());
            if (data.VariantType == Variant.Type.Dictionary)
            {
                _exploreProgressController.FromRuntimeDictionary((Godot.Collections.Dictionary<string, Variant>)data);
            }
        }

        private void WriteExploreRuntimeState(ConfigFile config)
        {
            if (_exploreProgressController == null)
            {
                return;
            }

            config.SetValue("explore", "runtime", _exploreProgressController.ToRuntimeDictionary());
        }

        private void ReadLevelRuntimeState(ConfigFile config)
        {
            if (_levelConfigLoader == null)
            {
                return;
            }

            Variant data = config.GetValue("level", "runtime", new Godot.Collections.Dictionary<string, Variant>());
            if (data.VariantType == Variant.Type.Dictionary)
            {
                _levelConfigLoader.FromRuntimeDictionary((Godot.Collections.Dictionary<string, Variant>)data);
            }
        }

        private void WriteLevelRuntimeState(ConfigFile config)
        {
            if (_levelConfigLoader == null)
            {
                return;
            }

            config.SetValue("level", "runtime", _levelConfigLoader.ToRuntimeDictionary());
        }

        private void ReadSystemSettings(ConfigFile config)
        {
            Variant systemData = config.GetValue("settings", "system", new Godot.Collections.Dictionary<string, Variant>());
            if (systemData.VariantType == Variant.Type.Dictionary)
            {
                var dict = (Godot.Collections.Dictionary<string, Variant>)systemData;
                _bookTabs.FromSystemSettingsDictionary(dict);
            }

            RefreshRuntimeSettingsFromBookTabs();
        }

        private void WriteSystemSettings(ConfigFile config)
        {
            var dict = _bookTabs.ToSystemSettingsDictionary();
            config.SetValue("settings", "system", dict);
            _cloudSyncEnabled = dict.ContainsKey("cloud_sync") && dict["cloud_sync"].AsBool();
            _activitySaveMarkIntervalSeconds = ReadActivitySaveInterval(dict);
        }

        private void RefreshRuntimeSettingsFromBookTabs()
        {
            var dict = _bookTabs.ToSystemSettingsDictionary();
            _cloudSyncEnabled = dict.ContainsKey("cloud_sync") && dict["cloud_sync"].AsBool();
            _activitySaveMarkIntervalSeconds = ReadActivitySaveInterval(dict);
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

