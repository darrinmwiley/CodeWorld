using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
 
public class DraggableWindowScript : MonoBehaviour, IDragHandler
{
    public Canvas canvas;
 
    private RectTransform rectTransform;
 
    // Start is called before the first frame update
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }
 
    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        Debug.Log("drag");
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
}
 