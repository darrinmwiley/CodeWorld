using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

//legacy mouse listener, needs update with focus changing to mouse.
public class LegacyMouseListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public bool isMouseDown = false;
    public bool isMouseDragging = false;
    public Vector2 mouseDownPosition;
    public Vector2 currentMousePosition;

    List<LegacyMouseAction> mouseDownHandlers = new List<LegacyMouseAction>(){};
    List<LegacyMouseAction> mouseUpHandlers = new List<LegacyMouseAction>(){};
    List<LegacyMouseAction> mouseDragHandlers = new List<LegacyMouseAction>(){};

    public delegate void LegacyMouseAction();

    public void AddMouseDownHandler(LegacyMouseAction mouseAction)
    {
        mouseDownHandlers.Add(mouseAction);
    }

    public void AddMouseUpHandler(LegacyMouseAction mouseAction)
    {
        mouseUpHandlers.Add(mouseAction);
    }

    public void AddMouseDragHandler(LegacyMouseAction mouseAction)
    {
        mouseDragHandlers.Add(mouseAction);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("pointer down");
        isMouseDown = true;
        // Calculate proportions when the mouse button is pressed.
        CalculateProportions(eventData.position);
        mouseDownPosition = currentMousePosition;
        foreach(LegacyMouseAction mouseAction in mouseDownHandlers){
            mouseAction();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("pointer up");
        isMouseDown = false;
        isMouseDragging = false;
        CalculateProportions(eventData.position);
        foreach(LegacyMouseAction mouseAction in mouseUpHandlers){
            mouseAction();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isMouseDown)
        {
            isMouseDragging = true;
            CalculateProportions(eventData.position);
            foreach (LegacyMouseAction mouseAction in mouseDragHandlers)
            {
                mouseAction();
            }
        }
    }

    private void CalculateProportions(Vector2 screenPosition)
    {
        // Convert screen coordinates to canvas coordinates
        RectTransform canvasRect = GetComponent<RectTransform>();
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out canvasPosition);

        // Calculate proportions based on canvas size
        float proportionX = (canvasPosition.x - transform.localPosition.x + (GetComponent<RectTransform>().sizeDelta.x / 2)) / GetComponent<RectTransform>().sizeDelta.x;
        float proportionY = (canvasPosition.y - transform.localPosition.y + (GetComponent<RectTransform>().sizeDelta.y / 2)) / GetComponent<RectTransform>().sizeDelta.y;
        currentMousePosition = new Vector2(proportionX, proportionY);
    }
}