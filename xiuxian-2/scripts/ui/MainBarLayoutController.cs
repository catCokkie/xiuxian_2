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
    private ProgressBar _exploreProgressBar = null!;
    private Panel _battleTrack = null!;

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
        _exploreProgressBar = GetNode<ProgressBar>("Chrome/ExploreProgressBar");
        _battleTrack = GetNode<Panel>("Chrome/BattleTrack");

        _dragHandle.GuiInput += OnDragHandleGuiInput;
        _resizeHandle.GuiInput += OnResizeHandleGuiInput;
        _bookButton.Pressed += () => EmitSignal(SignalName.BookButtonPressed);

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

        _resizeHandle.Position = new Vector2(Size.X - _resizeHandle.Size.X - rightMargin, _resizeHandle.Position.Y);
        _zoneLabel.Position = new Vector2(Size.X - _zoneLabel.Size.X - rightMargin, _zoneLabel.Position.Y);
        _exploreProgressBar.Position = new Vector2(Size.X - _exploreProgressBar.Size.X - rightMargin, _exploreProgressBar.Position.Y);
        _battleTrack.Size = new Vector2(Mathf.Max(320.0f, _exploreProgressBar.Position.X - _battleTrack.Position.X - 12.0f), _battleTrack.Size.Y);
    }

    private float GetBottomLockedY()
    {
        return GetViewportRect().Size.Y - Size.Y - _bottomMargin;
    }
}
