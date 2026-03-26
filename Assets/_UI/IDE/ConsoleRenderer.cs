
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public Color cursorColor = Color.white;
    public bool useProceduralCursorMask = true;
    public bool cursorBlinkEnabled = true;
    [Min(0.05f)] public float cursorBlinkInterval = 0.5f;

    [Header("Instance Offsetting")]
    public Vector3 perInstanceWorldOffset = Vector3.zero;
    public bool autoOffsetInstances = true;
    public float instancePaddingWorld = 10f;

    private const float CELL_X = 3f / 5f;
    private const float CELL_Y = 1f;

    private static readonly Color32 COLOR_BLACK = new Color32(0, 0, 0, 255);
    private static readonly Color32 COLOR_WHITE = new Color32(255, 255, 255, 255);
    private const char LOCK_GLYPH_SENTINEL = '\u0001';

    public RenderTexture RenderTex { get; private set; }
    public int DisplayTextureWidth { get; private set; }
    public int DisplayTextureHeight { get; private set; }

    private char[] _cellChars;
    private Color32[] _cellBackgroundColors;
    private Color32[] _cellTextColors;
    private bool[] _lockGlyphVisibleByRow;

    private Texture2D _compositeTexture;
    private Color32[] _pixelBuffer;
    private bool _cursorBlinkVisible = true;
    private float _nextCursorBlinkTime;

    private readonly Dictionary<char, GlyphMask> _glyphMasks = new Dictionary<char, GlyphMask>();
    private GlyphMask _spaceMask;
    private GlyphMask _lockGlyphMask;
    private GlyphMask _cursorMask;
    private bool _glyphCacheReady;

    private int _cellDisplayWidth;
    private int _cellDisplayHeight;
    private int _cellActualWidth;
    private int _cellActualHeight;

    private int _lastBakedCellWidth = -1;
    private int _lastBakedCellHeight = -1;

    private int _consoleLayer = -1;

    private GameObject _bakeRoot;
    private Camera _bakeCamera;
    private RenderTexture _bakeRenderTexture;
    private Texture2D _bakeReadbackTexture;

    private struct GlyphMask
    {
        public int width;
        public int height;
        public byte[] positive;
        public byte[] negative;

        public bool IsValid => positive != null && negative != null && positive.Length == width * height && negative.Length == width * height;
    }

    public void Initialize()
    {
        _consoleLayer = LayerMask.NameToLayer(consoleRenderLayerName);
        if (_consoleLayer < 0)
        {
            Debug.LogWarning($"ConsoleRenderer: Layer '{consoleRenderLayerName}' missing. Using Default layer for glyph baking.");
            _consoleLayer = 0;
        }

        SyncStatePaddingSettings();

        if (stateManager != null)
            stateManager.OnStateChanged += UpdateConsoleVisuals;

        ResetCursorBlink(true);
        GenerateGrid();
    }

    private void OnDestroy()
    {
        if (stateManager != null)
            stateManager.OnStateChanged -= UpdateConsoleVisuals;

        ReleaseRenderResources();
        ReleaseBakeResources();
        _glyphMasks.Clear();
        _glyphCacheReady = false;
    }

    private void Update()
    {
        if (!cursorBlinkEnabled || RenderTex == null || !_glyphCacheReady || stateManager == null)
            return;

        if (Time.unscaledTime < _nextCursorBlinkTime)
            return;

        _cursorBlinkVisible = !_cursorBlinkVisible;
        _nextCursorBlinkTime = Time.unscaledTime + Mathf.Max(0.05f, cursorBlinkInterval);
        ComposeRenderTexture();
    }

    public void GenerateGrid()
    {
        if (stateManager == null)
            return;

        SyncStatePaddingSettings();
        CalculateTextureSizing();
        EnsureCellStateBuffers();
        EnsureCompositeTexture();
        EnsureGlyphCacheBuilt();
        RecreateRenderTexture();
        UpdateConsoleVisuals();
    }

    private void CalculateTextureSizing()
    {
        int resolutionMultiplier = Mathf.Max(1, renderTextureResolutionMultiplier);

        if (lockGlyphPixelSize)
        {
            _cellDisplayWidth = Mathf.Max(1, glyphPixelWidth * glyphPixelScale);
            _cellDisplayHeight = Mathf.Max(1, glyphPixelHeight * glyphPixelScale);
        }
        else
        {
            _cellDisplayWidth = Mathf.Max(1, Mathf.CeilToInt(1000f / Mathf.Max(1, stateManager.viewportWidth)));
            _cellDisplayHeight = Mathf.Max(1, Mathf.CeilToInt(_cellDisplayWidth * (CELL_Y / CELL_X)));
        }

        _cellActualWidth = Mathf.Max(1, _cellDisplayWidth * resolutionMultiplier);
        _cellActualHeight = Mathf.Max(1, _cellDisplayHeight * resolutionMultiplier);

        DisplayTextureWidth = Mathf.Max(1, stateManager.viewportWidth * _cellDisplayWidth);
        DisplayTextureHeight = Mathf.Max(1, stateManager.viewportHeight * _cellDisplayHeight);
    }

    private void EnsureCellStateBuffers()
    {
        int cellCount = Mathf.Max(1, stateManager.viewportWidth * stateManager.viewportHeight);

        if (_cellChars == null || _cellChars.Length != cellCount)
            _cellChars = new char[cellCount];

        if (_cellBackgroundColors == null || _cellBackgroundColors.Length != cellCount)
            _cellBackgroundColors = new Color32[cellCount];

        if (_cellTextColors == null || _cellTextColors.Length != cellCount)
            _cellTextColors = new Color32[cellCount];

        if (_lockGlyphVisibleByRow == null || _lockGlyphVisibleByRow.Length != stateManager.viewportHeight)
            _lockGlyphVisibleByRow = new bool[stateManager.viewportHeight];
    }

    private void EnsureCompositeTexture()
    {
        int actualWidth = Mathf.Max(1, stateManager.viewportWidth * _cellActualWidth);
        int actualHeight = Mathf.Max(1, stateManager.viewportHeight * _cellActualHeight);

        bool needsTexture =
            _compositeTexture == null ||
            _compositeTexture.width != actualWidth ||
            _compositeTexture.height != actualHeight;

        if (needsTexture)
        {
            if (_compositeTexture != null)
            {
                if (Application.isPlaying) Destroy(_compositeTexture);
                else DestroyImmediate(_compositeTexture);
            }

            _compositeTexture = new Texture2D(actualWidth, actualHeight, TextureFormat.RGBA32, false, false);
            _compositeTexture.name = "ConsoleCompositeTexture";
            _compositeTexture.filterMode = FilterMode.Bilinear;
            _compositeTexture.wrapMode = TextureWrapMode.Clamp;
        }

        int pixelCount = actualWidth * actualHeight;
        if (_pixelBuffer == null || _pixelBuffer.Length != pixelCount)
            _pixelBuffer = new Color32[pixelCount];
    }

    private void RecreateRenderTexture()
    {
        int actualWidth = Mathf.Max(1, stateManager.viewportWidth * _cellActualWidth);
        int actualHeight = Mathf.Max(1, stateManager.viewportHeight * _cellActualHeight);

        bool needsRecreate =
            RenderTex == null ||
            RenderTex.width != actualWidth ||
            RenderTex.height != actualHeight;

        if (!needsRecreate)
            return;

        if (RenderTex != null)
        {
            RenderTex.Release();
            if (Application.isPlaying) Destroy(RenderTex);
            else DestroyImmediate(RenderTex);
            RenderTex = null;
        }

        RenderTex = new RenderTexture(actualWidth, actualHeight, 0, RenderTextureFormat.ARGB32);
        RenderTex.name = "ConsoleRenderTexture";
        RenderTex.antiAliasing = 1;
        RenderTex.filterMode = FilterMode.Bilinear;
        RenderTex.wrapMode = TextureWrapMode.Clamp;
        RenderTex.useMipMap = false;
        RenderTex.autoGenerateMips = false;
        RenderTex.Create();
    }

    private void ReleaseRenderResources()
    {
        if (RenderTex != null)
        {
            RenderTex.Release();
            if (Application.isPlaying) Destroy(RenderTex);
            else DestroyImmediate(RenderTex);
            RenderTex = null;
        }

        if (_compositeTexture != null)
        {
            if (Application.isPlaying) Destroy(_compositeTexture);
            else DestroyImmediate(_compositeTexture);
            _compositeTexture = null;
        }
    }

    private void ReleaseBakeResources()
    {
        if (_bakeRenderTexture != null)
        {
            _bakeRenderTexture.Release();
            if (Application.isPlaying) Destroy(_bakeRenderTexture);
            else DestroyImmediate(_bakeRenderTexture);
            _bakeRenderTexture = null;
        }

        if (_bakeReadbackTexture != null)
        {
            if (Application.isPlaying) Destroy(_bakeReadbackTexture);
            else DestroyImmediate(_bakeReadbackTexture);
            _bakeReadbackTexture = null;
        }

        if (_bakeRoot != null)
        {
            if (Application.isPlaying) Destroy(_bakeRoot);
            else DestroyImmediate(_bakeRoot);
            _bakeRoot = null;
            _bakeCamera = null;
        }
    }

    private void EnsureGlyphCacheBuilt()
    {
        if (_glyphCacheReady && _lastBakedCellWidth == _cellActualWidth && _lastBakedCellHeight == _cellActualHeight)
            return;

        ReleaseBakeResources();
        _glyphMasks.Clear();
        _spaceMask = default;
        _lockGlyphMask = default;
        _cursorMask = default;
        _glyphCacheReady = false;

        if (consoleCharPrefab == null)
        {
            Debug.LogError("ConsoleRenderer: consoleCharPrefab is required for glyph mask baking.");
            return;
        }

        SetupBakeScene();

        GameObject glyphGO = Instantiate(consoleCharPrefab, _bakeRoot.transform, false);
        glyphGO.name = "GlyphBakeSource";
        glyphGO.transform.localPosition = new Vector3(CELL_X * 0.5f, CELL_Y * 0.5f, 0f);
        SetLayerRecursively(glyphGO, _consoleLayer);

        ConsoleCharController glyphController = glyphGO.GetComponent<ConsoleCharController>();
        if (glyphController == null)
        {
            Debug.LogError("ConsoleRenderer: consoleCharPrefab must have a ConsoleCharController for glyph mask baking.");
            if (Application.isPlaying) Destroy(glyphGO);
            else DestroyImmediate(glyphGO);
            return;
        }

        glyphController.UpdateColor(Color.black);
        glyphController.UpdateTextColor(Color.white);

        for (int c = 32; c <= 126; c++)
        {
            char ch = (char)c;
            if (ch == ' ')
            {
                _glyphMasks[ch] = CreateEmptyMask();
                continue;
            }

            glyphController.UpdateText(ch.ToString());
            ForceBakeUIRefresh();
            _glyphMasks[ch] = CaptureMaskFromCurrentBakeScene();
        }

        _spaceMask = CreateEmptyMask();
        _glyphMasks[' '] = _spaceMask;

        if (Application.isPlaying) Destroy(glyphGO);
        else DestroyImmediate(glyphGO);

        _lockGlyphMask = BakeSpecialPrefab(lockGlyphPrefab, "LockGlyphBakeSource", CreateEmptyMask(), false);

        GlyphMask defaultCursorMask = CreateDefaultCursorMask();
        _cursorMask = useProceduralCursorMask
            ? defaultCursorMask
            : BakeSpecialPrefab(cursorPrefab, "CursorBakeSource", defaultCursorMask, true);

        _lastBakedCellWidth = _cellActualWidth;
        _lastBakedCellHeight = _cellActualHeight;
        _glyphCacheReady = true;
    }

    private void SetupBakeScene()
    {
        _bakeRoot = new GameObject("ConsoleRenderer_BakeRoot");
        _bakeRoot.hideFlags = HideFlags.HideAndDontSave;
        _bakeRoot.transform.SetParent(transform, false);
        _bakeRoot.transform.localPosition = new Vector3(0f, 0f, -5000f);

        GameObject cameraGO = new GameObject("ConsoleRenderer_BakeCamera");
        cameraGO.hideFlags = HideFlags.HideAndDontSave;
        cameraGO.transform.SetParent(_bakeRoot.transform, false);

        _bakeCamera = cameraGO.AddComponent<Camera>();
        _bakeCamera.enabled = false;
        _bakeCamera.clearFlags = CameraClearFlags.SolidColor;
        _bakeCamera.backgroundColor = Color.black;
        _bakeCamera.orthographic = true;
        _bakeCamera.orthographicSize = CELL_Y * 0.5f;
        _bakeCamera.aspect = (float)_cellActualWidth / Mathf.Max(1, _cellActualHeight);
        _bakeCamera.cullingMask = 1 << _consoleLayer;
        _bakeCamera.nearClipPlane = 0.01f;
        _bakeCamera.farClipPlane = 100f;
        _bakeCamera.transform.localPosition = new Vector3(CELL_X * 0.5f, CELL_Y * 0.5f, -10f);

        _bakeRenderTexture = new RenderTexture(_cellActualWidth, _cellActualHeight, 24, RenderTextureFormat.ARGB32);
        _bakeRenderTexture.hideFlags = HideFlags.HideAndDontSave;
        _bakeRenderTexture.filterMode = FilterMode.Bilinear;
        _bakeRenderTexture.wrapMode = TextureWrapMode.Clamp;
        _bakeRenderTexture.useMipMap = false;
        _bakeRenderTexture.autoGenerateMips = false;
        _bakeRenderTexture.antiAliasing = 1;
        _bakeRenderTexture.Create();

        _bakeReadbackTexture = new Texture2D(_cellActualWidth, _cellActualHeight, TextureFormat.RGBA32, false, false);
        _bakeReadbackTexture.hideFlags = HideFlags.HideAndDontSave;
        _bakeReadbackTexture.filterMode = FilterMode.Bilinear;
        _bakeReadbackTexture.wrapMode = TextureWrapMode.Clamp;

        _bakeCamera.targetTexture = _bakeRenderTexture;
    }

    private GlyphMask BakeSpecialPrefab(GameObject prefab, string tempName, GlyphMask fallback, bool suppressTextMeshes)
    {
        if (prefab == null)
            return fallback;

        GameObject temp = Instantiate(prefab, _bakeRoot.transform, false);
        temp.name = tempName;
        temp.transform.localPosition = new Vector3(CELL_X * 0.5f, CELL_Y * 0.5f, 0f);
        SetLayerRecursively(temp, _consoleLayer);

        ConsoleCharController controller = temp.GetComponent<ConsoleCharController>();
        if (controller != null)
        {
            controller.UpdateColor(Color.black);
            controller.UpdateTextColor(Color.white);
        }

        if (suppressTextMeshes)
        {
            TMP_Text[] textMeshes = temp.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < textMeshes.Length; i++)
            {
                textMeshes[i].text = string.Empty;
            }
        }

        ForceBakeUIRefresh();
        GlyphMask baked = CaptureMaskFromCurrentBakeScene();

        if (Application.isPlaying) Destroy(temp);
        else DestroyImmediate(temp);

        return baked.IsValid && HasAnyPositivePixels(baked) ? baked : fallback;
    }

    private bool HasAnyPositivePixels(GlyphMask mask)
    {
        if (!mask.IsValid || mask.positive == null)
            return false;

        for (int i = 0; i < mask.positive.Length; i++)
        {
            if (mask.positive[i] != 0)
                return true;
        }

        return false;
    }

    private void ResetCursorBlink(bool makeVisible)
    {
        _cursorBlinkVisible = makeVisible || !cursorBlinkEnabled;
        _nextCursorBlinkTime = Time.unscaledTime + Mathf.Max(0.05f, cursorBlinkInterval);
    }

    private void ForceBakeUIRefresh()
    {
        Canvas.ForceUpdateCanvases();
    }

    private GlyphMask CaptureMaskFromCurrentBakeScene()
    {
        if (_bakeCamera == null || _bakeRenderTexture == null || _bakeReadbackTexture == null)
            return CreateEmptyMask();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _bakeRenderTexture;

        _bakeCamera.Render();
        _bakeReadbackTexture.ReadPixels(new Rect(0, 0, _cellActualWidth, _cellActualHeight), 0, 0, false);
        _bakeReadbackTexture.Apply(false, false);

        Color32[] pixels = _bakeReadbackTexture.GetPixels32();
        byte[] positive = new byte[pixels.Length];
        byte[] negative = new byte[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 px = pixels[i];
            int luminance = px.r + px.g + px.b;
            bool on = px.a > 127 && luminance > 64;
            positive[i] = on ? (byte)255 : (byte)0;
            negative[i] = on ? (byte)0 : (byte)255;
        }

        RenderTexture.active = previous;

        return new GlyphMask
        {
            width = _cellActualWidth,
            height = _cellActualHeight,
            positive = positive,
            negative = negative
        };
    }

    private GlyphMask CreateEmptyMask()
    {
        int size = Mathf.Max(1, _cellActualWidth * _cellActualHeight);
        byte[] positive = new byte[size];
        byte[] negative = new byte[size];
        for (int i = 0; i < size; i++)
            negative[i] = 255;

        return new GlyphMask
        {
            width = _cellActualWidth,
            height = _cellActualHeight,
            positive = positive,
            negative = negative
        };
    }

    private GlyphMask CreateDefaultCursorMask()
    {
        int size = Mathf.Max(1, _cellActualWidth * _cellActualHeight);
        byte[] positive = new byte[size];
        byte[] negative = new byte[size];

        int barWidth = Mathf.Clamp(_cellActualWidth / 8, 1, Mathf.Max(1, _cellActualWidth));
        for (int y = 0; y < _cellActualHeight; y++)
        {
            int rowOffset = y * _cellActualWidth;
            for (int x = 0; x < _cellActualWidth; x++)
            {
                bool on = x < barWidth;
                int index = rowOffset + x;
                positive[index] = on ? (byte)255 : (byte)0;
                negative[index] = on ? (byte)0 : (byte)255;
            }
        }

        return new GlyphMask
        {
            width = _cellActualWidth,
            height = _cellActualHeight,
            positive = positive,
            negative = negative
        };
    }

    private void SyncStatePaddingSettings()
    {
        if (stateManager == null)
            return;

        stateManager.extraLeftPaddingColumns = showLockGlyphColumn ? 1 : 0;
    }

    public void UpdateConsoleVisuals()
    {
        if (stateManager == null || _cellChars == null || !_glyphCacheReady)
            return;

        SyncStatePaddingSettings();
        ResetCellStateBuffers();
        UpdateLockGlyphs();
        UpdateLineNumbers();
        UpdateLines();

        if (stateManager.isHighlighting)
            UpdateHighlight();

        ResetCursorBlink(true);
        ComposeRenderTexture();
    }

    private void ResetCellStateBuffers()
    {
        for (int i = 0; i < _cellChars.Length; i++)
        {
            _cellChars[i] = ' ';
            _cellBackgroundColors[i] = COLOR_BLACK;
            _cellTextColors[i] = COLOR_WHITE;
        }

        for (int row = 0; row < _lockGlyphVisibleByRow.Length; row++)
            _lockGlyphVisibleByRow[row] = false;
    }

    private void UpdateLockGlyphs()
    {
        if (!showLockGlyphColumn)
            return;

        for (int row = 0; row < stateManager.viewportHeight; row++)
        {
            bool hasLine = stateManager.verticalScroll + row < stateManager.lines.Count;
            SetCellColor(row, 0, hasLine ? new Color(.15f, .15f, .15f) : Color.black);
            SetCellTextColor(row, 0, Color.white);
            SetChar(row, 0, hasLine ? LOCK_GLYPH_SENTINEL : ' ');
            _lockGlyphVisibleByRow[row] = hasLine;
        }
    }

    private void UpdateLines()
    {
        int padding = stateManager.GetLineCountPadding();

        for (int row = 0; row < stateManager.viewportHeight; row++)
        {
            int lineNumber = row + stateManager.verticalScroll;

            for (int col = padding; col < stateManager.viewportWidth; col++)
            {
                SetCellColor(row, col, Color.black);
                SetCellTextColor(row, col, Color.white);

                int contentColumn = col - padding + stateManager.horizontalScroll;
                if (lineNumber >= 0 &&
                    lineNumber < stateManager.lines.Count &&
                    contentColumn >= 0 &&
                    contentColumn < stateManager.lines[lineNumber].Length)
                {
                    SetChar(row, col, stateManager.lines[lineNumber][contentColumn]);
                }
                else
                {
                    SetChar(row, col, ' ');
                }
            }
        }

        if (syntaxHighlighter != null)
            syntaxHighlighter.Highlight(this);
    }

    private void UpdateLineNumbers()
    {
        int leftGutterPadding = stateManager.GetLeftGutterPadding();
        int totalLineNumberLength = stateManager.GetLineNumberPadding();

        if (totalLineNumberLength <= 0)
            return;

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

            if (!hasLine)
                continue;

            string lineNumber = (stateManager.verticalScroll + row + 1).ToString();
            for (int digitIndex = 0; digitIndex < lineNumber.Length; digitIndex++)
            {
                int currentCol = leftGutterPadding + totalLineNumberLength - 2 - digitIndex;
                char currentChar = lineNumber[lineNumber.Length - 1 - digitIndex];
                SetChar(row, currentCol, currentChar);
            }
        }
    }

    private void UpdateHighlight()
    {
        int r1 = stateManager.dragStart.x;
        int c1 = stateManager.dragStart.y;
        int r2 = stateManager.dragCurrent.x;
        int c2 = stateManager.dragCurrent.y;

        if (r2 < r1 || (r2 == r1 && c2 < c1))
        {
            r1 = stateManager.dragCurrent.x;
            c1 = stateManager.dragCurrent.y;
            r2 = stateManager.dragStart.x;
            c2 = stateManager.dragStart.y;
        }

        int r = r1;
        int c = c1;
        bool done = false;

        while (!done && (r != r2 || c != c2))
        {
            if (r > r2)
                break;

            if (r >= 0 && r < stateManager.lines.Count && c <= stateManager.lines[r].Length)
            {
                int viewportR = r - stateManager.verticalScroll;
                int viewportC = c - stateManager.horizontalScroll + stateManager.GetLineCountPadding();
                if (viewportR >= 0 &&
                    viewportR < stateManager.viewportHeight &&
                    viewportC >= stateManager.GetLineCountPadding() &&
                    viewportC < stateManager.viewportWidth)
                {
                    SetCellColor(viewportR, viewportC, Color.white);
                    SetCellTextColor(viewportR, viewportC, Color.black);
                }
            }

            if (r >= 0 && r < stateManager.lines.Count && c <= stateManager.lines[r].Length - 1)
            {
                c++;
            }
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

    private void ComposeRenderTexture()
    {
        if (RenderTex == null || _compositeTexture == null || _pixelBuffer == null)
            return;

        int textureWidth = _compositeTexture.width;
        int textureHeight = _compositeTexture.height;

        for (int row = 0; row < stateManager.viewportHeight; row++)
        {
            int baseY = textureHeight - ((row + 1) * _cellActualHeight);

            for (int col = 0; col < stateManager.viewportWidth; col++)
            {
                int cellIndex = GetCellIndex(row, col);
                Color32 background = _cellBackgroundColors[cellIndex];
                Color32 foreground = _cellTextColors[cellIndex];

                GlyphMask mask = ResolveMaskForCell(row, col, _cellChars[cellIndex]);
                if (!mask.IsValid)
                    mask = _spaceMask;

                int baseX = col * _cellActualWidth;
                BlitCell(mask, background, foreground, baseX, baseY, textureWidth);
            }
        }

        DrawCursor(textureWidth, textureHeight);

        _compositeTexture.SetPixels32(_pixelBuffer);
        _compositeTexture.Apply(false, false);
        Graphics.Blit(_compositeTexture, RenderTex);
    }

    private void BlitCell(GlyphMask mask, Color32 background, Color32 foreground, int baseX, int baseY, int textureWidth)
    {
        for (int py = 0; py < _cellActualHeight; py++)
        {
            int dstIndex = (baseY + py) * textureWidth + baseX;
            int maskIndex = py * _cellActualWidth;

            for (int px = 0; px < _cellActualWidth; px++)
            {
                int i = maskIndex + px;
                Color32 result = mask.negative[i] != 0 ? background : COLOR_BLACK;
                if (mask.positive[i] != 0)
                    result = foreground;

                _pixelBuffer[dstIndex + px] = result;
            }
        }
    }

    private void DrawCursor(int textureWidth, int textureHeight)
    {
        if (!_cursorMask.IsValid || stateManager.lines == null || stateManager.lines.Count == 0)
            return;

        if (cursorBlinkEnabled && !_cursorBlinkVisible)
            return;

        int viewportRow = stateManager.cursorRow - stateManager.verticalScroll;
        int viewportCol = stateManager.visibleCursorCol + stateManager.GetLineCountPadding() - stateManager.horizontalScroll;

        if (viewportRow < 0 || viewportRow >= stateManager.viewportHeight)
            return;

        if (viewportCol < 0 || viewportCol >= stateManager.viewportWidth)
            return;

        int baseX = viewportCol * _cellActualWidth;
        int baseY = textureHeight - ((viewportRow + 1) * _cellActualHeight);
        Color32 color = cursorColor;

        for (int py = 0; py < _cellActualHeight; py++)
        {
            int dstIndex = (baseY + py) * textureWidth + baseX;
            int maskIndex = py * _cellActualWidth;

            for (int px = 0; px < _cellActualWidth; px++)
            {
                if (_cursorMask.positive[maskIndex + px] != 0)
                    _pixelBuffer[dstIndex + px] = color;
            }
        }
    }

    private GlyphMask ResolveMaskForCell(int row, int col, char ch)
    {
        if (ch == LOCK_GLYPH_SENTINEL)
            return _lockGlyphMask.IsValid ? _lockGlyphMask : _spaceMask;

        if (_glyphMasks.TryGetValue(ch, out GlyphMask mask))
            return mask;

        return _spaceMask.IsValid ? _spaceMask : CreateEmptyMask();
    }

    private int GetCellIndex(int row, int col)
    {
        return row * stateManager.viewportWidth + col;
    }

    private void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null)
            return;

        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    public void SetChar(int r, int c, char ch)
    {
        if (!IsCellInBounds(r, c))
            return;

        _cellChars[GetCellIndex(r, c)] = ch;
    }

    public void SetCellColor(int r, int c, Color color)
    {
        if (!IsCellInBounds(r, c))
            return;

        _cellBackgroundColors[GetCellIndex(r, c)] = (Color32)color;
    }

    public void SetCellTextColor(int r, int c, Color color)
    {
        if (!IsCellInBounds(r, c))
            return;

        _cellTextColors[GetCellIndex(r, c)] = (Color32)color;
    }

    private bool IsCellInBounds(int r, int c)
    {
        return stateManager != null &&
               _cellChars != null &&
               r >= 0 &&
               c >= 0 &&
               r < stateManager.viewportHeight &&
               c < stateManager.viewportWidth;
    }
}
