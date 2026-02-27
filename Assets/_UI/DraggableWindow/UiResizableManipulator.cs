using UnityEngine;
using UnityEngine.UIElements;
using System;

public class UIResizableManipulator : PointerManipulator
{
    private Vector2 _startMousePos;
    private Rect _startRect;
    private VisualElement _targetWindow;
    private bool _isResizing;
    private ResizeDirection _direction;
    private Action _bringToFrontAction;

    public UIResizableManipulator(VisualElement targetWindow, ResizeDirection direction, Action _bringToFrontAction)
    {
        _targetWindow = targetWindow;
        _direction = direction;
        _isResizing = false;
        this._bringToFrontAction = _bringToFrontAction;
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
        _isResizing = true;
        _startMousePos = evt.position;
        _startRect = new Rect(_targetWindow.resolvedStyle.left, _targetWindow.resolvedStyle.top, 
                              _targetWindow.resolvedStyle.width, _targetWindow.resolvedStyle.height);
        
        target.CapturePointer(evt.pointerId);
        _bringToFrontAction?.Invoke();
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_isResizing || !target.HasPointerCapture(evt.pointerId)) return;

        Vector2 delta = (Vector2)evt.position - _startMousePos;
        float newWidth = _startRect.width;
        float newHeight = _startRect.height;
        float newLeft = _startRect.x;
        float newTop = _startRect.y;

        // Horizontal Logic
        if (_direction == ResizeDirection.Right || _direction == ResizeDirection.BottomRight || _direction == ResizeDirection.TopRight)
        {
            newWidth = Mathf.Max(200, _startRect.width + delta.x);
        }
        else if (_direction == ResizeDirection.Left || _direction == ResizeDirection.BottomLeft || _direction == ResizeDirection.TopLeft)
        {
            float maxDeltaX = _startRect.width - 200;
            float actualDeltaX = Mathf.Min(delta.x, maxDeltaX);
            newWidth = _startRect.width - actualDeltaX;
            newLeft = _startRect.x + actualDeltaX;
        }

        // Vertical Logic
        if (_direction == ResizeDirection.Bottom || _direction == ResizeDirection.BottomRight || _direction == ResizeDirection.BottomLeft)
        {
            newHeight = Mathf.Max(150, _startRect.height + delta.y);
        }
        else if (_direction == ResizeDirection.Top || _direction == ResizeDirection.TopRight || _direction == ResizeDirection.TopLeft)
        {
            float maxDeltaY = _startRect.height - 150;
            float actualDeltaY = Mathf.Min(delta.y, maxDeltaY);
            newHeight = _startRect.height - actualDeltaY;
            newTop = _startRect.y + actualDeltaY;
        }

        _targetWindow.style.width = newWidth;
        _targetWindow.style.height = newHeight;
        _targetWindow.style.left = newLeft;
        _targetWindow.style.top = newTop;
        
        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        _isResizing = false;
        target.ReleasePointer(evt.pointerId);
        evt.StopPropagation();
    }
}