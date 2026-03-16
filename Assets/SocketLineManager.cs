using UnityEngine;

public class SocketLineManager : MonoBehaviour
{
    public LineController lineController;
    public ItemSocket socket;

    [Header("Line Colors")]
    public Color trueColor = Color.green;
    public Color falseColor = Color.red;
    public Color defaultColor = Color.white;

    void Awake()
    {
        if (socket == null) socket = GetComponent<ItemSocket>();

        if (socket != null)
        {
            socket.OnItemSocketed += OnItemPluggedIn;
            socket.OnItemUnsocketed += OnItemUnplugged; // Added unsocket handler
        }
    }

    void OnDestroy()
    {
        if (socket != null)
        {
            socket.OnItemSocketed -= OnItemPluggedIn;
            socket.OnItemUnsocketed -= OnItemUnplugged;
        }
    }

    private void OnItemPluggedIn(GameObject item)
    {
        Debug.Log("item plugged in");
        ItemValue itemVal = item.GetComponent<ItemValue>();
        Color targetColor = defaultColor;
        if(itemVal.value == "true")
        {
            targetColor = trueColor;
        }
        else if(itemVal.value == "false")
        {
            targetColor = falseColor;
        }

        lineController.UpdateLineColors(targetColor);
        lineController.RestartTransition();
    }

    private void OnItemUnplugged(GameObject item)
    {
        // Instead of restarting the transition (which animates),
        // we just clear the transition mesh and reset the base color.
        lineController.StopAndClearTransition();
        lineController.UpdateLineColors(defaultColor);
    }
}