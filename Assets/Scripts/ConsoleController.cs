using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ConsoleController : MonoBehaviour
{
    /*TODO: 
        0) absolute cursor position as source of truth (will probs be helpful in horz scrolling as well)
        1) figure out best place to decide to rubber band back to cursor position
            before and after every transaction?
            maybe it would be better to not pass the state, it's public anyways so just let the transaction grab it
            then we coudl put that in applyTransaction
        2) horizontal scrolling
        3) CTRL+ENTER presses enter twice

    //TODO Cosmetic
        //console frame
        //menu options (save, load, new, font size)
        //clickable scrollbar bottom and right when applicable
        //resize on edges
        //drag on top   
        //font size
        //source tree parsing
    */

    public GameObject consoleCharPrefab;
    public GameObject cursorPrefab;
    public MouseListener mouseListener;
    public int viewportWidth = 80;
    public int viewportHeight = 24;
    public int spacesPerTab = 4;
    public float heldKeyDelay = .005f;
    public float heldKeyTriggerTime = .4f;
    public List<string> lines = new List<string>();

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

    string copyBuffer;

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

    public ConsoleState GetState(){
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
        if(transactionPointer < 0)
            return;
        Transaction previous = mutations[transactionPointer];
        previous.Revert(this);
        transactionPointer--;
        AdjustVerticalScrollToCursor();
        UpdateConsole();
    }

    public void Redo()
    {
        if(transactionPointer >= mutations.Count - 1)
            return;
        Transaction next = mutations[transactionPointer+1];
        next.Apply(this);
        transactionPointer++;
        UpdateConsole();
        AdjustVerticalScrollToCursor();
        AdjustVerticalScrollToCursor();
    }

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
        specialKeyPressHandlers[KeyCode.C] = OnCKeyPressed;
        specialKeyPressHandlers[KeyCode.V] = OnVKeyPressed;
        specialKeyPressHandlers[KeyCode.Z] = OnZKeyPressed;
        specialKeyPressHandlers[KeyCode.Y] = OnYKeyPressed;
        specialKeyPressHandlers[KeyCode.Z] = OnZKeyPressed;
        specialKeyPressHandlers[KeyCode.X] = OnXKeyPressed;
        specialKeyPressHandlers[KeyCode.Delete] = OnDeletePressed;
        specialKeyPressHandlers[KeyCode.A] = OnAKeyPressed;
    }

    //first, we will simply make it as many lines as we can hold. Scrolling to be added later
    void UpdateConsole()
    {
        UpdateLineNumbers();
        UpdateLines();
        if(isHighlighting)
            UpdateHighlight();
    }

    void UpdateCursor()
    {
        int trueCursorCol = visibleCursorCol + GetLineCountPadding();
        cursor.transform.position = new Vector3((trueCursorCol * 3f / 5f) + .05f, verticalScroll + viewportHeight - 1 - cursorRow + .5f, -.1f);
    }

    public int GetLineCountPadding(){
        return (lines.Count+"").Length + 2;
    }

    public GameObject SpawnConsoleChar()
    {
        return Instantiate(consoleCharPrefab, new Vector3Int(0,0,0), Quaternion.identity);
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

        chars = new GameObject[viewportHeight, viewportWidth];

        for(int i = 0;i<viewportWidth;i++)
        {
            for(int j = 0;j<viewportHeight;j++)
            {
                GameObject cube = SpawnConsoleChar();
                cube.transform.parent = charsGO.transform;
                cube.transform.position = new Vector3((i * 3f / 5f) + .3f, viewportHeight - 1 - j + .5f, 0);
                chars[j,i] = cube;
            }
        }

        cursor = Instantiate(cursorPrefab, new Vector3Int(0,0,0), Quaternion.identity);
        cursor.transform.position = new Vector3((3 * 3f / 5f) + .05f, viewportHeight - 1 + .5f, -.1f);

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
        AdjustVerticalScrollToCursor();
        ApplyTransaction(new NewlineTransaction());
        UpdateConsole();
    }

    public void NewLine(){
        if(isHighlighting)
            DeleteHighlight();
        lines[cursorRow] = lines[cursorRow].Substring(0,visibleCursorCol);
        lines.Insert(cursorRow + 1, lines[cursorRow].Substring(visibleCursorCol));
        cursorRow++;
        visibleCursorCol = cursorCol = 0;
        AdjustVerticalScrollToCursor();
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
        for(int i = 0;i<numSpaces;i++)
            tab += " ";
        AdjustVerticalScrollToCursor();
        ApplyTransaction(new InsertTransaction(tab));
    }

    void UpdateLines(){
        int padding = GetLineCountPadding();
        for(int i = 0;i<viewportHeight;i++){
            for(int j = padding;j<viewportWidth;j++){
                SetCellColor(i,j,Color.black);
                SetCellTextColor(i,j,Color.white);
                int lineNumber = i + verticalScroll;
                if(lines.Count > lineNumber && lines[lineNumber].Length > j - padding){
                    SetChar(i,j,lines[lineNumber][j - padding]);
                }else{
                    SetChar(i,j,' ');
                }
            }
        }
    }

    void UpdateLineNumbers()
    {
        int maxLineNumberLength = (""+(lines.Count)).Length;
        int totalLineNumberLength = maxLineNumberLength + 2;

        for(int i = 0;i<viewportHeight;i++)
        {
            if(verticalScroll + i < lines.Count)
            {
                string lineNumber = (verticalScroll + i + 1 + "");
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
        if(cursorRow == 0)
            return;
        cursorRow--;
        visibleCursorCol = Mathf.Min(cursorCol, lines[cursorRow].Length);
        AdjustVerticalScrollToCursor();
        UpdateConsole();
    }

    public void OnDownArrowPressed()
    {
        isHighlighting = false;
        if(cursorRow >= lines.Count - 1)
            return;
        cursorRow++;
        visibleCursorCol = Mathf.Min(cursorCol, lines[cursorRow].Length);
        AdjustVerticalScrollToCursor();
        UpdateConsole();
    }

    public void MaybeDownScroll()
    {
        if(verticalScroll < lines.Count - 1)
            verticalScroll++;
        UpdateConsole();
    }

    public void MaybeUpScroll()
    {
        if(verticalScroll != 0)
            verticalScroll--;
        UpdateConsole();
    }

    public void OnLeftArrowPressed()
    {
        isHighlighting = false;
        if(visibleCursorCol != 0)
            visibleCursorCol--;
        else if(cursorRow != 0)
        {
            cursorRow--;
            visibleCursorCol = lines[cursorRow].Length;
        }
        cursorCol = visibleCursorCol;
        AdjustVerticalScrollToCursor();
        UpdateConsole();
    }

    public void OnRightArrowPressed()
    {
        isHighlighting = false;
        if(visibleCursorCol != lines[cursorRow].Length)
            visibleCursorCol++;
        else {
            if(cursorRow < lines.Count - 1)
            {
                cursorRow++;
                visibleCursorCol = 0;
            }
        }
        cursorCol = visibleCursorCol;
        AdjustVerticalScrollToCursor();
        UpdateConsole();
    }

    bool IsUpperCase()
    {
        return (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) ^ (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
    }

    

    public void OnCKeyPressed()
    {
        if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            copyBuffer = GetHighlightedText();
        }else{
            if(IsUpperCase())
                OnKeyPressed('C');
            else
                OnKeyPressed('c');
        }
    }

    public void OnVKeyPressed()
    {
        if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            AdjustVerticalScrollToCursor();
            ApplyTransaction(new InsertTransaction(copyBuffer));
            UpdateConsole();
        }else{
            if(IsUpperCase())
                OnKeyPressed('V');
            else 
                OnKeyPressed('v');
        }
    }

    //todo, if no highlight, this shouldn't delete anything
    public void OnXKeyPressed()
    {
        if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if(isHighlighting){
                copyBuffer = GetHighlightedText();
                AdjustVerticalScrollToCursor();
                ApplyTransaction(new DeleteTransaction(true));
                UpdateConsole();
            }
            isHighlighting = false;
        }else if(IsUpperCase()){
            OnKeyPressed('X');
        }else{
            OnKeyPressed('x');
        }
    }

    public void OnYKeyPressed()
    {
        if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            Redo();
        }else if(IsUpperCase()){
            OnKeyPressed('Y');
        }else{
            OnKeyPressed('y');
        }
    }

    public void OnZKeyPressed()
    {
        if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            Undo();
        }else if(IsUpperCase()){
            OnKeyPressed('Z');
        }else{
            OnKeyPressed('z');
        }
    }
    
    public void SelectAll()
    {
        isHighlighting = true;
        dragStart = new Vector2Int(0,0);
        dragCurrent = new Vector2Int(lines.Count - 1, lines[lines.Count - 1].Length);
        UpdateHighlight();
    }

    public void OnAKeyPressed()
    {
        if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            SelectAll();
        }else if(IsUpperCase()){
            OnKeyPressed('A');
        }else{
            OnKeyPressed('a');
        }
    }


    public void InsertLines(string[] strs)
    {
        if(isHighlighting)
            DeleteHighlight();
        for(int i = 0;i<strs.Length;i++)
        {
            string line = strs[i];
            OnKeysTyped(line);
            if(i != strs.Length - 1)
                NewLine();
        }
    }

    /* precondition, no \n in str */
    public void OnKeysTyped(string str)
    {
        if(isHighlighting)
            DeleteHighlight();
        lines[cursorRow] = lines[cursorRow].Insert(visibleCursorCol,str);
        visibleCursorCol += str.Length;
        cursorCol = visibleCursorCol;
    }

    void OnKeyPressed(char ch){
        OnKeyPressed(ch, true);
    }

    void ApplyTransaction(Transaction t)
    {
        AdjustVerticalScrollToCursor();
        string beforeTransaction = String.Join("\n", mutations);
        if(t.IsMutation())
        {
            if(transactionPointer < mutations.Count - 1)
                mutations.RemoveRange(transactionPointer + 1, mutations.Count - transactionPointer - 1);
            mutations.Add(t);
            transactionPointer++;
        }
        t.Apply(this);
    }

    public void AdjustVerticalScrollToCursor()
    {
        if(verticalScroll + viewportHeight - 1 < cursorRow)
        {
            verticalScroll = cursorRow - viewportHeight + 1;
        }
        if(verticalScroll > cursorRow)
        {
            verticalScroll = cursorRow;
        }
    }

    void OnKeyPressed(char ch, bool shouldUpdateConsole)
    {
        if(ch == (char)(0))
            return;
        AdjustVerticalScrollToCursor();
        ApplyTransaction(new InsertTransaction(ch+""));
        if(shouldUpdateConsole)
            UpdateConsole();
    }

    public void DeleteRegion(int r1, int c1, int r2, int c2)
    {
        lines[r1] = lines[r1].Substring(0,c1) + lines[r2].Substring(c2+1);
        lines.RemoveRange(r1 + 1, r2 - r1);
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

    void SetCellTextColor(int r, int c, Color color)
    {
        GameObject cell = chars[r,c];
        cell.GetComponent<ConsoleCharController>().UpdateTextColor(color);
    }

    void OnMouseUp(MouseListener mouseListener)
    {
        Vector2Int cursorLocation = GetCursorLocationForMouse();
    }

    Vector2Int GetCursorLocationForMouse()
    {
        int r = Mathf.Max(0,Mathf.Min(lines.Count - 1 - verticalScroll,(int)((1 - mouseListener.currentMousePosition.y) * viewportHeight)));
        int c = Mathf.Max(0,Mathf.Min(lines[r + verticalScroll].Length,(int)(mouseListener.currentMousePosition.x * viewportWidth + .5) - GetLineCountPadding()));
        return new Vector2Int(r, c);
    }

    void OnMouseDown(MouseListener mouseListener)
    {
        Vector2Int cursorLocation = GetCursorLocationForMouse();
        dragStart = new Vector2Int(cursorLocation.x + verticalScroll, cursorLocation.y);
        dragCurrent = new Vector2Int(cursorLocation.x + verticalScroll, cursorLocation.y);
        cursorRow = cursorLocation.x + verticalScroll;
        visibleCursorCol = cursorCol = cursorLocation.y;
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
        if(mouseListener.currentMousePosition.y > upTrigger)
        {
            return Mathf.Max(minDelay, maxDelay - (mouseListener.currentMousePosition.y - upTrigger) * sensitivity);
        }else if(mouseListener.currentMousePosition.y < downTrigger){
            return Mathf.Max(minDelay, maxDelay - (downTrigger - mouseListener.currentMousePosition.y) * sensitivity);
        }
        return 1f;
    }

    void OnMouseDrag(MouseListener mouseListener)
    {
        Vector2Int cursorLocation = GetCursorLocationForMouse();
        dragCurrent = new Vector2Int(cursorLocation.x + verticalScroll, cursorLocation.y);
        if(dragCurrent.x != dragStart.x || dragCurrent.y != dragStart.y)
            isHighlighting = true;
        UpdateConsole();
    }

    void UpdateHighlight()
    {
        int r1 = dragStart.x;
        int c1 = dragStart.y;
        int r2 = dragCurrent.x;
        int c2 = dragCurrent.y;
        if(dragCurrent.x < dragStart.x || (dragCurrent.x == dragStart.x && dragCurrent.y < dragStart.y))
        {
            r1 = dragCurrent.x;
            c1 = dragCurrent.y;
            r2 = dragStart.x;
            c2 = dragStart.y;
        } 
        int r = r1;
        int c = c1;
        bool done = false;
        while(!done && (r != r2 || c != c2))
        {
            if(c <= lines[r].Length){
                int viewportR = r - verticalScroll;
                if(viewportR >= 0 && viewportR < viewportHeight)
                    Highlight(viewportR, c + GetLineCountPadding());
            }  
            if(c <= lines[r].Length - 1){
                c++;
            }else{
                if(r == r2 && c == c2 - 1){
                    done = true;
                    break;
                }
                c = 0;
                r++;
            }
        }
    }

    //precondition, r1 c1 comes before r2 c2
    string GetRegion(int r1, int c1, int r2, int c2){
        if(r1 == r2){
            return lines[r1].Substring(c1, c2 - c1 + 1);
        }
        string region = "";
        region += lines[r1].Substring(c1);
        for(int i = r1 + 1;i<r2;i++)
            region += "\n" + lines[i];
        region += "\n" + lines[r2].Substring(0,c2 + 1);
        return region;
    }

    string GetHighlightedText(){
        int r1 = dragStart.x;
        int c1 = dragStart.y;
        int r2 = dragCurrent.x;
        int c2 = dragCurrent.y;
        if(dragCurrent.x < dragStart.x || (dragCurrent.x == dragStart.x && dragCurrent.y < dragStart.y))
        {
            r1 = dragCurrent.x;
            c1 = dragCurrent.y;
            r2 = dragStart.x;
            c2 = dragStart.y;
        } 
        return GetRegion(r1,c1,r2,c2 - 1);
        /*
        int r = r1;
        int c = c1;
        bool done = false;
        string highlightedText = "";
        while(!done && (r != r2 || c != c2))
        {
            if(c < lines[r].Length){
                int viewportR = r - verticalScroll;
                if(viewportR >= 0 && viewportR < viewportHeight){
                    highlightedText += chars[viewportR, c + GetLineCountPadding()].GetComponent<ConsoleCharController>().GetChar();
                }
            }  
            if(c <= lines[r].Length - 1){
                c++;
            }else{
                if(r == r2 && c == c2 - 1){
                    done = true;
                    break;
                }
                c = 0;
                r++;
                highlightedText += "\n";
            }
        }
        return highlightedText;*/
    }

    public string CaptureDeletion(bool isBackspace = true)
    {
        string deleted = "";
        if(isHighlighting)
        {
            deleted = GetHighlightedText();
            DeleteHighlight();
        }else if(isBackspace)
        {
            if(cursorCol != 0)
            {
                bool canBackspaceTab = CanBackspaceTab();
                do{
                    SetChar(cursorRow - verticalScroll, visibleCursorCol + GetLineCountPadding() - 1, ' ');
                    String line = lines[cursorRow];
                    deleted += line[visibleCursorCol - 1];
                    lines[cursorRow] = line.Substring(0,visibleCursorCol - 1) + line.Substring(visibleCursorCol);
                    cursorCol = --visibleCursorCol;
                }while(canBackspaceTab && visibleCursorCol % spacesPerTab != 0);
            }else if(cursorRow != 0){
                int newCursorCol = lines[cursorRow - 1].Length;
                lines[cursorRow - 1] += lines[cursorRow];
                lines.RemoveAt(cursorRow);
                cursorRow--;
                visibleCursorCol = cursorCol = newCursorCol;
                deleted += '\n';
            }
        }else{
            if(visibleCursorCol != lines[cursorRow + verticalScroll].Length)
            {   
                deleted += lines[cursorRow][visibleCursorCol];
                lines[cursorRow] = lines[cursorRow].Remove(visibleCursorCol, 1);
            }else if(cursorRow < lines.Count - 1){
                deleted = "\n";
                lines[cursorRow] += lines[cursorRow + 1];
                lines.RemoveAt(cursorRow + 1);
            }
        }
        AdjustVerticalScrollToCursor();
        UpdateConsole();
        return deleted;
    }

    void DeleteHighlight()
    {
        if(!isHighlighting)
            return;
        int r1 = dragStart.x;
        int c1 = dragStart.y;
        int r2 = dragCurrent.x;
        int c2 = dragCurrent.y;
        if(dragCurrent.x < dragStart.x || (dragCurrent.x == dragStart.x && dragCurrent.y < dragStart.y))
        {
            r1 = dragCurrent.x;
            c1 = dragCurrent.y;
            r2 = dragStart.x;
            c2 = dragStart.y;
        } 
        DeleteRegion(r1,c1,r2,c2 - 1);
        isHighlighting = false;
        /*EndHighlight();
        if(r1 == r2)
            lines[r1] = lines[r1].Substring(0,c1) + lines[r1].Substring(c2);
        else{
            int fullLines = r2 - r1 - 1;
            for(int i = 0;i<fullLines;i++)
            {
                lines.RemoveAt(r1 + 1);
            }
            lines[r1] = lines[r1].Substring(0,c1) + lines[r1+1].Substring(c2);
            lines.RemoveAt(r1+1);
        }*/
        cursorRow = r1;
        cursorCol = visibleCursorCol = c1;
        UpdateConsole();
    }

    void Highlight(int r, int c)
    {
        GameObject cell = chars[r,c];
        cell.GetComponent<ConsoleCharController>().UpdateColor(Color.white);
        cell.GetComponent<ConsoleCharController>().UpdateTextColor(Color.black);
    }

    void OnScroll(float scrollInput)
    {

    }

    void Update()
    {
        if(mouseListener.isMouseDragging && mouseListener.currentMousePosition.y > 1 && Time.time > lastDragScrollTime + GetTimeBetweenDragScroll())
        {
            MaybeUpScroll();
            Vector2Int cursorLocation = GetCursorLocationForMouse();
            dragCurrent = new Vector2Int(cursorLocation.x + verticalScroll, cursorLocation.y);
            lastDragScrollTime = Time.time;
        }
        if(mouseListener.isMouseDragging && mouseListener.currentMousePosition.y < 0 && Time.time > lastDragScrollTime + GetTimeBetweenDragScroll())
        {
            MaybeDownScroll();
            lastDragScrollTime = Time.time;
        }
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if(scrollInput != 0){
            if(scrollInput > 0)
            {
                MaybeUpScroll();
            }else{
                MaybeDownScroll();
            }
        }

        HashSet<char> excluded = new HashSet<char>(){(char)(8),(char)(10),(char)(13),'c','C','v','V','x','X','y','Y','z','Z','a','A'};
        foreach(char ch in Input.inputString)
        {
            if(!excluded.Contains(ch)){
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