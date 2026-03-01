using UnityEngine;
using Antlr4.Runtime;

public class SyntaxHighlighter : MonoBehaviour
{
    public virtual void Highlight(ConsoleController console) {}

    protected void PaintToken(ConsoleController console, IToken token, Color color)
    {
        // 1. Get token text and split to handle multi-line tokens
        string[] tokenLines = token.Text.Replace("\r", "").Split('\n');
        
        int absoluteStartRow = token.Line - 1; // ANTLR line (1-indexed)
        int absoluteStartCol = token.Column;   // ANTLR column (0-indexed)

        // Console viewport parameters
        int padding = console.GetLineCountPadding();
        int vScroll = console.verticalScroll;
        int hScroll = console.horizontalScroll;

        for (int i = 0; i < tokenLines.Length; i++)
        {
            int absoluteRow = absoluteStartRow + i;
            int absoluteColStart = (i == 0) ? absoluteStartCol : 0;

            for (int j = 0; j < tokenLines[i].Length; j++)
            {
                int absoluteCol = absoluteColStart + j;

                // 2. DEFENSIVE CHECKS (Mirroring UpdateLines logic)
                
                // Check if this character is within the current vertical scroll view
                int viewportRow = absoluteRow - vScroll;
                if (viewportRow < 0 || viewportRow >= console.viewportHeight) continue;

                // Check if this character is within the current horizontal scroll view
                // We add padding because the text starts after the line numbers
                int viewportCol = (absoluteCol - hScroll) + padding;
                if (viewportCol < padding || viewportCol >= console.viewportWidth) continue;

                // Check if the data actually exists in the lines list (Defensive check for safety)
                if (console.lines.Count > absoluteRow && console.lines[absoluteRow].Length > absoluteCol)
                {
                    // 3. Paint the character
                    console.SetCellTextColor(viewportRow, viewportCol, color);
                }
            }
        }
    }
}