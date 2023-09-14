using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
/* transaction for key presses without special handlers */
public class NewlineTransaction : Transaction
{
    ConsoleState preState;
    ConsoleState postState;

    public NewlineTransaction(ConsoleState preState)
    {
        this.preState = preState;
    }

    public void Apply(ConsoleController console)
    {
        console.SetState(preState);
        console.NewLine();
        postState = console.GetState();
    }

    public void Revert(ConsoleController console)
    {
        console.SetState(postState);
        console.RevertNewLine();
        console.SetState(preState);
    }

    public bool IsMutation()
    {
        return true;
    }

    public override string ToString()
    {
        return "newline";
    }
}
