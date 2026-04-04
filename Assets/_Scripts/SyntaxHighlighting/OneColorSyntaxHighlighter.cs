using UnityEngine;

public class OneColorSyntaxHighlighter : SyntaxHighlighter
{
    public Color highlightColor = new Color(0.502f, 0.800f, 1.000f);

    public override void Highlight(ConsoleRenderer renderer)
    {
        var state = renderer.stateManager;
        if (state == null) return;

        int linesCount = state.lines.Count;
        int vScroll = state.verticalScroll;
        int viewportHeight = state.viewportHeight;
        int padding = state.GetLineCountPadding();
        int hScroll = state.horizontalScroll;
        int viewportWidth = state.viewportWidth;

        // Iterate only through visible rows for efficiency
        for (int i = 0; i < viewportHeight; i++)
        {
            int absoluteRow = i + vScroll;
            if (absoluteRow >= linesCount) break;

            string lineContent = state.GetLineContent(absoluteRow);
            int lineLength = lineContent.Length;

            // Iterate only through visible columns for efficiency
            // viewportCol = (absoluteCol - hScroll) + padding
            // absoluteCol = viewportCol - padding + hScroll
            
            for (int viewportCol = padding; viewportCol < viewportWidth; viewportCol++)
            {
                int absoluteCol = viewportCol - padding + hScroll;
                
                if (absoluteCol >= 0 && absoluteCol < lineLength)
                {
                    renderer.SetCellTextColor(i, viewportCol, highlightColor);
                }
            }
        }
    }
}
