using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIToolkitMouseListenerMono : MonoBehaviour
{
    public bool isMouseDown { get; private set; }
    public bool isMouseDragging { get; private set; }
    public Vector2 mouseDownPosition { get; private set; }
    public Vector2 currentMousePosition { get; private set; }

    public delegate void LegacyMouseAction();

    readonly List<LegacyMouseAction> _mouseDownHandlers = new();
    readonly List<LegacyMouseAction> _mouseUpHandlers = new();
    readonly List<LegacyMouseAction> _mouseDragHandlers = new();

    VisualElement _target;
    int _activePointerId = -1;

    public void AddMouseDownHandler(LegacyMouseAction a) => _mouseDownHandlers.Add(a);
    public void AddMouseUpHandler(LegacyMouseAction a) => _mouseUpHandlers.Add(a);
    public void AddMouseDragHandler(LegacyMouseAction a) => _mouseDragHandlers.Add(a);

    public void Bind(VisualElement target)
    {
        Unbind();

        _target = target;
        if (_target == null) return;

        _target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        _target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    public void Unbind()
    {
        if (_target == null) return;

        _target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        _target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        _target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        _target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

        _target = null;
        _activePointerId = -1;
        isMouseDown = false;
        isMouseDragging = false;
    }

    void OnDisable() => Unbind();

    void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button != (int)MouseButton.LeftMouse) return;

        isMouseDown = true;
        isMouseDragging = false;
        _activePointerId = evt.pointerId;

        _target.CapturePointer(_activePointerId);

        UpdateProportions(evt.localPosition);
        mouseDownPosition = currentMousePosition;

        foreach (var a in _mouseDownHandlers) a();
        evt.StopPropagation();
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        if (!isMouseDown) return;
        if (_activePointerId != evt.pointerId) return;

        isMouseDown = false;
        isMouseDragging = false;

        UpdateProportions(evt.localPosition);

        if (_target.HasPointerCapture(_activePointerId))
            _target.ReleasePointer(_activePointerId);

        _activePointerId = -1;

        foreach (var a in _mouseUpHandlers) a();
        evt.StopPropagation();
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isMouseDown) return;
        if (_activePointerId != evt.pointerId) return;

        isMouseDragging = true;
        UpdateProportions(evt.localPosition);

        foreach (var a in _mouseDragHandlers) a();
        evt.StopPropagation();
    }

    void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        isMouseDown = false;
        isMouseDragging = false;
        _activePointerId = -1;
    }

    void UpdateProportions(Vector2 localPos)
    {
        float w = Mathf.Max(1f, _target.contentRect.width);
        float h = Mathf.Max(1f, _target.contentRect.height);

        float px = Mathf.Clamp01(localPos.x / w);
        float py = Mathf.Clamp01(1f - (localPos.y / h)); 

        currentMousePosition = new Vector2(px, py);

        // DIAGNOSTIC LOG
        Debug.Log($"[MouseListener] LocalPos: {localPos} | ContentRect: {w}x{h} | Normalized: {currentMousePosition}");
    }
}