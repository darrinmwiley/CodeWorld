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
    [SerializeField] private float _defaultBottomPaneHeight = 200f;
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
    private bool _hasInitializedHorizontalSplit;

    private float _dragStartMouseY;
    private float _dragStartHeight;
    private bool _isDraggingHorizontal;

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

    public void ApplyTheme(UITheme theme)
    {
        if (theme == null || _root == null)
            return;

        _root.style.backgroundColor = theme.backgroundBase;

        VisualElement leftPane = _root.Q<VisualElement>("LeftPane");
        if (leftPane != null)
            leftPane.style.backgroundColor = theme.backgroundSurface;

        if (_centerPane != null)
            _centerPane.style.backgroundColor = theme.backgroundBase;

        if (_topSlot != null)
            _topSlot.style.backgroundColor = theme.backgroundSurface;

        VisualElement verticalSep = _root.Q<VisualElement>("VerticalSeparator");
        if (verticalSep != null)
            verticalSep.style.backgroundColor = theme.border;

        VisualElement horizontalSep = _root.Q<VisualElement>("HorizontalSeparator");
        if (horizontalSep != null)
            horizontalSep.style.backgroundColor = theme.border;
    }

    private void OnRootResized(GeometryChangedEvent evt)
    {
        float totalWidth = evt.newRect.width;
        if (totalWidth <= 0 || !_isLeftPaneOpen || _leftPaneSlot == null) return;

        float currentLeftWidth = _leftPaneSlot.resolvedStyle.width;
        float minRightWidth = Mathf.Max(
            _minCenterPaneWidth,
            Mathf.Max(
                GetLargestMappedMinimumForSlots("Top").x,
                GetLargestMappedMinimumExcludingSlots("LeftPane", "Top").x
            )
        );

        if (currentLeftWidth + minRightWidth > totalWidth)
        {
            float allowedWidth = Mathf.Max(_minLeftPaneWidth, totalWidth - minRightWidth);
            SetLeftPaneSize(allowedWidth);
        }

        if (_hasInitializedHorizontalSplit)
            ClampTopPaneToValidRange();

        _windowRoot?.UpdateRootConstraints(GetMinimumSize());
    }

    private void SetupVerticalResizer()
    {
        VisualElement verticalSep = _root.Q<VisualElement>("VerticalSeparator");
        if (verticalSep == null || _leftPaneSlot == null) return;

        float openThreshold = _snapThreshold;
        float closeThreshold = _snapThreshold - 40f;
        float tabListOffset = 50f;

        verticalSep.RegisterCallback<PointerEnterEvent>(e =>
            UnityEngine.Cursor.SetCursor(horizontalCursor, hotSpot, CursorMode.Auto));

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
                    float maxAllowed = _root.resolvedStyle.width
                        - Mathf.Max(
                            _minCenterPaneWidth,
                            Mathf.Max(
                                GetLargestMappedMinimumForSlots("Top").x,
                                GetLargestMappedMinimumExcludingSlots("LeftPane", "Top").x
                            )
                        )
                        - tabListOffset;

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

        IStyle style = _leftPaneSlot.style;
        style.width = width;
        style.flexBasis = width;
        style.minWidth = width;
        style.maxWidth = width;
    }

    private void SetupHorizontalResizer()
    {
        VisualElement horizontalSep = _root.Q<VisualElement>("HorizontalSeparator");
        if (horizontalSep == null || _topSlot == null || _centerPane == null) return;

        horizontalSep.RegisterCallback<PointerEnterEvent>(e =>
            UnityEngine.Cursor.SetCursor(verticalCursor, hotSpot, CursorMode.Auto));

        horizontalSep.RegisterCallback<PointerLeaveEvent>(e =>
        {
            if (!horizontalSep.HasPointerCapture(e.pointerId))
                UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        });

        horizontalSep.RegisterCallback<PointerDownEvent>(e =>
        {
            horizontalSep.CapturePointer(e.pointerId);
            _dragStartMouseY = e.position.y;

            if (!_hasInitializedHorizontalSplit)
                ApplyInitialHorizontalSplit();
            else
                ClampTopPaneToValidRange();

            _dragStartHeight = _topSlot.resolvedStyle.height;
            _isDraggingHorizontal = true;

            e.StopPropagation();
        });

        horizontalSep.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (!horizontalSep.HasPointerCapture(e.pointerId)) return;

            float mouseDeltaY = e.position.y - _dragStartMouseY;
            float desiredHeight = _dragStartHeight + mouseDeltaY;
            ApplyTopPaneHeight(GetClampedTopPaneHeight(desiredHeight));
        });

        horizontalSep.RegisterCallback<PointerUpEvent>(e =>
        {
            if (horizontalSep.HasPointerCapture(e.pointerId))
                horizontalSep.ReleasePointer(e.pointerId);

            _isDraggingHorizontal = false;
            ClampTopPaneToValidRange();
            _windowRoot?.UpdateRootConstraints(GetMinimumSize());
        });
    }

    private void OnCenterPaneResized(GeometryChangedEvent evt)
    {
        if (_isDraggingHorizontal)
            return;

        if (!_hasInitializedHorizontalSplit)
            ApplyInitialHorizontalSplit();
        else
            ClampTopPaneToValidRange();

        _windowRoot?.UpdateRootConstraints(GetMinimumSize());
    }

    private void ApplyInitialHorizontalSplit()
    {
        if (_topSlot == null || _centerPane == null)
            return;

        float totalHeightAvailable = _centerPane.resolvedStyle.height;
        if (totalHeightAvailable <= 0f)
            return;

        float desiredTopHeight = totalHeightAvailable - _defaultBottomPaneHeight - _separatorThickness;
        ApplyTopPaneHeight(GetClampedTopPaneHeight(desiredTopHeight));
        _hasInitializedHorizontalSplit = true;
    }

    private void ClampTopPaneToValidRange()
    {
        if (_topSlot == null || _centerPane == null)
            return;

        float clampedHeight = GetClampedTopPaneHeight(_topSlot.resolvedStyle.height);
        ApplyTopPaneHeight(clampedHeight);
    }

    private float GetClampedTopPaneHeight(float desiredHeight)
    {
        float minTop = Mathf.Max(_minTopPaneHeight, GetLargestMappedMinimumForSlots("Top").y);
        float minBottom = Mathf.Max(_minBottomPaneHeight, GetLargestMappedMinimumExcludingSlots("LeftPane", "Top").y);

        float totalHeightAvailable = _centerPane != null ? _centerPane.resolvedStyle.height : 0f;
        float maxAllowed = totalHeightAvailable - minBottom - _separatorThickness;

        if (maxAllowed < minTop)
            maxAllowed = minTop;

        if (desiredHeight <= 0f)
            desiredHeight = minTop;

        return Mathf.Clamp(desiredHeight, minTop, maxAllowed);
    }

    private void ApplyTopPaneHeight(float height)
    {
        if (_topSlot == null)
            return;

        _topSlot.style.flexGrow = 0;
        _topSlot.style.flexBasis = height;
        _topSlot.style.height = height;
    }

    private Vector2 GetLargestMappedMinimumForSlots(params string[] slotNames)
    {
        Vector2 result = Vector2.zero;
        if (_subComponents == null)
            return result;

        foreach (WindowMapping map in _subComponents)
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

        foreach (WindowMapping map in _subComponents)
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
