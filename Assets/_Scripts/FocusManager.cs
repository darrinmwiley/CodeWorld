using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusManager : MonoBehaviour
{
    public static FocusManager Instance { get; private set; }
    private Stack<IFocusable> _focusStack = new Stack<IFocusable>();

    [Header("Player Reference")]
    public FirstPersonMovement playerMovement;

    [Header("UI Reference")]
    public MonoBehaviour mainUIWindow; 

    private bool _manualCursorUnlock = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        bool hasUIOpen = _focusStack.Count > 0;

        // 1. TAB: Open UI
        if (Input.GetKeyDown(KeyCode.Tab) && !hasUIOpen)
        {
            if (mainUIWindow != null)
            {
                IFocusable focusable = mainUIWindow.GetComponent<IFocusable>();
                if (focusable != null)
                {
                    _manualCursorUnlock = false;
                    PushFocus(focusable);
                }
            }
        }
        
        // 2. ESCAPE: The problematic toggle in Builds
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (hasUIOpen)
            {
                _manualCursorUnlock = false;
                PopFocus();
            }
            else
            {
                _manualCursorUnlock = !_manualCursorUnlock;
                UpdateState();
            }
        }

        // 3. Re-consume mouse
        if (_manualCursorUnlock && Input.GetMouseButtonDown(0) && !hasUIOpen)
        {
            _manualCursorUnlock = false;
            UpdateState();
        }
    }

    public void PushFocus(IFocusable focusable)
    {
        if (_focusStack.Count > 0 && _focusStack.Peek() == focusable) return;
        _focusStack.Push(focusable);
        focusable.OnFocus();
        UpdateState();
    }

    public void PopFocus()
    {
        if (_focusStack.Count > 0)
        {
            IFocusable top = _focusStack.Pop();
            top.OnDefocus();
        }
        UpdateState();
    }

    private void UpdateState()
    {
        bool hasUIOpen = _focusStack.Count > 0;
        bool shouldFreeMouse = hasUIOpen || _manualCursorUnlock;

        if (playerMovement != null)
        {
            playerMovement.movementLocked = shouldFreeMouse;
            if (shouldFreeMouse) ResetPhysics();
        }

        StopAllCoroutines();
        StartCoroutine(ApplyCursorBuildSafe(shouldFreeMouse));
    }

    private IEnumerator ApplyCursorBuildSafe(bool visible)
    {
        // 1. Wait for end of current frame
        yield return new WaitForEndOfFrame();
        
        // 2. Apply state
        Apply(visible);

        // 3. CRITICAL FOR BUILDS: Wait one more frame and force it again.
        // This prevents other scripts or the engine from "snatching" it back
        // immediately after the Escape key is processed.
        yield return null; 
        Apply(visible);
    }

    private void Apply(bool visible)
    {
        UnityEngine.Cursor.visible = visible;
        UnityEngine.Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        
        // Extra insurance for Windows/Mac builds
        if (visible) 
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
    }

    private void ResetPhysics()
    {
        var rb = playerMovement.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}