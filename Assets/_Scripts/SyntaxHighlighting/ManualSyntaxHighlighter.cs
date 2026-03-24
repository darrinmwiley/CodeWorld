using UnityEngine;
using System.Collections.Generic;

public class ManualSyntaxHighlighter : SyntaxHighlighter
{
    // Vector3Int: x = Row, y = StartColumn, z = Length
    public Dictionary<Vector3Int, Color> colorMap = new Dictionary<Vector3Int, Color>();

    public override void Highlight(ConsoleRenderer renderer)
    {
        foreach (var entry in colorMap)
        {
            Vector3Int pos = entry.Key;
            Color color = entry.Value;

            for (int i = 0; i < pos.z; i++)
            {
                PaintManual(renderer, pos.x, pos.y + i, color);
            }
        }
    }

    private void PaintManual(ConsoleRenderer renderer, int row, int col, Color color)
    {
        var state = renderer.stateManager;
        
        int padding = state.GetLineCountPadding();
        int vScroll = state.verticalScroll;
        int hScroll = state.horizontalScroll;

        int viewportRow = row - vScroll;
        int viewportCol = (col - hScroll) + padding;

        // Defensive checks across the State Manager constraints
        if (viewportRow >= 0 && viewportRow < state.viewportHeight &&
            viewportCol >= padding && viewportCol < state.viewportWidth &&
            row < state.lines.Count && col < state.lines[row].Length)
        {
            // Apply color to the Renderer
            renderer.SetCellTextColor(viewportRow, viewportCol, color);
        }
    }

    public void Clear() => colorMap.Clear();
    
    public void AddRange(int row, int col, int length, Color color)
    {
        colorMap[new Vector3Int(row, col, length)] = color;
    }
}