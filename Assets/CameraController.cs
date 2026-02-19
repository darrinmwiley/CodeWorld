using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f;          // Speed at which the camera moves.
    public float edgeThreshold = 10f;      // Distance from screen edge to start moving the camera.
    public float edgeMoveSpeed = 5f;       // Speed at which the camera moves when near the edge.
    public float zoomSpeed = 5f;           // Speed at which the camera zooms.
    private float zoomFactor = 1.732f;     // Factor based on tan(60 degrees) for adjusting z.

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Keyboard input for camera movement
        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            moveDirection += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDirection += Vector3.back;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveDirection += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDirection += Vector3.right;
        }

        // Normalize to prevent faster diagonal movement
        moveDirection.Normalize();

        // Apply keyboard movement
        mainCamera.transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

        // Mouse edge movement
        Vector3 mousePosition = Input.mousePosition;

        // Check if the mouse is within the screen boundaries
        if (mousePosition.x >= 0 && mousePosition.x <= Screen.width &&
            mousePosition.y >= 0 && mousePosition.y <= Screen.height)
        {
            if (mousePosition.x >= Screen.width - edgeThreshold)
            {
                mainCamera.transform.Translate(Vector3.right * edgeMoveSpeed * Time.deltaTime, Space.World);
            }
            if (mousePosition.x <= edgeThreshold)
            {
                mainCamera.transform.Translate(Vector3.left * edgeMoveSpeed * Time.deltaTime, Space.World);
            }
            if (mousePosition.y >= Screen.height - edgeThreshold)
            {
                mainCamera.transform.Translate(Vector3.forward * edgeMoveSpeed * Time.deltaTime, Space.World);
            }
            if (mousePosition.y <= edgeThreshold)
            {
                mainCamera.transform.Translate(Vector3.back * edgeMoveSpeed * Time.deltaTime, Space.World);
            }
        }

        // Zooming (assuming upward zoom = zoom out, downward zoom = zoom in)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            Zoom(-scrollInput);
        }
    }

    void Zoom(float scrollInput)
    {
        // Calculate the change in y position based on the scroll input
        float deltaY = scrollInput * zoomSpeed;
        Vector3 currentPosition = mainCamera.transform.position;

        // Adjust the y and z coordinates
        float newY = currentPosition.y + deltaY;
        float newZ = currentPosition.z - deltaY / zoomFactor;

        // Apply the new position to the camera
        mainCamera.transform.position = new Vector3(currentPosition.x, newY, newZ);
    }
}
