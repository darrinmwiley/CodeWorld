using UnityEngine;
using UnityEngine.UIElements;

public class MultiPaneWindowController : WindowComponent
{
    [Header("Layout Assets")]
    [SerializeField] private VisualTreeAsset _layoutAsset;

    [Header("Constraint Settings")]
    [SerializeField] private float _minLeftPaneWidth = 100f;
    [SerializeField] private float _minTopPaneHeight = 80f;
    [SerializeField] private float _minBottomPaneHeight = 80f;
    [SerializeField] private float _minCenterPaneWidth = 50f;

    [Header("Cursor Settings")]
    public Texture2D horizontalCursor;
    public Texture2D verticalCursor;
    public Vector2 hotSpot = new Vector2(16, 16);

    private VisualElement _root;
    private VisualElement _leftPaneSlot;
    private VisualElement _topSlot;
    private VisualElement _centerPane;

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        if (container == null || _layoutAsset == null) return;

        container.Clear(); 
        _root = _layoutAsset.Instantiate();
        _root.style.flexGrow = 1;
        container.Add(_root);

        _leftPaneSlot = _root.Q<VisualElement>("LeftPane");
        _centerPane = _root.Q<VisualElement>("CenterPane");
        _topSlot = _root.Q<VisualElement>("Top");

        SetupVerticalResizer();
        SetupHorizontalResizer();

        // Reactive logic: Listen for window resizes to enforce internal constraints
        _root.RegisterCallback<GeometryChangedEvent>(OnRootResized);
        _centerPane.RegisterCallback<GeometryChangedEvent>(OnCenterPaneResized);

        InitializeSubComponents(_root, root);
    }

    public override Vector2 GetMinimumSize()
    {
        // Total Min Width = Left Pane + Center Area Floor
        float minW = _minLeftPaneWidth + _minCenterPaneWidth; 
        // Total Min Height = Top + Bottom + Separator Thickness (~10px)
        float minH = _minTopPaneHeight + _minBottomPaneHeight + 10f;

        return new Vector2(minW, minH);
    }

    private void OnRootResized(GeometryChangedEvent evt)
    {
        // Enforce horizontal constraints for the Left Pane
        float totalWidth = evt.newRect.width;
        if (totalWidth <= 0) return;

        float currentLeftWidth = _leftPaneSlot.resolvedStyle.width;
        
        // If left pane + min center space is wider than window, shrink the left pane
        if (currentLeftWidth + _minCenterPaneWidth > totalWidth)
        {
            float allowedWidth = Mathf.Max(_minLeftPaneWidth, totalWidth - _minCenterPaneWidth);
            _leftPaneSlot.style.width = allowedWidth;
            _leftPaneSlot.style.flexBasis = allowedWidth;
        }
    }

    private void OnCenterPaneResized(GeometryChangedEvent evt)
    {
        // Enforce vertical constraints for Top/Bottom panes
        float totalHeight = evt.newRect.height;
        if (totalHeight <= 0) return;

        float currentTopHeight = _topSlot.resolvedStyle.height;
        float separatorHeight = 10f;

        // If top + bottom min + separator > available space, push the divider up
        if (currentTopHeight + _minBottomPaneHeight + separatorHeight > totalHeight)
        {
            float allowedHeight = Mathf.Max(_minTopPaneHeight, totalHeight - _minBottomPaneHeight - separatorHeight);
            _topSlot.style.height = allowedHeight;
            _topSlot.style.flexBasis = allowedHeight;
        }
    }

    private void SetupVerticalResizer()
    {
        var verticalSep = _root.Q<VisualElement>("VerticalSeparator");
        if (verticalSep == null || _leftPaneSlot == null) return;

        verticalSep.RegisterCallback<PointerEnterEvent>(e => UnityEngine.Cursor.SetCursor(horizontalCursor, hotSpot, CursorMode.Auto));
        verticalSep.RegisterCallback<PointerLeaveEvent>(e => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
        verticalSep.RegisterCallback<PointerDownEvent>(e => { verticalSep.CapturePointer(e.pointerId); e.StopPropagation(); });
        
        verticalSep.RegisterCallback<PointerMoveEvent>(e => {
            if (!verticalSep.HasPointerCapture(e.pointerId)) return;
            
            float newWidth = _root.WorldToLocal(e.position).x - _leftPaneSlot.layout.x;
            float maxAllowed = _root.resolvedStyle.width - _minCenterPaneWidth;

            if (newWidth > _minLeftPaneWidth && newWidth < maxAllowed) {
                _leftPaneSlot.style.width = newWidth;
                _leftPaneSlot.style.flexBasis = newWidth;
            }
        });
        verticalSep.RegisterCallback<PointerUpEvent>(e => verticalSep.ReleasePointer(e.pointerId));
    }

    private void SetupHorizontalResizer()
    {
        var horizontalSep = _root.Q<VisualElement>("HorizontalSeparator");
        if (horizontalSep == null || _topSlot == null || _centerPane == null) return;

        horizontalSep.RegisterCallback<PointerEnterEvent>(e => UnityEngine.Cursor.SetCursor(verticalCursor, hotSpot, CursorMode.Auto));
        horizontalSep.RegisterCallback<PointerLeaveEvent>(e => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
        horizontalSep.RegisterCallback<PointerDownEvent>(e => { horizontalSep.CapturePointer(e.pointerId); e.StopPropagation(); });
        
        horizontalSep.RegisterCallback<PointerMoveEvent>(e => {
            if (!horizontalSep.HasPointerCapture(e.pointerId)) return;

            Vector2 localMouse = _root.WorldToLocal(e.position);
            float newHeight = localMouse.y - _topSlot.layout.y;
            float totalHeightAvailable = _centerPane.resolvedStyle.height;
            float maxAllowed = totalHeightAvailable - _minBottomPaneHeight - 10f;

            if (newHeight > _minTopPaneHeight && newHeight < maxAllowed) {
                _topSlot.style.flexGrow = 0; 
                _topSlot.style.flexBasis = newHeight;
                _topSlot.style.height = newHeight;
            }
        });
        horizontalSep.RegisterCallback<PointerUpEvent>(e => horizontalSep.ReleasePointer(e.pointerId));
    }
}