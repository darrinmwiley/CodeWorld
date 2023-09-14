using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
/* transaction for key presses without special handlers */
public class InsertTransaction : Transaction
{
    ConsoleState preState;
    string[] insertion;

    public InsertTransaction(ConsoleState preState, string insertion)
    {
        this.preState = preState;
        this.insertion = insertion.Split("\n");
    }

    public void Apply(ConsoleController console)
    {
        console.SetState(preState);
        console.InsertLines(insertion);
    }

    public void Revert(ConsoleController console)
    {
        console.DeleteRegion(preState.cursorRow + preState.verticalScroll, preState.visibleCursorCol, preState.cursorRow + preState.verticalScroll + insertion.Length - 1, preState.visibleCursorCol + insertion[insertion.Length - 1].Length - 1);
        console.SetState(preState);
    }

    public bool IsMutation()
    {
        return true;
    }

    public override string ToString()
    {
        return "Insert "+String.Join('\n',insertion);
    }
}
