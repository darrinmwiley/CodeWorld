using UnityEngine;
using System.Collections.Generic;

public class HoldableItem : MonoBehaviour
{
    public ClickListener clickListener;
    public Transform holdPosition;
    
    private bool isHeld = false;
    private Rigidbody rb;
    private Transform originalParent;
    private Vector3 originalWorldScale;
    private ItemSocket currentSocket; 

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalWorldScale = transform.lossyScale;
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

        transform.SetParent(holdPosition);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Apply scale correctly using division to negate parent influence
        Vector3 targetScale = originalWorldScale * 0.5f;
        transform.localScale = new Vector3(
            targetScale.x / transform.parent.lossyScale.x,
            targetScale.y / transform.parent.lossyScale.y,
            targetScale.z / transform.parent.lossyScale.z
        );

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
        transform.SetParent(socket.snapPoint != null ? socket.snapPoint : socket.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one; 

        if (rb) rb.isKinematic = true;
    }

    private void PerformStandardDrop()
    {
        isHeld = false;
        transform.SetParent(originalParent);
        
        // Restore to original scale by re-calculating against the new parent
        Vector3 parentScale = (transform.parent != null) ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(
            originalWorldScale.x / parentScale.x,
            originalWorldScale.y / parentScale.y,
            originalWorldScale.z / parentScale.z
        );

        if (rb) rb.isKinematic = false;
    }
}