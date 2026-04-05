using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LineController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private List<Transform> points;
    [SerializeField] private Material lineMaterial;

    [Header("Settings")]
    [SerializeField] private Color color1 = Color.white;
    [SerializeField] private Color color2 = Color.green;
    [SerializeField] private float transitionTime = 2.0f;
    [SerializeField] private float lineWidth = 0.1f;
    [Tooltip("Small amount subtracted from the base mesh width so it doesn't z-fight with the transition mesh.")]
    [SerializeField] private float baseWidthInset = 0.002f;
    [Tooltip("Small width bias applied to the transition mesh to prevent z-fighting flicker against the base mesh.")]
    [SerializeField] private float transitionWidthBias = 0.002f;
    
    public float LineWidth => lineWidth;
    
    [Tooltip("If true, the base line (Mesh A) will not be drawn. Only the transition (Mesh B) will be visible.")]
    [SerializeField] private bool hideBaseMesh = false;

    [Tooltip("Number of points generated between each control point.")]
    [SerializeField] private int splineResolution = 20;

    [Tooltip("2 = flat ribbon, 4 = square prism, larger values approach a circular tube.")]
    [SerializeField] private int crossSectionVertices = 2;

    [Tooltip("Rotates the cross-section around the line direction.")]
    [SerializeField] private float baseRotationDegrees = 0f;

    [Tooltip("Fallback up vector used when constructing the frame.")]
    [SerializeField] private Vector3 frameUpHint = Vector3.up;

    private MeshFilter meshFilterA;
    private MeshFilter meshFilterB;
    private MeshRenderer meshRendererA;
    private MeshRenderer meshRendererB;
    private Mesh meshA;
    private Mesh meshB;

    private Coroutine transitionCoroutine;
    private bool isTransitioning = false;
    private bool hasCompletedAtLeastOneTransition = false;

    private const string BaseObjectName = "Line_Base";
    private const string TransitionObjectName = "Line_Transition";

    public bool IsTransitionComplete => !isTransitioning && hasCompletedAtLeastOneTransition;
    public Color CurrentColor => color2;

    private void Start()
    {
        EnsureMeshObjects();
        ApplyRendererSettings();
        Regenerate();
    }

    public void Regenerate()
    {
        EnsureMeshObjects();
        ApplyRendererSettings();

        List<Vector3> livePoints = GetLivePoints();

        // If hidden or not enough points, clear Mesh A
        if (hideBaseMesh || livePoints.Count < 2)
        {
            ClearMesh(meshA);
        }
        else
        {
            Spline path = new CatmullRomSpline(livePoints, splineResolution);
            float baseVisualWidth = Mathf.Max(0.001f, lineWidth - Mathf.Max(0f, baseWidthInset));
            BuildMeshInto(meshA, path.points, baseVisualWidth, Mathf.Max(2, crossSectionVertices), baseRotationDegrees);
        }

        if (!isTransitioning) ClearMesh(meshB);
    }

    public void RestartTransition()
    {
        EnsureMeshObjects();
        ApplyRendererSettings();
        hasCompletedAtLeastOneTransition = false;

        List<Vector3> livePoints = GetLivePoints();
        if (livePoints.Count < 2)
        {
            ClearMesh(meshB);
            isTransitioning = false;
            return;
        }

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionRoutine(livePoints));
    }

    private IEnumerator TransitionRoutine(List<Vector3> snapshotPoints)
    {
        isTransitioning = true;
        float elapsed = 0f;
        ClearMesh(meshB);

        Spline fullPath = new CatmullRomSpline(snapshotPoints, splineResolution);
        List<Vector3> smoothPoints = fullPath.points;

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionTime);

            int targetIndex = Mathf.FloorToInt(t * (smoothPoints.Count - 1));

            if (targetIndex >= 1)
            {
                List<Vector3> partialPath = smoothPoints.GetRange(0, targetIndex + 1);
                float transitionVisualWidth = lineWidth + Mathf.Max(0f, transitionWidthBias);
                BuildMeshInto(meshB, partialPath, transitionVisualWidth, Mathf.Max(2, crossSectionVertices), baseRotationDegrees);
            }
            else
            {
                ClearMesh(meshB);
            }
            yield return null;
        }

        float finalTransitionVisualWidth = lineWidth + Mathf.Max(0f, transitionWidthBias);
        BuildMeshInto(meshB, smoothPoints, finalTransitionVisualWidth, Mathf.Max(2, crossSectionVertices), baseRotationDegrees);
        isTransitioning = false;
        hasCompletedAtLeastOneTransition = true;
        transitionCoroutine = null;
    }

    /// <summary>
    /// Updates the colors used for the line transition.
    /// </summary>
    public void UpdateLineColors(Color newColor)
    {
        this.color2 = newColor;
        
        // This helper method re-applies the new colors to the MeshRenderers
        ApplyRendererSettings(); 
    }

    private void EnsureMeshObjects()
    {
        if (meshFilterA == null || meshRendererA == null)
        {
            GameObject go = FindOrCreateChild(BaseObjectName);
            meshFilterA = GetOrAddComponent<MeshFilter>(go);
            meshRendererA = GetOrAddComponent<MeshRenderer>(go);
            if (meshA == null) { meshA = new Mesh(); meshA.name = "Line_Base_Mesh"; meshA.MarkDynamic(); }
            meshFilterA.sharedMesh = meshA;
        }

        if (meshFilterB == null || meshRendererB == null)
        {
            GameObject go = FindOrCreateChild(TransitionObjectName);
            meshFilterB = GetOrAddComponent<MeshFilter>(go);
            meshRendererB = GetOrAddComponent<MeshRenderer>(go);
            if (meshB == null) { meshB = new Mesh(); meshB.name = "Line_Transition_Mesh"; meshB.MarkDynamic(); }
            meshFilterB.sharedMesh = meshB;
        }
    }

    private GameObject FindOrCreateChild(string objectName)
    {
        Transform existing = transform.Find(objectName);
        if (existing != null) return existing.gameObject;
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        return go;
    }

    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    private void ApplyRendererSettings()
    {
        ApplyRendererSettings(meshRendererA, color1, 0);
        ApplyRendererSettings(meshRendererB, color2, 1);
    }

    private void ApplyRendererSettings(MeshRenderer mr, Color color, int renderQueueOffset)
    {
        if (mr == null) return;
        Material source = lineMaterial != null ? lineMaterial : GetDefaultMaterial();
        mr.sharedMaterial = new Material(source);
        if (mr.sharedMaterial.HasProperty("_Color")) mr.sharedMaterial.color = color;

        // Keep both meshes depth-tested against world geometry, but enforce
        // a stable draw order between base/transition to avoid camera-angle flicker.
        int baseQueue = source != null ? source.renderQueue : 3000;
        if (baseQueue <= 0) baseQueue = 3000;
        mr.sharedMaterial.renderQueue = baseQueue + Mathf.Max(0, renderQueueOffset);
    }

    private Material GetDefaultMaterial() => new Material(Shader.Find("Sprites/Default"));

    public List<Vector3> GetLivePoints()
    {
        List<Vector3> live = new List<Vector3>();
        if (points == null) return live;
        foreach (var p in points) if (p != null) live.Add(p.position);
        return live;
    }

    private void BuildMeshInto(Mesh mesh, List<Vector3> centers, float width, int shapeVertexCount, float rotationDegrees)
    {
        if (mesh == null) return;
        mesh.Clear();
        if (centers == null || centers.Count < 2) return;

        if (shapeVertexCount <= 2) BuildRibbonMesh(mesh, centers, width, rotationDegrees);
        else BuildTubeMesh(mesh, centers, width, shapeVertexCount, rotationDegrees);
    }

    private void BuildRibbonMesh(Mesh mesh, List<Vector3> centers, float width, float rotationDegrees)
    {
        int ringCount = centers.Count;
        float halfWidth = width * 0.5f;
        Vector3[] tangents = ComputeTangents(centers);
        ComputeFrames(centers, tangents, out Vector3[] normalsRef, out Vector3[] binormalsRef);

        Vector3[] vertices = new Vector3[ringCount * 2];
        Vector3[] normals = new Vector3[ringCount * 2];
        Vector2[] uv = new Vector2[ringCount * 2];
        int[] triangles = new int[(ringCount - 1) * 12];
        float rotationRad = rotationDegrees * Mathf.Deg2Rad;

        for (int i = 0; i < ringCount; i++)
        {
            Vector3 side = (normalsRef[i] * Mathf.Cos(rotationRad) + binormalsRef[i] * Mathf.Sin(rotationRad)).normalized;
            Vector3 faceNormal = Vector3.Cross(side, tangents[i]).normalized;
            int v = i * 2;
            vertices[v + 0] = transform.InverseTransformPoint(centers[i] - side * halfWidth);
            vertices[v + 1] = transform.InverseTransformPoint(centers[i] + side * halfWidth);
            normals[v + 0] = transform.InverseTransformDirection(faceNormal);
            normals[v + 1] = transform.InverseTransformDirection(faceNormal);
            float uvY = ringCount > 1 ? (float)i / (ringCount - 1) : 0f;
            uv[v + 0] = new Vector2(0f, uvY); uv[v + 1] = new Vector2(1f, uvY);
        }

        int tri = 0;
        for (int i = 0; i < ringCount - 1; i++)
        {
            int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
            triangles[tri++] = a; triangles[tri++] = c; triangles[tri++] = b;
            triangles[tri++] = b; triangles[tri++] = c; triangles[tri++] = d;
            triangles[tri++] = b; triangles[tri++] = c; triangles[tri++] = a;
            triangles[tri++] = d; triangles[tri++] = c; triangles[tri++] = b;
        }
        mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void BuildTubeMesh(Mesh mesh, List<Vector3> centers, float width, int sides, float rotationDegrees)
    {
        int ringCount = centers.Count;
        float radius = width * 0.5f;
        Vector3[] tangents = ComputeTangents(centers);
        ComputeFrames(centers, tangents, out Vector3[] normalsRef, out Vector3[] binormalsRef);

        int sideVertexCount = ringCount * sides;
        Vector3[] vertices = new Vector3[sideVertexCount + 2];
        Vector3[] normals = new Vector3[sideVertexCount + 2];
        Vector2[] uv = new Vector2[sideVertexCount + 2];
        List<int> triangles = new List<int>();
        float rotationOffsetRad = rotationDegrees * Mathf.Deg2Rad;

        for (int i = 0; i < ringCount; i++)
        {
            float uvY = ringCount > 1 ? (float)i / (ringCount - 1) : 0f;
            for (int j = 0; j < sides; j++)
            {
                float angle = rotationOffsetRad + ((float)j / sides) * Mathf.PI * 2f;
                Vector3 radial = (normalsRef[i] * Mathf.Cos(angle) + binormalsRef[i] * Mathf.Sin(angle)).normalized;
                int index = i * sides + j;
                vertices[index] = transform.InverseTransformPoint(centers[i] + radial * radius);
                normals[index] = transform.InverseTransformDirection(radial);
                uv[index] = new Vector2((float)j / sides, uvY);
            }
        }

        int capS = sideVertexCount, capE = sideVertexCount + 1;
        vertices[capS] = transform.InverseTransformPoint(centers[0]);
        vertices[capE] = transform.InverseTransformPoint(centers[ringCount - 1]);
        normals[capS] = transform.InverseTransformDirection(-tangents[0]);
        normals[capE] = transform.InverseTransformDirection(tangents[ringCount - 1]);

        for (int i = 0; i < ringCount - 1; i++)
        {
            int r0 = i * sides, r1 = (i + 1) * sides;
            for (int j = 0; j < sides; j++)
            {
                int next = (j + 1) % sides;
                triangles.Add(r0 + j); triangles.Add(r1 + j); triangles.Add(r0 + next);
                triangles.Add(r0 + next); triangles.Add(r1 + j); triangles.Add(r1 + next);
            }
        }
        mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
    }

    private Vector3[] ComputeTangents(List<Vector3> centers)
    {
        int count = centers.Count;
        Vector3[] tangents = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Vector3 t = (i == 0) ? centers[1] - centers[0] : (i == count - 1) ? centers[count - 1] - centers[count - 2] : centers[i + 1] - centers[i - 1];
            tangents[i] = t.normalized;
        }
        return tangents;
    }

    private void ComputeFrames(List<Vector3> centers, Vector3[] tangents, out Vector3[] normalsRef, out Vector3[] binormalsRef)
    {
        int count = centers.Count;
        normalsRef = new Vector3[count]; binormalsRef = new Vector3[count];
        Vector3 n = Vector3.ProjectOnPlane(frameUpHint, tangents[0]).normalized;
        if (n.sqrMagnitude < 0.01f) n = Vector3.ProjectOnPlane(Vector3.right, tangents[0]).normalized;
        normalsRef[0] = n; binormalsRef[0] = Vector3.Cross(tangents[0], n).normalized;
        for (int i = 1; i < count; i++)
        {
            normalsRef[i] = Vector3.ProjectOnPlane(normalsRef[i - 1], tangents[i]).normalized;
            binormalsRef[i] = Vector3.Cross(tangents[i], normalsRef[i]).normalized;
        }
    }

    private void ClearMesh(Mesh m) { if (m != null) m.Clear(); }

    public void StopAndClearTransition()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        isTransitioning = false;
        hasCompletedAtLeastOneTransition = false;
        ClearMesh(meshB);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(LineController))]
public class LineControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();
        LineController c = (LineController)target;
        if (GUILayout.Button("Restart Transition")) c.RestartTransition();
    }
}
#endif