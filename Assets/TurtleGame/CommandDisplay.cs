using UnityEngine;
using System.Collections.Generic;

public class CommandDisplay : MonoBehaviour
{
    [Header("Assets")]
    public Material baseMaterial;
    public Texture2D texForward;
    public Texture2D texCW;
    public Texture2D texCCW;
    
    [Header("Layout Settings")]
    public float iconWidth = 0.1f;
    public float iconHeight = 0.1f;
    public float spacing = 0.02f;
    public int iconsPerRow = 8;
    public int maxRows = 4;

    [Header("Feedback")]
    public Color activeEmission = Color.white;
    public float emissionIntensity = 2.0f;

    private List<GameObject> _iconObjects = new List<GameObject>();
    private List<MaterialPropertyBlock> _propBlocks = new List<MaterialPropertyBlock>();
    
    // To identify which command is which
    private static readonly string _MainTex = "_MainTex";
    private static readonly string _EmissionColor = "_EmissionColor";

    public bool HasSpace => _iconObjects.Count < (iconsPerRow * maxRows);

    public void AddCommandIcon(TurtleCommand cmd)
    {
        if (!HasSpace) return;

        GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Quad);
        icon.name = $"Icon_{_iconObjects.Count}";
        icon.transform.SetParent(this.transform);
        Destroy(icon.GetComponent<MeshCollider>());

        // Calculate position (starting top-left of the parent quad)
        int index = _iconObjects.Count;
        int row = index / iconsPerRow;
        int col = index % iconsPerRow;

        // Local positioning math: pivot is center of parent
        float xPos = (col * (iconWidth + spacing)) - ((iconsPerRow - 1) * (iconWidth + spacing) * 0.5f);
        float yPos = ((maxRows - 1) * (iconHeight + spacing) * 0.5f) - (row * (iconHeight + spacing));

        icon.transform.localPosition = new Vector3(xPos, yPos, -0.01f);
        icon.transform.localRotation = Quaternion.identity;
        icon.transform.localScale = new Vector3(iconWidth, iconHeight, 1f);

        // Setup visuals
        MeshRenderer mr = icon.GetComponent<MeshRenderer>();
        mr.material = baseMaterial;
        
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        Texture2D targetTex = cmd == TurtleCommand.Forward ? texForward : 
                             (cmd == TurtleCommand.Clockwise ? texCW : texCCW);
        
        block.SetTexture(_MainTex, targetTex);
        mr.SetPropertyBlock(block);

        _iconObjects.Add(icon);
        _propBlocks.Add(block);
    }

    public void RemoveLastIcon()
    {
        if (_iconObjects.Count == 0) return;

        int lastIndex = _iconObjects.Count - 1;
        GameObject lastObj = _iconObjects[lastIndex];
        
        _iconObjects.RemoveAt(lastIndex);
        _propBlocks.RemoveAt(lastIndex);

        if (Application.isPlaying) Destroy(lastObj);
        else DestroyImmediate(lastObj);
    }

    public void HighlightCommand(int index)
    {
        // Reset all first
        for (int i = 0; i < _iconObjects.Count; i++)
        {
            SetEmission(i, Color.black);
        }

        // Set active
        if (index >= 0 && index < _iconObjects.Count)
        {
            SetEmission(index, activeEmission * emissionIntensity);
        }
    }

    public void ClearHighlights()
    {
        for (int i = 0; i < _iconObjects.Count; i++) SetEmission(i, Color.black);
    }

    private void SetEmission(int index, Color color)
    {
        var mr = _iconObjects[index].GetComponent<MeshRenderer>();
        _propBlocks[index].SetColor(_EmissionColor, color);
        mr.SetPropertyBlock(_propBlocks[index]);
    }

    public void ClearAllIcons()
    {
        foreach (var obj in _iconObjects)
        {
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
        _iconObjects.Clear();
        _propBlocks.Clear();
    }
}