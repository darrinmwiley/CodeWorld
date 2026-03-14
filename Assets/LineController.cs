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

    [Tooltip("Number of points generated between each control point. Higher values prevent mesh artifacts at sharp turns.")]
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

    private const string BaseObjectName = "Line_Base";
    private const string TransitionObjectName = "Line_Transition";

    private void Start()
    {
        EnsureMeshObjects();
        ApplyRendererSettings();
        Regenerate();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RestartTransition();
        }
    }

    public void Regenerate()
    {
        EnsureMeshObjects();
        ApplyRendererSettings();

        List<Vector3> livePoints = GetLivePoints();

        if (livePoints.Count < 2)
        {
            ClearMesh(meshA);
            if (!isTransitioning) ClearMesh(meshB);
            return;
        }

        // Generate smooth path using Catmull-Rom logic
        Spline path = new CatmullRomSpline(livePoints, splineResolution);

        BuildMeshInto(
            meshA,
            path.points,
            lineWidth,
            Mathf.Max(2, crossSectionVertices),
            baseRotationDegrees
        );

        if (!isTransitioning)
        {
            ClearMesh(meshB);
        }
    }

    public void RestartTransition()
    {
        EnsureMeshObjects();
        ApplyRendererSettings();

        List<Vector3> livePoints = GetLivePoints();
        if (livePoints.Count < 2)
        {
            ClearMesh(meshB);
            isTransitioning = false;
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(TransitionRoutine(livePoints));
    }

    private IEnumerator TransitionRoutine(List<Vector3> snapshotPoints)
    {
        isTransitioning = true;
        float elapsed = 0f;
        ClearMesh(meshB);

        // Calculate the full smooth path once
        Spline fullPath = new CatmullRomSpline(snapshotPoints, splineResolution);
        List<Vector3> smoothPoints = fullPath.points;

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionTime);

            // Calculate progress through the dense point list
            int pointCount = smoothPoints.Count;
            int targetIndex = Mathf.FloorToInt(t * (pointCount - 1));

            if (targetIndex >= 1)
            {
                // Create a subset of the smooth path for the "growing" mesh
                List<Vector3> partialPath = smoothPoints.GetRange(0, targetIndex + 1);
                
                BuildMeshInto(
                    meshB,
                    partialPath,
                    lineWidth,
                    Mathf.Max(2, crossSectionVertices),
                    baseRotationDegrees
                );
            }
            else
            {
                ClearMesh(meshB);
            }

            yield return null;
        }

        BuildMeshInto(
            meshB,
            smoothPoints,
            lineWidth,
            Mathf.Max(2, crossSectionVertices),
            baseRotationDegrees
        );

        isTransitioning = false;
        transitionCoroutine = null;
    }

    private void EnsureMeshObjects()
    {
        if (meshFilterA == null || meshRendererA == null)
        {
            GameObject go = FindOrCreateChild(BaseObjectName);
            meshFilterA = GetOrAddComponent<MeshFilter>(go);
            meshRendererA = GetOrAddComponent<MeshRenderer>(go);

            if (meshA == null)
            {
                meshA = new Mesh();
                meshA.name = "Line_Base_Mesh";
                meshA.MarkDynamic();
            }

            meshFilterA.sharedMesh = meshA;
        }

        if (meshFilterB == null || meshRendererB == null)
        {
            GameObject go = FindOrCreateChild(TransitionObjectName);
            meshFilterB = GetOrAddComponent<MeshFilter>(go);
            meshRendererB = GetOrAddComponent<MeshRenderer>(go);

            if (meshB == null)
            {
                meshB = new Mesh();
                meshB.name = "Line_Transition_Mesh";
                meshB.MarkDynamic();
            }

            meshFilterB.sharedMesh = meshB;
        }
    }

    private GameObject FindOrCreateChild(string objectName)
    {
        Transform existing = transform.Find(objectName);
        if (existing != null) return existing.gameObject;

        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
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

    private void ApplyRendererSettings(MeshRenderer mr, Color color, int sortingOrder)
    {
        if (mr == null) return;

        Material source = lineMaterial != null ? lineMaterial : GetDefaultMaterial();
        if (source != null)
        {
            mr.sharedMaterial = new Material(source);
            if (mr.sharedMaterial.HasProperty("_Color")) mr.sharedMaterial.color = color;
        }

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        mr.sortingOrder = sortingOrder;
    }

    private Material GetDefaultMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
        return shader != null ? new Material(shader) : null;
    }

    private List<Vector3> GetLivePoints()
    {
        List<Vector3> live = new List<Vector3>();
        if (points == null) return live;

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] != null) live.Add(points[i].position);
        }
        return live;
    }

    private void BuildMeshInto(Mesh mesh, List<Vector3> centers, float width, int shapeVertexCount, float rotationDegrees)
    {
        if (mesh == null) return;
        mesh.Clear();

        if (centers == null || centers.Count < 2) return;

        if (shapeVertexCount <= 2)
        {
            BuildRibbonMesh(mesh, centers, width, rotationDegrees);
        }
        else
        {
            BuildTubeMesh(mesh, centers, width, shapeVertexCount, rotationDegrees);
        }
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

            Vector3 left = centers[i] - side * halfWidth;
            Vector3 right = centers[i] + side * halfWidth;

            int v = i * 2;
            vertices[v + 0] = transform.InverseTransformPoint(left);
            vertices[v + 1] = transform.InverseTransformPoint(right);

            normals[v + 0] = transform.InverseTransformDirection(faceNormal);
            normals[v + 1] = transform.InverseTransformDirection(faceNormal);

            float uvY = ringCount > 1 ? (float)i / (ringCount - 1) : 0f;
            uv[v + 0] = new Vector2(0f, uvY);
            uv[v + 1] = new Vector2(1f, uvY);
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

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    

    private void BuildTubeMesh(Mesh mesh, List<Vector3> centers, float width, int sides, float rotationDegrees)
    {
        int ringCount = centers.Count;
        float radius = width * 0.5f;

        Vector3[] tangents = ComputeTangents(centers);
        ComputeFrames(centers, tangents, out Vector3[] normalsRef, out Vector3[] binormalsRef);

        int ringVertexCount = sides;
        int sideVertexCount = ringCount * ringVertexCount;
        int capStartCenterIndex = sideVertexCount;
        int capEndCenterIndex = sideVertexCount + 1;

        Vector3[] vertices = new Vector3[sideVertexCount + 2];
        Vector3[] normals = new Vector3[sideVertexCount + 2];
        Vector2[] uv = new Vector2[sideVertexCount + 2];
        List<int> triangles = new List<int>();

        float rotationOffsetRad = rotationDegrees * Mathf.Deg2Rad;

        for (int i = 0; i < ringCount; i++)
        {
            float uvY = ringCount > 1 ? (float)i / (ringCount - 1) : 0f;
            for (int j = 0; j < ringVertexCount; j++)
            {
                float angle = rotationOffsetRad + ((float)j / ringVertexCount) * Mathf.PI * 2f;
                Vector3 radial = (normalsRef[i] * Mathf.Cos(angle) + binormalsRef[i] * Mathf.Sin(angle)).normalized;
                int index = i * ringVertexCount + j;

                vertices[index] = transform.InverseTransformPoint(centers[i] + radial * radius);
                normals[index] = transform.InverseTransformDirection(radial);
                uv[index] = new Vector2((float)j / ringVertexCount, uvY);
            }
        }

        vertices[capStartCenterIndex] = transform.InverseTransformPoint(centers[0]);
        vertices[capEndCenterIndex] = transform.InverseTransformPoint(centers[ringCount - 1]);
        normals[capStartCenterIndex] = transform.InverseTransformDirection(-tangents[0]);
        normals[capEndCenterIndex] = transform.InverseTransformDirection(tangents[ringCount - 1]);
        uv[capStartCenterIndex] = uv[capEndCenterIndex] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < ringCount - 1; i++)
        {
            int ring0 = i * ringVertexCount;
            int ring1 = (i + 1) * ringVertexCount;
            for (int j = 0; j < ringVertexCount; j++)
            {
                int next = (j + 1) % ringVertexCount;
                int a = ring0 + j, b = ring0 + next, c = ring1 + j, d = ring1 + next;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        for (int j = 0; j < ringVertexCount; j++)
        {
            int next = (j + 1) % ringVertexCount;
            triangles.Add(capStartCenterIndex); triangles.Add(next); triangles.Add(j);
        }

        int endRingStart = (ringCount - 1) * ringVertexCount;
        for (int j = 0; j < ringVertexCount; j++)
        {
            int next = (j + 1) % ringVertexCount;
            triangles.Add(capEndCenterIndex); triangles.Add(endRingStart + j); triangles.Add(endRingStart + next);
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
    }

    private Vector3[] ComputeTangents(List<Vector3> centers)
    {
        int count = centers.Count;
        Vector3[] tangents = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 tangent;
            if (i == 0) tangent = centers[1] - centers[0];
            else if (i == count - 1) tangent = centers[count - 1] - centers[count - 2];
            else tangent = centers[i + 1] - centers[i - 1];

            if (tangent.sqrMagnitude < 0.000001f) tangent = Vector3.forward;
            tangents[i] = tangent.normalized;
        }
        return tangents;
    }

    private void ComputeFrames(List<Vector3> centers, Vector3[] tangents, out Vector3[] normalsRef, out Vector3[] binormalsRef)
    {
        int count = centers.Count;
        normalsRef = new Vector3[count];
        binormalsRef = new Vector3[count];

        Vector3 upHint = frameUpHint.sqrMagnitude > 0.000001f ? frameUpHint.normalized : Vector3.up;

        Vector3 n0 = Vector3.ProjectOnPlane(upHint, tangents[0]);
        if (n0.sqrMagnitude < 0.000001f) n0 = Vector3.ProjectOnPlane(Vector3.right, tangents[0]);
        if (n0.sqrMagnitude < 0.000001f) n0 = Vector3.ProjectOnPlane(Vector3.forward, tangents[0]);

        normalsRef[0] = n0.normalized;
        binormalsRef[0] = Vector3.Cross(tangents[0], normalsRef[0]).normalized;

        for (int i = 1; i < count; i++)
        {
            // Use parallel transport to minimize twisting along sharp turns
            Vector3 projectedNormal = Vector3.ProjectOnPlane(normalsRef[i - 1], tangents[i]);
            if (projectedNormal.sqrMagnitude < 0.000001f) projectedNormal = Vector3.ProjectOnPlane(binormalsRef[i - 1], tangents[i]);
            
            normalsRef[i] = projectedNormal.normalized;
            binormalsRef[i] = Vector3.Cross(tangents[i], normalsRef[i]).normalized;
        }
    }

    private void ClearMesh(Mesh mesh)
    {
        if (mesh != null) mesh.Clear();
    }

    private void OnDestroy()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        if (meshA != null) { if (Application.isPlaying) Destroy(meshA); else DestroyImmediate(meshA); }
        if (meshB != null) { if (Application.isPlaying) Destroy(meshB); else DestroyImmediate(meshB); }
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

        GUILayout.Space(8f);
        LineController controller = (LineController)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Regenerate"))
            {
                controller.Regenerate();
                EditorUtility.SetDirty(controller);
            }
            if (GUILayout.Button("Restart Transition"))
            {
                controller.RestartTransition();
                EditorUtility.SetDirty(controller);
            }
        }
    }
}
#endif