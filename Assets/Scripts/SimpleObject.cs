using UnityEngine;

public class SimpleObject : MonoBehaviour
{

    private MeshFilter meshFilter;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        gameObject.AddComponent<MeshRenderer>();
    }

    public void SetShape(ShapeType shapeType)
    {
        Mesh mesh = null;

        if (shapeType == ShapeType.SPHERE)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            // Deactivate the sphere object to remove it from the scene
            sphere.SetActive(false);
        }
        else if (shapeType == ShapeType.CUBE)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh = cube.GetComponent<MeshFilter>().sharedMesh;
            // Deactivate the cube object to remove it from the scene
            cube.SetActive(false);
        }

        // Assign the created mesh to the MeshFilter component
        meshFilter.mesh = mesh;
    }
}
