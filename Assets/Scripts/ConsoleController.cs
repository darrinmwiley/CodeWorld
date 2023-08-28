using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleController : MonoBehaviour
{
    public GameObject consoleCharPrefab;

    public int terminalWidth = 80;
    public int terminalHeight = 24;
    RenderTexture renderTexture;

    GameObject[,] chars;

    int cursorRow = 0;
    int cursorCol = 0;

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

    // Start is called before the first frame update
    void Start()
    {
        Generate();
    }

    // Update is called once per frame
    void Update()
    {
        // Check for any input from the user
        if (Input.anyKeyDown)
        {
            // Check if the input is a character (letter or digit)
            if (Input.inputString.Length > 0 && !Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.Backspace))
            {
                foreach(char ch in Input.inputString){
                    GameObject cell = chars[cursorRow,cursorCol];
                    cell.GetComponent<ConsoleCharController>().UpdateText(ch+"");
                    cursorCol++;
                    if(cursorCol == 80){
                        cursorCol = 0;
                        cursorRow++;
                    }
                }
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                // Handle Enter/Return key
                cursorRow++;
                cursorCol = 0;
            }
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                if(cursorCol == 0)
                {
                    if(cursorRow != 0)
                    {
                        cursorRow--;
                        cursorCol = 79;
                    }
                }else{
                    cursorCol--;
                }
                GameObject cell = chars[cursorRow,cursorCol];
                cell.GetComponent<ConsoleCharController>().UpdateText("");
            }
        }
    }
}
