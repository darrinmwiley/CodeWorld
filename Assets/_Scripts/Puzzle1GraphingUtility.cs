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
    // Using negative values to bring elements closer to the camera
    public float gridZ = -0.01f;
    public float goalZ = -0.02f;
    public float equationLineZ = -0.03f;

    [Header("Visuals")]
    public Color gridColor = new Color(0.3f, 0.3f, 0.3f);
    public Color axisColor = Color.white;
    public float gridLineWidth = 0.02f;

    [Header("Puzzle Configuration")]
    public ItemSocket socketM;
    public ItemSocket socketB;
    public LineController lineM;
    public LineController lineB;

    [Header("Target Points")]
    public Vector2 targetPointA = new Vector2(-2, -2);
    public Vector2 targetPointB = new Vector2(3, 1);
    public float pointRadius = 0.3f;

    [Header("Equation Visuals")]
    public Color equationLineColor = Color.cyan;
    public float equationLineWidth = 0.05f;

    private List<GameObject> _equationLineElements = new List<GameObject>();

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
            DrawEquationLine();
        }
        else
        {
            ClearEquationLine();
        }
    }

    private bool IsInputReady()
    {
        return socketM != null && socketM.IsOccupied && 
               socketB != null && socketB.IsOccupied &&
               lineM != null && lineM.IsTransitionComplete && 
               lineB != null && lineB.IsTransitionComplete;
    }

    private void DrawGrid()
    {
        for (int i = (int)xMin; i <= xMax; i++)
        {
            Color col = (i == 0) ? axisColor : gridColor;
            AddLine(new Vector2(i, yMin), new Vector2(i, yMax), col, gridLineWidth, gridZ);
        }
        for (int i = (int)yMin; i <= yMax; i++)
        {
            Color col = (i == 0) ? axisColor : gridColor;
            AddLine(new Vector2(xMin, i), new Vector2(xMax, i), col, gridLineWidth, gridZ);
        }
    }

    private void DrawEquationLine()
    {
        if (_equationLineElements.Count > 0) return;

        float m = (float)GetValueFromSocket(socketM);
        float b = (float)GetValueFromSocket(socketB);

        float yAtXMin = m * xMin + b;
        float yAtXMax = m * xMax + b;

        GameObject line = AddLine(new Vector2(xMin, yAtXMin), new Vector2(xMax, yAtXMax), equationLineColor, equationLineWidth, equationLineZ);
        _equationLineElements.Add(line);
    }

    private GameObject AddLine(Vector2 start, Vector2 end, Color col, float width, float zDepth)
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
        
        // Force the material into the transparent render queue to ensure correct layering
        Material mat = new Material(Shader.Find("Unlit/Color")) { color = col };
        mat.renderQueue = 3000;
        lineObj.GetComponent<MeshRenderer>().material = mat;
        
        return lineObj;
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

    private void ClearEquationLine()
    {
        foreach (var element in _equationLineElements)
        {
            if (element != null) Destroy(element);
        }
        _equationLineElements.Clear();
    }
}