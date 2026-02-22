using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GraphingUtility : MonoBehaviour
{
    public enum Level { Square, T, L, Arrow }

    [Header("Level Settings")]
    public Level currentLevel = Level.Square;
    public bool showGoalPath = true;
    public Color goalColor = new Color(1f, 1f, 0f, 0.5f);
    public float dashSize = 0.05f;

    [Header("Coordinate Bounds")]
    public float xMin = -5f;
    public float xMax = 5f;
    public float yMin = -5f;
    public float yMax = 5f;

    [Header("Turtle Settings")]
    public Vector2 turtlePosition = Vector2.zero;
    [Range(0, 7)] public int turtleDirection = 0; 
    public float turtleLength = 0.5f; 
    public float turtleWidth = 0.25f;
    public Color turtleColor = Color.red;

    [Header("Visuals")]
    public Color gridColor = new Color(0.3f, 0.3f, 0.3f);
    public Color axisColor = Color.white;
    public float lineWidth = 0.02f;
    
    private const float GRID_Z = -0.001f;
    private const float GOAL_Z = -0.0015f;
    private const float TRAIL_Z = -0.0018f;
    private const float TURTLE_Z = -0.002f; 

    private List<GameObject> _elements = new List<GameObject>();
    private List<GameObject> _trailElements = new List<GameObject>();

    // These now return the actual coordinate delta for the step
    private static readonly int[,] Directions = new int[8, 2] {
        { 0,  1 }, { 1,  1 }, { 1,  0 }, { 1, -1 }, 
        { 0, -1 }, {-1, -1 }, {-1,  0 }, {-1,  1 }
    };

    private static readonly Dictionary<Level, Vector2[]> LevelPaths = new Dictionary<Level, Vector2[]>()
    {
        { Level.Square, new Vector2[] { new(0,0), new(-1,0), new(-1,1), new(0,1), new(0,0) } },
        { Level.T, new Vector2[] { new(0,0), new(-1,0), new(-1,1), new(-2,1), new(-2,2), new(-1,2), new(0,2), new(1,2), new(1,1), new(0,1), new(0,0) } },
        { Level.L, new Vector2[] { new(0,0), new(-1,0), new(-2,0), new(-2,1), new(-2,2), new(-2,3), new(-2,4), new(-1,4), new(-1,3), new(-1,2), new(-1,1), new(0,1), new(0,0) } },
        { Level.Arrow, new Vector2[] { new(0,0), new(-1,0), new(-1,1), new(-1,2), new(-2,2), new(-1,3), new(0,4), new(1,3), new(2,2), new(1,2), new(1,1), new(1,0), new(0,0) } }
    };

    [ContextMenu("Generate Everything")]
    void Start() => Refresh();

    public void Refresh()
    {
        ClearTemporaryElements();
        GenerateGrid();
        if (showGoalPath) DrawGoalPath();
        DrawTurtleTriangle();
    }

    public void ClearTemporaryElements()
    {
        foreach (var obj in _elements)
        {
            if (obj != null) { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
        }
        _elements.Clear();
    }

    public void ClearTrail()
    {
        foreach (var obj in _trailElements)
        {
            if (obj != null) { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
        }
        _trailElements.Clear();
    }

    void GenerateGrid()
    {
        for (float x = Mathf.Ceil(xMin); x <= Mathf.Floor(xMax); x++)
            CreateMeshLine(new Vector2(x, yMin), new Vector2(x, yMax), Mathf.Abs(x) < 0.01f ? axisColor : gridColor, GRID_Z, lineWidth, _elements);

        for (float y = Mathf.Ceil(yMin); y <= Mathf.Floor(yMax); y++)
            CreateMeshLine(new Vector2(xMin, y), new Vector2(xMax, y), Mathf.Abs(y) < 0.01f ? axisColor : gridColor, GRID_Z, lineWidth, _elements);
    }

    void DrawGoalPath()
    {
        if (!LevelPaths.ContainsKey(currentLevel)) return;
        Vector2[] points = LevelPaths[currentLevel];
        for (int i = 0; i < points.Length - 1; i++)
            CreateDashedLine(points[i], points[i+1], goalColor, GOAL_Z, lineWidth * 1.5f);
    }

    void CreateDashedLine(Vector2 start, Vector2 end, Color col, float zDepth, float width)
    {
        float dist = Vector2.Distance(start, end);
        Vector2 dir = (end - start).normalized;
        float currentDist = 0;
        while (currentDist < dist)
        {
            float step = Mathf.Min(dashSize, dist - currentDist);
            Vector2 dStart = start + dir * currentDist;
            Vector2 dEnd = dStart + dir * step;
            CreateMeshLine(dStart, dEnd, col, zDepth, width, _elements);
            currentDist += step * 2f; 
        }
    }

    void DrawTurtleTriangle()
    {
        GameObject turtleObj = new GameObject("TurtleTriangle");
        turtleObj.transform.SetParent(this.transform);
        turtleObj.transform.localPosition = Vector3.zero;
        turtleObj.transform.localRotation = Quaternion.identity;
        turtleObj.transform.localScale = Vector3.one;
        _elements.Add(turtleObj);

        // We use normalized here only for the visual orientation of the turtle's nose
        Vector2 dirLook = new Vector2(Directions[turtleDirection, 0], Directions[turtleDirection, 1]).normalized;
        Vector2 perp = new Vector2(-dirLook.y, dirLook.x); 

        Vector3 vTip = MathToLocal(turtlePosition + (dirLook * turtleLength));
        Vector3 vLeft = MathToLocal(turtlePosition - (perp * (turtleWidth * 0.5f)));
        Vector3 vRight = MathToLocal(turtlePosition + (perp * (turtleWidth * 0.5f)));
        vTip.z = vLeft.z = vRight.z = TURTLE_Z;

        Mesh mesh = new Mesh { vertices = new Vector3[] { vTip, vLeft, vRight }, triangles = new int[] { 0, 1, 2 } };
        turtleObj.AddComponent<MeshFilter>().mesh = mesh;
        turtleObj.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color")) { color = turtleColor };
    }

    public void AddTrailSegment(Vector2 start, Vector2 end, Color color)
    {
        CreateMeshLine(start, end, color, TRAIL_Z, lineWidth * 2f, _trailElements);
    }

    public Vector2 GetDirectionVector()
    {
        // NO NORMALIZATION: returns (1,1) for diagonals to ensure full grid-step travel
        return new Vector2(Directions[turtleDirection, 0], Directions[turtleDirection, 1]);
    }

    void CreateMeshLine(Vector2 start, Vector2 end, Color col, float zDepth, float width, List<GameObject> container)
    {
        GameObject lineObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        lineObj.transform.SetParent(this.transform);
        container.Add(lineObj);
        DestroyImmediate(lineObj.GetComponent<MeshCollider>());

        Vector3 posA = MathToLocal(start);
        Vector3 posB = MathToLocal(end);
        Vector3 diff = posB - posA;
        float dist = diff.magnitude;
        if (dist < 0.0001f) return;

        lineObj.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
        
        // Use transform.lossyScale to keep line width consistent regardless of quad stretching
        float scaleY = width / ((transform.lossyScale.x + transform.lossyScale.y) / 2f);
        lineObj.transform.localScale = new Vector3(dist, scaleY, 1f);
        lineObj.transform.localPosition = posA + (diff.normalized * (dist * 0.5f)) + new Vector3(0, 0, zDepth);

        lineObj.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color")) { color = col };
    }

    public Vector3 MathToLocal(Vector2 mathCoord)
    {
        float xNorm = (mathCoord.x - xMin) / (xMax - xMin);
        float yNorm = (mathCoord.y - yMin) / (yMax - yMin);
        return new Vector3(xNorm - 0.5f, yNorm - 0.5f, 0);
    }

    public Vector2[] GetCurrentLevelPath()
    {
        if (LevelPaths.ContainsKey(currentLevel))
            return LevelPaths[currentLevel];
        return new Vector2[0];
    }

    public void SetTrailColor(Color newColor)
    {
        foreach (var segment in _trailElements)
        {
            if (segment == null) continue;
            MeshRenderer mr = segment.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                // Update the material color directly
                mr.material.color = newColor;
            }
        }
    }
}