using Godot;

public partial class SubmenuWindowController : Control
{
    [Signal]
    public delegate void VisibilityChangedEventHandler(bool isVisible);

    [Export] public Vector2 DefaultPosition = new(320, 120);
    [Export] public float OpenDuration = 0.22f;

    private Tween? _activeTween;
    private bool _isDragging;
    private Vector2 _dragOffset;

    public override void _Ready()
    {
        if (Position == Vector2.Zero)
        {
            Position = DefaultPosition;
        }

        PivotOffset = Size / 2.0f;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton
            && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                TryBeginDrag(mouseButton.GlobalPosition);
            }
            else
            {
                _isDragging = false;
            }

            return;
        }

        if (!_isDragging || @event is not InputEventMouseMotion mouseMotion)
        {
            return;
        }

        Vector2 viewportSize = GetViewportRect().Size;
        float maxX = Mathf.Max(0.0f, viewportSize.X - Size.X);
        float maxY = Mathf.Max(0.0f, viewportSize.Y - Size.Y);

        Vector2 target = mouseMotion.GlobalPosition - _dragOffset;
        Position = new Vector2(
            Mathf.Clamp(target.X, 0.0f, maxX),
            Mathf.Clamp(target.Y, 0.0f, maxY)
        );
    }

    public void ToggleVisible()
    {
        if (Visible)
        {
            AnimateClose();
            return;
        }

        AnimateOpen();
    }

    public void SetVisibleImmediate(bool isVisible)
    {
        Visible = isVisible;
        Modulate = new Color(1, 1, 1, isVisible ? 1.0f : 0.0f);
        Scale = isVisible ? Vector2.One : new Vector2(0.96f, 1.0f);
        EmitSignal(SignalName.VisibilityChanged, isVisible);
    }

    private void AnimateOpen()
    {
        StopActiveTween();
        Visible = true;
        Scale = new Vector2(0.94f, 1.0f);
        Modulate = new Color(1, 1, 1, 0.0f);

        _activeTween = CreateTween();
        _activeTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        _activeTween.TweenProperty(this, "scale", Vector2.One, OpenDuration);
        _activeTween.Parallel().TweenProperty(this, "modulate", Colors.White, OpenDuration);
        EmitSignal(SignalName.VisibilityChanged, true);
    }

    private void AnimateClose()
    {
        StopActiveTween();
        _activeTween = CreateTween();
        _activeTween.SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
        _activeTween.TweenProperty(this, "scale", new Vector2(0.96f, 1.0f), 0.16f);
        _activeTween.Parallel().TweenProperty(this, "modulate", new Color(1, 1, 1, 0.0f), 0.16f);
        _activeTween.Finished += () =>
        {
            Visible = false;
            EmitSignal(SignalName.VisibilityChanged, false);
        };
    }

    private void StopActiveTween()
    {
        if (_activeTween == null)
        {
            return;
        }

        _activeTween.Kill();
        _activeTween = null;
    }

    private void TryBeginDrag(Vector2 pointerGlobalPosition)
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

        Control hovered = GetViewport().GuiGetHoveredControl();
        if (hovered != null && IsNodeInteractionTarget(hovered))
        {
            return;
        }

        _isDragging = true;
        _dragOffset = pointerGlobalPosition - Position;
        StopActiveTween();
    }

    private bool IsNodeInteractionTarget(Control control)
    {
        if (!IsAncestorOf(control))
        {
            return false;
        }

        return control is BaseButton
            || control is RichTextLabel
            || control is LineEdit
            || control is TextEdit
            || control is ItemList
            || control is Tree;
    }
}
