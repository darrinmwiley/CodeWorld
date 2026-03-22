using UnityEngine;
using UnityEngine.UIElements;

public class ToolbarWindowController : WindowComponent
{
    [Header("Layout Assets")]
    [Tooltip("The UXML for the toolbar frame itself.")]
    [SerializeField] private VisualTreeAsset _toolbarLayoutAsset; 

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        if (_toolbarLayoutAsset == null || container == null) return;

        container.Clear();

        // Instantiate the Toolbar layout into the parent's slot
        var toolbarRoot = _toolbarLayoutAsset.Instantiate();
        toolbarRoot.style.flexGrow = 1;
        container.Add(toolbarRoot);

        InitializeButtons(toolbarRoot);

        // Setup the drag handle to move the top-level window via the generic interface
        var handle = toolbarRoot.Q<VisualElement>("Handle");
        if (handle != null && root != null)
        {
            handle.AddManipulator(new UIDraggableManipulator(root.RootElement, root.FocusWindow));
        }

        // Recursively initialize sub-components (like the MultiPane into InsideSpace)
        InitializeSubComponents(toolbarRoot, root);
    }

    private void InitializeButtons(VisualElement root)
    {
        SetupButton(root.Q<Button>("Button1"), "Btn1");
        SetupButton(root.Q<Button>("Button2"), "Btn2");
        SetupButton(root.Q<Button>("Button3"), "Btn3");
    }

    private void SetupButton(Button btn, string name) 
    {
        if (btn == null) return;
        btn.clicked += () => Debug.Log($"{name} Clicked");
    }
}