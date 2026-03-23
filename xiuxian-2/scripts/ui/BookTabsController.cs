using Godot;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

public partial class BookTabsController : Control
{
    [Signal]
    public delegate void ActiveTabsChangedEventHandler(string leftTabName, string rightTabName);

    private readonly Dictionary<string, string> _leftTabContentMap = new()
    {
        { "CultivationTab", UiText.CultivationTemplate },
        { "BattleLogTab", UiText.BattleLogEmpty },
        { "EquipmentTab", UiText.EquipmentTemplate },
        { "StatsTab", UiText.StatsTemplate },
    };

    private readonly Dictionary<string, string> _rightTabContentMap = new()
    {
        { "BugTab", UiText.BugTemplate },
        { "SettingsTab", UiText.SettingsTitle },
    };

    private RichTextLabel _leftContentLabel = null!;
    private Label _leftTitleLabel = null!;
    private Label _coinLabel = null!;
    private Control _leftPage = null!;
    private Control _rightPage = null!;
    private Button _closeButton = null!;

    private HBoxContainer _settingsNavRoot = null!;
    private VBoxContainer _settingsSystemRoot = null!;
    private VBoxContainer _settingsDisplayRoot = null!;
    private VBoxContainer _settingsProgressRoot = null!;
    private VBoxContainer _settingsActionRoot = null!;
    private VBoxContainer _bugFeedbackRoot = null!;
    private VBoxContainer _equipmentRoot = null!;
    private Button _settingsSystemBtn = null!;
    private Button _settingsDisplayBtn = null!;
    private Button _settingsProgressBtn = null!;

    private OptionButton _languageOption = null!;
    private CheckButton _keepOnTopCheck = null!;
    private CheckButton _taskbarIconCheck = null!;
    private CheckButton _vsyncCheck = null!;
    private OptionButton _fpsOption = null!;

    private OptionButton _resolutionOption = null!;
    private CheckButton _showControlMarkerCheck = null!;
    private CheckButton _showValidationPanelCheck = null!;
    private Button _openLogFolderButton = null!;
    private OptionButton _gameScaleOption = null!;
    private OptionButton _uiScaleOption = null!;
    private OptionButton _autoSaveIntervalOption = null!;
    private CheckButton _cloudSyncCheck = null!;
    private CheckButton _milestoneTipsCheck = null!;
    private CheckButton _globalDebugOverlayCheck = null!;
    private TextEdit _bugFeedbackInput = null!;
    private Label _bugFeedbackStatusLabel = null!;
    private RichTextLabel _equipmentContentLabel = null!;

    private Tween? _leftTween;
    private InputActivityState? _activityState;
    private BackpackState? _backpackState;
    private ResourceWalletState? _resourceWalletState;
    private PlayerProgressState? _playerProgressState;
    private EquippedItemsState? _equippedItemsState;
    private LevelConfigLoader? _levelConfigLoader;
    private ExploreProgressController? _exploreProgressController;

    public string ActiveLeftTabName { get; private set; } = "CultivationTab";
    public string ActiveRightTabName { get; private set; } = "BugTab";
    private bool _isShowingRightTab;

    private string _activeSettingsSection = "system";
    private bool _isApplyingSettingsUi;

    private readonly Godot.Collections.Dictionary<string, Variant> _settings = new()
    {
        ["language"] = "zh-CN",
        ["keep_on_top"] = false,
        ["taskbar_icon"] = true,
        ["startup_animation"] = true,
        ["admin_mode"] = false,
        ["handwriting_support"] = false,
        ["vsync"] = true,
        ["max_fps"] = 60,
        ["resolution"] = "1600x900",
        ["show_control_markers"] = true,
        ["show_validation_panel"] = true,
        ["game_scale"] = 1.33,
        ["ui_scale"] = 1.0,
        ["auto_save_interval_sec"] = 10,
        ["cloud_sync"] = false,
        ["milestone_tips"] = true,
        ["global_debug_overlay"] = false,
    };

    public override void _Ready()
    {
        _leftContentLabel = GetNode<RichTextLabel>("SpreadBody/LeftPage/LeftContentLabel");
        _leftTitleLabel = GetNode<Label>("SpreadBody/LeftPage/LeftTitle");
        _coinLabel = GetNode<Label>("BottomBar/CoinLabel");
        _leftPage = GetNode<Control>("SpreadBody/LeftPage");
        _rightPage = GetNode<Control>("SpreadBody/RightPage");
        _closeButton = GetNode<Button>("CloseButton");
        _closeButton.Pressed += CloseWindow;
        _activityState = GetNodeOrNull<InputActivityState>("/root/InputActivityState");
        _backpackState = GetNodeOrNull<BackpackState>("/root/BackpackState");
        _resourceWalletState = GetNodeOrNull<ResourceWalletState>("/root/ResourceWalletState");
        _playerProgressState = GetNodeOrNull<PlayerProgressState>("/root/PlayerProgressState");
        _equippedItemsState = GetNodeOrNull<EquippedItemsState>("/root/EquippedItemsState");
        _levelConfigLoader = GetNodeOrNull<LevelConfigLoader>("/root/LevelConfigLoader");
        _exploreProgressController = GetNodeOrNull<ExploreProgressController>("../../ExploreProgressController");

        if (_activityState != null)
        {
            _activityState.ActivityTick += OnActivityTick;
        }
        if (_backpackState != null)
        {
            _backpackState.InventoryChanged += OnInventoryChanged;
            _backpackState.EquipmentInventoryChanged += OnEquipmentInventoryChanged;
        }
        if (_resourceWalletState != null)
        {
            _resourceWalletState.WalletChanged += OnWalletChanged;
        }
        if (_playerProgressState != null)
        {
            _playerProgressState.RealmProgressChanged += OnRealmProgressChanged;
        }
        if (_equippedItemsState != null)
        {
            _equippedItemsState.EquippedItemsChanged += OnEquippedItemsChanged;
        }

        ApplyStaticTexts();

        BuildSettingsUi();
        ApplySettingsRuntime();
        UpdateSettingsControlsFromState();
        UpdateSettingsUiVisibility();

        BindButtons(_leftTabContentMap.Keys, "TopStrip/LeftTabs", SetActiveLeftTab);
        BindButtons(_rightTabContentMap.Keys, "TopStrip/RightTabs", SetActiveRightTab);

        RestoreActiveTabs(ActiveLeftTabName, ActiveRightTabName);
        RefreshCoinLabel();
        RefreshDynamicTabContent();
    }

    public override void _ExitTree()
    {
        if (_activityState != null)
        {
            _activityState.ActivityTick -= OnActivityTick;
        }
        if (_backpackState != null)
        {
            _backpackState.InventoryChanged -= OnInventoryChanged;
            _backpackState.EquipmentInventoryChanged -= OnEquipmentInventoryChanged;
        }
        if (_resourceWalletState != null)
        {
            _resourceWalletState.WalletChanged -= OnWalletChanged;
        }
        if (_playerProgressState != null)
        {
            _playerProgressState.RealmProgressChanged -= OnRealmProgressChanged;
        }
        if (_equippedItemsState != null)
        {
            _equippedItemsState.EquippedItemsChanged -= OnEquippedItemsChanged;
        }
    }

    public void SetSpiritStone(int amount)
    {
        _coinLabel.Text = UiText.SpiritStone(amount);
    }

    public void RestoreActiveTabs(string leftTabName, string rightTabName)
    {
        if (!_leftTabContentMap.ContainsKey(leftTabName))
        {
            leftTabName = "CultivationTab";
        }

        if (!_rightTabContentMap.ContainsKey(rightTabName))
        {
            rightTabName = "BugTab";
        }

        ActiveLeftTabName = leftTabName;
        ActiveRightTabName = rightTabName;
        _isShowingRightTab = false;

        SyncButtons("TopStrip/LeftTabs", _leftTabContentMap.Keys, ActiveLeftTabName);
        SyncButtons("TopStrip/RightTabs", _rightTabContentMap.Keys, ActiveRightTabName);
        RefreshCurrentPageContent();
    }

    public Godot.Collections.Dictionary<string, Variant> ToSystemSettingsDictionary()
    {
        return new Godot.Collections.Dictionary<string, Variant>(_settings);
    }

    public void FromSystemSettingsDictionary(Godot.Collections.Dictionary<string, Variant> data)
    {
        foreach (string key in _settings.Keys)
        {
            if (data.ContainsKey(key))
            {
                _settings[key] = data[key];
            }
        }

        ApplySettingsRuntime();
        UpdateSettingsControlsFromState();
    }

    private void SetActiveLeftTab(string tabName)
    {
        if (!_leftTabContentMap.ContainsKey(tabName))
        {
            return;
        }

        // Leaving settings/right-page mode should immediately return to left tabs.
        if (ActiveRightTabName == "SettingsTab")
        {
            ActiveRightTabName = "BugTab";
        }

        ActiveLeftTabName = tabName;
        _isShowingRightTab = false;
        SyncButtons("TopStrip/LeftTabs", _leftTabContentMap.Keys, ActiveLeftTabName);
        SyncButtons("TopStrip/RightTabs", _rightTabContentMap.Keys, ActiveRightTabName);
        RefreshCurrentPageContent();
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnActivityTick(double apThisSecond, double apFinal)
    {
        RefreshDynamicTabContent();
    }

    private void OnInventoryChanged(string itemId, int amount, int newTotal)
    {
        RefreshDynamicTabContent();
    }

    private void OnEquipmentInventoryChanged()
    {
        RefreshDynamicTabContent();
    }

    private void OnWalletChanged(double lingqi, double insight, double petAffinity)
    {
        RefreshCoinLabel();
        RefreshDynamicTabContent();
    }

    private void OnRealmProgressChanged(int realmLevel, double realmExp, double realmExpRequired)
    {
        RefreshDynamicTabContent();
    }

    private void OnEquippedItemsChanged()
    {
        RefreshDynamicTabContent();
    }

    private void RefreshDynamicTabContent()
    {
        if (ActiveRightTabName == "SettingsTab")
        {
            return;
        }

        if (!_isShowingRightTab && (ActiveLeftTabName == "CultivationTab" || ActiveLeftTabName == "StatsTab" || ActiveLeftTabName == "BattleLogTab" || ActiveLeftTabName == "EquipmentTab"))
        {
            string content = GetLeftTabContent(ActiveLeftTabName);
            if (ActiveLeftTabName == "EquipmentTab")
            {
                _equipmentContentLabel.Text = content;
            }
            else
            {
                _leftContentLabel.Text = content;
            }
        }
    }

    private string GetLeftTabContent(string tabName)
    {
        return tabName switch
        {
            "CultivationTab" => BuildCultivationOverviewText(),
            "BattleLogTab" => BuildBattleLogText(),
            "EquipmentTab" => BuildEquipmentOverviewText(),
            "StatsTab" => BuildStatsOverviewText(),
            _ => _leftTabContentMap[tabName]
        };
    }

    private string BuildEquipmentOverviewText()
    {
        if (_playerProgressState == null || _levelConfigLoader == null || _equippedItemsState == null || _backpackState == null)
        {
            return _leftTabContentMap["EquipmentTab"];
        }

        EquipmentStatProfile[] equippedProfiles = _equippedItemsState.GetEquippedProfiles();
        if (equippedProfiles.Length == 0)
        {
            return UiText.EquipmentEmpty;
        }

        CharacterStatBlock baseStats = PlayerBaseStatRules.BuildBaseStats(
            _playerProgressState.RealmLevel,
            _levelConfigLoader.PlayerBaseHp,
            _levelConfigLoader.PlayerAttackPerRound);
        CharacterStatBlock finalStats = CharacterStatRules.BuildFinalStats(baseStats, equippedProfiles);

        StringBuilder sb = new();
        sb.AppendLine(UiText.LeftTabEquipment);
        sb.AppendLine($"当前已装备 {equippedProfiles.Length} 件");
        sb.AppendLine($"基础属性：HP {baseStats.MaxHp} / 攻 {baseStats.Attack} / 防 {baseStats.Defense}");
        sb.AppendLine($"装备后：HP {finalStats.MaxHp} / 攻 {finalStats.Attack} / 防 {finalStats.Defense}");

        foreach (EquipmentStatProfile profile in equippedProfiles)
        {
            sb.AppendLine();
            sb.AppendLine($"[{BuildSlotLabel(profile.Slot)}] {profile.DisplayName}");
            sb.AppendLine(BuildModifierSummary(profile.Modifier));
        }

        EquipmentStatProfile[] backpackProfiles = _backpackState.GetEquipmentProfiles();
        sb.AppendLine();
        sb.AppendLine($"背包装备 {backpackProfiles.Length} 件");
        foreach (EquipmentStatProfile profile in backpackProfiles)
        {
            sb.AppendLine($"- [{BuildSlotLabel(profile.Slot)}] {profile.DisplayName} | {BuildModifierSummary(profile.Modifier)}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildSlotLabel(EquipmentSlotType slot)
    {
        return slot switch
        {
            EquipmentSlotType.Weapon => "武器",
            EquipmentSlotType.Armor => "护具",
            EquipmentSlotType.Accessory => "饰品",
            _ => "装备"
        };
    }

    private static string BuildModifierSummary(CharacterStatModifier modifier)
    {
        List<string> parts = new();
        if (modifier.MaxHpFlat != 0) parts.Add($"HP+{modifier.MaxHpFlat}");
        if (modifier.AttackFlat != 0) parts.Add($"攻击+{modifier.AttackFlat}");
        if (modifier.DefenseFlat != 0) parts.Add($"防御+{modifier.DefenseFlat}");
        if (modifier.SpeedFlat != 0) parts.Add($"速度+{modifier.SpeedFlat}");
        if (modifier.CritChanceDelta != 0.0) parts.Add($"暴击+{modifier.CritChanceDelta:P0}");
        if (modifier.CritDamageDelta != 0.0) parts.Add($"暴伤+{modifier.CritDamageDelta:0.##}");
        return parts.Count > 0 ? string.Join(" | ", parts) : "当前无额外词条";
    }

    private void EquipFromBackpack(EquipmentSlotType slot)
    {
        if (_equippedItemsState == null || _backpackState == null)
        {
            return;
        }

        if (!_backpackState.TryTakeEquipmentBySlot(slot, out EquipmentStatProfile nextProfile))
        {
            return;
        }

        if (_equippedItemsState.TryEquipReplacing(nextProfile, out EquipmentStatProfile replacedProfile))
        {
            _backpackState.AddEquipment(replacedProfile with { IsEquipped = false });
        }
    }

    private string BuildBattleLogText()
    {
        if (_exploreProgressController == null)
        {
            return UiText.BattleLogEmpty;
        }

        return _exploreProgressController.BuildRecentBattleLogText();
    }

    private string BuildCultivationOverviewText()
    {
        if (_playerProgressState == null || _resourceWalletState == null)
        {
            return _leftTabContentMap["CultivationTab"];
        }

        double expRequired = _playerProgressState.RealmExpRequired;
        double expPercent = expRequired > 0.0 ? _playerProgressState.RealmExp / expRequired * 100.0 : 0.0;

        return
            UiText.CultivationOverview(
                _playerProgressState.RealmLevel,
                _playerProgressState.RealmExp,
                expRequired,
                expPercent,
                _resourceWalletState.Lingqi,
                _resourceWalletState.Insight,
                _resourceWalletState.PetAffinity);
    }

    private string BuildStatsOverviewText()
    {
        if (_activityState == null)
        {
            return _leftTabContentMap["StatsTab"];
        }

        int herbCount = _backpackState?.GetItemCount("spirit_herb") ?? 0;
        int shardCount = _backpackState?.GetItemCount("lingqi_shard") ?? 0;

        return
            UiText.StatsOverview(
                _activityState.TotalKeyDownCount,
                _activityState.TotalMouseClickCount,
                _activityState.TotalMouseScrollSteps,
                _activityState.TotalMouseMoveDistancePx,
                _activityState.ApAccumulator,
                herbCount,
                shardCount);
    }

    private void RefreshCoinLabel()
    {
        if (_resourceWalletState == null)
        {
            return;
        }

        SetSpiritStone((int)_resourceWalletState.Lingqi);
    }

    private void SetActiveRightTab(string tabName)
    {
        if (!_rightTabContentMap.ContainsKey(tabName))
        {
            return;
        }

        ActiveRightTabName = tabName;
        _isShowingRightTab = tabName != "SettingsTab";
        SyncButtons("TopStrip/RightTabs", _rightTabContentMap.Keys, ActiveRightTabName);
        RefreshCurrentPageContent();
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void BuildSettingsUi()
    {
        BuildEquipmentUi();
        BuildBugFeedbackUi();

        _settingsNavRoot = new HBoxContainer();
        _settingsNavRoot.Name = "SettingsNavRoot";
        _settingsNavRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _settingsNavRoot.OffsetLeft = 20.0f;
        _settingsNavRoot.OffsetTop = 36.0f;
        _settingsNavRoot.OffsetRight = -20.0f;
        _settingsNavRoot.OffsetBottom = -330.0f;
        _settingsNavRoot.AddThemeConstantOverride("separation", 8);
        _leftPage.AddChild(_settingsNavRoot);

        _settingsSystemBtn = CreateSettingsSectionButton(UiText.SystemSection, "system");
        _settingsDisplayBtn = CreateSettingsSectionButton(UiText.DisplaySection, "display");
        _settingsProgressBtn = CreateSettingsSectionButton(UiText.ProgressSection, "progress");

        _settingsActionRoot = new VBoxContainer();
        _settingsActionRoot.Name = "SettingsActionRoot";
        _settingsActionRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _settingsActionRoot.OffsetLeft = 20.0f;
        _settingsActionRoot.OffsetTop = 286.0f;
        _settingsActionRoot.OffsetRight = -20.0f;
        _settingsActionRoot.OffsetBottom = -12.0f;
        _settingsActionRoot.AddThemeConstantOverride("separation", 8);
        _leftPage.AddChild(_settingsActionRoot);

        Button resetButton = new();
        resetButton.Text = UiText.ResetAndApply;
        resetButton.Pressed += ResetSettings;
        _settingsActionRoot.AddChild(resetButton);

        Button quitButton = new();
        quitButton.Text = UiText.Quit;
        quitButton.Pressed += () => GetTree().Quit();
        _settingsActionRoot.AddChild(quitButton);

        _settingsSystemRoot = CreateSettingsSectionRoot("SettingsSystemRoot");
        _settingsDisplayRoot = CreateSettingsSectionRoot("SettingsDisplayRoot");
        _settingsProgressRoot = CreateSettingsSectionRoot("SettingsProgressRoot");

        BuildSystemSection(_settingsSystemRoot);
        BuildDisplaySection(_settingsDisplayRoot);
        BuildProgressSection(_settingsProgressRoot);
    }

    private void BuildEquipmentUi()
    {
        _equipmentRoot = new VBoxContainer();
        _equipmentRoot.Name = "EquipmentRoot";
        _equipmentRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _equipmentRoot.OffsetLeft = 20.0f;
        _equipmentRoot.OffsetTop = 42.0f;
        _equipmentRoot.OffsetRight = -20.0f;
        _equipmentRoot.OffsetBottom = -18.0f;
        _equipmentRoot.AddThemeConstantOverride("separation", 10);
        _leftPage.AddChild(_equipmentRoot);

        HBoxContainer actionRow = new();
        actionRow.AddThemeConstantOverride("separation", 8);
        _equipmentRoot.AddChild(actionRow);

        Button equipWeaponButton = new();
        equipWeaponButton.Text = "装备背包武器";
        equipWeaponButton.Pressed += () => EquipFromBackpack(EquipmentSlotType.Weapon);
        actionRow.AddChild(equipWeaponButton);

        Button equipArmorButton = new();
        equipArmorButton.Text = "装备背包护具";
        equipArmorButton.Pressed += () => EquipFromBackpack(EquipmentSlotType.Armor);
        actionRow.AddChild(equipArmorButton);

        Label hint = new();
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        hint.Text = "新获得的装备会先进入背包，不会自动替换当前已装备物品。点击上方按钮后，才会把对应槽位旧装备换回背包。";
        _equipmentRoot.AddChild(hint);

        _equipmentContentLabel = new RichTextLabel();
        _equipmentContentLabel.FitContent = false;
        _equipmentContentLabel.ScrollActive = true;
        _equipmentContentLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _equipmentRoot.AddChild(_equipmentContentLabel);
    }

    private void BuildBugFeedbackUi()
    {
        _bugFeedbackRoot = new VBoxContainer();
        _bugFeedbackRoot.Name = "BugFeedbackRoot";
        _bugFeedbackRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _bugFeedbackRoot.OffsetLeft = 20.0f;
        _bugFeedbackRoot.OffsetTop = 42.0f;
        _bugFeedbackRoot.OffsetRight = -20.0f;
        _bugFeedbackRoot.OffsetBottom = -18.0f;
        _bugFeedbackRoot.AddThemeConstantOverride("separation", 10);
        _leftPage.AddChild(_bugFeedbackRoot);

        RichTextLabel hint = new();
        hint.FitContent = true;
        hint.ScrollActive = false;
        hint.Text = UiText.BugFeedbackHint;
        _bugFeedbackRoot.AddChild(hint);

        _bugFeedbackInput = new TextEdit();
        _bugFeedbackInput.CustomMinimumSize = new Vector2(0.0f, 180.0f);
        _bugFeedbackInput.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _bugFeedbackInput.PlaceholderText = UiText.BugFeedbackInputPlaceholder;
        _bugFeedbackRoot.AddChild(_bugFeedbackInput);

        HBoxContainer actionRow = new();
        actionRow.AddThemeConstantOverride("separation", 8);
        _bugFeedbackRoot.AddChild(actionRow);

        Button copyButton = new();
        copyButton.Text = UiText.CopyLogPath;
        copyButton.Pressed += CopyLogFolderPath;
        actionRow.AddChild(copyButton);

        Button exportButton = new();
        exportButton.Text = UiText.ExportFeedbackPack;
        exportButton.Pressed += ExportFeedbackPack;
        actionRow.AddChild(exportButton);

        Button openButton = new();
        openButton.Text = UiText.OpenDataFolder;
        openButton.Pressed += OpenLogFolder;
        actionRow.AddChild(openButton);

        _bugFeedbackStatusLabel = new Label();
        _bugFeedbackStatusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _bugFeedbackStatusLabel.Text = ProjectSettings.GlobalizePath("user://");
        _bugFeedbackRoot.AddChild(_bugFeedbackStatusLabel);
    }

    private Button CreateSettingsSectionButton(string title, string sectionId)
    {
        Button button = new();
        button.Text = title;
        button.ToggleMode = true;
        button.Pressed += () => ShowSettingsSection(sectionId);
        _settingsNavRoot.AddChild(button);
        return button;
    }

    private VBoxContainer CreateSettingsSectionRoot(string name)
    {
        VBoxContainer root = new();
        root.Name = name;
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 20.0f;
        root.OffsetTop = 74.0f;
        root.OffsetRight = -20.0f;
        root.OffsetBottom = -128.0f;
        root.AddThemeConstantOverride("separation", 6);
        _leftPage.AddChild(root);
        return root;
    }

    private void BuildSystemSection(VBoxContainer root)
    {
        _languageOption = AddOptionRow(root, UiText.Language, new[] { "简体中文", "English" });
        _keepOnTopCheck = AddCheckRow(root, UiText.KeepOnTop);
        _taskbarIconCheck = AddCheckRow(root, UiText.ReservedLabel(UiText.TaskbarIcon));
        _vsyncCheck = AddCheckRow(root, UiText.Vsync);
        _fpsOption = AddOptionRow(root, UiText.MaxFps, new[] { "30", "60", "120", "不限" });

        _languageOption.ItemSelected += _ => OnLanguageChanged();
        _keepOnTopCheck.Toggled += value => OnSettingChanged("keep_on_top", value, applyNow: true);
        _taskbarIconCheck.Toggled += value => OnSettingChanged("taskbar_icon", value);
        _vsyncCheck.Toggled += value => OnSettingChanged("vsync", value, applyNow: true);
        _fpsOption.ItemSelected += _ => OnFpsChanged();
    }

    private void BuildDisplaySection(VBoxContainer root)
    {
        _resolutionOption = AddOptionRow(root, UiText.Resolution, new[] { "1280x720", "1600x900", "1920x1080", "2560x1440" });
        _showControlMarkerCheck = AddCheckRow(root, UiText.ShowControlMarkers);
        _showValidationPanelCheck = AddCheckRow(root, UiText.ShowValidationPanel);

        HBoxContainer logRow = new();
        logRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(logRow);
        Label logLabel = new();
        logLabel.Text = UiText.LogFolder;
        logLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        logRow.AddChild(logLabel);
        _openLogFolderButton = new();
        _openLogFolderButton.Text = UiText.Open;
        _openLogFolderButton.Pressed += OpenLogFolder;
        logRow.AddChild(_openLogFolderButton);

        _gameScaleOption = AddOptionRow(root, UiText.ExperimentalLabel(UiText.GameScale), new[] { "1.00", "1.10", "1.25", "1.33", "1.50" });
        _uiScaleOption = AddOptionRow(root, UiText.UiScale, new[] { "1.00", "1.10", "1.25", "1.33", "1.50" });

        _resolutionOption.ItemSelected += _ => OnResolutionChanged();
        _showControlMarkerCheck.Toggled += value => OnSettingChanged("show_control_markers", value);
        _showValidationPanelCheck.Toggled += value => OnSettingChanged("show_validation_panel", value);
        _gameScaleOption.ItemSelected += _ => OnGameScaleChanged();
        _uiScaleOption.ItemSelected += _ => OnUiScaleChanged();
    }

    private void BuildProgressSection(VBoxContainer root)
    {
        _autoSaveIntervalOption = AddOptionRow(root, UiText.AutoSaveInterval, new[] { "5 秒", "10 秒", "30 秒", "60 秒" });
        _cloudSyncCheck = AddCheckRow(root, UiText.ReservedLabel(UiText.CloudSync));
        _milestoneTipsCheck = AddCheckRow(root, UiText.ExperimentalLabel(UiText.MilestoneTips));
        _globalDebugOverlayCheck = AddCheckRow(root, UiText.GlobalDebugOverlay);

        RichTextLabel hint = new();
        hint.FitContent = true;
        hint.ScrollActive = false;
        hint.Text = UiText.DevHintCloudSync;
        root.AddChild(hint);

        _autoSaveIntervalOption.ItemSelected += _ => OnAutoSaveIntervalChanged();
        _cloudSyncCheck.Toggled += value => OnSettingChanged("cloud_sync", value);
        _milestoneTipsCheck.Toggled += value => OnSettingChanged("milestone_tips", value);
        _globalDebugOverlayCheck.Toggled += value => OnSettingChanged("global_debug_overlay", value);
    }

    private CheckButton AddCheckRow(VBoxContainer parent, string title)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);
        parent.AddChild(row);

        Label label = new();
        label.Text = title;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(label);

        CheckButton check = new();
        row.AddChild(check);
        return check;
    }

    private OptionButton AddOptionRow(VBoxContainer parent, string title, string[] options)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);
        parent.AddChild(row);

        Label label = new();
        label.Text = title;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(label);

        OptionButton option = new();
        option.CustomMinimumSize = new Vector2(140.0f, 0.0f);
        foreach (string item in options)
        {
            option.AddItem(item);
        }
        row.AddChild(option);
        return option;
    }

    private void ShowSettingsSection(string sectionId)
    {
        _activeSettingsSection = sectionId;

        _settingsSystemRoot.Visible = sectionId == "system";
        _settingsDisplayRoot.Visible = sectionId == "display";
        _settingsProgressRoot.Visible = sectionId == "progress";

        _settingsSystemBtn.ButtonPressed = sectionId == "system";
        _settingsDisplayBtn.ButtonPressed = sectionId == "display";
        _settingsProgressBtn.ButtonPressed = sectionId == "progress";
    }

    private void UpdateSettingsUiVisibility()
    {
        bool isSettings = ActiveRightTabName == "SettingsTab";
        bool isBug = ActiveRightTabName == "BugTab";
        bool isEquipment = !_isShowingRightTab && ActiveLeftTabName == "EquipmentTab";
        _settingsNavRoot.Visible = isSettings;
        _settingsActionRoot.Visible = isSettings;
        _settingsSystemRoot.Visible = isSettings && _activeSettingsSection == "system";
        _settingsDisplayRoot.Visible = isSettings && _activeSettingsSection == "display";
        _settingsProgressRoot.Visible = isSettings && _activeSettingsSection == "progress";
        _bugFeedbackRoot.Visible = isBug;
        _equipmentRoot.Visible = isEquipment;
        _leftContentLabel.Visible = !isSettings && !isBug && !isEquipment;
        _rightPage.Visible = false;
    }

    private void UpdateSettingsControlsFromState()
    {
        _isApplyingSettingsUi = true;

        _languageOption.Selected = _settings["language"].AsString() == "en-US" ? 1 : 0;
        _keepOnTopCheck.ButtonPressed = _settings["keep_on_top"].AsBool();
        _taskbarIconCheck.ButtonPressed = _settings["taskbar_icon"].AsBool();
        _vsyncCheck.ButtonPressed = _settings["vsync"].AsBool();
        _showControlMarkerCheck.ButtonPressed = _settings["show_control_markers"].AsBool();
        _showValidationPanelCheck.ButtonPressed = _settings["show_validation_panel"].AsBool();
        _cloudSyncCheck.ButtonPressed = _settings["cloud_sync"].AsBool();
        _milestoneTipsCheck.ButtonPressed = _settings["milestone_tips"].AsBool();
        _globalDebugOverlayCheck.ButtonPressed = _settings["global_debug_overlay"].AsBool();

        _fpsOption.Selected = _settings["max_fps"].AsInt32() switch
        {
            30 => 0,
            60 => 1,
            120 => 2,
            _ => 3
        };

        SelectOptionByText(_resolutionOption, _settings["resolution"].AsString());
        SelectOptionByText(_gameScaleOption, _settings["game_scale"].AsDouble().ToString("0.00", CultureInfo.InvariantCulture));
        SelectOptionByText(_uiScaleOption, _settings["ui_scale"].AsDouble().ToString("0.00", CultureInfo.InvariantCulture));
        _autoSaveIntervalOption.Selected = _settings["auto_save_interval_sec"].AsInt32() switch
        {
            5 => 0,
            10 => 1,
            30 => 2,
            _ => 3
        };

        _isApplyingSettingsUi = false;
    }

    private static void SelectOptionByText(OptionButton option, string text)
    {
        for (int i = 0; i < option.ItemCount; i++)
        {
            if (option.GetItemText(i) == text)
            {
                option.Selected = i;
                return;
            }
        }
    }

    private void ResetSettings()
    {
        _settings["language"] = "zh-CN";
        _settings["keep_on_top"] = false;
        _settings["taskbar_icon"] = true;
        _settings["startup_animation"] = true;
        _settings["admin_mode"] = false;
        _settings["handwriting_support"] = false;
        _settings["vsync"] = true;
        _settings["max_fps"] = 60;
        _settings["resolution"] = "1600x900";
        _settings["show_control_markers"] = true;
        _settings["show_validation_panel"] = true;
        _settings["game_scale"] = 1.33;
        _settings["ui_scale"] = 1.0;
        _settings["auto_save_interval_sec"] = 10;
        _settings["cloud_sync"] = false;
        _settings["milestone_tips"] = true;
        _settings["global_debug_overlay"] = false;

        ApplySettingsRuntime();
        UpdateSettingsControlsFromState();
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnLanguageChanged()
    {
        if (_isApplyingSettingsUi) return;
        _settings["language"] = _languageOption.Selected == 1 ? "en-US" : "zh-CN";
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnFpsChanged()
    {
        if (_isApplyingSettingsUi) return;
        int maxFps = _fpsOption.Selected switch
        {
            0 => 30,
            1 => 60,
            2 => 120,
            _ => 0
        };
        _settings["max_fps"] = maxFps;
        ApplySettingsRuntime();
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnResolutionChanged()
    {
        if (_isApplyingSettingsUi) return;
        _settings["resolution"] = _resolutionOption.GetItemText(_resolutionOption.Selected);
        ApplyResolution();
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnGameScaleChanged()
    {
        if (_isApplyingSettingsUi) return;
        _settings["game_scale"] = ParseOptionFloat(_gameScaleOption);
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnUiScaleChanged()
    {
        if (_isApplyingSettingsUi) return;
        _settings["ui_scale"] = ParseOptionFloat(_uiScaleOption);
        ApplyUiScale();
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnAutoSaveIntervalChanged()
    {
        if (_isApplyingSettingsUi) return;
        int interval = _autoSaveIntervalOption.Selected switch
        {
            0 => 5,
            1 => 10,
            2 => 30,
            _ => 60
        };
        _settings["auto_save_interval_sec"] = interval;
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private double ParseOptionFloat(OptionButton option)
    {
        string text = option.GetItemText(option.Selected);
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            return value;
        }
        return 1.0;
    }

    private void OnSettingChanged(string key, bool value, bool applyNow = false)
    {
        if (_isApplyingSettingsUi) return;
        _settings[key] = value;
        if (applyNow)
        {
            ApplySettingsRuntime();
        }
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OpenLogFolder()
    {
        string path = ProjectSettings.GlobalizePath("user://");
        OS.ShellOpen(path);
    }

    private void CopyLogFolderPath()
    {
        string path = ProjectSettings.GlobalizePath("user://");
        DisplayServer.ClipboardSet(path);
        _bugFeedbackStatusLabel.Text = UiText.BugFeedbackCopied;
    }

    private void ExportFeedbackPack()
    {
        string description = _bugFeedbackInput.Text.Trim();
        if (string.IsNullOrEmpty(description))
        {
            _bugFeedbackStatusLabel.Text = UiText.BugFeedbackEmptyWarning;
            return;
        }

        string feedbackDir = "user://feedback";
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(feedbackDir));
        long timestamp = (long)Time.GetUnixTimeFromSystem();
        string filePath = $"{feedbackDir}/feedback_{timestamp}.txt";

        using FileAccess? file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            _bugFeedbackStatusLabel.Text = UiText.BugFeedbackExportFailed;
            return;
        }

        StringBuilder sb = new();
        sb.AppendLine("# Xiuxian Demo Feedback");
        sb.AppendLine($"timestamp_unix={timestamp}");
        sb.AppendLine($"left_tab={ActiveLeftTabName}");
        sb.AppendLine($"right_tab={ActiveRightTabName}");
        sb.AppendLine($"data_dir={ProjectSettings.GlobalizePath("user://")}");
        sb.AppendLine($"save_file={ProjectSettings.GlobalizePath("user://save_state.cfg")}");
        sb.AppendLine();
        sb.AppendLine("[description]");
        sb.AppendLine(description);
        file.StoreString(sb.ToString());

        _bugFeedbackStatusLabel.Text = UiText.BugFeedbackExportedPrefix + ProjectSettings.GlobalizePath(filePath);
    }

    private void ApplySettingsRuntime()
    {
        bool keepOnTop = _settings["keep_on_top"].AsBool();
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.AlwaysOnTop, keepOnTop);

        bool vsync = _settings["vsync"].AsBool();
        DisplayServer.WindowSetVsyncMode(vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

        Engine.MaxFps = _settings["max_fps"].AsInt32();
        ApplyResolution();
        ApplyUiScale();
    }

    private void ApplyResolution()
    {
        string resolution = _settings["resolution"].AsString();
        string[] parts = resolution.Split('x');
        if (parts.Length != 2)
        {
            return;
        }

        if (int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
        {
            DisplayServer.WindowSetSize(new Vector2I(width, height));
        }
    }

    private void ApplyUiScale()
    {
        double uiScale = _settings["ui_scale"].AsDouble();
        GetWindow().ContentScaleFactor = (float)uiScale;
    }

    private void RefreshCurrentPageContent()
    {
        if (ActiveRightTabName == "SettingsTab")
        {
            _leftTitleLabel.Text = UiText.SettingsTitle;
            UpdateSettingsUiVisibility();
            ShowSettingsSection(_activeSettingsSection);
            return;
        }

        UpdateSettingsUiVisibility();

        if (_isShowingRightTab)
        {
            _leftTitleLabel.Text = ButtonTextForTab("TopStrip/RightTabs", ActiveRightTabName);
            if (ActiveRightTabName == "BugTab")
            {
                UpdateSettingsUiVisibility();
                return;
            }
            AnimateContentSwap(_leftContentLabel, _leftTween, _rightTabContentMap[ActiveRightTabName], tween => _leftTween = tween, false);
            return;
        }

        _leftTitleLabel.Text = ButtonTextForTab("TopStrip/LeftTabs", ActiveLeftTabName);
        if (ActiveLeftTabName == "EquipmentTab")
        {
            _equipmentContentLabel.Text = GetLeftTabContent(ActiveLeftTabName);
            UpdateSettingsUiVisibility();
            return;
        }
        AnimateContentSwap(_leftContentLabel, _leftTween, GetLeftTabContent(ActiveLeftTabName), tween => _leftTween = tween, true);
    }

    private void CloseWindow()
    {
        if (GetParent() is SubmenuWindowController submenu)
        {
            submenu.ToggleVisible();
        }
    }

    private void ApplyStaticTexts()
    {
        SetButtonText("TopStrip/LeftTabs/CultivationTab", UiText.LeftTabCultivation);
        SetButtonText("TopStrip/LeftTabs/BattleLogTab", UiText.LeftTabBattleLog);
        SetButtonText("TopStrip/LeftTabs/EquipmentTab", UiText.LeftTabEquipment);
        SetButtonText("TopStrip/LeftTabs/StatsTab", UiText.LeftTabStats);
        SetButtonText("TopStrip/RightTabs/BugTab", UiText.RightTabBug);
        SetButtonText("TopStrip/RightTabs/SettingsTab", UiText.RightTabSettings);
        _closeButton.Text = "X";
    }

    private void SetButtonText(string nodePath, string text)
    {
        if (!HasNode(nodePath))
        {
            return;
        }

        GetNode<Button>(nodePath).Text = text;
    }

    private void BindButtons(IEnumerable<string> tabKeys, string groupPath, System.Action<string> setter)
    {
        foreach (string tabName in tabKeys)
        {
            if (!HasNode($"{groupPath}/{tabName}"))
            {
                continue;
            }

            Button button = GetNode<Button>($"{groupPath}/{tabName}");
            button.Pressed += () => setter(tabName);
        }
    }

    private void SyncButtons(string groupPath, IEnumerable<string> tabKeys, string activeTab)
    {
        foreach (string key in tabKeys)
        {
            if (!HasNode($"{groupPath}/{key}"))
            {
                continue;
            }

            Button button = GetNode<Button>($"{groupPath}/{key}");
            button.ButtonPressed = key == activeTab;
        }
    }

    private string ButtonTextForTab(string groupPath, string tabName)
    {
        if (!HasNode($"{groupPath}/{tabName}"))
        {
            return tabName;
        }

        return GetNode<Button>($"{groupPath}/{tabName}").Text;
    }

    private void AnimateContentSwap(
        RichTextLabel label,
        Tween? activeTween,
        string nextText,
        System.Action<Tween?> storeTween,
        bool isLeftPage)
    {
        activeTween?.Kill();

        Vector2 basePos = label.Position;
        float offset = isLeftPage ? 10.0f : -10.0f;

        Tween outTween = CreateTween();
        outTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        outTween.TweenProperty(label, "modulate", new Color(1, 1, 1, 0.18f), 0.08f);
        outTween.Parallel().TweenProperty(label, "position:x", basePos.X + offset, 0.08f);
        outTween.Finished += () =>
        {
            label.Text = nextText;
            label.Position = basePos - new Vector2(offset, 0.0f);

            Tween inTween = CreateTween();
            inTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            inTween.TweenProperty(label, "modulate", Colors.White, 0.12f);
            inTween.Parallel().TweenProperty(label, "position", basePos, 0.12f);
            storeTween(inTween);
        };

        storeTween(outTween);
    }
}
