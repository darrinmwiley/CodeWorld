using System; // <-- needed for Action
using UnityEngine;
using UnityEngine.UIElements;

public class DraggableWindow : MonoBehaviour
{
    private VisualElement _window;
    private bool _isOpen = false;

    public Texture2D diag1, diag2, vert, horiz; // Assign in inspector
    public Vector2 hotSpot = new Vector2(16, 16);

    public RenderTexture cameraRenderTexture;

    public ConsoleController console;

    void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;

        var root = uiDoc.rootVisualElement;
        if (root == null) return;

        _window = root.Q<VisualElement>("OuterBorder");
        var dragHandle = root.Q<VisualElement>("DragHandle");

        if (_window != null)
        {
            _window.style.display = DisplayStyle.None;
            _window.RegisterCallback<GeometryChangedEvent>(OnInitialLayout);

            // Find owning ConsoleController (same GO or parent)
            Action bringToFront = console != null ? console.BringToFront : null;

            if (dragHandle != null)
            {
                dragHandle.pickingMode = PickingMode.Position;
                dragHandle.BringToFront();

                // Requires UIDraggableManipulator ctor: (VisualElement windowToMove, Action bringToFront)
                dragHandle.AddManipulator(new UIDraggableManipulator(_window, bringToFront));
            }

            // Setup Resizers (Exact same pattern)
            SetupHandle("LeftResize", horiz, ResizeDirection.Left);
            SetupHandle("RightResize", horiz, ResizeDirection.Right);
            SetupHandle("TopResize", vert, ResizeDirection.Top);
            SetupHandle("BottomResize", vert, ResizeDirection.Bottom);
            SetupHandle("TopLeftResize", diag1, ResizeDirection.TopLeft);
            SetupHandle("TopRightResize", diag2, ResizeDirection.TopRight);
            SetupHandle("BottomLeftResize", diag2, ResizeDirection.BottomLeft);
            SetupHandle("BottomRightResize", diag1, ResizeDirection.BottomRight);

            var content = root.Q<VisualElement>("Content");
            if (content != null && cameraRenderTexture != null)
            {
                // StyleBackground accepts RenderTexture, Texture2D, or Sprite
                content.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(cameraRenderTexture));

                // Fill the 'Content' area
                content.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;

                // Ignore so clicks pass through to handles etc.
                content.pickingMode = PickingMode.Ignore;
            }
        }
    }

    private void SetupHandle(string name, Texture2D icon, ResizeDirection direction)
    {
        if (_window == null) return;

        var handle = _window.Q<VisualElement>(name);
        if (handle == null) return;

        handle.pickingMode = PickingMode.Position;
        handle.BringToFront();
        Action bringToFront = console != null ? console.BringToFront : null;
        handle.AddManipulator(new UIResizableManipulator(_window, direction, bringToFront));

        handle.RegisterCallback<PointerEnterEvent>(e => UnityEngine.Cursor.SetCursor(icon, hotSpot, CursorMode.ForceSoftware));
        handle.RegisterCallback<PointerLeaveEvent>(e => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
    }

    // Centering logic
    private void OnInitialLayout(GeometryChangedEvent evt)
    {
        if (_window == null) return;

        _window.style.width = evt.newRect.width;
        _window.style.height = evt.newRect.height;

        var parent = _window.parent.layout;
        _window.style.left = (parent.width - evt.newRect.width) / 2;
        _window.style.top = (parent.height - evt.newRect.height) / 2;

        _window.UnregisterCallback<GeometryChangedEvent>(OnInitialLayout);
    }

    public void ShowWindow()
    {
        _isOpen = true;
        GameState.IsInUI = true;
        UnityEngine.Cursor.visible = true;
        if (_window != null) _window.style.display = DisplayStyle.Flex;
    }

    public void HideWindow()
    {
        _isOpen = false;
        GameState.IsInUI = false;
        if (_window != null) _window.style.display = DisplayStyle.None;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isOpen) HideWindow();
            else ShowWindow();
        }
    }
}

public enum ResizeDirection
{
    Left, Right, Top, Bottom,
    TopLeft, TopRight, BottomLeft, BottomRight
}