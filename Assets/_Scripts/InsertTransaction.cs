using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
/* transaction for key presses without special handlers */
public class InsertTransaction : Transaction
{
    ConsoleState preDeletionState;
    ConsoleState preState;
    string[] insertion;
    string[] deletion;

    public InsertTransaction(string insertion)
    {
        this.insertion = insertion.Split("\n");
    }

    public void Apply(ConsoleController console)
    {
        if(preState == null)
        {
            if(console.isHighlighting){
                preDeletionState = console.GetState();
                deletion = console.CaptureDeletion().Split("\n");
            }
            if(preState == null)
                preState = console.GetState();
        }else if(preDeletionState != null){
            console.SetState(preDeletionState);
            console.CaptureDeletion();
        }
        console.SetState(preState);
        console.InsertLines(insertion);
    }

    public void Revert(ConsoleController console)
    {
        console.SetState(preState);
        if(insertion.Length == 1)
            console.DeleteRegion(preState.cursorRow, preState.visibleCursorCol, preState.cursorRow + insertion.Length - 1, preState.visibleCursorCol + insertion[0].Length - 1);
        else
            console.DeleteRegion(preState.cursorRow, preState.visibleCursorCol, preState.cursorRow + insertion.Length - 1, insertion[insertion.Length - 1].Length - 1);
        if(deletion != null){
            console.InsertLines(deletion);
            console.SetState(preDeletionState);
        }
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
