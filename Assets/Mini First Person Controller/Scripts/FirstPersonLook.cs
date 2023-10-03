using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField]
    Transform character;
    public float sensitivity = 2;
    public float smoothing = 1.5f;

    Vector2 velocity;
    Vector2 frameVelocity;

    public float raycastDistance = 10f; // Adjust this value for the raycast distance.

    private GameObject currentlyLookedAtObject;


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
        // Get smooth velocity.
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -90, 90);

        // Rotate camera up-down and controller left-right from velocity.
        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);

        // Raycast to detect the object being looked at.
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Check if the object being looked at has changed.
            if (hitObject != currentlyLookedAtObject)
            {
                // Call a method on the new object being looked at (if it has one).
                OnLook(hitObject);
                
                // Update the currently looked at object.
                currentlyLookedAtObject = hitObject;
            }
        }
        else
        {
            OnLook(null);
        }
    }

    private void OnLook(GameObject obj)
    {
        if (obj != null)
        {
            // Check if the object has an "Outline" script attached.
            Outline outline = obj.GetComponent<Outline>();
            if (outline != null)
            {
                // Activate or deactivate the outline based on the 'activateOutline' parameter.
                outline.enabled = true;
            }
        }
        if(currentlyLookedAtObject != null)
        {
            // Check if the object has an "Outline" script attached.
            Outline old = currentlyLookedAtObject.GetComponent<Outline>();
            if (old != null)
            {
                // Activate or deactivate the outline based on the 'activateOutline' parameter.
                old.enabled = false;
            }
            currentlyLookedAtObject = null;
        }else{
            currentlyLookedAtObject = null;
        }
    }
}
