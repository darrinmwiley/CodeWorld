using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ConsoleCharController : MonoBehaviour
{
    public void UpdateText(string newText)
    {
        Canvas canvas = gameObject.GetComponentInChildren<Canvas>();

        if (canvas != null)
        {
            TextMeshProUGUI textMeshPro = canvas.GetComponentInChildren<TextMeshProUGUI>();

            if (textMeshPro != null)
            {
                textMeshPro.text = newText;
            }
            else
            {
                Debug.LogError("TextMeshPro component not found in the canvas hierarchy.");
            }
        }
        else
        {
            Debug.LogError("Canvas component not found in the target object hierarchy.");
        }
    }

    public void UpdateTextColor(Color color)
    {
        Canvas canvas = gameObject.GetComponentInChildren<Canvas>();

        if (canvas != null)
        {
            TextMeshProUGUI textMeshPro = canvas.GetComponentInChildren<TextMeshProUGUI>();

            if (textMeshPro != null)
            {
                textMeshPro.color = color;
            }
            else
            {
                Debug.LogError("TextMeshPro component not found in the canvas hierarchy.");
            }
        }
        else
        {
            Debug.LogError("Canvas component not found in the target object hierarchy.");
        }
    }

    public void UpdateColor(Color color) 
    {
        GameObject cube = gameObject.transform.Find("Cube").gameObject;
        MeshRenderer renderer = cube.GetComponent<MeshRenderer>();

        if(renderer == null)
        {
            renderer = cube.AddComponent<MeshRenderer>();
        }

        renderer.material.SetColor("_color", color);
    }
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
