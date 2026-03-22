using UnityEngine;
using UnityEngine.UIElements;
using System;

public class UIResizableManipulator : PointerManipulator
{
    private Vector2 _startMousePos;
    private Vector2 _startSize;
    private Vector2 _startPos;
    private bool _active;
    private VisualElement _targetWindow;
    private ResizeDirection _direction;
    private Action _onPointerDown;

    public UIResizableManipulator(VisualElement targetWindow, ResizeDirection direction, Action onPointerDown = null)
    {
        _targetWindow = targetWindow;
        _direction = direction;
        _onPointerDown = onPointerDown;
    }

    protected override void RegisterCallbacksOnTarget()
    {
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

        _active = true;
        _startMousePos = evt.position;
        _startSize = new Vector2(_targetWindow.resolvedStyle.width, _targetWindow.resolvedStyle.height);
        _startPos = new Vector2(_targetWindow.resolvedStyle.left, _targetWindow.resolvedStyle.top);

        target.CapturePointer(evt.pointerId);
        
        // This allows the FocusManager to register the window focus
        _onPointerDown?.Invoke();
        
        // PROPAGATION REMOVED: This allows FocusManager.cs to see the click 
        // and correctly handle cursor locking/unlocking.
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_active || !target.HasPointerCapture(evt.pointerId)) return;

        Vector2 diff = (Vector2)evt.position - _startMousePos;
        const float minSize = 100f;

        switch (_direction)
        {
            case ResizeDirection.Right:
                _targetWindow.style.width = Mathf.Max(minSize, _startSize.x + diff.x);
                break;
            case ResizeDirection.Bottom:
                _targetWindow.style.height = Mathf.Max(minSize, _startSize.y + diff.y);
                break;
            case ResizeDirection.Left:
                float newWidthL = Mathf.Max(minSize, _startSize.x - diff.x);
                _targetWindow.style.width = newWidthL;
                _targetWindow.style.left = _startPos.x + (_startSize.x - newWidthL);
                break;
            case ResizeDirection.Top:
                float newHeightT = Mathf.Max(minSize, _startSize.y - diff.y);
                _targetWindow.style.height = newHeightT;
                _targetWindow.style.top = _startPos.y + (_startSize.y - newHeightT);
                break;
            case ResizeDirection.TopLeft:
                float nWL = Mathf.Max(minSize, _startSize.x - diff.x);
                float nHT = Mathf.Max(minSize, _startSize.y - diff.y);
                _targetWindow.style.width = nWL;
                _targetWindow.style.height = nHT;
                _targetWindow.style.left = _startPos.x + (_startSize.x - nWL);
                _targetWindow.style.top = _startPos.y + (_startSize.y - nHT);
                break;
            case ResizeDirection.TopRight:
                float nHTR = Mathf.Max(minSize, _startSize.y - diff.y);
                _targetWindow.style.width = Mathf.Max(minSize, _startSize.x + diff.x);
                _targetWindow.style.height = nHTR;
                _targetWindow.style.top = _startPos.y + (_startSize.y - nHTR);
                break;
            case ResizeDirection.BottomLeft:
                float nWBL = Mathf.Max(minSize, _startSize.x - diff.x);
                _targetWindow.style.width = nWBL;
                _targetWindow.style.height = Mathf.Max(minSize, _startSize.y + diff.y);
                _targetWindow.style.left = _startPos.x + (_startSize.x - nWBL);
                break;
            case ResizeDirection.BottomRight:
                _targetWindow.style.width = Mathf.Max(minSize, _startSize.x + diff.x);
                _targetWindow.style.height = Mathf.Max(minSize, _startSize.y + diff.y);
                break;
        }

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