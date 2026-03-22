using UnityEngine;
using UnityEngine.UIElements;
using System;

public class UIDraggableManipulator : PointerManipulator
{
    private Vector2 _startMousePos;
    private Vector2 _startWindowPos;
    private bool _active;
    private VisualElement _targetWindow;
    private Action _onPointerDown;

    public UIDraggableManipulator(VisualElement targetWindow, Action onPointerDown = null)
    {
        _targetWindow = targetWindow;
        _onPointerDown = onPointerDown;
        _active = false;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        // Ensure the target element can intercept mouse/pointer events
        target.pickingMode = PickingMode.Position;

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
        if (_active) return;

        _startMousePos = (Vector2)evt.position;
        _startWindowPos = new Vector2(_targetWindow.resolvedStyle.left, _targetWindow.resolvedStyle.top);
        
        _active = true;
        target.CapturePointer(evt.pointerId);
        
        _onPointerDown?.Invoke();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_active || !target.HasPointerCapture(evt.pointerId)) return;

        Vector2 diff = (Vector2)evt.position - _startMousePos;
        
        _targetWindow.style.left = _startWindowPos.x + diff.x;
        _targetWindow.style.top = _startWindowPos.y + diff.y;
        
        evt.StopImmediatePropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!_active || !target.HasPointerCapture(evt.pointerId)) return;

        _active = false;
        target.ReleasePointer(evt.pointerId);
        evt.StopImmediatePropagation();
    }
}