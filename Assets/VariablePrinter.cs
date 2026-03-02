using UnityEngine;
using System.Collections;

public class VariablePrinter : MonoBehaviour
{
    [Header("Shape Prototypes")]
    public GameObject Square;   // int
    public GameObject Triangle; // boolean
    public GameObject Rect;     // double

    [Header("Settings")]
    public float pushSpeed = 1.5f;
    
    private Collider printerCollider;

    private void Awake()
    {
        printerCollider = GetComponent<Collider>();
        
        // Hide prototypes on start
        if (Square) Square.SetActive(false);
        if (Triangle) Triangle.SetActive(false);
        if (Rect) Rect.SetActive(false);
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
        // 1. Create copy as child of Printer for perfect alignment
        GameObject copy = Instantiate(proto, proto.transform.position, proto.transform.rotation, this.transform);
        copy.SetActive(true);

        Collider shapeCol = copy.GetComponent<Collider>();
        
        // 2. Travel in -Y parent space (Blender Forward)
        // We modify the Y component of the localPosition vector
        while (shapeCol != null && printerCollider.bounds.Intersects(shapeCol.bounds))
        {
            Vector3 currentLocalPos = copy.transform.localPosition;
            currentLocalPos.y -= pushSpeed * Time.deltaTime; 
            copy.transform.localPosition = currentLocalPos;

            yield return null;
        }

        // Object remains a child of the printer as requested
        Debug.Log($"{proto.name} has cleared the printer bounds.");
    }
}