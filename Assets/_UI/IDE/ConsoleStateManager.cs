using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ConsoleStateManager : MonoBehaviour
{
    [Header("Console Settings")]
    public int spacesPerTab = 4;
    public bool showLineNumbers = true;
    public bool allowScrolling = true;
    public bool allowNewLines = true;
    public bool readOnly = false;
    [Min(0)] public int extraLeftPaddingColumns = 0;

    [Header("Current State")]
    public List<string> lines = new List<string>();
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

    private List<Transaction> mutations = new List<Transaction>();
    private int transactionPointer = -1;

    // Events to notify the Renderer
    public event Action OnStateChanged;

    public void Initialize()
    {
        extraLeftPaddingColumns = Mathf.Max(0, extraLeftPaddingColumns);
        if (lines.Count == 0) lines.Add("");
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
        return lines[row]?.Length ?? 0;
    }

    public void Save(string fileName)
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (string line in lines) writer.WriteLine(line);
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
        lines = new List<string>();
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, relativeFilePath);
            if (File.Exists(filePath)) lines.AddRange(File.ReadAllLines(filePath));
            else Debug.LogWarning("File not found: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Error loading file: " + e.Message);
        }
        if (lines.Count == 0) lines.Add("");
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
        AdjustScrollToCursor();
        if (t.IsMutation())
        {
            if (transactionPointer < mutations.Count - 1)
                mutations.RemoveRange(transactionPointer + 1, mutations.Count - transactionPointer - 1);
            mutations.Add(t);
            transactionPointer++;
        }
        t.Apply(this);
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

    public void NewLine()
    {
        if (isHighlighting) DeleteHighlight();
        lines.Insert(cursorRow + 1, lines[cursorRow].Substring(visibleCursorCol));
        lines[cursorRow] = lines[cursorRow].Substring(0, visibleCursorCol);
        cursorRow++;
        visibleCursorCol = cursorCol = 0;
        AdjustScrollToCursor();
    }

    public void RevertNewLine()
    {
        lines[cursorRow - 1] += lines[cursorRow];
        lines.RemoveAt(cursorRow);
    }

    public void InsertLines(string[] strs)
    {
        if (isHighlighting) DeleteHighlight();
        for (int i = 0; i < strs.Length; i++)
        {
            OnKeysTyped(strs[i]);
            if (i != strs.Length - 1) NewLine();
        }
    }

    public void OnKeysTyped(string str)
    {
        if (isHighlighting) DeleteHighlight();
        lines[cursorRow] = lines[cursorRow].Insert(visibleCursorCol, str);
        visibleCursorCol += str.Length;
        cursorCol = visibleCursorCol;
    }

    public void DeleteRegion(int r1, int c1, int r2, int c2)
    {
        lines[r1] = lines[r1].Substring(0, c1) + lines[r2].Substring(c2 + 1);
        lines.RemoveRange(r1 + 1, r2 - r1);
    }

    public string CaptureDeletion(bool isBackspace = true)
    {
        string deleted = "";
        if (isHighlighting)
        {
            deleted = GetHighlightedText();
            DeleteHighlight();
        }
        else if (isBackspace)
        {
            if (cursorCol != 0)
            {
                bool canBackspaceTab = CanBackspaceTab();
                do
                {
                    string line = lines[cursorRow];
                    deleted += line[visibleCursorCol - 1];
                    lines[cursorRow] = line.Substring(0, visibleCursorCol - 1) + line.Substring(visibleCursorCol);
                    cursorCol = --visibleCursorCol;
                } while (canBackspaceTab && visibleCursorCol % spacesPerTab != 0);
            }
            else if (cursorRow != 0)
            {
                int newCursorCol = lines[cursorRow - 1].Length;
                lines[cursorRow - 1] += lines[cursorRow];
                lines.RemoveAt(cursorRow);
                cursorRow--;
                visibleCursorCol = cursorCol = newCursorCol;
                deleted += '\n';
            }
        }
        else
        {
            if (visibleCursorCol != lines[cursorRow + verticalScroll].Length)
            {
                deleted += lines[cursorRow][visibleCursorCol];
                lines[cursorRow] = lines[cursorRow].Remove(visibleCursorCol, 1);
            }
            else if (cursorRow < lines.Count - 1)
            {
                deleted = "\n";
                lines[cursorRow] += lines[cursorRow + 1];
                lines.RemoveAt(cursorRow + 1);
            }
        }
        AdjustScrollToCursor();
        NotifyStateChanged();
        return deleted;
    }

    public void DeleteHighlight()
    {
        if (!isHighlighting) return;
        int r1 = dragStart.x, c1 = dragStart.y, r2 = dragCurrent.x, c2 = dragCurrent.y;
        if (dragCurrent.x < dragStart.x || (dragCurrent.x == dragStart.x && dragCurrent.y < dragStart.y))
        {
            r1 = dragCurrent.x; c1 = dragCurrent.y; r2 = dragStart.x; c2 = dragStart.y;
        }
        DeleteRegion(r1, c1, r2, c2 - 1);
        cursorRow = r1;
        cursorCol = visibleCursorCol = c1;
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
        int r1 = dragStart.x, c1 = dragStart.y, r2 = dragCurrent.x, c2 = dragCurrent.y;
        if (dragCurrent.x < dragStart.x || (dragCurrent.x == dragStart.x && dragCurrent.y < dragStart.y))
        {
            r1 = dragCurrent.x; c1 = dragCurrent.y; r2 = dragStart.x; c2 = dragStart.y;
        }
        return GetRegion(r1, c1, r2, c2 - 1);
    }

    private string GetRegion(int r1, int c1, int r2, int c2)
    {
        if (r1 == r2) return lines[r1].Substring(c1, c2 - c1 + 1);
        string region = lines[r1].Substring(c1);
        for (int i = r1 + 1; i < r2; i++) region += "\n" + lines[i];
        region += "\n" + lines[r2].Substring(0, c2 + 1);
        return region;
    }

    public bool CanBackspaceTab()
    {
        if (visibleCursorCol == 0) return false;
        for (int i = 0; i < visibleCursorCol; i++)
            if (lines[cursorRow][i] != ' ') return false;
        return true;
    }

    public void SetCopyBuffer(string text) => copyBuffer = text;
    public string GetCopyBuffer() => copyBuffer;
}