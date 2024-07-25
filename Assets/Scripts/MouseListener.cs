using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MouseListener : MonoBehaviour
{
    List<MouseAction> mouseDownHandlers = new List<MouseAction>(){};
    List<MouseAction> mouseUpHandlers = new List<MouseAction>(){};

    public delegate void MouseAction();

    public void OnMouseDown(){
        foreach(MouseAction action in mouseDownHandlers){
            action();
        }
    }

    public void OnMouseUp(){
        foreach(MouseAction action in mouseUpHandlers){
            action();
        }
    }

    public void AddMouseDownHandler(MouseAction action){
        mouseDownHandlers.Add(action);
    }

    public void AddMouseUpHandler(MouseAction action){
        mouseUpHandlers.Add(action);
    }

    public void Update(){
        if (Input.GetMouseButtonDown(0))
        {
            OnMouseDown();
        }

        if (Input.GetMouseButtonUp(0))
        {
            OnMouseUp();
        }
    }
}