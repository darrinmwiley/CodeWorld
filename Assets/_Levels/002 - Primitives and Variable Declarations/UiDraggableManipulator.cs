using UnityEngine;
using UnityEngine.UIElements;

public class UIDraggableManipulator : PointerManipulator
{
    private Vector2 _startMousePos;
    private Vector2 _startWindowPos;
    private VisualElement _windowToMove;
    private bool _isDragging;

    public UIDraggableManipulator(VisualElement windowToMove) 
    { 
        _windowToMove = windowToMove; 
        _isDragging = false;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        // This is where the magic happens. If this doesn't run, nothing works.
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (_windowToMove == null) return;

        _isDragging = true;
        _startMousePos = evt.position;
        _startWindowPos = new Vector2(_windowToMove.resolvedStyle.left, _windowToMove.resolvedStyle.top);

        target.CapturePointer(evt.pointerId);
        _windowToMove.BringToFront();
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_isDragging || !target.HasPointerCapture(evt.pointerId)) return;

        Vector2 diff = (Vector2)evt.position - _startMousePos;

        _windowToMove.style.left = _startWindowPos.x + diff.x;
        _windowToMove.style.top = _startWindowPos.y + diff.y;

        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!_isDragging || !target.HasPointerCapture(evt.pointerId)) return;

        _isDragging = false;
        target.ReleasePointer(evt.pointerId);
        evt.StopPropagation();
    }
}