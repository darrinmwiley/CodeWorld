using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Transaction
{   
    void Apply(ConsoleStateManager console);
    void Revert(ConsoleStateManager console);
    bool IsMutation();
}
