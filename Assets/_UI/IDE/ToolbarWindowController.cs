using UnityEngine;
using UnityEngine.UIElements;

public class ToolbarWindowController : WindowComponent
{
    [SerializeField] private VisualTreeAsset _toolbarLayoutAsset;

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        if (_toolbarLayoutAsset == null || container == null) return;

        container.Clear();
        var toolbarRoot = _toolbarLayoutAsset.Instantiate();
        toolbarRoot.style.flexGrow = 1;
        container.Add(toolbarRoot);

        var handle = toolbarRoot.Q<VisualElement>("Handle");
        if (handle != null && root != null)
        {
            handle.AddManipulator(new UIDraggableManipulator(root.RootElement, root.FocusWindow));
        }

        InitializeSubComponents(toolbarRoot, root);
    }

    public override Vector2 GetMinimumSize()
    {
        // 35px is the standard height for the toolbar handle/buttons
        Vector2 min = new Vector2(150, 35); 

        foreach (var map in _subComponents)
        {
            if (map.controller == null) continue;
            Vector2 childMin = map.controller.GetMinimumSize();
            
            min.x = Mathf.Max(min.x, childMin.x);
            // Height is additive because the MultiPane sits BELOW the toolbar
            min.y += childMin.y;
        }

        return min;
    }
}