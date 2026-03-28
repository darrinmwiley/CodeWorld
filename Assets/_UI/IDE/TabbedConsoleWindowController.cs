using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class TabbedConsoleWindowController : WindowComponent
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset _tabbedContentAsset;

    [Header("Tab Content")]
    [SerializeField] private ConsoleWindowController _consoleWindowPrefab;
    [SerializeField] private string _untitledPrefix = "Untitled";

    [Header("Sizing")]
    [SerializeField] private Vector2 _baseMinimumSize = new Vector2(260f, 150f);
    [SerializeField] private float _headerHeight = 28f;
    [SerializeField] private float _tabMinWidth = 120f;
    [SerializeField] private float _tabMaxWidth = 220f;

    [Header("Tab Behavior")]
    [SerializeField] private bool _tabsClosable = true;

    [Header("Debug")]
    [SerializeField] private bool _verboseLogging = false;

    private const string RootClass = "tabbed-console-root";
    private const string HeaderRowClass = "tabbed-console-header-row";
    private const string HeaderScrollClass = "tabbed-console-header-scroll";
    private const string HeaderStripClass = "tabbed-console-header-strip";
    private const string TabClass = "tabbed-console-tab";
    private const string ActiveTabClass = "tabbed-console-tab--active";
    private const string TabLabelClass = "tabbed-console-tab-label";
    private const string TabCloseClass = "tabbed-console-tab-close";
    private const string ContentViewportClass = "tabbed-console-content-viewport";
    private const string ContentHostClass = "tabbed-console-tab-content";

    private IBaseWindow _rootWindow;
    private VisualElement _hostContainer;
    private VisualElement _runtimeRoot;
    private VisualElement _headerRow;
    private ScrollView _headerScrollView;
    private VisualElement _headerStrip;
    private VisualElement _contentViewport;

    private int _untitledCounter = 1;
    private readonly Dictionary<string, TabData> _tabsByKey = new Dictionary<string, TabData>();
    private readonly List<TabData> _tabOrder = new List<TabData>();

    public string ActiveTabKey { get; private set; }
    public int TabCount => _tabOrder.Count;

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        _rootWindow = root;
        _hostContainer = container;

        ConfigureFillElement(_hostContainer, clip: true);

        TemplateContainer styledRoot = _tabbedContentAsset != null ? _tabbedContentAsset.Instantiate() : new TemplateContainer();
        styledRoot.Clear();
        ConfigureFillElement(styledRoot, clip: true);

        _runtimeRoot = BuildRuntimeLayout();
        styledRoot.Add(_runtimeRoot);

        _hostContainer.Clear();
        _hostContainer.Add(styledRoot);

        _hostContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        _headerScrollView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

        NotifyRootConstraintsChanged();
        Log("Initialize complete.");
        InitializeSubComponents(styledRoot, root);
    }

    public override Vector2 GetMinimumSize()
    {
        return _baseMinimumSize;
    }

    public ConsoleWindowController OpenFile(VirtualFileNode fileNode)
    {
        if (fileNode == null || fileNode.IsDirectory)
            return null;

        return OpenFile(fileNode.Path, fileNode.Name);
    }

    public ConsoleWindowController OpenFile(string filePath, string title = null)
    {
        string key = string.IsNullOrWhiteSpace(filePath) ? GetNextUntitledKey() : filePath;
        string resolvedTitle = string.IsNullOrWhiteSpace(title) ? Path.GetFileName(key) : title;

        if (string.IsNullOrWhiteSpace(resolvedTitle))
            resolvedTitle = $"{_untitledPrefix} {_untitledCounter++}";

        if (_tabsByKey.TryGetValue(key, out TabData existingTab))
        {
            SelectTab(existingTab.Key);
            return existingTab.Console;
        }

        return AddConsoleTabInternal(key, resolvedTitle, filePath);
    }

    public bool CloseTab(string tabKey)
    {
        if (string.IsNullOrWhiteSpace(tabKey) || !_tabsByKey.TryGetValue(tabKey, out TabData tabData))
            return false;

        CloseTabInternal(tabData);
        return true;
    }

    public void SelectTab(string tabKey)
    {
        if (string.IsNullOrWhiteSpace(tabKey) || !_tabsByKey.TryGetValue(tabKey, out TabData selectedTab))
            return;

        ActiveTabKey = tabKey;

        foreach (TabData tabData in _tabOrder)
        {
            bool isActive = ReferenceEquals(tabData, selectedTab);

            if (tabData.HeaderRoot != null)
                tabData.HeaderRoot.EnableInClassList(ActiveTabClass, isActive);

            if (tabData.ContentHost != null)
                tabData.ContentHost.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;

            if (tabData.Console != null)
            {
                if (isActive)
                    tabData.Console.BringToFront();
                else
                    tabData.Console.DefocusInternal();
            }
        }

        EnsureHeaderVisible(selectedTab);
        NotifyRootConstraintsChanged();
        Log($"Selected tab '{tabKey}' at index {GetActiveTabIndex()}");
    }

    private VisualElement BuildRuntimeLayout()
    {
        VisualElement root = new VisualElement();
        root.AddToClassList(RootClass);
        ConfigureFillElement(root, clip: true);
        root.style.flexDirection = FlexDirection.Column;

        _headerRow = new VisualElement();
        _headerRow.AddToClassList(HeaderRowClass);
        _headerRow.style.flexDirection = FlexDirection.Row;
        _headerRow.style.flexGrow = 0f;
        _headerRow.style.flexShrink = 0f;
        _headerRow.style.height = _headerHeight;
        _headerRow.style.minHeight = _headerHeight;
        _headerRow.style.overflow = Overflow.Hidden;

        _headerScrollView = new ScrollView(ScrollViewMode.Horizontal);
        _headerScrollView.AddToClassList(HeaderScrollClass);
        _headerScrollView.style.flexGrow = 1f;
        _headerScrollView.style.flexShrink = 1f;
        _headerScrollView.style.minWidth = 0f;
        _headerScrollView.style.minHeight = _headerHeight;
        _headerScrollView.style.height = _headerHeight;
        _headerScrollView.style.overflow = Overflow.Hidden;
        _headerScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        _headerScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        _headerScrollView.contentContainer.style.flexDirection = FlexDirection.Row;
        _headerScrollView.contentContainer.style.flexGrow = 0f;
        _headerScrollView.contentContainer.style.flexShrink = 0f;
        _headerScrollView.contentContainer.style.minHeight = _headerHeight;
        _headerScrollView.contentContainer.style.height = _headerHeight;

        _headerStrip = _headerScrollView.contentContainer;
        _headerStrip.AddToClassList(HeaderStripClass);

        _contentViewport = new VisualElement();
        _contentViewport.AddToClassList(ContentViewportClass);
        ConfigureFillElement(_contentViewport, clip: true);

        _headerRow.Add(_headerScrollView);
        root.Add(_headerRow);
        root.Add(_contentViewport);
        return root;
    }

    private ConsoleWindowController AddConsoleTabInternal(string key, string title, string filePath)
    {
        if (_consoleWindowPrefab == null)
        {
            Debug.LogError("[TabbedConsole] No console prefab assigned.", this);
            return null;
        }

        VisualElement headerRoot = new VisualElement();
        headerRoot.AddToClassList(TabClass);
        headerRoot.style.minWidth = _tabMinWidth;
        headerRoot.style.maxWidth = _tabMaxWidth;
        headerRoot.style.height = _headerHeight;
        headerRoot.style.minHeight = _headerHeight;
        headerRoot.style.flexShrink = 0f;
        headerRoot.style.flexDirection = FlexDirection.Row;
        headerRoot.style.alignItems = Align.Center;

        Label headerLabel = new Label(title);
        headerLabel.AddToClassList(TabLabelClass);
        headerRoot.Add(headerLabel);

        Button closeButton = null;
        if (_tabsClosable)
        {
            closeButton = new Button();
            closeButton.text = "×";
            closeButton.AddToClassList(TabCloseClass);
            closeButton.clicked += () =>
            {
                if (_tabsByKey.TryGetValue(key, out TabData existing))
                    CloseTabInternal(existing);
            };
            closeButton.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            headerRoot.Add(closeButton);
        }

        headerRoot.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            SelectTab(key);
            evt.StopPropagation();
        });

        VisualElement contentHost = new VisualElement();
        contentHost.AddToClassList(ContentHostClass);
        ConfigureFillElement(contentHost, clip: true);
        contentHost.style.display = DisplayStyle.None;

        _headerStrip.Add(headerRoot);
        _contentViewport.Add(contentHost);

        GameObject instance = Instantiate(_consoleWindowPrefab.gameObject, transform);
        instance.name = $"ConsoleTab_{title}";

        ConsoleWindowController console = instance.GetComponent<ConsoleWindowController>();
        if (console == null)
        {
            Debug.LogError("[TabbedConsole] Assigned prefab does not contain a ConsoleWindowController.", this);

            if (Application.isPlaying) Destroy(instance);
            else DestroyImmediate(instance);

            headerRoot.RemoveFromHierarchy();
            contentHost.RemoveFromHierarchy();
            return null;
        }

        console.Initialize(contentHost, _rootWindow);

        TabData tabData = new TabData
        {
            Key = key,
            Title = title,
            FilePath = filePath,
            HeaderRoot = headerRoot,
            HeaderLabel = headerLabel,
            CloseButton = closeButton,
            ContentHost = contentHost,
            Console = console,
            ConsoleInstance = instance
        };

        _tabsByKey.Add(key, tabData);
        _tabOrder.Add(tabData);

        SelectTab(key);
        Log($"Added tab '{title}' for key '{key}'. Total tabs: {_tabOrder.Count}");
        return console;
    }

    private void CloseTabInternal(TabData tabData)
    {
        if (tabData == null)
            return;

        int closedIndex = _tabOrder.IndexOf(tabData);
        bool wasActive = ActiveTabKey == tabData.Key;

        _tabsByKey.Remove(tabData.Key);
        _tabOrder.Remove(tabData);

        if (tabData.HeaderRoot != null)
            tabData.HeaderRoot.RemoveFromHierarchy();

        if (tabData.ContentHost != null)
            tabData.ContentHost.RemoveFromHierarchy();

        if (tabData.ConsoleInstance != null)
        {
            if (Application.isPlaying) Destroy(tabData.ConsoleInstance);
            else DestroyImmediate(tabData.ConsoleInstance);
        }

        if (wasActive)
        {
            if (_tabOrder.Count == 0)
            {
                ActiveTabKey = null;
            }
            else
            {
                int nextIndex = Mathf.Clamp(closedIndex, 0, _tabOrder.Count - 1);
                SelectTab(_tabOrder[nextIndex].Key);
            }
        }
        else
        {
            NotifyRootConstraintsChanged();
        }

        Log($"Closed tab '{tabData.Key}'. Remaining tabs: {_tabOrder.Count}");
    }

    private int GetActiveTabIndex()
    {
        if (string.IsNullOrWhiteSpace(ActiveTabKey))
            return -1;

        for (int i = 0; i < _tabOrder.Count; i++)
        {
            if (_tabOrder[i].Key == ActiveTabKey)
                return i;
        }

        return -1;
    }

    private void EnsureHeaderVisible(TabData tabData)
    {
        if (tabData?.HeaderRoot == null || _headerScrollView == null)
            return;

        _headerScrollView.schedule.Execute(() =>
        {
            float currentOffset = _headerScrollView.scrollOffset.x;
            float viewportWidth = _headerScrollView.contentViewport.resolvedStyle.width;
            float left = tabData.HeaderRoot.layout.xMin;
            float right = tabData.HeaderRoot.layout.xMax;

            float newOffset = currentOffset;
            if (left < currentOffset)
                newOffset = left;
            else if (right > currentOffset + viewportWidth)
                newOffset = Mathf.Max(0f, right - viewportWidth);

            if (!Mathf.Approximately(newOffset, currentOffset))
                _headerScrollView.scrollOffset = new Vector2(newOffset, 0f);
        });
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        NotifyRootConstraintsChanged();
    }

    private void NotifyRootConstraintsChanged()
    {
        _rootWindow?.UpdateRootConstraints(GetMinimumSize());
    }

    private void ConfigureFillElement(VisualElement element, bool clip)
    {
        if (element == null)
            return;

        element.style.flexGrow = 1f;
        element.style.minWidth = 0f;
        element.style.minHeight = 0f;
        element.style.width = Length.Percent(100);
        element.style.height = Length.Percent(100);

        if (clip)
            element.style.overflow = Overflow.Hidden;
    }

    private string GetNextUntitledKey()
    {
        string key;
        do
        {
            key = $"{_untitledPrefix}_{_untitledCounter++}";
        }
        while (_tabsByKey.ContainsKey(key));

        return key;
    }

    private void Log(string message)
    {
        if (_verboseLogging)
            Debug.Log($"[TabbedConsole] {message}", this);
    }

    private sealed class TabData
    {
        public string Key;
        public string Title;
        public string FilePath;
        public VisualElement HeaderRoot;
        public Label HeaderLabel;
        public Button CloseButton;
        public VisualElement ContentHost;
        public ConsoleWindowController Console;
        public GameObject ConsoleInstance;
    }
}
