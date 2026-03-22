using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusManager : MonoBehaviour
{
    public static FocusManager Instance { get; private set; }
    private Stack<IFocusable> _focusStack = new Stack<IFocusable>();

    [Header("Player References")]
    public FirstPersonMovement playerMovement;
    public MonoBehaviour mainUIWindow; 

    private bool _manualCursorUnlock = false;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            // Initialize global state before any Start() methods run
            GameState.IsInUI = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // FORCE the hardware to be consumed/locked at the very beginning.
        // This prevents the "leak" where the mouse is free until the first dialog.
        _manualCursorUnlock = false;
        UpdateState(); 
    }

    private void Update()
    {
        bool hasUIOpen = _focusStack.Count > 0;
        
        // Sync global flag every frame
        GameState.IsInUI = hasUIOpen;

        // Persistent enforcement: If UI is open, keep the mouse free.
        // This beats other scripts trying to "snatch" the mouse back.
        if (hasUIOpen || _manualCursorUnlock)
        {
            ApplyCursorImmediate(true);
        }

        // --- Input Handling ---

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
        
        // 2. ESCAPE: Close UI or toggle free-look mouse
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
        // "Busy" means we want the mouse visible and movement stopped
        bool isBusy = (_focusStack.Count > 0 || _manualCursorUnlock);
        
        // Update the static state for FirstPersonLook
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
        
        // Standard secondary check for standalone builds
        if (!visible)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
    }
}