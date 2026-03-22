using UnityEngine;
using UnityEngine.UIElements;

public class MultiPaneWindowController : MonoBehaviour
{
    [Header("Layout Assets")]
    [SerializeField] private VisualTreeAsset _layoutAsset;
    [SerializeField] private VisualTreeAsset _tabListAsset;
    [SerializeField] private VisualTreeAsset _leftPaneAsset;
    [SerializeField] private VisualTreeAsset _topPaneAsset;
    [SerializeField] private VisualTreeAsset _bottomPaneAsset;

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
    private VisualElement _bottomSlot;
    private VisualElement _centerPane;

    public void InitializeInParent(VisualElement parentSlot)
    {
        if (parentSlot == null || _layoutAsset == null) return;

        parentSlot.Clear(); 
        
        _root = _layoutAsset.Instantiate();
        _root.style.flexGrow = 1;
        parentSlot.Add(_root);

        _leftPaneSlot = _root.Q<VisualElement>("LeftPane");
        _centerPane = _root.Q<VisualElement>("CenterPane");
        _topSlot = _root.Q<VisualElement>("Top");
        _bottomSlot = _root.Q<VisualElement>("Bottom");
        var tabListSlot = _root.Q<VisualElement>("TabList");

        InjectContent(tabListSlot, _tabListAsset);
        InjectContent(_leftPaneSlot, _leftPaneAsset);
        InjectContent(_topSlot, _topPaneAsset);
        InjectContent(_bottomSlot, _bottomPaneAsset);

        SetupVerticalResizer();
        SetupHorizontalResizer();
    }

    private void InjectContent(VisualElement slot, VisualTreeAsset asset)
    {
        if (slot == null || asset == null) return;
        slot.Clear();
        VisualElement content = asset.Instantiate();
        content.style.flexGrow = 1;
        content.style.width = Length.Percent(100);
        content.style.height = Length.Percent(100);
        slot.Add(content);
    }

    private void SetupVerticalResizer()
    {
        var verticalSep = _root.Q<VisualElement>("VerticalSeparator");
        if (verticalSep == null || _leftPaneSlot == null) return;

        // Cursor Feedback
        verticalSep.RegisterCallback<PointerEnterEvent>(e => {
            if (horizontalCursor != null) UnityEngine.Cursor.SetCursor(horizontalCursor, hotSpot, CursorMode.Auto);
        });
        verticalSep.RegisterCallback<PointerLeaveEvent>(e => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));

        verticalSep.RegisterCallback<PointerDownEvent>(e => { 
            verticalSep.CapturePointer(e.pointerId); 
            e.StopPropagation(); 
        });

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

        // Cursor Feedback
        horizontalSep.RegisterCallback<PointerEnterEvent>(e => {
            if (verticalCursor != null) UnityEngine.Cursor.SetCursor(verticalCursor, hotSpot, CursorMode.Auto);
        });
        horizontalSep.RegisterCallback<PointerLeaveEvent>(e => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));

        horizontalSep.RegisterCallback<PointerDownEvent>(e => { 
            horizontalSep.CapturePointer(e.pointerId); 
            e.StopPropagation(); 
        });

        horizontalSep.RegisterCallback<PointerMoveEvent>(e => {
            if (!horizontalSep.HasPointerCapture(e.pointerId)) return;

            // FIX: Calculate position relative to _root instead of _centerPane
            // We subtract the _topSlot.layout.y to get the height relative to the top of the container
            Vector2 localMouse = _root.WorldToLocal(e.position);
            float newHeight = localMouse.y - _topSlot.layout.y;

            float totalHeight = _centerPane.resolvedStyle.height;

            if (newHeight > _minTopPaneHeight && (totalHeight - newHeight) > _minBottomPaneHeight) {
                _topSlot.style.flexGrow = 0; // Ensure flexGrow isn't fighting the Basis
                _topSlot.style.flexBasis = newHeight;
                _topSlot.style.height = newHeight;
            }
        });

        horizontalSep.RegisterCallback<PointerUpEvent>(e => horizontalSep.ReleasePointer(e.pointerId));
    }
}