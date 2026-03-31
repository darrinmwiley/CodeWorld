using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class OptionBarWrapperController : WindowComponent
{
    [Serializable]
    public struct DropdownItem
    {
        public string label;
        public string commandCode;
    }

    [Serializable]
    public struct DropdownDefinition
    {
        public string menuLabel;
        public List<DropdownItem> items;
    }

    [Serializable]
    public struct ToolbarDefinition
    {
        public Texture2D icon;
        public string tooltip;
        public string commandCode;
    }

    [Header("Theme")]
    [SerializeField] private UITheme _theme;

    [Header("Layout Settings")]
    [SerializeField] private float _barHeight = 30f;
    [SerializeField] private float _dropdownWidth = 180f;

    [Header("Injected Content")]
    [SerializeField] private List<DropdownDefinition> _dropdowns = new List<DropdownDefinition>();
    [SerializeField] private List<ToolbarDefinition> _toolbarButtons = new List<ToolbarDefinition>();

    private VisualElement _topBar;
    private VisualElement _menuContainer;
    private VisualElement _toolbarContainer;
    private VisualElement _contentViewport;
    
    // The "Ghost" fix: Ensure this is nulled out properly
    private VisualElement _activeMenuOverlay;
    private IBaseWindow _windowRoot;

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        _windowRoot = root;
        container.Clear();
        container.style.flexDirection = FlexDirection.Column;

        _topBar = new VisualElement { name = "OptionBar" };
        _topBar.style.height = _barHeight;
        _topBar.style.flexDirection = FlexDirection.Row;
        _topBar.style.alignItems = Align.Center;
        _topBar.style.paddingLeft = 5;

        _menuContainer = new VisualElement { name = "MenuContainer" };
        _menuContainer.style.flexDirection = FlexDirection.Row;
        _menuContainer.style.flexGrow = 1;

        _toolbarContainer = new VisualElement { name = "ToolbarContainer" };
        _toolbarContainer.style.flexDirection = FlexDirection.Row;

        _topBar.Add(_menuContainer);
        _topBar.Add(_toolbarContainer);

        _contentViewport = new VisualElement { name = "ContentViewport" };
        _contentViewport.style.flexGrow = 1;

        container.Add(_topBar);
        container.Add(_contentViewport);

        foreach (var def in _dropdowns)
            AddDropdownMenu(def);

        foreach (var tool in _toolbarButtons)
            AddToolbarButton(tool);

        InitializeSubComponents(container, root);

        // This ensures clicking the background closes any stray menus
        container.RegisterCallback<PointerDownEvent>(evt => CloseActiveMenu(), TrickleDown.NoTrickleDown);

        ApplyTheme(_theme);
    }

    /// <summary>
    /// Central command hub. Add your logic here!
    /// </summary>
    private void ExecuteCommand(string code)
    {
        if (string.IsNullOrEmpty(code)) return;
        
        Debug.Log($"[OptionBar] Command Received: {code}");

        switch (code)
        {
            case "FILE_SAVE":
                // Save logic
                break;
            case "EXIT_APP":
                Application.Quit();
                break;
            case "PLAY_CODE":
            case "EXECUTE_CODE":
                var executor = FindObjectOfType<CodeExecutor>();
                if (executor != null)
                {
                    executor.ExecuteActiveTab();
                }
                else
                {
                    Debug.LogWarning("[OptionBar] Could not find CodeExecutor in the scene.");
                }
                break;
            default:
                break;
        }
    }

    private void AddDropdownMenu(DropdownDefinition def)
    {
        Button menuBtn = new Button { text = def.menuLabel };
        StyleMenuButton(menuBtn);
        // We use an anonymous function to ensure ToggleMenu gets the right anchor
        menuBtn.clicked += () => ToggleMenu(menuBtn, def.items);
        _menuContainer.Add(menuBtn);
    }

    private void ToggleMenu(Button anchor, List<DropdownItem> items)
    {
        // Check if we are clicking the same button that is already open
        bool isReclickingSame = (_activeMenuOverlay != null && _activeMenuOverlay.userData == anchor);

        // Always clear the old one first
        CloseActiveMenu();

        // If it was a re-click, we just stop (the menu was closed by CloseActiveMenu)
        if (isReclickingSame) return;

        _activeMenuOverlay = new VisualElement { name = "DropdownOverlay" };
        _activeMenuOverlay.userData = anchor; // Mark the owner
        _activeMenuOverlay.style.position = Position.Absolute;
        _activeMenuOverlay.style.width = _dropdownWidth;
        
        // Manual border application to prevent CS1061
        _activeMenuOverlay.style.borderTopWidth = 1;
        _activeMenuOverlay.style.borderBottomWidth = 1;
        _activeMenuOverlay.style.borderLeftWidth = 1;
        _activeMenuOverlay.style.borderRightWidth = 1;

        // Positioning relative to window root
        _activeMenuOverlay.style.top = _barHeight;
        _activeMenuOverlay.style.left = anchor.worldBound.xMin - _windowRoot.RootElement.worldBound.xMin;

        foreach (var item in items)
        {
            Button itemBtn = new Button(() => {
                ExecuteCommand(item.commandCode);
                CloseActiveMenu();
            }) { text = item.label };
            
            StyleDropdownItem(itemBtn);
            _activeMenuOverlay.Add(itemBtn);
        }

        _windowRoot.RootElement.Add(_activeMenuOverlay);
        _activeMenuOverlay.BringToFront();
        
        // Force theme update on the new element
        RefreshOverlayColors();
    }

    private void CloseActiveMenu()
    {
        if (_activeMenuOverlay != null)
        {
            _activeMenuOverlay.RemoveFromHierarchy();
            _activeMenuOverlay = null; // CRITICAL: Null this out so ToggleMenu can reset
        }
    }

    private void AddToolbarButton(ToolbarDefinition tool)
    {
        Button btn = new Button(() => ExecuteCommand(tool.commandCode));
        btn.tooltip = tool.tooltip;
        btn.style.backgroundImage = tool.icon;
        btn.style.width = _barHeight - 10f;
        btn.style.height = _barHeight - 10f;
        btn.style.marginLeft = 4f;
        StyleToolbarButton(btn);
        _toolbarContainer.Add(btn);
    }

    public override void ApplyTheme(UITheme theme)
    {
        _theme = theme;
        if (theme == null || _topBar == null) return;

        _topBar.style.backgroundColor = theme.backgroundSurface;
        _topBar.style.borderBottomColor = theme.border;
        _topBar.style.borderBottomWidth = 1f;

        RefreshOverlayColors();

        foreach (var child in _menuContainer.Children())
            if (child is Button b) StyleMenuButton(b);

        foreach (var child in _toolbarContainer.Children())
            if (child is Button b) StyleToolbarButton(b);

        if (_subComponents != null)
            foreach (var map in _subComponents)
                map.controller?.ApplyTheme(theme);
    }

    private void RefreshOverlayColors()
    {
        if (_activeMenuOverlay == null || _theme == null) return;

        _activeMenuOverlay.style.backgroundColor = _theme.backgroundSurface;
        _activeMenuOverlay.style.borderTopColor = _theme.border;
        _activeMenuOverlay.style.borderBottomColor = _theme.border;
        _activeMenuOverlay.style.borderLeftColor = _theme.border;
        _activeMenuOverlay.style.borderRightColor = _theme.border;
    }

    private void StyleMenuButton(Button b)
    {
        b.style.backgroundColor = new StyleColor(StyleKeyword.None);
        b.style.borderTopWidth = 0; b.style.borderBottomWidth = 0;
        b.style.borderLeftWidth = 0; b.style.borderRightWidth = 0;
        b.style.color = _theme != null ? _theme.text : Color.white;
        b.style.fontSize = 12;
        b.style.paddingLeft = 8;
        b.style.paddingRight = 8;
    }

    private void StyleDropdownItem(Button b)
    {
        StyleMenuButton(b);
        b.style.unityTextAlign = TextAnchor.MiddleLeft;
        b.style.height = 25;
        b.RegisterCallback<PointerEnterEvent>(e => b.style.backgroundColor = _theme.backgroundActive);
        b.RegisterCallback<PointerLeaveEvent>(e => b.style.backgroundColor = new StyleColor(StyleKeyword.None));
    }

    private void StyleToolbarButton(Button b)
    {
        b.style.borderTopWidth = 0; b.style.borderBottomWidth = 0;
        b.style.borderLeftWidth = 0; b.style.borderRightWidth = 0;
        b.style.backgroundColor = new StyleColor(StyleKeyword.None);
        b.style.unityBackgroundImageTintColor = _theme != null ? _theme.text : Color.white;
    }

    public override Vector2 GetMinimumSize()
    {
        Vector2 min = new Vector2(200f, _barHeight);
        Vector2 childMin = GetAggregatedSubComponentMinimumSize();
        return new Vector2(Mathf.Max(min.x, childMin.x), min.y + childMin.y);
    }
}