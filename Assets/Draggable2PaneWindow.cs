using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Draggable2PaneWindow : MonoBehaviour
{
    private VisualElement _window;
    private bool _isOpen = false;

    [Header("Cursor Icons (assign in inspector)")]
    public Texture2D diag1, diag2, vert, horiz;
    public Vector2 hotSpot = new Vector2(16, 16);

    [Header("Pane Render Textures")]
    public RenderTexture leftOrTopRenderTexture;
    public RenderTexture rightOrBottomRenderTexture;

    [Header("Optional: Consoles in this window (for BringToFront)")]
    public ConsoleController consoleA;
    public ConsoleController consoleB;

    // Cached pane elements (UXML has two elements named "Content")
    private VisualElement _pane0;
    private VisualElement _pane1;

    void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;

        var root = uiDoc.rootVisualElement;
        if (root == null) return;

        _window = root.Q<VisualElement>("OuterBorder");
        var dragHandle = root.Q<VisualElement>("DragHandle");

        if (_window == null) return;

        _window.style.display = DisplayStyle.None;
        _window.RegisterCallback<GeometryChangedEvent>(OnInitialLayout);

        // Choose a BringToFront action (prefer explicit refs; fallback to parent lookup)
        Action bringToFront = ResolveBringToFrontAction();

        // Drag
        if (dragHandle != null)
        {
            dragHandle.pickingMode = PickingMode.Position;
            dragHandle.BringToFront();

            // Requires your updated ctor: UIDraggableManipulator(VisualElement, Action)
            dragHandle.AddManipulator(new UIDraggableManipulator(_window, bringToFront));
        }

        // Resize handles
        SetupHandle("LeftResize", horiz, ResizeDirection.Left);
        SetupHandle("RightResize", horiz, ResizeDirection.Right);
        SetupHandle("TopResize", vert, ResizeDirection.Top);
        SetupHandle("BottomResize", vert, ResizeDirection.Bottom);
        SetupHandle("TopLeftResize", diag1, ResizeDirection.TopLeft);
        SetupHandle("TopRightResize", diag2, ResizeDirection.TopRight);
        SetupHandle("BottomLeftResize", diag2, ResizeDirection.BottomLeft);
        SetupHandle("BottomRightResize", diag1, ResizeDirection.BottomRight);

        // Find BOTH "Content" panes (your Draggable2Pane.uxml has two with the same name)
        var panes = new List<VisualElement>();
        root.Query<VisualElement>("Content").ToList(panes);

        if (panes.Count >= 1) _pane0 = panes[0];
        if (panes.Count >= 2) _pane1 = panes[1];

        // Inject RTs
        ApplyPaneRT(_pane0, leftOrTopRenderTexture);
        ApplyPaneRT(_pane1, rightOrBottomRenderTexture);

        // If you have a divider and want it always on top:
        var divider = root.Q<VisualElement>("CenterDivider");
        if (divider != null) divider.BringToFront();
    }

    private Action ResolveBringToFrontAction()
    {
        // Prefer explicitly assigned controllers
        if (consoleA != null) return consoleA.BringToFront;
        if (consoleB != null) return consoleB.BringToFront;

        // Fallback: search parent for any ConsoleController
        var cc = GetComponentInParent<ConsoleController>();
        if (cc != null) return cc.BringToFront;

        // Nothing found
        return null;
    }

    private void ApplyPaneRT(VisualElement pane, RenderTexture rt)
    {
        if (pane == null || rt == null) return;

        pane.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
        pane.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;

        // Important: lets your drag/resize handles receive pointer events
        pane.pickingMode = PickingMode.Ignore;
    }

    private void SetupHandle(string name, Texture2D icon, ResizeDirection direction)
    {
        if (_window == null) return;

        var handle = _window.Q<VisualElement>(name);
        if (handle == null) return;

        handle.pickingMode = PickingMode.Position;
        handle.BringToFront();

        Action bringToFront = ResolveBringToFrontAction();

        // unchanged: uses your existing UIResizableManipulator signature
        handle.AddManipulator(new UIResizableManipulator(_window, direction, bringToFront));

        handle.RegisterCallback<PointerEnterEvent>(e =>
            UnityEngine.Cursor.SetCursor(icon, hotSpot, CursorMode.ForceSoftware));
        handle.RegisterCallback<PointerLeaveEvent>(e =>
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
    }

    private void OnInitialLayout(GeometryChangedEvent evt)
    {
        if (_window == null) return;

        _window.style.width = evt.newRect.width;
        _window.style.height = evt.newRect.height;

        var parent = _window.parent.layout;
        _window.style.left = (parent.width - evt.newRect.width) / 2;
        _window.style.top = (parent.height - evt.newRect.height) / 2;

        _window.UnregisterCallback<GeometryChangedEvent>(OnInitialLayout);
    }

    public void ShowWindow()
    {
        _isOpen = true;
        GameState.IsInUI = true;
        UnityEngine.Cursor.visible = true;
        if (_window != null) _window.style.display = DisplayStyle.Flex;
    }

    public void HideWindow()
    {
        _isOpen = false;
        GameState.IsInUI = false;
        if (_window != null) _window.style.display = DisplayStyle.None;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isOpen) HideWindow();
            else ShowWindow();
        }
    }
}