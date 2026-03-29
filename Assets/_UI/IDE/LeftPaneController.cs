using UnityEngine;
using UnityEngine.UIElements;

public class LeftTabPaneController : WindowComponent
{
    private const float MinPaneWidth = 50f;

    [Header("Theme")]
    [SerializeField] private UITheme _theme;

    [Header("Layout Asset")]
    [SerializeField] private VisualTreeAsset _layoutAsset;

    [Header("Bindings")]
    [SerializeField] private MultiPaneWindowController _multiPaneController;

    [Header("Element Names")]
    [SerializeField] private string _fileExplorerButtonName = "FileExplorerButton";
    [SerializeField] private string _settingsButtonName = "SettingsButton";

    [Header("Debug")]
    [SerializeField] private bool _verboseLogging = false;

    private VisualElement _container;
    private VisualElement _root;
    private VisualElement _fileExplorerButton;
    private VisualElement _settingsButton;

    private bool _hoveringFileExplorer;
    private bool _hoveringSettings;

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        if (container == null)
            return;

        _container = container;

        if (_multiPaneController == null)
            _multiPaneController = FindObjectOfType<MultiPaneWindowController>();

        _container.Clear();
        _container.style.flexGrow = 0f;
        _container.style.flexShrink = 0f;
        _container.style.width = MinPaneWidth;
        _container.style.minWidth = MinPaneWidth;
        _container.style.maxWidth = MinPaneWidth;
        _container.style.minHeight = 0f;

        ApplyContainerBackground();

        if (_layoutAsset != null)
        {
            _root = _layoutAsset.Instantiate();
            _root.style.flexGrow = 1f;
            _root.style.flexShrink = 0f;
            _root.style.width = Length.Percent(100);
            _root.style.minWidth = MinPaneWidth;
            _root.style.maxWidth = Length.Percent(100);
            _root.style.minHeight = 0f;
            _container.Add(_root);
        }
        else
        {
            _root = _container;
        }

        _fileExplorerButton = FindButtonElement(_root, _fileExplorerButtonName);
        _settingsButton = FindButtonElement(_root, _settingsButtonName);

        HookButton(_fileExplorerButton, OnFileExplorerClicked, OnFileExplorerHoverChanged);
        HookButton(_settingsButton, OnSettingsClicked, OnSettingsHoverChanged);

        if (_multiPaneController != null)
            _multiPaneController.LeftPaneVisibilityChanged += OnLeftPaneVisibilityChanged;

        ApplyTheme(_theme);
        RefreshButtonVisuals();

        InitializeSubComponents(_root, root);
    }

    private void ApplyContainerBackground()
    {
        if (_container == null)
            return;

        Color baseColor = _theme != null
            ? _theme.backgroundActive
            : new Color(0.11f, 0.11f, 0.11f, 1f);

        _container.style.backgroundColor = baseColor;
    }

    private void OnDestroy()
    {
        if (_multiPaneController != null)
            _multiPaneController.LeftPaneVisibilityChanged -= OnLeftPaneVisibilityChanged;
    }

    public override Vector2 GetMinimumSize()
    {
        return new Vector2(MinPaneWidth, 0f);
    }

    public void ApplyTheme(UITheme theme)
    {
        _theme = theme;
        ApplyContainerBackground();
        RefreshButtonVisuals();
    }

    private VisualElement FindButtonElement(VisualElement root, string elementName)
    {
        if (root == null || string.IsNullOrWhiteSpace(elementName))
            return null;

        Button button = root.Q<Button>(elementName);
        if (button != null)
            return button;

        return root.Q<VisualElement>(elementName);
    }

    private void HookButton(VisualElement buttonElement, System.Action clickAction, System.Action<bool> hoverAction)
    {
        if (buttonElement == null)
            return;

        buttonElement.focusable = false;
        buttonElement.tabIndex = -1;

        buttonElement.RegisterCallback<PointerEnterEvent>(_ => hoverAction?.Invoke(true));
        buttonElement.RegisterCallback<PointerLeaveEvent>(_ => hoverAction?.Invoke(false));

        if (buttonElement is Button button)
        {
            button.clicked += () => clickAction?.Invoke();
        }
        else
        {
            buttonElement.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse)
                    return;

                clickAction?.Invoke();
                evt.StopPropagation();
            });
        }
    }

    private void OnFileExplorerClicked()
    {
        _multiPaneController?.ToggleLeftPane();
        RefreshButtonVisuals();
    }

    private void OnSettingsClicked()
    {
        Debug.Log("settings click", this);
    }

    private void OnFileExplorerHoverChanged(bool isHovering)
    {
        _hoveringFileExplorer = isHovering;
        RefreshButtonVisuals();
    }

    private void OnSettingsHoverChanged(bool isHovering)
    {
        _hoveringSettings = isHovering;
        RefreshButtonVisuals();
    }

    private void OnLeftPaneVisibilityChanged(bool isOpen)
    {
        RefreshButtonVisuals();
    }

    private void RefreshButtonVisuals()
    {
        Color baseColor = _theme != null ? _theme.backgroundBase : new Color(0.11f, 0.11f, 0.11f, 1f);
        Color activeColor = _theme != null ? _theme.backgroundActive : new Color(0.24f, 0.24f, 0.24f, 1f);
        Color border = _theme != null ? _theme.border : new Color(0.3f, 0.3f, 0.3f, 1f);
        Color text = _theme != null ? _theme.text : Color.white;

        bool fileExplorerActive = _multiPaneController != null && _multiPaneController.IsLeftPaneOpen;

        StyleButtonElement(_fileExplorerButton, fileExplorerActive || _hoveringFileExplorer, baseColor, activeColor, border, text);
        StyleButtonElement(_settingsButton, _hoveringSettings, baseColor, activeColor, border, text);
    }

    private void StyleButtonElement(
        VisualElement buttonElement,
        bool active,
        Color baseColor,
        Color activeColor,
        Color border,
        Color text)
    {
        if (buttonElement == null)
            return;

        buttonElement.style.borderLeftColor = border;
        buttonElement.style.borderRightColor = border;
        buttonElement.style.borderTopColor = border;
        buttonElement.style.borderBottomColor = border;
        buttonElement.style.borderLeftWidth = 1f;
        buttonElement.style.borderRightWidth = 1f;
        buttonElement.style.borderTopWidth = 1f;
        buttonElement.style.borderBottomWidth = 1f;
        buttonElement.style.color = text;

        Label label = buttonElement.Q<Label>();
        if (label != null)
            label.style.color = text;
    }
}