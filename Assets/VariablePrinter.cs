using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
    public int maxQueueSize = 10;
    public float pauseBetweenPrints = 0.2f;
    
    private Collider printerCollider;

    private struct PrintJob 
    {
        public string type;
        public string value;
        public PrintJob(string t, string v) { type = t; value = v; }
    }

    private Queue<PrintJob> _printQueue = new Queue<PrintJob>();
    private bool _isProcessingQueue = false;

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
        if (_printQueue.Count >= maxQueueSize)
        {
            Debug.LogWarning($"[VariablePrinter] Queue full ({maxQueueSize}). Discarding job: {type}={value}");
            return;
        }

        _printQueue.Enqueue(new PrintJob(type, value));
        
        if (!_isProcessingQueue)
        {
            StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        _isProcessingQueue = true;

        while (_printQueue.Count > 0)
        {
            PrintJob job = _printQueue.Dequeue();
            GameObject proto = GetPrototype(job.type);

            if (proto != null)
            {
                // Wait for the specific spawn and push to complete before starting next one
                yield return StartCoroutine(SpawnAndPush(proto, job.value));
                yield return new WaitForSeconds(pauseBetweenPrints);
            }
        }

        _isProcessingQueue = false;
    }

    private GameObject GetPrototype(string type)
    {
        if (type == "int") return Square;
        if (type == "boolean") return Triangle;
        if (type == "double") return Rect;
        return null;
    }

    private IEnumerator SpawnAndPush(GameObject proto, string value)
    {
        Vector3 spawnPos = outputNode != null ? outputNode.position : transform.position;
        Vector3 pushDirection = outputNode != null ? outputNode.forward : -transform.up;

        GameObject copy = Instantiate(proto, spawnPos, proto.transform.rotation);
        
        // 1. Disable Outline by default
        Outline outline = copy.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        // 2. Initial Physics State: Kinematic and Trigger so it slides out cleanly
        Rigidbody rb = copy.GetComponent<Rigidbody>();
        Collider shapeCol = copy.GetComponent<Collider>();
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (shapeCol != null)
        {
            shapeCol.isTrigger = true;
        }

        // Disable collision with player specifically (just in case)
        GameObject player = GameObject.FindGameObjectWithTag("Player"); 
        if (player != null && shapeCol != null)
        {
            Collider playerCol = player.GetComponent<Collider>();
            if (playerCol != null) Physics.IgnoreCollision(shapeCol, playerCol, true);
        }

        ItemValue itemVal = copy.GetComponent<ItemValue>() ?? copy.AddComponent<ItemValue>();
        itemVal.value = value;
        copy.SetActive(true);

        // 3. Pushing Phase
        int frames = 0;
        while (shapeCol != null && printerCollider.bounds.Intersects(shapeCol.bounds) && frames < 600)
        {
            copy.transform.position += pushDirection * pushSpeed * Time.deltaTime;
            frames++;
            yield return null;
        }

        // 4. Final Physics State: Restore gravity, collision, and physics response
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (shapeCol != null)
        {
            shapeCol.isTrigger = false;
        }

        // Restore collision with player
        if (player != null && shapeCol != null)
        {
            Collider playerCol = player.GetComponent<Collider>();
            if (playerCol != null) Physics.IgnoreCollision(shapeCol, playerCol, false);
        }
    }
}