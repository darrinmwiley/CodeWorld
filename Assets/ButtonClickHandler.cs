using UnityEngine;

public class ButtonClickHandler : MonoBehaviour
{
    public ClickListener clickListener;
    
    [Header("Animation Settings")]
    public float moveDistance = 0.2f; // X amount
    public float duration = 0.1f;     // Y seconds (one way)
    
    private Vector3 _startLocalPos;
    private float _pressTime = -1f;
    private bool _isAnimating = false;

    void Awake()
    {
        // Still store local so we know the "anchor" point relative to parent
        _startLocalPos = transform.localPosition;
    }

    void Start()
    {
        if (clickListener != null)
        {
            clickListener.RemoveClickHandler(OnPress);
            clickListener.AddClickHandler(OnPress);
        }
    }

    private void OnDisable()
    {
        if (clickListener != null)
            clickListener.RemoveClickHandler(OnPress);
    }

    public void OnPress()
    {
        _pressTime = Time.time;
        _isAnimating = true;
    }

    void FixedUpdate()
    {
        if (!_isAnimating) return;

        float elapsed = Time.time - _pressTime;
        float totalAnimationTime = duration * 2f;

        if (elapsed < totalAnimationTime)
        {
            float t;
            if (elapsed < duration)
            {
                t = elapsed / duration;
            }
            else
            {
                t = 2f - (elapsed / duration);
            }

            // 1. Find the "Anchor" in world space (where the button would be if not moving)
            Vector3 worldAnchor = transform.parent != null 
                ? transform.parent.TransformPoint(_startLocalPos) 
                : _startLocalPos;

            // 2. Move along the object's CURRENT negative Y axis in world space
            // transform.up is the object's local Y axis projected into the world
            transform.position = worldAnchor - (transform.up * (moveDistance * t));
        }
        else
        {
            transform.localPosition = _startLocalPos;
            _isAnimating = false;
        }
    }
}