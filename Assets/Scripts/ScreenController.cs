using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenController : MonoBehaviour
{
    public RawImage rawImagePrefab; // The RawImage prefab to project onto the Canvas
    private RawImage rawImageInstance; // The instantiated RawImage component
    private Canvas canvas; // The Canvas component to project onto

    private void Start()
    {
        // Create a RawImage as a child of the Canvas if it doesn't exist
        if (rawImageInstance == null)
        {
            rawImageInstance = Instantiate(rawImagePrefab, canvas.transform);
        }

        // Set the RawImage size to match the GameObject's size
        RectTransform canvasRect = GetComponent<RectTransform>();
        RectTransform rawImageRect = rawImageInstance.GetComponent<RectTransform>();
        Vector3 objectSize = GetComponent<Renderer>().bounds.size; // Assumes that the GameObject has a Renderer component

        // Set the Canvas size to match the GameObject's size in world space
        canvasRect.localScale = objectSize;

        // Set the RawImage size to match the Canvas size
        rawImageRect.sizeDelta = canvasRect.sizeDelta;
    }
}
