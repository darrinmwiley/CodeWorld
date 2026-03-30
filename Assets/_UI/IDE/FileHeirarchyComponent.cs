using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

public class FileHierarchyComponent : WindowComponent
{
    [Header("Theme")]
    [SerializeField] private UITheme _theme;

    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset _hierarchyAsset;

    [Header("Bindings")]
    [SerializeField] private TabbedConsoleWindowController _tabbedContent;
    [SerializeField] private ConsoleStateManager _consoleStateManager;
    [SerializeField] private ConsoleLevelFileSet _fileSet;
    [SerializeField] private string _treeViewElementName = "FileTree";

    [Header("Appearance")]
    [SerializeField] private float _rowHeight = 22f;
    [SerializeField] private int _fontSize = 11;
    [SerializeField] private float _indentWidth = 14f;
    [SerializeField] private float _rowHorizontalPadding = 6f;
    [SerializeField] private float _iconWidth = 12f;
    [SerializeField] private float _scrollbarThickness = 10f;
    [SerializeField] private float _minThumbHeight = 24f;
    [SerializeField] private float _wheelStep = 24f;

    [Header("Runtime")]
    [SerializeField] private bool _preserveEditsWhenSwitchingFiles = true;

    [Header("Debug")]
    [SerializeField] private bool _verboseLogging = false;

    private VisualElement _mainView;
    private VisualElement _explorerRoot;
    private VisualElement _viewport;
    private VisualElement _content;
    private VisualElement _scrollbarTrack;
    private VisualElement _scrollbarThumb;

    private readonly List<ExplorerItem> _rootItems = new List<ExplorerItem>();
    private readonly List<RowVisual> _rowVisuals = new List<RowVisual>();
    private readonly Dictionary<string, ConsoleLevelFileEntry> _entriesByPath = new Dictionary<string, ConsoleLevelFileEntry>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<ConsoleStateManager.Line>> _runtimeDocuments = new Dictionary<string, List<ConsoleStateManager.Line>>(StringComparer.OrdinalIgnoreCase);

    private float _scrollOffset;
    private float _contentHeight;
    private bool _draggingThumb;
    private float _thumbDragStartMouseY;
    private float _thumbDragStartOffset;

    private string _currentOpenFilePath;
    private ConsoleStateManager _activeStateManager;

    public event Action<VirtualFileNode> FileClicked;

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        if (_hierarchyAsset == null)
        {
            Debug.LogError($"[FileHierarchy] Main hierarchy asset is missing on {gameObject.name}", this);
            return;
        }

        container.Clear();
        container.style.flexGrow = 1f;
        container.style.minWidth = 0f;
        container.style.minHeight = 0f;
        container.style.overflow = Overflow.Hidden;

        _mainView = _hierarchyAsset.Instantiate();
        _mainView.style.flexGrow = 1f;
        _mainView.style.minWidth = 0f;
        _mainView.style.minHeight = 0f;
        _mainView.style.overflow = Overflow.Hidden;
        container.Add(_mainView);

        _activeStateManager = _consoleStateManager;

        BuildCustomExplorerHost();
        BuildFileTreeFromInjectedFileSet();
        RebuildVisibleRows();
        ApplyTheme(_theme);

        InitializeSubComponents(_mainView, root);
        Log("Initialize complete.");
    }

    public void SetFileSet(ConsoleLevelFileSet fileSet)
    {
        _fileSet = fileSet;
        BuildFileTreeFromInjectedFileSet();
        RebuildVisibleRows();
    }

    public void ApplyTheme(UITheme theme)
    {
        _theme = theme;
        if (_theme == null)
            return;

        Color surface = _theme.backgroundSurface;
        Color active = _theme.backgroundActive;
        Color text = _theme.text;
        Color track = new Color(text.r, text.g, text.b, 0.08f);
        Color thumb = new Color(text.r, text.g, text.b, 0.34f);

        if (_mainView != null)
        {
            _mainView.style.backgroundColor = surface;
            _mainView.style.color = text;
        }

        if (_explorerRoot != null)
            _explorerRoot.style.backgroundColor = surface;

        if (_viewport != null)
            _viewport.style.backgroundColor = surface;

        if (_content != null)
            _content.style.backgroundColor = surface;

        if (_scrollbarTrack != null)
            _scrollbarTrack.style.backgroundColor = track;

        if (_scrollbarThumb != null)
            _scrollbarThumb.style.backgroundColor = thumb;

        RefreshRowVisuals();
        UpdateScrollVisuals();
    }

    private void BuildCustomExplorerHost()
    {
        VisualElement placeholder = _mainView.Q<VisualElement>(_treeViewElementName);
        VisualElement parent = placeholder != null ? placeholder.parent : _mainView;
        int insertIndex = 0;

        if (placeholder != null && parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.ElementAt(i) == placeholder)
                {
                    insertIndex = i;
                    break;
                }
            }

            placeholder.RemoveFromHierarchy();
        }

        _explorerRoot = new VisualElement();
        _explorerRoot.name = _treeViewElementName;
        _explorerRoot.style.flexGrow = 1f;
        _explorerRoot.style.flexDirection = FlexDirection.Row;
        _explorerRoot.style.minWidth = 0f;
        _explorerRoot.style.minHeight = 0f;
        _explorerRoot.style.overflow = Overflow.Hidden;
        _explorerRoot.focusable = false;
        _explorerRoot.tabIndex = -1;

        _viewport = new VisualElement();
        _viewport.style.flexGrow = 1f;
        _viewport.style.minWidth = 0f;
        _viewport.style.minHeight = 0f;
        _viewport.style.overflow = Overflow.Hidden;
        _viewport.focusable = false;
        _viewport.tabIndex = -1;

        _content = new VisualElement();
        _content.style.position = Position.Relative;
        _content.style.left = 0f;
        _content.style.top = 0f;
        _content.style.flexDirection = FlexDirection.Column;
        _content.style.minWidth = 0f;
        _content.style.minHeight = 0f;
        _content.focusable = false;
        _content.tabIndex = -1;

        _scrollbarTrack = new VisualElement();
        _scrollbarTrack.style.width = _scrollbarThickness;
        _scrollbarTrack.style.minWidth = _scrollbarThickness;
        _scrollbarTrack.style.maxWidth = _scrollbarThickness;
        _scrollbarTrack.style.alignSelf = Align.Stretch;
        _scrollbarTrack.style.position = Position.Relative;
        _scrollbarTrack.style.marginTop = 0f;
        _scrollbarTrack.style.marginBottom = 0f;
        _scrollbarTrack.style.marginLeft = 0f;
        _scrollbarTrack.style.marginRight = 0f;
        _scrollbarTrack.style.paddingTop = 0f;
        _scrollbarTrack.style.paddingBottom = 0f;
        _scrollbarTrack.style.paddingLeft = 0f;
        _scrollbarTrack.style.paddingRight = 0f;
        _scrollbarTrack.style.borderLeftWidth = 0f;
        _scrollbarTrack.style.borderRightWidth = 0f;
        _scrollbarTrack.style.borderTopWidth = 0f;
        _scrollbarTrack.style.borderBottomWidth = 0f;
        _scrollbarTrack.focusable = false;
        _scrollbarTrack.tabIndex = -1;

        _scrollbarThumb = new VisualElement();
        _scrollbarThumb.style.position = Position.Absolute;
        _scrollbarThumb.style.left = 0f;
        _scrollbarThumb.style.right = 0f;
        _scrollbarThumb.style.top = 0f;
        _scrollbarThumb.style.height = _minThumbHeight;
        _scrollbarThumb.style.borderLeftWidth = 0f;
        _scrollbarThumb.style.borderRightWidth = 0f;
        _scrollbarThumb.style.borderTopWidth = 0f;
        _scrollbarThumb.style.borderBottomWidth = 0f;
        _scrollbarThumb.style.borderTopLeftRadius = _scrollbarThickness * 0.5f;
        _scrollbarThumb.style.borderTopRightRadius = _scrollbarThickness * 0.5f;
        _scrollbarThumb.style.borderBottomLeftRadius = _scrollbarThickness * 0.5f;
        _scrollbarThumb.style.borderBottomRightRadius = _scrollbarThickness * 0.5f;
        _scrollbarThumb.focusable = false;
        _scrollbarThumb.tabIndex = -1;

        _viewport.Add(_content);
        _scrollbarTrack.Add(_scrollbarThumb);
        _explorerRoot.Add(_viewport);
        _explorerRoot.Add(_scrollbarTrack);

        if (parent != null)
            parent.Insert(insertIndex, _explorerRoot);
        else
            _mainView.Add(_explorerRoot);

        _viewport.RegisterCallback<WheelEvent>(OnWheelScrolled, TrickleDown.TrickleDown);
        _explorerRoot.RegisterCallback<KeyDownEvent>(SuppressKeyboardEvent, TrickleDown.TrickleDown);
        _explorerRoot.RegisterCallback<KeyUpEvent>(SuppressKeyboardEvent, TrickleDown.TrickleDown);
        _explorerRoot.RegisterCallback<NavigationMoveEvent>(SuppressNavigationEvent, TrickleDown.TrickleDown);
        _explorerRoot.RegisterCallback<NavigationSubmitEvent>(SuppressNavigationEvent, TrickleDown.TrickleDown);
        _explorerRoot.RegisterCallback<NavigationCancelEvent>(SuppressNavigationEvent, TrickleDown.TrickleDown);
        _explorerRoot.RegisterCallback<FocusInEvent>(OnExplorerFocusIn, TrickleDown.TrickleDown);

        _viewport.RegisterCallback<GeometryChangedEvent>(_ => UpdateScrollVisuals());
        _content.RegisterCallback<GeometryChangedEvent>(_ => UpdateScrollVisuals());
        _scrollbarTrack.RegisterCallback<GeometryChangedEvent>(_ => UpdateScrollVisuals());

        _scrollbarTrack.RegisterCallback<PointerDownEvent>(OnScrollbarTrackPointerDown);
        _scrollbarThumb.RegisterCallback<PointerDownEvent>(OnScrollbarThumbPointerDown);
        _scrollbarThumb.RegisterCallback<PointerMoveEvent>(OnScrollbarThumbPointerMove);
        _scrollbarThumb.RegisterCallback<PointerUpEvent>(OnScrollbarThumbPointerUp);
    }

    private void SuppressKeyboardEvent(EventBase evt)
    {
        evt.StopImmediatePropagation();
    }

    private void SuppressNavigationEvent(EventBase evt)
    {
        evt.StopImmediatePropagation();
    }

    private void OnExplorerFocusIn(FocusInEvent evt)
    {
        _explorerRoot?.Blur();
        evt.StopImmediatePropagation();
    }

    private void OnWheelScrolled(WheelEvent evt)
    {
        SetScrollOffset(_scrollOffset + evt.delta.y * _wheelStep);
        evt.StopPropagation();
    }

    private void OnScrollbarTrackPointerDown(PointerDownEvent evt)
    {
        if (evt.target == _scrollbarThumb)
            return;

        float trackHeight = GetTrackHeight();
        float thumbHeight = GetThumbHeight(trackHeight);
        float clickY = evt.localPosition.y;
        float newThumbTop = Mathf.Clamp(clickY - thumbHeight * 0.5f, 0f, Mathf.Max(0f, trackHeight - thumbHeight));
        SetScrollOffset(ThumbTopToScrollOffset(newThumbTop, trackHeight, thumbHeight));
        evt.StopPropagation();
    }

    private void OnScrollbarThumbPointerDown(PointerDownEvent evt)
    {
        _draggingThumb = true;
        _thumbDragStartMouseY = evt.position.y;
        _thumbDragStartOffset = _scrollOffset;
        _scrollbarThumb.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnScrollbarThumbPointerMove(PointerMoveEvent evt)
    {
        if (!_draggingThumb || !_scrollbarThumb.HasPointerCapture(evt.pointerId))
            return;

        float trackHeight = GetTrackHeight();
        float thumbHeight = GetThumbHeight(trackHeight);
        float maxScroll = GetMaxScrollOffset();
        float availableTrack = Mathf.Max(1f, trackHeight - thumbHeight);
        float deltaY = evt.position.y - _thumbDragStartMouseY;
        float scrollDelta = deltaY * maxScroll / availableTrack;

        SetScrollOffset(_thumbDragStartOffset + scrollDelta);
        evt.StopPropagation();
    }

    private void OnScrollbarThumbPointerUp(PointerUpEvent evt)
    {
        if (_scrollbarThumb.HasPointerCapture(evt.pointerId))
            _scrollbarThumb.ReleasePointer(evt.pointerId);

        _draggingThumb = false;
        evt.StopPropagation();
    }

    private void SetScrollOffset(float offset)
    {
        _scrollOffset = Mathf.Clamp(offset, 0f, GetMaxScrollOffset());
        UpdateScrollVisuals();
    }

    private float GetMaxScrollOffset()
    {
        float viewportHeight = _viewport != null ? _viewport.resolvedStyle.height : 0f;
        return Mathf.Max(0f, _contentHeight - viewportHeight);
    }

    private float GetTrackHeight()
    {
        return _scrollbarTrack != null ? _scrollbarTrack.resolvedStyle.height : 0f;
    }

    private float GetThumbHeight(float trackHeight)
    {
        float viewportHeight = _viewport != null ? _viewport.resolvedStyle.height : 0f;
        if (_contentHeight <= 0f || viewportHeight <= 0f || trackHeight <= 0f)
            return trackHeight;

        if (_contentHeight <= viewportHeight)
            return trackHeight;

        return Mathf.Clamp(trackHeight * (viewportHeight / _contentHeight), _minThumbHeight, trackHeight);
    }

    private float ThumbTopToScrollOffset(float thumbTop, float trackHeight, float thumbHeight)
    {
        float maxScroll = GetMaxScrollOffset();
        float availableTrack = Mathf.Max(1f, trackHeight - thumbHeight);
        if (maxScroll <= 0f)
            return 0f;

        return thumbTop / availableTrack * maxScroll;
    }

    private void UpdateScrollVisuals()
    {
        if (_content == null || _viewport == null || _scrollbarTrack == null || _scrollbarThumb == null)
            return;

        _contentHeight = _rowVisuals.Count * _rowHeight;
        float viewportHeight = _viewport.resolvedStyle.height;
        float trackHeight = GetTrackHeight();
        float maxScroll = GetMaxScrollOffset();

        _scrollOffset = Mathf.Clamp(_scrollOffset, 0f, maxScroll);
        _content.style.top = -_scrollOffset;

        bool needsScroll = maxScroll > 0.5f && viewportHeight > 0f && trackHeight > 0f;
        _scrollbarTrack.style.display = needsScroll ? DisplayStyle.Flex : DisplayStyle.None;

        if (!needsScroll)
        {
            _scrollbarThumb.style.top = 0f;
            _scrollbarThumb.style.height = trackHeight;
            return;
        }

        float thumbHeight = GetThumbHeight(trackHeight);
        float availableTrack = Mathf.Max(1f, trackHeight - thumbHeight);
        float thumbTop = maxScroll <= 0f ? 0f : (_scrollOffset / maxScroll) * availableTrack;

        _scrollbarThumb.style.top = thumbTop;
        _scrollbarThumb.style.height = thumbHeight;
    }

    private void BuildFileTreeFromInjectedFileSet()
    {
        _rootItems.Clear();
        _entriesByPath.Clear();
        _runtimeDocuments.Clear();
        _currentOpenFilePath = null;

        if (_fileSet == null || _fileSet.files == null)
        {
            Log("No injected file set assigned.");
            return;
        }

        for (int i = 0; i < _fileSet.files.Count; i++)
        {
            ConsoleLevelFileEntry entry = _fileSet.files[i];
            if (entry == null)
                continue;

            string normalizedPath = NormalizePath(entry.virtualPath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
                continue;

            _entriesByPath[normalizedPath] = entry;
            _runtimeDocuments[normalizedPath] = BuildLinesForEntry(entry);
            AddPathToTree(normalizedPath);
        }
    }

    private void AddPathToTree(string normalizedPath)
    {
        string[] segments = normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return;

        List<ExplorerItem> currentLevel = _rootItems;
        string runningPath = string.Empty;

        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            bool isLeaf = i == segments.Length - 1;
            runningPath = string.IsNullOrEmpty(runningPath) ? segment : runningPath + "/" + segment;

            ExplorerItem child = FindChild(currentLevel, segment, !isLeaf);
            if (child == null)
            {
                child = isLeaf
                    ? File(segment, runningPath)
                    : Dir(segment, runningPath);

                currentLevel.Add(child);
            }

            if (!isLeaf)
                currentLevel = child.Children;
        }
    }

    private ExplorerItem FindChild(List<ExplorerItem> children, string name, bool isDirectory)
    {
        if (children == null)
            return null;

        for (int i = 0; i < children.Count; i++)
        {
            ExplorerItem child = children[i];
            if (child == null)
                continue;

            if (child.IsDirectory == isDirectory &&
                string.Equals(child.Node.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private ExplorerItem Dir(string name, string path, params ExplorerItem[] children)
    {
        ExplorerItem item = new ExplorerItem(new VirtualFileNode(name, true, path), true);
        if (children != null)
        {
            foreach (ExplorerItem child in children)
            {
                if (child == null)
                    continue;

                child.Parent = item;
                item.Children.Add(child);
            }
        }

        return item;
    }

    private ExplorerItem File(string name, string path)
    {
        return new ExplorerItem(new VirtualFileNode(name, false, path), false);
    }

    private void RebuildVisibleRows()
    {
        if (_content == null)
            return;

        _content.Clear();
        _rowVisuals.Clear();

        foreach (ExplorerItem item in _rootItems)
            AddVisibleRowsRecursive(item, 0);

        UpdateScrollVisuals();
    }

    private void AddVisibleRowsRecursive(ExplorerItem item, int depth)
    {
        RowVisual row = CreateRow(item, depth);
        _rowVisuals.Add(row);
        _content.Add(row.Root);

        if (!item.IsDirectory || !item.IsExpanded)
            return;

        for (int i = 0; i < item.Children.Count; i++)
            AddVisibleRowsRecursive(item.Children[i], depth + 1);
    }

    private RowVisual CreateRow(ExplorerItem item, int depth)
    {
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.justifyContent = Justify.FlexStart;
        row.style.flexGrow = 0f;
        row.style.flexShrink = 0f;
        row.style.minHeight = _rowHeight;
        row.style.height = _rowHeight;
        row.style.marginTop = 0f;
        row.style.marginBottom = 0f;
        row.style.paddingLeft = _rowHorizontalPadding + depth * _indentWidth;
        row.style.paddingRight = _rowHorizontalPadding;
        row.style.borderLeftWidth = 0f;
        row.style.borderRightWidth = 0f;
        row.style.borderTopWidth = 0f;
        row.style.borderBottomWidth = 0f;
        row.focusable = false;
        row.tabIndex = -1;

        Label toggle = new Label();
        toggle.style.width = _iconWidth;
        toggle.style.minWidth = _iconWidth;
        toggle.style.unityTextAlign = TextAnchor.MiddleCenter;
        toggle.style.fontSize = _fontSize - 1;
        toggle.style.marginRight = 2f;
        toggle.text = item.IsDirectory ? (item.IsExpanded ? "▾" : "▸") : "";

        Label label = new Label(item.Node.Name);
        label.style.flexGrow = 1f;
        label.style.flexShrink = 1f;
        label.style.minWidth = 0f;
        label.style.fontSize = _fontSize;
        label.style.unityTextAlign = TextAnchor.MiddleLeft;
        label.style.whiteSpace = WhiteSpace.NoWrap;
        label.style.overflow = Overflow.Hidden;

        row.Add(toggle);
        row.Add(label);

        RowVisual visual = new RowVisual
        {
            Root = row,
            Toggle = toggle,
            Label = label,
            Item = item,
            Depth = depth
        };

        row.RegisterCallback<PointerEnterEvent>(_ => ApplyRowStyle(visual, true));
        row.RegisterCallback<PointerLeaveEvent>(_ => ApplyRowStyle(visual, false));
        row.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (item.IsDirectory)
            {
                item.IsExpanded = !item.IsExpanded;
                RebuildVisibleRows();
            }
            else
            {
                HandleNodeClicked(item.Node);
            }

            evt.StopPropagation();
        });

        ApplyRowStyle(visual, false);
        return visual;
    }

    private void RefreshRowVisuals()
    {
        for (int i = 0; i < _rowVisuals.Count; i++)
            ApplyRowStyle(_rowVisuals[i], false);

        UpdateScrollVisuals();
    }

    private void ApplyRowStyle(RowVisual visual, bool hovered)
    {
        if (visual == null || visual.Root == null)
            return;

        Color surface = _theme != null ? _theme.backgroundSurface : new Color(0.16f, 0.16f, 0.16f, 1f);
        Color active = _theme != null ? _theme.backgroundActive : new Color(0.24f, 0.24f, 0.24f, 1f);
        Color text = _theme != null ? _theme.text : Color.white;
        Color track = _theme != null ? new Color(_theme.text.r, _theme.text.g, _theme.text.b, 0.08f) : new Color(1f, 1f, 1f, 0.08f);
        Color thumb = _theme != null ? new Color(_theme.text.r, _theme.text.g, _theme.text.b, 0.34f) : new Color(1f, 1f, 1f, 0.34f);

        visual.Root.style.backgroundColor = hovered ? active : Color.clear;
        visual.Root.style.color = text;
        visual.Root.style.paddingLeft = _rowHorizontalPadding + visual.Depth * _indentWidth;
        visual.Root.style.paddingRight = _rowHorizontalPadding;

        if (visual.Toggle != null)
        {
            visual.Toggle.text = visual.Item.IsDirectory ? (visual.Item.IsExpanded ? "▾" : "▸") : "";
            visual.Toggle.style.color = text;
        }

        if (visual.Label != null)
        {
            visual.Label.text = visual.Item.Node.Name;
            visual.Label.style.color = text;
            visual.Label.style.unityFontStyleAndWeight = visual.Item.IsDirectory ? FontStyle.Bold : FontStyle.Normal;
            visual.Label.style.unityTextAlign = TextAnchor.MiddleLeft;
        }

        if (_mainView != null)
            _mainView.style.backgroundColor = surface;

        if (_explorerRoot != null)
            _explorerRoot.style.backgroundColor = surface;

        if (_viewport != null)
            _viewport.style.backgroundColor = surface;

        if (_content != null)
            _content.style.backgroundColor = surface;

        if (_scrollbarTrack != null)
            _scrollbarTrack.style.backgroundColor = track;

        if (_scrollbarThumb != null)
            _scrollbarThumb.style.backgroundColor = thumb;
    }

    private void HandleNodeClicked(VirtualFileNode node)
    {
        Log($"HandleNodeClicked: name='{node?.Name}', path='{node?.Path}', isDirectory={node?.IsDirectory}");
        if (node == null || node.IsDirectory)
            return;

        SaveCurrentOpenDocument();

        object openResult = InvokeTabbedOpenFile(node);
        ConsoleStateManager targetStateManager = ResolveStateManagerFromOpenResult(openResult);

        if (targetStateManager == null)
            targetStateManager = _consoleStateManager;

        if (targetStateManager == null)
        {
            Debug.LogWarning("[FileHierarchy] No ConsoleStateManager available to load document content.", this);
            return;
        }

        LoadDocumentIntoConsole(node.Path, targetStateManager);
        FileClicked?.Invoke(node);
    }

    private object InvokeTabbedOpenFile(VirtualFileNode node)
    {
        if (_tabbedContent == null || node == null)
            return null;

        try
        {
            MethodInfo[] methods = _tabbedContent.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (!string.Equals(method.Name, "OpenFile", StringComparison.Ordinal))
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1)
                    continue;

                Type paramType = parameters[0].ParameterType;
                if (!paramType.IsAssignableFrom(node.GetType()))
                    continue;

                return method.Invoke(_tabbedContent, new object[] { node });
            }

            Log("No matching OpenFile(VirtualFileNode) method found on tabbed content.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FileHierarchy] Failed to invoke tabbed OpenFile: {e.Message}", this);
        }

        return null;
    }

    private ConsoleStateManager ResolveStateManagerFromOpenResult(object openResult)
    {
        if (openResult == null)
            return null;

        if (openResult is ConsoleStateManager directStateManager)
            return directStateManager;

        Type resultType = openResult.GetType();

        try
        {
            FieldInfo stateField = resultType.GetField("stateManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (stateField != null)
            {
                object value = stateField.GetValue(openResult);
                if (value is ConsoleStateManager fieldStateManager)
                    return fieldStateManager;
            }

            PropertyInfo stateProperty = resultType.GetProperty("stateManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (stateProperty != null)
            {
                object value = stateProperty.GetValue(openResult, null);
                if (value is ConsoleStateManager propertyStateManager)
                    return propertyStateManager;
            }

            PropertyInfo pascalProperty = resultType.GetProperty("StateManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pascalProperty != null)
            {
                object value = pascalProperty.GetValue(openResult, null);
                if (value is ConsoleStateManager pascalStateManager)
                    return pascalStateManager;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FileHierarchy] Failed to resolve state manager from tab open result: {e.Message}", this);
        }

        return null;
    }

    private void SaveCurrentOpenDocument()
    {
        if (!_preserveEditsWhenSwitchingFiles)
            return;

        if (string.IsNullOrWhiteSpace(_currentOpenFilePath))
            return;

        ConsoleStateManager sourceStateManager = _activeStateManager != null ? _activeStateManager : _consoleStateManager;
        if (sourceStateManager == null)
            return;

        _runtimeDocuments[_currentOpenFilePath] = sourceStateManager.ExportLines();
    }

    private void LoadDocumentIntoConsole(string path, ConsoleStateManager targetStateManager)
    {
        if (targetStateManager == null)
        {
            Debug.LogWarning("[FileHierarchy] No target ConsoleStateManager assigned.", this);
            return;
        }

        string normalizedPath = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalizedPath))
            return;

        if (!_runtimeDocuments.TryGetValue(normalizedPath, out List<ConsoleStateManager.Line> documentLines))
        {
            if (_entriesByPath.TryGetValue(normalizedPath, out ConsoleLevelFileEntry entry))
            {
                documentLines = BuildLinesForEntry(entry);
                _runtimeDocuments[normalizedPath] = documentLines;
            }
            else
            {
                documentLines = new List<ConsoleStateManager.Line>
                {
                    new ConsoleStateManager.Line(string.Empty, false)
                };
            }
        }

        targetStateManager.LoadLines(CloneLines(documentLines));
        _activeStateManager = targetStateManager;
        _currentOpenFilePath = normalizedPath;
    }

    private List<ConsoleStateManager.Line> BuildLinesForEntry(ConsoleLevelFileEntry entry)
    {
        string text = entry != null && entry.fileAsset != null ? entry.fileAsset.text : string.Empty;
        text = NormalizeNewlines(text);

        string[] rawLines = text.Split('\n');
        if (rawLines == null || rawLines.Length == 0)
            rawLines = new[] { string.Empty };

        HashSet<int> lockedLineIndices = ParseLockedLineSpec(entry != null ? entry.lockedLines : null, rawLines.Length);

        List<ConsoleStateManager.Line> result = new List<ConsoleStateManager.Line>(rawLines.Length);
        for (int i = 0; i < rawLines.Length; i++)
            result.Add(new ConsoleStateManager.Line(rawLines[i], lockedLineIndices.Contains(i)));

        return result;
    }

    private HashSet<int> ParseLockedLineSpec(string spec, int lineCount)
    {
        HashSet<int> locked = new HashSet<int>();

        if (lineCount <= 0 || string.IsNullOrWhiteSpace(spec))
            return locked;

        string[] tokens = spec.Split(',');
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim();
            if (string.IsNullOrEmpty(token))
                continue;

            if (string.Equals(token, "all", StringComparison.OrdinalIgnoreCase))
            {
                for (int line = 0; line < lineCount; line++)
                    locked.Add(line);
                continue;
            }

            string[] rangeParts = token.Split('-');
            if (rangeParts.Length == 2)
            {
                if (!int.TryParse(rangeParts[0].Trim(), out int start1Based) ||
                    !int.TryParse(rangeParts[1].Trim(), out int end1Based))
                {
                    Debug.LogWarning($"[FileHierarchy] Invalid lock token '{token}' on {gameObject.name}", this);
                    continue;
                }

                if (end1Based < start1Based)
                {
                    int temp = start1Based;
                    start1Based = end1Based;
                    end1Based = temp;
                }

                start1Based = Mathf.Clamp(start1Based, 1, lineCount);
                end1Based = Mathf.Clamp(end1Based, 1, lineCount);

                for (int line = start1Based; line <= end1Based; line++)
                    locked.Add(line - 1);

                continue;
            }

            if (int.TryParse(token, out int single1Based))
            {
                if (single1Based >= 1 && single1Based <= lineCount)
                {
                    locked.Add(single1Based - 1);
                }
                else
                {
                    Debug.LogWarning($"[FileHierarchy] Lock line '{single1Based}' is out of range 1-{lineCount} on {gameObject.name}", this);
                }

                continue;
            }

            Debug.LogWarning($"[FileHierarchy] Invalid lock token '{token}' on {gameObject.name}. Supported forms: all, N, N-M", this);
        }

        return locked;
    }

    private List<ConsoleStateManager.Line> CloneLines(List<ConsoleStateManager.Line> source)
    {
        List<ConsoleStateManager.Line> clone = new List<ConsoleStateManager.Line>();
        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            ConsoleStateManager.Line line = source[i];
            clone.Add(new ConsoleStateManager.Line(line.content, line.locked));
        }

        return clone;
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        path = path.Replace('\\', '/').Trim();

        while (path.StartsWith("/"))
            path = path.Substring(1);

        while (path.EndsWith("/"))
            path = path.Substring(0, path.Length - 1);

        return path;
    }

    private string NormalizeNewlines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private void Log(string message)
    {
        if (_verboseLogging)
            Debug.Log($"[FileHierarchy] {message}", this);
    }

    private sealed class ExplorerItem
    {
        public VirtualFileNode Node { get; }
        public bool IsDirectory { get; }
        public bool IsExpanded { get; set; }
        public ExplorerItem Parent { get; set; }
        public List<ExplorerItem> Children { get; }

        public ExplorerItem(VirtualFileNode node, bool isDirectory)
        {
            Node = node;
            IsDirectory = isDirectory;
            IsExpanded = true;
            Children = new List<ExplorerItem>();
        }
    }

    private sealed class RowVisual
    {
        public VisualElement Root;
        public Label Toggle;
        public Label Label;
        public ExplorerItem Item;
        public int Depth;
    }
}

[Serializable]
public class VirtualFileNode
{
    public string Name;
    public bool IsDirectory;
    public string Path;

    public VirtualFileNode(string name, bool isDirectory)
        : this(name, isDirectory, name)
    {
    }

    public VirtualFileNode(string name, bool isDirectory, string path)
    {
        Name = name;
        IsDirectory = isDirectory;
        Path = string.IsNullOrWhiteSpace(path) ? name : path;
    }
}