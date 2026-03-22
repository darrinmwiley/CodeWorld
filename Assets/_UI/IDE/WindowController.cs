using UnityEngine;
using UnityEngine.UIElements;

public class WindowContainerController : MonoBehaviour, IFocusable
{
    [SerializeField] private UIDocument _windowShell;
    [SerializeField] private MultiPaneWindowController _paneController;
    [SerializeField] private ToolbarWindowController _toolbarController;

    [Header("Window Settings")]
    [SerializeField] private Vector2 _defaultSize = new Vector2(800, 600);
    [SerializeField] private bool _centerOnEnable = true;

    [Header("Cursor Sprites")]
    public Texture2D horizontalCursor;
    public Texture2D verticalCursor;
    public Texture2D diagonalRightCursor;
    public Texture2D diagonalLeftCursor;
    public Vector2 hotSpot = new Vector2(16, 16);

    private VisualElement _outerWindow;
    private VisualElement _frameSlot;

    void OnEnable()
    {
        if (_windowShell == null) return;

        var root = _windowShell.rootVisualElement;
        
        // This targets the top-level container in ResizableWindow.uxml
        _outerWindow = root.Q<VisualElement>("ResizableWindow");
        // This targets the unique slot we created for the content frame
        _frameSlot = root.Q<VisualElement>("WindowFrameSlot"); 

        if (_outerWindow != null && _frameSlot != null)
        {
            // Set initial window state
            _outerWindow.style.position = Position.Absolute;
            _outerWindow.style.width = _defaultSize.x;
            _outerWindow.style.height = _defaultSize.y;
            _outerWindow.style.display = DisplayStyle.None; 

            if (_centerOnEnable)
                _outerWindow.RegisterCallback<GeometryChangedEvent>(CenterWindow);

            // 1. Initialize the Chain: Shell -> Toolbar -> MultiPane
            if (_toolbarController != null)
            {
                _toolbarController.InitializeInParent(_frameSlot);
                
                VisualElement innerSpace = _frameSlot.Q<VisualElement>("InsideSpace");
                if (innerSpace != null && _paneController != null)
                {
                    _paneController.InitializeInParent(innerSpace);
                }
            }
            
            // 2. Setup Resize Zones with Cursors
            SetupAllResizeZones();

            // 3. Setup Drag Handle
            SetupHandle(root);
        }
    }

    private void SetupHandle(VisualElement root)
    {
        var handle = root.Q<VisualElement>("Handle");
        if (handle != null)
        {
            handle.AddManipulator(new UIDraggableManipulator(_outerWindow, () => {
                FocusManager.Instance?.PushFocus(this);
            }));
        }
    }

    private void CenterWindow(GeometryChangedEvent evt)
    {
        VisualElement parent = _outerWindow.parent;
        if (parent != null)
        {
            float newLeft = (parent.layout.width - _defaultSize.x) * 0.5f;
            float newTop = (parent.layout.height - _defaultSize.y) * 0.5f;
            _outerWindow.style.left = Mathf.Max(0, newLeft);
            _outerWindow.style.top = Mathf.Max(0, newTop);
        }
        _outerWindow.UnregisterCallback<GeometryChangedEvent>(CenterWindow);
    }

    private void SetupAllResizeZones()
    {
        // Sides
        SetupZone("LeftBorderHoverZone", horizontalCursor, ResizeDirection.Left);
        SetupZone("RightBorderHoverZone", horizontalCursor, ResizeDirection.Right);
        SetupZone("TopBorderHoverZone", verticalCursor, ResizeDirection.Top);
        SetupZone("BottomBorderHoverZone", verticalCursor, ResizeDirection.Bottom);
        
        // Corners
        SetupZone("TopLeftHoverZone", diagonalRightCursor, ResizeDirection.TopLeft);
        SetupZone("BottomRightHoverZone", diagonalRightCursor, ResizeDirection.BottomRight);
        SetupZone("TopRightHoverZone", diagonalLeftCursor, ResizeDirection.TopRight);
        SetupZone("BottomLeftHoverZone", diagonalLeftCursor, ResizeDirection.BottomLeft);
    }

    private void SetupZone(string zoneName, Texture2D cursor, ResizeDirection direction)
    {
        var zone = _outerWindow.Q<VisualElement>(zoneName);
        if (zone == null) return;

        // Ensure the zone is on top so it can catch the cursor
        zone.BringToFront();

        // Add the resizing logic
        zone.AddManipulator(new UIResizableManipulator(_outerWindow, direction, () => {
            FocusManager.Instance?.PushFocus(this);
        }));

        // Add the cursor feedback logic
        zone.RegisterCallback<PointerEnterEvent>(e => UnityEngine.Cursor.SetCursor(cursor, hotSpot, CursorMode.Auto));
        zone.RegisterCallback<PointerLeaveEvent>(e => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
    }

    public void OnFocus() 
    { 
        if (_outerWindow != null) 
        {
            _outerWindow.style.display = DisplayStyle.Flex; 
            _outerWindow.BringToFront();
        }
    }

    public void OnDefocus() => _outerWindow.style.display = DisplayStyle.None;
}