using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
/* transaction for key presses without special handlers */
public class DeleteTransaction : Transaction
{
    ConsoleState preState;
    string[] deletion;
    ConsoleState postState;
    public bool isBackspace;

    public DeleteTransaction(ConsoleState preState, bool isBackspace)
    {
        this.preState = preState;
        this.isBackspace = isBackspace;
    }

    public void Apply(ConsoleController console)
    {
        console.SetState(preState);
        string deleted = console.CaptureDeletion(isBackspace);
        this.deletion = deleted.Split("\n");
        this.postState = console.GetState();
        Debug.Log(this);
    }

    public void Revert(ConsoleController console)
    {
        Debug.Log("reverting: "+this);
        console.SetState(postState);
        console.InsertLines(this.deletion);
    }
        
    public bool IsMutation()
    {
        return true;
    }

    public override string ToString()
    {
        return "Delete "+String.Join('\n',deletion);
    }
}
