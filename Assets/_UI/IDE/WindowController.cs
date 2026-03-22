using UnityEngine;
using UnityEngine.UIElements;

public class WindowContainerController : MonoBehaviour, IFocusable
{
    [Header("UI Document (The only one in scene)")]
    [SerializeField] private UIDocument _windowShell;

    [Header("Sub-Controllers")]
    [Tooltip("The controller for the inner toolbar and dynamic content.")]
    [SerializeField] private ToolbarWindowController _toolbarController;

    [Header("Window Settings")]
    [SerializeField] private Vector2 _defaultSize = new Vector2(500, 400);
    [SerializeField] private bool _centerOnEnable = true;

    [Header("Cursor Sprites")]
    public Texture2D horizontalCursor;
    public Texture2D verticalCursor;
    public Texture2D diagonalRightCursor;
    public Texture2D diagonalLeftCursor;
    public Vector2 hotSpot = new Vector2(16, 16);

    private VisualElement _outerWindow;
    private VisualElement _contentSlot;

    void OnEnable()
    {
        if (_windowShell == null) return;

        var root = _windowShell.rootVisualElement;
        
        _outerWindow = root.Q<VisualElement>("ResizableWindow");
        _contentSlot = root.Q<VisualElement>("MainContent"); 

        if (_outerWindow != null && _contentSlot != null)
        {
            _outerWindow.style.position = Position.Absolute;
            _outerWindow.style.display = DisplayStyle.None; 
            
            _outerWindow.style.bottom = StyleKeyword.Null;
            _outerWindow.style.right = StyleKeyword.Null;
            _outerWindow.style.width = _defaultSize.x;
            _outerWindow.style.height = _defaultSize.y;

            if (_centerOnEnable)
                _outerWindow.RegisterCallback<GeometryChangedEvent>(CenterWindow);
            
            SetupAllResizeZones();

            // FIX: Initialize the toolbar hierarchy FIRST so the "Handle" is added to the tree
            if (_toolbarController != null)
            {
                _toolbarController.InitializeInParent(_contentSlot);
            }

            // NOW the handle can be found and the manipulator attached
            SetupHandle(root);
        }
    }

    private void SetupHandle(VisualElement root)
    {
        // This search is recursive and will find the handle nested inside the toolbar
        var handle = root.Q<VisualElement>("Handle");
        if (handle != null)
        {
            handle.AddManipulator(new UIDraggableManipulator(_outerWindow, () => {
                FocusManager.Instance?.PushFocus(this);
            }));
        }
        else
        {
            Debug.LogWarning("WindowContainerController: 'Handle' element not found after toolbar initialization.");
        }
    }

    public void OnFocus() 
    {
        if (_outerWindow != null)
        {
            _outerWindow.style.display = DisplayStyle.Flex;
            _outerWindow.BringToFront();
        }
    }

    public void OnDefocus() 
    {
        if (_outerWindow != null) _outerWindow.style.display = DisplayStyle.None;
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
        SetupZone("LeftBorderHoverZone", horizontalCursor, ResizeDirection.Left);
        SetupZone("RightBorderHoverZone", horizontalCursor, ResizeDirection.Right);
        SetupZone("TopBorderHoverZone", verticalCursor, ResizeDirection.Top);
        SetupZone("BottomBorderHoverZone", verticalCursor, ResizeDirection.Bottom);

        SetupCorner("TopLeftHoverZone", diagonalRightCursor, ResizeDirection.TopLeft);
        SetupCorner("BottomRightHoverZone", diagonalRightCursor, ResizeDirection.BottomRight);
        SetupCorner("TopRightHoverZone", diagonalLeftCursor, ResizeDirection.TopRight);
        SetupCorner("BottomLeftHoverZone", diagonalLeftCursor, ResizeDirection.BottomLeft);
    }

    private void SetupCorner(string name, Texture2D cursor, ResizeDirection dir)
    {
        var zone = _outerWindow.Q<VisualElement>(name);
        if (zone != null) zone.BringToFront();
        SetupZone(name, cursor, dir);
    }

    private void SetupZone(string zoneName, Texture2D cursor, ResizeDirection direction)
    {
        var zone = _outerWindow.Q<VisualElement>(zoneName);
        if (zone == null) return;

        zone.AddManipulator(new UIResizableManipulator(_outerWindow, direction, () => {
            FocusManager.Instance?.PushFocus(this);
        }));

        zone.RegisterCallback<PointerEnterEvent>(e => UnityEngine.Cursor.SetCursor(cursor, hotSpot, CursorMode.Auto));
        zone.RegisterCallback<PointerLeaveEvent>(e => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
    }
}