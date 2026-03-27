using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ConsoleStateManager : MonoBehaviour
{
    [Serializable]
    public struct Line
    {
        public string content;
        public bool locked;

        public Line(string content, bool locked)
        {
            this.content = content ?? string.Empty;
            this.locked = locked;
        }

        public int Length => string.IsNullOrEmpty(content) ? 0 : content.Length;

        public char this[int index] => (content ?? string.Empty)[index];

        public override string ToString() => content ?? string.Empty;
    }

    public const string LockPrefix = "//LOCK";

    [Header("Console Settings")]
    public int spacesPerTab = 4;
    public bool showLineNumbers = true;
    public bool allowScrolling = true;
    public bool allowNewLines = true;
    public bool readOnly = false;
    [Min(0)] public int extraLeftPaddingColumns = 0;

    [Header("Current State")]
    public List<Line> lines = new List<Line>();
    public int viewportWidth = 80;
    public int viewportHeight = 24;

    public int cursorRow = 0;
    public int cursorCol = 0;
    public int visibleCursorCol = 0;

    public int verticalScroll;
    public int horizontalScroll;

    public bool isHighlighting;
    public Vector2Int dragStart;
    public Vector2Int dragCurrent;

    private string copyBuffer;

    private readonly List<Transaction> mutations = new List<Transaction>();
    private int transactionPointer = -1;
    private int documentVersion;

    public event Action OnStateChanged;

    public void Initialize()
    {
        extraLeftPaddingColumns = Mathf.Max(0, extraLeftPaddingColumns);
        NormalizeAndEnsureLines();
        ClampCursorState();
        NotifyStateChanged();
    }

    public void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }

    public int GetLeftGutterPadding()
    {
        return Mathf.Max(0, extraLeftPaddingColumns);
    }

    public int GetLineNumberPadding()
    {
        if (!showLineNumbers) return 0;
        return (lines.Count + "").Length + 2;
    }

    public int GetLineCountPadding()
    {
        return GetLeftGutterPadding() + GetLineNumberPadding();
    }

    public int GetLineLength(int row)
    {
        if (row < 0 || row >= lines.Count) return 0;
        return lines[row].Length;
    }

    public string GetLineContent(int row)
    {
        if (row < 0 || row >= lines.Count) return string.Empty;
        return lines[row].content ?? string.Empty;
    }

    public bool IsLineLocked(int row)
    {
        if (row < 0 || row >= lines.Count) return false;
        return lines[row].locked;
    }

    public void Save(string fileName)
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                for (int i = 0; i < lines.Count; i++)
                    writer.WriteLine(ToSerializedString(lines[i]));
            }
            Debug.Log("File saved successfully: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Error saving file: " + e.Message);
        }
    }

    public void Load(string relativeFilePath)
    {
        lines = new List<Line>();
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, relativeFilePath);
            if (File.Exists(filePath))
            {
                string[] rawLines = File.ReadAllLines(filePath);
                for (int i = 0; i < rawLines.Length; i++)
                    lines.Add(ParseSerializedLine(rawLines[i]));
            }
            else
            {
                Debug.LogWarning("File not found: " + filePath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error loading file: " + e.Message);
        }

        NormalizeAndEnsureLines();
        cursorRow = 0;
        cursorCol = 0;
        visibleCursorCol = 0;
        verticalScroll = 0;
        horizontalScroll = 0;
        ResetDragState();
        mutations.Clear();
        transactionPointer = -1;
        documentVersion = 0;
        NotifyStateChanged();
    }

    public ConsoleState GetState()
    {
        return new ConsoleState.Builder()
            .setCursorCol(cursorCol)
            .setCursorRow(cursorRow)
            .setVisibleCursorCol(visibleCursorCol)
            .setIsHighlighting(isHighlighting)
            .setDragStart(dragStart)
            .setDragCurrent(dragCurrent)
            .build();
    }

    public void SetState(ConsoleState consoleState)
    {
        cursorCol = consoleState.cursorCol;
        cursorRow = consoleState.cursorRow;
        visibleCursorCol = consoleState.visibleCursorCol;
        isHighlighting = consoleState.isHighlighting;
        dragStart = consoleState.dragStart;
        dragCurrent = consoleState.dragCurrent;
        ClampCursorState();
        NotifyStateChanged();
    }

    public void Undo()
    {
        if (transactionPointer < 0) return;
        Transaction previous = mutations[transactionPointer];
        previous.Revert(this);
        transactionPointer--;
        AdjustScrollToCursor();
        NotifyStateChanged();
    }

    public void Redo()
    {
        if (transactionPointer >= mutations.Count - 1) return;
        Transaction next = mutations[transactionPointer + 1];
        next.Apply(this);
        transactionPointer++;
        AdjustScrollToCursor();
        NotifyStateChanged();
    }

    public void ApplyTransaction(Transaction t)
    {
        if (t == null) return;

        AdjustScrollToCursor();

        if (!t.CanApply(this))
        {
            AdjustScrollToCursor();
            NotifyStateChanged();
            return;
        }

        int versionBefore = documentVersion;
        t.Apply(this);
        bool changed = documentVersion != versionBefore;

        if (changed && t.IsMutation())
        {
            if (transactionPointer < mutations.Count - 1)
                mutations.RemoveRange(transactionPointer + 1, mutations.Count - transactionPointer - 1);

            mutations.Add(t);
            transactionPointer++;
        }

        AdjustScrollToCursor();
        NotifyStateChanged();
    }

    public void AdjustScrollToCursor()
    {
        if (!allowScrolling)
        {
            verticalScroll = 0;
            horizontalScroll = 0;
            return;
        }

        if (verticalScroll + viewportHeight - 1 < cursorRow)
            verticalScroll = cursorRow - viewportHeight + 1;
        if (verticalScroll > cursorRow)
            verticalScroll = cursorRow;

        int padding = GetLineCountPadding();
        if (horizontalScroll + viewportWidth - 1 - padding < visibleCursorCol)
            horizontalScroll = visibleCursorCol - viewportWidth + 1 + padding;
        if (horizontalScroll > visibleCursorCol - 4)
            horizontalScroll = Mathf.Max(0, visibleCursorCol - 4);
    }

    public bool CanApplyInsertion(string[] insertedLines)
    {
        if (insertedLines == null || insertedLines.Length == 0)
            return false;

        if (TryGetOrderedSelection(out int r1, out int c1, out int r2, out int c2Exclusive))
        {
            if (!CanDeleteSelection(r1, c1, r2, c2Exclusive))
                return false;

            return !WouldSelectionDeleteLeaveLockedLineAtCursor(r1, c1, r2, c2Exclusive);
        }

        return !IsLineLocked(cursorRow);
    }

    public bool CanApplyNewline()
    {
        if (!allowNewLines)
            return false;

        if (TryGetOrderedSelection(out int r1, out int c1, out int r2, out int c2Exclusive))
        {
            if (!CanDeleteSelection(r1, c1, r2, c2Exclusive))
                return false;

            return !WouldSelectionDeleteLeaveLockedLineAtCursor(r1, c1, r2, c2Exclusive);
        }

        if (!IsLineLocked(cursorRow))
            return true;

        int lineLength = GetLineLength(cursorRow);
        return visibleCursorCol == 0 || visibleCursorCol == lineLength;
    }

    public bool CanApplyDeletion(bool isBackspace)
    {
        if (TryGetOrderedSelection(out int r1, out int c1, out int r2, out int c2Exclusive))
            return CanDeleteSelection(r1, c1, r2, c2Exclusive);

        if (isBackspace)
        {
            if (visibleCursorCol > 0)
                return !IsLineLocked(cursorRow);

            if (cursorRow <= 0)
                return false;

            bool previousLocked = IsLineLocked(cursorRow - 1);
            bool currentLocked = IsLineLocked(cursorRow);
            int previousLength = GetLineLength(cursorRow - 1);
            int currentLength = GetLineLength(cursorRow);

            if (currentLocked)
                return previousLength == 0 && !previousLocked;

            if (previousLocked)
                return currentLength == 0 && !currentLocked;

            return !currentLocked;
        }

        int currentLineLength = GetLineLength(cursorRow);
        if (visibleCursorCol < currentLineLength)
            return !IsLineLocked(cursorRow);

        if (cursorRow >= lines.Count - 1)
            return false;

        bool currentLineLocked = IsLineLocked(cursorRow);
        bool nextLineLocked = IsLineLocked(cursorRow + 1);
        int nextLength = GetLineLength(cursorRow + 1);

        if (currentLineLocked)
            return nextLength == 0;

        if (nextLineLocked)
            return nextLength == 0;

        return true;
    }

    public void NewLine()
    {
        if (isHighlighting) DeleteHighlight();

        string currentContent = GetLineContent(cursorRow);
        bool currentLocked = IsLineLocked(cursorRow);
        int lineLength = currentContent.Length;

        if (currentLocked)
        {
            if (visibleCursorCol == 0)
            {
                lines.Insert(cursorRow, new Line(string.Empty, false));
                cursorRow++;
                visibleCursorCol = 0;
                cursorCol = 0;
                MarkDocumentChanged();
                AdjustScrollToCursor();
                return;
            }

            if (visibleCursorCol == lineLength)
            {
                lines.Insert(cursorRow + 1, new Line(string.Empty, false));
                cursorRow++;
                visibleCursorCol = 0;
                cursorCol = 0;
                MarkDocumentChanged();
                AdjustScrollToCursor();
                return;
            }
        }

        string before = currentContent.Substring(0, visibleCursorCol);
        string after = currentContent.Substring(visibleCursorCol);

        lines[cursorRow] = new Line(before, false);
        lines.Insert(cursorRow + 1, new Line(after, false));

        cursorRow++;
        visibleCursorCol = 0;
        cursorCol = 0;
        MarkDocumentChanged();
        AdjustScrollToCursor();
    }

    public void RevertNewLine()
    {
        if (cursorRow <= 0 || cursorRow >= lines.Count)
            return;

        string previousContent = GetLineContent(cursorRow - 1);
        string currentContent = GetLineContent(cursorRow);
        bool previousLocked = IsLineLocked(cursorRow - 1);
        bool currentLocked = IsLineLocked(cursorRow);

        if (!previousLocked && previousContent.Length == 0 && currentLocked)
        {
            lines.RemoveAt(cursorRow - 1);
            cursorRow--;
            MarkDocumentChanged();
            return;
        }

        if (previousLocked && !currentLocked && currentContent.Length == 0)
        {
            lines.RemoveAt(cursorRow);
            cursorRow--;
            MarkDocumentChanged();
            return;
        }

        string merged = previousContent + currentContent;
        bool locked = previousLocked;
        lines[cursorRow - 1] = new Line(merged, locked);
        lines.RemoveAt(cursorRow);
        MarkDocumentChanged();
    }

    public void InsertLines(string[] strs)
    {
        if (strs == null || strs.Length == 0)
            return;

        if (isHighlighting) DeleteHighlight();

        for (int i = 0; i < strs.Length; i++)
        {
            OnKeysTyped(strs[i]);
            if (i != strs.Length - 1) NewLine();
        }
    }

    public void OnKeysTyped(string str)
    {
        if (string.IsNullOrEmpty(str))
            return;

        if (isHighlighting) DeleteHighlight();

        string line = GetLineContent(cursorRow);
        line = line.Insert(visibleCursorCol, str);
        lines[cursorRow] = new Line(line, IsLineLocked(cursorRow));
        visibleCursorCol += str.Length;
        cursorCol = visibleCursorCol;
        MarkDocumentChanged();
    }

    public void DeleteRegion(int r1, int c1, int r2, int c2)
    {
        ClampRegion(ref r1, ref c1, ref r2, ref c2);

        string startLine = GetLineContent(r1);
        string endLine = GetLineContent(r2);

        string prefix = startLine.Substring(0, Mathf.Clamp(c1, 0, startLine.Length));
        int suffixStart = Mathf.Clamp(c2 + 1, 0, endLine.Length);
        string suffix = endLine.Substring(suffixStart);
        string newContent = prefix + suffix;

        bool newLocked;
        if (r1 == r2)
        {
            newLocked = IsLineLocked(r1);
        }
        else
        {
            newLocked = (IsLineLocked(r1) && newContent == startLine) ||
                        (IsLineLocked(r2) && newContent == endLine);
        }

        lines[r1] = new Line(newContent, newLocked);

        if (r2 > r1)
            lines.RemoveRange(r1 + 1, r2 - r1);

        if (lines.Count == 0)
            lines.Add(new Line(string.Empty, false));

        MarkDocumentChanged();
    }

    public string CaptureDeletion(bool isBackspace = true)
    {
        string deleted = string.Empty;

        if (isHighlighting)
        {
            deleted = GetHighlightedText();
            DeleteHighlight();
        }
        else if (isBackspace)
        {
            if (visibleCursorCol != 0)
            {
                bool canBackspaceTab = CanBackspaceTab();
                do
                {
                    string line = GetLineContent(cursorRow);
                    deleted += line[visibleCursorCol - 1];
                    line = line.Substring(0, visibleCursorCol - 1) + line.Substring(visibleCursorCol);
                    lines[cursorRow] = new Line(line, IsLineLocked(cursorRow));
                    cursorCol = --visibleCursorCol;
                    MarkDocumentChanged();
                }
                while (canBackspaceTab && visibleCursorCol % spacesPerTab != 0);
            }
            else if (cursorRow != 0)
            {
                int newCursorCol = GetLineLength(cursorRow - 1);
                string previousContent = GetLineContent(cursorRow - 1);
                string currentContent = GetLineContent(cursorRow);
                string merged = previousContent + currentContent;
                bool locked = IsLineLocked(cursorRow - 1) ||
                              (previousContent.Length == 0 && IsLineLocked(cursorRow));
                lines[cursorRow - 1] = new Line(merged, locked);
                lines.RemoveAt(cursorRow);
                cursorRow--;
                visibleCursorCol = cursorCol = newCursorCol;
                deleted += '\n';
                MarkDocumentChanged();
            }
        }
        else
        {
            if (visibleCursorCol != GetLineLength(cursorRow))
            {
                string line = GetLineContent(cursorRow);
                deleted += line[visibleCursorCol];
                line = line.Remove(visibleCursorCol, 1);
                lines[cursorRow] = new Line(line, IsLineLocked(cursorRow));
                MarkDocumentChanged();
            }
            else if (cursorRow < lines.Count - 1)
            {
                deleted = "\n";
                string merged = GetLineContent(cursorRow) + GetLineContent(cursorRow + 1);
                bool locked = IsLineLocked(cursorRow);
                lines[cursorRow] = new Line(merged, locked);
                lines.RemoveAt(cursorRow + 1);
                MarkDocumentChanged();
            }
        }

        AdjustScrollToCursor();
        NotifyStateChanged();
        return deleted;
    }

    public void DeleteHighlight()
    {
        if (!TryGetOrderedSelection(out int r1, out int c1, out int r2, out int c2Exclusive))
            return;

        DeleteRegion(r1, c1, r2, c2Exclusive - 1);
        cursorRow = r1;
        cursorCol = c1;
        visibleCursorCol = c1;
        ResetDragState();
        NotifyStateChanged();
    }

    public void ResetDragState()
    {
        isHighlighting = false;
        dragStart = new Vector2Int(cursorRow, visibleCursorCol);
        dragCurrent = new Vector2Int(cursorRow, visibleCursorCol);
    }

    public string GetHighlightedText()
    {
        if (!TryGetOrderedSelection(out int r1, out int c1, out int r2, out int c2Exclusive))
            return string.Empty;

        return GetRegion(r1, c1, r2, c2Exclusive - 1);
    }

    public bool CanBackspaceTab()
    {
        if (visibleCursorCol == 0) return false;
        string line = GetLineContent(cursorRow);
        for (int i = 0; i < visibleCursorCol; i++)
            if (line[i] != ' ') return false;
        return true;
    }

    public void SetCopyBuffer(string text) => copyBuffer = text;
    public string GetCopyBuffer() => copyBuffer;

    private bool TryGetOrderedSelection(out int r1, out int c1, out int r2, out int c2Exclusive)
    {
        r1 = c1 = r2 = c2Exclusive = 0;

        if (!isHighlighting)
            return false;

        Vector2Int start = dragStart;
        Vector2Int end = dragCurrent;

        if (end.x < start.x || (end.x == start.x && end.y < start.y))
        {
            Vector2Int temp = start;
            start = end;
            end = temp;
        }

        r1 = Mathf.Clamp(start.x, 0, Mathf.Max(0, lines.Count - 1));
        r2 = Mathf.Clamp(end.x, 0, Mathf.Max(0, lines.Count - 1));
        c1 = Mathf.Clamp(start.y, 0, GetLineLength(r1));
        c2Exclusive = Mathf.Clamp(end.y, 0, GetLineLength(r2));

        return r1 != r2 || c1 != c2Exclusive;
    }

    private bool CanDeleteSelection(int r1, int c1, int r2, int c2Exclusive)
    {
        if (r1 > r2 || (r1 == r2 && c1 > c2Exclusive))
            return false;

        for (int row = r1; row <= r2; row++)
        {
            if (!IsLineLocked(row))
                continue;

            int start = row == r1 ? c1 : 0;
            int endExclusive = row == r2 ? c2Exclusive : GetLineLength(row);
            if (endExclusive > start)
                return false;
        }

        if (r1 != r2)
        {
            string prefix = GetLineContent(r1).Substring(0, Mathf.Clamp(c1, 0, GetLineLength(r1)));
            string suffix = GetLineContent(r2).Substring(Mathf.Clamp(c2Exclusive, 0, GetLineLength(r2)));

            if (IsLineLocked(r1))
            {
                bool lockedLinePreservedExactly = prefix == GetLineContent(r1) && suffix.Length == 0;
                if (!lockedLinePreservedExactly)
                    return false;
            }

            if (IsLineLocked(r2))
            {
                bool lockedLinePreservedExactly = prefix.Length == 0 && suffix == GetLineContent(r2);
                if (!lockedLinePreservedExactly)
                    return false;
            }
        }

        return true;
    }

    private bool WouldSelectionDeleteLeaveLockedLineAtCursor(int r1, int c1, int r2, int c2Exclusive)
    {
        if (r1 == r2)
            return IsLineLocked(r1);

        string prefix = GetLineContent(r1).Substring(0, Mathf.Clamp(c1, 0, GetLineLength(r1)));
        string suffix = GetLineContent(r2).Substring(Mathf.Clamp(c2Exclusive, 0, GetLineLength(r2)));
        string newContent = prefix + suffix;

        return (IsLineLocked(r1) && newContent == GetLineContent(r1)) ||
               (IsLineLocked(r2) && newContent == GetLineContent(r2));
    }

    private string GetRegion(int r1, int c1, int r2, int c2)
    {
        if (r1 == r2)
        {
            if (c2 < c1)
                return string.Empty;

            return GetLineContent(r1).Substring(c1, c2 - c1 + 1);
        }

        string region = GetLineContent(r1).Substring(c1);
        for (int i = r1 + 1; i < r2; i++)
            region += "\n" + GetLineContent(i);

        region += "\n" + GetLineContent(r2).Substring(0, Mathf.Max(0, c2 + 1));
        return region;
    }

    private void NormalizeAndEnsureLines()
    {
        if (lines == null)
            lines = new List<Line>();

        for (int i = 0; i < lines.Count; i++)
        {
            Line normalized = NormalizeLine(lines[i]);
            lines[i] = normalized;
        }

        if (lines.Count == 0)
            lines.Add(new Line(string.Empty, false));
    }

    private Line NormalizeLine(Line line)
    {
        string content = line.content ?? string.Empty;
        bool locked = line.locked;

        if (content.StartsWith(LockPrefix, StringComparison.Ordinal))
        {
            locked = true;
            content = content.Substring(LockPrefix.Length);
        }

        return new Line(content, locked);
    }

    private Line ParseSerializedLine(string raw)
    {
        if (raw == null)
            raw = string.Empty;
        if (raw.StartsWith(LockPrefix, StringComparison.Ordinal))
            return new Line(raw.Substring(LockPrefix.Length), true);

        return new Line(raw, false);
    }

    private string ToSerializedString(Line line)
    {
        string content = line.content ?? string.Empty;
        return line.locked ? LockPrefix + content : content;
    }

    private void ClampCursorState()
    {
        NormalizeAndEnsureLines();

        cursorRow = Mathf.Clamp(cursorRow, 0, Mathf.Max(0, lines.Count - 1));
        visibleCursorCol = Mathf.Clamp(visibleCursorCol, 0, GetLineLength(cursorRow));
        cursorCol = Mathf.Clamp(cursorCol, 0, GetLineLength(cursorRow));

        dragStart = new Vector2Int(
            Mathf.Clamp(dragStart.x, 0, Mathf.Max(0, lines.Count - 1)),
            Mathf.Clamp(dragStart.y, 0, GetLineLength(Mathf.Clamp(dragStart.x, 0, Mathf.Max(0, lines.Count - 1)))));

        dragCurrent = new Vector2Int(
            Mathf.Clamp(dragCurrent.x, 0, Mathf.Max(0, lines.Count - 1)),
            Mathf.Clamp(dragCurrent.y, 0, GetLineLength(Mathf.Clamp(dragCurrent.x, 0, Mathf.Max(0, lines.Count - 1)))));
    }

    private void ClampRegion(ref int r1, ref int c1, ref int r2, ref int c2)
    {
        r1 = Mathf.Clamp(r1, 0, Mathf.Max(0, lines.Count - 1));
        r2 = Mathf.Clamp(r2, 0, Mathf.Max(0, lines.Count - 1));

        if (r2 < r1 || (r2 == r1 && c2 < c1))
        {
            int tempR = r1;
            r1 = r2;
            r2 = tempR;

            int tempC = c1;
            c1 = c2;
            c2 = tempC;
        }

        c1 = Mathf.Clamp(c1, 0, GetLineLength(r1));
        c2 = Mathf.Clamp(c2, -1, GetLineLength(r2) - 1);
    }

    private void MarkDocumentChanged()
    {
        documentVersion++;
    }
}
