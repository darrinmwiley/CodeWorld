using RoslynCSharp;
using UnityEngine;

public class GameController : MonoBehaviour
{

    ConsoleController consoleController;

    //we're going to assume XY plane for now
    public void fitRectangle(float tlx, float tly, float brx, float bry)
    {
        // Calculate the center of the rectangle
        float midX = (tlx + brx) / 2;
        float midY = (tly + bry) / 2;
        
        // Calculate the width and height of the rectangle
        float width = Mathf.Abs(brx - tlx);
        float height = Mathf.Abs(bry - tly);
        
        // Find the main camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("Main camera not found.");
            return;
        }
        
        // Set the camera's orthographic size based on the rectangle's dimensions
        // assuming the camera's default rotation is looking down the negative Z-axis
        float orthoSizeY = height / 2f;
        mainCamera.orthographicSize = orthoSizeY;
        
        // Calculate the aspect ratio of the rectangle
        float aspectRatio = width / height;
        
        // Calculate the new camera position
        Vector3 newPosition = new Vector3(midX, midY, -10);
        mainCamera.transform.position = newPosition;
        
        // Set the camera's orthographic view
        mainCamera.orthographic = true;
        mainCamera.aspect = aspectRatio;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
