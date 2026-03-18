using UnityEngine;

public class ValueColorController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The GameObject containing the ItemValue component to monitor.")]
    public GameObject targetItem;
    
    [Header("Material Settings")]
    public Material baseMaterial;
    [ColorUsage(true, true)]
    public Color TrueColor = Color.green;
    [ColorUsage(true, true)]
    public Color FalseColor = Color.red;

    private MeshRenderer meshRenderer;
    private string lastValue = "";

    void Awake()
    {
        // Get the renderer on the object this script is attached to
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        if (targetItem == null || meshRenderer == null) return;

        // Read the ItemValue component from the target item
        ItemValue itemValue = targetItem.GetComponent<ItemValue>();
        
        if (itemValue == null) return;

        // Only trigger if the value has changed since the last frame
        if (itemValue.value != lastValue)
        {
            string currentVal = itemValue.value.ToLower().Trim();

            if (currentVal == "true" || currentVal == "false")
            {
                UpdateMaterial(currentVal == "true");
            }

            // Update state so we only trigger "for the first time after being set"
            lastValue = itemValue.value;
        }
    }

    private void UpdateMaterial(bool isTrue)
    {
        // Create an explicit copy of the base material
        Material materialCopy = new Material(baseMaterial);
        
        // Select the color based on the boolean state
        Color targetColor = isTrue ? TrueColor : FalseColor;

        // Set the _Color property on the material instance
        materialCopy.SetColor("_Color", targetColor);

        // Assign the new material instance to the renderer
        meshRenderer.material = materialCopy;
    }
}