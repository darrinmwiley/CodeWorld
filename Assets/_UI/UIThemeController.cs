using UnityEngine;

[ExecuteAlways]
public class UIThemeController : MonoBehaviour
{
    [SerializeField] private UITheme _theme;
    [SerializeField] private bool _applyOnEnable = true;
    [SerializeField] private bool _applyContinuouslyInEditMode = false;
    [SerializeField] private WindowContainerController[] _windowContainers;
    [SerializeField] private MultiPaneWindowController[] _multiPaneControllers;
    [SerializeField] private LeftTabPaneController[] _leftTabPaneControllers;
    [SerializeField] private TabbedConsoleWindowController[] _tabbedConsoleControllers;
    [SerializeField] private FileHierarchyComponent[] _fileHierarchyControllers;
    [SerializeField] private ConsoleRenderer[] _consoleRenderers;

    private void OnEnable()
    {
        if (_applyOnEnable)
            ApplyTheme();
    }

    private void Update()
    {
        if (!Application.isPlaying && _applyContinuouslyInEditMode)
            ApplyTheme();
    }

    [ContextMenu("Apply Theme")]
    public void ApplyTheme()
    {
        if (_theme == null)
            return;

        CacheMissingReferences();

        if (_windowContainers != null)
            foreach (var controller in _windowContainers)
                controller?.ApplyTheme(_theme);

        if (_multiPaneControllers != null)
            foreach (var controller in _multiPaneControllers)
                controller?.ApplyTheme(_theme);

        if (_leftTabPaneControllers != null)
            foreach (var controller in _leftTabPaneControllers)
                controller?.ApplyTheme(_theme);

        if (_tabbedConsoleControllers != null)
            foreach (var controller in _tabbedConsoleControllers)
                controller?.ApplyTheme(_theme);

        if (_fileHierarchyControllers != null)
            foreach (var controller in _fileHierarchyControllers)
                controller?.ApplyTheme(_theme);

        if (_consoleRenderers != null)
            foreach (var renderer in _consoleRenderers)
                renderer?.ApplyTheme(_theme);
    }

    private void CacheMissingReferences()
    {
        if (_windowContainers == null || _windowContainers.Length == 0)
            _windowContainers = FindObjectsByType<WindowContainerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (_multiPaneControllers == null || _multiPaneControllers.Length == 0)
            _multiPaneControllers = FindObjectsByType<MultiPaneWindowController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (_leftTabPaneControllers == null || _leftTabPaneControllers.Length == 0)
            _leftTabPaneControllers = FindObjectsByType<LeftTabPaneController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (_tabbedConsoleControllers == null || _tabbedConsoleControllers.Length == 0)
            _tabbedConsoleControllers = FindObjectsByType<TabbedConsoleWindowController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (_fileHierarchyControllers == null || _fileHierarchyControllers.Length == 0)
            _fileHierarchyControllers = FindObjectsByType<FileHierarchyComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (_consoleRenderers == null || _consoleRenderers.Length == 0)
            _consoleRenderers = FindObjectsByType<ConsoleRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }
}
