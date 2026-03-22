using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Draggable2PaneWindow : MonoBehaviour, IFocusable
{
    private VisualElement _window;

    [Header("Cursor Icons")]
    public Texture2D diag1, diag2, vert, horiz;
    public Vector2 hotSpot = new Vector2(16, 16);

    [Header("Pane Render Textures")]
    public RenderTexture leftOrTopRenderTexture;
    public RenderTexture rightOrBottomRenderTexture;

    [Header("Consoles")]
    public ConsoleController consoleA;
    public ConsoleController consoleB;

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;

        var root = uiDoc.rootVisualElement;
        
        // Ensure the root container itself doesn't block the world
        root.pickingMode = PickingMode.Ignore;

        _window = root.Q<VisualElement>("OuterBorder");
        if (_window == null) return;

        _window.style.display = DisplayStyle.None;
        _window.pickingMode = PickingMode.Position; 

        // --- THE CLICK SHIELD ---
        // This ensures ANY click anywhere on the window is consumed 
        // so the FirstPersonLook script doesn't see a "Hardware Click".
        _window.RegisterCallback<PointerDownEvent>(evt => {
            evt.StopImmediatePropagation();
        });

        // Center the window on first show
        _window.RegisterCallback<GeometryChangedEvent>(OnInitialLayout);

        // Logic to keep consoles on top
        Action bringToFront = () => {
            if (consoleA != null) consoleA.BringToFront();
            if (consoleB != null) consoleB.BringToFront();
        };

        // Setup Drag Handle
        var dragHandle = _window.Q<VisualElement>("DragHandle");
        if (dragHandle != null)
        {
            dragHandle.pickingMode = PickingMode.Position;
            dragHandle.AddManipulator(new UIDraggableManipulator(_window, bringToFront));
        }

        // Setup Resize Handles
        SetupHandle("LeftResize", horiz, ResizeDirection.Left, bringToFront);
        SetupHandle("RightResize", horiz, ResizeDirection.Right, bringToFront);
        SetupHandle("TopResize", vert, ResizeDirection.Top, bringToFront);
        SetupHandle("BottomResize", vert, ResizeDirection.Bottom, bringToFront);
        SetupHandle("BottomRightResize", diag1, ResizeDirection.BottomRight, bringToFront);

        // Bind Panes to RenderTextures
        var panes = root.Query<VisualElement>("Content").ToList();
        if (panes.Count >= 1 && leftOrTopRenderTexture != null)
            panes[0].style.backgroundImage = new StyleBackground(Background.FromRenderTexture(leftOrTopRenderTexture));
        if (panes.Count >= 2 && rightOrBottomRenderTexture != null)
            panes[1].style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rightOrBottomRenderTexture));
    }

    private void SetupHandle(string name, Texture2D icon, ResizeDirection direction, Action bringToFront)
    {
        var handle = _window.Q<VisualElement>(name);
        if (handle == null) return;

        handle.pickingMode = PickingMode.Position;
        
        // Ensure you have updated UIResizableManipulator to use worldPosition as well
        handle.AddManipulator(new UIResizableManipulator(_window, direction, bringToFront));

        // Use explicit UnityEngine.Cursor to ensure icons show up correctly
        handle.RegisterCallback<PointerEnterEvent>(e => 
        {
            UnityEngine.Cursor.SetCursor(icon, hotSpot, CursorMode.Auto);
        });

        handle.RegisterCallback<PointerLeaveEvent>(e => 
        {
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        });
    }

    private void OnInitialLayout(GeometryChangedEvent evt)
    {
        var parent = _window.parent.layout;
        _window.style.left = (parent.width - evt.newRect.width) / 2;
        _window.style.top = (parent.height - evt.newRect.height) / 2;
        _window.UnregisterCallback<GeometryChangedEvent>(OnInitialLayout);
    }

    // --- IFocusable Implementation ---

    public void ShowWindow() => FocusManager.Instance.PushFocus(this);

    public void OnFocus()
    {
        if (_window == null) return;
        _window.style.display = DisplayStyle.Flex;
        
        if (consoleA != null) consoleA.BringToFront();
        if (consoleB != null) consoleB.BringToFront();
    }

    public void OnDefocus()
    {
        if (_window == null) return;
        _window.style.display = DisplayStyle.None;
        
        // Clean up cursor icons if we close while hovering a handle
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}