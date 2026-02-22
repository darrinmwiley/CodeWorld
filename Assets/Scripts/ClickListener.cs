using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickListener : MonoBehaviour
{
    public LookListener lookListener;
    //public MouseListener mouseListener;

    List<ClickAction> clickHandlers = new List<ClickAction>(){};

    public delegate void ClickAction();

    public bool lookedAt;
    // Start is called before the first frame update
    void Start()
    {
        lookListener.AddLookHandler(OnLook);
        lookListener.AddLookAwayHandler(OnLookAway);
    }

    public void OnLook()
    {
        lookedAt = true;
    }

    public void OnLookAway()
    {
        lookedAt = false;
    }

    public void OnMouseUp()
    {
        if(lookedAt){
            foreach(ClickAction action in clickHandlers)
            {
                action();
            }
        }
    }

    public void AddClickHandler(ClickAction action)
    {
        clickHandlers.Add(action);
    }

    public void RemoveClickHandler(ClickAction action)
    {
        if (clickHandlers.Contains(action))
        {
            clickHandlers.Remove(action);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
