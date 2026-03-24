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

    [Header("UI Constraints")]
    public Vector2 minConsoleSize = new Vector2(200f, 150f);
    public string outputElementName = "Content";
    public bool autoFitToWindow = true;
    [Min(0f)] public float geometryPollDelay = 0.05f;
    public int minViewportWidth = 10;
    public int minViewportHeight = 5;
    public int maxViewportWidth = 300;
    public int maxViewportHeight = 120;

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
        if (!s_allConsoles.Contains(this)) s_allConsoles.Add(this);
    }
    private void OnDisable() => s_allConsoles.Remove(this);

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        container.style.flexGrow = 1;
        UIDocument rootDoc = null;
        if (root is MonoBehaviour monoRoot) rootDoc = monoRoot.GetComponent<UIDocument>();

        BindToElement(container, rootDoc);
        InitializeSubComponents(container, root);
    }

    public override Vector2 GetMinimumSize() => minConsoleSize;

    public void BindToElement(VisualElement element, UIDocument doc)
    {
        if (element == null) return;
        _outputVE = element;
        uiDocument = doc;

        stateManager.Initialize();
        rendererManager.Initialize();
        inputManager.Initialize();

        HookToUIToolkit();
    }

    private void HookToUIToolkit()
    {
        if (_outputVE == null) return;

        BindRenderTextureToOutput();

        if (_uiHooked) return;
        _uiHooked = true;

        _outputVE.focusable = true;
        _outputVE.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != (int)MouseButton.LeftMouse) return;
            FocusThisConsole();
            BringToFront();
            evt.StopPropagation();
        });

        _outputVE.RegisterCallback<GeometryChangedEvent>(OnOutputGeometryChanged);

        if (inputManager.mouseListener != null)
        {
            inputManager.mouseListener.Bind(_outputVE);
        }
    }

    private void BindRenderTextureToOutput()
    {
        if (_outputVE == null || rendererManager.RenderTex == null) return;
        _outputVE.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rendererManager.RenderTex));
        _outputVE.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
        _outputVE.style.backgroundSize = new BackgroundSize(rendererManager.RenderTex.width, rendererManager.RenderTex.height);
        _outputVE.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left);
        _outputVE.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top);
        _outputVE.style.backgroundColor = Color.black;
    }

    private void OnOutputGeometryChanged(GeometryChangedEvent evt)
    {
        if (!autoFitToWindow || _outputVE == null || rendererManager.RenderTex == null) return;
        if (_autoFitRoutine != null) StopCoroutine(_autoFitRoutine);
        _autoFitRoutine = StartCoroutine(AutoFitAfterDelay());
    }

    private IEnumerator AutoFitAfterDelay()
    {
        yield return null;
        if (geometryPollDelay > 0f) yield return new WaitForSeconds(geometryPollDelay);
        _autoFitRoutine = null;
        ApplyAutoFitFromElement();
    }

    private void ApplyAutoFitFromElement()
    {
        float w = _outputVE.resolvedStyle.width;
        float h = _outputVE.resolvedStyle.height;
        if (w <= 1f || h <= 1f) return;

        float pxPerCol = Mathf.Max(1f, (float)rendererManager.RenderTex.width / Mathf.Max(1, stateManager.viewportWidth));
        float pxPerRow = Mathf.Max(1f, (float)rendererManager.RenderTex.height / Mathf.Max(1, stateManager.viewportHeight));

        int desiredCols = Mathf.Clamp(Mathf.FloorToInt(w / pxPerCol), minViewportWidth, maxViewportWidth);
        int desiredRows = Mathf.Clamp(Mathf.FloorToInt(h / pxPerRow), minViewportHeight, maxViewportHeight);

        if (desiredCols == stateManager.viewportWidth && desiredRows == stateManager.viewportHeight) return;

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
        if (_outputVE == null) return;
        VisualElement windowRoot = _outputVE;
        while (windowRoot.parent != null && (uiDocument == null || windowRoot.parent != uiDocument.rootVisualElement))
            windowRoot = windowRoot.parent;
        windowRoot?.BringToFront();

        if (uiDocument != null)
        {
            int maxSortOrder = 0;
            foreach (var console in s_allConsoles)
                if (console != null && console.uiDocument != null)
                    maxSortOrder = (int)(Mathf.Max(maxSortOrder, console.uiDocument.sortingOrder));
            uiDocument.sortingOrder = maxSortOrder + 1;
        }
    }

    private void FocusThisConsole()
    {
        foreach (var c in s_allConsoles) if (c != null && c != this) c.DefocusInternal();
        IsFocused = true;
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
        foreach (var c in s_allConsoles) if (c != null) c.DefocusInternal();
        UnityEngine.Cursor.lockState = lockMode;
        UnityEngine.Cursor.visible = visible;
    }

    public bool IsPointerInsideThisConsole(Vector3 mousePos)
    {
        if (_outputVE == null || _outputVE.panel == null) return false;
        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(_outputVE.panel, mousePos);
        return _outputVE.worldBound.Contains(panelPos);
    }

    public Vector2Int GetCursorLocationForMouse(Vector2 currentMousePosition)
    {
        if (_outputVE == null || rendererManager.RenderTex == null) return Vector2Int.zero;
        float elemW = _outputVE.resolvedStyle.width;
        float elemH = _outputVE.resolvedStyle.height;
        if (elemW <= 0 || elemH <= 0) return Vector2Int.zero;

        float pixelX = currentMousePosition.x * elemW;
        float pixelY = (1f - currentMousePosition.y) * elemH;
        float textureU = Mathf.Clamp01(pixelX / rendererManager.RenderTex.width);
        float textureV = Mathf.Clamp01(1f - (pixelY / rendererManager.RenderTex.height));

        int r = Mathf.Max(0, Mathf.Min(stateManager.lines.Count - 1 - stateManager.verticalScroll, (int)((1 - textureV) * stateManager.viewportHeight)));
        int padding = stateManager.GetLineCountPadding();
        int c = Mathf.Max(0, Mathf.Min(stateManager.lines[r + stateManager.verticalScroll].Length, (int)(textureU * stateManager.viewportWidth + .5) - padding));

        return new Vector2Int(r, c);
    }
}