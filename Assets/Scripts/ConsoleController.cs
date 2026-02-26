using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using UnityEngine.UIElements;

public class ConsoleController : MonoBehaviour
{

    //TODO Cosmetic
    //console frame
    //menu options (save, load, new, font size)
    //clickable scrollbar bottom and right when applicable
    //resize on edges
    //drag on top
    //font size
    //source tree parsing
    //automatic horizontal scroll on mouse drag.


    public GameObject consoleCharPrefab;
    public GameObject cursorPrefab;
    public UIToolkitMouseListenerMono mouseListener;
    public int viewportWidth = 80;
    public int viewportHeight = 24;
    public int spacesPerTab = 4;
    public float heldKeyDelay = .005f;
    public float heldKeyTriggerTime = .4f;
    public List<string> lines = new List<string>();
    //public List<GameObject> rawImageHolders;

    public UIDocument uiDocument;
    string outputElementName = "Content";

    VisualElement _outputVE;


    RenderTexture renderTexture;
    GameObject[,] chars;
    GameObject cursor;
    int cursorRow = 0;
    int cursorCol = 0;
    int visibleCursorCol = 0;
    KeyCode latest = KeyCode.None;
    float latestDownTime = 0;
    bool isKeyHeld = false;
    float lastHeldKeyTriggerTime = 0;
    float lastDragScrollTime = 0;
    Vector2Int dragStart;
    Vector2Int dragCurrent;
    Dictionary<KeyCode, Action> specialKeyPressHandlers;

    List<Transaction> mutations = new List<Transaction>();
    int transactionPointer = -1;

    public bool isHighlighting;

    int verticalScroll;
    int horizontalScroll;

    string copyBuffer;

    public bool isFocused;

    // --------------------------
    // Focus behavior
    // --------------------------
    [Header("Focus")]
    [Tooltip("Pressing Escape will defocus all consoles and apply the cursor lock/visibility settings below.")]
    public bool escapeDefocusesAll = true;

    [Tooltip("When Escape defocuses all consoles, set this Cursor lock state.")]
    public CursorLockMode escapeCursorLockMode = CursorLockMode.Locked;

    [Tooltip("When Escape defocuses all consoles, set Cursor.visible to this value.")]
    public bool escapeCursorVisible = false;

    // --------------------------
    // Render isolation + offsets
    // --------------------------
    [Header("Rendering Isolation")]
    [SerializeField] private string consoleRenderLayerName = "ConsoleUI";

    // World-space size of a single glyph cell in your grid (matches your positioning math)
    private const float CELL_X = 3f / 5f; // 0.6
    private const float CELL_Y = 1f;

    [Header("Instance Offsetting")]
    [SerializeField] private Vector3 perInstanceWorldOffset = Vector3.zero;
    [SerializeField] private bool autoOffsetInstances = true;
    [SerializeField] private float instancePaddingWorld = 10f;

    private static int s_instanceCounter = 0;
    private int _instanceId = -1;
    private int _consoleLayer = -1;

    // Track all active consoles so Escape can defocus all, even across multiple UIDocuments/panels.
    private static readonly List<ConsoleController> s_allConsoles = new List<ConsoleController>();

    private void OnEnable()
    {
        if (!s_allConsoles.Contains(this))
            s_allConsoles.Add(this);
    }

    private void OnDisable()
    {
        s_allConsoles.Remove(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        _instanceId = s_instanceCounter++;
        _consoleLayer = LayerMask.NameToLayer(consoleRenderLayerName);
        if (_consoleLayer < 0)
        {
            Debug.LogError($"Layer '{consoleRenderLayerName}' does not exist. Create it in Project Settings > Tags and Layers.");
        }

        mouseListener.AddMouseDownHandler(OnMouseDown);
        mouseListener.AddMouseUpHandler(OnMouseUp);
        mouseListener.AddMouseDragHandler(OnMouseDrag);

        InitKeyHandlers();
        lines.Add("");
        Generate();
        HookToUIToolkit();
        UpdateConsole();
    }

    void HookToUIToolkit()
    {
        _outputVE = uiDocument.rootVisualElement.Q<VisualElement>(outputElementName);

        // 1) Apply the RenderTexture as a background image
        _outputVE.style.backgroundImage =
            new StyleBackground(Background.FromRenderTexture(renderTexture));

        // 2) PREVENT SCALING: Set ScaleMode to 'None' (ScaleAndCrop with backgroundSize fixes the ratio)
        _outputVE.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;

        // 3) 1:1 PIXEL RATIO: Force the background to match the texture's actual dimensions
        // This prevents the UI from stretching the image when the container grows.
        _outputVE.style.backgroundSize = new BackgroundSize(renderTexture.width, renderTexture.height);

        // 4) ANCHOR TOP-LEFT: Ensure the texture doesn't center itself
        _outputVE.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left);
        _outputVE.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top);

        // 5) FILL EXCESS: Set the container background to black for any space outside the texture
        _outputVE.style.backgroundColor = Color.black;

        // Make console focusable
        _outputVE.focusable = true;

        // Click inside console → focus this console (and defocus others)
        _outputVE.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != (int)MouseButton.LeftMouse) return;

            FocusThisConsole();

            evt.StopPropagation();
        });

        // Bind mouse listener to this VisualElement
        mouseListener.Bind(_outputVE);
        // NOTE: handlers were already added in Start(); don't double-add here.
    }

    private void FocusThisConsole()
    {
        // Defocus all other consoles first (works across multiple panels/UIDocuments)
        for (int i = 0; i < s_allConsoles.Count; i++)
        {
            var c = s_allConsoles[i];
            if (c == null) continue;
            if (c != this) c.DefocusInternal();
        }

        isFocused = true;
        if (_outputVE != null)
            _outputVE.Focus();

        // You might want the cursor free while typing.
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    private void DefocusInternal()
    {
        isFocused = false;
        if (_outputVE != null)
            _outputVE.Blur();

        // Also stop highlighting so selection doesn't "stick" weirdly across focus changes
        isHighlighting = false;
    }

    public static void DefocusAllAndRecaptureMouse(CursorLockMode lockMode, bool visible)
    {
        for (int i = 0; i < s_allConsoles.Count; i++)
        {
            var c = s_allConsoles[i];
            if (c == null) continue;
            c.DefocusInternal();
        }

        UnityEngine.Cursor.lockState = lockMode;
        UnityEngine.Cursor.visible = visible;
    }

    public void Save(string fileName)
    {
        try
        {
            // Combine the persistent data path with the file name
            string filePath = Path.Combine(Application.persistentDataPath, fileName);

            // Create a StreamWriter to write to the file
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write each line from the string array to the file
                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }
            }

            Debug.Log("File saved successfully: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Error saving file: " + e.Message);
        }
    }

    public void Load(string relativeFilePath)
    {
        lines = new List<string>();

        try
        {
            // Combine the persistent data path with the relative file path
            string filePath = Path.Combine(Application.persistentDataPath, relativeFilePath);

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Read all lines from the file and add them to the List
                string[] fileLines = File.ReadAllLines(filePath);
                lines.AddRange(fileLines);
            }
            else
            {
                Debug.LogWarning("File not found: " + filePath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error loading file: " + e.Message);
        }

        UpdateConsole();
    }

    public ConsoleState GetState()
    {
        return new ConsoleState.Builder()
            .setCursorCol(cursorCol)
            .setCursorRow(cursorRow)
            .setVisibleCursorCol(visibleCursorCol)
            .setIsHighlighting(isHighlighting)
            .setDragStart(dragStart)
            .setDragCurrent(dragCurrent)
            .build();
    }

    public void SetState(ConsoleState consoleState)
    {
        this.cursorCol = consoleState.cursorCol;
        this.cursorRow = consoleState.cursorRow;
        this.visibleCursorCol = consoleState.visibleCursorCol;
        this.isHighlighting = consoleState.isHighlighting;
        this.dragStart = consoleState.dragStart;
        this.dragCurrent = consoleState.dragCurrent;
    }

    public void Undo()
    {
        if (transactionPointer < 0)
            return;
        Transaction previous = mutations[transactionPointer];
        previous.Revert(this);
        transactionPointer--;
        AdjustScrollToCursor();
        UpdateConsole();
    }

    public void Redo()
    {
        if (transactionPointer >= mutations.Count - 1)
            return;
        Transaction next = mutations[transactionPointer + 1];
        next.Apply(this);
        transactionPointer++;
        UpdateConsole();
        AdjustScrollToCursor();
        AdjustScrollToCursor();
    }

    void InitKeyHandlers()
    {
        specialKeyPressHandlers = new Dictionary<KeyCode, Action>();
        specialKeyPressHandlers[KeyCode.Return] = OnReturnPressed;
        specialKeyPressHandlers[KeyCode.KeypadEnter] = OnReturnPressed;
        specialKeyPressHandlers[KeyCode.Backspace] = OnBackspacePressed;
        specialKeyPressHandlers[KeyCode.LeftArrow] = OnLeftArrowPressed;
        specialKeyPressHandlers[KeyCode.RightArrow] = OnRightArrowPressed;
        specialKeyPressHandlers[KeyCode.UpArrow] = OnUpArrowPressed;
        specialKeyPressHandlers[KeyCode.DownArrow] = OnDownArrowPressed;
        specialKeyPressHandlers[KeyCode.Tab] = OnTabPressed;
        specialKeyPressHandlers[KeyCode.C] = OnCKeyPressed;
        specialKeyPressHandlers[KeyCode.V] = OnVKeyPressed;
        specialKeyPressHandlers[KeyCode.Z] = OnZKeyPressed;
        specialKeyPressHandlers[KeyCode.Y] = OnYKeyPressed;
        specialKeyPressHandlers[KeyCode.Z] = OnZKeyPressed;
        specialKeyPressHandlers[KeyCode.X] = OnXKeyPressed;
        specialKeyPressHandlers[KeyCode.Delete] = OnDeletePressed;
        specialKeyPressHandlers[KeyCode.A] = OnAKeyPressed;
        specialKeyPressHandlers[KeyCode.S] = OnSKeyPressed;
        specialKeyPressHandlers[KeyCode.L] = OnLKeyPressed;
    }

    //first, we will simply make it as many lines as we can hold. Scrolling to be added later
    void UpdateConsole()
    {
        UpdateLineNumbers();
        UpdateLines();
        if (isHighlighting)
            UpdateHighlight();
        string code = String.Join("\n", lines);
    }

    void UpdateCursor()
    {
        int trueCursorCol = visibleCursorCol + GetLineCountPadding() - horizontalScroll;

        // Cursor is parented under charsGO now, so local coords:
        cursor.transform.localPosition = new Vector3(
            (trueCursorCol * CELL_X) + .05f,
            (verticalScroll + viewportHeight - 1 - cursorRow) * CELL_Y + .5f,
            -0.1f
        );
    }

    public int GetLineCountPadding()
    {
        return (lines.Count + "").Length + 2;
    }

    public GameObject SpawnConsoleChar()
    {
        return Instantiate(consoleCharPrefab, new Vector3Int(0, 0, 0), Quaternion.identity);
    }

    public RenderTexture CreateRenderTexture(Camera camera, int renderTextureWidth)
    {
        float aspectRatio = camera.aspect;
        int renderTextureHeight = Mathf.RoundToInt(renderTextureWidth / aspectRatio); // Calculate height based on aspect ratio
        RenderTexture renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24);
        renderTexture.name = "CameraRenderTexture"; // Set a name for the RenderTexture
        renderTexture.antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing); // Use the camera's anti-aliasing settings or default to 1
        renderTexture.filterMode = FilterMode.Bilinear; // You can adjust the filter mode as needed
        camera.targetTexture = renderTexture;
        return renderTexture;
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private Vector3 ComputeAutoOffsetWorld()
    {
        float gridWorldW = viewportWidth * CELL_X;
        float spacing = gridWorldW + instancePaddingWorld;
        return new Vector3(_instanceId * spacing, 0f, 0f);
    }

    public void Generate()
    {
        Transform charsTransform = transform.Find("chars");
        GameObject charsGO = charsTransform == null ? new GameObject("chars") : charsTransform.gameObject;

        Transform cameraTransform = transform.Find("camera");
        GameObject cameraGO = cameraTransform == null ? new GameObject("camera") : cameraTransform.gameObject;

        charsGO.transform.SetParent(transform, false);
        cameraGO.transform.SetParent(transform, false);

        // Clear old children
        for (int i = charsGO.transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(charsGO.transform.GetChild(i).gameObject);

        // Decide this instance’s world offset
        Vector3 instanceOffset = perInstanceWorldOffset;
        if (autoOffsetInstances)
            instanceOffset += ComputeAutoOffsetWorld();

        // Position the whole console “world” away from others
        charsGO.transform.position = transform.position + instanceOffset;

        chars = new GameObject[viewportHeight, viewportWidth];
        for (int i = 0; i < viewportWidth; i++)
        {
            for (int j = 0; j < viewportHeight; j++)
            {
                GameObject cube = SpawnConsoleChar();
                cube.transform.SetParent(charsGO.transform, false);

                // Local space grid
                cube.transform.localPosition = new Vector3((i * CELL_X) + .3f, (viewportHeight - 1 - j) * CELL_Y + .5f, 0f);

                chars[j, i] = cube;
            }
        }

        if (cursor != null) DestroyImmediate(cursor);
        cursor = Instantiate(cursorPrefab, Vector3.zero, Quaternion.identity);
        cursor.transform.SetParent(charsGO.transform, false);
        cursor.transform.localPosition = new Vector3((3 * CELL_X) + .05f, (viewportHeight - 1) * CELL_Y + .5f, -0.1f);

        // Force everything (chars + cursor) onto ConsoleUI layer
        if (_consoleLayer >= 0)
            SetLayerRecursively(charsGO, _consoleLayer);

        Camera camera = cameraGO.GetComponent<Camera>();
        if (camera == null)
            camera = cameraGO.AddComponent<Camera>();

        // Only render ConsoleUI
        if (_consoleLayer >= 0)
            camera.cullingMask = 1 << _consoleLayer;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;

        // Deterministic framing (no GetBounds needed)
        float gridMinX = charsGO.transform.position.x + 0f;
        float gridMaxX = charsGO.transform.position.x + (viewportWidth * CELL_X) + 0.6f; // small fudge for your +.3f
        float gridMinY = charsGO.transform.position.y + 0f;
        float gridMaxY = charsGO.transform.position.y + (viewportHeight * CELL_Y) + 1.0f;

        SetCamera(camera, gridMinX, gridMaxY, gridMaxX, gridMinY);

        int textureWidth = 1000;
        renderTexture = CreateRenderTexture(camera, textureWidth);
    }

    public Bounds GetBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    //we're going to assume XY plane for now
    public void SetCamera(Camera camera, float tlx, float tly, float brx, float bry)
    {
        // Calculate the center of the rectangle
        float midX = (tlx + brx) / 2;
        float midY = (tly + bry) / 2;

        // Calculate the width and height of the rectangle
        float width = Mathf.Abs(brx - tlx);
        float height = Mathf.Abs(bry - tly);

        // Set the camera's orthographic size based on the rectangle's dimensions
        // assuming the camera's default rotation is looking down the negative Z-axis
        float orthoSizeY = height / 2f;
        camera.orthographicSize = orthoSizeY;

        // Calculate the aspect ratio of the rectangle
        float aspectRatio = width / height;

        // Calculate the new camera position
        Vector3 newPosition = new Vector3(midX, midY, -10);
        camera.transform.position = newPosition;

        // Set the camera's orthographic view
        camera.orthographic = true;
        camera.aspect = aspectRatio;
    }

    public void OnReturnPressed()
    {
        AdjustScrollToCursor();
        ApplyTransaction(new NewlineTransaction());
        UpdateConsole();
    }

    public void NewLine()
    {
        if (isHighlighting)
            DeleteHighlight();
        lines.Insert(cursorRow + 1, lines[cursorRow].Substring(visibleCursorCol));
        lines[cursorRow] = lines[cursorRow].Substring(0, visibleCursorCol);
        cursorRow++;
        visibleCursorCol = cursorCol = 0;
        AdjustScrollToCursor();
    }

    public void RevertNewLine()
    {
        lines[cursorRow - 1] += lines[cursorRow];
        lines.RemoveAt(cursorRow);
    }

    public void OnTabPressed()
    {
        int numSpaces = spacesPerTab - (visibleCursorCol % spacesPerTab);
        string tab = "";
        for (int i = 0; i < numSpaces; i++)
            tab += " ";
        AdjustScrollToCursor();
        ApplyTransaction(new InsertTransaction(tab));
    }

    void UpdateLines()
    {
        List<Vector3Int> keywordLocations = SyntaxHighlighter.GetKeywordLocations(String.Join("\n", lines));
        List<Vector3Int> stringLocations = SyntaxHighlighter.GetStringLocations(String.Join("\n", lines));
        List<Vector3Int> commentLocations = SyntaxHighlighter.GetCommentLocations(String.Join("\n", lines));
        int padding = GetLineCountPadding();
        for (int i = 0; i < viewportHeight; i++)
        {
            for (int j = padding; j < viewportWidth; j++)
            {
                SetCellColor(i, j, Color.black);
                SetCellTextColor(i, j, Color.white);
                int lineNumber = i + verticalScroll;
                if (lines.Count > lineNumber && lines[lineNumber].Length > j - padding + horizontalScroll)
                {
                    //TODO make more efficient
                    int c = j - padding + horizontalScroll;
                    //blue
                    foreach (Vector3Int keyword in keywordLocations)
                    {
                        if (keyword.x - 1 == lineNumber && keyword.y <= c && keyword.z >= c)
                        {
                            SetCellTextColor(i, j, new Color(86 / 256f, 156 / 256f, 214 / 256f));
                        }
                    }
                    //orange
                    foreach (Vector3Int keyword in stringLocations)
                    {
                        if (keyword.x - 1 == lineNumber && keyword.y <= c && keyword.z >= c)
                        {
                            SetCellTextColor(i, j, new Color(206 / 256f, 132 / 256f, 77 / 256f));
                        }
                    }
                    //green
                    foreach (Vector3Int keyword in commentLocations)
                    {
                        if (keyword.x - 1 == lineNumber && keyword.y <= c && keyword.z >= c)
                        {
                            SetCellTextColor(i, j, new Color(106 / 256f, 153 / 256f, 85 / 256f));
                        }
                    }
                    SetChar(i, j, lines[lineNumber][j - padding + horizontalScroll]);
                }
                else
                {
                    SetChar(i, j, ' ');
                }
            }
        }
    }

    void UpdateLineNumbers()
    {
        int maxLineNumberLength = ("" + (lines.Count)).Length;
        int totalLineNumberLength = maxLineNumberLength + 2;

        for (int i = 0; i < viewportHeight; i++)
        {
            if (verticalScroll + i < lines.Count)
            {
                string lineNumber = (verticalScroll + i + 1 + "");
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

    public bool CanBackspaceTab()
    {
        if (visibleCursorCol == 0)
            return false;
        for (int i = 0; i < visibleCursorCol; i++)
        {
            if (lines[cursorRow][i] != ' ')
                return false;
        }
        return true;
    }

    public void OnBackspacePressed()
    {
        ApplyTransaction(new DeleteTransaction(/*isBackspace = */true));
        UpdateConsole();
    }

    public void OnDeletePressed()
    {
        ApplyTransaction(new DeleteTransaction(/*isBackspace = */false));
        UpdateConsole();
    }

    public void OnUpArrowPressed()
    {
        isHighlighting = false;
        if (cursorRow == 0)
            return;
        cursorRow--;
        visibleCursorCol = Mathf.Min(cursorCol, lines[cursorRow].Length);
        AdjustScrollToCursor();
        UpdateConsole();
    }

    public void OnDownArrowPressed()
    {
        isHighlighting = false;
        if (cursorRow >= lines.Count - 1)
            return;
        cursorRow++;
        visibleCursorCol = Mathf.Min(cursorCol, lines[cursorRow].Length);
        AdjustScrollToCursor();
        UpdateConsole();
    }

    public void MaybeDownScroll()
    {
        if (verticalScroll < lines.Count - 1)
            verticalScroll++;
        UpdateConsole();
    }

    public void MaybeUpScroll()
    {
        if (verticalScroll != 0)
            verticalScroll--;
        UpdateConsole();
    }

    public void OnLeftArrowPressed()
    {
        isHighlighting = false;
        if (visibleCursorCol != 0)
            visibleCursorCol--;
        else if (cursorRow != 0)
        {
            cursorRow--;
            visibleCursorCol = lines[cursorRow].Length;
        }
        cursorCol = visibleCursorCol;
        AdjustScrollToCursor();
        UpdateConsole();
    }

    public void OnRightArrowPressed()
    {
        isHighlighting = false;
        if (visibleCursorCol != lines[cursorRow].Length)
            visibleCursorCol++;
        else
        {
            if (cursorRow < lines.Count - 1)
            {
                cursorRow++;
                visibleCursorCol = 0;
            }
        }
        cursorCol = visibleCursorCol;
        AdjustScrollToCursor();
        UpdateConsole();
    }

    bool IsUpperCase()
    {
        return (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) ^ (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
    }

    public void OnCKeyPressed()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            copyBuffer = GetHighlightedText();
        }
        else
        {
            if (IsUpperCase())
                OnKeyPressed('C');
            else
                OnKeyPressed('c');
        }
    }

    public void OnVKeyPressed()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            AdjustScrollToCursor();
            ApplyTransaction(new InsertTransaction(copyBuffer));
            UpdateConsole();
        }
        else
        {
            if (IsUpperCase())
                OnKeyPressed('V');
            else
                OnKeyPressed('v');
        }
    }

    //todo, if no highlight, this shouldn't delete anything
    public void OnXKeyPressed()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (isHighlighting)
            {
                copyBuffer = GetHighlightedText();
                AdjustScrollToCursor();
                ApplyTransaction(new DeleteTransaction(true));
                UpdateConsole();
            }
            isHighlighting = false;
        }
        else if (IsUpperCase())
        {
            OnKeyPressed('X');
        }
        else
        {
            OnKeyPressed('x');
        }
    }

    public void OnYKeyPressed()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            Redo();
        }
        else if (IsUpperCase())
        {
            OnKeyPressed('Y');
        }
        else
        {
            OnKeyPressed('y');
        }
    }

    public void OnZKeyPressed()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            Undo();
        }
        else if (IsUpperCase())
        {
            OnKeyPressed('Z');
        }
        else
        {
            OnKeyPressed('z');
        }
    }

    public void SelectAll()
    {
        isHighlighting = true;
        dragStart = new Vector2Int(0, 0);
        dragCurrent = new Vector2Int(lines.Count - 1, lines[lines.Count - 1].Length);
        UpdateHighlight();
    }

    public void OnAKeyPressed()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            SelectAll();
        }
        else if (IsUpperCase())
        {
            OnKeyPressed('A');
        }
        else
        {
            OnKeyPressed('a');
        }
    }

    public void OnSKeyPressed()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            Save("fname.txt");
        }
        else if (IsUpperCase())
        {
            OnKeyPressed('S');
        }
        else
        {
            OnKeyPressed('s');
        }
    }

    public void OnLKeyPressed()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            Load("fname.txt");
        }
        else if (IsUpperCase())
        {
            OnKeyPressed('L');
        }
        else
        {
            OnKeyPressed('l');
        }
    }

    public void InsertLines(string[] strs)
    {
        if (isHighlighting)
            DeleteHighlight();
        for (int i = 0; i < strs.Length; i++)
        {
            string line = strs[i];
            OnKeysTyped(line);
            if (i != strs.Length - 1)
                NewLine();
        }
    }

    /* precondition, no \n in str */
    public void OnKeysTyped(string str)
    {
        if (isHighlighting)
            DeleteHighlight();
        lines[cursorRow] = lines[cursorRow].Insert(visibleCursorCol, str);
        visibleCursorCol += str.Length;
        cursorCol = visibleCursorCol;
    }

    void OnKeyPressed(char ch)
    {
        OnKeyPressed(ch, true);
    }

    void ApplyTransaction(Transaction t)
    {
        AdjustScrollToCursor();
        string beforeTransaction = String.Join("\n", mutations);
        if (t.IsMutation())
        {
            if (transactionPointer < mutations.Count - 1)
                mutations.RemoveRange(transactionPointer + 1, mutations.Count - transactionPointer - 1);
            mutations.Add(t);
            transactionPointer++;
        }
        t.Apply(this);
        AdjustScrollToCursor();
        UpdateConsole();
    }

    public void AdjustScrollToCursor()
    {
        if (verticalScroll + viewportHeight - 1 < cursorRow)
        {
            verticalScroll = cursorRow - viewportHeight + 1;
        }
        if (verticalScroll > cursorRow)
        {
            verticalScroll = cursorRow;
        }
        if (horizontalScroll + viewportWidth - 1 - GetLineCountPadding() < visibleCursorCol)
        {
            horizontalScroll = visibleCursorCol - viewportWidth + 1 + GetLineCountPadding();
        }
        if (horizontalScroll > visibleCursorCol - 4)
        {
            horizontalScroll = Mathf.Max(0, visibleCursorCol - 4);
        }
    }

    void OnKeyPressed(char ch, bool shouldUpdateConsole)
    {
        if (ch == (char)(0))
            return;
        AdjustScrollToCursor();
        ApplyTransaction(new InsertTransaction(ch + ""));
        if (shouldUpdateConsole)
            UpdateConsole();
    }

    public void DeleteRegion(int r1, int c1, int r2, int c2)
    {
        lines[r1] = lines[r1].Substring(0, c1) + lines[r2].Substring(c2 + 1);
        lines.RemoveRange(r1 + 1, r2 - r1);
    }

    void SetChar(int r, int c, char ch)
    {
        GameObject cell = chars[r, c];
        cell.GetComponent<ConsoleCharController>().UpdateText(ch + "");
    }

    void SetCellColor(int r, int c, Color color)
    {
        GameObject cell = chars[r, c];
        cell.GetComponent<ConsoleCharController>().UpdateColor(color);
    }

    void SetCellTextColor(int r, int c, Color color)
    {
        GameObject cell = chars[r, c];
        cell.GetComponent<ConsoleCharController>().UpdateTextColor(color);
    }

    void OnMouseUp()
    {
        Vector2Int cursorLocation = GetCursorLocationForMouse();
    }

    Vector2Int GetCursorLocationForMouse()
    {
        int r = Mathf.Max(0, Mathf.Min(lines.Count - 1 - verticalScroll, (int)((1 - mouseListener.currentMousePosition.y) * viewportHeight)));
        int c = Mathf.Max(0, Mathf.Min(lines[r + verticalScroll].Length, (int)(mouseListener.currentMousePosition.x * viewportWidth + .5) - GetLineCountPadding()));
        return new Vector2Int(r, c);
    }

    void OnMouseDown()
    {
        Vector2Int cursorLocation = GetCursorLocationForMouse();
        dragStart = new Vector2Int(cursorLocation.x + verticalScroll, cursorLocation.y + horizontalScroll);
        dragCurrent = new Vector2Int(cursorLocation.x + verticalScroll, cursorLocation.y + horizontalScroll);
        cursorRow = cursorLocation.x + verticalScroll;
        visibleCursorCol = cursorCol = cursorLocation.y + horizontalScroll;
        UpdateConsole();
    }

    //TODO: convert this to LPS (lines per second), and use this to recalculate verticalScroll?
    float GetTimeBetweenDragScroll()
    {
        float minDelay = 0;
        float maxDelay = .2f;
        float sensitivity = 1f;
        float upTrigger = .95f;
        float downTrigger = .05f;
        if (mouseListener.currentMousePosition.y > upTrigger)
        {
            return Mathf.Max(minDelay, maxDelay - (mouseListener.currentMousePosition.y - upTrigger) * sensitivity);
        }
        else if (mouseListener.currentMousePosition.y < downTrigger)
        {
            return Mathf.Max(minDelay, maxDelay - (downTrigger - mouseListener.currentMousePosition.y) * sensitivity);
        }
        return 1f;
    }

    void OnMouseDrag()
    {
        Vector2Int cursorLocation = GetCursorLocationForMouse();
        dragCurrent = new Vector2Int(cursorLocation.x + verticalScroll, cursorLocation.y + horizontalScroll);
        if (dragCurrent.x != dragStart.x || dragCurrent.y != dragStart.y)
            isHighlighting = true;
        UpdateConsole();
    }

    void UpdateHighlight()
    {
        int r1 = dragStart.x;
        int c1 = dragStart.y;
        int r2 = dragCurrent.x;
        int c2 = dragCurrent.y;
        if (dragCurrent.x < dragStart.x || (dragCurrent.x == dragStart.x && dragCurrent.y < dragStart.y))
        {
            r1 = dragCurrent.x;
            c1 = dragCurrent.y;
            r2 = dragStart.x;
            c2 = dragStart.y;
        }
        int r = r1;
        int c = c1;
        bool done = false;
        while (!done && (r != r2 || c != c2))
        {
            if (r > r2)
                break;
            if (r < lines.Count && c <= lines[r].Length)
            {
                int viewportR = r - verticalScroll;
                int viewportC = c - horizontalScroll + GetLineCountPadding();
                if (viewportR >= 0 && viewportR < viewportHeight && viewportC >= GetLineCountPadding() && viewportC < viewportWidth)
                    Highlight(viewportR, viewportC);
            }
            if (c <= lines[r].Length - 1)
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

    //precondition, r1 c1 comes before r2 c2
    string GetRegion(int r1, int c1, int r2, int c2)
    {
        Debug.Log("get region " + r1 + " " + c1 + " " + r2 + " " + c2);
        if (r1 == r2)
        {
            return lines[r1].Substring(c1, c2 - c1 + 1);
        }
        string region = "";
        region += lines[r1].Substring(c1);
        for (int i = r1 + 1; i < r2; i++)
            region += "\n" + lines[i];
        region += "\n" + lines[r2].Substring(0, c2 + 1);
        return region;
    }

    string GetHighlightedText()
    {
        int r1 = dragStart.x;
        int c1 = dragStart.y;
        int r2 = dragCurrent.x;
        int c2 = dragCurrent.y;
        if (dragCurrent.x < dragStart.x || (dragCurrent.x == dragStart.x && dragCurrent.y < dragStart.y))
        {
            r1 = dragCurrent.x;
            c1 = dragCurrent.y;
            r2 = dragStart.x;
            c2 = dragStart.y;
        }
        return GetRegion(r1, c1, r2, c2 - 1);
    }

    public string CaptureDeletion(bool isBackspace = true)
    {
        string deleted = "";
        if (isHighlighting)
        {
            deleted = GetHighlightedText();
            DeleteHighlight();
        }
        else if (isBackspace)
        {
            if (cursorCol != 0)
            {
                bool canBackspaceTab = CanBackspaceTab();
                do
                {
                    String line = lines[cursorRow];
                    deleted += line[visibleCursorCol - 1];
                    lines[cursorRow] = line.Substring(0, visibleCursorCol - 1) + line.Substring(visibleCursorCol);
                    cursorCol = --visibleCursorCol;
                } while (canBackspaceTab && visibleCursorCol % spacesPerTab != 0);
            }
            else if (cursorRow != 0)
            {
                int newCursorCol = lines[cursorRow - 1].Length;
                lines[cursorRow - 1] += lines[cursorRow];
                lines.RemoveAt(cursorRow);
                cursorRow--;
                visibleCursorCol = cursorCol = newCursorCol;
                deleted += '\n';
            }
        }
        else
        {
            if (visibleCursorCol != lines[cursorRow + verticalScroll].Length)
            {
                deleted += lines[cursorRow][visibleCursorCol];
                lines[cursorRow] = lines[cursorRow].Remove(visibleCursorCol, 1);
            }
            else if (cursorRow < lines.Count - 1)
            {
                deleted = "\n";
                lines[cursorRow] += lines[cursorRow + 1];
                lines.RemoveAt(cursorRow + 1);
            }
        }
        AdjustScrollToCursor();
        UpdateConsole();
        return deleted;
    }

    void DeleteHighlight()
    {
        if (!isHighlighting)
            return;
        int r1 = dragStart.x;
        int c1 = dragStart.y;
        int r2 = dragCurrent.x;
        int c2 = dragCurrent.y;
        if (dragCurrent.x < dragStart.x || (dragCurrent.x == dragStart.x && dragCurrent.y < dragStart.y))
        {
            r1 = dragCurrent.x;
            c1 = dragCurrent.y;
            r2 = dragStart.x;
            c2 = dragStart.y;
        }
        DeleteRegion(r1, c1, r2, c2 - 1);
        cursorRow = r1;
        cursorCol = visibleCursorCol = c1;
        ResetDragState();
        UpdateConsole();
    }

    void Highlight(int r, int c)
    {
        GameObject cell = chars[r, c];
        cell.GetComponent<ConsoleCharController>().UpdateColor(Color.white);
        cell.GetComponent<ConsoleCharController>().UpdateTextColor(Color.black);
    }

    private void ResetDragState()
    {
        isHighlighting = false;
        // Sync the drag anchors to the current cursor to prevent jumps
        dragStart = new Vector2Int(cursorRow, visibleCursorCol);
        dragCurrent = new Vector2Int(cursorRow, visibleCursorCol);
    }

    void OnScroll(float scrollInput)
    {

    }

    private bool IsPointerInsideThisConsole()
    {
        if (_outputVE == null || _outputVE.panel == null)
            return false;

        // Convert screen mouse position to panel coordinates
        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(_outputVE.panel, Input.mousePosition);

        // worldBound is in panel space (for runtime UI Toolkit)
        return _outputVE.worldBound.Contains(panelPos);
    }

    void Update()
    {
        // Escape should defocus all consoles and recapture mouse
        if (escapeDefocusesAll && Input.GetKeyDown(KeyCode.Escape))
        {
            DefocusAllAndRecaptureMouse(escapeCursorLockMode, escapeCursorVisible);
            return;
        }

        // Clicking outside this console should defocus it (even if you clicked another console or empty space)
        if (isFocused && Input.GetMouseButtonDown(0))
        {
            if (!IsPointerInsideThisConsole())
            {
                DefocusInternal();
            }
        }

        if (!isFocused)
            return;

        if (mouseListener.isMouseDragging && mouseListener.currentMousePosition.y > 1 && Time.time > lastDragScrollTime + GetTimeBetweenDragScroll())
        {
            MaybeUpScroll();
            Vector2Int cursorLocation = GetCursorLocationForMouse();
            dragCurrent = new Vector2Int(cursorLocation.x + verticalScroll, cursorLocation.y);
            lastDragScrollTime = Time.time;
        }
        if (mouseListener.isMouseDragging && mouseListener.currentMousePosition.y < 0 && Time.time > lastDragScrollTime + GetTimeBetweenDragScroll())
        {
            MaybeDownScroll();
            lastDragScrollTime = Time.time;
        }
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            if (scrollInput > 0)
            {
                MaybeUpScroll();
            }
            else
            {
                MaybeDownScroll();
            }
        }

        HashSet<char> excluded = new HashSet<char>() { (char)(8), (char)(10), (char)(13), 'c', 'C', 'v', 'V', 'x', 'X', 'y', 'Y', 'z', 'Z', 'a', 'A', 's', 'S', 'l', 'L' };
        foreach (char ch in Input.inputString)
        {
            if (!excluded.Contains(ch))
            {
                OnKeyPressed(ch);
                latest = KeyCode.None;
            }
        }

        if (!Input.GetKey(latest))
            isKeyHeld = false;

        foreach (KeyCode specialKeyCode in specialKeyPressHandlers.Keys)
        {
            if (Input.GetKeyDown(specialKeyCode))
            {
                if (specialKeyCode != latest)
                    isKeyHeld = false;
                specialKeyPressHandlers[specialKeyCode].Invoke();
                latest = specialKeyCode;
                latestDownTime = Time.time;
            }
            if (Input.GetKey(specialKeyCode))
            {
                if (specialKeyCode == latest && !isKeyHeld && Time.time - latestDownTime >= heldKeyTriggerTime)
                {
                    isKeyHeld = true;
                    lastHeldKeyTriggerTime = Time.time;
                }
                else if (specialKeyCode == latest && isKeyHeld && Time.time - lastHeldKeyTriggerTime > heldKeyDelay)
                {
                    specialKeyPressHandlers[specialKeyCode].Invoke();
                    lastHeldKeyTriggerTime = Time.time;
                }
            }
        }
        UpdateCursor();
    }
}
