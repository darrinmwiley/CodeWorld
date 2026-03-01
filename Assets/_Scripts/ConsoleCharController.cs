using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

[DisallowMultipleComponent]
public class ConsoleCharController : MonoBehaviour
{
    protected char Char;

    Canvas _canvas;
    TextMeshProUGUI _tmp;

    MeshRenderer _bgRenderer;
    Material _bgMat;
    int _colorPropId = -1;

    public char GetChar() => Char;

    void Awake()
    {
        // Cache TMP
        _canvas = GetComponentInChildren<Canvas>();
        if (_canvas != null)
            _tmp = _canvas.GetComponentInChildren<TextMeshProUGUI>();

        if (_tmp == null)
            Debug.LogError("ConsoleCharController: TMP not found.");

        // Cache background cube
        Transform cube = transform.Find("Cube");
        if (cube == null)
        {
            Debug.LogError("ConsoleCharController: Cube child missing.");
            return;
        }

        _bgRenderer = cube.GetComponent<MeshRenderer>();
        if (_bgRenderer == null)
            _bgRenderer = cube.gameObject.AddComponent<MeshRenderer>();

        // Disable lighting influence
        _bgRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _bgRenderer.receiveShadows = false;
        _bgRenderer.lightProbeUsage = LightProbeUsage.Off;
        _bgRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        // Pick unlit shader automatically
        Shader s =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("HDRP/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Unlit/Texture");

        if (s == null)
        {
            Debug.LogError("ConsoleCharController: Could not find Unlit shader.");
            return;
        }

        // Create material ONCE
        _bgMat = new Material(s) { name = "ConsoleCell_Unlit_Instance" };
        _bgRenderer.sharedMaterial = _bgMat;

        // Detect correct color property
        if (_bgMat.HasProperty("_BaseColor")) _colorPropId = Shader.PropertyToID("_BaseColor");
        else if (_bgMat.HasProperty("_Color")) _colorPropId = Shader.PropertyToID("_Color");
        else if (_bgMat.HasProperty("_color")) _colorPropId = Shader.PropertyToID("_color");
    }

    void OnDestroy()
    {
        if (_bgMat != null)
        {
            if (Application.isPlaying) Destroy(_bgMat);
            else DestroyImmediate(_bgMat);
        }
    }

    public void UpdateText(string newText)
    {
        if (string.IsNullOrEmpty(newText)) return;

        Char = newText[0];

        if (_tmp != null)
            _tmp.text = newText;
    }

    public void UpdateTextColor(Color color)
    {
        if (_tmp != null)
            _tmp.color = color;
    }

    public void UpdateColor(Color color)
    {
        if (_bgMat == null || _colorPropId == -1) return;
        _bgMat.SetColor(_colorPropId, color);
    }
}