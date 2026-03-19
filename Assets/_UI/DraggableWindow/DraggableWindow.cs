using System;
using UnityEngine;
using UnityEngine.UIElements;

public class DraggableWindow : MonoBehaviour, IFocusable
{
    private VisualElement _window;
    public Texture2D diag1, diag2, vert, horiz; 
    public Vector2 hotSpot = new Vector2(16, 16);
    public RenderTexture cameraRenderTexture;
    public ConsoleController console;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _window = root.Q<VisualElement>("OuterBorder");
        var dragHandle = root.Q<VisualElement>("DragHandle");

        if (_window != null)
        {
            _window.style.display = DisplayStyle.None;
            _window.RegisterCallback<GeometryChangedEvent>(OnInitialLayout);
            Action bringToFront = console != null ? console.BringToFront : null;

            if (dragHandle != null)
                dragHandle.AddManipulator(new UIDraggableManipulator(_window, bringToFront));

            SetupHandle("LeftResize", horiz, ResizeDirection.Left, bringToFront);
            SetupHandle("RightResize", horiz, ResizeDirection.Right, bringToFront);
            SetupHandle("TopResize", vert, ResizeDirection.Top, bringToFront);
            SetupHandle("BottomResize", vert, ResizeDirection.Bottom, bringToFront);
            SetupHandle("TopLeftResize", diag1, ResizeDirection.TopLeft, bringToFront);
            SetupHandle("TopRightResize", diag2, ResizeDirection.TopRight, bringToFront);
            SetupHandle("BottomLeftResize", diag2, ResizeDirection.BottomLeft, bringToFront);
            SetupHandle("BottomRightResize", diag1, ResizeDirection.BottomRight, bringToFront);

            var content = root.Q<VisualElement>("Content");
            if (content != null && cameraRenderTexture != null)
            {
                content.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(cameraRenderTexture));
                content.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
                content.pickingMode = PickingMode.Ignore;
            }
        }
    }

    private void SetupHandle(string name, Texture2D icon, ResizeDirection direction, Action bringToFront)
    {
        var handle = _window.Q<VisualElement>(name);
        if (handle == null) return;
        handle.AddManipulator(new UIResizableManipulator(_window, direction, bringToFront));
        handle.RegisterCallback<PointerEnterEvent>(e => UnityEngine.Cursor.SetCursor(icon, hotSpot, CursorMode.ForceSoftware));
        handle.RegisterCallback<PointerLeaveEvent>(e => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
    }

    private void OnInitialLayout(GeometryChangedEvent evt)
    {
        var parent = _window.parent.layout;
        _window.style.left = (parent.width - evt.newRect.width) / 2;
        _window.style.top = (parent.height - evt.newRect.height) / 2;
        _window.UnregisterCallback<GeometryChangedEvent>(OnInitialLayout);
    }

    public void OnFocus()
    {
        _window.style.display = DisplayStyle.Flex;
        if (console != null) console.BringToFront();
    }

    public void OnDefocus() => _window.style.display = DisplayStyle.None;
}