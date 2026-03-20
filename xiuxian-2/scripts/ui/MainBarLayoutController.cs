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

    private Button _dragHandle = null!;
    private Button _resizeHandle = null!;
    private Button _bookButton = null!;
    private Label _zoneLabel = null!;
    private Label _activityRateLabel = null!;
    private Label _realmStageLabel = null!;
    private ProgressBar _exploreProgressBar = null!;
    private Label _cultivationLabel = null!;
    private ProgressBar _cultivationProgressBar = null!;
    private Button _breakthroughButton = null!;
    private Panel _battleTrack = null!;
    private Panel _validationPanel = null!;
    private Label _validationTitleLabel = null!;
    private RichTextLabel _validationBodyLabel = null!;
    private OptionButton? _actionModeOptionButton;
    private OptionButton? _levelOptionButton;

    private float _defaultExploreProgressBarWidth;
    private float _defaultCultivationProgressBarWidth;
    private float _defaultValidationPanelWidth;
    private float _defaultActionModeWidth;
    private float _defaultLevelOptionWidth;
    private float _defaultZoneLabelWidth;
    private float _defaultRealmStageLabelWidth;

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
        _exploreProgressBar = GetNode<ProgressBar>("Chrome/ExploreProgressBar");
        _cultivationLabel = GetNode<Label>("Chrome/CultivationLabel");
        _cultivationProgressBar = GetNode<ProgressBar>("Chrome/CultivationProgressBar");
        _breakthroughButton = GetNode<Button>("Chrome/BreakthroughButton");
        _battleTrack = GetNode<Panel>("Chrome/BattleTrack");
        _validationPanel = GetNode<Panel>("Chrome/ConfigValidationPanel");
        _validationTitleLabel = GetNode<Label>("Chrome/ConfigValidationPanel/TitleLabel");
        _validationBodyLabel = GetNode<RichTextLabel>("Chrome/ConfigValidationPanel/BodyLabel");
        _actionModeOptionButton = GetNodeOrNull<OptionButton>("Chrome/ActionModeOptionButton");
        _levelOptionButton = GetNodeOrNull<OptionButton>("Chrome/LevelOptionButton");

        _defaultExploreProgressBarWidth = _exploreProgressBar.Size.X;
        _defaultCultivationProgressBarWidth = _cultivationProgressBar.Size.X;
        _defaultValidationPanelWidth = _validationPanel.Size.X;
        _defaultActionModeWidth = _actionModeOptionButton?.Size.X ?? 130.0f;
        _defaultLevelOptionWidth = _levelOptionButton?.Size.X ?? 220.0f;
        _defaultZoneLabelWidth = _zoneLabel.Size.X;
        _defaultRealmStageLabelWidth = _realmStageLabel.Size.X;

        _dragHandle.GuiInput += OnDragHandleGuiInput;
        _resizeHandle.GuiInput += OnResizeHandleGuiInput;
        _bookButton.Pressed += () => EmitSignal(SignalName.BookButtonPressed);
        _dragHandle.Text = UiText.DragHandle;
        _resizeHandle.Text = UiText.ResizeHandle;
        _bookButton.Text = UiText.BookButton;
        _zoneLabel.Visible = true;
        _activityRateLabel.Visible = false;
        _validationPanel.Visible = false;

        _bottomMargin = Mathf.Max(0.0f, MinBottomMargin);
        _fixedBottomY = GetBottomLockedY();
        Position = new Vector2(Position.X, _fixedBottomY);
        UpdateRightAnchoredLayout();
    }

    public override void _Process(double delta)
    {
        float maxX = Mathf.Max(0.0f, GetViewportRect().Size.X - Size.X);
        float clampedX = Mathf.Clamp(Position.X, 0.0f, maxX);

        if (!LockToBottom)
        {
            if (!Mathf.IsEqualApprox(Position.X, clampedX))
            {
                Position = new Vector2(clampedX, Position.Y);
            }
            return;
        }

        float nextY = GetBottomLockedY();
        _fixedBottomY = nextY;
        if (!Mathf.IsEqualApprox(Position.X, clampedX) || !Mathf.IsEqualApprox(Position.Y, nextY))
        {
            Position = new Vector2(clampedX, nextY);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                TryBeginBlankAreaDrag(mouseButton.GlobalPosition);
            }
            else
            {
                if (_isDragging || _isResizing)
                {
                    EmitSignal(SignalName.LayoutChanged, Position.X, Size.X);
                }

                _isDragging = false;
                _isResizing = false;
            }

            return;
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
            if (mouseButton.Pressed)
            {
                BeginDrag(mouseButton.GlobalPosition);
            }
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

    private void UpdateRightAnchoredLayout()
    {
        float rightMargin = 12.0f;
        float controlRowY = _battleTrack.Position.Y + _battleTrack.Size.Y + 4.0f;
        float textRowY = controlRowY + 34.0f;
        float barRowY = textRowY + 22.0f;
        float bottomTextY = controlRowY + 62.0f;
        bool compactLayout = Size.X <= 920.0f;

        float exploreBarWidth = compactLayout
            ? Mathf.Clamp(Size.X * 0.22f, 156.0f, _defaultExploreProgressBarWidth)
            : _defaultExploreProgressBarWidth;
        float cultivationBarWidth = compactLayout
            ? Mathf.Clamp(Size.X * 0.21f, 148.0f, _defaultCultivationProgressBarWidth)
            : _defaultCultivationProgressBarWidth;
        float actionModeWidth = compactLayout
            ? Mathf.Min(_defaultActionModeWidth, 112.0f)
            : _defaultActionModeWidth;
        float levelOptionWidth = compactLayout
            ? Mathf.Min(_defaultLevelOptionWidth, 156.0f)
            : _defaultLevelOptionWidth;

        _resizeHandle.Position = new Vector2(Size.X - _resizeHandle.Size.X - rightMargin, _resizeHandle.Position.Y);
        _exploreProgressBar.Size = new Vector2(exploreBarWidth, _exploreProgressBar.Size.Y);
        _exploreProgressBar.Position = new Vector2(Size.X - _exploreProgressBar.Size.X - rightMargin, barRowY);

        _breakthroughButton.Position = new Vector2(_exploreProgressBar.Position.X - _breakthroughButton.Size.X - 10.0f, barRowY - 2.0f);
        _cultivationProgressBar.Size = new Vector2(cultivationBarWidth, _cultivationProgressBar.Size.Y);
        _cultivationProgressBar.Position = new Vector2(_breakthroughButton.Position.X - _cultivationProgressBar.Size.X - 10.0f, barRowY);
        _cultivationLabel.Position = new Vector2(_cultivationProgressBar.Position.X, textRowY);

        float zoneMinWidth = 92.0f;
        float zoneLeftLimit = _cultivationLabel.Position.X + _cultivationLabel.Size.X + 12.0f;
        float zoneWidth = Mathf.Clamp(Size.X - zoneLeftLimit - rightMargin, zoneMinWidth, _defaultZoneLabelWidth);
        _zoneLabel.Size = new Vector2(zoneWidth, _zoneLabel.Size.Y);
        _zoneLabel.Position = new Vector2(Size.X - zoneWidth - rightMargin, textRowY);

        float realmRightLimit = _cultivationProgressBar.Position.X - 12.0f;
        float realmWidth = Mathf.Clamp(realmRightLimit - _realmStageLabel.Position.X, 160.0f, _defaultRealmStageLabelWidth);
        _realmStageLabel.Size = new Vector2(realmWidth, _realmStageLabel.Size.Y);
        _realmStageLabel.Position = new Vector2(_realmStageLabel.Position.X, bottomTextY);
        _activityRateLabel.Position = new Vector2(_activityRateLabel.Position.X, bottomTextY);

        float rightBlockStartX = _cultivationProgressBar.Position.X;
        _battleTrack.Size = new Vector2(Mathf.Max(320.0f, rightBlockStartX - _battleTrack.Position.X - 12.0f), _battleTrack.Size.Y);

        float optionStartX = _battleTrack.Position.X + 8.0f;
        if (_actionModeOptionButton != null)
        {
            _actionModeOptionButton.Size = new Vector2(actionModeWidth, _actionModeOptionButton.Size.Y);
            _actionModeOptionButton.Position = new Vector2(optionStartX, controlRowY);
        }

        if (_levelOptionButton != null)
        {
            float availableLevelWidth = Mathf.Max(112.0f, rightBlockStartX - optionStartX - 8.0f);
            _levelOptionButton.Size = new Vector2(Mathf.Min(levelOptionWidth, availableLevelWidth), _levelOptionButton.Size.Y);
            float leftX = _battleTrack.Position.X + 8.0f;
            _levelOptionButton.Position = new Vector2(leftX, textRowY - 2.0f);
        }

        if (_validationPanel != null)
        {
            float validationMinX = optionStartX;
            if (_actionModeOptionButton != null)
            {
                validationMinX = _actionModeOptionButton.Position.X + _actionModeOptionButton.Size.X + 8.0f;
            }

            float availableValidationWidth = rightBlockStartX - validationMinX - 8.0f;
            if (compactLayout && availableValidationWidth < 88.0f)
            {
                _validationPanel.Visible = false;
            }
            else
            {
                float targetValidationWidth = compactLayout
                    ? Mathf.Clamp(availableValidationWidth, 88.0f, _defaultValidationPanelWidth)
                    : _defaultValidationPanelWidth;
                _validationPanel.Visible = true;
                _validationPanel.Size = new Vector2(targetValidationWidth, _validationPanel.Size.Y);
                _validationPanel.Position = new Vector2(validationMinX, controlRowY);
                _validationTitleLabel.Size = new Vector2(Mathf.Max(0.0f, targetValidationWidth - 16.0f), _validationTitleLabel.Size.Y);
                _validationBodyLabel.Size = new Vector2(Mathf.Max(0.0f, targetValidationWidth - 16.0f), _validationBodyLabel.Size.Y);
            }
        }
    }

    private float GetBottomLockedY()
    {
        float y = GetViewportRect().Size.Y - Size.Y - _bottomMargin;
        return Mathf.Max(0.0f, y);
    }

    private void TryBeginBlankAreaDrag(Vector2 pointerGlobalPosition)
    {
        if (!Visible)
        {
            return;
        }

        var windowRect = new Rect2(Position, Size);
        if (!windowRect.HasPoint(pointerGlobalPosition))
        {
            return;
        }

        Control? hovered = GetViewport().GuiGetHoveredControl();
        if (hovered == null || !IsAncestorOf(hovered))
        {
            BeginDrag(pointerGlobalPosition);
            return;
        }

        if (hovered == _resizeHandle)
        {
            return;
        }

        if (hovered == _dragHandle)
        {
            BeginDrag(pointerGlobalPosition);
            return;
        }

        if (IsInteractionTarget(hovered))
        {
            return;
        }

        BeginDrag(pointerGlobalPosition);
    }

    private void BeginDrag(Vector2 pointerGlobalPosition)
    {
        _isDragging = true;
        _lastMousePos = pointerGlobalPosition;
    }

    private bool IsInteractionTarget(Control control)
    {
        return control is BaseButton
            || control is OptionButton
            || control is RichTextLabel
            || control is ProgressBar;
    }
}
