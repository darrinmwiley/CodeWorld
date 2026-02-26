using UnityEngine;
using UnityEngine.UIElements;

public class DraggableWindow : MonoBehaviour
{
    private VisualElement _window;
    private bool _isOpen = false;

    public Texture2D diag1, diag2, vert, horiz; // Assign in inspector
    public Vector2 hotSpot = new Vector2(16, 16);

    public RenderTexture cameraRenderTexture;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _window = root.Q<VisualElement>("OuterBorder");
        var dragHandle = root.Q<VisualElement>("DragHandle");

        if (_window != null)
        {
            _window.style.display = DisplayStyle.None;
            _window.RegisterCallback<GeometryChangedEvent>(OnInitialLayout);

            if (dragHandle != null)
            {
                dragHandle.pickingMode = PickingMode.Position;
                dragHandle.BringToFront();
                dragHandle.AddManipulator(new UIDraggableManipulator(_window));
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
                
                // This ensures the camera feed fills the 'Content' area
                content.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
                
                // Crucial: Set this to Ignore if you want to click handles 'under' the image, 
                // or Position if the camera feed itself needs to be clickable.
                content.pickingMode = PickingMode.Ignore; 
            }
        }
    }

    private void SetupHandle(string name, Texture2D icon, ResizeDirection direction)
    {
        var handle = _window.Q<VisualElement>(name);
        if (handle == null) return;
        handle.pickingMode = PickingMode.Position;
        handle.BringToFront();
        handle.AddManipulator(new UIResizableManipulator(_window, direction));
        
        handle.RegisterCallback<PointerEnterEvent>(e => UnityEngine.Cursor.SetCursor(icon, hotSpot, CursorMode.ForceSoftware));
        handle.RegisterCallback<PointerLeaveEvent>(e => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
    }

    // Centering logic
    private void OnInitialLayout(GeometryChangedEvent evt)
    {
        _window.style.width = evt.newRect.width;
        _window.style.height = evt.newRect.height;
        var parent = _window.parent.layout;
        _window.style.left = (parent.width - evt.newRect.width) / 2;
        _window.style.top = (parent.height - evt.newRect.height) / 2;
        _window.UnregisterCallback<GeometryChangedEvent>(OnInitialLayout);
    }

    public void ShowWindow() { _isOpen = true; GameState.IsInUI = true; UnityEngine.Cursor.visible = true; _window.style.display = DisplayStyle.Flex; }
    public void HideWindow() { _isOpen = false; GameState.IsInUI = false; _window.style.display = DisplayStyle.None; }
    void Update() { if (Input.GetKeyDown(KeyCode.Escape)) { if (_isOpen) HideWindow(); else ShowWindow(); } }
}public enum ResizeDirection { 
    Left, Right, Top, Bottom, 
    TopLeft, TopRight, BottomLeft, BottomRight 
}