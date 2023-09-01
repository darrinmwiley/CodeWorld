using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ConsoleController : MonoBehaviour
{
    //TODO Cosmetic
        //console frame
        //menu options (save, load, new, font size)
    //TODO Mouse input
        //clickable scrollbar bottom and right when applicable
        //resize on edges
        //drag on top
        //highlighting elsewhere
    //TODO CTRLC, CTRLV, CTRLX, CRTLY, CTRLZ
    //rework the console to be driven by font size and dispay as many lines as will fit
    //any roundoff padding we can just give to the line number padding or similar


    public GameObject consoleCharPrefab;
    public GameObject cursorPrefab;
    public MouseListener mouseListener;

    public int terminalWidth = 80;
    public int terminalHeight = 24;
    public int spacesPerTab = 4;
    RenderTexture renderTexture;

    GameObject[,] chars;
    GameObject cursor;

    List<string> lines = new List<string>();

    int cursorRow = 0;
    int cursorCol = 0;
    int visibleCursorCol = 0;

    // Start is called before the first frame update
    void Start()
    {
        mouseListener.AddMouseDownHandler(OnMouseDown);
        mouseListener.AddMouseUpHandler(OnMouseUp);
        mouseListener.AddMouseDragHandler(OnMouseDrag);
        InitKeyHandlers();
        lines.Add("");
        Generate();
        UpdateConsole();
    }

    //first, we will simply make it as many lines as we can hold. Scrolling to be added later
    void UpdateConsole()
    {
        UpdateLineNumbers();
        UpdateLines();
    }

    void UpdateCursor()
    {
        int trueCursorCol = visibleCursorCol + GetLineCountPadding();
        cursor.transform.position = new Vector3((trueCursorCol * 3f / 5f) + .05f, terminalHeight - 1 - cursorRow + .5f, -.1f);
    }

    int GetLineCountPadding(){
        return (lines.Count+"").Length + 2;
    }

    public GameObject SpawnConsoleChar()
    {
        return Instantiate(consoleCharPrefab, new Vector3Int(0,0,0), Quaternion.identity);
    }

    //ALLOW CORNER RESIZE BUT MAINTAIN ASPECT RATIO
    //TODO RECIEVE KEY PRESS EVENTS
    //PORT IN EVENT LISTENER IDEA (SUBSCRIBE TO EVENT TYPE USING ?DELEGATES?)

    //IDEA WRAPPER ON CONSOLE THAT CAN ALSO OPEN FILES, SCROLL, CLICK CURSOR, ETC

    //NEED FILESYSTEM

    //IDEA EACH OBJECT GETS ITS OWN SCRIPTS SUBDIRECTORY 

    // Function to create a RenderTexture with the same aspect ratio as the input camera
    public RenderTexture CreateRenderTexture(Camera camera, int renderTextureWidth)
    {
        // Calculate the aspect ratio based on the camera's properties
        float aspectRatio = camera.aspect;

        // Define the desired resolution (you can adjust this as needed)
        int renderTextureHeight = Mathf.RoundToInt(renderTextureWidth / aspectRatio); // Calculate height based on aspect ratio

        // Create a RenderTexture with the calculated resolution
        RenderTexture renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24);
        renderTexture.name = "CameraRenderTexture"; // Set a name for the RenderTexture
        renderTexture.antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing); // Use the camera's anti-aliasing settings or default to 1
        renderTexture.filterMode = FilterMode.Bilinear; // You can adjust the filter mode as needed

        // Set the camera's target texture to the newly created RenderTexture
        camera.targetTexture = renderTexture;

        // Return the created RenderTexture
        return renderTexture;
    }

    public void Generate()
    {
        Transform charsTransform = gameObject.transform.Find("chars");
        GameObject charsGO = charsTransform == null ? new GameObject("chars") : charsTransform.gameObject;
        Transform cameraTransform = gameObject.transform.Find("camera");
        GameObject cameraGO = cameraTransform == null ? new GameObject("camera") : cameraTransform.gameObject;
        charsGO.transform.parent = gameObject.transform;
        cameraGO.transform.parent = gameObject.transform;
        for (int i = transform.Find("chars").childCount - 1; i >= 0; i--)
        {
            Transform child = charsTransform.GetChild(i);
            DestroyImmediate(child.gameObject);
        }

        chars = new GameObject[terminalHeight, terminalWidth];

        for(int i = 0;i<terminalWidth;i++)
        {
            for(int j = 0;j<terminalHeight;j++)
            {
                GameObject cube = SpawnConsoleChar();
                cube.transform.parent = charsGO.transform;
                cube.transform.position = new Vector3((i * 3f / 5f) + .3f, terminalHeight - 1 - j + .5f, 0);
                chars[j,i] = cube;
            }
        }

        cursor = Instantiate(cursorPrefab, new Vector3Int(0,0,0), Quaternion.identity);
        cursor.transform.position = new Vector3((3 * 3f / 5f) + .05f, terminalHeight - 1 + .5f, -.1f);

        Bounds bounds = GetBounds();
        Camera camera = cameraGO.GetComponent<Camera>();
        if(camera == null)
            camera = cameraGO.AddComponent<Camera>();
        SetCamera(camera, bounds.min.x, bounds.max.y, bounds.max.x, bounds.min.y);

        int textureWidth = 1000;

        RenderTexture renderTexture = CreateRenderTexture(camera, textureWidth);

        //QUICKCODE SKETCH
        Transform UiTransform = transform.parent;
        Transform canvasTransform = UiTransform.Find("Canvas");
        RawImage rawImage = canvasTransform.Find("RawImage").GetComponent<RawImage>();
        rawImage.texture = renderTexture;
        float height = textureWidth * ((renderTexture.height + 0f) / renderTexture.width);
        RectTransform rectTransform = rawImage.rectTransform;
        rectTransform.sizeDelta = new Vector2(textureWidth, height);
    }

    public Bounds GetBounds()
    {
        // Get the initial bounds from the root GameObject.
        Bounds bounds = new Bounds(transform.position, Vector3.zero);

        // Iterate through all renderers in the hierarchy.
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

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
        Debug.Log("on return pressed");
        string beginningOfLine = lines[cursorRow].Substring(0,visibleCursorCol);  
        string restOfLine = lines[cursorRow].Substring(visibleCursorCol); 

        lines[cursorRow] = beginningOfLine;
        lines.Insert(cursorRow + 1, restOfLine);
        cursorRow++;
        visibleCursorCol = cursorCol = 0;
        UpdateConsole();
    }

    public void OnTabPressed()
    {
        do{
            OnKeyPressed(' ');
        }while(visibleCursorCol % spacesPerTab != 0);
    }

    void UpdateLines(){
        //TODO make padding a var instead of a method
        int padding = GetLineCountPadding();
        for(int i = 0;i<terminalHeight;i++)
        {
            for(int j = padding;j<terminalWidth;j++)
            {
                if(lines.Count > i && lines[i].Length > j - padding)
                {
                    SetCellColor(i,j,Color.black);
                    SetChar(i,j,lines[i][j - padding]);
                }else{
                    SetCellColor(i,j,Color.black);
                    SetChar(i,j,' ');
                }
            }
        }
    }

    void UpdateLineNumbers()
    {
        int maxLineNumberLength = (""+(lines.Count)).Length;
        int totalLineNumberLength = maxLineNumberLength + 2;

        for(int i = 0;i<terminalHeight;i++)
        {
            if(i < lines.Count)
            {
                string lineNumber = (i + 1 + "");
                for(int j = 0;j<totalLineNumberLength;j++)
                {
                    SetCellColor(i,j,new Color(.15f,.15f,.15f));
                    SetChar(i,j,' ');
                }
                for(int j = 0;j<lineNumber.Length;j++)
                {
                    int currentCol = totalLineNumberLength - 2 - j;
                    char currentChar = lineNumber[lineNumber.Length - 1 - j];
                    SetChar(i,currentCol, currentChar);
                }
            }else{
                for(int j = 0;j<totalLineNumberLength;j++)
                {
                    SetCellColor(i,j,Color.black);
                    SetChar(i,j,' ');
                }
            }
        }
    }

    //you can backtab if there is nothing but spaces behind you
    //TODO: optimize this to O(1) by keeping track of index of first non-space char of each line
    public bool CanBackspaceTab()
    {
        if(visibleCursorCol == 0)
            return false;
        for(int i = 0;i<visibleCursorCol;i++)
        {
            if(lines[cursorRow][i] != ' ')
                return false;
        }
        return true;
    }

    public void OnBackspacePressed()
    {
        if(cursorCol != 0)
        {
            bool canBackspaceTab = CanBackspaceTab();
            do{
                SetChar(cursorRow, visibleCursorCol + GetLineCountPadding() - 1, ' ');
                String line = lines[cursorRow];
                lines[cursorRow] = line.Substring(0,visibleCursorCol - 1) + line.Substring(visibleCursorCol);
                cursorCol = --visibleCursorCol;
            }while(canBackspaceTab && visibleCursorCol % spacesPerTab != 0);
            
        }else if(cursorRow != 0){
            int newCursorCol = lines[cursorRow - 1].Length;
            lines[cursorRow - 1] += lines[cursorRow];
            lines.RemoveAt(cursorRow);
            cursorRow--;
            visibleCursorCol = cursorCol = newCursorCol;
        }
        UpdateConsole();
    }

    public void OnUpArrowPressed()
    {
        cursorRow = Mathf.Max(0,cursorRow - 1);
        visibleCursorCol = Mathf.Min(cursorCol, lines[cursorRow].Length);
    }

    public void OnDownArrowPressed()
    {
        cursorRow = Mathf.Min(lines.Count - 1, cursorRow + 1);
        visibleCursorCol = Mathf.Min(cursorCol, lines[cursorRow].Length);
    }

    public void OnLeftArrowPressed()
    {
        if(visibleCursorCol != 0)
            visibleCursorCol--;
        else if(cursorRow != 0)
        {
            cursorRow--;
            visibleCursorCol = lines[cursorRow].Length;
        }
        cursorCol = visibleCursorCol;
    }

    public void OnRightArrowPressed()
    {
        if(visibleCursorCol != lines[cursorRow].Length)
            visibleCursorCol++;
        else if(cursorRow != lines.Count - 1)
        {
            cursorRow++;
            visibleCursorCol = 0;
        }
        cursorCol = visibleCursorCol;
    }

    void OnKeyPressed(char ch)
    {
        if(ch == (char)(0))
            return;
        lines[cursorRow] = lines[cursorRow].Insert(visibleCursorCol,ch+"");
        UpdateLines();
        cursorCol = ++visibleCursorCol;
    }

    void SetChar(int r, int c, char ch)
    {
        GameObject cell = chars[r,c];
        cell.GetComponent<ConsoleCharController>().UpdateText(ch+"");
    }

    void SetCellColor(int r, int c, Color color)
    {
        GameObject cell = chars[r,c];
        cell.GetComponent<ConsoleCharController>().UpdateColor(color);
    }

    Dictionary<KeyCode, Action> specialKeyPressHandlers;
    
    void InitKeyHandlers(){
        specialKeyPressHandlers = new Dictionary<KeyCode, Action>();
        specialKeyPressHandlers[KeyCode.Return] = OnReturnPressed;
        specialKeyPressHandlers[KeyCode.KeypadEnter] = OnReturnPressed;
        specialKeyPressHandlers[KeyCode.Backspace] = OnBackspacePressed;
        specialKeyPressHandlers[KeyCode.LeftArrow] = OnLeftArrowPressed;
        specialKeyPressHandlers[KeyCode.RightArrow] = OnRightArrowPressed;
        specialKeyPressHandlers[KeyCode.UpArrow] = OnUpArrowPressed;
        specialKeyPressHandlers[KeyCode.DownArrow] = OnDownArrowPressed;
        specialKeyPressHandlers[KeyCode.Tab] = OnTabPressed;
    }

    KeyCode latest = KeyCode.None;
    float latestDownTime = 0;
    bool isKeyHeld = false;
    public float heldKeyDelay = .005f;
    public float heldKeyTriggerTime = .4f;
    float lastHeldKeyTriggerTime = 0;

    void OnMouseUp(MouseListener mouseListener)
    {
        Debug.Log("mouse up");
    }

    void OnMouseDown(MouseListener mouseListener)
    {
        Debug.Log("mouse down");
        int clickCol = (int)(mouseListener.currentMousePosition.x * terminalWidth);
        int clickRow = (int)((1 - mouseListener.currentMousePosition.y) * terminalHeight);
        SetCellColor(clickRow, clickCol, Color.red);
    }

    void OnMouseDrag(MouseListener mouseListener)
    {
        Debug.Log("drag");
    }

    void Update()
    {
        HashSet<char> excluded = new HashSet<char>(){(char)(8), (char)(13)};
        foreach(char ch in Input.inputString)
        {
            if(!excluded.Contains(ch)){
                Debug.Log("input: "+(int)(ch));
                OnKeyPressed(ch);
                latest = KeyCode.None;
            }
        }

        if(!Input.GetKey(latest))
            isKeyHeld = false;

        foreach(KeyCode specialKeyCode in specialKeyPressHandlers.Keys)
        {
            if(Input.GetKeyDown(specialKeyCode))
            {
                if(specialKeyCode != latest)
                    isKeyHeld = false;
                specialKeyPressHandlers[specialKeyCode].Invoke();
                latest = specialKeyCode;
                latestDownTime = Time.time;
            }
            if(Input.GetKey(specialKeyCode)){
                if(specialKeyCode == latest && !isKeyHeld && Time.time - latestDownTime >= heldKeyTriggerTime)
                {
                    isKeyHeld = true;
                    lastHeldKeyTriggerTime = Time.time;
                }else if(specialKeyCode == latest && isKeyHeld && Time.time - lastHeldKeyTriggerTime > heldKeyDelay)
                {
                    specialKeyPressHandlers[specialKeyCode].Invoke();
                    lastHeldKeyTriggerTime = Time.time;
                }
            }
        }
        UpdateCursor();
    }
}
