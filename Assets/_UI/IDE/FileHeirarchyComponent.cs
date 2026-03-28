using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FileHierarchyComponent : WindowComponent
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset _hierarchyAsset;
    [SerializeField] private VisualTreeAsset _rowTemplate;

    [Header("Bindings")]
    [SerializeField] private TabbedConsoleWindowController _tabbedContent;
    [SerializeField] private string _treeViewElementName = "FileTree";
    [SerializeField] private string _fileNameLabelName = "FileName";

    [Header("Debug")]
    [SerializeField] private bool _verboseLogging = false;

    private TreeView _treeView;

    public event Action<VirtualFileNode> FileClicked;

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        if (_hierarchyAsset == null)
        {
            Debug.LogError($"[FileHierarchy] Main hierarchy asset is missing on {gameObject.name}", this);
            return;
        }

        container.style.flexGrow = 1;
        container.style.minHeight = 0;
        container.style.minWidth = 0;

        VisualElement mainView = _hierarchyAsset.Instantiate();
        mainView.style.flexGrow = 1;
        mainView.style.minHeight = 0;
        mainView.style.minWidth = 0;
        container.Add(mainView);

        _treeView = mainView.Q<TreeView>(_treeViewElementName);
        if (_treeView == null)
        {
            Debug.LogError($"[FileHierarchy] TreeView '{_treeViewElementName}' not found inside the hierarchy asset.", this);
            return;
        }

        _treeView.makeItem = MakeRow;
        _treeView.bindItem = BindRow;
        _treeView.selectionType = SelectionType.Single;
        _treeView.SetRootItems(GetMockData());
        _treeView.Rebuild();

        Log($"Initialize complete. TreeView found: name='{_treeView.name}'");

        InitializeSubComponents(mainView, root);
    }

    private VisualElement MakeRow()
    {
        if (_rowTemplate != null)
            return _rowTemplate.Instantiate();

        VisualElement fallback = new VisualElement();
        fallback.style.flexDirection = FlexDirection.Row;
        fallback.style.flexGrow = 1;
        fallback.Add(new Label { name = _fileNameLabelName });
        return fallback;
    }

    private void BindRow(VisualElement element, int index)
    {
        VirtualFileNode data = _treeView.GetItemDataForIndex<VirtualFileNode>(index);
        Label fileName = element.Q<Label>(_fileNameLabelName);
        if (fileName != null)
            fileName.text = data.Name;

        element.userData = data;
        element.UnregisterCallback<PointerDownEvent>(OnRowPointerDown);
        element.RegisterCallback<PointerDownEvent>(OnRowPointerDown);
    }

    private void OnRowPointerDown(PointerDownEvent evt)
    {
        if (evt.button != (int)MouseButton.LeftMouse)
            return;

        if (!(evt.currentTarget is VisualElement rowElement))
            return;

        if (!(rowElement.userData is VirtualFileNode node))
            return;

        if (node.IsDirectory)
            return;

        HandleNodeClicked(node);
        evt.StopPropagation();
    }

    private void HandleNodeClicked(VirtualFileNode node)
    {
        Log($"HandleNodeClicked: name='{node?.Name}', path='{node?.Path}', isDirectory={node?.IsDirectory}");

        if (node == null || node.IsDirectory)
            return;

        FileClicked?.Invoke(node);
        _tabbedContent?.OpenFile(node);
    }

    private List<TreeViewItemData<VirtualFileNode>> GetMockData()
    {
        var rootItems = new List<TreeViewItemData<VirtualFileNode>>();

        var srcChildren = new List<TreeViewItemData<VirtualFileNode>>
        {
            new TreeViewItemData<VirtualFileNode>(2, new VirtualFileNode("Player.cs", false, "Assets/Player.cs")),
            new TreeViewItemData<VirtualFileNode>(3, new VirtualFileNode("Utils.cs", false, "Assets/Utils.cs"))
        };

        rootItems.Add(new TreeViewItemData<VirtualFileNode>(1, new VirtualFileNode("Assets", true, "Assets"), srcChildren));
        rootItems.Add(new TreeViewItemData<VirtualFileNode>(4, new VirtualFileNode("Config.yaml", false, "Config.yaml")));

        return rootItems;
    }

    private void Log(string message)
    {
        if (_verboseLogging)
            Debug.Log($"[FileHierarchy] {message}", this);
    }
}

[Serializable]
public class VirtualFileNode
{
    public string Name;
    public bool IsDirectory;
    public string Path;

    public VirtualFileNode(string name, bool isDirectory)
        : this(name, isDirectory, name)
    {
    }

    public VirtualFileNode(string name, bool isDirectory, string path)
    {
        Name = name;
        IsDirectory = isDirectory;
        Path = string.IsNullOrWhiteSpace(path) ? name : path;
    }
}
