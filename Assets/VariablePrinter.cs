using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class VariablePrinter : BaseClickable
{
    [Header("UI Window")]
    //public Draggable2PaneWindow printerWindow;
    public UnityEvent OnPrinterClicked;

    [Header("Shape Prototypes")]
    public GameObject Square; 
    public GameObject Triangle;
    public GameObject Rect;

    [Header("Output Configuration")]
    public Transform outputNode; 

    [Header("Settings")]
    public float pushSpeed = 1.5f;
    
    private Collider printerCollider;

    protected override void Awake()
    {
        base.Awake();
        printerCollider = GetComponent<Collider>();
        if (Square) Square.SetActive(false);
        if (Triangle) Triangle.SetActive(false);
        if (Rect) Rect.SetActive(false);
    }

    protected override void HandleClick()
    {
        /*if (printerWindow != null)
        {
            printerWindow.ShowWindow();
            OnPrinterClicked?.Invoke();
        }*/
    }

    public void PrintShape(string type, string value)
    {
        GameObject proto = null;
        if (type == "int") proto = Square;
        else if (type == "boolean") proto = Triangle;
        else if (type == "double") proto = Rect;

        if (proto != null) StartCoroutine(SpawnAndPush(proto, value));
    }

    private IEnumerator SpawnAndPush(GameObject proto, string value)
    {
        Vector3 spawnPos = outputNode != null ? outputNode.position : transform.position;
        Vector3 pushDirection = outputNode != null ? outputNode.forward : -transform.up;

        GameObject copy = Instantiate(proto, spawnPos, proto.transform.rotation);
        
        // Disable collision with player to prevent spinning
        Collider shapeCol = copy.GetComponent<Collider>();
        GameObject player = GameObject.FindGameObjectWithTag("Player"); 
        if (player != null && shapeCol != null)
        {
            Collider playerCol = player.GetComponent<Collider>();
            if (playerCol != null) Physics.IgnoreCollision(shapeCol, playerCol, true);
        }

        ItemValue itemVal = copy.GetComponent<ItemValue>() ?? copy.AddComponent<ItemValue>();
        itemVal.value = value;
        copy.SetActive(true);

        int frames = 0;
        while (shapeCol != null && printerCollider.bounds.Intersects(shapeCol.bounds) && frames < 300)
        {
            copy.transform.position += pushDirection * pushSpeed * Time.deltaTime;
            frames++;
            yield return null;
        }

        // Restore collision
        if (player != null && shapeCol != null)
        {
            Collider playerCol = player.GetComponent<Collider>();
            if (playerCol != null) Physics.IgnoreCollision(shapeCol, playerCol, false);
        }
    }
}