using UnityEngine;
using UnityEngine.UIElements;

public class MultiPaneWindowController : WindowComponent
{
    [Header("Layout Assets")]
    [SerializeField] private VisualTreeAsset _layoutAsset;

    [Header("Constraint Settings")]
    [SerializeField] private float _minLeftPaneWidth = 200f; 
    [SerializeField] private float _snapThreshold = 100f;    // Threshold for the PANE width
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

    private bool _isLeftPaneOpen = true;

    // Drag State Tracking
    private float _dragStartMouseY;
    private float _dragStartHeight;

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

        if (_leftPaneSlot != null)
        {
            _leftPaneSlot.style.flexGrow = 0;
            _leftPaneSlot.style.flexShrink = 0;
        }

        SetupVerticalResizer();
        SetupHorizontalResizer();

        _root.RegisterCallback<GeometryChangedEvent>(OnRootResized);
        _centerPane.RegisterCallback<GeometryChangedEvent>(OnCenterPaneResized);

        InitializeSubComponents(_root, root);
    }

    public override Vector2 GetMinimumSize()
    {
        float currentLeftMin = _isLeftPaneOpen ? _minLeftPaneWidth : 0f;
        float minW = currentLeftMin + _minCenterPaneWidth; 
        float minH = _minTopPaneHeight + _minBottomPaneHeight + 10f;

        return new Vector2(minW, minH);
    }

    private void OnRootResized(GeometryChangedEvent evt)
    {
        float totalWidth = evt.newRect.width;
        if (totalWidth <= 0 || !_isLeftPaneOpen) return;

        float currentLeftWidth = _leftPaneSlot.resolvedStyle.width;
        
        if (currentLeftWidth + _minCenterPaneWidth > totalWidth)
        {
            float allowedWidth = Mathf.Max(_minLeftPaneWidth, totalWidth - _minCenterPaneWidth);
            SetLeftPaneSize(allowedWidth);
        }
    }

private void SetupVerticalResizer()
    {
        var verticalSep = _root.Q<VisualElement>("VerticalSeparator");
        if (verticalSep == null || _leftPaneSlot == null) return;

        // Configuration
        float openThreshold = _snapThreshold;            // e.g. 100
        float closeThreshold = _snapThreshold - 40f;    // e.g. 60 (Larger buffer to stop flicker)
        
        // We need to know where the TabList ends. 
        // If it's a fixed width, you can set this to that value (e.g. 50f).
        // Otherwise, we'll grab it once.
        float tabListOffset = 50f; 

        verticalSep.RegisterCallback<PointerEnterEvent>(e => UnityEngine.Cursor.SetCursor(horizontalCursor, hotSpot, CursorMode.Auto));
        verticalSep.RegisterCallback<PointerLeaveEvent>(e => {
            if (!verticalSep.HasPointerCapture(e.pointerId))
                UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        });
        
        verticalSep.RegisterCallback<PointerDownEvent>(e => { 
            verticalSep.CapturePointer(e.pointerId); 
            e.StopPropagation(); 
        });
        
        verticalSep.RegisterCallback<PointerMoveEvent>(e => {
            if (!verticalSep.HasPointerCapture(e.pointerId)) return;
            
            // Measure mouse relative to the window root (0 is far left of screen/window)
            float localMouseX = _root.WorldToLocal(e.position).x;

            // IMPORTANT: Calculate width relative to the WINDOW edge, not the pane layout.
            // This stops the flickering because the window edge never moves.
            float desiredPaneWidth = localMouseX - tabListOffset;

            if (!_isLeftPaneOpen)
            {
                if (desiredPaneWidth >= openThreshold)
                {
                    _isLeftPaneOpen = true;
                    _leftPaneSlot.style.display = DisplayStyle.Flex;
                    SetLeftPaneSize(_minLeftPaneWidth);
                }
            }
            else
            {
                // Must pull back significantly further than the open point to close
                if (desiredPaneWidth < closeThreshold)
                {
                    _isLeftPaneOpen = false;
                    _leftPaneSlot.style.display = DisplayStyle.None;
                    SetLeftPaneSize(0);
                }
                // Zone C: Active Resizing
                else if (desiredPaneWidth > _minLeftPaneWidth)
                {
                    float maxAllowed = _root.resolvedStyle.width - _minCenterPaneWidth - tabListOffset;
                    SetLeftPaneSize(Mathf.Clamp(desiredPaneWidth, _minLeftPaneWidth, maxAllowed));
                }
                // Zone B: Snappy Deadzone (Force MinWidth)
                else
                {
                    SetLeftPaneSize(_minLeftPaneWidth);
                }
            }
        });

        verticalSep.RegisterCallback<PointerUpEvent>(e => {
            verticalSep.ReleasePointer(e.pointerId);
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        });
    }
    private void SetLeftPaneSize(float width)
    {
        if (_leftPaneSlot == null) return;
        var style = _leftPaneSlot.style;
        style.width = width;
        style.flexBasis = width;
        style.minWidth = width;
        style.maxWidth = width; 
    }

    private void SetupHorizontalResizer()
    {
        var horizontalSep = _root.Q<VisualElement>("HorizontalSeparator");
        if (horizontalSep == null || _topSlot == null || _centerPane == null) return;

        horizontalSep.RegisterCallback<PointerEnterEvent>(e => UnityEngine.Cursor.SetCursor(verticalCursor, hotSpot, CursorMode.Auto));
        horizontalSep.RegisterCallback<PointerLeaveEvent>(e => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
        
        horizontalSep.RegisterCallback<PointerDownEvent>(e => { 
            horizontalSep.CapturePointer(e.pointerId); 
            _dragStartMouseY = e.position.y;
            _dragStartHeight = _topSlot.resolvedStyle.height;
            e.StopPropagation(); 
        });
        
        horizontalSep.RegisterCallback<PointerMoveEvent>(e => {
            if (!horizontalSep.HasPointerCapture(e.pointerId)) return;

            float mouseDeltaY = e.position.y - _dragStartMouseY;
            float newHeight = _dragStartHeight + mouseDeltaY;

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

    private void OnCenterPaneResized(GeometryChangedEvent evt)
    {
        float totalHeight = evt.newRect.height;
        if (totalHeight <= 0) return;

        float currentTopHeight = _topSlot.resolvedStyle.height;
        float separatorHeight = 10f;

        if (currentTopHeight + _minBottomPaneHeight + separatorHeight > totalHeight)
        {
            float allowedHeight = Mathf.Max(_minTopPaneHeight, totalHeight - _minBottomPaneHeight - separatorHeight);
            _topSlot.style.height = allowedHeight;
            _topSlot.style.flexBasis = allowedHeight;
        }
    }
}