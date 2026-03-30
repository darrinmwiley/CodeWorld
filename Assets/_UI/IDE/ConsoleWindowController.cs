using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class ConsoleWindowController : WindowComponent
{
    [Header("Sub-Managers")]
    public ConsoleStateManager stateManager;
    public ConsoleRenderer rendererManager;
    public ConsoleInputManager inputManager;

    [Header("Session")]
    [SerializeField] private ConsoleFileSessionManager _fileSessionManager;

    [Header("UI Constraints")]
    public Vector2 minConsoleSize = new Vector2(200f, 150f);
    public string outputElementName = "Content";
    public bool autoFitToWindow = true;
    [Min(0f)] public float geometryPollDelay = 0.05f;
    public int minViewportWidth = 10;
    public int minViewportHeight = 5;
    public int maxViewportWidth = 300;
    public int maxViewportHeight = 120;

    [Header("Auto-Fit Buffer")]
    public int extraBufferColumns = 1;
    public int extraBufferRows = 1;

    [Header("Focus")]
    public bool escapeDefocusesAll = true;
    public CursorLockMode escapeCursorLockMode = CursorLockMode.Locked;
    public bool escapeCursorVisible = false;

    public bool IsFocused { get; private set; }

    private UIDocument uiDocument;
    private VisualElement _outputVE;
    private bool _uiHooked;
    private Coroutine _autoFitRoutine;

    private static readonly List<ConsoleWindowController> s_allConsoles = new List<ConsoleWindowController>();

    private void OnEnable()
    {
        if (!s_allConsoles.Contains(this))
            s_allConsoles.Add(this);
    }

    private void OnDisable()
    {
        _fileSessionManager?.SaveDocumentForStateManager(stateManager);
        s_allConsoles.Remove(this);
    }

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        container.style.flexGrow = 1;
        UIDocument rootDoc = null;
        if (root is MonoBehaviour monoRoot)
            rootDoc = monoRoot.GetComponent<UIDocument>();

        BindToElement(container, rootDoc);
        InitializeSubComponents(container, root);
    }

    public override Vector2 GetMinimumSize() => minConsoleSize;

    public void BindToElement(VisualElement element, UIDocument doc)
    {
        if (element == null)
            return;

        _outputVE = element;
        uiDocument = doc;

        stateManager.Initialize();
        rendererManager.Initialize();
        inputManager.Initialize();

        HookToUIToolkit();

        if (_fileSessionManager != null)
            _fileSessionManager.RestoreActiveDocument(stateManager);
    }

    private void HookToUIToolkit()
    {
        if (_outputVE == null)
            return;

        BindRenderTextureToOutput();

        if (_uiHooked)
            return;

        _uiHooked = true;

        _outputVE.focusable = true;
        _outputVE.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            FocusFromInteraction();
            evt.StopPropagation();
        });

        _outputVE.RegisterCallback<GeometryChangedEvent>(OnOutputGeometryChanged);

        if (inputManager.mouseListener != null)
            inputManager.mouseListener.Bind(_outputVE);
    }

    private void BindRenderTextureToOutput()
    {
        if (_outputVE == null || rendererManager.RenderTex == null)
            return;

        int displayWidth = rendererManager.DisplayTextureWidth > 0 ? rendererManager.DisplayTextureWidth : rendererManager.RenderTex.width;
        int displayHeight = rendererManager.DisplayTextureHeight > 0 ? rendererManager.DisplayTextureHeight : rendererManager.RenderTex.height;

        _outputVE.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rendererManager.RenderTex));
        _outputVE.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
        _outputVE.style.backgroundSize = new BackgroundSize(displayWidth, displayHeight);
        _outputVE.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left);
        _outputVE.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top);
        _outputVE.style.backgroundColor = rendererManager != null ? rendererManager.BackgroundColor : Color.black;
    }

    private void OnOutputGeometryChanged(GeometryChangedEvent evt)
    {
        if (!autoFitToWindow || _outputVE == null || rendererManager.RenderTex == null)
            return;

        if (_autoFitRoutine != null)
            StopCoroutine(_autoFitRoutine);

        _autoFitRoutine = StartCoroutine(AutoFitAfterDelay());
    }

    private IEnumerator AutoFitAfterDelay()
    {
        yield return null;

        if (geometryPollDelay > 0f)
            yield return new WaitForSeconds(geometryPollDelay);

        _autoFitRoutine = null;
        ApplyAutoFitFromElement();
    }

    private void ApplyAutoFitFromElement()
    {
        float w = _outputVE.resolvedStyle.width;
        float h = _outputVE.resolvedStyle.height;
        if (w <= 1f || h <= 1f)
            return;

        float displayWidth = Mathf.Max(1f, rendererManager.DisplayTextureWidth);
        float displayHeight = Mathf.Max(1f, rendererManager.DisplayTextureHeight);

        float pxPerCol = displayWidth / Mathf.Max(1, stateManager.viewportWidth);
        float pxPerRow = displayHeight / Mathf.Max(1, stateManager.viewportHeight);

        int desiredCols = Mathf.Clamp(
            Mathf.CeilToInt(w / pxPerCol) + Mathf.Max(0, extraBufferColumns),
            minViewportWidth,
            maxViewportWidth
        );

        int desiredRows = Mathf.Clamp(
            Mathf.CeilToInt(h / pxPerRow) + Mathf.Max(0, extraBufferRows),
            minViewportHeight,
            maxViewportHeight
        );

        if (desiredCols == stateManager.viewportWidth && desiredRows == stateManager.viewportHeight)
            return;

        ResizeViewport(desiredCols, desiredRows);
    }

    private void ResizeViewport(int newCols, int newRows)
    {
        int targetVScroll = stateManager.verticalScroll;
        int targetHScroll = stateManager.horizontalScroll;

        stateManager.viewportWidth = newCols;
        stateManager.viewportHeight = newRows;
        stateManager.cursorRow = Mathf.Clamp(stateManager.cursorRow, 0, Mathf.Max(0, stateManager.lines.Count - 1));
        stateManager.visibleCursorCol = Mathf.Clamp(stateManager.visibleCursorCol, 0, stateManager.GetLineLength(stateManager.cursorRow));
        stateManager.cursorCol = stateManager.visibleCursorCol;

        rendererManager.GenerateGrid();
        BindRenderTextureToOutput();

        stateManager.verticalScroll = Mathf.Clamp(targetVScroll, 0, Mathf.Max(0, stateManager.lines.Count - 1));
        stateManager.horizontalScroll = Mathf.Max(0, targetHScroll);
        stateManager.NotifyStateChanged();
    }

    public void BringToFront()
    {
        if (_outputVE == null)
            return;

        VisualElement windowRoot = _outputVE;
        while (windowRoot.parent != null && (uiDocument == null || windowRoot.parent != uiDocument.rootVisualElement))
            windowRoot = windowRoot.parent;

        windowRoot?.BringToFront();

        if (uiDocument != null)
        {
            int maxSortOrder = 0;
            foreach (var console in s_allConsoles)
            {
                if (console != null && console.uiDocument != null)
                    maxSortOrder = (int)Mathf.Max(maxSortOrder, console.uiDocument.sortingOrder);
            }

            uiDocument.sortingOrder = maxSortOrder + 1;
        }
    }

    public void FocusFromInteraction()
    {
        foreach (var c in s_allConsoles)
        {
            if (c != null && c != this)
                c.DefocusInternal();
        }

        IsFocused = true;
        BringToFront();
        _outputVE?.Focus();
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    public void DefocusInternal()
    {
        IsFocused = false;
        _outputVE?.Blur();
        stateManager.isHighlighting = false;
        stateManager.NotifyStateChanged();
    }

    public static void DefocusAllAndRecaptureMouse(CursorLockMode lockMode, bool visible)
    {
        foreach (var c in s_allConsoles)
        {
            if (c != null)
                c.DefocusInternal();
        }

        UnityEngine.Cursor.lockState = lockMode;
        UnityEngine.Cursor.visible = visible;
    }

    public bool IsPointerInsideThisConsole(Vector3 mousePos)
    {
        if (_outputVE == null || _outputVE.panel == null)
            return false;

        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(_outputVE.panel, mousePos);
        return _outputVE.worldBound.Contains(panelPos);
    }

    public Vector2Int GetCursorLocationForMouse(Vector2 currentMousePosition)
    {
        if (_outputVE == null || rendererManager.RenderTex == null)
            return Vector2Int.zero;

        float elemW = _outputVE.resolvedStyle.width;
        float elemH = _outputVE.resolvedStyle.height;
        if (elemW <= 0 || elemH <= 0)
            return Vector2Int.zero;

        float displayWidth = Mathf.Max(1f, rendererManager.DisplayTextureWidth);
        float displayHeight = Mathf.Max(1f, rendererManager.DisplayTextureHeight);

        float pixelX = currentMousePosition.x * elemW;
        float pixelY = (1f - currentMousePosition.y) * elemH;
        float textureU = Mathf.Clamp01(pixelX / displayWidth);
        float textureV = Mathf.Clamp01(1f - (pixelY / displayHeight));

        int r = Mathf.Max(0, Mathf.Min(stateManager.lines.Count - 1 - stateManager.verticalScroll, (int)((1f - textureV) * stateManager.viewportHeight)));
        int padding = stateManager.GetLineCountPadding();
        int c = Mathf.Max(0, Mathf.Min(stateManager.GetLineLength(r + stateManager.verticalScroll), (int)(textureU * stateManager.viewportWidth + .5f) - padding));

        return new Vector2Int(r, c);
    }
}
