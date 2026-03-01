using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class LookListener : MonoBehaviour
{
    List<LookAction> onLookHandlers = new List<LookAction>(){};
    List<LookAction> onLookAwayHandlers = new List<LookAction>(){};

    public delegate void LookAction();

    public void OnLook(){
        foreach(LookAction lookAction in onLookHandlers){
            lookAction();
        }
    }

    public void OnLookAway(){
        foreach(LookAction lookAction in onLookAwayHandlers){
            lookAction();
        }
    }

    public void AddLookHandler(LookAction lookAction){
        onLookHandlers.Add(lookAction);
    }

    public void AddLookAwayHandler(LookAction lookAction){
        onLookAwayHandlers.Add(lookAction);
    }
}