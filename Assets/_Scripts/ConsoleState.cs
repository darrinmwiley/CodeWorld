using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleState
{
    public readonly int verticalScroll;
    public readonly int cursorRow;
    public readonly int cursorCol;
    public readonly int visibleCursorCol;
    public readonly bool isHighlighting;
    public readonly Vector2Int dragStart;
    public readonly Vector2Int dragCurrent;

    private ConsoleState(int verticalScroll, int cursorRow, int cursorCol, int visibleCursorCol, bool isHighlighting, Vector2Int dragStart, Vector2Int dragCurrent)
    {
        this.verticalScroll = verticalScroll;
        this.cursorRow = cursorRow;
        this.cursorCol = cursorCol;
        this.visibleCursorCol = visibleCursorCol;
        this.isHighlighting = isHighlighting;
        this.dragStart = dragStart;
        this.dragCurrent = dragCurrent;
    }

    public class Builder{

        public int verticalScroll = 0;
        public int cursorRow = 0;
        public int cursorCol = 0;
        public int visibleCursorCol = 0;
        public bool isHighlighting = false;
        public Vector2Int dragStart = new Vector2Int(0,0);
        public Vector2Int dragCurrent = new Vector2Int(0,0);

        public Builder setVerticalScroll(int verticalScroll)
        {
            this.verticalScroll = verticalScroll;
            return this;
        }

        public Builder setCursorRow(int cursorRow)
        {
            this.cursorRow = cursorRow;
            return this;
        }

        public Builder setCursorCol(int cursorCol)
        {
            this.cursorCol = cursorCol;
            return this;
        }

        public Builder setVisibleCursorCol(int visibleCursorCol)
        {
            this.visibleCursorCol = visibleCursorCol;
            return this;
        }

        public Builder setIsHighlighting(bool isHighlighting)
        {
            this.isHighlighting = isHighlighting;
            return this;
        }

        public Builder setDragStart(Vector2Int dragStart)
        {
            this.dragStart = dragStart;
            return this;
        }

        public Builder setDragCurrent(Vector2Int dragCurrent)
        {
            this.dragCurrent = dragCurrent;
            return this;
        }

        public ConsoleState build()
        {
            return new ConsoleState(verticalScroll, cursorRow, cursorCol, visibleCursorCol, isHighlighting, dragStart, dragCurrent);
        }

    }
}
