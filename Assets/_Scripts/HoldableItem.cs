using UnityEngine;

public class HoldableItem : MonoBehaviour
{
    [Header("Configuration")]
    public ClickListener clickListener;
    public Transform holdPosition;
    
    private bool isHeld = false;
    private Rigidbody rb;
    private Transform originalParent;
    
    // The "Source of Truth" for the item's size
    private Vector3 originalWorldScale; 
    private ItemSocket currentSocket; 

    // The target we are currently 'ghosting'
    private Transform followTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Capture how big the item is supposed to be in the world
        originalWorldScale = transform.lossyScale;
    }

    void Start()
    {
        if (clickListener != null) clickListener.AddClickHandler(OnToggleHold);
    }

    // This runs after all movements, ensuring no jitter
    void LateUpdate()
    {
        if (followTarget != null)
        {
            // We match the world position and rotation exactly
            transform.position = followTarget.position;
            transform.rotation = followTarget.rotation;
        }
    }

    public void OnToggleHold()
    {
        if (!isHeld) Pickup();
        else Drop();
    }

    private void Pickup()
    {
        if (currentSocket != null)
        {
            currentSocket.NotifyUnsocket(gameObject);
            currentSocket = null; 
        }

        isHeld = true;
        originalParent = transform.parent;

        // THE TRICK: Set parent to NULL (the scene root). 
        // Objects at the root CANNOT shear, even with non-uniform scale.
        transform.SetParent(null);
        
        // Tell the LateUpdate to start following the hand
        followTarget = holdPosition;
        
        // Scale it to 50% of its natural world size
        transform.localScale = originalWorldScale * 0.5f;

        if (rb) rb.isKinematic = true;
    }

    private void Drop()
    {
        ItemSocket[] sockets = FindObjectsOfType<ItemSocket>();
        ItemSocket focusedSocket = null;

        foreach (var s in sockets)
        {
            if (s.isFocused) { focusedSocket = s; break; }
        }

        if (focusedSocket != null && focusedSocket.TrySocket(gameObject))
        {
            SnapToSocket(focusedSocket);
        }
        else
        {
            PerformStandardDrop();
        }
    }

    private void SnapToSocket(ItemSocket socket)
    {
        currentSocket = socket; 
        isHeld = false;
        
        Transform targetAnchor = socket.snapPoint != null ? socket.snapPoint : socket.transform;
        
        // Keep it at the root to prevent shearing from the socket's scale
        transform.SetParent(null);
        followTarget = targetAnchor;
        
        // Match the target's WORLD scale (it will now fit the stretched socket perfectly)
        transform.localScale = targetAnchor.lossyScale;

        if (rb) rb.isKinematic = true;
    }

    private void PerformStandardDrop()
    {
        isHeld = false;
        followTarget = null;
        
        // Return to the original hierarchy
        transform.SetParent(originalParent);
        
        // Restore true visual size
        SetWorldScale(originalWorldScale); 

        if (rb) 
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
        }
    }

    // Helper to ensure scale is correct even when returning to a potentially scaled parent
    private void SetWorldScale(Vector3 targetWorldScale)
    {
        if (transform.parent == null)
        {
            transform.localScale = targetWorldScale;
            return;
        }
        Vector3 pScale = transform.parent.lossyScale;
        transform.localScale = new Vector3(
            targetWorldScale.x / pScale.x,
            targetWorldScale.y / pScale.y,
            targetWorldScale.z / pScale.z
        );
    }
}