using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A generic interface representing the top-level window shell.
/// Guarantees that any deeply nested component can access the root element for dragging,
/// can trigger focus, and can update the window's minimum size constraints.
/// </summary>
public interface IBaseWindow
{
    VisualElement RootElement { get; }
    void FocusWindow();
    void UpdateRootConstraints(Vector2 minSize);
}