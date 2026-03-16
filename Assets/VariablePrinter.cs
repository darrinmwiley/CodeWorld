using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class VariablePrinter : BaseClickable
{
    [Header("UI Window")]
    public Draggable2PaneWindow printerWindow;
    public UnityEvent OnPrinterClicked;

    [Header("Shape Prototypes")]
    public GameObject Square;   // int
    public GameObject Triangle; // boolean
    public GameObject Rect;     // double

    [Header("Output Configuration")]
    // Create an empty child GameObject at the exit point 
    // and rotate it so the Blue Arrow points in the exit direction.
    public Transform outputNode; 

    [Header("Settings")]
    public float pushSpeed = 1.5f;
    
    private Collider printerCollider;

    protected override void Awake()
    {
        // Run BaseClickable setup first
        base.Awake();

        printerCollider = GetComponent<Collider>();
        
        // Hide prototypes
        if (Square) Square.SetActive(false);
        if (Triangle) Triangle.SetActive(false);
        if (Rect) Rect.SetActive(false);
    }

    protected override void HandleClick()
    {
        if (printerWindow != null)
        {
            printerWindow.ShowWindow();
            OnPrinterClicked?.Invoke();
        }
    }

    /// <summary>
    /// Prints a shape and assigns the provided value to its ItemValue component.
    /// </summary>
    /// <param name="type">The type of shape ("int", "boolean", "double").</param>
    /// <param name="value">The data value to assign to the new object.</param>
    public void PrintShape(string type, string value)
    {
        GameObject proto = null;
        if (type == "int") proto = Square;
        else if (type == "boolean") proto = Triangle;
        else if (type == "double") proto = Rect;

        if (proto != null) 
            StartCoroutine(SpawnAndPush(proto, value));
    }

    private IEnumerator SpawnAndPush(GameObject proto, string value)
    {
        // 1. Determine spawn position and push direction
        Vector3 spawnPos = outputNode != null ? outputNode.position : transform.position;
        Vector3 pushDirection = outputNode != null ? outputNode.forward : -transform.up;

        // 2. Spawn the object
        GameObject copy = Instantiate(proto, spawnPos, proto.transform.rotation);
        
        // 3. Assign the value to the ItemValue component
        ItemValue itemVal = copy.GetComponent<ItemValue>();
        if (itemVal == null)
        {
            itemVal = copy.AddComponent<ItemValue>();
        }
        itemVal.value = value;

        copy.SetActive(true);

        Collider shapeCol = copy.GetComponent<Collider>();

        // Safety break to prevent infinite loops
        int frames = 0;
        int maxFrames = 300; 

        while (shapeCol != null && printerCollider.bounds.Intersects(shapeCol.bounds) && frames < maxFrames)
        {
            copy.transform.position += pushDirection * pushSpeed * Time.deltaTime;
            frames++;
            yield return null;
        }

        Debug.Log($"{proto.name} printed with value: {value}");
    }
}