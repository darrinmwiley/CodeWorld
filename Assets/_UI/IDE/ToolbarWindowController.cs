using UnityEngine;
using UnityEngine.UIElements;

public class ToolbarWindowController : WindowComponent
{
    [SerializeField] private VisualTreeAsset _toolbarLayoutAsset;

    [Header("Toolbar Element Names")]
    [SerializeField] private string _handleElementName = "Handle";
    [SerializeField] private string _closeButtonElementName = "Button1";
    [SerializeField] private string _maximizeButtonElementName = "Button2";

    [Header("Debug")]
    [SerializeField] private bool _verboseLogging = false;

    private IBaseWindow _rootWindow;
    private VisualElement _toolbarRoot;
    private VisualElement _handle;
    private VisualElement _closeButton;
    private VisualElement _maximizeButton;

    private bool _isFullscreen;
    private Vector2 _restorePosition;
    private Vector2 _restoreSize;

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        if (_toolbarLayoutAsset == null || container == null)
            return;

        _rootWindow = root;

        container.Clear();
        _toolbarRoot = _toolbarLayoutAsset.Instantiate();
        _toolbarRoot.style.flexGrow = 1f;
        _toolbarRoot.style.minWidth = 0f;
        _toolbarRoot.style.minHeight = 0f;
        container.Add(_toolbarRoot);

        _handle = _toolbarRoot.Q<VisualElement>(_handleElementName);
        if (_handle != null && root != null)
        {
            _handle.AddManipulator(new UIDraggableManipulator(root.RootElement, root.FocusWindow));
        }

        _closeButton = FindClickable(_toolbarRoot, _closeButtonElementName);
        _maximizeButton = FindClickable(_toolbarRoot, _maximizeButtonElementName);

        HookClickable(_closeButton, OnCloseClicked);
        HookClickable(_maximizeButton, OnMaximizeClicked);

        if (_verboseLogging)
        {
            Debug.Log(
                $"[ToolbarWindowController] Init complete. Handle found={_handle != null}, Close found={_closeButton != null}, Maximize found={_maximizeButton != null}",
                this);
        }

        InitializeSubComponents(_toolbarRoot, root);
    }

    public override Vector2 GetMinimumSize()
    {
        Vector2 min = new Vector2(150f, 35f);

        foreach (var map in _subComponents)
        {
            if (map.controller == null)
                continue;

            Vector2 childMin = map.controller.GetMinimumSize();
            min.x = Mathf.Max(min.x, childMin.x);
            min.y += childMin.y;
        }

        return min;
    }

    private VisualElement FindClickable(VisualElement root, string elementName)
    {
        if (root == null || string.IsNullOrWhiteSpace(elementName))
            return null;

        Button button = root.Q<Button>(elementName);
        if (button != null)
            return button;

        return root.Q<VisualElement>(elementName);
    }

    private void HookClickable(VisualElement element, System.Action action)
    {
        if (element == null || action == null)
            return;

        element.focusable = false;
        element.tabIndex = -1;

        if (element is Button button)
        {
            button.clicked += () => action.Invoke();
        }
        else
        {
            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse)
                    return;

                action.Invoke();
                evt.StopPropagation();
            });
        }
    }

    private void OnCloseClicked()
    {
        if (_verboseLogging)
            Debug.Log("[ToolbarWindowController] Close button clicked. Notifying FocusManager.", this);

        // Instead of manually hiding styles, we notify the FocusManager.
        // We cast the root window to IFocusable (which it should implement).
        if (_rootWindow is IFocusable focusableWindow)
        {
            FocusManager.Instance.RemoveFocus(focusableWindow);
        }
        else
        {
            // Fallback: If for some reason it's not IFocusable, hide it manually
            if (_rootWindow?.RootElement != null)
                _rootWindow.RootElement.style.display = DisplayStyle.None;
            
            Debug.LogWarning("[ToolbarWindowController] Root window does not implement IFocusable. FocusManager state might be desynced.");
        }
    }

    private void OnMaximizeClicked()
    {
        if (_rootWindow?.RootElement == null)
            return;

        VisualElement window = _rootWindow.RootElement;
        VisualElement parent = window.parent;
        if (parent == null)
            return;

        if (!_isFullscreen)
        {
            _restorePosition = new Vector2(window.resolvedStyle.left, window.resolvedStyle.top);
            _restoreSize = new Vector2(window.resolvedStyle.width, window.resolvedStyle.height);

            float parentWidth = parent.resolvedStyle.width;
            float parentHeight = parent.resolvedStyle.height;

            if (parentWidth <= 0f || parentHeight <= 0f) return;

            window.style.left = 0f;
            window.style.top = 0f;
            window.style.width = parentWidth;
            window.style.height = parentHeight;

            _isFullscreen = true;
        }
        else
        {
            window.style.left = _restorePosition.x;
            window.style.top = _restorePosition.y;
            window.style.width = _restoreSize.x;
            window.style.height = _restoreSize.y;

            _isFullscreen = false;
        }

        _rootWindow.UpdateRootConstraints(GetMinimumSize());
        _rootWindow.FocusWindow();
    }
}