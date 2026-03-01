using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleScreenFocuser : MonoBehaviour
{

    public LookListener lookListener;
    public ConsoleController consoleController;
    public FirstPersonMovement firstPersonMovement;

    public bool lookingAtScreen;
    public bool focused;

    // Start is called before the first frame update
    void Start()
    {
        lookListener.AddLookHandler(OnLook);
        lookListener.AddLookAwayHandler(OnLookAway);
    }

    public void OnLook(){
        lookingAtScreen = true;
    }

    public void OnLookAway(){
        lookingAtScreen = false;
    }

    public void OnClick(){
        if(lookingAtScreen)
        {
            focused = true;
            firstPersonMovement.movementLocked = true;
            consoleController.isFocused = true;
        }
    }
    
    //on escape, unlock movement

    // Update is called once per frame
    void Update()
    {
        // Check for mouse click.
        if (Input.GetMouseButtonDown(0)) // Change to the appropriate mouse button (0 for left-click).
        {
            // Call OnClick method when the mouse is clicked.
            OnClick();
        }

        // Check for escape key to unlock movement.
        if (focused && Input.GetKeyDown(KeyCode.Escape))
        {
            focused = false;
            firstPersonMovement.movementLocked = false;
            consoleController.isFocused = false;
        }
    }
}
