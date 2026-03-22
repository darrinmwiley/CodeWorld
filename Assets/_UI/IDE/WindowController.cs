using UnityEngine;
using UnityEngine.UIElements;

public class WindowContainerController : WindowComponent, IFocusable, IBaseWindow
{
    [Header("Shell References")]
    [SerializeField] private UIDocument _windowShell;

    [Header("Window Settings")]
    [SerializeField] private Vector2 _defaultSize = new Vector2(800, 600);
    [SerializeField] private bool _centerOnEnable = true;

    [Header("Cursor Sprites")]
    public Texture2D horizontalCursor;
    public Texture2D verticalCursor;
    public Texture2D diagonalRightCursor;
    public Texture2D diagonalLeftCursor;
    public Vector2 hotSpot = new Vector2(16, 16);

    // --- IBaseWindow Implementation ---
    public VisualElement RootElement { get; private set; }
    public void FocusWindow() => FocusManager.Instance?.PushFocus(this);

    void OnEnable()
    {
        if (_windowShell == null) return;

        RootElement = _windowShell.rootVisualElement.Q<VisualElement>("ResizableWindow");
        if (RootElement == null) return;

        // 1. Initial State Setup
        RootElement.style.position = Position.Absolute;
        RootElement.style.width = _defaultSize.x;
        RootElement.style.height = _defaultSize.y;
        RootElement.style.display = DisplayStyle.None;

        if (_centerOnEnable)
        {
            RootElement.RegisterCallback<GeometryChangedEvent>(CenterWindow);
        }

        // 2. Setup Resize Boundaries
        SetupAllResizeZones();

        // 3. Kick off the recursive self-assembly for any direct children (e.g., Toolbar into WindowFrameSlot)
        InitializeSubComponents(_windowShell.rootVisualElement, this);
    }

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        // This handles edge-cases where the entire Shell needs to be injected into another massive system.
        if (RootElement != null && container != null)
        {
            container.Add(RootElement);
            InitializeSubComponents(RootElement, root);
        }
    }

    private void CenterWindow(GeometryChangedEvent evt)
    {
        VisualElement parent = RootElement.parent;
        if (parent != null)
        {
            float newLeft = (parent.layout.width - _defaultSize.x) * 0.5f;
            float newTop = (parent.layout.height - _defaultSize.y) * 0.5f;
            RootElement.style.left = Mathf.Max(0, newLeft);
            RootElement.style.top = Mathf.Max(0, newTop);
        }
        RootElement.UnregisterCallback<GeometryChangedEvent>(CenterWindow);
    }

    private void SetupAllResizeZones()
    {
        SetupZone("LeftBorderHoverZone", horizontalCursor, ResizeDirection.Left);
        SetupZone("RightBorderHoverZone", horizontalCursor, ResizeDirection.Right);
        SetupZone("TopBorderHoverZone", verticalCursor, ResizeDirection.Top);
        SetupZone("BottomBorderHoverZone", verticalCursor, ResizeDirection.Bottom);
        
        SetupZone("TopLeftHoverZone", diagonalRightCursor, ResizeDirection.TopLeft);
        SetupZone("BottomRightHoverZone", diagonalRightCursor, ResizeDirection.BottomRight);
        SetupZone("TopRightHoverZone", diagonalLeftCursor, ResizeDirection.TopRight);
        SetupZone("BottomLeftHoverZone", diagonalLeftCursor, ResizeDirection.BottomLeft);
    }

    private void SetupZone(string zoneName, Texture2D cursor, ResizeDirection direction)
    {
        var zone = RootElement.Q<VisualElement>(zoneName);
        if (zone == null) return;

        zone.BringToFront();
        zone.AddManipulator(new UIResizableManipulator(RootElement, direction, FocusWindow));

        zone.RegisterCallback<PointerEnterEvent>(e => UnityEngine.Cursor.SetCursor(cursor, hotSpot, CursorMode.Auto));
        zone.RegisterCallback<PointerLeaveEvent>(e => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
    }

    public void OnFocus()
    {
        if (RootElement != null)
        {
            RootElement.style.display = DisplayStyle.Flex;
            RootElement.BringToFront();
        }
    }

    public void OnDefocus()
    {
        if (RootElement != null)
        {
            RootElement.style.display = DisplayStyle.None;
        }
    }
}