using System;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleInputManager : MonoBehaviour
{
    [Header("Dependencies")]
    public ConsoleStateManager stateManager;
    public ConsoleWindowController windowController;

    [Header("Settings")]
    public UIToolkitMouseListenerMono mouseListener;
    public float heldKeyDelay = .005f;
    public float heldKeyTriggerTime = .4f;

    private Dictionary<KeyCode, Action> specialKeyPressHandlers;
    private HashSet<char> excludedInputStringChars;
    private KeyCode latest = KeyCode.None;
    private float latestDownTime = 0;
    private bool isKeyHeld = false;
    private float lastHeldKeyTriggerTime = 0;
    private float lastDragScrollTime = 0;
    private bool capsLockActive = false;

    public void Initialize()
    {
        InitKeyHandlers();
        if (mouseListener != null)
        {
            mouseListener.AddMouseDownHandler(OnMouseDown);
            mouseListener.AddMouseDragHandler(OnMouseDrag);
        }
    }

    private void OnGUI()
    {
        if (Event.current != null)
            capsLockActive = Event.current.capsLock;
    }

    private void InitKeyHandlers()
    {
        specialKeyPressHandlers = new Dictionary<KeyCode, Action>
        {
            { KeyCode.Return, OnReturnPressed },
            { KeyCode.KeypadEnter, OnReturnPressed },
            { KeyCode.Backspace, OnBackspacePressed },
            { KeyCode.LeftArrow, OnLeftArrowPressed },
            { KeyCode.RightArrow, OnRightArrowPressed },
            { KeyCode.UpArrow, OnUpArrowPressed },
            { KeyCode.DownArrow, OnDownArrowPressed },
            { KeyCode.Tab, OnTabPressed },
            { KeyCode.Delete, OnDeletePressed }
        };

        excludedInputStringChars = new HashSet<char>
        {
            (char)0,
            (char)8,
            (char)9,
            (char)10,
            (char)13
        };

        RegisterAlphaShortcut(KeyCode.C, 'c', 'C', OnCAction);
        RegisterAlphaShortcut(KeyCode.V, 'v', 'V', OnVAction);
        RegisterAlphaShortcut(KeyCode.X, 'x', 'X', OnXAction);
        RegisterAlphaShortcut(KeyCode.Y, 'y', 'Y', () => stateManager.Redo());
        RegisterAlphaShortcut(KeyCode.Z, 'z', 'Z', () => stateManager.Undo());
        RegisterAlphaShortcut(KeyCode.A, 'a', 'A', SelectAll);
        RegisterAlphaShortcut(KeyCode.S, 's', 'S', () => stateManager.Save("fname.txt"));
        RegisterAlphaShortcut(KeyCode.L, 'l', 'L', () => stateManager.Load("fname.txt"));

        RegisterPrintableKey(KeyCode.Space, ' ', ' ');
        RegisterPrintableKey(KeyCode.Alpha1, '1', '!');
        RegisterPrintableKey(KeyCode.Alpha2, '2', '@');
        RegisterPrintableKey(KeyCode.Alpha3, '3', '#');
        RegisterPrintableKey(KeyCode.Alpha4, '4', '$');
        RegisterPrintableKey(KeyCode.Alpha5, '5', '%');
        RegisterPrintableKey(KeyCode.Alpha6, '6', '^');
        RegisterPrintableKey(KeyCode.Alpha7, '7', '&');
        RegisterPrintableKey(KeyCode.Alpha8, '8', '*');
        RegisterPrintableKey(KeyCode.Alpha9, '9', '(');
        RegisterPrintableKey(KeyCode.Alpha0, '0', ')');
        RegisterPrintableKey(KeyCode.Minus, '-', '_');
        RegisterPrintableKey(KeyCode.Equals, '=', '+');
        RegisterPrintableKey(KeyCode.LeftBracket, '[', '{');
        RegisterPrintableKey(KeyCode.RightBracket, ']', '}');
        RegisterPrintableKey(KeyCode.Backslash, '\\', '|');
        RegisterPrintableKey(KeyCode.Semicolon, ';', ':');
        RegisterPrintableKey(KeyCode.Quote, '\'', '"');
        RegisterPrintableKey(KeyCode.Comma, ',', '<');
        RegisterPrintableKey(KeyCode.Period, '.', '>');
        RegisterPrintableKey(KeyCode.Slash, '/', '?');
        RegisterPrintableKey(KeyCode.BackQuote, '`', '~');
    }

    private void RegisterAlphaShortcut(KeyCode key, char lower, char upper, Action ctrlAction)
    {
        specialKeyPressHandlers[key] = () => HandleAlphaShortcut(lower, upper, ctrlAction);
        excludedInputStringChars.Add(lower);
        excludedInputStringChars.Add(upper);
    }

    private void RegisterPrintableKey(KeyCode key, char normal, char shifted)
    {
        specialKeyPressHandlers[key] = () => HandlePrintableKey(normal, shifted);
        excludedInputStringChars.Add(normal);
        excludedInputStringChars.Add(shifted);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.CapsLock))
            capsLockActive = !capsLockActive;

        if (windowController.escapeDefocusesAll && Input.GetKeyDown(KeyCode.Escape))
        {
            ConsoleWindowController.DefocusAllAndRecaptureMouse(windowController.escapeCursorLockMode, windowController.escapeCursorVisible);
            return;
        }

        if (windowController.IsFocused && Input.GetMouseButtonDown(0))
        {
            if (!windowController.IsPointerInsideThisConsole(Input.mousePosition))
                windowController.DefocusInternal();
        }

        if (!windowController.IsFocused) return;

        HandleMouseScrolling();
        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        if (stateManager.readOnly) return;

        foreach (char ch in Input.inputString)
        {
            if (!excludedInputStringChars.Contains(ch))
            {
                OnCharTyped(ch);
                latest = KeyCode.None;
            }
        }

        if (!Input.GetKey(latest)) isKeyHeld = false;

        foreach (var kvp in specialKeyPressHandlers)
        {
            KeyCode code = kvp.Key;
            if (Input.GetKeyDown(code))
            {
                if (code != latest) isKeyHeld = false;
                kvp.Value.Invoke();
                latest = code;
                latestDownTime = Time.time;
            }
            if (Input.GetKey(code))
            {
                if (code == latest && !isKeyHeld && Time.time - latestDownTime >= heldKeyTriggerTime)
                {
                    isKeyHeld = true;
                    lastHeldKeyTriggerTime = Time.time;
                }
                else if (code == latest && isKeyHeld && Time.time - lastHeldKeyTriggerTime > heldKeyDelay)
                {
                    kvp.Value.Invoke();
                    lastHeldKeyTriggerTime = Time.time;
                }
            }
        }
    }

    private void HandleMouseScrolling()
    {
        if (mouseListener != null && mouseListener.isMouseDragging)
        {
            float dragDelay = GetTimeBetweenDragScroll();
            if (mouseListener.currentMousePosition.y > 1 && Time.time > lastDragScrollTime + dragDelay)
            {
                MaybeUpScroll();
                Vector2Int loc = windowController.GetCursorLocationForMouse(mouseListener.currentMousePosition);
                stateManager.dragCurrent = new Vector2Int(loc.x + stateManager.verticalScroll, loc.y);
                lastDragScrollTime = Time.time;
            }
            else if (mouseListener.currentMousePosition.y < 0 && Time.time > lastDragScrollTime + dragDelay)
            {
                MaybeDownScroll();
                lastDragScrollTime = Time.time;
            }
        }

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            if (scrollInput > 0) MaybeUpScroll();
            else MaybeDownScroll();
        }
    }

    private void OnMouseDown()
    {
        windowController.FocusFromInteraction();
        Vector2Int loc = windowController.GetCursorLocationForMouse(mouseListener.currentMousePosition);
        stateManager.dragStart = new Vector2Int(loc.x + stateManager.verticalScroll, loc.y + stateManager.horizontalScroll);
        stateManager.dragCurrent = stateManager.dragStart;
        stateManager.cursorRow = loc.x + stateManager.verticalScroll;
        stateManager.visibleCursorCol = stateManager.cursorCol = loc.y + stateManager.horizontalScroll;
        stateManager.NotifyStateChanged();
    }

    private void OnMouseDrag()
    {
        Vector2Int loc = windowController.GetCursorLocationForMouse(mouseListener.currentMousePosition);
        stateManager.dragCurrent = new Vector2Int(loc.x + stateManager.verticalScroll, loc.y + stateManager.horizontalScroll);
        if (stateManager.dragCurrent != stateManager.dragStart) stateManager.isHighlighting = true;
        stateManager.NotifyStateChanged();
    }

    private float GetTimeBetweenDragScroll()
    {
        if (mouseListener.currentMousePosition.y > .95f) return Mathf.Max(0, .2f - (mouseListener.currentMousePosition.y - .95f));
        if (mouseListener.currentMousePosition.y < .05f) return Mathf.Max(0, .2f - (.05f - mouseListener.currentMousePosition.y));
        return 1f;
    }

    // --- Key Binding Logic ---
    private void OnReturnPressed()
    {
        if (!stateManager.allowNewLines) return;
        stateManager.ApplyTransaction(new NewlineTransaction());
    }

    private void OnBackspacePressed() => stateManager.ApplyTransaction(new DeleteTransaction(true));
    private void OnDeletePressed() => stateManager.ApplyTransaction(new DeleteTransaction(false));

    private void OnUpArrowPressed()
    {
        stateManager.isHighlighting = false;
        if (stateManager.cursorRow > 0)
        {
            stateManager.cursorRow--;
            stateManager.visibleCursorCol = Mathf.Min(stateManager.cursorCol, stateManager.GetLineLength(stateManager.cursorRow));
            stateManager.AdjustScrollToCursor();
            stateManager.NotifyStateChanged();
        }
    }

    private void OnDownArrowPressed()
    {
        stateManager.isHighlighting = false;
        if (stateManager.cursorRow < stateManager.lines.Count - 1)
        {
            stateManager.cursorRow++;
            stateManager.visibleCursorCol = Mathf.Min(stateManager.cursorCol, stateManager.GetLineLength(stateManager.cursorRow));
            stateManager.AdjustScrollToCursor();
            stateManager.NotifyStateChanged();
        }
    }

    private void OnLeftArrowPressed()
    {
        stateManager.isHighlighting = false;
        if (stateManager.visibleCursorCol != 0) stateManager.visibleCursorCol--;
        else if (stateManager.cursorRow != 0)
        {
            stateManager.cursorRow--;
            stateManager.visibleCursorCol = stateManager.GetLineLength(stateManager.cursorRow);
        }
        stateManager.cursorCol = stateManager.visibleCursorCol;
        stateManager.AdjustScrollToCursor();
        stateManager.NotifyStateChanged();
    }

    private void OnRightArrowPressed()
    {
        stateManager.isHighlighting = false;
        if (stateManager.visibleCursorCol != stateManager.GetLineLength(stateManager.cursorRow)) stateManager.visibleCursorCol++;
        else if (stateManager.cursorRow < stateManager.lines.Count - 1)
        {
            stateManager.cursorRow++;
            stateManager.visibleCursorCol = 0;
        }
        stateManager.cursorCol = stateManager.visibleCursorCol;
        stateManager.AdjustScrollToCursor();
        stateManager.NotifyStateChanged();
    }

    private void OnTabPressed()
    {
        int numSpaces = stateManager.spacesPerTab - (stateManager.visibleCursorCol % stateManager.spacesPerTab);
        string tab = new string(' ', numSpaces);
        stateManager.ApplyTransaction(new InsertTransaction(tab));
    }

    private void OnCharTyped(char ch)
    {
        if (ch == (char)0) return;
        stateManager.ApplyTransaction(new InsertTransaction(ch + ""));
    }

    private void HandleAlphaShortcut(char lower, char upper, Action ctrlAction)
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            ctrlAction?.Invoke();
            return;
        }

        OnCharTyped(GetResolvedAlphaShortcutChar(lower, upper));
    }

    private char GetResolvedAlphaShortcutChar(char lower, char upper)
    {
        foreach (char ch in Input.inputString)
        {
            if (char.ToLowerInvariant(ch) == char.ToLowerInvariant(lower))
                return ch;
        }

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        return shift ? upper : lower;
    }
    
    private void HandlePrintableKey(char normal, char shifted)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        OnCharTyped(shift ? shifted : normal);
    }

    private bool ShouldUseUppercaseLetter()
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        return shift ^ capsLockActive;
    }

    private bool ShouldUseShiftedPrintable()
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        return shift ^ capsLockActive;
    }

    private void OnCAction() => stateManager.SetCopyBuffer(stateManager.GetHighlightedText());
    private void OnVAction() => stateManager.ApplyTransaction(new InsertTransaction(stateManager.GetCopyBuffer()));
    private void OnXAction()
    {
        if (stateManager.isHighlighting)
        {
            stateManager.SetCopyBuffer(stateManager.GetHighlightedText());
            stateManager.ApplyTransaction(new DeleteTransaction(true));
        }
        stateManager.isHighlighting = false;
    }
    private void SelectAll()
    {
        stateManager.isHighlighting = true;
        stateManager.dragStart = new Vector2Int(0, 0);
        stateManager.dragCurrent = new Vector2Int(stateManager.lines.Count - 1, stateManager.GetLineLength(stateManager.lines.Count - 1));
        stateManager.NotifyStateChanged();
    }

    private void MaybeDownScroll()
    {
        if (stateManager.verticalScroll < stateManager.lines.Count - 1) stateManager.verticalScroll++;
        stateManager.NotifyStateChanged();
    }
    private void MaybeUpScroll()
    {
        if (stateManager.verticalScroll != 0) stateManager.verticalScroll--;
        stateManager.NotifyStateChanged();
    }
}
