using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Villager : MonoBehaviour
{
    private Transform cameraTransform;
    public GameObject outline;
    private float ht = 0.67f;

    // Start is called before the first frame update
    void Start()
    {
        // Get the main camera's transform
        cameraTransform = Camera.main.transform;

        // Ensure the outline is initially off
        if (outline != null)
        {
            outline.SetActive(false);
        }

        // Get the current rotation of the villager
        Vector3 villagerRotation = transform.rotation.eulerAngles;

        // Set the X rotation to match the main camera's X rotation
        villagerRotation.x = cameraTransform.rotation.eulerAngles.x;

        // Calculate the new y-coordinate using the given formula
        float newY = 0.25f + Mathf.Sin(Mathf.Deg2Rad * (90f - cameraTransform.rotation.eulerAngles.x)) * ht / 2;

        // Apply the new y-coordinate to the villager's position
        Vector3 villagerPosition = transform.position;
        villagerPosition.y = newY;
        transform.position = villagerPosition;

        // Apply the updated rotation back to the villager
        transform.rotation = Quaternion.Euler(villagerRotation);
    }

    void OnMouseEnter()
    {
        // Turn the outline on when the mouse enters the sprite area
        if (outline != null)
        {
            outline.SetActive(true);
        }
    }

    void OnMouseExit()
    {
        // Turn the outline off when the mouse exits the sprite area
        if (outline != null)
        {
            outline.SetActive(false);
        }
    }
}
