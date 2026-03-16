
using UnityEngine;

public class HoldableItem : MonoBehaviour
{
    [Header("Configuration")]
    public ClickListener clickListener;
    public Transform holdPosition;
    
    private bool isHeld = false;
    private Rigidbody rb;
    private Transform originalParent;
    private ItemSocket currentSocket; 

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (clickListener != null) clickListener.AddClickHandler(OnToggleHold);
    }

    public void OnToggleHold()
    {
        if (!isHeld) Pickup();
        else Drop();
    }

    private void Pickup()
    {
        // Notify the current socket we are leaving
        if (currentSocket != null)
        {
            currentSocket.NotifyUnsocket(gameObject);
            currentSocket = null; 
        }

        isHeld = true;
        originalParent = transform.parent;

        // Parent to hold position - gem will inherit the scale of 'holdPosition'
        transform.SetParent(holdPosition);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (rb) rb.isKinematic = true;
    }

    private void Drop()
    {
        // Find all sockets in the scene
        ItemSocket[] sockets = FindObjectsOfType<ItemSocket>();
        ItemSocket focusedSocket = null;

        foreach (var s in sockets)
        {
            if (s.isFocused)
            {
                focusedSocket = s;
                break;
            }
        }

        // Attempt to socket, otherwise drop
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
        
        // Use snapPoint if it exists, otherwise default to the socket object itself
        Transform targetParent = socket.snapPoint != null ? socket.snapPoint : socket.transform;
        
        transform.SetParent(targetParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        
        // Note: We no longer set localScale here. 
        // The gem will now match the scale of the targetParent automatically.

        if (rb) rb.isKinematic = true;
    }

    private void PerformStandardDrop()
    {
        isHeld = false;
        transform.SetParent(originalParent);
        
        // When dropping back into the world, we want to ensure it doesn't 
        // stay tiny or huge from the previous parent's scale.
        // If your gems have a specific 'default' scale, you can set it here.
        transform.localScale = Vector3.one; 

        if (rb) 
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero; // Optional: stop the object's momentum
        }
    }
}