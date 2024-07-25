using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineOnLook : MonoBehaviour
{
    public LookListener lookListener;
    public Outline outline;

    // Start is called before the first frame update
    void Start()
    {
        lookListener.AddLookHandler(OnLook);
        lookListener.AddLookAwayHandler(OnLookAway);
    }

    void OnLook()
    {
        outline.enabled = true;
    }

    void OnLookAway(){
        outline.enabled = false;
    }
}
