using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A generic component used to inject a VisualTreeAsset into a window slot.
/// This is used for "Leaf" content that doesn't require unique C# logic,
/// such as a Console view, a Code display, or a Tab list.
/// </summary>
public class StaticContentController : WindowComponent
{
    [Header("Static Content")]
    [Tooltip("The UXML asset to instantiate into the parent's slot.")]
    [SerializeField] private VisualTreeAsset _contentAsset;

    /// <summary>
    /// Injects the asset into the container and continues the recursive initialization
    /// for any sub-components defined in the Inspector.
    /// </summary>
    public override void Initialize(VisualElement container, IBaseWindow root)
    {
        if (container == null) return;

        container.Clear();

        if (_contentAsset != null)
        {
            // Instantiate the UXML
            VisualElement instance = _contentAsset.Instantiate();
            
            // Ensure it fills the slot entirely
            instance.style.flexGrow = 1;
            instance.style.width = Length.Percent(100);
            instance.style.height = Length.Percent(100);
            
            container.Add(instance);

            // Continue the chain: if this "static" content has 
            // defined slots for further sub-components, initialize them.
            InitializeSubComponents(instance, root);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] No VisualTreeAsset assigned to StaticContentController.");
        }
    }
}