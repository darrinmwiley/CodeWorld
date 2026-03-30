using System;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleFileSessionManager : MonoBehaviour
{
    [SerializeField] private bool _verboseLogging = false;

    private ConsoleLevelFileSet _currentFileSet;

    private readonly Dictionary<string, ConsoleLevelFileEntry> _entriesByPath =
        new Dictionary<string, ConsoleLevelFileEntry>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, List<ConsoleStateManager.Line>> _runtimeDocuments =
        new Dictionary<string, List<ConsoleStateManager.Line>>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<ConsoleStateManager, string> _pathsByStateManager =
        new Dictionary<ConsoleStateManager, string>();

    public string CurrentOpenFilePath { get; private set; }

    public void InitializeFromFileSet(ConsoleLevelFileSet fileSet)
    {
        if (fileSet == null)
            return;

        if (ReferenceEquals(_currentFileSet, fileSet) && _entriesByPath.Count > 0)
            return;

        _currentFileSet = fileSet;
        _entriesByPath.Clear();
        _runtimeDocuments.Clear();
        _pathsByStateManager.Clear();
        CurrentOpenFilePath = null;

        if (fileSet.files == null)
            return;

        for (int i = 0; i < fileSet.files.Count; i++)
        {
            ConsoleLevelFileEntry entry = fileSet.files[i];
            if (entry == null)
                continue;

            string normalizedPath = NormalizePath(entry.virtualPath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
                continue;

            _entriesByPath[normalizedPath] = entry;
            _runtimeDocuments[normalizedPath] = BuildLinesForEntry(entry);
        }

        Log($"Initialized session from file set '{fileSet.name}' with {_runtimeDocuments.Count} documents.");
    }

    public void BindStateManagerToPath(ConsoleStateManager stateManager, string path)
    {
        if (stateManager == null)
            return;

        string normalizedPath = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalizedPath))
            return;

        _pathsByStateManager[stateManager] = normalizedPath;
        CurrentOpenFilePath = normalizedPath;
        Log($"Bound state manager to '{normalizedPath}'.");
    }

    public bool TryGetBoundPath(ConsoleStateManager stateManager, out string path)
    {
        path = null;

        if (stateManager == null)
            return false;

        return _pathsByStateManager.TryGetValue(stateManager, out path);
    }

    public void SaveActiveDocument(ConsoleStateManager sourceStateManager)
    {
        SaveDocumentForStateManager(sourceStateManager);
    }

    public void SaveDocument(string path, ConsoleStateManager sourceStateManager)
    {
        if (sourceStateManager == null)
            return;

        string normalizedPath = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalizedPath))
            return;

        _runtimeDocuments[normalizedPath] = sourceStateManager.ExportLines();
        _pathsByStateManager[sourceStateManager] = normalizedPath;
        CurrentOpenFilePath = normalizedPath;
        Log($"Saved runtime document '{normalizedPath}'.");
    }

    public void SaveDocumentForStateManager(ConsoleStateManager sourceStateManager)
    {
        if (sourceStateManager == null)
            return;

        if (!_pathsByStateManager.TryGetValue(sourceStateManager, out string boundPath))
            return;

        SaveDocument(boundPath, sourceStateManager);
    }

    public bool LoadDocumentInto(string path, ConsoleStateManager targetStateManager)
    {
        if (targetStateManager == null)
            return false;

        string normalizedPath = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalizedPath))
            return false;

        if (!_runtimeDocuments.TryGetValue(normalizedPath, out List<ConsoleStateManager.Line> documentLines))
        {
            if (_entriesByPath.TryGetValue(normalizedPath, out ConsoleLevelFileEntry entry))
            {
                documentLines = BuildLinesForEntry(entry);
                _runtimeDocuments[normalizedPath] = documentLines;
            }
            else
            {
                documentLines = new List<ConsoleStateManager.Line>
                {
                    new ConsoleStateManager.Line(string.Empty, false)
                };
            }
        }

        _pathsByStateManager[targetStateManager] = normalizedPath;
        CurrentOpenFilePath = normalizedPath;
        targetStateManager.LoadLines(CloneLines(documentLines));

        Log($"Loaded runtime document '{normalizedPath}'.");
        return true;
    }

    public bool RestoreActiveDocument(ConsoleStateManager targetStateManager)
    {
        if (targetStateManager == null)
            return false;

        if (_pathsByStateManager.TryGetValue(targetStateManager, out string boundPath))
            return LoadDocumentInto(boundPath, targetStateManager);

        if (string.IsNullOrWhiteSpace(CurrentOpenFilePath))
            return false;

        return LoadDocumentInto(CurrentOpenFilePath, targetStateManager);
    }

    private List<ConsoleStateManager.Line> BuildLinesForEntry(ConsoleLevelFileEntry entry)
    {
        string text = entry != null && entry.fileAsset != null ? entry.fileAsset.text : string.Empty;
        text = NormalizeNewlines(text);

        string[] rawLines = text.Split('\n');
        if (rawLines == null || rawLines.Length == 0)
            rawLines = new[] { string.Empty };

        HashSet<int> lockedLineIndices = ParseLockedLineSpec(entry != null ? entry.lockedLines : null, rawLines.Length);

        List<ConsoleStateManager.Line> result = new List<ConsoleStateManager.Line>(rawLines.Length);
        for (int i = 0; i < rawLines.Length; i++)
            result.Add(new ConsoleStateManager.Line(rawLines[i], lockedLineIndices.Contains(i)));

        return result;
    }

    private HashSet<int> ParseLockedLineSpec(string spec, int lineCount)
    {
        HashSet<int> locked = new HashSet<int>();

        if (lineCount <= 0 || string.IsNullOrWhiteSpace(spec))
            return locked;

        string[] tokens = spec.Split(',');
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim();
            if (string.IsNullOrEmpty(token))
                continue;

            if (string.Equals(token, "all", StringComparison.OrdinalIgnoreCase))
            {
                for (int line = 0; line < lineCount; line++)
                    locked.Add(line);

                continue;
            }

            string[] rangeParts = token.Split('-');
            if (rangeParts.Length == 2)
            {
                if (!int.TryParse(rangeParts[0].Trim(), out int start1Based) ||
                    !int.TryParse(rangeParts[1].Trim(), out int end1Based))
                {
                    Debug.LogWarning($"[ConsoleFileSessionManager] Invalid lock token '{token}' on {gameObject.name}", this);
                    continue;
                }

                if (end1Based < start1Based)
                {
                    int temp = start1Based;
                    start1Based = end1Based;
                    end1Based = temp;
                }

                start1Based = Mathf.Clamp(start1Based, 1, lineCount);
                end1Based = Mathf.Clamp(end1Based, 1, lineCount);

                for (int line = start1Based; line <= end1Based; line++)
                    locked.Add(line - 1);

                continue;
            }

            if (int.TryParse(token, out int single1Based))
            {
                if (single1Based >= 1 && single1Based <= lineCount)
                {
                    locked.Add(single1Based - 1);
                }
                else
                {
                    Debug.LogWarning($"[ConsoleFileSessionManager] Lock line '{single1Based}' is out of range 1-{lineCount} on {gameObject.name}", this);
                }

                continue;
            }

            Debug.LogWarning($"[ConsoleFileSessionManager] Invalid lock token '{token}' on {gameObject.name}. Supported forms: all, N, N-M", this);
        }

        return locked;
    }

    private List<ConsoleStateManager.Line> CloneLines(List<ConsoleStateManager.Line> source)
    {
        List<ConsoleStateManager.Line> clone = new List<ConsoleStateManager.Line>();
        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            ConsoleStateManager.Line line = source[i];
            clone.Add(new ConsoleStateManager.Line(line.content, line.locked));
        }

        return clone;
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        path = path.Replace('\\', '/').Trim();

        while (path.StartsWith("/"))
            path = path.Substring(1);

        while (path.EndsWith("/"))
            path = path.Substring(0, path.Length - 1);

        return path;
    }

    private string NormalizeNewlines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private void Log(string message)
    {
        if (_verboseLogging)
            Debug.Log($"[ConsoleFileSessionManager] {message}", this);
    }
}
