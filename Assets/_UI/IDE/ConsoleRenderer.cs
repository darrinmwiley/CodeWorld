using UnityEngine;

public class ConsoleRenderer : MonoBehaviour
{
    [Header("Dependencies")]
    public ConsoleStateManager stateManager;
    public SyntaxHighlighter syntaxHighlighter;

    [Header("Prefabs")]
    public GameObject consoleCharPrefab;
    public GameObject cursorPrefab;

    [Header("Rendering Settings")]
    public string consoleRenderLayerName = "ConsoleUI";
    public bool lockGlyphPixelSize = true;
    public int glyphPixelScale = 3;
    public int glyphPixelWidth = 3;
    public int glyphPixelHeight = 5;

    [Header("Instance Offsetting")]
    public Vector3 perInstanceWorldOffset = Vector3.zero;
    public bool autoOffsetInstances = true;
    public float instancePaddingWorld = 10f;

    // Constants
    private const float CELL_X = 3f / 5f; // 0.6
    private const float CELL_Y = 1f;

    // Internal State
    public RenderTexture RenderTex { get; private set; }
    private GameObject[,] chars;
    private GameObject cursor;
    private Camera renderCamera;
    private GameObject charsGO;

    private static int s_instanceCounter = 0;
    private int _instanceId = -1;
    private int _consoleLayer = -1;

    public void Initialize()
    {
        _instanceId = s_instanceCounter++;
        _consoleLayer = LayerMask.NameToLayer(consoleRenderLayerName);
        if (_consoleLayer < 0) Debug.LogError($"Layer '{consoleRenderLayerName}' missing.");

        if (stateManager != null)
        {
            stateManager.OnStateChanged += UpdateConsoleVisuals;
        }
        GenerateGrid();
    }

    private void OnDestroy()
    {
        if (stateManager != null) stateManager.OnStateChanged -= UpdateConsoleVisuals;
        
        if (RenderTex != null)
        {
            // Unbind before cleanup
            if (renderCamera != null) renderCamera.targetTexture = null;
            
            RenderTex.Release();
            
            // Use Destroy in builds, DestroyImmediate in Editor-only scripts
            if (Application.isPlaying) Destroy(RenderTex);
            else DestroyImmediate(RenderTex);
        }
    }

    public void GenerateGrid()
    {
        if (stateManager == null) return;

        // --- GameObject Setup ---
        Transform charsTransform = transform.Find("chars");
        charsGO = charsTransform == null ? new GameObject("chars") : charsTransform.gameObject;
        
        Transform cameraTransform = transform.Find("camera");
        GameObject cameraGO = cameraTransform == null ? new GameObject("camera") : cameraTransform.gameObject;

        charsGO.transform.SetParent(transform, false);
        cameraGO.transform.SetParent(transform, false);

        // Clean up old grid
        for (int i = charsGO.transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(charsGO.transform.GetChild(i).gameObject);

        // --- Grid Positioning ---
        Vector3 instanceOffset = perInstanceWorldOffset;
        if (autoOffsetInstances) 
            instanceOffset += new Vector3(_instanceId * ((stateManager.viewportWidth * CELL_X) + instancePaddingWorld), 0f, 0f);
        
        charsGO.transform.position = transform.position + instanceOffset;

        chars = new GameObject[stateManager.viewportHeight, stateManager.viewportWidth];
        for (int i = 0; i < stateManager.viewportWidth; i++)
        {
            for (int j = 0; j < stateManager.viewportHeight; j++)
            {
                GameObject cube = Instantiate(consoleCharPrefab, Vector3.zero, Quaternion.identity);
                cube.transform.SetParent(charsGO.transform, false);
                cube.transform.localPosition = new Vector3((i * CELL_X) + .3f, (stateManager.viewportHeight - 1 - j) * CELL_Y + .5f, 0f);
                chars[j, i] = cube;
            }
        }

        if (cursor != null) DestroyImmediate(cursor);
        cursor = Instantiate(cursorPrefab, Vector3.zero, Quaternion.identity);
        cursor.transform.SetParent(charsGO.transform, false);
        cursor.transform.localPosition = new Vector3((3 * CELL_X) + .05f, (stateManager.viewportHeight - 1) * CELL_Y + .5f, -0.1f);

        if (_consoleLayer >= 0) SetLayerRecursively(charsGO, _consoleLayer);

        // --- Camera Component Setup ---
        renderCamera = cameraGO.GetComponent<Camera>();
        if (renderCamera == null)
        {
            renderCamera = cameraGO.AddComponent<Camera>();
        }

        if (_consoleLayer >= 0) renderCamera.cullingMask = 1 << _consoleLayer;
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = Color.black;
        renderCamera.orthographic = true;

        // Calculate Bounds
        float gridMinX = charsGO.transform.position.x;
        float gridMaxX = charsGO.transform.position.x + (stateManager.viewportWidth * CELL_X);
        float gridMinY = charsGO.transform.position.y;
        float gridMaxY = charsGO.transform.position.y + (stateManager.viewportHeight * CELL_Y);

        SetCamera(renderCamera, gridMinX, gridMaxY, gridMaxX, gridMinY);

        // --- CRITICAL FIX: RenderTexture Re-initialization ---
        if (RenderTex != null)
        {
            // 1. Unbind from camera first to prevent the warning
            if (renderCamera != null) renderCamera.targetTexture = null;
            
            // 2. Safely release and destroy
            RenderTex.Release();
            DestroyImmediate(RenderTex);
            RenderTex = null;
        }

        if (lockGlyphPixelSize)
        {
            int rtW = stateManager.viewportWidth * glyphPixelWidth * glyphPixelScale;
            int rtH = stateManager.viewportHeight * glyphPixelHeight * glyphPixelScale;
            RenderTex = CreateRenderTextureExact(renderCamera, rtW, rtH);
        }
        else
        {
            RenderTex = CreateRenderTextureExact(renderCamera, 1000, Mathf.RoundToInt(1000 / Mathf.Max(0.0001f, renderCamera.aspect)));
        }

        UpdateConsoleVisuals();
    }

    private void SetCamera(Camera cam, float tlx, float tly, float brx, float bry)
    {
        if (cam == null) return;

        float midX = (tlx + brx) / 2;
        float midY = (tly + bry) / 2;
        float width = Mathf.Abs(brx - tlx);
        float height = Mathf.Abs(bry - tly);

        cam.orthographicSize = height / 2f;
        cam.transform.position = new Vector3(midX, midY, -10);
        cam.aspect = width / height;
    }

    public void UpdateConsoleVisuals()
    {
        if (stateManager == null || chars == null) return;
        UpdateLineNumbers();
        UpdateLines();
        if (stateManager.isHighlighting) UpdateHighlight();
        UpdateCursor();
    }

    private void UpdateLines()
    {
        int padding = stateManager.GetLineCountPadding();
        for (int i = 0; i < stateManager.viewportHeight; i++)
        {
            for (int j = padding; j < stateManager.viewportWidth; j++)
            {
                SetCellColor(i, j, Color.black);
                SetCellTextColor(i, j, Color.white);
                int lineNumber = i + stateManager.verticalScroll;
                if (stateManager.lines.Count > lineNumber && stateManager.lines[lineNumber].Length > j - padding + stateManager.horizontalScroll)
                {
                    SetChar(i, j, stateManager.lines[lineNumber][j - padding + stateManager.horizontalScroll]);
                }
                else
                {
                    SetChar(i, j, ' ');
                }
            }
        }
        
        // CORRECTION: Pass the renderer itself to the syntax highlighter
        if (syntaxHighlighter != null) syntaxHighlighter.Highlight(this);
    }

    private void UpdateLineNumbers()
    {
        int totalLineNumberLength = ("" + (stateManager.lines.Count)).Length + 2;
        for (int i = 0; i < stateManager.viewportHeight; i++)
        {
            if (stateManager.verticalScroll + i < stateManager.lines.Count)
            {
                string lineNumber = (stateManager.verticalScroll + i + 1 + "");
                for (int j = 0; j < totalLineNumberLength; j++)
                {
                    SetCellColor(i, j, new Color(.15f, .15f, .15f));
                    SetChar(i, j, ' ');
                }
                for (int j = 0; j < lineNumber.Length; j++)
                {
                    int currentCol = totalLineNumberLength - 2 - j;
                    char currentChar = lineNumber[lineNumber.Length - 1 - j];
                    SetChar(i, currentCol, currentChar);
                }
            }
            else
            {
                for (int j = 0; j < totalLineNumberLength; j++)
                {
                    SetCellColor(i, j, Color.black);
                    SetChar(i, j, ' ');
                }
            }
        }
    }

    private void UpdateCursor()
    {
        int trueCursorCol = stateManager.visibleCursorCol + stateManager.GetLineCountPadding() - stateManager.horizontalScroll;
        cursor.transform.localPosition = new Vector3(
            (trueCursorCol * CELL_X) + .05f,
            (stateManager.verticalScroll + stateManager.viewportHeight - 1 - stateManager.cursorRow) * CELL_Y + .5f,
            -0.1f
        );
    }

    private void UpdateHighlight()
    {
        int r1 = stateManager.dragStart.x, c1 = stateManager.dragStart.y;
        int r2 = stateManager.dragCurrent.x, c2 = stateManager.dragCurrent.y;
        if (r2 < r1 || (r2 == r1 && c2 < c1))
        {
            r1 = stateManager.dragCurrent.x; c1 = stateManager.dragCurrent.y;
            r2 = stateManager.dragStart.x; c2 = stateManager.dragStart.y;
        }

        int r = r1, c = c1;
        bool done = false;
        while (!done && (r != r2 || c != c2))
        {
            if (r > r2) break;
            if (r < stateManager.lines.Count && c <= stateManager.lines[r].Length)
            {
                int viewportR = r - stateManager.verticalScroll;
                int viewportC = c - stateManager.horizontalScroll + stateManager.GetLineCountPadding();
                if (viewportR >= 0 && viewportR < stateManager.viewportHeight && viewportC >= stateManager.GetLineCountPadding() && viewportC < stateManager.viewportWidth)
                {
                    SetCellColor(viewportR, viewportC, Color.white);
                    SetCellTextColor(viewportR, viewportC, Color.black);
                }
            }
            if (c <= stateManager.lines[r].Length - 1) c++;
            else
            {
                if (r == r2 && c == c2 - 1) { done = true; break; }
                c = 0; r++;
            }
        }
    }

    private RenderTexture CreateRenderTextureExact(Camera cam, int w, int h)
    {
        RenderTexture rt = new RenderTexture(Mathf.Max(1, w), Mathf.Max(1, h), 24);
        rt.name = "ConsoleRenderTexture";
        rt.antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
        rt.filterMode = FilterMode.Bilinear;
        rt.wrapMode = TextureWrapMode.Clamp;
        cam.targetTexture = rt;
        return rt;
    }

    private void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
    }

    public void SetChar(int r, int c, char ch) => chars[r, c].GetComponent<ConsoleCharController>().UpdateText(ch + "");
    public void SetCellColor(int r, int c, Color color) => chars[r, c].GetComponent<ConsoleCharController>().UpdateColor(color);
    public void SetCellTextColor(int r, int c, Color color) => chars[r, c].GetComponent<ConsoleCharController>().UpdateTextColor(color);
}