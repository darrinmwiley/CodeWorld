using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways] // Allows the script to run in the Scene view without entering Play Mode
public class Colorable : MonoBehaviour
{
    [Header("Settings")]
    public Material targetMaterial;
    public Color defaultColor = Color.white;

    [SerializeField] private string colorPropertyName = "_Color";

    private MeshRenderer _renderer;
    private Material _instanceMaterial;

    void Awake()
    {
        SetupMaterial();
    }

    void Start()
    {
        ApplyColor(defaultColor);
    }

    /// <summary>
    /// Ensures we are working with a unique instance of the material 
    /// so we don't accidentally edit the original Asset file.
    /// </summary>
    private void SetupMaterial()
    {
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        
        if (_renderer != null && targetMaterial != null)
        {
            // Create a local instance of the material
            if (_instanceMaterial == null)
            {
                _instanceMaterial = new Material(targetMaterial);
                _instanceMaterial.name = $"{targetMaterial.name} (Instance)";
            }
            
            _renderer.sharedMaterial = _instanceMaterial;
        }
    }

    public void ApplyColor(Color newColor)
    {
        SetupMaterial();

        if (_instanceMaterial != null)
        {
            _instanceMaterial.SetColor(colorPropertyName, newColor);
        }
        else
        {
            Debug.LogWarning($"Target Material or Renderer is missing on {gameObject.name}!", this);
        }
    }

    // This runs whenever you change a value in the Inspector
    private void OnValidate()
    {
        // Use delayCall to avoid "SendMessage cannot be called during Awake/OnValidate" errors
        #if UNITY_EDITOR
        EditorApplication.delayCall += () => {
            if (this != null) ApplyColor(defaultColor);
        };
        #endif
    }
}

// This section adds the "Refresh" button to the Inspector
#if UNITY_EDITOR
[CustomEditor(typeof(Colorable))]
public class ColorableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Colorable script = (Colorable)target;
        if (GUILayout.Button("Refresh Color ✨"))
        {
            script.ApplyColor(script.defaultColor);
        }
    }
}
#endif