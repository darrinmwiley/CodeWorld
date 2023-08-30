using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ConsoleController : MonoBehaviour
{
    //TODO any character that isn't in in KeyCodes should be tracked for hold too
    //TODO tabs
        //write in spaces, delete in tabs (when applicable)
    //TODO Mouse input
        //highlighting
    //TODO CRTLY + CTRLZ
    //TODO CTRLX + CTRLC + CTRLV

    public GameObject consoleCharPrefab;
    public GameObject cursorPrefab;

    public int terminalWidth = 80;
    public int terminalHeight = 24;
    RenderTexture renderTexture;

    GameObject[,] chars;
    GameObject cursor;

    List<string> lines = new List<string>();
    KeyListener keyListener;

    int cursorRow = 0;
    int cursorCol = 0;
    int visibleCursorCol = 0;

    public float heldKeyDelay = .01f;
    public float heldKeyTriggerTime = .5f;
    public bool isKeyHeld = false;
    float lastHeldKeyTriggerTime;

    KeyInfo previousHeldKey = null;

    // Start is called before the first frame update
    void Start()
    {
        keyListener = gameObject.GetComponent<KeyListener>();
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

    //TODO ON KEY PRESS PULL UP A TERMINAL IN UI SPACE (camera stacking?)
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
        string beginningOfLine = lines[cursorRow].Substring(0,visibleCursorCol);  
        string restOfLine = lines[cursorRow].Substring(visibleCursorCol); 

        lines[cursorRow] = beginningOfLine;
        lines.Insert(cursorRow + 1, restOfLine);
        cursorRow++;
        visibleCursorCol = cursorCol = 0;
        UpdateConsole();
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

    public void OnBackspacePressed()
    {
        if(cursorCol != 0)
        {
            SetChar(cursorRow, cursorCol + GetLineCountPadding() - 1, ' ');
            String line = lines[cursorRow];
            lines[cursorRow] = line.Substring(0,cursorCol - 1) + line.Substring(cursorCol);
            visibleCursorCol = --cursorCol;
        }else if(cursorRow != 0){
            int newCursorCol = lines[cursorRow - 1].Length;
            lines[cursorRow - 1] += lines[cursorRow];
            lines.RemoveAt(cursorRow);
            cursorRow--;
            visibleCursorCol = cursorCol = newCursorCol;
        }
        UpdateConsole();
        //TODO cut out one char, if line is empty cut out the line instead
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

    void OnKeyPressed(KeyInfo keyInfo)
    {
        if(keyInfo.Code.HasValue){
            if(specialKeyPressHandlers.ContainsKey(keyInfo.Code.Value))
            {
                specialKeyPressHandlers[keyInfo.Code.Value].Invoke();
            }else if (KeyListener.KeyCodeToChar(keyInfo.Code.Value) != 0)
            {
                OnKeyPressed(keyInfo.ToChar());
            }
        }else{

        }
        
    }

    void OnKeyPressed(char ch)
    {
        //todo handle TAB press
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
        specialKeyPressHandlers[KeyCode.Backspace] = OnBackspacePressed;
        specialKeyPressHandlers[KeyCode.LeftArrow] = OnLeftArrowPressed;
        specialKeyPressHandlers[KeyCode.RightArrow] = OnRightArrowPressed;
        specialKeyPressHandlers[KeyCode.UpArrow] = OnUpArrowPressed;
        specialKeyPressHandlers[KeyCode.DownArrow] = OnDownArrowPressed;
    }

    KeyCode? GetSpecialKeyPressed()
    {
        foreach(KeyCode keycode in specialKeyPressHandlers.Keys)
        {
            if(Input.GetKey(keycode))
                return keycode;
        }
        return null;
    }

    public void HandleHeldKey()
    {
        KeyInfo latest = keyListener.GetLatestKeyPress();
        if(latest != null)
        {
            if(!isKeyHeld && latest.IsKeyDown && Time.time - latest.LastDownTime > heldKeyTriggerTime)
            {
                isKeyHeld = true;
            }
            if(!latest.IsKeyDown)
            {
                isKeyHeld = false;
            }
            if(latest != previousHeldKey)
            {
                isKeyHeld = false;
            }
            if(isKeyHeld)
            {
                if(Time.time - lastHeldKeyTriggerTime > heldKeyDelay)
                {
                    lastHeldKeyTriggerTime = Time.time;
                    OnKeyPressed(latest);
                }
            }
        }
        previousHeldKey = latest;
    }

    //TODO package KeyListener into GameSkeleton
    // Update is called once per frame
    void Update()
    {
        HandleHeldKey();
        UpdateCursor();
        // Check for any input from the user

        if (!isKeyHeld)
        {
            foreach(KeyInfo keyInfo in keyListener.queue)
            {
                OnKeyPressed(keyInfo);
            }
        }
    }
}
