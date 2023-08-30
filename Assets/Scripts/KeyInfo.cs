using UnityEngine;

public class KeyInfo
{
    public KeyCode? Code { get; }
    public char? Char{ get; }
    public bool IsKeyDown { get; set;}
    public float LastDownTime { get; set;}
    public bool IsShiftPressed { get; set;}
    public bool IsAltPressed { get; set;}
    public bool IsCtrlPressed { get; set;}
    public bool IsCapsLockOn {get; set;}

    public KeyInfo(KeyCode keyCode, bool isKeyDown, float lastDownTime, bool isShiftPressed, bool isAltPressed, bool isCtrlPressed)
    {
        Code = keyCode;
        Char = null;
        IsKeyDown = isKeyDown;
        LastDownTime = lastDownTime;
        IsShiftPressed = isShiftPressed;
        IsAltPressed = isAltPressed;
        IsCtrlPressed = isCtrlPressed;
    }

    public KeyInfo(char ch, bool isKeyDown, float lastDownTime, bool isShiftPressed, bool isAltPressed, bool isCtrlPressed)
    {
        Code = null;
        Char = ch;
        IsKeyDown = isKeyDown;
        LastDownTime = lastDownTime;
        IsShiftPressed = isShiftPressed;
        IsAltPressed = isAltPressed;
        IsCtrlPressed = isCtrlPressed;
    }

    public char ToChar()
    {
        if(Char.HasValue)
            return Char.Value;
        char ch = char.ToLower(KeyListener.KeyCodeToChar(Code.Value));
        if(IsCapsLockOn && char.IsLower(ch))
        {
            ch = char.ToUpper(ch);
        }
        if(IsShiftPressed && char.IsLower(ch))
        {
            ch = char.ToUpper(ch);
        }else if(IsShiftPressed && char.IsUpper(ch))
        {
            ch = char.ToLower(ch);
        }
        return ch;
    }

    public override string ToString()
    {
        if(Char.HasValue)
            return $"KeyCode: {Char.Value}, IsKeyDown: {IsKeyDown}, LastDownTime: {LastDownTime}, Shift: {IsShiftPressed}, Alt: {IsAltPressed}, Ctrl: {IsCtrlPressed}, Caps: {IsCapsLockOn}";
        else
            return $"KeyCode: {Code.Value}, IsKeyDown: {IsKeyDown}, LastDownTime: {LastDownTime}, Shift: {IsShiftPressed}, Alt: {IsAltPressed}, Ctrl: {IsCtrlPressed}, Caps: {IsCapsLockOn}";
    
    }
}