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
        
        // Use resolvedStyle to get the actual current rendered size/pos
        _startSize = new Vector2(_targetWindow.resolvedStyle.width, _targetWindow.resolvedStyle.height);
        _startPos = new Vector2(_targetWindow.resolvedStyle.left, _targetWindow.resolvedStyle.top);

        target.CapturePointer(evt.pointerId);
        _onPointerDown?.Invoke();
        evt.StopImmediatePropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_active || !target.HasPointerCapture(evt.pointerId)) return;

        Vector2 diff = (Vector2)evt.position - _startMousePos;
        
        // Get the hard constraints from the style (set by WindowContainerController)
        float minW = _targetWindow.resolvedStyle.minWidth.value;
        float minH = _targetWindow.resolvedStyle.minHeight.value;

        switch (_direction)
        {
            case ResizeDirection.Right:
                _targetWindow.style.width = Mathf.Max(minW, _startSize.x + diff.x);
                break;

            case ResizeDirection.Bottom:
                _targetWindow.style.height = Mathf.Max(minH, _startSize.y + diff.y);
                break;

            case ResizeDirection.Left:
                float newWidthL = Mathf.Max(minW, _startSize.x - diff.x);
                float actualDiffL = newWidthL - _startSize.x; 
                _targetWindow.style.width = newWidthL;
                // Move 'left' only by the amount the width actually changed
                _targetWindow.style.left = _startPos.x - actualDiffL;
                break;

            case ResizeDirection.Top:
                float newHeightT = Mathf.Max(minH, _startSize.y - diff.y);
                float actualDiffT = newHeightT - _startSize.y;
                _targetWindow.style.height = newHeightT;
                // Move 'top' only by the amount the height actually changed
                _targetWindow.style.top = _startPos.y - actualDiffT;
                break;

            case ResizeDirection.TopLeft:
                float nWL = Mathf.Max(minW, _startSize.x - diff.x);
                float nHT = Mathf.Max(minH, _startSize.y - diff.y);
                float actDiffWL = nWL - _startSize.x;
                float actDiffHT = nHT - _startSize.y;
                
                _targetWindow.style.width = nWL;
                _targetWindow.style.height = nHT;
                _targetWindow.style.left = _startPos.x - actDiffWL;
                _targetWindow.style.top = _startPos.y - actDiffHT;
                break;

            case ResizeDirection.TopRight:
                float nHTR = Mathf.Max(minH, _startSize.y - diff.y);
                float actDiffHTR = nHTR - _startSize.y;
                
                _targetWindow.style.width = Mathf.Max(minW, _startSize.x + diff.x);
                _targetWindow.style.height = nHTR;
                _targetWindow.style.top = _startPos.y - actDiffHTR;
                break;

            case ResizeDirection.BottomLeft:
                float nWBL = Mathf.Max(minW, _startSize.x - diff.x);
                float actDiffWBL = nWBL - _startSize.x;
                
                _targetWindow.style.width = nWBL;
                _targetWindow.style.height = Mathf.Max(minH, _startSize.y + diff.y);
                _targetWindow.style.left = _startPos.x - actDiffWBL;
                break;

            case ResizeDirection.BottomRight:
                _targetWindow.style.width = Mathf.Max(minW, _startSize.x + diff.x);
                _targetWindow.style.height = Mathf.Max(minH, _startSize.y + diff.y);
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