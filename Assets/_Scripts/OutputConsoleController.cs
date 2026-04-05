
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// Injects a read-only output console into a UI slot (e.g. the Bottom slot of MultiPane).
/// Call AppendLine() from CodeExecutor to display compile/runtime output.
/// </summary>
public class OutputConsoleController : WindowComponent
{
    private struct OutputEntry
    {
        public string text;
        public ConsoleStateManager.LineStyle style;

        public OutputEntry(string text, ConsoleStateManager.LineStyle style)
        {
            this.text = text ?? string.Empty;
            this.style = style;
        }
    }

    [Header("Console")]
    [SerializeField] private ConsoleWindowController _consolePrefab;

    [Header("Behavior")]
    public bool clearOnNewRun = true;

    private ConsoleWindowController _console;
    private ConsoleStateManager _state;
    private readonly List<OutputEntry> _rawOutputLines = new List<OutputEntry>();
    private int _lastWrapWidth = -1;
    private bool _isRebuilding;

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
            _state.OnStateChanged -= HandleStateChanged;
            _state.OnStateChanged += HandleStateChanged;
        }

        _console.Initialize(container, root);
        InitializeSubComponents(container, root);
    }

    public override Vector2 GetMinimumSize() => new Vector2(100f, 80f);

    private void OnDestroy()
    {
        if (_state != null)
            _state.OnStateChanged -= HandleStateChanged;
    }

    /// <summary>Clears all output lines.</summary>
    public void Clear()
    {
        if (_state == null)
            return;

        _rawOutputLines.Clear();
        RebuildWrappedOutput(scrollToBottom: false);
    }

    /// <summary>Appends a line to the output console. All lines are locked (read-only).</summary>
    public void AppendLine(string text, bool isError = false, bool isSuccess = false)
    {
        if (_state == null)
            return;

        ConsoleStateManager.LineStyle style = ConsoleStateManager.LineStyle.Normal;
        if (isError) style = ConsoleStateManager.LineStyle.Error;
        else if (isSuccess) style = ConsoleStateManager.LineStyle.Success;

        string normalized = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        string[] segments = normalized.Split('\n');

        if (segments.Length == 0)
        {
            _rawOutputLines.Add(new OutputEntry(string.Empty, style));
        }
        else
        {
            for (int i = 0; i < segments.Length; i++)
                _rawOutputLines.Add(new OutputEntry(segments[i] ?? string.Empty, style));
        }

        RebuildWrappedOutput(scrollToBottom: true);
    }

    private void HandleStateChanged()
    {
        if (_state == null || _isRebuilding)
            return;

        int currentWrapWidth = GetWrapContentWidth();
        if (currentWrapWidth != _lastWrapWidth)
            RebuildWrappedOutput(scrollToBottom: true);
    }

    private int GetWrapContentWidth()
    {
        if (_state == null)
            return 1;

        int available = _state.viewportWidth - _state.GetLineCountPadding() - 2;
        return Mathf.Max(1, available);
    }

    private void RebuildWrappedOutput(bool scrollToBottom)
    {
        if (_state == null)
            return;

        int wrapWidth = GetWrapContentWidth();
        _lastWrapWidth = wrapWidth;

        _isRebuilding = true;
        try
        {
            _state.lines.Clear();

            if (_rawOutputLines.Count == 0)
            {
                _state.lines.Add(new ConsoleStateManager.Line(string.Empty, true));
            }
            else
            {
                for (int i = 0; i < _rawOutputLines.Count; i++)
                    AppendWrappedLineToState(_rawOutputLines[i], wrapWidth);
            }

            if (_state.lines.Count == 0)
                _state.lines.Add(new ConsoleStateManager.Line(string.Empty, true));

            _state.cursorRow = Mathf.Max(0, _state.lines.Count - 1);
            _state.visibleCursorCol = Mathf.Clamp(_state.visibleCursorCol, 0, _state.GetLineLength(_state.cursorRow));
            _state.cursorCol = _state.visibleCursorCol;
            _state.horizontalScroll = 0;

            if (scrollToBottom)
                _state.verticalScroll = Mathf.Max(0, _state.lines.Count - _state.viewportHeight);
            else
                _state.verticalScroll = 0;

            _state.NotifyStateChanged();
        }
        finally
        {
            _isRebuilding = false;
        }
    }

    private void AppendWrappedLineToState(OutputEntry entry, int wrapWidth)
    {
        string safe = entry.text ?? string.Empty;
        if (safe.Length == 0)
        {
            _state.lines.Add(new ConsoleStateManager.Line(string.Empty, true, entry.style));
            return;
        }

        int index = 0;
        while (index < safe.Length)
        {
            int take = Mathf.Min(wrapWidth, safe.Length - index);
            string segment = safe.Substring(index, take);
            _state.lines.Add(new ConsoleStateManager.Line(segment, true, entry.style));
            index += take;
        }
    }
}
