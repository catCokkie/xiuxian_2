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
    private OptionButton? _actionModeOptionButton;
    private OptionButton? _levelOptionButton;

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
        _actionModeOptionButton = GetNodeOrNull<OptionButton>("Chrome/ActionModeOptionButton");
        _levelOptionButton = GetNodeOrNull<OptionButton>("Chrome/LevelOptionButton");

        _dragHandle.GuiInput += OnDragHandleGuiInput;
        _resizeHandle.GuiInput += OnResizeHandleGuiInput;
        _bookButton.Pressed += () => EmitSignal(SignalName.BookButtonPressed);
        _dragHandle.Text = UiText.DragHandle;
        _resizeHandle.Text = UiText.ResizeHandle;
        _bookButton.Text = UiText.BookButton;
        _zoneLabel.Visible = false;

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

    private void UpdateRightAnchoredLayout()
    {
        float rightMargin = 12.0f;
        float controlRowY = _battleTrack.Position.Y + _battleTrack.Size.Y + 4.0f;
        float textRowY = controlRowY + 34.0f;
        float barRowY = textRowY + 22.0f;
        float bottomTextY = controlRowY + 62.0f;

        _resizeHandle.Position = new Vector2(Size.X - _resizeHandle.Size.X - rightMargin, _resizeHandle.Position.Y);
        _zoneLabel.Position = new Vector2(Size.X - _zoneLabel.Size.X - rightMargin, textRowY);
        _exploreProgressBar.Position = new Vector2(Size.X - _exploreProgressBar.Size.X - rightMargin, barRowY);
        _realmStageLabel.Position = new Vector2(_realmStageLabel.Position.X, bottomTextY);
        _activityRateLabel.Position = new Vector2(_activityRateLabel.Position.X, bottomTextY);

        _breakthroughButton.Position = new Vector2(_exploreProgressBar.Position.X - _breakthroughButton.Size.X - 10.0f, barRowY - 2.0f);
        _cultivationProgressBar.Position = new Vector2(_breakthroughButton.Position.X - _cultivationProgressBar.Size.X - 10.0f, barRowY);
        _cultivationLabel.Position = new Vector2(_cultivationProgressBar.Position.X, textRowY);

        float rightBlockStartX = _cultivationProgressBar.Position.X;
        _battleTrack.Size = new Vector2(Mathf.Max(320.0f, rightBlockStartX - _battleTrack.Position.X - 12.0f), _battleTrack.Size.Y);

        float optionStartX = _battleTrack.Position.X + 8.0f;
        if (_actionModeOptionButton != null)
        {
            _actionModeOptionButton.Position = new Vector2(optionStartX, controlRowY);
        }

        if (_levelOptionButton != null)
        {
            float leftX = _battleTrack.Position.X + 8.0f;
            _levelOptionButton.Position = new Vector2(leftX, textRowY - 2.0f);
        }

        if (_validationPanel != null)
        {
            float validationWidth = _validationPanel.Size.X;
            float x = Mathf.Max(_battleTrack.Position.X + 8.0f, rightBlockStartX - validationWidth - 10.0f);
            _validationPanel.Position = new Vector2(x, controlRowY);
        }
    }

    private float GetBottomLockedY()
    {
        return GetViewportRect().Size.Y - Size.Y - _bottomMargin;
    }
}
