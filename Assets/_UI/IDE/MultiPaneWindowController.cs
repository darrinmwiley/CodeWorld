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

        InitializeSubComponents(_root, root);
    }

    public override Vector2 GetMinimumSize()
    {
        // Width is the left pane min width + some margin for center
        float minW = _minLeftPaneWidth + 50f; 
        // Height is the sum of Top + Bottom + Separator
        float minH = _minTopPaneHeight + _minBottomPaneHeight + 10f;

        return new Vector2(minW, minH);
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
            if (newWidth > _minLeftPaneWidth) {
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
            float totalHeight = _centerPane.resolvedStyle.height;

            if (newHeight > _minTopPaneHeight && (totalHeight - newHeight) > _minBottomPaneHeight) {
                _topSlot.style.flexGrow = 0; 
                _topSlot.style.flexBasis = newHeight;
                _topSlot.style.height = newHeight;
            }
        });
        horizontalSep.RegisterCallback<PointerUpEvent>(e => horizontalSep.ReleasePointer(e.pointerId));
    }
}