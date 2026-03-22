using UnityEngine.UIElements;

/// <summary>
/// A generic interface representing the top-level window shell.
/// Guarantees that any deeply nested component can access the root element for dragging,
/// and can trigger the window to focus.
/// </summary>
public interface IBaseWindow
{
    VisualElement RootElement { get; }
    void FocusWindow();
}