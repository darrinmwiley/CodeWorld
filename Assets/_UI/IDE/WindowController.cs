using UnityEngine;
using UnityEngine.UIElements;

public class WindowContainerController : WindowComponent, IFocusable, IBaseWindow
{
    [Header("Theme")]
    [SerializeField] private UITheme _theme;
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

    public VisualElement RootElement { get; private set; }
    public void FocusWindow() => FocusManager.Instance?.PushFocus(this);

    void OnEnable()
    {
        if (_windowShell == null) return;

        RootElement = _windowShell.rootVisualElement.Q<VisualElement>("ResizableWindow");
        if (RootElement == null) return;

        RootElement.style.position = Position.Absolute;
        RootElement.style.width = _defaultSize.x;
        RootElement.style.height = _defaultSize.y;
        RootElement.style.display = DisplayStyle.None;

        if (_centerOnEnable)
        {
            RootElement.RegisterCallback<GeometryChangedEvent>(CenterWindow);
        }

        SetupAllResizeZones();
        ApplyTheme(_theme);
        InitializeSubComponents(_windowShell.rootVisualElement, this);
    }

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        if (RootElement != null && container != null)
        {
            container.Add(RootElement);
            InitializeSubComponents(RootElement, root);
        }
    }

    public void UpdateRootConstraints(Vector2 minSize)
    {
        if (RootElement == null) return;
        RootElement.style.minWidth = minSize.x;
        RootElement.style.minHeight = minSize.y;
    }

    public override Vector2 GetMinimumSize()
    {
        Vector2 totalMin = new Vector2(200, 150); 
        foreach (var map in _subComponents)
        {
            if (map.controller == null) continue;
            Vector2 childMin = map.controller.GetMinimumSize();
            totalMin.x = Mathf.Max(totalMin.x, childMin.x);
            totalMin.y = Mathf.Max(totalMin.y, childMin.y);
        }
        return totalMin;
    }

    private void CenterWindow(GeometryChangedEvent evt)
    {
        VisualElement parent = RootElement.parent;
        if (parent != null)
        {
            // Center based on actual layout size, not requested default size
            float newLeft = (parent.layout.width - RootElement.layout.width) * 0.5f;
            float newTop = (parent.layout.height - RootElement.layout.height) * 0.5f;
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
        RootElement.style.display = DisplayStyle.Flex;
        RootElement.BringToFront();
    }

    public void OnDefocus() => RootElement.style.display = DisplayStyle.None;

    public override void ApplyTheme(UITheme theme)
    {
        _theme = theme;
        if (theme == null || RootElement == null)
            return;

        RootElement.style.backgroundColor = theme.backgroundBase;
        RootElement.style.borderLeftColor = theme.border;
        RootElement.style.borderRightColor = theme.border;
        RootElement.style.borderTopColor = theme.border;
        RootElement.style.borderBottomColor = theme.border;
        RootElement.style.color = theme.text;

        TryStyleElement("TitleBar", theme.backgroundSurface, theme.text, theme.border);
        TryStyleElement("ResizableWindow", theme.backgroundBase, theme.text, theme.border);
    }

    private void TryStyleElement(string name, Color background, Color? textColor, Color? borderColor)
    {
        var element = RootElement?.Q<VisualElement>(name);
        if (element == null)
            return;

        element.style.backgroundColor = background;
        if (textColor.HasValue)
            element.style.color = textColor.Value;
        if (borderColor.HasValue)
        {
            element.style.borderLeftColor = borderColor.Value;
            element.style.borderRightColor = borderColor.Value;
            element.style.borderTopColor = borderColor.Value;
            element.style.borderBottomColor = borderColor.Value;
        }
    }
}