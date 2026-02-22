using UnityEngine;

[DisallowMultipleComponent]
public class QuadFaceAnimator : MonoBehaviour
{
    public enum FaceMode { Off, Idle, Talking }

    [Header("Renderer")]
    public MeshRenderer r;
    public string prop = "_MainTex"; // optional override; leave as-is for auto

    [Header("Textures")]
    public Texture idle, talkA, talkB, offTex;

    [Header("State")]
    public FaceMode mode = FaceMode.Idle;
    [Min(0.01f)] public float talkDelay = 0.2f;

    [Header("Turn On/Off")]
    public bool playOnStart = true;
    public float finalScale = 3f;
    [Min(0.001f)] public float xDur = 0.08f;
    [Min(0.001f)] public float xyDur = 0.12f;

    [Tooltip("Visible line thickness during phase 1 (in local scale units).")]
    [Min(0.0001f)] public float lineY = 0.06f;

    [Tooltip("How far X grows (fraction of finalScale) during phase 1.")]
    [Range(0.1f, 1f)] public float xLineFrac = 0.65f;

    MaterialPropertyBlock _b;
    string _p;

    FaceMode _lastMode = (FaceMode)(-1);

    // Turn progress: 0 = fully off (invisible), 1 = fully on (finalScale)
    float _turnT = 1f;
    int _turnDir = 0; // -1 turning off, +1 turning on, 0 idle

    // Talking tick
    float _talkTimer;
    bool _talkFlip;

    float _zScale = 1f;

    void Awake()
    {
        if (!r) r = GetComponent<MeshRenderer>();
        _b = new MaterialPropertyBlock();

        var m = r ? r.sharedMaterial : null;
        _p = (m != null && m.HasProperty(prop)) ? prop :
             (m != null && m.HasProperty("_BaseMap")) ? "_BaseMap" :
             (m != null && m.HasProperty("_MainTex")) ? "_MainTex" : prop;

        _zScale = Mathf.Approximately(transform.localScale.z, 0f) ? 1f : transform.localScale.z;
    }

    void OnEnable()
    {
        // Ensure we’re in a consistent pose/tex immediately.
        if (mode == FaceMode.Off)
        {
            _turnT = 0f;
            _turnDir = 0;
            ApplyScaleImmediate();
            SetTex(offTex);
        }
        else
        {
            _turnT = 1f;
            _turnDir = 0;
            ApplyScaleImmediate();
            ApplyTextureForCurrentState(forceIdleFace: false);
        }

        _lastMode = mode;
    }

    void Start()
    {
        if (playOnStart && mode != FaceMode.Off)
        {
            // If starting “on”, do the turn-on animation from off.
            _turnT = 0f;
            _turnDir = +1;
            ApplyScaleImmediate(); // show initial line immediately
            ApplyTextureForCurrentState(forceIdleFace: true);
        }
    }

    void FixedUpdate()
    {
        // Detect mode changes (including off -> on auto-start of turn-on).
        if (mode != _lastMode)
        {
            if (_lastMode == FaceMode.Off && mode != FaceMode.Off)
            {
                // Off -> something: start turning on
                _turnDir = +1;
            }
            else if (mode == FaceMode.Off)
            {
                // Anything -> Off: start turning off
                _turnDir = -1;
            }

            _lastMode = mode;

            // Reset talking cadence when changing modes.
            _talkTimer = 0f;
            _talkFlip = false;
        }

        StepTurnAnimation(Time.fixedDeltaTime);
        StepTalking(Time.fixedDeltaTime);

        // Apply texture each tick based on whether we’re turning and the mode.
        // While turning (either direction), we force the “idle” face (like a TV powering).
        ApplyTextureForCurrentState(forceIdleFace: _turnT < 1f);

        // If fully off, force off texture.
        if (IsFullyOff())
            SetTex(offTex);
    }

    void StepTurnAnimation(float dt)
    {
        if (_turnDir == 0)
        {
            // Not turning: if mode is non-off, ensure scale is correct final.
            if (mode != FaceMode.Off && _turnT < 1f) _turnT = 1f;
            if (mode == FaceMode.Off && _turnT > 0f) _turnT = 0f;
            ApplyScaleImmediate();
            return;
        }

        float total = Mathf.Max(0.0001f, xDur + xyDur);
        _turnT = Mathf.Clamp01(_turnT + _turnDir * (dt / total));

        ApplyScaleFromTurnT(_turnT);

        // Finished?
        if (_turnDir > 0 && _turnT >= 1f - 1e-6f)
        {
            _turnT = 1f;
            _turnDir = 0;
            ApplyScaleImmediate();
        }
        else if (_turnDir < 0 && _turnT <= 0f + 1e-6f)
        {
            _turnT = 0f;
            _turnDir = 0;

            // TV “off”: collapse fully so it disappears.
            transform.localScale = new Vector3(0f, 0f, _zScale);
        }
    }

    void StepTalking(float dt)
    {
        if (mode != FaceMode.Talking) return;

        // Pause mouth-flap during turning (both on and off).
        if (_turnT < 1f) return;

        _talkTimer += dt;
        if (_talkTimer >= talkDelay)
        {
            // Consume in a stable way (handles big dt spikes).
            _talkTimer -= talkDelay;
            _talkFlip = !_talkFlip;
        }
    }

    void ApplyTextureForCurrentState(bool forceIdleFace)
    {
        if (!r) return;

        if (mode == FaceMode.Off)
        {
            // During turn-off, we show idle face until fully off.
            if (IsFullyOff()) SetTex(offTex);
            else SetTex(GetIdleFallback());
            return;
        }

        if (forceIdleFace)
        {
            SetTex(GetIdleFallback());
            return;
        }

        if (mode == FaceMode.Idle)
        {
            SetTex(GetIdleFallback());
            return;
        }

        // Talking
        var a = talkA ? talkA : GetIdleFallback();
        var b = talkB ? talkB : a;
        SetTex(_talkFlip ? a : b);
    }

    Texture GetIdleFallback()
    {
        if (idle) return idle;
        if (talkA) return talkA;
        return offTex;
    }

    bool IsFullyOff() => mode == FaceMode.Off && _turnT <= 0f + 1e-6f;

    void ApplyScaleImmediate()
    {
        if (_turnT <= 0f + 1e-6f)
        {
            // Fully off: invisible
            transform.localScale = new Vector3(0f, 0f, _zScale);
        }
        else if (_turnT >= 1f - 1e-6f)
        {
            // Fully on: final
            float S = finalScale;
            transform.localScale = new Vector3(S, S, _zScale);
        }
        else
        {
            ApplyScaleFromTurnT(_turnT);
        }
    }

    void ApplyScaleFromTurnT(float t01)
    {
        float S = finalScale;
        float y0 = Mathf.Min(lineY, S);
        float x1 = Mathf.Clamp01(xLineFrac) * S;

        float total = Mathf.Max(0.0001f, xDur + xyDur);
        float f1 = Mathf.Clamp01(xDur / total);
        float f2 = 1f - f1;

        float x, y;

        if (t01 <= 0f)
        {
            // Start of turn-on: visible line (then we collapse to 0,0 only when fully off)
            x = 0f; y = y0;
        }
        else if (t01 < f1 && f1 > 1e-6f)
        {
            float u = t01 / f1;
            x = Mathf.Lerp(0f, x1, u);
            y = y0;
        }
        else
        {
            float u = (f2 > 1e-6f) ? (t01 - f1) / f2 : 1f;
            float e = Ease(u);
            x = Mathf.Lerp(x1, S, e);
            y = Mathf.Lerp(y0, S, e);
        }

        transform.localScale = new Vector3(x, y, _zScale);
    }

    void SetTex(Texture t)
    {
        if (!r || t == null) return;
        r.GetPropertyBlock(_b);
        _b.SetTexture(_p, t);
        r.SetPropertyBlock(_b);
    }

    static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t); // easeOutQuad
    }

    // ---- Inspector menu actions ----
    [ContextMenu("Turn On")]
    public void TurnOn()
    {
        if (mode == FaceMode.Off) mode = FaceMode.Idle;

        // Reversible: just flip direction.
        _turnDir = +1;

        // If we were fully off (0,0), snap to the initial visible line immediately.
        if (_turnT <= 0f + 1e-6f)
        {
            _turnT = 0f;
            ApplyScaleFromTurnT(_turnT);
        }
    }

    [ContextMenu("Turn Off")]
    public void TurnOff()
    {
        mode = FaceMode.Off;
        _turnDir = -1; // reversible
    }

    [ContextMenu("Set Idle")]
    public void SetIdle()
    {
        mode = FaceMode.Idle;
        // If we’re currently off-ish, turning on should happen automatically (mode change logic in FixedUpdate),
        // but if you want immediate intent, force direction:
        if (_turnT < 1f) _turnDir = +1;
    }

    [ContextMenu("Set Talking")]
    public void SetTalking()
    {
        mode = FaceMode.Talking;
        if (_turnT < 1f) _turnDir = +1;
    }
}
