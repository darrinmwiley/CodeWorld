using UnityEngine;

[CreateAssetMenu(fileName = "UITheme", menuName = "UI/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("Palette")]
    public Color backgroundBase = new Color32(28, 28, 28, 255);
    public Color backgroundSurface = new Color32(60, 60, 60, 230);
    public Color backgroundActive = new Color32(85, 85, 85, 255);
    public Color border = new Color32(80, 80, 80, 255);
    public Color text = new Color32(220, 220, 220, 255);
}
