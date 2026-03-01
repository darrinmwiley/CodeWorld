using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class BlinkingCursorController : MonoBehaviour
{
    [Header("Blink")]
    [Min(0.01f)] public float cursorBlinkSeconds = 1f;

    [Header("Colors")]
    public Color onColor = Color.white * 1.5f;
    public Color offColor = Color.black;

    [Header("Shader (optional)")]
    [Tooltip("Leave empty to auto-pick an Unlit shader.")]
    public Shader unlitShaderOverride;

    float _lastCursorBlink;
    bool _cursorBlinkOn = true;
    Vector3 _previousPosition;

    MeshRenderer _renderer;
    Material _mat;
    int _colorPropId = -1;

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        if (_renderer == null)
            _renderer = gameObject.AddComponent<MeshRenderer>();

        // Ensure no lighting/shadows affect this
        _renderer.shadowCastingMode = ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        _renderer.lightProbeUsage = LightProbeUsage.Off;
        _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        // Pick an unlit shader (URP/HDRP/Built-in friendly)
        Shader s = unlitShaderOverride;
        if (s == null)
            s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null)
            s = Shader.Find("HDRP/Unlit");
        if (s == null)
            s = Shader.Find("Unlit/Color");
        if (s == null)
            s = Shader.Find("Unlit/Texture"); // last-ditch fallback

        if (s == null)
        {
            Debug.LogError("BlinkingCursorController: Could not find an Unlit shader. Assign one in 'unlitShaderOverride'.");
            enabled = false;
            return;
        }

        // Create + assign a dedicated material ONCE (avoid renderer.material spam)
        _mat = new Material(s) { name = "Cursor_Unlit_Instance" };
        _renderer.sharedMaterial = _mat;

        // Determine which color property this shader supports
        if (_mat.HasProperty("_BaseColor")) _colorPropId = Shader.PropertyToID("_BaseColor"); // URP/HDRP common
        else if (_mat.HasProperty("_Color")) _colorPropId = Shader.PropertyToID("_Color");   // Built-in Unlit/Color
        else if (_mat.HasProperty("_color")) _colorPropId = Shader.PropertyToID("_color");   // just in case

        _lastCursorBlink = Time.time;
        _previousPosition = transform.position;

        SetCursorVisible(true);
    }

    void OnDestroy()
    {
        // Destroy the runtime material instance to avoid leaks (especially in editor play mode)
        if (_mat != null)
        {
            if (Application.isPlaying) Destroy(_mat);
            else DestroyImmediate(_mat);
        }
    }

    /// <summary>Public API: force cursor color immediately (still unlit).</summary>
    public void UpdateColor(Color color)
    {
        if (_mat == null) return;

        if (_colorPropId != -1)
            _mat.SetColor(_colorPropId, color);
        // If no color property exists, do nothing (shader likely doesn't support tint).
    }

    void SetCursorVisible(bool on)
    {
        _cursorBlinkOn = on;
        UpdateColor(on ? onColor : offColor);
    }

    void Update()
    {
        // If the cursor moved, turn it on and restart blink timer
        if (transform.position != _previousPosition)
        {
            _lastCursorBlink = Time.time;
            SetCursorVisible(true);
        }

        if (Time.time - _lastCursorBlink >= cursorBlinkSeconds)
        {
            _lastCursorBlink = Time.time;
            SetCursorVisible(!_cursorBlinkOn);
        }

        _previousPosition = transform.position;
    }
}