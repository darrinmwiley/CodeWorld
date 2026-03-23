using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// An adapter component that bridges the standard WindowComponent architecture 
/// with the complex, standalone ConsoleController system.
/// </summary>
public class ConsoleWindowComponent : WindowComponent
{
    [Header("Console Integration")]
    [Tooltip("The ConsoleController instance to inject into this window slot.")]
    public ConsoleController targetConsole;

    [Tooltip("Minimum size required for this console pane.")]
    public Vector2 minConsoleSize = new Vector2(200f, 150f);

    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        Debug.Log($"[DEBUG] ConsoleWindowComponent.Initialize called for slot: {container.name}");

        if (targetConsole == null)
        {
            Debug.LogError($"[DEBUG] No Target Console assigned to ConsoleWindowComponent on {gameObject.name}. Connection aborted.");
            return;
        }

        container.style.flexGrow = 1;

        UIDocument rootDoc = null;
        if (root is MonoBehaviour monoRoot)
        {
            rootDoc = monoRoot.GetComponent<UIDocument>();
        }
        
        Debug.Log($"[DEBUG] Injecting element into ConsoleController: {targetConsole.gameObject.name}");
        targetConsole.BindToElement(container, rootDoc);

        InitializeSubComponents(container, root);
    }

    public override Vector2 GetMinimumSize()
    {
        return minConsoleSize;
    }
}