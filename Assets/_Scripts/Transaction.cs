using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Transaction
{   
    void Apply(ConsoleController console);
    void Revert(ConsoleController console);
    bool IsMutation();
}
