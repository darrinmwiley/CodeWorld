using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkingCursorController : MonoBehaviour
{

    float cursorBlinkSeconds = 1;
    float lastCursorBlink;
    bool cursorBlinkOn = true;

    public void Start()
    {
        lastCursorBlink = Time.time;
    }

    public void UpdateColor(Color color) 
    {
        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();

        if(renderer == null)
        {
            renderer = gameObject.AddComponent<MeshRenderer>();
        }

        renderer.material.SetColor("_color", color);
    }

    public void Update()
    {
        if(Time.time - lastCursorBlink >= cursorBlinkSeconds)
        {
            lastCursorBlink = Time.time;
            cursorBlinkOn = !cursorBlinkOn;
            UpdateColor(cursorBlinkOn ? Color.white : Color.black);
        }
    }

    
}
