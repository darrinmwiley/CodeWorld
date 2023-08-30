using UnityEngine;

public class KeyInfo
{
    public KeyCode? KeyCode { get; }
    public char? Character { get; }
    public bool IsKeyDown { get; }
    public float LastDownTime { get; }

    public KeyInfo(KeyCode keyCode, bool isKeyDown, float lastDownTime)
    {
        KeyCode = keyCode;
        Character = null;
        IsKeyDown = isKeyDown;
        LastDownTime = lastDownTime;
    }

    public KeyInfo(char character, bool isKeyDown, float lastDownTime)
    {
        KeyCode = null;
        Character = character;
        IsKeyDown = isKeyDown;
        LastDownTime = lastDownTime;
    }

    public override string ToString()
    {
        if (KeyCode.HasValue)
        {
            return $"KeyCode: {KeyCode}, IsKeyDown: {IsKeyDown}, LastDownTime: {LastDownTime}";
        }
        else if (Character.HasValue)
        {
            return $"Character: '{Character}', IsKeyDown: {IsKeyDown}, LastDownTime: {LastDownTime}";
        }
        else
        {
            return "Unknown Key";
        }
    }
}