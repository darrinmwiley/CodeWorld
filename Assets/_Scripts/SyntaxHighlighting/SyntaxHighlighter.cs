using UnityEngine;
using Antlr4.Runtime;

public class SyntaxHighlighter : MonoBehaviour
{
    public virtual void Highlight(ConsoleRenderer renderer) {}

    protected void PaintToken(ConsoleRenderer renderer, IToken token, Color color)
    {
        // 1. Grab the state manager from the renderer
        var state = renderer.stateManager;
        
        // 2. Get token text and split to handle multi-line tokens
        string[] tokenLines = token.Text.Replace("\r", "").Split('\n');
        
        int absoluteStartRow = token.Line - 1; // ANTLR line (1-indexed)
        int absoluteStartCol = token.Column;   // ANTLR column (0-indexed)

        // Console viewport parameters pulled from State Manager
        int padding = state.GetLineCountPadding();
        int vScroll = state.verticalScroll;
        int hScroll = state.horizontalScroll;

        for (int i = 0; i < tokenLines.Length; i++)
        {
            int absoluteRow = absoluteStartRow + i;
            int absoluteColStart = (i == 0) ? absoluteStartCol : 0;

            for (int j = 0; j < tokenLines[i].Length; j++)
            {
                int absoluteCol = absoluteColStart + j;

                // 3. DEFENSIVE CHECKS
                
                // Check if this character is within the current vertical scroll view
                int viewportRow = absoluteRow - vScroll;
                if (viewportRow < 0 || viewportRow >= state.viewportHeight) continue;

                // Check if this character is within the current horizontal scroll view
                int viewportCol = (absoluteCol - hScroll) + padding;
                if (viewportCol < padding || viewportCol >= state.viewportWidth) continue;

                // Check if the data actually exists in the lines list
                if (state.lines.Count > absoluteRow && state.lines[absoluteRow].Length > absoluteCol)
                {
                    // 4. Paint the character via the Renderer
                    renderer.SetCellTextColor(viewportRow, viewportCol, color);
                }
            }
        }
    }
}