using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MouseListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public bool isMouseDown = false;
    public bool isMouseDragging = false;
    public Vector2 mouseDownPosition;
    public Vector2 currentMousePosition;

    List<MouseAction> mouseDownHandlers = new List<MouseAction>(){};
    List<MouseAction> mouseUpHandlers = new List<MouseAction>(){};
    List<MouseAction> mouseDragHandlers = new List<MouseAction>(){};

    public delegate void MouseAction();

    public void AddMouseDownHandler(MouseAction mouseAction)
    {
        mouseDownHandlers.Add(mouseAction);
    }

    public void AddMouseUpHandler(MouseAction mouseAction)
    {
        mouseUpHandlers.Add(mouseAction);
    }

    public void AddMouseDragHandler(MouseAction mouseAction)
    {
        mouseDragHandlers.Add(mouseAction);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isMouseDown = true;
        // Calculate proportions when the mouse button is pressed.
        CalculateProportions(eventData.position);
        mouseDownPosition = currentMousePosition;
        foreach(MouseAction mouseAction in mouseDownHandlers){
            mouseAction();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("pointer up");
        isMouseDown = false;
        isMouseDragging = false;
        CalculateProportions(eventData.position);
        foreach(MouseAction mouseAction in mouseUpHandlers){
            mouseAction();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isMouseDown)
        {
            isMouseDragging = true;
            CalculateProportions(eventData.position);
            foreach (MouseAction mouseAction in mouseDragHandlers)
            {
                mouseAction();
            }
        }
    }

    private void CalculateProportions(Vector2 screenPosition)
    {
        // Convert screen coordinates to canvas coordinates
        RectTransform canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out canvasPosition);

        // Calculate proportions based on canvas size
        float proportionX = (canvasPosition.x - transform.localPosition.x + (GetComponent<RectTransform>().sizeDelta.x / 2)) / GetComponent<RectTransform>().sizeDelta.x;
        float proportionY = (canvasPosition.y - transform.localPosition.y + (GetComponent<RectTransform>().sizeDelta.y / 2)) / GetComponent<RectTransform>().sizeDelta.y;
        currentMousePosition = new Vector2(proportionX, proportionY);
    }
}