
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Injects a read-only output console into a UI slot (e.g. the Bottom slot of MultiPane).
/// Call AppendLine() from CodeExecutor to display compile/runtime output.
/// </summary>
public class OutputConsoleController : WindowComponent
{
    [Header("Console")]
    [SerializeField] private ConsoleWindowController _consolePrefab;

    [Header("Behavior")]
    public bool clearOnNewRun = true;

    private ConsoleWindowController _console;
    private ConsoleStateManager _state;

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        if (_consolePrefab == null)
        {
            Debug.LogError("[OutputConsole] No console prefab assigned.", this);
            return;
        }

        // Instantiate the console into this GameObject's hierarchy
        GameObject instance = Instantiate(_consolePrefab.gameObject, transform);
        instance.name = "OutputConsoleInstance";

        _console = instance.GetComponent<ConsoleWindowController>();
        if (_console == null)
        {
            Debug.LogError("[OutputConsole] Prefab does not have ConsoleWindowController.", this);
            return;
        }

        // Make the console read-only
        _state = _console.stateManager;
        if (_state != null)
        {
            _state.readOnly = true;
            _state.allowNewLines = false;
            _state.showLineNumbers = false;
        }

        _console.Initialize(container, root);
        InitializeSubComponents(container, root);
    }

    public override Vector2 GetMinimumSize() => new Vector2(100f, 80f);

    /// <summary>Clears all output lines.</summary>
    public void Clear()
    {
        if (_state == null) return;
        _state.lines.Clear();
        _state.lines.Add(new ConsoleStateManager.Line(string.Empty, false));
        _state.cursorRow = 0;
        _state.cursorCol = 0;
        _state.visibleCursorCol = 0;
        _state.verticalScroll = 0;
        _state.NotifyStateChanged();
    }

    /// <summary>Appends a line to the output console. All lines are locked (read-only).</summary>
    public void AppendLine(string text, bool isError = false)
    {
        Debug.Log("trying to append line: "+text);
        if (_state == null) return;

        // Ensure we don't start with a stale empty line at the top
        if (_state.lines.Count == 1 && string.IsNullOrEmpty(_state.lines[0].content) && !_state.lines[0].locked)
        {
            _state.lines[0] = new ConsoleStateManager.Line(text ?? string.Empty, true);
        }
        else
        {
            _state.lines.Add(new ConsoleStateManager.Line(text ?? string.Empty, true));
        }

        // Scroll to the bottom
        _state.cursorRow = _state.lines.Count - 1;
        _state.verticalScroll = Mathf.Max(0, _state.lines.Count - _state.viewportHeight);
        _state.NotifyStateChanged();
    }
}
