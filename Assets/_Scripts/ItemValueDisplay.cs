using UnityEngine;
using TMPro;

public class ItemValueDisplay : MonoBehaviour
{
    [Header("References")]
    public GameObject targetObject; 
    public TextMeshProUGUI textDisplay;

    private ItemValue cachedItemValue;

    void Start()
    {
        // Cache the component once at the start for better performance
        if (targetObject != null)
        {
            cachedItemValue = targetObject.GetComponent<ItemValue>();
        }
    }

    void Update()
    {
        // If the target or component is missing, do nothing (No empty text/prefix)
        if (cachedItemValue == null && targetObject != null)
        {
            cachedItemValue = targetObject.GetComponent<ItemValue>();
        }

        if (cachedItemValue != null)
        {
            // Just the value, no extra strings
            textDisplay.text = cachedItemValue.value.ToString();
        }
    }
}