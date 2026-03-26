using UnityEngine;

public class ConsoleRenderer : MonoBehaviour
{
    [Header("Dependencies")]
    public ConsoleStateManager stateManager;
    public SyntaxHighlighter syntaxHighlighter;

    [Header("Prefabs")]
    public GameObject consoleCharPrefab;
    public GameObject cursorPrefab;
    public GameObject lockGlyphPrefab;

    [Header("Rendering Settings")]
    public string consoleRenderLayerName = "ConsoleUI";
    public bool showLockGlyphColumn = false;
    public bool lockGlyphPixelSize = true;
    public int glyphPixelScale = 3;
    public int glyphPixelWidth = 3;
    public int glyphPixelHeight = 5;
    [Min(1)] public int renderTextureResolutionMultiplier = 4;

    [Header("Instance Offsetting")]
    public Vector3 perInstanceWorldOffset = Vector3.zero;
    public bool autoOffsetInstances = true;
    public float instancePaddingWorld = 10f;

    // Constants
    private const float CELL_X = 3f / 5f; // 0.6
    private const float CELL_Y = 1f;

    // Internal State
    public RenderTexture RenderTex { get; private set; }
    public int DisplayTextureWidth { get; private set; }
    public int DisplayTextureHeight { get; private set; }

    private GameObject[,] chars;
    private GameObject[] lockGlyphs;
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

        SyncStatePaddingSettings();

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
            if (renderCamera != null) renderCamera.targetTexture = null;

            RenderTex.Release();

            if (Application.isPlaying) Destroy(RenderTex);
            else DestroyImmediate(RenderTex);
        }
    }

    public void GenerateGrid()
    {
        if (stateManager == null) return;

        SyncStatePaddingSettings();

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
        for (int col = 0; col < stateManager.viewportWidth; col++)
        {
            for (int row = 0; row < stateManager.viewportHeight; row++)
            {
                GameObject glyph = Instantiate(consoleCharPrefab, Vector3.zero, Quaternion.identity);
                glyph.transform.SetParent(charsGO.transform, false);
                glyph.transform.localPosition = GetCellLocalPosition(col, row, 0f);
                chars[row, col] = glyph;
            }
        }

        lockGlyphs = showLockGlyphColumn ? new GameObject[stateManager.viewportHeight] : null;
        if (showLockGlyphColumn)
        {
            if (lockGlyphPrefab == null)
            {
                Debug.LogWarning("ConsoleRenderer: showLockGlyphColumn is enabled, but lockGlyphPrefab is not assigned.");
            }
            else
            {
                for (int row = 0; row < stateManager.viewportHeight; row++)
                {
                    GameObject lockGlyph = Instantiate(lockGlyphPrefab, Vector3.zero, Quaternion.identity);
                    lockGlyph.transform.SetParent(charsGO.transform, false);
                    lockGlyph.transform.localPosition = GetCellLocalPosition(0, row, -0.05f);
                    lockGlyphs[row] = lockGlyph;
                }
            }
        }

        if (cursor != null) DestroyImmediate(cursor);
        cursor = Instantiate(cursorPrefab, Vector3.zero, Quaternion.identity);
        cursor.transform.SetParent(charsGO.transform, false);
        cursor.transform.localPosition = GetCellLocalPosition(3, 0, -0.1f);

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

        // --- RenderTexture Re-initialization ---
        if (RenderTex != null)
        {
            if (renderCamera != null) renderCamera.targetTexture = null;

            RenderTex.Release();
            DestroyImmediate(RenderTex);
            RenderTex = null;
        }

        // DisplayTextureWidth/Height are the logical on-screen size.
        // The actual RT can be larger for more detail while still being displayed at the same size.
        if (lockGlyphPixelSize)
        {
            DisplayTextureWidth = Mathf.Max(1, stateManager.viewportWidth * glyphPixelWidth * glyphPixelScale);
            DisplayTextureHeight = Mathf.Max(1, stateManager.viewportHeight * glyphPixelHeight * glyphPixelScale);
        }
        else
        {
            DisplayTextureWidth = 1000;
            DisplayTextureHeight = Mathf.Max(1, Mathf.RoundToInt(1000 / Mathf.Max(0.0001f, renderCamera.aspect)));
        }

        int actualTextureWidth = Mathf.Max(1, DisplayTextureWidth * Mathf.Max(1, renderTextureResolutionMultiplier));
        int actualTextureHeight = Mathf.Max(1, DisplayTextureHeight * Mathf.Max(1, renderTextureResolutionMultiplier));
        RenderTex = CreateRenderTextureExact(renderCamera, actualTextureWidth, actualTextureHeight);

        UpdateConsoleVisuals();
    }

    private void SyncStatePaddingSettings()
    {
        if (stateManager == null) return;
        stateManager.extraLeftPaddingColumns = showLockGlyphColumn ? 1 : 0;
    }

    private Vector3 GetCellLocalPosition(int col, int row, float z)
    {
        return new Vector3(
            (col * CELL_X) + .3f,
            (stateManager.viewportHeight - 1 - row) * CELL_Y + .5f,
            z
        );
    }

    private void SetCamera(Camera cam, float tlx, float tly, float brx, float bry)
    {
        if (cam == null) return;

        float midX = (tlx + brx) / 2f;
        float midY = (tly + bry) / 2f;
        float width = Mathf.Abs(brx - tlx);
        float height = Mathf.Abs(bry - tly);

        cam.orthographicSize = height / 2f;
        cam.transform.position = new Vector3(midX, midY, -10f);
        cam.aspect = width / height;
    }

    public void UpdateConsoleVisuals()
    {
        if (stateManager == null || chars == null) return;

        SyncStatePaddingSettings();

        UpdateLockGlyphs();
        UpdateLineNumbers();
        UpdateLines();

        if (stateManager.isHighlighting) UpdateHighlight();

        UpdateCursor();
    }

    private void UpdateLockGlyphs()
    {
        if (!showLockGlyphColumn)
        {
            if (lockGlyphs != null)
            {
                for (int row = 0; row < lockGlyphs.Length; row++)
                    if (lockGlyphs[row] != null) lockGlyphs[row].SetActive(false);
            }
            return;
        }

        for (int row = 0; row < stateManager.viewportHeight; row++)
        {
            bool hasLine = stateManager.verticalScroll + row < stateManager.lines.Count;
            SetCellColor(row, 0, hasLine ? new Color(.15f, .15f, .15f) : Color.black);
            SetCellTextColor(row, 0, Color.white);
            SetChar(row, 0, ' ');

            if (lockGlyphs != null && row < lockGlyphs.Length && lockGlyphs[row] != null)
            {
                lockGlyphs[row].SetActive(hasLine);
                if (hasLine) lockGlyphs[row].transform.localPosition = GetCellLocalPosition(0, row, -0.05f);
            }
        }
    }

    private void UpdateLines()
    {
        int padding = stateManager.GetLineCountPadding();
        for (int row = 0; row < stateManager.viewportHeight; row++)
        {
            for (int col = padding; col < stateManager.viewportWidth; col++)
            {
                SetCellColor(row, col, Color.black);
                SetCellTextColor(row, col, Color.white);

                int lineNumber = row + stateManager.verticalScroll;
                int contentColumn = col - padding + stateManager.horizontalScroll;
                if (stateManager.lines.Count > lineNumber && stateManager.lines[lineNumber].Length > contentColumn)
                {
                    SetChar(row, col, stateManager.lines[lineNumber][contentColumn]);
                }
                else
                {
                    SetChar(row, col, ' ');
                }
            }
        }

        if (syntaxHighlighter != null) syntaxHighlighter.Highlight(this);
    }

    private void UpdateLineNumbers()
    {
        int leftGutterPadding = stateManager.GetLeftGutterPadding();
        int totalLineNumberLength = stateManager.GetLineNumberPadding();

        if (totalLineNumberLength <= 0) return;

        for (int row = 0; row < stateManager.viewportHeight; row++)
        {
            bool hasLine = stateManager.verticalScroll + row < stateManager.lines.Count;

            for (int offset = 0; offset < totalLineNumberLength; offset++)
            {
                int col = leftGutterPadding + offset;
                SetCellColor(row, col, hasLine ? new Color(.15f, .15f, .15f) : Color.black);
                SetCellTextColor(row, col, Color.white);
                SetChar(row, col, ' ');
            }

            if (!hasLine) continue;

            string lineNumber = (stateManager.verticalScroll + row + 1).ToString();
            for (int digitIndex = 0; digitIndex < lineNumber.Length; digitIndex++)
            {
                int currentCol = leftGutterPadding + totalLineNumberLength - 2 - digitIndex;
                char currentChar = lineNumber[lineNumber.Length - 1 - digitIndex];
                SetChar(row, currentCol, currentChar);
            }
        }
    }

    private void UpdateCursor()
    {
        if (cursor == null) return;

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
                if (r == r2 && c == c2 - 1)
                {
                    done = true;
                    break;
                }

                c = 0;
                r++;
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
        rt.useMipMap = false;
        rt.autoGenerateMips = false;
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