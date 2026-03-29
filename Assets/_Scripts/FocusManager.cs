using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusManager : MonoBehaviour
{
    public static FocusManager Instance { get; private set; }
    private List<IFocusable> _focusStack = new List<IFocusable>(); // Changed to List for specific removal

    [Header("Player References")]
    public FirstPersonMovement playerMovement;
    public MonoBehaviour mainUIWindow; 

    private bool _manualCursorUnlock = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            GameState.IsInUI = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _manualCursorUnlock = false;
        UpdateState(); 
    }

    private void Update()
    {
        bool hasUIOpen = _focusStack.Count > 0;
        
        GameState.IsInUI = hasUIOpen;

        if (hasUIOpen || _manualCursorUnlock)
        {
            ApplyCursorImmediate(true);
        }

        // 1. TAB: Open the main window
        if (Input.GetKeyDown(KeyCode.Tab) && !hasUIOpen)
        {
            IFocusable focusable = mainUIWindow?.GetComponent<IFocusable>();
            if (focusable != null)
            {
                _manualCursorUnlock = false;
                PushFocus(focusable);
            }
        }
        
        // 2. ESCAPE: Close top UI or toggle free-look mouse
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

        // 3. Left Click: Re-lock if we were in manual free-mouse mode
        if (_manualCursorUnlock && Input.GetMouseButtonDown(0) && !hasUIOpen)
        {
            _manualCursorUnlock = false;
            UpdateState();
        }
    }

    public void PushFocus(IFocusable focusable)
    {
        if (_focusStack.Count > 0 && _focusStack[_focusStack.Count - 1] == focusable) return;
        
        _focusStack.Add(focusable);
        focusable.OnFocus();
        UpdateState();
    }

    public void PopFocus()
    {
        if (_focusStack.Count > 0)
        {
            IFocusable top = _focusStack[_focusStack.Count - 1];
            _focusStack.RemoveAt(_focusStack.Count - 1);
            top.OnDefocus();
        }
        UpdateState();
    }

    // New helper: Allows a specific window (like one being closed via X button) to remove itself
    public void RemoveFocus(IFocusable focusable)
    {
        if (focusable == null) return;
        if (_focusStack.Contains(focusable))
        {
            _focusStack.Remove(focusable);
            focusable.OnDefocus();
            UpdateState();
        }
    }

    private void UpdateState()
    {
        bool isBusy = (_focusStack.Count > 0 || _manualCursorUnlock);
        GameState.IsInUI = (_focusStack.Count > 0);

        if (playerMovement != null)
        {
            playerMovement.movementLocked = isBusy;
        }

        ApplyCursorImmediate(isBusy);
    }

    private void ApplyCursorImmediate(bool visible)
    {
        UnityEngine.Cursor.visible = visible;
        UnityEngine.Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        
        if (!visible)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
    }
}