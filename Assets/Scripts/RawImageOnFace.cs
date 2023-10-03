using UnityEngine;
using UnityEngine.UI;

public class RawImageOnFace : MonoBehaviour
{
    public RawImage rawImageToPaste; // Reference to the RawImage you want to paste.

    private void Start()
    {
        // Get the cube's renderer.
        Renderer cubeRenderer = GetComponent<Renderer>();

        // Create a new material for the cube's +X face.
        Material cubeMaterial = new Material(cubeRenderer.sharedMaterial);

        // Assign the RawImage's texture to the +X face material.
        cubeMaterial.mainTexture = rawImageToPaste.texture;

        // Set the material for the +X face.
        cubeRenderer.materials[0] = cubeMaterial;
    }
}
