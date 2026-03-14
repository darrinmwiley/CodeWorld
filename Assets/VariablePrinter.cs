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

    // Overriding the base click handler
    protected override void HandleClick()
    {
        if (printerWindow != null)
        {
            printerWindow.ShowWindow();
            OnPrinterClicked?.Invoke();
        }
    }

    public void PrintShape(string type)
    {
        GameObject proto = null;
        if (type == "int") proto = Square;
        else if (type == "boolean") proto = Triangle;
        else if (type == "double") proto = Rect;

        if (proto != null) 
            StartCoroutine(SpawnAndPush(proto));
    }

    private IEnumerator SpawnAndPush(GameObject proto)
    {
        Debug.Log("Starting to print shape: " + proto.name);
        // Spawn in World Space to preserve original Prefab scale
        GameObject copy = Instantiate(proto, proto.transform.position, proto.transform.rotation);
        copy.SetActive(true);

        Collider shapeCol = copy.GetComponent<Collider>();
        
        // Move in the direction of the Printer's negative Y axis
        Vector3 pushDirection = -transform.up;

        while (shapeCol != null && printerCollider.bounds.Intersects(shapeCol.bounds))
        {
            copy.transform.position += pushDirection * pushSpeed * Time.deltaTime;
            yield return null;
        }

        Debug.Log($"{proto.name} has cleared the printer bounds.");
    }
}