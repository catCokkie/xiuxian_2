using Godot;

public partial class MainBarLayoutController : Control
{
    [Signal]
    public delegate void BookButtonPressedEventHandler();
    [Signal]
    public delegate void LayoutChangedEventHandler(float x, float width);

    [Export] public float MinWidth = 720.0f;
    [Export] public float MaxWidth = 1500.0f;
    [Export] public bool LockToBottom = true;
    [Export] public float MinBottomMargin = 8.0f;
    [Export] public float SectionGap = 8.0f;
    [Export] public float LineGap = 6.0f;
    [Export] public float Padding = 10.0f;

    private Button _dragHandle = null!;
    private Button _resizeHandle = null!;
    private Button _bookButton = null!;
    private Label _zoneLabel = null!;
    private Label _activityRateLabel = null!;
    private Label _realmStageLabel = null!;
    private Label? _moveDebugLabel;
    private ProgressBar _exploreProgressBar = null!;
    private Label _cultivationLabel = null!;
    private ProgressBar _cultivationProgressBar = null!;
    private Button _breakthroughButton = null!;
    private Panel _battleTrack = null!;
    private Panel _validationPanel = null!;
    private OptionButton? _actionModeOptionButton;
    private OptionButton? _levelOptionButton;
    private VBoxContainer _rootStack = null!;
    private Panel _petStagePanel = null!;
    private Label _petStatusChip = null!;
    private Label _interactionHint = null!;
    private HBoxContainer _drawerButtons = null!;
    private Button _battleDrawerButton = null!;
    private Button _validationDrawerButton = null!;
    private Button _debugDrawerButton = null!;
    private Button _compactModeButton = null!;
    private Panel _compactPetBar = null!;
    private Label _compactStatusLabel = null!;
    private Button _compactExpandButton = null!;
    private bool _compactMode;
    private bool _battleDrawerRequested;
    private bool _validationDrawerRequested;
    private bool _debugDrawerRequested;

    private bool _isDragging;
    private bool _isResizing;
    private Vector2 _lastMousePos;
    private float _fixedBottomY;
    private float _bottomMargin;

    public override void _Ready()
    {
        _dragHandle = GetNode<Button>("Chrome/DragHandleButton");
        _resizeHandle = GetNode<Button>("Chrome/ResizeHandleButton");
        _bookButton = GetNode<Button>("Chrome/BookButton");
        _zoneLabel = GetNode<Label>("Chrome/ZoneLabel");
        _activityRateLabel = GetNode<Label>("Chrome/ActivityRateLabel");
        _realmStageLabel = GetNode<Label>("Chrome/RealmStageLabel");
        _moveDebugLabel = GetNodeOrNull<Label>("Chrome/MoveDebugLabel");
        _exploreProgressBar = GetNode<ProgressBar>("Chrome/ExploreProgressBar");
        _cultivationLabel = GetNode<Label>("Chrome/CultivationLabel");
        _cultivationProgressBar = GetNode<ProgressBar>("Chrome/CultivationProgressBar");
        _breakthroughButton = GetNode<Button>("Chrome/BreakthroughButton");
        _battleTrack = GetNode<Panel>("Chrome/BattleTrack");
        _validationPanel = GetNode<Panel>("Chrome/ConfigValidationPanel");
        _actionModeOptionButton = GetNodeOrNull<OptionButton>("Chrome/ActionModeOptionButton");
        _levelOptionButton = GetNodeOrNull<OptionButton>("Chrome/LevelOptionButton");
        _rootStack = GetNode<VBoxContainer>("Chrome/RootStack");
        _petStagePanel = GetNode<Panel>("Chrome/RootStack/PetStagePanel");
        _petStatusChip = GetNode<Label>("Chrome/RootStack/PetStagePanel/PetStatusChip");
        _interactionHint = GetNode<Label>("Chrome/RootStack/PetStagePanel/InteractionHint");
        _drawerButtons = GetNode<HBoxContainer>("Chrome/RootStack/DrawerToggleButtons");
        _battleDrawerButton = GetNode<Button>("Chrome/RootStack/DrawerToggleButtons/BattleDrawerButton");
        _validationDrawerButton = GetNode<Button>("Chrome/RootStack/DrawerToggleButtons/ValidationDrawerButton");
        _debugDrawerButton = GetNode<Button>("Chrome/RootStack/DrawerToggleButtons/DebugDrawerButton");
        _compactModeButton = GetNode<Button>("Chrome/RootStack/DrawerToggleButtons/CompactModeButton");
        _compactPetBar = GetNode<Panel>("Chrome/RootStack/CompactPetBar");
        _compactStatusLabel = GetNode<Label>("Chrome/RootStack/CompactPetBar/CompactStatusLabel");
        _compactExpandButton = GetNode<Button>("Chrome/RootStack/CompactPetBar/CompactExpandButton");

        _dragHandle.GuiInput += OnDragHandleGuiInput;
        _resizeHandle.GuiInput += OnResizeHandleGuiInput;
        _bookButton.Pressed += () => EmitSignal(SignalName.BookButtonPressed);
        _dragHandle.Text = UiText.DragHandle;
        _resizeHandle.Text = UiText.ResizeHandle;
        _bookButton.Text = UiText.BookButton;
        _zoneLabel.Visible = false;
        _battleDrawerButton.Text = UiText.DrawerBattleButton;
        _validationDrawerButton.Text = UiText.DrawerValidationButton;
        _debugDrawerButton.Text = UiText.DrawerDebugButton;
        _compactModeButton.Text = UiText.CompactModeButton;
        _compactExpandButton.Text = UiText.CompactExpandButton;
        _battleDrawerButton.Pressed += () => SetDrawerVisible("battle", _battleDrawerButton.ButtonPressed);
        _validationDrawerButton.Pressed += () => SetDrawerVisible("validation", _validationDrawerButton.ButtonPressed);
        _debugDrawerButton.Pressed += () => SetDrawerVisible("debug", _debugDrawerButton.ButtonPressed);
        _compactModeButton.Pressed += () => SetCompactMode(true);
        _compactExpandButton.Pressed += () => SetCompactMode(false);
        _petStatusChip.Text = UiText.PetStatusExploring;
        _interactionHint.Text = UiText.PetInteractHint;
        _compactStatusLabel.Text = UiText.CompactModeActive;
        SetDrawerVisible("battle", false);
        SetDrawerVisible("validation", false);
        SetDrawerVisible("debug", false);

        _bottomMargin = Mathf.Max(MinBottomMargin, GetViewportRect().Size.Y - (Position.Y + Size.Y));
        _fixedBottomY = GetBottomLockedY();
        Position = new Vector2(Position.X, _fixedBottomY);
        UpdateRightAnchoredLayout();
    }

    public override void _Process(double delta)
    {
        if (!LockToBottom)
        {
            return;
        }

        float nextY = GetBottomLockedY();
        _fixedBottomY = nextY;
        if (!Mathf.IsEqualApprox(Position.Y, nextY))
        {
            Position = new Vector2(Position.X, nextY);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && !mouseButton.Pressed)
        {
            if (_isDragging || _isResizing)
            {
                EmitSignal(SignalName.LayoutChanged, Position.X, Size.X);
            }
            _isDragging = false;
            _isResizing = false;
        }

        if (@event is not InputEventMouseMotion mouseMotion)
        {
            return;
        }

        if (_isDragging)
        {
            Vector2 delta = mouseMotion.GlobalPosition - _lastMousePos;
            float targetX = Position.X + delta.X;
            float maxX = GetViewportRect().Size.X - Size.X;
            Position = new Vector2(Mathf.Clamp(targetX, 0.0f, Mathf.Max(maxX, 0.0f)), LockToBottom ? _fixedBottomY : Position.Y + delta.Y);
            _lastMousePos = mouseMotion.GlobalPosition;
            EmitSignal(SignalName.LayoutChanged, Position.X, Size.X);
        }

        if (_isResizing)
        {
            float nextWidth = Mathf.Clamp(Size.X + mouseMotion.Relative.X, MinWidth, MaxWidth);
            Size = new Vector2(nextWidth, Size.Y);
            UpdateRightAnchoredLayout();
            EmitSignal(SignalName.LayoutChanged, Position.X, Size.X);
        }
    }

    private void OnDragHandleGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            _isDragging = mouseButton.Pressed;
            _lastMousePos = mouseButton.GlobalPosition;
        }
    }

    private void OnResizeHandleGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            _isResizing = mouseButton.Pressed;
        }
    }

    public void ApplyLayout(float x, float width)
    {
        float clampedWidth = Mathf.Clamp(width, MinWidth, MaxWidth);
        Size = new Vector2(clampedWidth, Size.Y);

        float maxX = GetViewportRect().Size.X - Size.X;
        Position = new Vector2(Mathf.Clamp(x, 0.0f, Mathf.Max(maxX, 0.0f)), LockToBottom ? _fixedBottomY : Position.Y);
        UpdateRightAnchoredLayout();
    }

    public void SetCompactMode(bool compact)
    {
        _compactMode = compact;
        _compactPetBar.Visible = compact;
        _petStagePanel.Visible = !compact;
        _drawerButtons.Visible = !compact;
        ApplyDrawerVisibility();
        UpdateRightAnchoredLayout();
    }

    public bool IsCompactMode()
    {
        return _compactMode;
    }

    public void SetDrawerVisible(string drawerId, bool visible)
    {
        switch (drawerId)
        {
            case "battle":
                _battleDrawerRequested = visible;
                _battleDrawerButton.SetPressedNoSignal(visible);
                break;
            case "validation":
                _validationDrawerRequested = visible;
                _validationDrawerButton.SetPressedNoSignal(visible);
                break;
            case "debug":
                _debugDrawerRequested = visible;
                _debugDrawerButton.SetPressedNoSignal(visible);
                break;
        }
        ApplyDrawerVisibility();
        UpdateRightAnchoredLayout();
    }

    public bool IsDrawerVisible(string drawerId)
    {
        return drawerId switch
        {
            "battle" => _battleDrawerRequested,
            "validation" => _validationDrawerRequested,
            "debug" => _debugDrawerRequested,
            _ => false
        };
    }

    private void ApplyDrawerVisibility()
    {
        _battleTrack.Visible = !_compactMode && _battleDrawerRequested;
        _validationPanel.Visible = !_compactMode && _validationDrawerRequested;
        if (_moveDebugLabel != null)
        {
            _moveDebugLabel.Visible = !_compactMode && _debugDrawerRequested;
        }
    }

    private void UpdateRightAnchoredLayout()
    {
        float rightMargin = Padding;
        float leftMargin = Padding;
        float topY = Padding;
        float rightEdge = Size.X - rightMargin;
        float availableContentWidth = rightEdge - (leftMargin + 58.0f);

        float titleRailWidth = 58.0f;
        float contentLeft = leftMargin + titleRailWidth;

        float optionHeight = 28.0f;
        float barHeight = 24.0f;
        float footerHeight = 18.0f;
        float labelHeight = 20.0f;
        float bottomBlockTop = Size.Y - Padding - (optionHeight + LineGap + labelHeight + LineGap + barHeight + LineGap + footerHeight);
        bottomBlockTop = Mathf.Max(topY + 124.0f, bottomBlockTop);
        float rootBottomInset = Mathf.Max(56.0f, Size.Y - bottomBlockTop);

        _rootStack.OffsetLeft = contentLeft;
        _rootStack.OffsetTop = topY;
        _rootStack.OffsetRight = -rightMargin;
        _rootStack.OffsetBottom = -rootBottomInset;

        float petHeight = _compactMode ? 0.0f : 96.0f;
        _petStagePanel.CustomMinimumSize = new Vector2(0.0f, petHeight);
        _compactPetBar.CustomMinimumSize = new Vector2(0.0f, 40.0f);

        _dragHandle.Position = new Vector2(leftMargin, topY);
        _bookButton.Position = new Vector2(leftMargin, topY + 62.0f);
        _resizeHandle.Position = new Vector2(rightEdge - _resizeHandle.Size.X, topY);
        _compactExpandButton.Position = new Vector2(rightEdge - _compactExpandButton.Size.X - 6.0f, _compactExpandButton.Position.Y);

        float drawerStartY = topY + (_compactMode ? 44.0f : 96.0f) + SectionGap + 30.0f + SectionGap;
        float drawerRightLimit = rightEdge - 2.0f;

        float breakthroughWidth = _breakthroughButton.Size.X;
        float exploreWidth = Mathf.Clamp(availableContentWidth * 0.18f, 110.0f, 240.0f);
        float cultivationWidth = Mathf.Clamp(availableContentWidth * 0.22f, 130.0f, _cultivationProgressBar.Size.X);
        float rightClusterWidth = cultivationWidth + 8.0f + breakthroughWidth + 8.0f + exploreWidth;
        float leftMinReserve = 130.0f + 8.0f + 110.0f + 16.0f;
        float rightMax = Mathf.Max(300.0f, availableContentWidth - leftMinReserve);
        if (rightClusterWidth > rightMax)
        {
            float overflow = rightClusterWidth - rightMax;
            float reduceCultivation = Mathf.Min(overflow * 0.55f, cultivationWidth - 130.0f);
            cultivationWidth -= reduceCultivation;
            overflow -= reduceCultivation;
            if (overflow > 0.0f)
            {
                float reduceExplore = Mathf.Min(overflow, exploreWidth - 110.0f);
                exploreWidth -= reduceExplore;
            }
            rightClusterWidth = cultivationWidth + 8.0f + breakthroughWidth + 8.0f + exploreWidth;
        }

        float rightBlockStartX = rightEdge - (exploreWidth + 8.0f + breakthroughWidth + 8.0f + cultivationWidth);
        float leftAvailWidth = Mathf.Max(240.0f, rightBlockStartX - contentLeft - 8.0f);
        float levelWidth = Mathf.Clamp(leftAvailWidth * 0.60f, 130.0f, 260.0f);
        float actionWidth = Mathf.Clamp(leftAvailWidth - levelWidth - 8.0f, 110.0f, 160.0f);
        float activityWidth = Mathf.Max(180.0f, rightBlockStartX - (contentLeft + 110.0f) - 8.0f);

        float optionRowY = bottomBlockTop;
        float labelRowY = optionRowY + optionHeight + LineGap;
        float barRowY = labelRowY + labelHeight + LineGap;
        float footerRowY = barRowY + barHeight + LineGap;

        if (_levelOptionButton != null)
        {
            _levelOptionButton.Size = new Vector2(levelWidth, _levelOptionButton.Size.Y);
            _levelOptionButton.Position = new Vector2(contentLeft, optionRowY);
        }

        if (_actionModeOptionButton != null)
        {
            _actionModeOptionButton.Size = new Vector2(actionWidth, _actionModeOptionButton.Size.Y);
            _actionModeOptionButton.Position = new Vector2(contentLeft + levelWidth + 8.0f, optionRowY);
        }

        _cultivationLabel.Position = new Vector2(rightBlockStartX, labelRowY);
        _cultivationProgressBar.Position = new Vector2(rightBlockStartX, barRowY);
        _breakthroughButton.Position = new Vector2(rightBlockStartX + cultivationWidth + 8.0f, barRowY - 2.0f);
        _exploreProgressBar.Size = new Vector2(exploreWidth, _exploreProgressBar.Size.Y);
        _exploreProgressBar.Position = new Vector2(rightEdge - exploreWidth, barRowY);

        _zoneLabel.Position = new Vector2(rightEdge - _zoneLabel.Size.X, labelRowY);
        _realmStageLabel.Position = new Vector2(contentLeft, footerRowY);
        _activityRateLabel.Size = new Vector2(activityWidth, _activityRateLabel.Size.Y);
        _activityRateLabel.Position = new Vector2(contentLeft + 110.0f, footerRowY);

        float drawerWidth = Mathf.Max(360.0f, drawerRightLimit - contentLeft);
        float drawerY = drawerStartY;
        if (_battleTrack.Visible)
        {
            _battleTrack.Position = new Vector2(contentLeft, drawerY);
            _battleTrack.Size = new Vector2(drawerWidth, _battleTrack.Size.Y);
            drawerY += _battleTrack.Size.Y + LineGap;
        }

        if (_validationPanel != null && _validationPanel.Visible)
        {
            float validationWidth = Mathf.Clamp(drawerWidth * 0.68f, 320.0f, drawerWidth);
            _validationPanel.Size = new Vector2(validationWidth, _validationPanel.Size.Y);
            _validationPanel.Position = new Vector2(contentLeft, drawerY);
            drawerY += _validationPanel.Size.Y + LineGap;
        }

        if (_moveDebugLabel != null && _moveDebugLabel.Visible)
        {
            _moveDebugLabel.Position = new Vector2(contentLeft, drawerY);
            _moveDebugLabel.Size = new Vector2(drawerWidth, 64.0f);
        }
    }

    private float GetBottomLockedY()
    {
        return GetViewportRect().Size.Y - Size.Y - _bottomMargin;
    }
}
