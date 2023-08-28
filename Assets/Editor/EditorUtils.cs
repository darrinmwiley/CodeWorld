using UnityEditor;
using UnityEngine;

public class EditorUtils : EditorWindow
{
    private delegate void CustomAction();

    private SimpleObject simpleObject;

    private struct Button
    {
        public CustomAction Function;
        public string Text;
    }

    private Button[] buttons;

    private void OnGUI()
    {
        if (buttons != null)
        {
            foreach (Button button in buttons)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                if (GUILayout.Button(button.Text))
                {
                    button.Function();
                }
                GUILayout.EndHorizontal();
            }
        }
    }

    private GameController GetGameController(){
        return FindObjectOfType<GameController>();
    }

    private void SetCamera()
    {
        GetGameController().fitRectangle(0, 0, 80 * 3f / 5f, 24);
    }

    [MenuItem("Window/Typemage3D Custom Utils")]
    public static void OpenWindow()
    {
        EditorUtils window = GetWindow<EditorUtils>();
        window.InitializeButtons();
        window.Show();
    }

    public void MakeObj()
    {
        GameObject gameObj = new GameObject("simple object");
        simpleObject = gameObj.AddComponent<SimpleObject>();
    }

    public void Cube()
    {
        simpleObject.SetShape(ShapeType.CUBE);
    }

    public void Sphere()
    {
        simpleObject.SetShape(ShapeType.SPHERE);
    }

    private void InitializeButtons()
    {
        buttons = new Button[]
        {
            new Button { Function = MakeObj, Text = "Make object" },
            new Button { Function = Cube, Text = "cube" },
            new Button { Function = Sphere, Text = "sphere" },
            // Add more buttons as needed
        };
    }
}