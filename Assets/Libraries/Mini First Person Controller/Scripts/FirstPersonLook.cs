using System.Collections.Generic;
using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField]
    Transform character;
    public float sensitivity = 2;
    public float smoothing = 1.5f;

    Vector2 velocity;
    Vector2 frameVelocity;
    private HashSet<GameObject> lookingAtObjects = new HashSet<GameObject>();

    public float raycastDistance = 10f; 
    private GameObject currentlyLookedAtObject;

    void Reset()
    {
        character = GetComponentInParent<FirstPersonMovement>().transform;
    }

    void Update()
    {
        // 1. EXIT IMMEDIATELY IF IN UI
        if (GameState.IsInUI)
        {
            // Reset frame velocity so the camera doesn't "drift" or "spin" 
            // from the last movement before entering UI.
            frameVelocity = Vector2.zero;
            return; 
        }

        // 2. PROCESS MOUSE LOOK
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        
        // Use smoothing logic
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -90, 90);

        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);

        // 3. RAYCASTING
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, raycastDistance);
        HashSet<GameObject> newLookingAtObjects = new HashSet<GameObject>();

        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;
            LookListener lookListener = hitObject.GetComponent<LookListener>();
            if (lookListener != null)
            {
                if(!lookingAtObjects.Contains(hitObject))
                {
                    lookListener.OnLook();
                }
                newLookingAtObjects.Add(hitObject);
            }
        }

        foreach (GameObject oldObject in lookingAtObjects)
        {
            if (!newLookingAtObjects.Contains(oldObject))
            {
                LookListener lookListener = oldObject.GetComponent<LookListener>();
                if (lookListener != null)
                {
                    lookListener.OnLookAway();
                }
            }
        }
        
        lookingAtObjects = newLookingAtObjects;
    }
}