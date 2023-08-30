using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class KeyListener : MonoBehaviour
{
    private Dictionary<KeyCode, KeyInfo> keyStates = new Dictionary<KeyCode, KeyInfo>();
    private Dictionary<char, KeyInfo> charStates = new Dictionary<char, KeyInfo>();
    private KeyInfo lastKeyPressed = null;
    public List<KeyInfo> queue = new List<KeyInfo>();

    bool isShiftPressed = false;
    bool isAltPressed = false;
    bool isCtrlPressed = false;
    bool isCapsLockOn = false;

    [DllImport("user32.dll")]
    public static extern short GetKeyState(int keyCode);

    void Start()
    {
        isCapsLockOn = (((ushort)GetKeyState(0x14)) & 0xffff) != 0;//init stat
        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            keyStates[keyCode] = new KeyInfo(keyCode, false, 0, false, false, false);
        }
    }

    public KeyInfo GetKeyInfo(KeyCode keyCode)
    {
        return keyStates[keyCode];
    }

    public KeyInfo GetLatestKeyPress()
    {
        return lastKeyPressed;
    }

    void Update()
    {
        // Update Shift, Alt, and Ctrl key status
        isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        isAltPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        isCtrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if(Input.GetKeyDown(KeyCode.CapsLock) && !keyStates[KeyCode.CapsLock].IsKeyDown)
        {
            isCapsLockOn  = !isCapsLockOn;
        }

        queue.Clear();
        foreach(char ch in Input.inputString)
        { 
            queue.Add(keyStates[charToKeyCode(ch)]);
        }
        
        // Update the key states and down times for KeyCodes
        foreach (KeyCode keyCode in keyStates.Keys)
        {
            bool keyState = Input.GetKey(keyCode);
            float currentTime = Time.time;
            if (keyState)
            {
                if (!keyStates[keyCode].IsKeyDown)
                {
                    if(!Input.inputString.Contains(KeyListener.KeyCodeToChar(keyCode)))
                    {
                        queue.Add(keyStates[keyCode]);
                    }
                    keyStates[keyCode].LastDownTime = currentTime;
                    keyStates[keyCode].IsShiftPressed = isShiftPressed;
                    keyStates[keyCode].IsAltPressed = isAltPressed;
                    keyStates[keyCode].IsCtrlPressed = isCtrlPressed;
                    keyStates[keyCode].IsCapsLockOn = isCapsLockOn;
                    lastKeyPressed = keyStates[keyCode];
                }
                keyStates[keyCode].IsKeyDown = true;
            }
            else
            {
                keyStates[keyCode].IsKeyDown = false;
            }
        }
    }
    
    public static char KeyCodeToChar(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.A: return 'A';
            case KeyCode.B: return 'B';
            case KeyCode.C: return 'C';
            case KeyCode.D: return 'D';
            case KeyCode.E: return 'E';
            case KeyCode.F: return 'F';
            case KeyCode.G: return 'G';
            case KeyCode.H: return 'H';
            case KeyCode.I: return 'I';
            case KeyCode.J: return 'J';
            case KeyCode.K: return 'K';
            case KeyCode.L: return 'L';
            case KeyCode.M: return 'M';
            case KeyCode.N: return 'N';
            case KeyCode.O: return 'O';
            case KeyCode.P: return 'P';
            case KeyCode.Q: return 'Q';
            case KeyCode.R: return 'R';
            case KeyCode.S: return 'S';
            case KeyCode.T: return 'T';
            case KeyCode.U: return 'U';
            case KeyCode.V: return 'V';
            case KeyCode.W: return 'W';
            case KeyCode.X: return 'X';
            case KeyCode.Y: return 'Y';
            case KeyCode.Z: return 'Z';

            case KeyCode.Alpha0: return '0';
            case KeyCode.Alpha1: return '1';
            case KeyCode.Alpha2: return '2';
            case KeyCode.Alpha3: return '3';
            case KeyCode.Alpha4: return '4';
            case KeyCode.Alpha5: return '5';
            case KeyCode.Alpha6: return '6';
            case KeyCode.Alpha7: return '7';
            case KeyCode.Alpha8: return '8';
            case KeyCode.Alpha9: return '9';

            case KeyCode.Keypad0: return '0';
            case KeyCode.Keypad1: return '1';
            case KeyCode.Keypad2: return '2';
            case KeyCode.Keypad3: return '3';
            case KeyCode.Keypad4: return '4';
            case KeyCode.Keypad5: return '5';
            case KeyCode.Keypad6: return '6';
            case KeyCode.Keypad7: return '7';
            case KeyCode.Keypad8: return '8';
            case KeyCode.Keypad9: return '9';

            case KeyCode.Space: return ' ';
            case KeyCode.Tab: return '\t';

            // Add more cases for other typable symbols if needed.
            case KeyCode.Comma: return ',';
            case KeyCode.Period: return '.';
            case KeyCode.Exclaim: return '!';

            default: return (char)(0); // Return 0 for unrecognized keys.
        }
    }

    public static KeyCode charToKeyCode(char ch)
    {
        switch (ch)
        {
            case 'A': return KeyCode.A;
            case 'B': return KeyCode.B;
            case 'C': return KeyCode.C;
            case 'D': return KeyCode.D;
            case 'E': return KeyCode.E;
            case 'F': return KeyCode.F;
            case 'G': return KeyCode.G;
            case 'H': return KeyCode.H;
            case 'I': return KeyCode.I;
            case 'J': return KeyCode.J;
            case 'K': return KeyCode.K;
            case 'L': return KeyCode.L;
            case 'M': return KeyCode.M;
            case 'N': return KeyCode.N;
            case 'O': return KeyCode.O;
            case 'P': return KeyCode.P;
            case 'Q': return KeyCode.Q;
            case 'R': return KeyCode.R;
            case 'S': return KeyCode.S;
            case 'T': return KeyCode.T;
            case 'U': return KeyCode.U;
            case 'V': return KeyCode.V;
            case 'W': return KeyCode.W;
            case 'X': return KeyCode.X;
            case 'Y': return KeyCode.Y;
            case 'Z': return KeyCode.Z;

            case '0': return KeyCode.Alpha0;
            case '1': return KeyCode.Alpha1;
            case '2': return KeyCode.Alpha2;
            case '3': return KeyCode.Alpha3;
            case '4': return KeyCode.Alpha4;
            case '5': return KeyCode.Alpha5;
            case '6': return KeyCode.Alpha6;
            case '7': return KeyCode.Alpha7;
            case '8': return KeyCode.Alpha8;
            case '9': return KeyCode.Alpha9;

            case ' ': return KeyCode.Space;
            case '\t': return KeyCode.Tab;

            // Add more cases for other typable symbols if needed.
            case ',': return KeyCode.Comma;
            case '.': return KeyCode.Period;
            case '!': return KeyCode.Exclaim;

            default: return KeyCode.None; // Return KeyCode.None for unrecognized characters.
        }
    }

}
