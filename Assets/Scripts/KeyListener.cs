using UnityEngine;
using System;
using System.Collections.Generic;

public class KeyListener : MonoBehaviour
{
    private Dictionary<KeyCode, KeyState> keyStates = new Dictionary<KeyCode, KeyState>();
    private KeyInfo lastKeyPressed = null;

    void Start()
    {
        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            keyCodeStates[keyCode] = new KeyState();
        }
    }

    public KeyInfo GetKeyInfo(KeyCode keyCode)
    {
        return keyStates(keyCode);
    }

    public KeyInfo GetLatestKeyPress()
    {
        return lastKeyPressed;
    }

    void Update()
    {
        // Update the key states and down times for KeyCodes
        foreach (KeyCode keyCode in keyCodeStates.Keys)
        {
            bool keyState = Input.GetKey(keyCode);
            float currentTime = Time.time;
            if (keyState)
            {
                if(!keyCodeStates[keyCode].IsKeyDown){
                    keyCodeStates[keyCode].IsKeyDown = true;
                    keyCodeStates[keyCode].LastDownTime = currentTime;
                    lastKeyPressed = new KeyInfo(keyCode, true, currentTime);
                }
                keyCodeStates[keyCode].IsKeyDown = true;
            }
            else
            {
                keyCodeStates[keyCode].IsKeyDown = false;
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
            
            case KeyCode.Space: return ' ';
            case KeyCode.Tab: return '\t';
            
            // Add more cases for other typable symbols if needed.
            case KeyCode.Comma: return ',';
            case KeyCode.Period: return '.';
            case KeyCode.Exclaim: return '!';
            
            default: return '?'; // Return a question mark for unrecognized keys.
        }
    }
}
