using UnityEngine;
using System.Collections.Generic;
using System;

public class ItemSocket : MonoBehaviour
{
    public Transform snapPoint;
    public LookListener lookListener;
    public bool isFocused = false;
    
    [Header("Acceptance Rules")]
    public List<string> acceptedNames;
    
    private GameObject occupiedItem;

    // This is a Property. Access it as 'socket.IsOccupied', not 'socket.IsOccupied()'
    public bool IsOccupied => occupiedItem != null;

    public Action<GameObject> OnItemSocketed;
    public Action<GameObject> OnItemUnsocketed;

    void Awake()
    {
        if (lookListener == null) lookListener = GetComponent<LookListener>();
        lookListener.AddLookHandler(() => isFocused = true);
        lookListener.AddLookAwayHandler(() => isFocused = false);
    }

    // ADD THIS METHOD so other scripts can access the item
    public GameObject GetSocketedItem()
    {
        return occupiedItem;
    }

    public bool TrySocket(GameObject item)
    {
        if (IsOccupied)
        {
            Debug.Log("Socket is already occupied!");
            return false;
        }

        string cleanName = item.name.Replace("(Clone)", "").Trim();
        if (acceptedNames.Contains(cleanName))
        {
            occupiedItem = item;
            OnItemSocketed?.Invoke(item);
            return true;
        }
        return false;
    }

    public void NotifyUnsocket(GameObject item)
    {
        if (occupiedItem == item)
        {
            occupiedItem = null;
            OnItemUnsocketed?.Invoke(item);
        }
    }
}