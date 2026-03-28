using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MultiPaneWindowController : WindowComponent
{
    [Header("Layout Assets")]
    [SerializeField] private VisualTreeAsset _layoutAsset;

    [Header("Constraint Settings")]
    [SerializeField] private float _minLeftPaneWidth = 200f;
    [SerializeField] private float _snapThreshold = 100f;
    [SerializeField] private float _minTopPaneHeight = 80f;
    [SerializeField] private float _minBottomPaneHeight = 80f;
    [SerializeField] private float _minCenterPaneWidth = 50f;
    [SerializeField] private float _separatorThickness = 10f;

    [Header("Cursor Settings")]
    public Texture2D horizontalCursor;
    public Texture2D verticalCursor;
    public Vector2 hotSpot = new Vector2(16, 16);

    private VisualElement _root;
    private VisualElement _leftPaneSlot;
    private VisualElement _topSlot;
    private VisualElement _centerPane;
    private IBaseWindow _windowRoot;

    private bool _isLeftPaneOpen = true;

    private float _dragStartMouseY;
    private float _dragStartHeight;

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        if (container == null || _layoutAsset == null) return;

        _windowRoot = root;

        container.Clear();
        _root = _layoutAsset.Instantiate();
        _root.style.flexGrow = 1;
        _root.style.minWidth = 0;
        _root.style.minHeight = 0;
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
        if (_centerPane != null)
            _centerPane.RegisterCallback<GeometryChangedEvent>(OnCenterPaneResized);

        InitializeSubComponents(_root, root);
        _windowRoot?.UpdateRootConstraints(GetMinimumSize());
    }

    public override Vector2 GetMinimumSize()
    {
        Vector2 leftChildMin = GetLargestMappedMinimumForSlots("LeftPane");
        Vector2 topChildMin = GetLargestMappedMinimumForSlots("Top");
        Vector2 centerChildMin = GetLargestMappedMinimumExcludingSlots("LeftPane", "Top");

        float effectiveLeftWidth = _isLeftPaneOpen ? Mathf.Max(_minLeftPaneWidth, leftChildMin.x) : 0f;
        float effectiveCenterWidth = Mathf.Max(_minCenterPaneWidth, Mathf.Max(topChildMin.x, centerChildMin.x));

        float effectiveTopHeight = Mathf.Max(_minTopPaneHeight, topChildMin.y);
        float effectiveBottomHeight = Mathf.Max(_minBottomPaneHeight, centerChildMin.y);

        float minW = effectiveLeftWidth + effectiveCenterWidth;
        float minH = effectiveTopHeight + effectiveBottomHeight + _separatorThickness;

        return new Vector2(minW, minH);
    }

    private void OnRootResized(GeometryChangedEvent evt)
    {
        float totalWidth = evt.newRect.width;
        if (totalWidth <= 0 || !_isLeftPaneOpen || _leftPaneSlot == null) return;

        float currentLeftWidth = _leftPaneSlot.resolvedStyle.width;
        float minRightWidth = Mathf.Max(_minCenterPaneWidth, Mathf.Max(GetLargestMappedMinimumForSlots("Top").x, GetLargestMappedMinimumExcludingSlots("LeftPane", "Top").x));

        if (currentLeftWidth + minRightWidth > totalWidth)
        {
            float allowedWidth = Mathf.Max(_minLeftPaneWidth, totalWidth - minRightWidth);
            SetLeftPaneSize(allowedWidth);
        }

        _windowRoot?.UpdateRootConstraints(GetMinimumSize());
    }

    private void SetupVerticalResizer()
    {
        var verticalSep = _root.Q<VisualElement>("VerticalSeparator");
        if (verticalSep == null || _leftPaneSlot == null) return;

        float openThreshold = _snapThreshold;
        float closeThreshold = _snapThreshold - 40f;
        float tabListOffset = 50f;

        verticalSep.RegisterCallback<PointerEnterEvent>(e => UnityEngine.Cursor.SetCursor(horizontalCursor, hotSpot, CursorMode.Auto));
        verticalSep.RegisterCallback<PointerLeaveEvent>(e =>
        {
            if (!verticalSep.HasPointerCapture(e.pointerId))
                UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        });

        verticalSep.RegisterCallback<PointerDownEvent>(e =>
        {
            verticalSep.CapturePointer(e.pointerId);
            e.StopPropagation();
        });

        verticalSep.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (!verticalSep.HasPointerCapture(e.pointerId)) return;

            float localMouseX = _root.WorldToLocal(e.position).x;
            float desiredPaneWidth = localMouseX - tabListOffset;

            if (!_isLeftPaneOpen)
            {
                if (desiredPaneWidth >= openThreshold)
                {
                    _isLeftPaneOpen = true;
                    _leftPaneSlot.style.display = DisplayStyle.Flex;
                    SetLeftPaneSize(Mathf.Max(_minLeftPaneWidth, GetLargestMappedMinimumForSlots("LeftPane").x));
                    _windowRoot?.UpdateRootConstraints(GetMinimumSize());
                }
            }
            else
            {
                if (desiredPaneWidth < closeThreshold)
                {
                    _isLeftPaneOpen = false;
                    _leftPaneSlot.style.display = DisplayStyle.None;
                    SetLeftPaneSize(0);
                    _windowRoot?.UpdateRootConstraints(GetMinimumSize());
                }
                else if (desiredPaneWidth > _minLeftPaneWidth)
                {
                    float maxAllowed = _root.resolvedStyle.width - Mathf.Max(_minCenterPaneWidth, Mathf.Max(GetLargestMappedMinimumForSlots("Top").x, GetLargestMappedMinimumExcludingSlots("LeftPane", "Top").x)) - tabListOffset;
                    float minAllowed = Mathf.Max(_minLeftPaneWidth, GetLargestMappedMinimumForSlots("LeftPane").x);
                    SetLeftPaneSize(Mathf.Clamp(desiredPaneWidth, minAllowed, maxAllowed));
                }
                else
                {
                    SetLeftPaneSize(Mathf.Max(_minLeftPaneWidth, GetLargestMappedMinimumForSlots("LeftPane").x));
                }
            }
        });

        verticalSep.RegisterCallback<PointerUpEvent>(e =>
        {
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

        horizontalSep.RegisterCallback<PointerDownEvent>(e =>
        {
            horizontalSep.CapturePointer(e.pointerId);
            _dragStartMouseY = e.position.y;
            _dragStartHeight = _topSlot.resolvedStyle.height;
            e.StopPropagation();
        });

        horizontalSep.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (!horizontalSep.HasPointerCapture(e.pointerId)) return;

            float mouseDeltaY = e.position.y - _dragStartMouseY;
            float newHeight = _dragStartHeight + mouseDeltaY;

            float totalHeightAvailable = _centerPane.resolvedStyle.height;
            float minBottom = Mathf.Max(_minBottomPaneHeight, GetLargestMappedMinimumExcludingSlots("LeftPane", "Top").y);
            float minTop = Mathf.Max(_minTopPaneHeight, GetLargestMappedMinimumForSlots("Top").y);
            float maxAllowed = totalHeightAvailable - minBottom - _separatorThickness;

            if (newHeight > minTop && newHeight < maxAllowed)
            {
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
        if (totalHeight <= 0 || _topSlot == null) return;

        float currentTopHeight = _topSlot.resolvedStyle.height;
        float minBottom = Mathf.Max(_minBottomPaneHeight, GetLargestMappedMinimumExcludingSlots("LeftPane", "Top").y);

        if (currentTopHeight + minBottom + _separatorThickness > totalHeight)
        {
            float allowedHeight = Mathf.Max(_minTopPaneHeight, Mathf.Max(GetLargestMappedMinimumForSlots("Top").y, totalHeight - minBottom - _separatorThickness));
            _topSlot.style.height = allowedHeight;
            _topSlot.style.flexBasis = allowedHeight;
        }

        _windowRoot?.UpdateRootConstraints(GetMinimumSize());
    }

    private Vector2 GetLargestMappedMinimumForSlots(params string[] slotNames)
    {
        Vector2 result = Vector2.zero;
        if (_subComponents == null)
            return result;

        foreach (var map in _subComponents)
        {
            if (map.controller == null || string.IsNullOrWhiteSpace(map.slotName))
                continue;

            if (!MatchesAnySlot(map.slotName, slotNames))
                continue;

            Vector2 min = map.controller.GetMinimumSize();
            result.x = Mathf.Max(result.x, min.x);
            result.y = Mathf.Max(result.y, min.y);
        }

        return result;
    }

    private Vector2 GetLargestMappedMinimumExcludingSlots(params string[] excludedSlots)
    {
        Vector2 result = Vector2.zero;
        if (_subComponents == null)
            return result;

        foreach (var map in _subComponents)
        {
            if (map.controller == null || string.IsNullOrWhiteSpace(map.slotName))
                continue;

            if (MatchesAnySlot(map.slotName, excludedSlots))
                continue;

            Vector2 min = map.controller.GetMinimumSize();
            result.x = Mathf.Max(result.x, min.x);
            result.y = Mathf.Max(result.y, min.y);
        }

        return result;
    }

    private bool MatchesAnySlot(string slotName, params string[] candidates)
    {
        if (string.IsNullOrWhiteSpace(slotName) || candidates == null)
            return false;

        foreach (string candidate in candidates)
        {
            if (string.Equals(slotName, candidate, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
