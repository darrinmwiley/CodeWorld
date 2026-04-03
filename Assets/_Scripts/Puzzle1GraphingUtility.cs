using UnityEngine;
using System.Collections.Generic;

public class Puzzle1GraphingUtility : MonoBehaviour
{
    [Header("Coordinate Bounds")]
    public float xMin = -5f;
    public float xMax = 5f;
    public float yMin = -5f;
    public float yMax = 5f;

    [Header("Visual Layers (Z-Depth)")]
    public float gridZ = -0.01f;
    public float goalZ = -0.02f;
    public float lineZ = -0.03f;

    [Header("Grid Visuals")]
    public Color gridColor = new Color(0.3f, 0.3f, 0.3f);
    public Color axisColor = Color.white;
    public float gridLineWidth = 0.02f;

    [Header("Puzzle Configuration")]
    public ItemSocket socketM;
    public ItemSocket socketB;
    public LineController lineM;
    public LineController lineB;

    [Header("Line Control")]
    public LineController resultLine; // The "Laser"
    public Transform pointStart;      // Drag child transform here
    public Transform pointEnd;        // Drag child transform here

    [Header("Target Points")]
    public Vector2 targetPointA = new Vector2(-2, -2);
    public Vector2 targetPointB = new Vector2(3, 3);
    public float pointRadius = 0.3f;

    public float CurrentM { get; private set; }
    public float CurrentB { get; private set; }
    public bool IsLaserActive => isFiring;

    private bool isFiring = false;

    void Start()
    {
        DrawGrid();
        DrawGoalDot(targetPointA);
        DrawGoalDot(targetPointB);
    }

    private void Update()
    {
        if (IsInputReady())
        {
            if (!isFiring) FireLaser();
        }
        else
        {
            ResetLaser();
        }
    }

    private bool IsInputReady()
    {
        return socketM != null && socketM.IsOccupied && 
               socketB != null && socketB.IsOccupied &&
               lineM != null && lineM.IsTransitionComplete && 
               lineB != null && lineB.IsTransitionComplete;
    }

    private void FireLaser()
    {
        isFiring = true;

        float m = (float)GetValueFromSocket(socketM);
        float b = (float)GetValueFromSocket(socketB);
        
        CurrentM = m;
        CurrentB = b;

        Vector2 start, end;
        if (CalculateClippedLine(m, b, out start, out end))
        {
            // Position the transforms that drive the LineController
            pointStart.localPosition = MathToLocal(start) + new Vector3(0, 0, lineZ);
            pointEnd.localPosition = MathToLocal(end) + new Vector3(0, 0, lineZ);
            
            resultLine.UpdateLineColors(Color.cyan);
            resultLine.RestartTransition();
        }
    }

    private void ResetLaser()
    {
        if (isFiring)
        {
            isFiring = false;
            resultLine.StopAndClearTransition();
        }
    }

    private bool CalculateClippedLine(float m, float b, out Vector2 start, out Vector2 end)
    {
        List<Vector2> points = new List<Vector2>();

        // Check intersections with all 4 boundaries to ensure the line stays within grid
        float yAtXMin = m * xMin + b;
        if (yAtXMin >= yMin && yAtXMin <= yMax) points.Add(new Vector2(xMin, yAtXMin));

        float yAtXMax = m * xMax + b;
        if (yAtXMax >= yMin && yAtXMax <= yMax) points.Add(new Vector2(xMax, yAtXMax));

        float xAtYMin = (yMin - b) / m;
        if (xAtYMin >= xMin && xAtYMin <= xMax) points.Add(new Vector2(xAtYMin, yMin));

        float xAtYMax = (yMax - b) / m;
        if (xAtYMax >= xMin && xAtYMax <= xMax) points.Add(new Vector2(xAtYMax, yMax));

        if (points.Count >= 2)
        {
            start = points[0];
            end = points[1];
            return true;
        }

        start = end = Vector2.zero;
        return false;
    }

    private void DrawGrid()
    {
        for (int i = (int)xMin; i <= xMax; i++)
            AddVisualLine(new Vector2(i, yMin), new Vector2(i, yMax), (i == 0) ? axisColor : gridColor, gridLineWidth, gridZ);
        for (int i = (int)yMin; i <= yMax; i++)
            AddVisualLine(new Vector2(xMin, i), new Vector2(xMax, i), (i == 0) ? axisColor : gridColor, gridLineWidth, gridZ);
    }

    private void AddVisualLine(Vector2 start, Vector2 end, Color col, float width, float zDepth)
    {
        GameObject lineObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        lineObj.transform.SetParent(this.transform);
        Vector3 posA = MathToLocal(start);
        Vector3 posB = MathToLocal(end);
        Vector3 diff = posB - posA;
        float dist = diff.magnitude;

        lineObj.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
        lineObj.transform.localScale = new Vector3(dist, width, 1f);
        lineObj.transform.localPosition = posA + (diff.normalized * (dist * 0.5f)) + new Vector3(0, 0, zDepth);
        lineObj.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color")) { color = col };
    }

    private void DrawGoalDot(Vector2 coord)
    {
        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.transform.SetParent(this.transform);
        if (dot.TryGetComponent<Collider>(out var col)) DestroyImmediate(col);
        dot.transform.localPosition = MathToLocal(coord) + new Vector3(0, 0, goalZ);
        dot.transform.localScale = Vector3.one * pointRadius;
        dot.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color")) { color = Color.red };
    }

    private Vector3 MathToLocal(Vector2 mathCoord)
    {
        float xNorm = (mathCoord.x - xMin) / (xMax - xMin);
        float yNorm = (mathCoord.y - yMin) / (yMax - yMin);
        return new Vector3(xNorm - 0.5f, yNorm - 0.5f, 0);
    }

    private int GetValueFromSocket(ItemSocket s)
    {
        GameObject item = s.GetSocketedItem();
        return (item != null && item.TryGetComponent<ItemValue>(out var iv)) ? iv.ToInt() : 0;
    }
}