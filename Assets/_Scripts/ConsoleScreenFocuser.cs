using UnityEngine;

public class ConsoleScreenFocuser : MonoBehaviour, IFocusable
{
    public LookListener lookListener;
    public ConsoleController consoleController;
    private bool lookingAtScreen;
    private bool isFocused;

    void Start()
    {
        lookListener.AddLookHandler(() => lookingAtScreen = true);
        lookListener.AddLookAwayHandler(() => lookingAtScreen = false);
    }

    void Update()
    {
        // Click the screen in-world to focus it
        if (Input.GetMouseButtonDown(0) && lookingAtScreen && !isFocused)
        {
            FocusManager.Instance.PushFocus(this);
        }
    }

    public void OnFocus()
    {
        isFocused = true;
        if (consoleController != null) consoleController.isFocused = true;
    }

    public void OnDefocus()
    {
        isFocused = false;
        if (consoleController != null) consoleController.isFocused = false;
    }
}