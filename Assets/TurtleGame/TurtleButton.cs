using UnityEngine;

public class TurtleButton : MonoBehaviour
{
    public enum ButtonType { Action, Backspace, Play }

    [Header("Logic Connections")]
    public TurtleCommander commander;
    public ClickListener clickListener;
    
    [Header("Button Identity")]
    public ButtonType type;
    public TurtleCommand action; // Only used if type is Action

    [Header("Animation Settings")]
    public float moveDistance = 0.05f; 
    public float duration = 0.1f; 
    
    private Vector3 _startLocalPos;
    private float _pressTime = -1f;
    private bool _isAnimating = false;

    void Awake()
    {
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
        if (_isAnimating || commander == null) return;

        _pressTime = Time.time;
        _isAnimating = true;

        switch (type)
        {
            case ButtonType.Action:
                commander.AddCommand(action);
                break;
            case ButtonType.Backspace:
                commander.Backspace();
                break;
            case ButtonType.Play:
                commander.Play();
                break;
        }
    }

    void FixedUpdate()
    {
        if (!_isAnimating) return;

        float elapsed = Time.time - _pressTime;
        float totalAnimationTime = duration * 2f;

        if (elapsed < totalAnimationTime)
        {
            float t = (elapsed < duration) 
                ? (elapsed / duration) 
                : (2f - (elapsed / duration));

            // Move along the object's local negative Y axis (plunger motion)
            Vector3 worldAnchor = transform.parent != null 
                ? transform.parent.TransformPoint(_startLocalPos) 
                : _startLocalPos;

            transform.position = worldAnchor - (transform.up * (moveDistance * t));
        }
        else
        {
            transform.localPosition = _startLocalPos;
            _isAnimating = false;
        }
    }
}