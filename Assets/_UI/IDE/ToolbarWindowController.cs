using UnityEngine;
using UnityEngine.UIElements;

public class ToolbarWindowController : MonoBehaviour
{
    [Header("Layout Assets")]
    [Tooltip("The UXML for the toolbar frame itself.")]
    [SerializeField] private VisualTreeAsset _toolbarLayoutAsset; 

    [Tooltip("The UXML for the dynamic content inside the toolbar.")]
    [SerializeField] private VisualTreeAsset _innerContentAsset; 

    private VisualElement _toolbarRoot; 
    private VisualElement _insideSpace; 

    /// <summary>
    /// Called by the WindowContainerController to inject this UI into the shell.
    /// </summary>
    public void InitializeInParent(VisualElement parentSlot)
    {
        if (_toolbarLayoutAsset == null || parentSlot == null) return;

        parentSlot.Clear();

        // Instantiate the Toolbar layout into the parent's slot
        _toolbarRoot = _toolbarLayoutAsset.Instantiate();
        _toolbarRoot.style.flexGrow = 1;
        parentSlot.Add(_toolbarRoot);

        // Find the container for the 3rd level (dynamic content)
        _insideSpace = _toolbarRoot.Q<VisualElement>("InsideSpace");

        if (_insideSpace != null)
        {
            InitializeButtons(_toolbarRoot);
            InitializeDynamicContent();
        }
    }

    private void InitializeDynamicContent()
    {
        if (_insideSpace == null || _innerContentAsset == null) return;

        _insideSpace.Clear();
        
        VisualElement content = _innerContentAsset.Instantiate();
        content.style.flexGrow = 1;
        content.style.width = Length.Percent(100);
        content.style.height = Length.Percent(100);
        
        _insideSpace.Add(content);
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