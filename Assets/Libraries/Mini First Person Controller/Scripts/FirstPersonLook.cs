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

    public float raycastDistance = 10f; // Adjust this value for the raycast distance.

    private GameObject currentlyLookedAtObject;

    //add a set of things you're looking at


    void Reset()
    {
        // Get the character from the FirstPersonMovement in parents.
        character = GetComponentInParent<FirstPersonMovement>().transform;
    }

    void Start()
    {
        // Lock the mouse cursor to the game screen.
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (GameState.IsInUI)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return; // Stop processing mouse look and raycasts while UI is open
        }
        else
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        // Get smooth velocity.
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -90, 90);

        // Rotate camera up-down and controller left-right from velocity.
        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);

         // Raycast to detect objects being looked at.
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, raycastDistance);
        
        // Create a set to track newly looked at objects.
        HashSet<GameObject> newLookingAtObjects = new HashSet<GameObject>();

        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;
            // Check if the object being looked at has a LookListener component.
            LookListener lookListener = hitObject.GetComponent<LookListener>();
            if (lookListener != null)
            {
                if(!lookingAtObjects.Contains(hitObject))
                {
                    // Call a method on the object being looked at (if it has one).
                    lookListener.OnLook();
                }

                newLookingAtObjects.Add(hitObject);
            }
        }

        // Find objects that are no longer being looked at and call OnLookAway.
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
        
        // Update the currently looked at objects.
        lookingAtObjects = newLookingAtObjects;
    }
}
