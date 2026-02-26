using Godot;
using System.Collections.Generic;
using System.Globalization;
using Xiuxian.Scripts.Services;

public partial class BookTabsController : Control
{
    [Signal]
    public delegate void ActiveTabsChangedEventHandler(string leftTabName, string rightTabName);

    private readonly Dictionary<string, string> _leftTabContentMap = new()
    {
        { "CultivationTab", "修炼概况\n- 当前境界\n- 突破条件\n- 心法加成" },
        { "EquipmentTab", "装备情况\n- 武器/护具/饰品\n- 词条预览\n- 套装效果" },
        { "StatsTab", "统计概览\n- 总输入次数\n- 累计探索时长\n- 战斗胜率" },
    };

    private readonly Dictionary<string, string> _rightTabContentMap = new()
    {
        { "OnlineTab", "联机\n该功能开发中。" },
        { "BugTab", "Bug反馈\n- 描述问题\n- 复制日志路径\n- 导出反馈包" },
        { "SettingsTab", "设置" },
    };

    private RichTextLabel _leftContentLabel = null!;
    private RichTextLabel _rightContentLabel = null!;
    private Label _leftTitleLabel = null!;
    private Label _rightTitleLabel = null!;
    private Label _coinLabel = null!;
    private Control _leftPage = null!;
    private Control _rightPage = null!;

    private VBoxContainer _settingsNavRoot = null!;
    private VBoxContainer _settingsSystemRoot = null!;
    private VBoxContainer _settingsDisplayRoot = null!;
    private VBoxContainer _settingsProgressRoot = null!;
    private VBoxContainer _settingsActionRoot = null!;
    private Button _settingsSystemBtn = null!;
    private Button _settingsDisplayBtn = null!;
    private Button _settingsProgressBtn = null!;

    private OptionButton _languageOption = null!;
    private CheckButton _keepOnTopCheck = null!;
    private CheckButton _taskbarIconCheck = null!;
    private CheckButton _startupAnimCheck = null!;
    private CheckButton _adminModeCheck = null!;
    private CheckButton _handwritingCheck = null!;
    private CheckButton _vsyncCheck = null!;
    private OptionButton _fpsOption = null!;

    private OptionButton _resolutionOption = null!;
    private CheckButton _showControlMarkerCheck = null!;
    private Button _openLogFolderButton = null!;
    private OptionButton _gameScaleOption = null!;
    private OptionButton _uiScaleOption = null!;
    private OptionButton _autoSaveIntervalOption = null!;
    private CheckButton _cloudSyncCheck = null!;
    private CheckButton _milestoneTipsCheck = null!;

    private Tween? _leftTween;
    private Tween? _rightTween;
    private InputActivityState? _activityState;
    private BackpackState? _backpackState;
    private ResourceWalletState? _resourceWalletState;
    private PlayerProgressState? _playerProgressState;

    public string ActiveLeftTabName { get; private set; } = "CultivationTab";
    public string ActiveRightTabName { get; private set; } = "OnlineTab";

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
        ["game_scale"] = 1.33,
        ["ui_scale"] = 1.0,
        ["auto_save_interval_sec"] = 10,
        ["cloud_sync"] = false,
        ["milestone_tips"] = true,
    };

    public override void _Ready()
    {
        _leftContentLabel = GetNode<RichTextLabel>("SpreadBody/LeftPage/LeftContentLabel");
        _rightContentLabel = GetNode<RichTextLabel>("SpreadBody/RightPage/RightContentLabel");
        _leftTitleLabel = GetNode<Label>("SpreadBody/LeftPage/LeftTitle");
        _rightTitleLabel = GetNode<Label>("SpreadBody/RightPage/RightTitle");
        _coinLabel = GetNode<Label>("BottomBar/CoinLabel");
        _leftPage = GetNode<Control>("SpreadBody/LeftPage");
        _rightPage = GetNode<Control>("SpreadBody/RightPage");
        _activityState = GetNodeOrNull<InputActivityState>("/root/InputActivityState");
        _backpackState = GetNodeOrNull<BackpackState>("/root/BackpackState");
        _resourceWalletState = GetNodeOrNull<ResourceWalletState>("/root/ResourceWalletState");
        _playerProgressState = GetNodeOrNull<PlayerProgressState>("/root/PlayerProgressState");

        if (_activityState != null)
        {
            _activityState.ActivityTick += OnActivityTick;
        }
        if (_backpackState != null)
        {
            _backpackState.InventoryChanged += OnInventoryChanged;
        }
        if (_resourceWalletState != null)
        {
            _resourceWalletState.WalletChanged += OnWalletChanged;
        }
        if (_playerProgressState != null)
        {
            _playerProgressState.RealmProgressChanged += OnRealmProgressChanged;
        }

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
        }
        if (_resourceWalletState != null)
        {
            _resourceWalletState.WalletChanged -= OnWalletChanged;
        }
        if (_playerProgressState != null)
        {
            _playerProgressState.RealmProgressChanged -= OnRealmProgressChanged;
        }
    }

    public void SetSpiritStone(int amount)
    {
        _coinLabel.Text = $"灵石 {amount}";
    }

    public void RestoreActiveTabs(string leftTabName, string rightTabName)
    {
        if (!_leftTabContentMap.ContainsKey(leftTabName))
        {
            leftTabName = "CultivationTab";
        }

        if (!_rightTabContentMap.ContainsKey(rightTabName))
        {
            rightTabName = "OnlineTab";
        }

        ActiveLeftTabName = leftTabName;
        ActiveRightTabName = rightTabName;

        SyncButtons("TopStrip/LeftTabs", _leftTabContentMap.Keys, ActiveLeftTabName);
        SyncButtons("TopStrip/RightTabs", _rightTabContentMap.Keys, ActiveRightTabName);

        _leftTitleLabel.Text = ButtonTextForTab("TopStrip/LeftTabs", ActiveLeftTabName);
        _rightTitleLabel.Text = ButtonTextForTab("TopStrip/RightTabs", ActiveRightTabName);
        _leftContentLabel.Text = GetLeftTabContent(ActiveLeftTabName);

        if (ActiveRightTabName == "SettingsTab")
        {
            _rightContentLabel.Visible = false;
        }
        else
        {
            _rightContentLabel.Visible = true;
            _rightContentLabel.Text = _rightTabContentMap[ActiveRightTabName];
        }

        UpdateSettingsUiVisibility();
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

        ActiveLeftTabName = tabName;
        SyncButtons("TopStrip/LeftTabs", _leftTabContentMap.Keys, ActiveLeftTabName);
        _leftTitleLabel.Text = ButtonTextForTab("TopStrip/LeftTabs", tabName);
        AnimateContentSwap(_leftContentLabel, _leftTween, GetLeftTabContent(tabName), tween => _leftTween = tween, true);
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

    private void OnWalletChanged(double lingqi, double insight, double petAffinity)
    {
        RefreshCoinLabel();
        RefreshDynamicTabContent();
    }

    private void OnRealmProgressChanged(int realmLevel, double realmExp, double realmExpRequired)
    {
        RefreshDynamicTabContent();
    }

    private void RefreshDynamicTabContent()
    {
        if (ActiveRightTabName == "SettingsTab")
        {
            return;
        }

        if (ActiveLeftTabName == "CultivationTab" || ActiveLeftTabName == "StatsTab")
        {
            _leftContentLabel.Text = GetLeftTabContent(ActiveLeftTabName);
        }
    }

    private string GetLeftTabContent(string tabName)
    {
        return tabName switch
        {
            "CultivationTab" => BuildCultivationOverviewText(),
            "StatsTab" => BuildStatsOverviewText(),
            _ => _leftTabContentMap[tabName]
        };
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
            $"修炼概况\n" +
            $"- 当前境界: 炼气{_playerProgressState.RealmLevel}层\n" +
            $"- 境界经验: {_playerProgressState.RealmExp:0.0}/{expRequired:0.0} ({expPercent:0}%)\n" +
            $"- 灵气: {_resourceWalletState.Lingqi:0.0}\n" +
            $"- 悟性: {_resourceWalletState.Insight:0.0}\n" +
            $"- 灵宠亲和: {_resourceWalletState.PetAffinity:0.0}\n" +
            $"- 心情倍率: x{_playerProgressState.GetMoodMultiplier():0.00}";
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
            $"统计概览\n" +
            $"- 总键盘按下: {_activityState.TotalKeyDownCount:N0}\n" +
            $"- 总鼠标点击: {_activityState.TotalMouseClickCount:N0}\n" +
            $"- 总滚轮步数: {_activityState.TotalMouseScrollSteps:N0}\n" +
            $"- 总移动距离: {_activityState.TotalMouseMoveDistancePx:N0}px\n" +
            $"- AP缓冲: {_activityState.ApAccumulator:0.0}\n" +
            $"- 灵草库存: {herbCount}\n" +
            $"- 灵气碎片库存: {shardCount}";
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
        SyncButtons("TopStrip/RightTabs", _rightTabContentMap.Keys, ActiveRightTabName);
        _rightTitleLabel.Text = ButtonTextForTab("TopStrip/RightTabs", tabName);

        if (tabName == "SettingsTab")
        {
            _rightTween?.Kill();
            _rightContentLabel.Visible = false;
            _leftTitleLabel.Text = "设置";
            ShowSettingsSection(_activeSettingsSection);
            UpdateSettingsUiVisibility();
        }
        else
        {
            _rightContentLabel.Visible = true;
            _leftTitleLabel.Text = ButtonTextForTab("TopStrip/LeftTabs", ActiveLeftTabName);
            UpdateSettingsUiVisibility();
            AnimateContentSwap(_rightContentLabel, _rightTween, _rightTabContentMap[tabName], tween => _rightTween = tween, false);
        }

        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void BuildSettingsUi()
    {
        _settingsNavRoot = new VBoxContainer();
        _settingsNavRoot.Name = "SettingsNavRoot";
        _settingsNavRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _settingsNavRoot.OffsetLeft = 20.0f;
        _settingsNavRoot.OffsetTop = 36.0f;
        _settingsNavRoot.OffsetRight = -20.0f;
        _settingsNavRoot.OffsetBottom = -72.0f;
        _settingsNavRoot.AddThemeConstantOverride("separation", 8);
        _leftPage.AddChild(_settingsNavRoot);

        _settingsSystemBtn = CreateSettingsSectionButton("系统", "system");
        _settingsDisplayBtn = CreateSettingsSectionButton("画面", "display");
        _settingsProgressBtn = CreateSettingsSectionButton("进度", "progress");

        _settingsActionRoot = new VBoxContainer();
        _settingsActionRoot.Name = "SettingsActionRoot";
        _settingsActionRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _settingsActionRoot.OffsetLeft = 20.0f;
        _settingsActionRoot.OffsetTop = 300.0f;
        _settingsActionRoot.OffsetRight = -20.0f;
        _settingsActionRoot.OffsetBottom = -12.0f;
        _settingsActionRoot.AddThemeConstantOverride("separation", 8);
        _leftPage.AddChild(_settingsActionRoot);

        Button resetButton = new();
        resetButton.Text = "重置并重启";
        resetButton.Pressed += ResetSettings;
        _settingsActionRoot.AddChild(resetButton);

        Button quitButton = new();
        quitButton.Text = "退出";
        quitButton.Pressed += () => GetTree().Quit();
        _settingsActionRoot.AddChild(quitButton);

        _settingsSystemRoot = CreateRightSectionRoot("SettingsSystemRoot");
        _settingsDisplayRoot = CreateRightSectionRoot("SettingsDisplayRoot");
        _settingsProgressRoot = CreateRightSectionRoot("SettingsProgressRoot");

        BuildSystemSection(_settingsSystemRoot);
        BuildDisplaySection(_settingsDisplayRoot);
        BuildProgressSection(_settingsProgressRoot);
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

    private VBoxContainer CreateRightSectionRoot(string name)
    {
        VBoxContainer root = new();
        root.Name = name;
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 16.0f;
        root.OffsetTop = 36.0f;
        root.OffsetRight = -16.0f;
        root.OffsetBottom = -16.0f;
        root.AddThemeConstantOverride("separation", 6);
        _rightPage.AddChild(root);
        return root;
    }

    private void BuildSystemSection(VBoxContainer root)
    {
        _languageOption = AddOptionRow(root, "语言", new[] { "简体中文", "English" });
        _keepOnTopCheck = AddCheckRow(root, "保持窗口置顶");
        _taskbarIconCheck = AddCheckRow(root, "任务栏图标");
        _startupAnimCheck = AddCheckRow(root, "开机启动动画");
        _adminModeCheck = AddCheckRow(root, "管理员模式");
        _handwritingCheck = AddCheckRow(root, "手写支持");
        _vsyncCheck = AddCheckRow(root, "垂直同步");
        _fpsOption = AddOptionRow(root, "帧率", new[] { "30", "60", "120", "不限" });

        _languageOption.ItemSelected += _ => OnLanguageChanged();
        _keepOnTopCheck.Toggled += value => OnSettingChanged("keep_on_top", value, applyNow: true);
        _taskbarIconCheck.Toggled += value => OnSettingChanged("taskbar_icon", value);
        _startupAnimCheck.Toggled += value => OnSettingChanged("startup_animation", value);
        _adminModeCheck.Toggled += value => OnSettingChanged("admin_mode", value);
        _handwritingCheck.Toggled += value => OnSettingChanged("handwriting_support", value);
        _vsyncCheck.Toggled += value => OnSettingChanged("vsync", value, applyNow: true);
        _fpsOption.ItemSelected += _ => OnFpsChanged();
    }

    private void BuildDisplaySection(VBoxContainer root)
    {
        _resolutionOption = AddOptionRow(root, "主界面分辨率", new[] { "1280x720", "1600x900", "1920x1080", "2560x1440" });
        _showControlMarkerCheck = AddCheckRow(root, "显示界面控制标记");

        HBoxContainer logRow = new();
        logRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(logRow);
        Label logLabel = new();
        logLabel.Text = "显示日志文件夹";
        logLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        logRow.AddChild(logLabel);
        _openLogFolderButton = new();
        _openLogFolderButton.Text = "打开";
        _openLogFolderButton.Pressed += OpenLogFolder;
        logRow.AddChild(_openLogFolderButton);

        _gameScaleOption = AddOptionRow(root, "游戏缩放比例", new[] { "1.00", "1.10", "1.25", "1.33", "1.50" });
        _uiScaleOption = AddOptionRow(root, "界面缩放比例", new[] { "1.00", "1.10", "1.25", "1.33", "1.50" });

        _resolutionOption.ItemSelected += _ => OnResolutionChanged();
        _showControlMarkerCheck.Toggled += value => OnSettingChanged("show_control_markers", value);
        _gameScaleOption.ItemSelected += _ => OnGameScaleChanged();
        _uiScaleOption.ItemSelected += _ => OnUiScaleChanged();
    }

    private void BuildProgressSection(VBoxContainer root)
    {
        _autoSaveIntervalOption = AddOptionRow(root, "自动存档频率", new[] { "5 秒", "10 秒", "30 秒", "60 秒" });
        _cloudSyncCheck = AddCheckRow(root, "云端同步");
        _milestoneTipsCheck = AddCheckRow(root, "里程碑提示");

        RichTextLabel hint = new();
        hint.FitContent = true;
        hint.ScrollActive = false;
        hint.Text = "说明：云端同步功能尚在开发中，当前仅保存开关状态。";
        root.AddChild(hint);

        _autoSaveIntervalOption.ItemSelected += _ => OnAutoSaveIntervalChanged();
        _cloudSyncCheck.Toggled += value => OnSettingChanged("cloud_sync", value);
        _milestoneTipsCheck.Toggled += value => OnSettingChanged("milestone_tips", value);
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
        _settingsNavRoot.Visible = isSettings;
        _settingsActionRoot.Visible = isSettings;
        _settingsSystemRoot.Visible = isSettings && _activeSettingsSection == "system";
        _settingsDisplayRoot.Visible = isSettings && _activeSettingsSection == "display";
        _settingsProgressRoot.Visible = isSettings && _activeSettingsSection == "progress";
        _leftContentLabel.Visible = !isSettings;
    }

    private void UpdateSettingsControlsFromState()
    {
        _isApplyingSettingsUi = true;

        _languageOption.Selected = _settings["language"].AsString() == "en-US" ? 1 : 0;
        _keepOnTopCheck.ButtonPressed = _settings["keep_on_top"].AsBool();
        _taskbarIconCheck.ButtonPressed = _settings["taskbar_icon"].AsBool();
        _startupAnimCheck.ButtonPressed = _settings["startup_animation"].AsBool();
        _adminModeCheck.ButtonPressed = _settings["admin_mode"].AsBool();
        _handwritingCheck.ButtonPressed = _settings["handwriting_support"].AsBool();
        _vsyncCheck.ButtonPressed = _settings["vsync"].AsBool();
        _showControlMarkerCheck.ButtonPressed = _settings["show_control_markers"].AsBool();
        _cloudSyncCheck.ButtonPressed = _settings["cloud_sync"].AsBool();
        _milestoneTipsCheck.ButtonPressed = _settings["milestone_tips"].AsBool();

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
        _settings["game_scale"] = 1.33;
        _settings["ui_scale"] = 1.0;
        _settings["auto_save_interval_sec"] = 10;
        _settings["cloud_sync"] = false;
        _settings["milestone_tips"] = true;

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
