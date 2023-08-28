using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIActivationListener : MonoBehaviour
{
    private bool isEscapeKeyPressed = false;
    private bool uiActive = false;

    void Update()
    {
        // Check for a new Escape key press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isEscapeKeyPressed)
            {
                SetUiActive(!uiActive);
                isEscapeKeyPressed = true;
            }
        }
        else
        {
            // Reset the flag when the Escape key is released
            isEscapeKeyPressed = false;
        }
    }

    public void SetUiActive(bool active){
        this.uiActive = active;
        transform.Find("UI").gameObject.SetActive(active);
        if(active)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }else{
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
