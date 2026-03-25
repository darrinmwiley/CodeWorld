using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FileHierarchyComponent : WindowComponent
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset _hierarchyAsset; // The main FileHierarchy.uxml
    [SerializeField] private VisualTreeAsset _rowTemplate;    // The individual FileRow.uxml

    private TreeView _treeView;

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        if (_hierarchyAsset == null)
        {
            Debug.LogError($"[FileHierarchy] Main Hierarchy Asset is missing on {gameObject.name}");
            return;
        }

        // 1. Instantiate the main UI asset and add it to the container
        VisualElement mainView = _hierarchyAsset.Instantiate();
        mainView.style.flexGrow = 1; // Ensure it fills the window slot
        container.Add(mainView);

        // 2. Find the TreeView inside the UI we just created
        _treeView = mainView.Q<TreeView>("FileTree");

        if (_treeView == null)
        {
            Debug.LogError("TreeView 'FileTree' not found inside the Hierarchy Asset.");
            return;
        }

        // 3. Setup the Row logic (using the row template)
        _treeView.makeItem = () => _rowTemplate.Instantiate();
        
        _treeView.bindItem = (VisualElement element, int index) =>
        {
            var data = _treeView.GetItemDataForIndex<VirtualFileNode>(index);
            element.Q<Label>("FileName").text = data.Name;
        };

        // 4. Load Data
        _treeView.SetRootItems(GetMockData());

        // 5. Initialize sub-components (if any)
        InitializeSubComponents(mainView, root);
    }

    private List<TreeViewItemData<VirtualFileNode>> GetMockData()
    {
        var rootItems = new List<TreeViewItemData<VirtualFileNode>>();
        
        var srcChildren = new List<TreeViewItemData<VirtualFileNode>>
        {
            new TreeViewItemData<VirtualFileNode>(2, new VirtualFileNode("Player.cs", false)),
            new TreeViewItemData<VirtualFileNode>(3, new VirtualFileNode("Utils.cs", false))
        };

        rootItems.Add(new TreeViewItemData<VirtualFileNode>(1, new VirtualFileNode("Assets", true), srcChildren));
        rootItems.Add(new TreeViewItemData<VirtualFileNode>(4, new VirtualFileNode("Config.yaml", false)));

        return rootItems;
    }
}

public class VirtualFileNode
{
    public string Name;
    public bool IsDirectory;
    public VirtualFileNode(string name, bool isDirectory) { Name = name; IsDirectory = isDirectory; }
}