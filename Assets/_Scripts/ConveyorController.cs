using UnityEngine;

public class ConveyorController : MonoBehaviour
{
    public float speed = 1.0f; // Positive or negative speed determines direction and magnitude.

    private void FixedUpdate()
    {
        // Calculate the movement vector based on speed and deltaTime.
        Vector3 movement = transform.right * speed * Time.fixedDeltaTime;

        // Boxcast from the bounds of the conveyor belt.
        Bounds bounds = GetComponent<Collider>().bounds;
        Vector3 center = bounds.center;
        Vector3 halfExtents = bounds.extents;

        // Perform the boxcast.
        RaycastHit[] hits = Physics.BoxCastAll(center, halfExtents, movement, Quaternion.identity, movement.magnitude);

        // Apply conveyor motion to physics objects and print their names.
        foreach (RaycastHit hit in hits)
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply the conveyor motion to the object's position.
                rb.position += movement;

                // Print the name of the GameObject hit.
                Debug.Log("Hit Object Name: " + hit.collider.gameObject.name);
            }
        }
    }
}