using UnityEngine;
using System.Collections.Generic;

public class ManualSyntaxHighlighter : SyntaxHighlighter
{
    // Vector3Int: x = Row, y = StartColumn, z = Length
    public Dictionary<Vector3Int, Color> colorMap = new Dictionary<Vector3Int, Color>();

    public override void Highlight(ConsoleController console)
    {
        foreach (var entry in colorMap)
        {
            Vector3Int pos = entry.Key;
            Color color = entry.Value;

            for (int i = 0; i < pos.z; i++)
            {
                PaintManual(console, pos.x, pos.y + i, color);
            }
        }
    }

    private void PaintManual(ConsoleController console, int row, int col, Color color)
    {
        int padding = console.GetLineCountPadding();
        int vScroll = console.verticalScroll;
        int hScroll = console.horizontalScroll;

        int viewportRow = row - vScroll;
        int viewportCol = (col - hScroll) + padding;

        // Defensive checks
        if (viewportRow >= 0 && viewportRow < console.viewportHeight &&
            viewportCol >= padding && viewportCol < console.viewportWidth &&
            row < console.lines.Count && col < console.lines[row].Length)
        {
            console.SetCellTextColor(viewportRow, viewportCol, color);
        }
    }

    public void Clear() => colorMap.Clear();
    
    public void AddRange(int row, int col, int length, Color color)
    {
        colorMap[new Vector3Int(row, col, length)] = color;
    }
}